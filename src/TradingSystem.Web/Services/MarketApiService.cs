using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IMarketApiService
{
    Task<StockQuote?> GetQuoteAsync(string symbol);
    Task<TechnicalIndicators?> GetIndicatorsAsync(string symbol);
    Task<List<StockData>?> GetHistoryAsync(string symbol, int days = 30);
}

public class MarketApiService : IMarketApiService
{
    private readonly HttpClient _http;

    public MarketApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<StockQuote?> GetQuoteAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<StockQuote>>($"market/{symbol}");
        return response?.Data;
    }

    public async Task<TechnicalIndicators?> GetIndicatorsAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<TechnicalIndicators>>($"market/{symbol}/indicators");
        return response?.Data;
    }

    public async Task<List<StockData>?> GetHistoryAsync(string symbol, int days = 30)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<List<StockData>>>($"market/{symbol}/history?days={days}");
        return response?.Data;
    }
}
