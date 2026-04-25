using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<UserInfo?> GetUserAsync(string username);
    Task<LoginResponse> RefreshTokenAsync(string username);
    Task SeedDefaultUsersAsync();
}

public class AuthService : IAuthService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(IServiceScopeFactory scopeFactory, ILogger<AuthService> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task SeedDefaultUsersAsync()
    {
        using var db = CreateDbContext();

        if (await db.Users.AnyAsync())
        {
            _logger.LogInformation("Database already has users, skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding default users into database...");

        var defaultUsers = new List<UserEntity>
        {
            new()
            {
                Username = "admin",
                PasswordHash = HashPassword("Admin@123"),
                DisplayName = "Admin User",
                Role = "Admin",
                Email = "admin@tradingsystem.com",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "trader",
                PasswordHash = HashPassword("Trader@123"),
                DisplayName = "John Trader",
                Role = "Trader",
                Email = "trader@tradingsystem.com",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "demo",
                PasswordHash = HashPassword("Demo@123"),
                DisplayName = "Demo User",
                Role = "Viewer",
                Email = "demo@tradingsystem.com",
                CreatedAt = DateTime.UtcNow
            }
        };

        db.Users.AddRange(defaultUsers);
        await db.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} default users.", defaultUsers.Count);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        using var db = CreateDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());

        if (user == null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found", request.Username);
            return new LoginResponse { Success = false, Error = "Invalid username or password" };
        }

        if (user.PasswordHash != HashPassword(request.Password))
        {
            _logger.LogWarning("Login failed: invalid password for '{Username}'", request.Username);
            return new LoginResponse { Success = false, Error = "Invalid username or password" };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        _logger.LogInformation("Login successful for user: {Username}", request.Username);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role,
                Email = user.Email
            }
        };
    }

    public async Task<UserInfo?> GetUserAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null) return null;

        return new UserInfo
        {
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Email = user.Email
        };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for user: {Username}", request.Username);

        using var db = CreateDbContext();

        if (await db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower()))
        {
            return new RegisterResponse { Success = false, Error = "Username already exists" };
        }

        if (await db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return new RegisterResponse { Success = false, Error = "Email already registered" };
        }

        var newUser = new UserEntity
        {
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.Username,
            Role = "Trader",
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Username}", request.Username);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful! You can now login."
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(string username)
    {
        _logger.LogInformation("Token refresh for user: {Username}", username);

        using var db = CreateDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            return new LoginResponse { Success = false, Error = "User not found" };
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Username = user.Username,
                DisplayName = user.DisplayName,
                Role = user.Role,
                Email = user.Email
            }
        };
    }

    private string GenerateJwtToken(UserEntity user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "TradingSystem_SuperSecret_Key_2026_!@#$%^&*()_LONG_ENOUGH_256BITS";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "TradingSystem",
            audience: _configuration["Jwt:Audience"] ?? "TradingSystemUI",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "_TradingSalt2026"));
        return Convert.ToBase64String(bytes);
    }
}
