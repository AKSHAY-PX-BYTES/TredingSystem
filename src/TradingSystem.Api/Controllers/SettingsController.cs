using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("settings")]
[Produces("application/json")]
public class SettingsController : ControllerBase
{
    private readonly IServiceProvider _sp;
    private readonly IOtpService _otpService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IServiceProvider sp, IOtpService otpService, ILogger<SettingsController> logger)
    {
        _sp = sp;
        _otpService = otpService;
        _logger = logger;
    }

    private AppDbContext CreateDb() => _sp.GetRequiredService<AppDbContext>();
    private string? CurrentUser => User.Identity?.Name;

    // ==================== 1. PROFILE ====================

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        return Ok(ApiResponse<UserProfileResponse>.Ok(new UserProfileResponse
        {
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Country = user.Country,
            DateOfBirth = user.DateOfBirth,
            TradingExperience = user.TradingExperience,
            PhoneNumber = user.PhoneNumber,
            IsPhoneVerified = user.IsPhoneVerified,
            PreferredLanguage = user.PreferredLanguage,
            Theme = user.Theme,
            Role = user.Role,
            Plan = user.Plan,
            CreatedAt = user.CreatedAt
        }));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        if (!string.IsNullOrWhiteSpace(req.FirstName)) user.FirstName = req.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(req.LastName)) user.LastName = req.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(req.DisplayName)) user.DisplayName = req.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(req.Country)) user.Country = req.Country.Trim();
        if (req.DateOfBirth.HasValue) user.DateOfBirth = req.DateOfBirth;
        if (!string.IsNullOrWhiteSpace(req.TradingExperience)) user.TradingExperience = req.TradingExperience;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Profile updated successfully"));
    }

    [HttpPost("profile/change-email")]
    public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        // Verify password
        var authService = _sp.GetRequiredService<IAuthService>();
        var loginResult = await authService.LoginAsync(new LoginRequest { Username = user.Username, Password = req.Password });
        if (!loginResult.Success)
            return BadRequest(ApiResponse<string>.Fail("Invalid password"));

        // Check if email is already taken
        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == req.NewEmail.ToLower() && u.Id != user.Id);
        if (exists) return BadRequest(ApiResponse<string>.Fail("Email already in use"));

        // Send OTP to new email
        await _otpService.SendOtpAsync(req.NewEmail);
        return Ok(ApiResponse<string>.Ok("Verification code sent to new email"));
    }

    [HttpPost("profile/verify-email-change")]
    public async Task<IActionResult> VerifyEmailChange([FromBody] VerifyEmailChangeRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        var verified = await _otpService.VerifyOtpAsync(req.NewEmail, req.OtpCode);
        if (!verified) return BadRequest(ApiResponse<string>.Fail("Invalid or expired verification code"));

        user.Email = req.NewEmail;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Email updated successfully"));
    }

    [HttpPut("profile/language")]
    public async Task<IActionResult> ChangeLanguage([FromBody] ChangeLanguageRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        user.PreferredLanguage = req.Language;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Language updated"));
    }

    // ==================== 2. ACCOUNT & SECURITY ====================

    [HttpGet("security")]
    public async Task<IActionResult> GetSecurityInfo()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        var passwordAge = user.PasswordChangedAt.HasValue
            ? (int)(DateTime.UtcNow - user.PasswordChangedAt.Value).TotalDays
            : (int)(DateTime.UtcNow - user.CreatedAt).TotalDays;

        return Ok(ApiResponse<SecurityInfoResponse>.Ok(new SecurityInfoResponse
        {
            PasswordAgeDays = passwordAge,
            PasswordExpiringSoon = passwordAge > 75,
            PasswordExpired = passwordAge > 90,
            PasswordChangedAt = user.PasswordChangedAt,
            RecoveryEmail = user.RecoveryEmail,
            MfaEnabled = user.MfaEnabled,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        }));
    }

    [HttpPut("security/recovery-email")]
    public async Task<IActionResult> SetRecoveryEmail([FromBody] SetRecoveryEmailRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        user.RecoveryEmail = req.RecoveryEmail;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Recovery email updated"));
    }

    [HttpPost("security/toggle-mfa")]
    public async Task<IActionResult> ToggleMfa([FromBody] ToggleMfaRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        user.MfaEnabled = req.Enable;
        if (req.Enable && string.IsNullOrEmpty(user.MfaSecret))
        {
            // Generate MFA secret (simplified - in production use TOTP library)
            user.MfaSecret = Guid.NewGuid().ToString("N")[..16];
        }
        await db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { mfaEnabled = user.MfaEnabled }));
    }

    [HttpPost("security/sign-out-all")]
    public async Task<IActionResult> SignOutAllDevices()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        // Invalidate all tokens by rotating session token
        user.SessionToken = Guid.NewGuid().ToString("N");
        user.SessionTokenIssuedAt = DateTime.UtcNow;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Signed out from all devices. Please log in again."));
    }

    [HttpPost("security/delete-account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        // Verify password
        var authService = _sp.GetRequiredService<IAuthService>();
        var loginResult = await authService.LoginAsync(new LoginRequest { Username = user.Username, Password = req.Password });
        if (!loginResult.Success)
            return BadRequest(ApiResponse<string>.Fail("Invalid password"));

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletionReason = req.Reason;
        user.RefreshToken = null;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            message = "Account scheduled for deletion. Your data will be permanently removed in 30 days.",
            accountCreated = user.CreatedAt,
            warning = "All your trading data, signals, watchlists, and subscription will be lost permanently."
        }));
    }

    // ==================== 3. ACCESSIBILITY ====================

    [HttpPut("accessibility/theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] UpdateThemeRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        user.Theme = req.Theme;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok($"Theme set to {req.Theme}"));
    }

    // ==================== 4. MESSAGE PREFERENCES ====================

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationPrefs()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        return Ok(ApiResponse<object>.Ok(new
        {
            notifyWhatsNew = user.NotifyWhatsNew,
            notifyRecommendations = user.NotifyRecommendations,
            notifyEmailUpdates = user.NotifyEmailUpdates
        }));
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationPrefs([FromBody] UpdateNotificationPrefsRequest req)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        user.NotifyWhatsNew = req.NotifyWhatsNew;
        user.NotifyRecommendations = req.NotifyRecommendations;
        user.NotifyEmailUpdates = req.NotifyEmailUpdates;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("Notification preferences updated"));
    }

    // ==================== 5. PRIVACY CONTROL ====================

    [HttpGet("privacy")]
    public async Task<IActionResult> GetPrivacyInfo()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        return Ok(ApiResponse<object>.Ok(new
        {
            consentFinancialRisk = user.ConsentFinancialRisk,
            consentTermsAndConditions = user.ConsentTermsAndConditions,
            consentPrivacyPolicy = user.ConsentPrivacyPolicy,
            consentAiSignals = user.ConsentAiSignals,
            consentedAt = user.ConsentedAt,
            dataCollected = new[] { "Login activity", "Trading preferences", "Device info", "Usage analytics" },
            dataRetentionDays = 365,
            canRequestExport = true
        }));
    }

    [HttpPut("privacy/ai-signals-consent")]
    public async Task<IActionResult> UpdateAiSignalsConsent([FromBody] bool consent)
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        user.ConsentAiSignals = consent;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<string>.Ok("AI signals consent updated"));
    }

    // ==================== 6. ORDERS & INVOICES ====================

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        var payments = await db.Payments
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new OrderInvoiceResponse
            {
                Id = p.Id,
                OrderId = p.OrderId,
                Plan = p.Plan,
                Amount = p.Amount,
                Currency = p.Currency ?? "INR",
                Status = p.Status,
                IsAnnual = p.IsAnnual,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<OrderInvoiceResponse>>.Ok(payments));
    }

    // ==================== 7. SUBSCRIPTION DETAILS ====================

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscriptionDetail()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        var sub = await db.Subscriptions
            .Where(s => s.UserId == user.Id && s.IsActive)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        var trialExpired = user.IsTrialUsed && user.TrialEndsAt < DateTime.UtcNow;
        
        if (sub != null)
        {
            var daysRemaining = Math.Max(0, (int)(sub.EndDate - DateTime.UtcNow).TotalDays);
            var totalDays = (int)(sub.EndDate - sub.StartDate).TotalDays;
            var usagePercent = totalDays > 0 ? Math.Round((double)(totalDays - daysRemaining) / totalDays * 100, 1) : 0;

            return Ok(ApiResponse<SubscriptionDetailResponse>.Ok(new SubscriptionDetailResponse
            {
                Plan = sub.Plan,
                StartDate = sub.StartDate,
                EndDate = sub.EndDate,
                DaysRemaining = daysRemaining,
                UsagePercent = (decimal)usagePercent,
                IsActive = true,
                IsTrialExpired = false
            }));
        }

        // Free/Trial user
        var trialDaysLeft = Math.Max(0, (int)(user.TrialEndsAt - DateTime.UtcNow).TotalDays);
        return Ok(ApiResponse<SubscriptionDetailResponse>.Ok(new SubscriptionDetailResponse
        {
            Plan = user.Plan,
            StartDate = user.CreatedAt,
            EndDate = user.TrialEndsAt,
            DaysRemaining = trialDaysLeft,
            UsagePercent = trialDaysLeft > 0 ? Math.Round((decimal)(7 - trialDaysLeft) / 7 * 100, 1) : 100,
            IsActive = !trialExpired,
            IsTrialExpired = trialExpired
        }));
    }

    // ==================== 8. FEEDBACK & SUGGESTIONS ====================

    [HttpGet("feedback")]
    public async Task<IActionResult> GetMyFeedback()
    {
        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        var feedbacks = await db.Feedbacks
            .Where(f => f.UserId == user.Id)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackResponse
            {
                Id = f.Id,
                Type = f.Type,
                Subject = f.Subject,
                Message = f.Message,
                Status = f.Status,
                AdminResponse = f.AdminResponse,
                CreatedAt = f.CreatedAt,
                RespondedAt = f.RespondedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<FeedbackResponse>>.Ok(feedbacks));
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> CreateFeedback([FromBody] CreateFeedbackRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        using var db = CreateDb();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == CurrentUser);
        if (user == null) return Unauthorized();

        var feedback = new Data.Entities.FeedbackEntity
        {
            UserId = user.Id,
            Type = req.Type,
            Subject = req.Subject,
            Message = req.Message,
            CreatedAt = DateTime.UtcNow
        };

        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Feedback submitted successfully. Thank you!"));
    }

    // ==================== SUPPORTED LANGUAGES LIST ====================

    [HttpGet("languages")]
    public IActionResult GetLanguages()
    {
        var languages = new[]
        {
            new { code = "en", name = "English (US)", native = "English" },
            new { code = "hi", name = "Hindi", native = "हिन्दी" },
            new { code = "es", name = "Spanish", native = "Español" },
            new { code = "fr", name = "French", native = "Français" },
            new { code = "de", name = "German", native = "Deutsch" },
            new { code = "ja", name = "Japanese", native = "日本語" },
            new { code = "zh", name = "Chinese (Simplified)", native = "中文" },
            new { code = "ko", name = "Korean", native = "한국어" },
            new { code = "pt", name = "Portuguese", native = "Português" },
            new { code = "ar", name = "Arabic", native = "العربية" },
            new { code = "ta", name = "Tamil", native = "தமிழ்" },
            new { code = "te", name = "Telugu", native = "తెలుగు" },
            new { code = "mr", name = "Marathi", native = "मराठी" },
            new { code = "bn", name = "Bengali", native = "বাংলা" },
        };
        return Ok(ApiResponse<object>.Ok(languages));
    }
}
