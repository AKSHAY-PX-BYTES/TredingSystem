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
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("JWT signing key not configured. Set Jwt:Key in appsettings or JWT_KEY environment variable.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TradingSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TradingSystemUI";

if (jwtKey.Length < 32)
    throw new InvalidOperationException("JWT signing key must be at least 256 bits (32 characters).");

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
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
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
builder.Services.AddScoped<IPhoneOtpService, PhoneOtpService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IActivityTrackingService, ActivityTrackingService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAiSignalService, AiSignalService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
builder.Services.AddHttpContextAccessor();

// Payment service (Razorpay)
builder.Services.AddHttpClient("Razorpay");
builder.Services.AddScoped<IPaymentService, RazorpayPaymentService>();

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
builder.Services.AddHostedService<NotificationBroadcaster>();

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
        
        // Add plan column to users table if not exists
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS plan VARCHAR(20) NOT NULL DEFAULT 'Free'");
        
        // Migrate old plan names to new ones
        db.Database.ExecuteSqlRaw("UPDATE users SET plan = 'Enterprise' WHERE plan = 'Super'");
        db.Database.ExecuteSqlRaw("UPDATE users SET plan = 'Free' WHERE plan = 'Basic'");
        
        // Add phone columns to users table if not exists
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS phone_number VARCHAR(20)");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS country_code VARCHAR(5)");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS is_phone_verified BOOLEAN DEFAULT false");
        
        // Add subscription/trial columns
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS trial_ends_at TIMESTAMP DEFAULT NOW() + INTERVAL '7 days'");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS is_trial_used BOOLEAN DEFAULT false");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS preferred_language VARCHAR(10) DEFAULT 'en'");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS preferred_currency VARCHAR(10) DEFAULT 'USD'");
        
        // Account lockout & refresh token columns
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS failed_login_attempts INTEGER DEFAULT 0");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS lockout_end_utc TIMESTAMP");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS refresh_token VARCHAR(200)");
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS refresh_token_expires_at TIMESTAMP");

        // Create subscriptions table
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS subscriptions (
                id SERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id),
                plan VARCHAR(20) NOT NULL DEFAULT 'Free',
                start_date TIMESTAMP NOT NULL DEFAULT NOW(),
                end_date TIMESTAMP,
                trial_ends_at TIMESTAMP DEFAULT NOW() + INTERVAL '7 days',
                is_trial_used BOOLEAN DEFAULT false,
                is_active BOOLEAN DEFAULT true,
                price_per_month DECIMAL(10,2) DEFAULT 0,
                payment_method VARCHAR(50),
                transaction_id VARCHAR(200),
                auto_renew BOOLEAN DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                cancelled_at TIMESTAMP
            )");

        // Create notifications table
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS notifications (
                id BIGSERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id),
                type VARCHAR(50) NOT NULL,
                title VARCHAR(200) NOT NULL,
                message VARCHAR(2000) NOT NULL,
                symbol VARCHAR(20),
                data TEXT,
                is_read BOOLEAN DEFAULT false,
                is_emailed BOOLEAN DEFAULT false,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                read_at TIMESTAMP
            )");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_notifications_user_read ON notifications(user_id, is_read)");

        // Create price_alerts table
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS price_alerts (
                id SERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id),
                symbol VARCHAR(20) NOT NULL,
                target_price DECIMAL(18,4) NOT NULL,
                threshold_percent DECIMAL(5,2) DEFAULT 5.0,
                direction VARCHAR(10) DEFAULT 'Above',
                is_triggered BOOLEAN DEFAULT false,
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                triggered_at TIMESTAMP
            )");

        // Create ai_signals table
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ai_signals (
                id BIGSERIAL PRIMARY KEY,
                symbol VARCHAR(20) NOT NULL,
                signal_type VARCHAR(20) NOT NULL,
                confidence DECIMAL(5,2),
                source VARCHAR(50) NOT NULL,
                analysis TEXT,
                metadata TEXT,
                generated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                expires_at TIMESTAMP
            )");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_ai_signals_symbol ON ai_signals(symbol)");

        // Create chat_messages table
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS chat_messages (
                id BIGSERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id),
                session_id VARCHAR(100) NOT NULL,
                role VARCHAR(20) NOT NULL,
                content TEXT NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_chat_messages_user_session ON chat_messages(user_id, session_id)");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS password_reset_tokens (
                id SERIAL PRIMARY KEY,
                email VARCHAR(100) NOT NULL,
                token VARCHAR(200) NOT NULL UNIQUE,
                expires_at TIMESTAMP NOT NULL,
                is_used BOOLEAN NOT NULL DEFAULT false,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            )");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_email ON password_reset_tokens(email)");

        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS payments (
                id SERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
                order_id VARCHAR(100) NOT NULL UNIQUE,
                payment_id VARCHAR(100),
                plan VARCHAR(20) NOT NULL,
                amount DECIMAL(10,2) NOT NULL,
                currency VARCHAR(10) DEFAULT 'INR',
                status VARCHAR(20) NOT NULL DEFAULT 'created',
                payment_method VARCHAR(50),
                signature VARCHAR(500),
                is_annual BOOLEAN DEFAULT false,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                paid_at TIMESTAMP,
                raw_response TEXT
            )");
        db.Database.ExecuteSqlRaw("ALTER TABLE payments ALTER COLUMN user_id DROP NOT NULL");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_payments_user_id ON payments(user_id)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_payments_payment_id ON payments(payment_id)");
        
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
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Only enable Swagger in Development or explicitly allowed environments
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading System API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SubscriptionAccessMiddleware>();

app.MapControllers();
app.MapHub<TradingHub>("/hubs/trading");

app.Logger.LogInformation("Trading System API started successfully");

app.Run();
