using System.Net.Http.Json;

namespace SC.Api.Services
{
    public interface IGameServerClient
    {
        Task<bool> HasActiveGameAsync(int playerId);
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

        public async Task<bool> HasActiveGameAsync(int playerId)
        {
            try
            {
                var response = await _http.GetAsync($"/internal/active-game/{playerId}");
                if (!response.IsSuccessStatusCode) return false;
                var result = await response.Content.ReadFromJsonAsync<HasActiveGameResponse>();
                return result?.HasActiveGame ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GameServer not reachable, assuming no active game");
                return false;
            }
        }

        private class HasActiveGameResponse
        {
            public bool HasActiveGame { get; set; }
        }
    }
}