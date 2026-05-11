using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("ai")]
[Produces("application/json")]
public class AiController : ControllerBase
{
    private readonly IAiSignalService _aiSignalService;
    private readonly IChatbotService _chatbotService;
    private readonly ISubscriptionService _subscriptionService;

    public AiController(IAiSignalService aiSignalService, IChatbotService chatbotService, ISubscriptionService subscriptionService)
    {
        _aiSignalService = aiSignalService;
        _chatbotService = chatbotService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet("signals")]
    public async Task<IActionResult> GetSignals([FromQuery] string? symbol = null, [FromQuery] int count = 20)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        // Check if user has AI signals access (Premium+ only)
        var hasAccess = await _subscriptionService.HasFeatureAccessAsync(username, "ai_signals");
        if (!hasAccess)
            return StatusCode(403, new { error = "AI Signals require Premium or Enterprise plan.", code = "UPGRADE_REQUIRED" });

        var signals = await _aiSignalService.GetSignalsAsync(symbol, count);
        return Ok(ApiResponse<List<AiSignalDto>>.Ok(signals));
    }

    [HttpPost("signals/generate/{symbol}")]
    public async Task<IActionResult> GenerateSignal(string symbol)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var hasAccess = await _subscriptionService.HasFeatureAccessAsync(username, "ai_signals");
        if (!hasAccess)
            return StatusCode(403, new { error = "AI Signals require Premium or Enterprise plan.", code = "UPGRADE_REQUIRED" });

        var signal = await _aiSignalService.GenerateSignalAsync(symbol);
        if (signal == null) return BadRequest(new { error = "Failed to generate signal" });
        return Ok(ApiResponse<AiSignalDto>.Ok(signal));
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        var response = await _chatbotService.ProcessMessageAsync(username, request);
        return Ok(ApiResponse<ChatResponse>.Ok(response));
    }
}
