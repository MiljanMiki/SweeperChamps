using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SC_Backend.DataContext;
using SC.Domain.DataModels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC.Api.Services
{
    // ─────────────────────────────────────────────
    // Message contract (from GameServer)
    // ─────────────────────────────────────────────
    public class GameFinishedMessage
    {
        public int GameId { get; set; }
        public DateTime EndTime { get; set; }
        public object? Status { get; set; }   // accepts string or int
        public List<PlayerResultDto> Results { get; set; } = new();
    }

    public class PlayerResultDto
    {
        public int PlayerId { get; set; }
        public int Score { get; set; }
    }

    public class GameFinishedConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GameFinishedConsumer> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public GameFinishedConsumer(
            IConnection connection,
            IServiceScopeFactory scopeFactory,
            ILogger<GameFinishedConsumer> logger)
        {
            _connection = connection;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: "game.finished",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    _logger.LogInformation("RAW game.finished: {Json}", json);

                    var msg = JsonSerializer.Deserialize<GameFinishedMessage>(json, JsonOptions);
                    if (msg == null)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // ── 1. Load game + its players ──
                    var game = await db.Games
                        .Include(g => g.GamePlayers)
                        .FirstOrDefaultAsync(g => g.GamesId == msg.GameId, stoppingToken);

                    if (game == null)
                    {
                        _logger.LogWarning("game.finished for unknown game {GameId}", msg.GameId);
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    // ── 2. Determine winning team by total score per team ──
                    var winningTeam = ComputeWinningTeam(game, msg.Results);

                    // ── 3. Update the Game row ──
                    game.Status = GameStatuses.Finished;
                    game.EndTime = DateTime.SpecifyKind(msg.EndTime, DateTimeKind.Unspecified); game.DurationSeconds = Math.Max(0, (int)(msg.EndTime - game.StartTime).TotalSeconds);
                    game.WinningTeam = winningTeam;

                    // ── 4. Update each GamePlayer row ──
                    foreach (var gp in game.GamePlayers)
                    {
                        var result = msg.Results.FirstOrDefault(r => r.PlayerId == gp.PlayerId);
                        if (result == null) continue;

                        gp.Score = result.Score;
                        gp.Outcome = ComputeOutcome(gp.TeamColor, winningTeam);
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Game {GameId} marked finished. Duration {Duration}s, Winner {Winner}",
                        game.GamesId, game.DurationSeconds, winningTeam);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling game.finished");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "game.finished",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("GameFinishedConsumer started, listening on 'game.finished'.");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private static TeamColors? ComputeWinningTeam(
            SC.Domain.DataModels.Game game,
            List<PlayerResultDto> results)
        {
            if (game.GamePlayers == null || game.GamePlayers.Count == 0)
                return null;

            int redScore = 0;
            int blueScore = 0;

            foreach (var gp in game.GamePlayers)
            {
                var r = results.FirstOrDefault(x => x.PlayerId == gp.PlayerId);
                int score = r?.Score ?? 0;

                if (gp.TeamColor == TeamColors.Red) redScore += score;
                else if (gp.TeamColor == TeamColors.Blue) blueScore += score;
            }

            if (redScore == blueScore) return null;      // draw
            return redScore > blueScore ? TeamColors.Red : TeamColors.Blue;
        }

        private static Outcomes ComputeOutcome(TeamColors playerTeam, TeamColors? winningTeam)
        {
            if (winningTeam == null) return Outcomes.Draw;
            return playerTeam == winningTeam ? Outcomes.Win : Outcomes.Loss;
        }
    }
}