namespace TradingSystem.Api.Data.Entities;

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Trader";
    public string Plan { get; set; } = "Free";
    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public bool IsPhoneVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime TrialEndsAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool IsTrialUsed { get; set; } = false;
    public string PreferredLanguage { get; set; } = "en";
    public string PreferredCurrency { get; set; } = "USD";
    
    // Account lockout
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndUtc { get; set; }
    
    // Refresh token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // --- NEW: Profile fields ---
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? TradingExperience { get; set; } // None, Beginner, Intermediate, Advanced, Expert

    // --- NEW: Legal consents (recorded at signup) ---
    public bool ConsentFinancialRisk { get; set; } = false;
    public bool ConsentTermsAndConditions { get; set; } = false;
    public bool ConsentPrivacyPolicy { get; set; } = false;
    public bool ConsentAiSignals { get; set; } = false;
    public DateTime? ConsentedAt { get; set; }

    // --- NEW: Security ---
    public DateTime? PasswordChangedAt { get; set; }
    public string? RecoveryEmail { get; set; }
    public bool MfaEnabled { get; set; } = false;
    public string? MfaSecret { get; set; }
    public string? SessionToken { get; set; } // For sign-out-all-devices
    public DateTime? SessionTokenIssuedAt { get; set; }

    // --- NEW: Preferences ---
    public string Theme { get; set; } = "system"; // light, dark, system
    public bool NotifyWhatsNew { get; set; } = true;
    public bool NotifyRecommendations { get; set; } = true;
    public bool NotifyEmailUpdates { get; set; } = true;

    // --- NEW: Account deletion ---
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletionReason { get; set; }
}
