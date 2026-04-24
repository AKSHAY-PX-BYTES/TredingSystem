using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[ApiController]
[Route("strategy")]
[Produces("application/json")]
public class StrategyController : ControllerBase
{
    private readonly IStrategyEngine _strategyEngine;
    private readonly ILogger<StrategyController> _logger;

    public StrategyController(IStrategyEngine strategyEngine, ILogger<StrategyController> logger)
    {
        _strategyEngine = strategyEngine;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive strategy evaluation for a symbol
    /// </summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<StrategyResult>), 200)]
    public async Task<IActionResult> GetStrategy(string symbol)
    {
        _logger.LogInformation("GET /strategy/{Symbol}", symbol);
        var result = await _strategyEngine.EvaluateAsync(symbol);
        return Ok(ApiResponse<StrategyResult>.Ok(result));
    }
}
