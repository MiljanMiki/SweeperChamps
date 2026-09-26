using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SC.Api.Hubs;
using SC.Api.Hubs.Interfaces;
using SC.Messaging;
using SC.Messaging.Matchmaker;
using System.Text;
using System.Text.Json;

namespace SC.Api.Services
{
    public class MatchmakingResultsConsumer : BackgroundService
    {
        // IHubContext allows calling SignalR clients from outside the Hub itself
        private readonly IHubContext<MatchmakingHub, IMatchmakingClient> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MatchmakingResultsConsumer> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public MatchmakingResultsConsumer(
            IHubContext<MatchmakingHub, IMatchmakingClient> hubContext,
            IConfiguration configuration,
            ILogger<MatchmakingResultsConsumer> logger)
        {
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
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

            // Declare queue and bind to "match.found" routing key
            await _channel.QueueDeclareAsync(RabbitMqConstants.MatchmakingResultsQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(RabbitMqConstants.MatchmakingResultsQueue, RabbitMqConstants.MatchmakingExchange, "match.found", cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var matchEvent = JsonSerializer.Deserialize<MatchFoundEvent>(json);

                    if (matchEvent != null)
                    {
                        _logger.LogInformation("Notifying clients for Game {GameId}", matchEvent.GameId);

                        // SIGNALR MAGIC: Broadcast ONLY to the users in this specific match
                        await _hubContext.Clients.Users(matchEvent.UserIds).MatchFound(matchEvent.GameId);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing match results.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await _channel.BasicConsumeAsync(RabbitMqConstants.MatchmakingResultsQueue, autoAck: false, consumer, cancellationToken: stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is { IsOpen: true }) await _channel.CloseAsync(cancellationToken);
            if (_connection is { IsOpen: true }) await _connection.CloseAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
