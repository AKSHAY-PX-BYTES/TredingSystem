using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ExchangeController : ControllerBase
{
    private readonly IMarketExchangeService _exchangeService;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(IMarketExchangeService exchangeService, ILogger<ExchangeController> logger)
    {
        _exchangeService = exchangeService;
        _logger = logger;
    }

    /// <summary>
    /// Get market overview with all indices and top stocks per exchange
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<MarketOverview>), 200)]
    public async Task<IActionResult> GetOverview()
    {
        _logger.LogInformation("GET /exchange/overview");
        var overview = await _exchangeService.GetMarketOverviewAsync();
        return Ok(ApiResponse<MarketOverview>.Ok(overview));
    }

    /// <summary>
    /// Get detailed data for a specific exchange (NSE, BSE, US, GLOBAL)
    /// </summary>
    [HttpGet("{exchange}")]
    [ProducesResponseType(typeof(ApiResponse<ExchangeData>), 200)]
    public async Task<IActionResult> GetExchangeData(string exchange)
    {
        _logger.LogInformation("GET /exchange/{Exchange}", exchange);
        var data = await _exchangeService.GetExchangeDataAsync(exchange);
        return Ok(ApiResponse<ExchangeData>.Ok(data));
    }

    /// <summary>
    /// Get list of supported exchanges
    /// </summary>
    [HttpGet("supported")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    public IActionResult GetSupported()
    {
        var exchanges = _exchangeService.GetSupportedExchanges();
        return Ok(ApiResponse<List<string>>.Ok(exchanges));
    }
}
