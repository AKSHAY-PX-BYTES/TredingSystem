using System.Text.Json;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface ICurrencyService
{
    List<CurrencyInfo> GetSupportedCurrencies();
    decimal Convert(decimal amountInUsd, string targetCurrency);
    CurrencyInfo? GetCurrency(string code);
    Task RefreshRatesAsync();
}

public class CurrencyService : ICurrencyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CurrencyService> _logger;
    private static DateTime _lastFetched = DateTime.MinValue;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

    // Hardcoded fallback rates — used only if live fetch fails
    private static readonly List<CurrencyInfo> _currencies = new()
    {
        new() { Code = "USD", Symbol = "$",  Name = "US Dollar",            RateFromUsd = 1.0m },
        new() { Code = "INR", Symbol = "₹",  Name = "Indian Rupee",         RateFromUsd = 83.50m },
        new() { Code = "EUR", Symbol = "€",  Name = "Euro",                 RateFromUsd = 0.92m },
        new() { Code = "GBP", Symbol = "£",  Name = "British Pound",        RateFromUsd = 0.79m },
        new() { Code = "JPY", Symbol = "¥",  Name = "Japanese Yen",         RateFromUsd = 154.50m },
        new() { Code = "AUD", Symbol = "A$", Name = "Australian Dollar",    RateFromUsd = 1.53m },
        new() { Code = "CAD", Symbol = "C$", Name = "Canadian Dollar",      RateFromUsd = 1.36m },
        new() { Code = "CHF", Symbol = "Fr", Name = "Swiss Franc",          RateFromUsd = 0.88m },
        new() { Code = "CNY", Symbol = "¥",  Name = "Chinese Yuan",         RateFromUsd = 7.24m },
        new() { Code = "SGD", Symbol = "S$", Name = "Singapore Dollar",     RateFromUsd = 1.34m },
    };

    public CurrencyService(IHttpClientFactory httpClientFactory, ILogger<CurrencyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public List<CurrencyInfo> GetSupportedCurrencies() => _currencies;

    public decimal Convert(decimal amountInUsd, string targetCurrency)
    {
        var currency = _currencies.FirstOrDefault(c =>
            c.Code.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase));

        if (currency == null) return amountInUsd;
        return Math.Round(amountInUsd * currency.RateFromUsd, 2);
    }

    public CurrencyInfo? GetCurrency(string code)
    {
        return _currencies.FirstOrDefault(c =>
            c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Fetches live exchange rates from a free API and updates the in-memory rates.
    /// Called periodically by a background service or on-demand.
    /// </summary>
    public async Task RefreshRatesAsync()
    {
        if ((DateTime.UtcNow - _lastFetched) < _cacheDuration)
            return;

        try
        {
            var codes = string.Join(",", _currencies.Where(c => c.Code != "USD").Select(c => c.Code));
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Free API — no key needed, 1500 requests/month
            var url = $"https://api.exchangerate.host/latest?base=USD&symbols={codes}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Try fallback free API
                url = $"https://open.er-api.com/v6/latest/USD";
                response = await client.GetAsync(url);
            }

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Both APIs put rates under a "rates" property
                if (root.TryGetProperty("rates", out var rates))
                {
                    foreach (var currency in _currencies)
                    {
                        if (currency.Code == "USD") continue;
                        if (rates.TryGetProperty(currency.Code, out var rateVal) &&
                            rateVal.ValueKind == JsonValueKind.Number)
                        {
                            currency.RateFromUsd = rateVal.GetDecimal();
                        }
                    }
                    _lastFetched = DateTime.UtcNow;
                    _logger.LogInformation("Live currency rates updated. INR={InrRate}", 
                        _currencies.First(c => c.Code == "INR").RateFromUsd);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live currency rates, using cached/fallback values");
        }
    }
}
