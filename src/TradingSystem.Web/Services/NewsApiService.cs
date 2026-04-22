using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface INewsApiService
{
    Task<NewsAnalysisResponse?> AnalyzeAsync(NewsAnalysisRequest request);
    Task<List<NewsItem>?> GetNewsAsync(string symbol);
}

public class NewsApiService : INewsApiService
{
    private readonly HttpClient _http;

    public NewsApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<NewsAnalysisResponse?> AnalyzeAsync(NewsAnalysisRequest request)
    {
        var response = await _http.PostAsJsonAsync("news/analyze", request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<NewsAnalysisResponse>>();
        return result?.Data;
    }

    public async Task<List<NewsItem>?> GetNewsAsync(string symbol)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<List<NewsItem>>>($"news/{symbol}");
        return response?.Data;
    }
}
