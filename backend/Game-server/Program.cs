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
builder.Services.AddSignalR();

// ---- Auth ----
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = false,
            ValidateAudience         = false
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

// ---- RabbitMQ ----
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config  = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMq:Host"]     ?? "localhost",
        Port     = int.Parse(config["RabbitMq:Port"] ?? "5672"),
        UserName = config["RabbitMq:User"]     ?? "guest",
        Password = config["RabbitMq:Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<GameCreatedConsumer>();

// ---- Game ----
builder.Services.AddSingleton<IGameStateManager, GameStateManager>();
builder.Services.AddSingleton<IGameEngine, MinesweeperGameEngine>();
builder.Services.AddSingleton<GameResultProcessor>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>("/hubs/game");

app.Run();
