using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SC.Api.Services.Implementations;
using SC.Api.Services.Interfaces;
using SC.Matchmaker;
using SC.Matchmaker.Core;
using SC.Matchmaker.Services.Interfaces;
using SC.Matchmaker.Strategies.Implementations;
using SC.Matchmaker.Strategies.Interfaces;
using SC_Backend.DataContext;
using SC_Backend.Matchmaker.Services;
using SC_Backend.Repositories.AsyncImplementations;
using SC.Domain.Repositories.AsyncInterfaces;

var builder = Host.CreateApplicationBuilder(args);

var queueType = builder.Configuration["WorkerConfig:QueueType"] ?? "casual";
var requiredPlayers = Int32.Parse(builder.Configuration["WorkerConfig:RequiredPlayers"] ?? "2");

if (queueType.Equals("ranked", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IMatchmakingStrategy>(sp =>
        new EloMatchmakingStrategy(requiredPlayers, maxEloDifference: 100));
}
else
{
    builder.Services.AddSingleton<IMatchmakingStrategy>(sp =>
        new OldestTicketStrategy(requiredPlayers));
}

builder.Services.AddSingleton<IMatchmakingStrategyFactory, MatchmakingStrategyFactory>();
builder.Services.AddSingleton<IMatchFoundPublisher, RabbitMqResultsPublisher>();

// ── Database + repositories (SAME DB as SC.Api) ──
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SweeperChamps")));
builder.Services.AddScoped<DbContext>(sp =>
    sp.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameSettingRepository, GameSettingRepository>();
builder.Services.AddScoped<IGamePlayerRepository, GamePlayerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();
builder.Services.AddScoped<IMovesRepository, MovesRepository>();

// ── GameService — what MatchmakingEngine needs ──
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGamePlayerService, GamePlayerService>();
builder.Services.AddScoped<IGameSettingsService, GameSettingsService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();

// ── Matchmaker core ──
builder.Services.AddSingleton<TicketPool>();
builder.Services.AddSingleton<MatchmakingEngine>();
builder.Services.AddHostedService<MatchmakingWorker>();

var host = builder.Build();
host.Run();