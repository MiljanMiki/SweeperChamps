using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SC.Matchmaker.Core;
using SC.Messaging;
using SC.Messaging.Matchmaker;
using System.Text;
using System.Text.Json;

namespace SC.Matchmaker;

public class MatchmakingWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatchmakingWorker> _logger;
    private readonly MatchmakingEngine _engine;
    private IConnection? _connection;
    private IChannel? _channel;

    public MatchmakingWorker(IConfiguration configuration, ILogger<MatchmakingWorker> logger, MatchmakingEngine engine)
    {
        _configuration = configuration;
        _logger = logger;
        _engine = engine;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // 1. Ensure exchange exists (matching the API project)
        await _channel.ExchangeDeclareAsync(
            exchange: RabbitMqConstants.MatchmakingExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var routingKeyPattern = _configuration["WorkerConfig:RoutingKeyPattern"] ?? "ticket.*.casual";
        var queueName = $"matchmaking.tickets.{_configuration["WorkerConfig:QueueType"] ?? "casual"}.queue";

        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(queueName, RabbitMqConstants.MatchmakingExchange, routingKeyPattern, cancellationToken: stoppingToken);

        // 3. Declare and bind the Cancellations Queue
        await _channel.QueueDeclareAsync(RabbitMqConstants.TicketCancellationsQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(RabbitMqConstants.TicketCancellationsQueue, RabbitMqConstants.MatchmakingExchange, "ticket.cancel", cancellationToken: stoppingToken);

        // 4. Setup Consumers
        var ticketConsumer = new AsyncEventingBasicConsumer(_channel);
        ticketConsumer.ReceivedAsync += async (sender, ea) => await HandleTicketReceived(ea);

        var cancelConsumer = new AsyncEventingBasicConsumer(_channel);
        cancelConsumer.ReceivedAsync += async (sender, ea) => await HandleCancelReceived(ea);

        // Start consuming
        await _channel.BasicConsumeAsync(queueName, autoAck: false, ticketConsumer, cancellationToken: stoppingToken);
        await _channel.BasicConsumeAsync(RabbitMqConstants.TicketCancellationsQueue, autoAck: false, cancelConsumer, cancellationToken: stoppingToken);

        _logger.LogInformation("Matchmaking Worker started. Waiting for tickets...");

        // Keep the worker alive until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task HandleTicketReceived(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var ticket = JsonSerializer.Deserialize<MatchTicketRequest>(json);

            if (ticket != null)
            {
                _logger.LogInformation("Received Ticket: User {UserId}, Settings {SettingsId}, Ranked: {IsRanked}",
                    ticket.UserId, ticket.GameSettingsId, ticket.IsRanked);

                await _engine.ProcessNewTicket(ticket);
            }

            // Acknowledge the message so RabbitMQ removes it from the queue
            if (_channel != null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticket request.");
            // Nack with requeue=false if it's a poison message (bad JSON) to avoid infinite loops
            if (_channel != null) await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    private async Task HandleCancelReceived(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var cancelEvent = JsonSerializer.Deserialize<CancelTicketEvent>(json);

            if (cancelEvent != null)
            {
                _logger.LogInformation("Received Cancellation for User {UserId}", cancelEvent.UserId);

                // TODO in Phase 4: Remove ticket from in-memory pool
                _engine.CancelTicket(cancelEvent.UserId);
            }

            if (_channel != null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cancellation.");
            if (_channel != null) await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }   
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) await _channel.CloseAsync(cancellationToken);
        if (_connection is { IsOpen: true }) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}