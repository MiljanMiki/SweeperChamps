using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SC.Matchmaker;
using SC.Matchmaker.Core;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<TicketPool>();
builder.Services.AddSingleton<MatchmakingEngine>();
builder.Services.AddHostedService<MatchmakingWorker>();

var host = builder.Build();
host.Run();
