using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IBacktestApiService
{
    Task<BacktestResult?> RunAsync(BacktestRequest request);
}

public class BacktestApiService : IBacktestApiService
{
    private readonly HttpClient _http;

    public BacktestApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<BacktestResult?> RunAsync(BacktestRequest request)
    {
        var response = await _http.PostAsJsonAsync("backtest", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BacktestResult>>();
        return result?.Data;
    }
}
