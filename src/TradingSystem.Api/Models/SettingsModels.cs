using System.ComponentModel.DataAnnotations;

namespace TradingSystem.Api.Models;

// ===== Profile =====
public class UpdateProfileRequest
{
    [StringLength(50, MinimumLength = 1)]
    public string? FirstName { get; set; }

    [StringLength(50, MinimumLength = 1)]
    public string? LastName { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? TradingExperience { get; set; }
}

public class ChangeEmailRequest
{
    [Required, EmailAddress]
    public string NewEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty; // Verify identity
}

public class VerifyEmailChangeRequest
{
    [Required, EmailAddress]
    public string NewEmail { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string OtpCode { get; set; } = string.Empty;
}

public class ChangeLanguageRequest
{
    [Required]
    public string Language { get; set; } = "en"; // en, hi, es, fr, de, ja, zh, ko, pt, ar
}

// ===== Security =====
public class SetRecoveryEmailRequest
{
    [Required, EmailAddress]
    public string RecoveryEmail { get; set; } = string.Empty;
}

public class ToggleMfaRequest
{
    public bool Enable { get; set; }
}

public class DeleteAccountRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;

    public string? Reason { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm account deletion")]
    public bool Confirm { get; set; }
}

// ===== Preferences =====
public class UpdateThemeRequest
{
    [Required]
    [RegularExpression("^(light|dark|system)$", ErrorMessage = "Theme must be light, dark, or system")]
    public string Theme { get; set; } = "system";
}

public class UpdateNotificationPrefsRequest
{
    public bool NotifyWhatsNew { get; set; } = true;
    public bool NotifyRecommendations { get; set; } = true;
    public bool NotifyEmailUpdates { get; set; } = true;
}

// ===== Feedback =====
public class CreateFeedbackRequest
{
    [Required]
    [RegularExpression("^(Bug|Suggestion|Feedback)$")]
    public string Type { get; set; } = "Suggestion";

    [Required, StringLength(200, MinimumLength = 3)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;
}

public class AdminRespondFeedbackRequest
{
    [Required, StringLength(5000, MinimumLength = 1)]
    public string Response { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Open|InProgress|Resolved|Closed)$")]
    public string Status { get; set; } = "Resolved";
}

// ===== Response Models =====
public class UserProfileResponse
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? TradingExperience { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsPhoneVerified { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string Theme { get; set; } = "system";
    public string Role { get; set; } = "Trader";
    public string Plan { get; set; } = "Free";
    public DateTime CreatedAt { get; set; }
}

public class SecurityInfoResponse
{
    public int PasswordAgeDays { get; set; }
    public bool PasswordExpiringSoon { get; set; } // > 75 days
    public bool PasswordExpired { get; set; }      // > 90 days
    public DateTime? PasswordChangedAt { get; set; }
    public string? RecoveryEmail { get; set; }
    public bool MfaEnabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FeedbackResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminResponse { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class AdminFeedbackResponse : FeedbackResponse
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class OrderInvoiceResponse
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsAnnual { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class SubscriptionDetailResponse
{
    public string Plan { get; set; } = "Free";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public decimal UsagePercent { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrialExpired { get; set; }
}
