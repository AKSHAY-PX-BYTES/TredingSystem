using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin/activity")]
[Produces("application/json")]
public class ActivityController : ControllerBase
{
    private readonly IActivityTrackingService _activityService;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(IActivityTrackingService activityService, ILogger<ActivityController> logger)
    {
        _activityService = activityService;
        _logger = logger;
    }

    /// <summary>
    /// Get activity stats summary for the given period
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ActivityStats>), 200)]
    public async Task<IActionResult> GetStats([FromQuery] int days = 30)
    {
        var stats = await _activityService.GetStatsAsync(days);
        return Ok(ApiResponse<ActivityStats>.Ok(stats));
    }

    /// <summary>
    /// Get activity timeline (daily breakdown) for charts
    /// </summary>
    [HttpGet("timeline")]
    [ProducesResponseType(typeof(ApiResponse<List<ActivityTimeline>>), 200)]
    public async Task<IActionResult> GetTimeline([FromQuery] int days = 30)
    {
        var timeline = await _activityService.GetTimelineAsync(days);
        return Ok(ApiResponse<List<ActivityTimeline>>.Ok(timeline));
    }

    /// <summary>
    /// Get country-wise breakdown
    /// </summary>
    [HttpGet("countries")]
    [ProducesResponseType(typeof(ApiResponse<List<CountryStats>>), 200)]
    public async Task<IActionResult> GetCountries([FromQuery] int days = 30)
    {
        var countries = await _activityService.GetCountryStatsAsync(days);
        return Ok(ApiResponse<List<CountryStats>>.Ok(countries));
    }

    /// <summary>
    /// Get device/browser breakdown
    /// </summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(ApiResponse<List<DeviceStats>>), 200)]
    public async Task<IActionResult> GetDevices([FromQuery] int days = 30)
    {
        var devices = await _activityService.GetDeviceStatsAsync(days);
        return Ok(ApiResponse<List<DeviceStats>>.Ok(devices));
    }

    /// <summary>
    /// Get recent activity log entries
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(ApiResponse<List<ActivityLogDto>>), 200)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
    {
        var activities = await _activityService.GetRecentAsync(count);
        var dtos = activities.Select(a => new ActivityLogDto
        {
            Id = a.Id,
            EventType = a.EventType,
            Username = a.Username,
            Email = a.Email,
            IpAddress = a.IpAddress,
            Country = a.Country,
            City = a.City,
            CountryCode = a.CountryCode,
            DeviceType = a.DeviceType,
            Browser = a.Browser,
            OperatingSystem = a.OperatingSystem,
            IsSuccess = a.IsSuccess,
            Details = a.Details,
            RequestPath = a.RequestPath,
            CreatedAt = a.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<ActivityLogDto>>.Ok(dtos));
    }

    /// <summary>
    /// Query activities with filters
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<ActivityLogDto>>), 200)]
    public async Task<IActionResult> Search(
        [FromQuery] string? eventType,
        [FromQuery] string? username,
        [FromQuery] string? country,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? isSuccess,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new ActivityQuery
        {
            EventType = eventType,
            Username = username,
            Country = country,
            From = from,
            To = to,
            IsSuccess = isSuccess,
            Page = page,
            PageSize = Math.Min(pageSize, 200)
        };

        var activities = await _activityService.GetActivitiesAsync(query);
        var dtos = activities.Select(a => new ActivityLogDto
        {
            Id = a.Id,
            EventType = a.EventType,
            Username = a.Username,
            Email = a.Email,
            IpAddress = a.IpAddress,
            Country = a.Country,
            City = a.City,
            CountryCode = a.CountryCode,
            DeviceType = a.DeviceType,
            Browser = a.Browser,
            OperatingSystem = a.OperatingSystem,
            IsSuccess = a.IsSuccess,
            Details = a.Details,
            RequestPath = a.RequestPath,
            CreatedAt = a.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<ActivityLogDto>>.Ok(dtos));
    }

    /// <summary>
    /// Get activity summary for a specific user
    /// </summary>
    [HttpGet("user/{username}")]
    [ProducesResponseType(typeof(ApiResponse<UserActivitySummary>), 200)]
    public async Task<IActionResult> GetUserActivity(string username, [FromQuery] int days = 30)
    {
        var summary = await _activityService.GetUserActivityAsync(username, days);
        return Ok(ApiResponse<UserActivitySummary>.Ok(summary));
    }
}

// DTO to avoid exposing raw entity fields
public class ActivityLogDto
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? CountryCode { get; set; }
    public string DeviceType { get; set; } = "Unknown";
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public bool IsSuccess { get; set; }
    public string? Details { get; set; }
    public string? RequestPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
