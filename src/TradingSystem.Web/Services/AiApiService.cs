using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IAiApiService
{
    Task<List<AiSignalDto>> GetSignalsAsync(string? symbol = null);
    Task<AiSignalDto?> GenerateSignalAsync(string symbol);
    Task<ChatResponse?> SendChatAsync(ChatRequest request);
}

public class AiApiService : IAiApiService
{
    private readonly HttpClient _httpClient;

    public AiApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<List<AiSignalDto>> GetSignalsAsync(string? symbol = null)
    {
        try
        {
            var url = string.IsNullOrEmpty(symbol) ? "/ai/signals" : $"/ai/signals?symbol={symbol}";
            var resp = await _httpClient.GetFromJsonAsync<ApiResponse<List<AiSignalDto>>>(url);
            return resp?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<AiSignalDto?> GenerateSignalAsync(string symbol)
    {
        try
        {
            var resp = await _httpClient.PostAsync($"/ai/signals/generate/{symbol}", null);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<AiSignalDto>>();
            return result?.Data;
        }
        catch { return null; }
    }

    public async Task<ChatResponse?> SendChatAsync(ChatRequest request)
    {
        try
        {
            var resp = await _httpClient.PostAsJsonAsync("/ai/chat", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<ChatResponse>>();
            return result?.Data;
        }
        catch { return null; }
    }
}
