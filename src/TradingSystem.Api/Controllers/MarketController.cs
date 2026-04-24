using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("market")]
[Produces("application/json")]
public class MarketController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IExportService _exportService;
    private readonly ILogger<MarketController> _logger;

    public MarketController(
        IMarketDataService marketDataService,
        IExportService exportService,
        ILogger<MarketController> logger)
    {
        _marketDataService = marketDataService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Get stock quote for a symbol
    /// </summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<StockQuote>), 200)]
    public async Task<IActionResult> GetQuote(string symbol)
    {
        _logger.LogInformation("GET /market/{Symbol}", symbol);
        var quote = await _marketDataService.GetQuoteAsync(symbol);
        return Ok(ApiResponse<StockQuote>.Ok(quote));
    }

    /// <summary>
    /// Get technical indicators for a symbol
    /// </summary>
    [HttpGet("{symbol}/indicators")]
    [ProducesResponseType(typeof(ApiResponse<TechnicalIndicators>), 200)]
    public async Task<IActionResult> GetIndicators(string symbol)
    {
        _logger.LogInformation("GET /market/{Symbol}/indicators", symbol);
        var indicators = await _marketDataService.GetIndicatorsAsync(symbol);
        return Ok(ApiResponse<TechnicalIndicators>.Ok(indicators));
    }

    /// <summary>
    /// Get historical price data for a symbol
    /// </summary>
    [HttpGet("{symbol}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<StockData>>), 200)]
    public async Task<IActionResult> GetHistory(string symbol, [FromQuery] int days = 30)
    {
        _logger.LogInformation("GET /market/{Symbol}/history?days={Days}", symbol, days);
        var data = await _marketDataService.GetHistoricalDataAsync(symbol, days);
        return Ok(ApiResponse<List<StockData>>.Ok(data));
    }

    /// <summary>
    /// Export market data to CSV
    /// </summary>
    [HttpGet("{symbol}/export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export(string symbol, [FromQuery] int days = 30)
    {
        var csvBytes = await _exportService.ExportMarketDataToCsvAsync(symbol, days);
        return File(csvBytes, "text/csv", $"{symbol}_market_data.csv");
    }
}
