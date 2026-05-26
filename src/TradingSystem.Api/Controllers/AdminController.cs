using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IActivityTrackingService _activityTracker;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IFeatureFlagService featureFlagService, IActivityTrackingService activityTracker, IServiceProvider sp, ILogger<AdminController> logger)
    {
        _featureFlagService = featureFlagService;
        _activityTracker = activityTracker;
        _sp = sp;
        _logger = logger;
    }

    private AppDbContext CreateDb() => _sp.GetRequiredService<AppDbContext>();

    /// <summary>
    /// Get all feature flags (public — so frontend can check what's enabled)
    /// </summary>
    [HttpGet("features")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatures()
    {
        var flags = await _featureFlagService.GetAllFlagsAsync();
        var result = flags.Select(f => new FeatureFlagDto
        {
            FeatureKey = f.FeatureKey,
            DisplayName = f.DisplayName,
            Description = f.Description,
            IsEnabled = f.IsEnabled,
            UpdatedAt = f.UpdatedAt,
            UpdatedBy = f.UpdatedBy
        }).ToList();

        return Ok(new ApiResponse<List<FeatureFlagDto>> { Success = true, Data = result, Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Update a feature flag (Admin only)
    /// </summary>
    [HttpPut("features/{featureKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFeature(string featureKey, [FromBody] UpdateFeatureFlagRequest request)
    {
        var username = User.Identity?.Name ?? "unknown";
        _logger.LogInformation("Admin {User} updating feature '{Feature}' to {Enabled}", username, featureKey, request.IsEnabled);

        var updated = await _featureFlagService.UpdateFlagAsync(featureKey, request.IsEnabled, username);
        if (updated == null)
            return NotFound(new ApiResponse<string> { Success = false, Data = null, Timestamp = DateTime.UtcNow });

        await _activityTracker.TrackAsync(new ActivityEvent
        {
            EventType = "FeatureToggle",
            Username = username,
            IsSuccess = true,
            Details = $"{featureKey} → {(request.IsEnabled ? "Enabled" : "Disabled")}"
        });

        return Ok(new ApiResponse<FeatureFlagDto>
        {
            Success = true,
            Data = new FeatureFlagDto
            {
                FeatureKey = updated.FeatureKey,
                DisplayName = updated.DisplayName,
                Description = updated.Description,
                IsEnabled = updated.IsEnabled,
                UpdatedAt = updated.UpdatedAt,
                UpdatedBy = updated.UpdatedBy
            },
            Timestamp = DateTime.UtcNow
        });
    }

    // ==================== FEEDBACK MANAGEMENT (Admin) ====================

    /// <summary>All user feedback/bugs - Admin dashboard</summary>
    [HttpGet("feedback")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllFeedback([FromQuery] string? status = null, [FromQuery] string? type = null)
    {
        using var db = CreateDb();
        var query = db.Feedbacks.Include(f => f.User).AsQueryable();
        
        if (!string.IsNullOrEmpty(status)) query = query.Where(f => f.Status == status);
        if (!string.IsNullOrEmpty(type)) query = query.Where(f => f.Type == type);

        var feedbacks = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new AdminFeedbackResponse
            {
                Id = f.Id,
                Type = f.Type,
                Subject = f.Subject,
                Message = f.Message,
                Status = f.Status,
                AdminResponse = f.AdminResponse,
                CreatedAt = f.CreatedAt,
                RespondedAt = f.RespondedAt,
                Username = f.User != null ? f.User.Username : "unknown",
                Email = f.User != null ? f.User.Email : ""
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AdminFeedbackResponse>>.Ok(feedbacks));
    }

    /// <summary>Respond to a feedback item</summary>
    [HttpPut("feedback/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RespondToFeedback(int id, [FromBody] AdminRespondFeedbackRequest req)
    {
        using var db = CreateDb();
        var feedback = await db.Feedbacks.FindAsync(id);
        if (feedback == null) return NotFound(ApiResponse<string>.Fail("Feedback not found"));

        feedback.AdminResponse = req.Response;
        feedback.Status = req.Status;
        feedback.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Response saved"));
    }

    // ==================== USER MANAGEMENT (Admin) ====================

    /// <summary>Get all users with stats - Admin dashboard</summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        using var db = CreateDb();
        var total = await db.Users.CountAsync(u => !u.IsDeleted);
        var users = await db.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id, u.Username, u.Email, u.DisplayName, u.FirstName, u.LastName,
                u.Role, u.Plan, u.Country, u.TradingExperience,
                u.CreatedAt, u.LastLoginAt, u.MfaEnabled, u.IsPhoneVerified,
                passwordAgeDays = u.PasswordChangedAt.HasValue
                    ? (int)(DateTime.UtcNow - u.PasswordChangedAt.Value).TotalDays
                    : (int)(DateTime.UtcNow - u.CreatedAt).TotalDays
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { total, page, pageSize, users }));
    }

    /// <summary>Admin dashboard stats</summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDashboardStats()
    {
        using var db = CreateDb();
        var totalUsers = await db.Users.CountAsync(u => !u.IsDeleted);
        var activeToday = await db.Users.CountAsync(u => u.LastLoginAt != null && u.LastLoginAt > DateTime.UtcNow.AddHours(-24));
        var paidUsers = await db.Users.CountAsync(u => u.Plan != "Free" && !u.IsDeleted);
        var openFeedback = await db.Feedbacks.CountAsync(f => f.Status == "Open");
        var totalFeedback = await db.Feedbacks.CountAsync();
        var recentSignups = await db.Users.CountAsync(u => u.CreatedAt > DateTime.UtcNow.AddDays(-7));
        var deletedAccounts = await db.Users.CountAsync(u => u.IsDeleted);

        return Ok(ApiResponse<object>.Ok(new
        {
            totalUsers, activeToday, paidUsers, openFeedback, totalFeedback, recentSignups, deletedAccounts
        }));
    }

    /// <summary>Send notification/email to users with email updates enabled</summary>
    [HttpPost("send-notification")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendBroadcastNotification([FromBody] AdminNotificationRequest req)
    {
        using var db = CreateDb();
        var targetUsers = await db.Users
            .Where(u => !u.IsDeleted && u.NotifyEmailUpdates)
            .ToListAsync();

        // Create notification for each eligible user
        foreach (var user in targetUsers)
        {
            db.Notifications.Add(new Data.Entities.NotificationEntity
            {
                UserId = user.Id,
                Type = req.Type ?? "Announcement",
                Title = req.Title,
                Message = req.Message,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { sent = targetUsers.Count, message = "Notification sent to eligible users" }));
    }
}

public class AdminNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; } = "Announcement";
}
