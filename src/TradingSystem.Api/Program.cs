using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TradingSystem.Api.BackgroundServices;
using TradingSystem.Api.Data;
using TradingSystem.Api.Hubs;
using TradingSystem.Api.Middleware;
using TradingSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Trading System API",
        Version = "v1",
        Description = "AI-Based Trading Strategy Predictor API with ML.NET integration"
    });

    // JWT Bearer auth in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Login via POST /auth/login to get a token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TradingSystem_SuperSecret_Key_2026_!@#$%^&*()_LONG_ENOUGH_256BITS";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TradingSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TradingSystemUI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // Allow JWT token in SignalR query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS - allow Blazor WASM frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] {
        "https://localhost:5002",
        "http://localhost:5003",
        "https://localhost:7002",
        "http://localhost:5173"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR();

// PostgreSQL Database (Neon.tech free tier — persists across redeploys)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("Database connection string not found. Set ConnectionStrings:DefaultConnection or DATABASE_URL env var.");

// Neon.tech provides postgres:// URLs, Npgsql needs postgresql:// or Host= format
if (connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register application services (DI)
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<INewsService, NewsService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IStrategyEngine, StrategyEngine>();
builder.Services.AddScoped<IBacktestService, BacktestService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ICurrencyService, CurrencyService>();

// Yahoo Finance live market data
builder.Services.AddHttpClient("YahooFinance", client =>
{
    client.BaseAddress = new Uri("https://query2.finance.yahoo.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<ILiveMarketDataService, YahooFinanceService>();
builder.Services.AddSingleton<IMarketExchangeService, MarketExchangeService>();

// Background service for real-time updates
builder.Services.AddHostedService<MarketDataBroadcaster>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Auto-migrate and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await authService.SeedDefaultUsersAsync();
}

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Always enable Swagger for demo/free-tier
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading System API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TradingHub>("/hubs/trading");

// Log startup info
app.Logger.LogInformation("Trading System API started on {Urls}", string.Join(", ", app.Urls));
app.Logger.LogInformation("Swagger UI available at /swagger");
app.Logger.LogInformation("SignalR hub available at /hubs/trading");

app.Run();
