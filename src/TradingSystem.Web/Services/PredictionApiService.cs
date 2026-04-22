using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IPredictionApiService
{
    Task<PredictionResult?> PredictAsync(string symbol);
    Task<StrategyResult?> GetStrategyAsync(string symbol);
}

public class PredictionApiService : IPredictionApiService
{
    private readonly HttpClient _http;

    public PredictionApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PredictionResult?> PredictAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<PredictionResult>>($"predict/{symbol}");
        return response?.Data;
    }

    public async Task<StrategyResult?> GetStrategyAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<StrategyResult>>($"strategy/{symbol}");
        return response?.Data;
    }
}
