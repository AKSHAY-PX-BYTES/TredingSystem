using System.Net.Http.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface ISubscriptionApiService
{
    Task<SubscriptionInfo?> GetStatusAsync();
    Task<List<PlanTier>> GetPlansAsync();
    Task<UpgradeResponse?> UpgradeAsync(UpgradeRequest request);
    Task<UpgradeResponse?> CancelAsync();
    Task<bool> CheckFeatureAccessAsync(string feature);
}

public class SubscriptionApiService : ISubscriptionApiService
{
    private readonly HttpClient _httpClient;

    public SubscriptionApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<SubscriptionInfo?> GetStatusAsync()
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<ApiResponse<SubscriptionInfo>>("/subscription/status");
            return resp?.Data;
        }
        catch { return null; }
    }

    public async Task<List<PlanTier>> GetPlansAsync()
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<ApiResponse<List<PlanTier>>>("/subscription/plans");
            return resp?.Data ?? new();
        }
        catch { return new(); }
    }

    public async Task<UpgradeResponse?> UpgradeAsync(UpgradeRequest request)
    {
        try
        {
            var resp = await _httpClient.PostAsJsonAsync("/subscription/upgrade", request);
            return await resp.Content.ReadFromJsonAsync<UpgradeResponse>();
        }
        catch { return null; }
    }

    public async Task<UpgradeResponse?> CancelAsync()
    {
        try
        {
            var resp = await _httpClient.PostAsync("/subscription/cancel", null);
            return await resp.Content.ReadFromJsonAsync<UpgradeResponse>();
        }
        catch { return null; }
    }

    public async Task<bool> CheckFeatureAccessAsync(string feature)
    {
        try
        {
            var resp = await _httpClient.GetFromJsonAsync<dynamic>($"/subscription/check-feature/{feature}");
            return true;
        }
        catch { return false; }
    }
}
