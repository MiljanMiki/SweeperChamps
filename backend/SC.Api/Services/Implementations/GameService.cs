using Humanizer;
using SC.Api.Services.Interfaces;
using SC.Domain.DataModels;
using SC.Domain.Repositories.AsyncInterfaces;

namespace SC.Api.Services.Implementations
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameSettingRepository _gameSettingRepository;
        private readonly IGamePlayerRepository _gamePlayerRepository;

        public GameService(IGameRepository gameRepository,IGameSettingRepository gameSettingRepository ,IGamePlayerRepository gamePlayerRepository)
        {
            _gameRepository = gameRepository;
            _gameSettingRepository = gameSettingRepository;
            _gamePlayerRepository = gamePlayerRepository;
        }

        

        public async Task<int> CreateGame(int gameSettingId, List<string> players, bool isRanked)
        {
            if (gameSettingId <= 0)
                throw new ArgumentException($"{nameof(GameSetting)} ID cannot be negative or 0!");
            if (players.Count == 0)
                throw new ArgumentException("Player count is 0!");
            if (players.Count % 2 != 0)
                throw new ArgumentException("Player count must be a multiple of 2!");

            var setting = await _gameSettingRepository.GetAsync(gameSettingId);
            if (setting == null)
                throw new KeyNotFoundException($"Invalid {nameof(GameSetting)} ID: {gameSettingId}. It does not map to any row");


            Game game = new Game
            {
                StartTime = DateTime.Now,
                EndTime = null,
                Status = GameStatuses.InProgress,
                IsRanked = isRanked,
                DurationSeconds = null,
                WinningTeam = null,
                GameSettingsId = gameSettingId,
            };

            _gameRepository.Add(game);

            await _gameRepository.SaveChangesAsync();

            var gameId = game.GamesId;

            int counter = 0;
            List<GamePlayer> playerList = new List<GamePlayer>();
            foreach(var player in players)
            {
                GamePlayer gp = new GamePlayer
                {
                    GameId = gameId,
                    PlayerId = Int32.Parse(player),
                    Score = 0,
                    TeamColor = counter % 2 == 0 ? TeamColors.Red : TeamColors.Blue,
                    Outcome = Outcomes.Pending,
                    EloChange = null,
                    Accuracy = 0
                };

                playerList.Add(gp);

                ++counter;
            }

            game.GamePlayers = playerList;
            _gameRepository.Update(game);


            await _gameRepository.SaveChangesAsync();

            return gameId;
        }

        public async Task MarkGameFinished()
        {
            throw new NotImplementedException();
        }
    }
}
