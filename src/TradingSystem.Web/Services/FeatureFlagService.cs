using System.Net.Http.Json;

namespace TradingSystem.Web.Services;

public interface IFeatureFlagService
{
    event Action? OnFlagsChanged;
    Task LoadFlagsAsync();
    bool IsEnabled(string featureKey);
    List<FeatureFlagItem> GetAllFlags();
    Task<bool> UpdateFlagAsync(string featureKey, bool isEnabled);
}

public class FeatureFlagItem
{
    public string FeatureKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly HttpClient _httpClient;
    private List<FeatureFlagItem> _flags = new();
    private bool _loaded = false;

    public event Action? OnFlagsChanged;

    public FeatureFlagService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoadFlagsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponseWrapper<List<FeatureFlagItem>>>("/admin/features");
            if (response?.Success == true && response.Data != null)
            {
                _flags = response.Data;
                _loaded = true;
                OnFlagsChanged?.Invoke();
            }
        }
        catch
        {
            // If API is down, default all to enabled
            _loaded = false;
        }
    }

    public bool IsEnabled(string featureKey)
    {
        if (!_loaded) return true; // Default to enabled if not loaded
        var flag = _flags.FirstOrDefault(f => f.FeatureKey == featureKey);
        return flag?.IsEnabled ?? true;
    }

    public List<FeatureFlagItem> GetAllFlags() => _flags;

    public async Task<bool> UpdateFlagAsync(string featureKey, bool isEnabled)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/admin/features/{featureKey}", new { IsEnabled = isEnabled });
            if (response.IsSuccessStatusCode)
            {
                // Update local cache
                var flag = _flags.FirstOrDefault(f => f.FeatureKey == featureKey);
                if (flag != null)
                {
                    flag.IsEnabled = isEnabled;
                    OnFlagsChanged?.Invoke();
                }
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // Simple wrapper to deserialize API responses
    private class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
