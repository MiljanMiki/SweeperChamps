using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using SC_GameServer.GameEngine;
using SC_GameServer.Hubs;
using SC_GameServer.Messaging;
using SC_GameServer.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---- SignalR ----
// TODO: once you run more than one GameServer instance, add a backplane:
// builder.Services.AddSignalR().AddStackExchangeRedis(redisConnectionString);
builder.Services.AddSignalR();

// ---- Auth ----
// Assumes the same JWT scheme/keys the web API already issues tokens with.
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,   // set true + configure in production
            ValidateAudience = false
        };

        // SignalR sends the token via query string (?access_token=), not the
        // Authorization header, since browsers can't set headers on WS upgrades.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/game"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ---- RabbitMQ ----
// v7.x client is fully async, so connection creation is awaited once at
// startup via GetAwaiter().GetResult() - acceptable for a one-time singleton
// init; everything after this (channels, publish, consume) is async.
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMq:Host"] ?? "localhost",
        Port = int.Parse(config["RabbitMq:Port"] ?? "5672"),
        UserName = config["RabbitMq:User"] ?? "guest",
        Password = config["RabbitMq:Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<GameCreatedConsumer>();

// ---- Game state / engine ----
builder.Services.AddSingleton<IGameStateManager, GameStateManager>();
builder.Services.AddSingleton<IGameEngine, MinesweeperGameEngine>();
builder.Services.AddSingleton<IGameResultProcessor, GameResultProcessor>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("/hubs/game");

app.Run();