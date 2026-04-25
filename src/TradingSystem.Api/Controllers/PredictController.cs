using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("predict")]
[Produces("application/json")]
public class PredictController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ILogger<PredictController> _logger;

    public PredictController(IAIService aiService, ILogger<PredictController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Get AI-based price prediction for a symbol
    /// </summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<PredictionResult>), 200)]
    public async Task<IActionResult> Predict(string symbol)
    {
        _logger.LogInformation("GET /predict/{Symbol}", symbol);
        var result = await _aiService.PredictAsync(symbol);
        return Ok(ApiResponse<PredictionResult>.Ok(result));
    }
}
