using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IFnoApiService
{
    Task<List<FnoIndex>?> GetIndicesAsync();
    Task<OptionsChain?> GetOptionsChainAsync(string symbol, string? expiry = null);
    Task<FnoAnalysis?> GetAnalysisAsync(string symbol);
    Task<FnoChartData?> GetChartDataAsync(string symbol, string optionType, decimal strike, string expiry);
    Task<List<FnoSignal>?> GetSignalsAsync(string symbol);
}

public class FnoApiService : IFnoApiService
{
    private readonly HttpClient _http;

    public FnoApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<FnoIndex>?> GetIndicesAsync()
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<List<FnoIndex>>>("fno/indices");
        return response?.Data;
    }

    public async Task<OptionsChain?> GetOptionsChainAsync(string symbol, string? expiry = null)
    {
        var url = $"fno/options-chain/{symbol}";
        if (!string.IsNullOrEmpty(expiry)) url += $"?expiry={expiry}";
        var response = await _http.GetFromJsonAsync<ApiResponse<OptionsChain>>(url);
        return response?.Data;
    }

    public async Task<FnoAnalysis?> GetAnalysisAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<FnoAnalysis>>($"fno/analysis/{symbol}");
        return response?.Data;
    }

    public async Task<FnoChartData?> GetChartDataAsync(string symbol, string optionType, decimal strike, string expiry)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<FnoChartData>>(
            $"fno/chart/{symbol}?type={optionType}&strike={strike}&expiry={expiry}");
        return response?.Data;
    }

    public async Task<List<FnoSignal>?> GetSignalsAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<List<FnoSignal>>>($"fno/signals/{symbol}");
        return response?.Data;
    }
}
