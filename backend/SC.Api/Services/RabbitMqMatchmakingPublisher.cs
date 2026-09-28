using RabbitMQ.Client;
using SC.Api.Services.Interfaces;
using SC.Messaging;
using SC.Messaging.Matchmaker;
using System.Text;
using System.Text.Json;

namespace SC.Api.Services
{
    public class RabbitMqMatchmakingPublisher : IMatchmakingPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMqMatchmakingPublisher> _logger;

        // IncludeFields = true guards against future message classes using public fields.
        // JsonSerializerDefaults.Web = camelCase + case-insensitive deserialization.
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            IncludeFields = true
        };

        public RabbitMqMatchmakingPublisher(
            IConfiguration configuration,
            ILogger<RabbitMqMatchmakingPublisher> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(
                exchange: RabbitMqConstants.MatchmakingExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            ).GetAwaiter().GetResult();
        }

        public void PublishTicket(MatchTicketRequest ticket)
        {
            var routingKey = RabbitMqConstants.GetTicketRoutingKey(ticket.GameSettingsId, ticket.IsRanked);
            PublishMessage(routingKey, ticket);

            _logger.LogInformation(
                "Published MatchTicketRequest for User {UserId} with RoutingKey '{RoutingKey}'",
                ticket.UserId, routingKey);
        }

        public void PublishCancelTicket(string userId)
        {
            var cancelEvent = new CancelTicketEvent(userId);
            PublishMessage("ticket.cancel", cancelEvent);

            _logger.LogInformation("Published CancelTicketEvent for User {UserId}", userId);
        }

        private void PublishMessage<T>(string routingKey, T message)
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);

            // DEBUG LOG — remove once matchmaking works
            _logger.LogInformation("PUBLISHING JSON to '{RoutingKey}': {Json}", routingKey, json);

            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            _channel.BasicPublishAsync(
                exchange: RabbitMqConstants.MatchmakingExchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body
            ).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (_channel.IsOpen)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _channel.Dispose();
            }

            if (_connection.IsOpen)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
                _connection.Dispose();
            }
        }
    }
}