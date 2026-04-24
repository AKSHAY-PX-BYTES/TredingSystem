using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[ApiController]
[Route("currency")]
[Produces("application/json")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    /// Get all supported currencies with exchange rates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CurrencyListResponse>), 200)]
    public IActionResult GetCurrencies()
    {
        var currencies = _currencyService.GetSupportedCurrencies();
        return Ok(ApiResponse<CurrencyListResponse>.Ok(new CurrencyListResponse { Currencies = currencies }));
    }

    /// <summary>
    /// Convert an amount from USD to target currency
    /// </summary>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Convert([FromQuery] decimal amount, [FromQuery] string to = "INR")
    {
        var converted = _currencyService.Convert(amount, to);
        var currency = _currencyService.GetCurrency(to);

        return Ok(ApiResponse<object>.Ok(new
        {
            OriginalAmount = amount,
            OriginalCurrency = "USD",
            ConvertedAmount = converted,
            TargetCurrency = to.ToUpperInvariant(),
            Symbol = currency?.Symbol ?? "$",
            Rate = currency?.RateFromUsd ?? 1m
        }));
    }
}
