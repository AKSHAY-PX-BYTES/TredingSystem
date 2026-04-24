using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<UserInfo?> GetUserAsync(string username);
}

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    // In-memory user store (pre-seeded demo users)
    private static readonly List<UserRecord> _users = new()
    {
        new()
        {
            Username = "admin",
            PasswordHash = HashPassword("Admin@123"),
            DisplayName = "Admin User",
            Role = "Admin",
            Email = "admin@tradingsystem.com"
        },
        new()
        {
            Username = "trader",
            PasswordHash = HashPassword("Trader@123"),
            DisplayName = "John Trader",
            Role = "Trader",
            Email = "trader@tradingsystem.com"
        },
        new()
        {
            Username = "demo",
            PasswordHash = HashPassword("Demo@123"),
            DisplayName = "Demo User",
            Role = "Viewer",
            Email = "demo@tradingsystem.com"
        }
    };

    public AuthService(ILogger<AuthService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        var user = _users.FirstOrDefault(u =>
            u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found", request.Username);
            return Task.FromResult(new LoginResponse
            {
                Success = false,
                Error = "Invalid username or password"
            });
        }

        if (user.PasswordHash != HashPassword(request.Password))
        {
            _logger.LogWarning("Login failed: invalid password for '{Username}'", request.Username);
            return Task.FromResult(new LoginResponse
            {
                Success = false,
                Error = "Invalid username or password"
            });
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("Login successful for user: {Username}", request.Username);

        return Task.FromResult(new LoginResponse
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
        });
    }

    public Task<UserInfo?> GetUserAsync(string username)
    {
        var user = _users.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (user == null) return Task.FromResult<UserInfo?>(null);

        return Task.FromResult<UserInfo?>(new UserInfo
        {
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            Email = user.Email
        });
    }

    public Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for user: {Username}", request.Username);

        // Check if username already exists
        if (_users.Any(u => u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Registration failed: username '{Username}' already exists", request.Username);
            return Task.FromResult(new RegisterResponse
            {
                Success = false,
                Error = "Username already exists"
            });
        }

        // Check if email already exists
        if (_users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Registration failed: email '{Email}' already exists", request.Email);
            return Task.FromResult(new RegisterResponse
            {
                Success = false,
                Error = "Email already registered"
            });
        }

        // Create new user
        var newUser = new UserRecord
        {
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.Username, // Use username as display name initially
            Role = "Trader", // Default role for new users
            Email = request.Email
        };

        _users.Add(newUser);
        _logger.LogInformation("User registered successfully: {Username}", request.Username);

        return Task.FromResult(new RegisterResponse
        {
            Success = true,
            Message = "Registration successful! You can now login."
        });
    }

    private string GenerateJwtToken(UserRecord user)
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
            expires: DateTime.UtcNow.AddHours(8),
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
