using System.Net.Http.Json;
using Microsoft.JSInterop;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface ICurrencyService
{
    string CurrentCurrencyCode { get; }
    string CurrentCurrencySymbol { get; }
    decimal CurrentRate { get; }
    List<CurrencyInfo> AvailableCurrencies { get; }
    event Action? OnCurrencyChanged;
    Task LoadCurrenciesAsync();
    Task SetCurrencyAsync(string currencyCode);
    string FormatPrice(decimal usdValue);
    /// <summary>
    /// Format a price that is in the given source currency, converting to the user's selected display currency.
    /// </summary>
    string FormatPrice(decimal value, string sourceCurrency);
    decimal ConvertFromUsd(decimal usdValue);
    /// <summary>
    /// Convert a value from the given source currency to the user's selected display currency.
    /// </summary>
    decimal Convert(decimal value, string sourceCurrency);
}

public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string CurrencyKey = "selectedCurrency";

    public string CurrentCurrencyCode { get; private set; } = "USD";
    public string CurrentCurrencySymbol { get; private set; } = "$";
    public decimal CurrentRate { get; private set; } = 1.0m;
    public List<CurrencyInfo> AvailableCurrencies { get; private set; } = new();

    public event Action? OnCurrencyChanged;

    public CurrencyService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task LoadCurrenciesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CurrencyListResponse>>("/currency");
            if (response?.Success == true && response.Data != null)
            {
                AvailableCurrencies = response.Data.Currencies;
            }
        }
        catch
        {
            // Fallback currencies
            AvailableCurrencies = new List<CurrencyInfo>
            {
                new() { Code = "USD", Symbol = "$", Name = "US Dollar", RateFromUsd = 1.0m },
                new() { Code = "INR", Symbol = "₹", Name = "Indian Rupee", RateFromUsd = 83.50m },
                new() { Code = "EUR", Symbol = "€", Name = "Euro", RateFromUsd = 0.92m },
                new() { Code = "GBP", Symbol = "£", Name = "British Pound", RateFromUsd = 0.79m }
            };
        }

        // Load saved preference
        try
        {
            var savedCurrency = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CurrencyKey);
            if (!string.IsNullOrEmpty(savedCurrency))
            {
                var currency = AvailableCurrencies.FirstOrDefault(c => c.Code == savedCurrency);
                if (currency != null)
                {
                    CurrentCurrencyCode = currency.Code;
                    CurrentCurrencySymbol = currency.Symbol;
                    CurrentRate = currency.RateFromUsd;
                }
            }
        }
        catch
        {
            // Default to USD
        }
    }

    public async Task SetCurrencyAsync(string currencyCode)
    {
        var currency = AvailableCurrencies.FirstOrDefault(c => c.Code == currencyCode);
        if (currency == null) return;

        CurrentCurrencyCode = currency.Code;
        CurrentCurrencySymbol = currency.Symbol;
        CurrentRate = currency.RateFromUsd;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CurrencyKey, currencyCode);
        }
        catch { }

        OnCurrencyChanged?.Invoke();
    }

    public string FormatPrice(decimal usdValue)
    {
        var converted = usdValue * CurrentRate;
        return $"{CurrentCurrencySymbol}{converted:N2}";
    }

    public string FormatPrice(decimal value, string sourceCurrency)
    {
        var converted = Convert(value, sourceCurrency);
        return $"{CurrentCurrencySymbol}{converted:N2}";
    }

    public decimal ConvertFromUsd(decimal usdValue)
    {
        return usdValue * CurrentRate;
    }

    public decimal Convert(decimal value, string sourceCurrency)
    {
        if (string.IsNullOrEmpty(sourceCurrency))
            sourceCurrency = "USD";

        // If source currency == display currency, no conversion needed
        if (sourceCurrency.Equals(CurrentCurrencyCode, StringComparison.OrdinalIgnoreCase))
            return value;

        // Step 1: Convert source → USD (divide by source rate)
        var sourceRate = GetRateForCurrency(sourceCurrency);
        var valueInUsd = value / sourceRate;

        // Step 2: Convert USD → target (multiply by target rate)
        return valueInUsd * CurrentRate;
    }

    private decimal GetRateForCurrency(string currencyCode)
    {
        var currency = AvailableCurrencies.FirstOrDefault(c =>
            c.Code.Equals(currencyCode, StringComparison.OrdinalIgnoreCase));
        return currency?.RateFromUsd ?? 1.0m;
    }
}
