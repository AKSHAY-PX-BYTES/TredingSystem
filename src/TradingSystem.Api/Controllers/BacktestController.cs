using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("backtest")]
[Produces("application/json")]
public class BacktestController : ControllerBase
{
    private readonly IBacktestService _backtestService;
    private readonly IExportService _exportService;
    private readonly ILogger<BacktestController> _logger;

    public BacktestController(
        IBacktestService backtestService,
        IExportService exportService,
        ILogger<BacktestController> logger)
    {
        _backtestService = backtestService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Run a backtest simulation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BacktestResult>), 200)]
    public async Task<IActionResult> RunBacktest([FromBody] BacktestRequest request)
    {
        _logger.LogInformation("POST /backtest for {Symbol}, {Days} days, strategy: {Strategy}",
            request.Symbol, request.Days, request.Strategy);

        if (string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest(ApiResponse<BacktestResult>.Fail("Symbol is required"));

        if (request.Days < 10 || request.Days > 365)
            return BadRequest(ApiResponse<BacktestResult>.Fail("Days must be between 10 and 365"));

        var result = await _backtestService.RunAsync(request);
        return Ok(ApiResponse<BacktestResult>.Ok(result));
    }

    /// <summary>
    /// Export backtest results to CSV
    /// </summary>
    [HttpPost("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportBacktest([FromBody] BacktestRequest request)
    {
        var result = await _backtestService.RunAsync(request);
        var csvBytes = await _exportService.ExportBacktestToCsvAsync(result);
        return File(csvBytes, "text/csv", $"{request.Symbol}_backtest_results.csv");
    }
}
