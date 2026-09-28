using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using SC.Api.Hubs;
using SC.Api.Services;
using SC.Api.Services.Implementations;
using SC.Api.Services.Interfaces;
using SC.Domain.Repositories.AsyncInterfaces;
using SC_Backend.DataContext;
using SC_Backend.Repositories.AsyncImplementations;
using SC_Backend.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SweeperChamps")));
builder.Services.AddScoped<DbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new ArgumentNullException("JwtSettings:SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;

            var queryToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(queryToken) &&
                (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/chatHub")))
            {
                context.Token = queryToken;
                return Task.CompletedTask;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) &&
                authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
            }

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT AUTH FAILED] Path: {context.HttpContext.Request.Path}");
            Console.WriteLine($"[JWT AUTH FAILED] Reason: {context.Exception?.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"[JWT CHALLENGE] Path: {context.HttpContext.Request.Path}");
            Console.WriteLine($"[JWT CHALLENGE] Error: {context.Error}, Description: {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

builder.Services.AddSignalR();

// ── RabbitMQ shared connection ──
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMQ:HostName"] ?? "localhost",
        Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
        UserName = config["RabbitMQ:UserName"] ?? "guest",
        Password = config["RabbitMQ:Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// ── Publishers ──
builder.Services.AddSingleton<IMatchmakingPublisher, RabbitMqMatchmakingPublisher>();
builder.Services.AddSingleton<IGameCreatedPublisher, GameCreatedPublisher>();

// ── Consumers ──
builder.Services.AddHostedService<MatchmakingResultsConsumer>();
builder.Services.AddHostedService<GameFinishedConsumer>();

// ── Repos ──
builder.Services.AddScoped<IGamePlayerRepository, GamePlayerRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameSettingRepository, GameSettingRepository>();
builder.Services.AddScoped<IMovesRepository, MovesRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();

// ── Services ──
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGamePlayerService, GamePlayerService>();
builder.Services.AddScoped<IGameSettingsService, GameSettingsService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();
builder.Services.AddScoped<IGameCompletionOrchestrator, GameCompletionOrchestrator>();

builder.Services.AddLogging();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<IGameServerClient, GameServerClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7230"); // tvoj GameServer URL
    client.Timeout = TimeSpan.FromSeconds(3);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Za self-signed dev cert
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebTemplate API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MatchmakingHub>("/hubs/matchmaking");

app.Run();