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
    Task<ChangePasswordResponse> ChangePasswordAsync(string username, ChangePasswordRequest request);
    Task<bool> UsernameExistsAsync(string username);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(string email, string resetBaseUrl);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
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
                Plan = "Enterprise",
                Email = "admin@tradingsystem.com",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "trader",
                PasswordHash = HashPassword("Trader@123"),
                DisplayName = "John Trader",
                Role = "Trader",
                Plan = "Pro",
                Email = "trader@tradingsystem.com",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "demo",
                PasswordHash = HashPassword("Demo@123"),
                DisplayName = "Demo User",
                Role = "Viewer",
                Plan = "Free",
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
                Email = user.Email,
                Plan = user.Plan,
                TrialEndsAt = user.TrialEndsAt,
                IsTrialExpired = user.Plan == "Free" && DateTime.UtcNow >= user.TrialEndsAt,
                HasAccess = user.Plan != "Free" || DateTime.UtcNow < user.TrialEndsAt
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
            Email = user.Email,
            Plan = user.Plan,
            TrialEndsAt = user.TrialEndsAt,
            IsTrialExpired = user.Plan == "Free" && DateTime.UtcNow >= user.TrialEndsAt,
            HasAccess = user.Plan != "Free" || DateTime.UtcNow < user.TrialEndsAt
        };
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        using var db = CreateDbContext();
        return await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
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

        var validPlans = new[] { "Free", "Pro", "Premium", "Enterprise" };
        var plan = validPlans.Contains(request.Plan) ? request.Plan : "Free";

        var newUser = new UserEntity
        {
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.Username,
            Role = "Trader",
            Plan = plan,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CountryCode = request.CountryCode,
            IsPhoneVerified = !string.IsNullOrEmpty(request.PhoneNumber),
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
                Email = user.Email,
                Plan = user.Plan,
                TrialEndsAt = user.TrialEndsAt,
                IsTrialExpired = user.Plan == "Free" && DateTime.UtcNow >= user.TrialEndsAt,
                HasAccess = user.Plan != "Free" || DateTime.UtcNow < user.TrialEndsAt
            }
        };
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(string username, ChangePasswordRequest request)
    {
        _logger.LogInformation("Password change attempt for user: {Username}", username);

        using var db = CreateDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
            return new ChangePasswordResponse { Success = false, Error = "User not found" };

        if (user.PasswordHash != HashPassword(request.CurrentPassword))
            return new ChangePasswordResponse { Success = false, Error = "Current password is incorrect" };

        if (request.NewPassword != request.ConfirmNewPassword)
            return new ChangePasswordResponse { Success = false, Error = "New passwords do not match" };

        user.PasswordHash = HashPassword(request.NewPassword);
        await db.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for user: {Username}", username);
        return new ChangePasswordResponse { Success = true, Message = "Password changed successfully" };
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
            new Claim("Plan", user.Plan),
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

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email, string resetBaseUrl)
    {
        using var db = CreateDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user == null)
        {
            // Don't reveal that email doesn't exist (security best practice)
            return new ForgotPasswordResponse { Success = true, Message = "If this email is registered, a password reset link has been sent." };
        }

        // Generate a secure token
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Save token to DB
        var resetToken = new Data.Entities.PasswordResetTokenEntity
        {
            Email = user.Email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        db.PasswordResetTokens.Add(resetToken);
        await db.SaveChangesAsync();

        _logger.LogInformation("Password reset token generated for: {Email}", email);

        return new ForgotPasswordResponse
        {
            Success = true,
            Message = "If this email is registered, a password reset link has been sent.",
            // Return token info for the controller to send the email
            Error = token // Reusing Error field to pass token back to controller (not shown to user)
        };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        using var db = CreateDbContext();

        var resetToken = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token && t.Email.ToLower() == request.Email.ToLower());

        if (resetToken == null)
        {
            return new ResetPasswordResponse { Success = false, Error = "Invalid or expired reset link." };
        }

        if (resetToken.IsUsed)
        {
            return new ResetPasswordResponse { Success = false, Error = "This reset link has already been used." };
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return new ResetPasswordResponse { Success = false, Error = "This reset link has expired. Please request a new one." };
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (user == null)
        {
            return new ResetPasswordResponse { Success = false, Error = "User not found." };
        }

        // Update password
        user.PasswordHash = HashPassword(request.NewPassword);
        resetToken.IsUsed = true;
        await db.SaveChangesAsync();

        _logger.LogInformation("Password reset successful for: {Email}", request.Email);

        return new ResetPasswordResponse { Success = true, Message = "Password has been reset successfully. You can now login with your new password." };
    }
}
