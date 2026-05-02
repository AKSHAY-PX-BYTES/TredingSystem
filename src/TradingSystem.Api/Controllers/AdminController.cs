using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<AdminController> _logger;

    public AdminController(IFeatureFlagService featureFlagService, IActivityTrackingService activityTracker, ILogger<AdminController> logger)
    {
        _featureFlagService = featureFlagService;
        _activityTracker = activityTracker;
        _logger = logger;
    }

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
}
