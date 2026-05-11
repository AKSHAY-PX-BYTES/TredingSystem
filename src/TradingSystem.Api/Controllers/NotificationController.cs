using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("notifications")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var notifications = await _notificationService.GetNotificationsAsync(username, page, pageSize);
        return Ok(ApiResponse<List<NotificationDto>>.Ok(notifications));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(username);
        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        await _notificationService.MarkAsReadAsync(username, id);
        return Ok(new { success = true });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(username);
        return Ok(new { success = true });
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var alerts = await _notificationService.GetPriceAlertsAsync(username);
        return Ok(ApiResponse<List<PriceAlertDto>>.Ok(alerts));
    }

    [HttpPost("alerts")]
    public async Task<IActionResult> CreateAlert([FromBody] CreatePriceAlertRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var alert = await _notificationService.CreatePriceAlertAsync(username, request);
        if (alert == null) return BadRequest(new { error = "Failed to create alert" });
        return Ok(ApiResponse<PriceAlertDto>.Ok(alert));
    }

    [HttpDelete("alerts/{id}")]
    public async Task<IActionResult> DeleteAlert(int id)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var result = await _notificationService.DeletePriceAlertAsync(username, id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
