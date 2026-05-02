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
using TradingSystem.Api.Services.EmailProviders;

// Fix Npgsql timestamp handling - allow DateTime without explicit Kind
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
var dbConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = !string.IsNullOrWhiteSpace(dbConnStr) ? dbConnStr
    : Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Database connection string not found. Set ConnectionStrings:DefaultConnection or DATABASE_URL env var.");

// Neon.tech provides postgres:// URLs, Npgsql needs Host= format
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
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
builder.Services.AddScoped<IFeatureFlagService, FeatureFlagService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IActivityTrackingService, ActivityTrackingService>();
builder.Services.AddHttpContextAccessor();

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
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        logger.LogInformation("Ensuring database is created...");
        var created = db.Database.EnsureCreated();
        logger.LogInformation("Database EnsureCreated result: {Created}", created);
        
        // Verify tables exist by running raw checks
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS users (id SERIAL PRIMARY KEY, username VARCHAR(50) NOT NULL UNIQUE, email VARCHAR(100) NOT NULL UNIQUE, password_hash TEXT NOT NULL, display_name VARCHAR(100) NOT NULL, role VARCHAR(20) NOT NULL, created_at TIMESTAMP, last_login_at TIMESTAMP)");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS feature_flags (id SERIAL PRIMARY KEY, feature_key VARCHAR(50) NOT NULL UNIQUE, display_name VARCHAR(100) NOT NULL, description VARCHAR(500), is_enabled BOOLEAN DEFAULT true, updated_at TIMESTAMP, updated_by VARCHAR(50))");
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS otps (id SERIAL PRIMARY KEY, email VARCHAR(100) NOT NULL, code VARCHAR(6) NOT NULL, created_at TIMESTAMP NOT NULL, expires_at TIMESTAMP NOT NULL, is_verified BOOLEAN NOT NULL DEFAULT false, UNIQUE(email, code))");
        db.Database.ExecuteSqlRaw(
            "CREATE INDEX IF NOT EXISTS ix_otps_email ON otps(email)");
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS activity_logs (
                id BIGSERIAL PRIMARY KEY,
                event_type VARCHAR(50) NOT NULL,
                username VARCHAR(50),
                email VARCHAR(100),
                ip_address VARCHAR(45) NOT NULL,
                user_agent VARCHAR(500),
                country VARCHAR(100),
                city VARCHAR(100),
                region VARCHAR(100),
                country_code VARCHAR(5),
                latitude DOUBLE PRECISION,
                longitude DOUBLE PRECISION,
                isp VARCHAR(200),
                timezone VARCHAR(50),
                device_type VARCHAR(20),
                browser VARCHAR(50),
                os VARCHAR(50),
                is_success BOOLEAN DEFAULT true,
                details VARCHAR(500),
                http_method VARCHAR(10),
                request_path VARCHAR(200),
                session_id VARCHAR(100),
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_activity_logs_event_type ON activity_logs(event_type)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_activity_logs_username ON activity_logs(username)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_activity_logs_created_at ON activity_logs(created_at)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_activity_logs_ip ON activity_logs(ip_address)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_activity_logs_country ON activity_logs(country_code)");
        logger.LogInformation("Table check done.");

        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        await authService.SeedDefaultUsersAsync();

        var featureFlagService = scope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
        await featureFlagService.SeedDefaultFlagsAsync();
        logger.LogInformation("Database seeding complete.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database initialization");
        throw;
    }
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
