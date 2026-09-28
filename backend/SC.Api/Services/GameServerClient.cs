using System.Net.Http.Json;

namespace SC.Api.Services
{
    public interface IGameServerClient
    {
        Task<ActiveGameInfo?> GetActiveGameForPlayerAsync(int playerId);
    }

    public class ActiveGameInfo
    {
        public bool HasActiveGame { get; set; }
        public int? GameId { get; set; }
    }

    public class GameServerClient : IGameServerClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<GameServerClient> _logger;

        public GameServerClient(HttpClient http, ILogger<GameServerClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<ActiveGameInfo?> GetActiveGameForPlayerAsync(int playerId)
        {
            try
            {
                var response = await _http.GetAsync($"/internal/active-game/{playerId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GameServer returned {Status} for player {PlayerId}",
                        response.StatusCode, playerId);
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<ActiveGameInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GameServer unreachable");
                return null;
            }
        }
    }
}