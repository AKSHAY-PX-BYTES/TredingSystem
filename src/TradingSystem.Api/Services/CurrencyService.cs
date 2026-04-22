using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface ICurrencyService
{
    List<CurrencyInfo> GetSupportedCurrencies();
    decimal Convert(decimal amountInUsd, string targetCurrency);
    CurrencyInfo? GetCurrency(string code);
}

public class CurrencyService : ICurrencyService
{
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
}
