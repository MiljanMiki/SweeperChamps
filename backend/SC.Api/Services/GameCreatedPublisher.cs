using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using SC.Api.Hubs.Interfaces;

namespace SC.Api.Services
{
    public interface IGameCreatedPublisher
    {
        Task PublishAsync(GameCreatedMessage message);
    }

    public class GameCreatedMessage
    {
        public int GameId { get; set; }
        public GameSettingsDto GameSettings { get; set; } = new();
        public List<GamePlayerDto> Players { get; set; } = new();
    }

    public class GameCreatedPublisher : IGameCreatedPublisher
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly IConnection _connection;
        private readonly ILogger<GameCreatedPublisher> _logger;

        public GameCreatedPublisher(IConnection connection, ILogger<GameCreatedPublisher> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task PublishAsync(GameCreatedMessage message)
        {
            using var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "game.created",
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message, JsonOptions);
            _logger.LogInformation("PUBLISHING game.created: {Json}", json);

            var body = Encoding.UTF8.GetBytes(json);
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "game.created",
                mandatory: false,
                basicProperties: props,
                body: body);
        }
    }
}