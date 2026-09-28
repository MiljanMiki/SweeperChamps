using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SC.Api.Hubs;
using SC.Api.Hubs.Interfaces;
using SC_Backend.DataContext;
using SC.Messaging;
using SC.Messaging.Matchmaker;
using System.Text;
using System.Text.Json;

namespace SC.Api.Services
{
    public class MatchmakingResultsConsumer : BackgroundService
    {
        private readonly IHubContext<MatchmakingHub, IMatchmakingClient> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MatchmakingResultsConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IGameCreatedPublisher _gameCreatedPublisher;
        private IConnection? _connection;
        private IChannel? _channel;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            IncludeFields = true
        };

        public MatchmakingResultsConsumer(
            IHubContext<MatchmakingHub, IMatchmakingClient> hubContext,
            IConfiguration configuration,
            ILogger<MatchmakingResultsConsumer> logger,
            IServiceScopeFactory scopeFactory,
            IGameCreatedPublisher gameCreatedPublisher)
        {
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _gameCreatedPublisher = gameCreatedPublisher;
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

            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqConstants.MatchmakingExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                RabbitMqConstants.MatchmakingResultsQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueBindAsync(
                RabbitMqConstants.MatchmakingResultsQueue,
                RabbitMqConstants.MatchmakingExchange,
                "match.found",
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("RAW MATCH FOUND JSON: {Json}", json);

                    var matchEvent = JsonSerializer.Deserialize<MatchFoundEvent>(json, JsonOptions);

                    if (matchEvent != null)
                    {
                        // ── 1. Load the game + settings + players from DB ──
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var game = await db.Games
                            .Include(g => g.GameSettings)
                            .Include(g => g.GamePlayers)
                            .FirstOrDefaultAsync(g => g.GamesId == matchEvent.GameId, stoppingToken);

                        if (game == null)
                        {
                            _logger.LogError("Game {GameId} not found in DB", matchEvent.GameId);
                            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                            return;
                        }

                        // ── 2. Build settings DTO ──
                        var settingsDto = new GameSettingsDto
                        {
                            Width = game.GameSettings.Width,
                            Height = game.GameSettings.Height,
                            NumberOfMines = game.GameSettings.NumberOfMines,
                            StartTimeSeconds = game.GameSettings.StartTimeSeconds,
                            TeamSize = game.GameSettings.TeamSize,
                            WinCondition = game.GameSettings.WinCondition.ToString(),
                            HasPowerUps = game.GameSettings.HasPowerUps
                        };

                        // ── 3. Look up usernames for all players in one query ──
                        var playerIds = game.GamePlayers.Select(gp => gp.PlayerId).ToList();
                        var usernames = await db.Users
                            .Where(u => playerIds.Contains(u.UsersId))
                            .ToDictionaryAsync(u => u.UsersId, u => u.Username, stoppingToken);

                        // ── 4. Build players DTO with usernames ──
                        var playersDto = game.GamePlayers
                            .Select(gp => new GamePlayerDto
                            {
                                PlayerId = gp.PlayerId,
                                Username = usernames.TryGetValue(gp.PlayerId, out var name)
                                            ? name
                                            : $"Player {gp.PlayerId}",
                                TeamColor = gp.TeamColor.ToString()
                            })
                            .ToList();

                        // ── 5. Publish to GameServer ──
                        await _gameCreatedPublisher.PublishAsync(new GameCreatedMessage
                        {
                            GameId = game.GamesId,
                            GameSettings = settingsDto,
                            Players = playersDto
                        });

                        // ── 6. Notify browsers via SignalR ──
                        await _hubContext.Clients
                            .Users(matchEvent.UserIds)
                            .MatchFound(new MatchFoundPayload
                            {
                                GameId = game.GamesId,
                                GameSettings = settingsDto,
                                Players = playersDto
                            });

                        _logger.LogInformation(
                            "Notified clients and published game.created for Game {GameId}",
                            matchEvent.GameId);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing match results.");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await _channel.BasicConsumeAsync(
                RabbitMqConstants.MatchmakingResultsQueue,
                autoAck: false,
                consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("MatchmakingResultsConsumer started, listening on 'match.found'.");

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