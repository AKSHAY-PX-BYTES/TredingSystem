using Microsoft.JSInterop;
using System.Text.Json;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IPlanAccessService
{
    Task<string> GetCurrentPlanAsync();
    Task<bool> IsFreeUserAsync();
    Task<bool> CanUseFeatureAsync(string feature);
}

public class PlanAccessService : IPlanAccessService
{
    private readonly IJSRuntime _js;
    private string? _cachedPlan;

    // Define which features each plan can access
    private static readonly Dictionary<string, HashSet<string>> PlanFeatures = new()
    {
        ["Free"] = new() { "view_default_stocks", "basic_charts", "news" },
        ["Pro"] = new() { "view_default_stocks", "basic_charts", "news", "analyze_any_stock", "live_quotes", "watchlist_unlimited", "advanced_charts", "alerts", "markets_analyze" },
        ["Premium"] = new() { "view_default_stocks", "basic_charts", "news", "analyze_any_stock", "live_quotes", "watchlist_unlimited", "advanced_charts", "alerts", "markets_analyze", "ai_signals", "backtest", "ai_chat", "api_access" },
        ["Enterprise"] = new() { "view_default_stocks", "basic_charts", "news", "analyze_any_stock", "live_quotes", "watchlist_unlimited", "advanced_charts", "alerts", "markets_analyze", "ai_signals", "backtest", "ai_chat", "api_access", "white_label", "dedicated_api" }
    };

    public PlanAccessService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string> GetCurrentPlanAsync()
    {
        if (_cachedPlan != null) return _cachedPlan;

        try
        {
            var userJson = await _js.InvokeAsync<string?>("localStorage.getItem", "authUser");
            if (!string.IsNullOrEmpty(userJson))
            {
                var user = JsonSerializer.Deserialize<UserInfo>(userJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _cachedPlan = user?.Plan ?? "Free";
                return _cachedPlan;
            }
        }
        catch { }

        return "Free";
    }

    public async Task<bool> IsFreeUserAsync()
    {
        var plan = await GetCurrentPlanAsync();
        return plan == "Free";
    }

    public async Task<bool> CanUseFeatureAsync(string feature)
    {
        var plan = await GetCurrentPlanAsync();
        if (PlanFeatures.TryGetValue(plan, out var features))
            return features.Contains(feature);
        return false;
    }
}
