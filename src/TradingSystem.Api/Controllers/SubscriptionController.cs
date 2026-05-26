using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("subscription")]
[Produces("application/json")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet("plans")]
    [AllowAnonymous]
    public IActionResult GetPlans()
    {
        return Ok(ApiResponse<List<PlanTier>>.Ok(_subscriptionService.GetAvailablePlans()));
    }

    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var info = await _subscriptionService.GetSubscriptionAsync(username);
        return Ok(ApiResponse<SubscriptionInfo>.Ok(info));
    }

    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var result = await _subscriptionService.UpgradeAsync(username, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var result = await _subscriptionService.CancelAsync(username);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("check-feature/{feature}")]
    [Authorize]
    public async Task<IActionResult> CheckFeature(string feature)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var hasAccess = await _subscriptionService.HasFeatureAccessAsync(username, feature);
        return Ok(new { hasAccess, feature });
    }
}
