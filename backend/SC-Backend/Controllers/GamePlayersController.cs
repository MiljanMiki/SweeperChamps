using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using SC_Backend.DataContext;
using SC_Backend.DataModels;
using SC_Backend.DTOs.GamePlayers;
using SC_Backend.DTOs.Games;
using SC_Backend.Repositories.AsyncInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SC_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamePlayersController : ControllerBase
    {
        private readonly IGamePlayerRepository _gamePlayerRepository;

        public GamePlayersController( IGamePlayerRepository gamePlayerRepository)
        {
            _gamePlayerRepository = gamePlayerRepository;
        }

        #region CRUD
        // GET: api/GamePlayers
        //PROMENI U DTO
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<GamePlayerDto>>> GetGamePlayersAsync()
        {
            var list = await _gamePlayerRepository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        // GET: api/GamePlayers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GamePlayerDto>> GetGamePlayerAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var gamePlayer = await _gamePlayerRepository.GetAsync(id);

            if (gamePlayer == null)
            {
                return NotFound();
            }

            return MapToDto(gamePlayer);
        }

        // PUT: api/GamePlayers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGamePlayerAsync(int id, PutGamePlayerRequestDto gamePlayerDto)
        {
            if (gamePlayerDto == null)
                return BadRequest("DTO is null");

            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");
            if (gamePlayerDto.Score < 0)
                return BadRequest("Score cannot be negative");
            if (!Enum.IsDefined(typeof(TeamColors), gamePlayerDto.TeamColor))
                return BadRequest("Enum value is not defined");

            var gamePlayer = await _gamePlayerRepository.GetAsync(id);

            if (gamePlayer == null)
            {
                return BadRequest($"Game player with ID {id} doesnt exist.");
            }


            gamePlayer.Score = gamePlayerDto.Score;
            gamePlayer.TeamColor = gamePlayerDto.TeamColor;

            //_gamePlayerRepository.Update(gamePlayer);

            try
            {
                await _gamePlayerRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await GamePlayerExists(id) == false)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/GamePlayers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GamePlayer>> PostGamePlayerAsync(GamePlayerDto gamePlayerDto)
        {
            if (gamePlayerDto == null)
                return BadRequest("DTO is null");
            if (gamePlayerDto.PlayerId <= 0)
                return BadRequest("ID of player cannot be negative or 0.");
            if (gamePlayerDto.GameId <= 0)
                return BadRequest("ID of game cannot be negative or 0.");
            if (gamePlayerDto.Score < 0)
                return BadRequest("Score cannot be negative.");
            if (!Enum.IsDefined(typeof(TeamColors), gamePlayerDto.TeamColor))
                return BadRequest("Enum value is not defined");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var gamePlayer = new GamePlayer
            {
                GameId = gamePlayerDto.GameId,
                PlayerId = gamePlayerDto.PlayerId,
                TeamColor = gamePlayerDto.TeamColor,
                Score = gamePlayerDto.Score
            };

            try
            {
                _gamePlayerRepository.Add(gamePlayer);
                await _gamePlayerRepository.SaveChangesAsync();
            }
            catch(ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            

            return CreatedAtAction(nameof(GetGamePlayerAsync), new { id = gamePlayer.GamePlayersId }, gamePlayer);
        }

        // DELETE: api/GamePlayers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGamePlayerAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var gamePlayer = await _gamePlayerRepository.GetAsync(id);
            if (gamePlayer == null)
            {
                return NotFound();
            }

            try
            {
                _gamePlayerRepository.Delete(gamePlayer);
                await _gamePlayerRepository.SaveChangesAsync();
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        #endregion CRUD

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<PlayerSummaryDto>>> GetAllPlayersFromGameAsync(int gameId)
        {
            if (gameId <= 0)
                return BadRequest("ID cannot be negative or 0.");


            try
            {
                var listaIgraca = (await _gamePlayerRepository.GetAllPlayersFromGameAsync(gameId)).ToList();

                if (listaIgraca.Count == 0)
                    return BadRequest("Returned 0 players.");

                if (listaIgraca.Count % 2 != 0)
                    return BadRequest("Number of players in a game must be even.");


                return Ok(listaIgraca.Select(player => new PlayerSummaryDto
                {
                    PlayerId = player.PlayerId,
                    Username = player.Player.Username,
                    TeamColor = player.TeamColor.ToString(),
                    Score = player.Score,
                    Elo = player.Player.Elo,
                }).ToList());
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("player/{playerId}")]
        public async Task<ActionResult<IEnumerable<GameSummaryDto>>> GetAllGamesFromPlayerAsync(int playerId, bool orderByScore = false)
        {
            if (playerId <= 0)
                return BadRequest("ID cannot be negative or 0");

            var player = await _gamePlayerRepository.GetAsync(playerId);
            if (player == null)
                return BadRequest($"Player with ID {playerId} doesnt exist.");

            var listaIgra = await _gamePlayerRepository.GetGamesFromPlayerAsync(playerId, orderByScore);

            return listaIgra.Select(game => new GameSummaryDto
            {
                GamesId = game.GamesId,
                StartTime = game.StartTime,
                EndTime = game.EndTime,
                Status = game.Status,
                Score = player.Score
            }).ToList();
        }
        [HttpGet("head-to-head")]
        public async Task<ActionResult<IEnumerable<AllGamesTwoPlayersRequestDto>>> GamesBetweenPlayersAsync(int[] playerIDs)
        {
            try
            {
                var games = await _gamePlayerRepository.GamesBetweenPlayersAsync(playerIDs);

                var gamesDTOs = games.Select(g => new AllGamesTwoPlayersRequestDto
                {
                    GamesId = g.GamesId,
                    StartTime = g.StartTime,
                    EndTime = g.EndTime,
                    Status = g.Status,
                    Width = g.GameSettings.Width,
                    Height = g.GameSettings.Height,
                    NumberOfMines = g.GameSettings.NumberOfMines,
                    PlayerSummary = g.GamePlayers.Select(player => new PlayerSummaryDto
                    {
                        PlayerId = player.PlayerId,
                        Username = player.Player.Username,
                        TeamColor = player.TeamColor.ToString(),
                        Score = player.Score,
                        Elo = player.Player.Elo
                    }).ToList()
                }).ToList();

                return Ok(gamesDTOs);

            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch(ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("loaded/{id}")]
        public async Task<ActionResult<LoadedPlayerDto>> GetLoadedGamePlayerAsync(int id)
        {
            if (id <= 0)
                return BadRequest("ID cannot be negative or 0");

            var gp = await _gamePlayerRepository.GetLoadedGamePlayerAsync(id);

            if (gp == null)
                return NotFound($"{nameof(GamePlayer)} with the given ID {id} does not exist or the navigation properties are null.");

            return Ok(new LoadedPlayerDto
            {
                GamePlayer = MapToDto(gp),
                Game = new GameSummaryDto
                {
                    GamesId = gp.Game.GamesId,
                    StartTime = gp.Game.StartTime,
                    EndTime = gp.Game.EndTime,
                    Status = gp.Game.Status,
                    Score = gp.Score//redundantno!!!! vec ga ima u GamePlayer
                },
                User = new UserSummaryDto
                {
                    Username = gp.Player.Username,
                    Datecreated = gp.Player.Datecreated,
                    Elo = gp.Player.Elo
                }
            });
        }

        [HttpGet("from-setting/{playerID}/{settingID}")]
        public async Task<ActionResult<IEnumerable<DTOs.Games.GameDto>>> GetGamesFromSetting(int playerID, int settingID)
        {
            if (playerID <= 0 || settingID <= 0)
                return BadRequest("ID cannot be negative or 0");

            try
            {
                var games = await _gamePlayerRepository.GetGamesFromPlayerWithSettingAsync(playerID, settingID);


                return Ok(games.Select(g => new GameDto
                {
                    GamesId = g.GamesId,
                    StartTime = g.StartTime,
                    EndTime = g.EndTime,
                    Status = g.Status,
                    IsRanked = g.IsRanked,
                    DurationSeconds = g.DurationSeconds,
                    WinningTeam = g.WinningTeam,
                    GameSettingsId = g.GameSettingsId
                }));
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("match-history")]
        public async Task<ActionResult<IEnumerable<MatchHistoryDto>>> GetUserMatchHistoryAsync(MatchHistoryRequestDto dto)
        {
            if (dto == null)
                return BadRequest("DTO is null");
            if (dto.playerID <= 0)
                return BadRequest("ID cannot be negative or 0");
            if (dto.page < 0)
                return BadRequest("Page cannot be negative!");
            if (dto.pageSize <= 0)
                return BadRequest("Page size cannot be negative or 0!");

            var history = await _gamePlayerRepository.GetUserMatchHistoryAsync(dto.playerID, dto.page, dto.pageSize);

            var dtos = history.Select(gp => new MatchHistoryDto
            {
                GamePlayer = MapToDto(gp),
                Game = new GameSummaryDto
                {
                    GamesId = gp.Game.GamesId,
                    StartTime = gp.Game.StartTime,
                    EndTime = gp.Game.EndTime,
                    Status = gp.Game.Status,
                    Score = gp.Score
                }
            }).ToList();

            return Ok(dtos);
        }

        
        [HttpPut("results")]
        //IDs that do not map to any user will be skipped! 
        public async Task<IActionResult> UpdatePlayerResultsAsync(IEnumerable<PlayerStatsRequestDto> finalPlayerStats)
        {
            if (finalPlayerStats == null)
                return BadRequest("DTO list is null");
            if (finalPlayerStats.Any(ps => ps.GamePlayerId <= 0))
                return BadRequest("All IDs must be greater than 0");
            if (finalPlayerStats.Any(ps => ps.Score <0))
                return BadRequest("All scores must be positive");
            if (finalPlayerStats.Any(ps => ps.Accuracy < 0))
                return BadRequest("All accuracies must be positive");

            List<GamePlayer> gamePlayers = new();
            foreach(var dto in finalPlayerStats)
            {
                var gp = await _gamePlayerRepository.GetAsync(dto.GamePlayerId);
                if (gp != null)
                {
                    gp.Score = dto.Score;
                    gp.Outcome = dto.Outcome;
                    gp.EloChange = dto.EloChange;
                    gp.Accuracy = dto.Accuracy;

                    gamePlayers.Add(gp);
                }
            }

            await _gamePlayerRepository.UpdatePlayerResultsAsync(gamePlayers);

            return NoContent();
        }

        [HttpGet("score/{playerID}")]
        public async Task<ActionResult<int>> GetTotalScoreForUserAsync(int playerID)
        {
            if(playerID <=0)
                return BadRequest("ID cannot be negative or 0");

            var score = await _gamePlayerRepository.GetTotalScoreForUserAsync(playerID);

            return Ok(score);
        }
        private static GamePlayerDto MapToDto(GamePlayer gp)
        {
            return new GamePlayerDto
            {
                PlayerId = gp.PlayerId,
                GameId = gp.GameId,
                TeamColor = gp.TeamColor,
                Score = gp.Score
            };
        }
        private async Task<bool> GamePlayerExists(int id)
        {
            return await _gamePlayerRepository.GetAsync(id) != null;
        }
    }
}
