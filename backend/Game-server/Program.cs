using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using SC_GameServer.GameEngine;
using SC_GameServer.Hubs;
using SC_GameServer.Messaging;
using SC_GameServer.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:3000",
                "https://localhost:3000"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ── SignalR ──
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
    });

// ── Auth ──
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

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

// ── RabbitMQ ──
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

// ── Game ──
builder.Services.AddSingleton<IGameStateManager, GameStateManager>();
builder.Services.AddSingleton<IGameEngine, MinesweeperGameEngine>();
builder.Services.AddSingleton<GameResultProcessor>();

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("/hubs/game");

// ── Internal endpoint used by the API to resolve a user's active game ──
app.MapGet("/internal/active-game/{playerId:int}", (int playerId, IGameStateManager mgr) =>
{
    if (mgr.TryGetGameForPlayer(playerId, out var game) && game is not null)
    {
        return Results.Ok(new
        {
            hasActiveGame = true,
            gameId = game.GameId
        });
    }
    return Results.Ok(new { hasActiveGame = false, gameId = (int?)null });
});
app.MapGet("/internal/all-active-games", (IGameStateManager mgr) =>
{
    var games = mgr.GetAllGames()
        .Select(g => new
        {
            gameId = g.GameId,
            playerCount = g.Players.Count,
            players = g.Players.Select(p => new
            {
                playerId = p.PlayerId,
                teamColor = p.TeamColor.ToString()
            })
        })
        .ToList();

    return Results.Ok(games);
});

app.Run();