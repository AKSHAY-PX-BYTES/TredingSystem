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
    Task<LoginResponse> RefreshTokenAsync(string username, string refreshToken);
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
        _logger.LogInformation("Login attempt for: {Identifier}", request.Username);

        using var db = CreateDbContext();
        
        // Allow login with username OR email
        var identifier = request.Username.Trim().ToLower();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

        if (user == null)
        {
            _logger.LogWarning("Login failed: '{Identifier}' not found (checked username and email)", request.Username);
            return new LoginResponse { Success = false, Error = "Invalid username/email or password" };
        }

        // Soft-deleted account check
        if (user.IsDeleted)
        {
            return new LoginResponse { Success = false, Error = "This account has been deleted. Contact support to restore." };
        }

        // Account lockout check
        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes + 1;
            _logger.LogWarning("Login blocked: account '{Username}' is locked out for {Minutes} more minutes", request.Username, remainingMinutes);
            return new LoginResponse { Success = false, Error = $"Account locked due to too many failed attempts. Try again in {remainingMinutes} minute(s)." };
        }

        if (user.PasswordHash != HashPassword(request.Password))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("Account '{Username}' locked after {Attempts} failed attempts", request.Username, user.FailedLoginAttempts);
            }
            await db.SaveChangesAsync();

            var attemptsRemaining = 5 - user.FailedLoginAttempts;
            var errorMsg = attemptsRemaining > 0
                ? $"Invalid username or password. {attemptsRemaining} attempt(s) remaining."
                : "Account locked due to too many failed attempts. Try again in 15 minutes.";
            
            return new LoginResponse { Success = false, Error = errorMsg };
        }

        // Successful login — reset lockout
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAt = DateTime.UtcNow;

        // Generate refresh token
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        
        await db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 8);
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        _logger.LogInformation("Login successful for user: {Username}", request.Username);

        // Password expiry warning
        string? warning = null;
        var passwordAge = user.PasswordChangedAt.HasValue
            ? (int)(DateTime.UtcNow - user.PasswordChangedAt.Value).TotalDays
            : (int)(DateTime.UtcNow - user.CreatedAt).TotalDays;
        if (passwordAge > 90)
            warning = "Your password has expired (over 90 days). Please update your password immediately for security.";
        else if (passwordAge > 75)
            warning = $"Your password will expire in {90 - passwordAge} days. Please update it soon.";

        return new LoginResponse
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Warning = warning,
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

        // If paid plan, verify that payment was completed
        if (plan != "Free" && !string.IsNullOrEmpty(request.PaymentOrderId))
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.PaymentOrderId && p.Status == "paid");
            if (payment == null)
            {
                return new RegisterResponse { Success = false, Error = "Payment not verified. Please complete payment before registration." };
            }
        }
        else if (plan != "Free")
        {
            // Paid plan but no payment proof — downgrade to Free
            plan = "Free";
        }

        var newUser = new UserEntity
        {
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            DisplayName = $"{request.FirstName} {request.LastName}".Trim(),
            Role = "Trader",
            Plan = plan,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CountryCode = request.CountryCode,
            IsPhoneVerified = !string.IsNullOrEmpty(request.PhoneNumber),
            CreatedAt = DateTime.UtcNow,
            // New profile fields
            FirstName = request.FirstName,
            LastName = request.LastName,
            Country = request.Country,
            DateOfBirth = request.DateOfBirth,
            TradingExperience = request.TradingExperience,
            // Legal consents
            ConsentFinancialRisk = request.ConsentFinancialRisk,
            ConsentTermsAndConditions = request.ConsentTermsAndConditions,
            ConsentPrivacyPolicy = request.ConsentPrivacyPolicy,
            ConsentAiSignals = request.ConsentAiSignals,
            ConsentedAt = DateTime.UtcNow,
            // Security defaults
            PasswordChangedAt = DateTime.UtcNow,
            SessionToken = Guid.NewGuid().ToString("N"),
            SessionTokenIssuedAt = DateTime.UtcNow
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        // Link payment record to the new user
        if (plan != "Free" && !string.IsNullOrEmpty(request.PaymentOrderId))
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == request.PaymentOrderId);
            if (payment != null)
            {
                payment.UserId = newUser.Id;
                await db.SaveChangesAsync();
            }
        }

        _logger.LogInformation("User registered successfully: {Username}", request.Username);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful! You can now login."
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(string username, string refreshToken)
    {
        _logger.LogInformation("Token refresh for user: {Username}", username);

        using var db = CreateDbContext();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null)
        {
            return new LoginResponse { Success = false, Error = "User not found" };
        }

        // Validate refresh token
        if (user.RefreshToken != refreshToken || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            // Possible token theft — invalidate all tokens
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await db.SaveChangesAsync();
            _logger.LogWarning("Invalid refresh token for user {Username} — possible token theft", username);
            return new LoginResponse { Success = false, Error = "Invalid or expired refresh token. Please login again." };
        }

        var token = GenerateJwtToken(user);
        var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 8);
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        // Rotate refresh token
        var newRefreshToken = GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return new LoginResponse
        {
            Success = true,
            Token = token,
            RefreshToken = newRefreshToken,
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
        user.PasswordChangedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for user: {Username}", username);
        return new ChangePasswordResponse { Success = true, Message = "Password changed successfully" };
    }

    private string GenerateJwtToken(UserEntity user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT signing key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryHours = _configuration.GetValue<int>("Jwt:ExpiryHours", 8);

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
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
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
