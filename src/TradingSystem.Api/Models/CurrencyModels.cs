namespace TradingSystem.Api.Models;

public class CurrencyInfo
{
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal RateFromUsd { get; set; } = 1m;
}

public class CurrencyListResponse
{
    public List<CurrencyInfo> Currencies { get; set; } = new();
}
