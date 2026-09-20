using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SC.Matchmaker;
using SC.Matchmaker.Core;
using SC.Matchmaker.Strategies.Implementations;
using SC.Matchmaker.Strategies.Interfaces;

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

builder.Services.AddSingleton<TicketPool>();
builder.Services.AddSingleton<MatchmakingEngine>();
builder.Services.AddHostedService<MatchmakingWorker>();

var host = builder.Build();
host.Run();
