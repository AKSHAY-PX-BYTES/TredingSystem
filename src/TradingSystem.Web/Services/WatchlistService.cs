using System.Text.Json;
using Microsoft.JSInterop;
using TradingSystem.Web.Models;

namespace TradingSystem.Web.Services;

public interface IWatchlistService
{
    event Action? OnWatchlistChanged;
    Task<List<WatchlistItem>> GetWatchlistAsync();
    Task AddToWatchlistAsync(string symbol, string companyName);
    Task RemoveFromWatchlistAsync(string symbol);
    Task<bool> IsInWatchlistAsync(string symbol);
    Task UpdatePricesAsync(Dictionary<string, decimal> prices);
}

public class WatchlistService : IWatchlistService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private const string StorageKey = "trading_watchlist";
    private List<WatchlistItem> _watchlist = new();
    private bool _isLoaded = false;

    public event Action? OnWatchlistChanged;

    public WatchlistService(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public async Task<List<WatchlistItem>> GetWatchlistAsync()
    {
        if (!_isLoaded)
        {
            await LoadFromStorageAsync();
        }
        return _watchlist;
    }

    public async Task AddToWatchlistAsync(string symbol, string companyName)
    {
        if (!_isLoaded) await LoadFromStorageAsync();

        if (_watchlist.Any(w => w.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
            return;

        var item = new WatchlistItem
        {
            Symbol = symbol.ToUpper(),
            CompanyName = companyName,
            AddedAt = DateTime.UtcNow
        };

        _watchlist.Add(item);
        await SaveToStorageAsync();
        OnWatchlistChanged?.Invoke();
    }

    public async Task RemoveFromWatchlistAsync(string symbol)
    {
        if (!_isLoaded) await LoadFromStorageAsync();

        var item = _watchlist.FirstOrDefault(w => w.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            _watchlist.Remove(item);
            await SaveToStorageAsync();
            OnWatchlistChanged?.Invoke();
        }
    }

    public async Task<bool> IsInWatchlistAsync(string symbol)
    {
        if (!_isLoaded) await LoadFromStorageAsync();
        return _watchlist.Any(w => w.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }

    public Task UpdatePricesAsync(Dictionary<string, decimal> prices)
    {
        foreach (var item in _watchlist)
        {
            if (prices.TryGetValue(item.Symbol, out var price))
            {
                if (item.CurrentPrice > 0)
                {
                    item.PreviousPrice = item.CurrentPrice;
                }
                item.CurrentPrice = price;
                item.LastUpdated = DateTime.UtcNow;
            }
        }
        OnWatchlistChanged?.Invoke();
        return Task.CompletedTask;
    }

    private async Task LoadFromStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                _watchlist = JsonSerializer.Deserialize<List<WatchlistItem>>(json) ?? new();
            }
        }
        catch
        {
            _watchlist = new();
        }
        _isLoaded = true;
    }

    private async Task SaveToStorageAsync()
    {
        var json = JsonSerializer.Serialize(_watchlist);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
}

public class WatchlistItem
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousPrice { get; set; }
    public decimal Change => CurrentPrice - PreviousPrice;
    public decimal ChangePercent => PreviousPrice > 0 ? (Change / PreviousPrice) * 100 : 0;
    public DateTime AddedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public int? Quantity { get; set; }
    public decimal? BuyPrice { get; set; }
    public decimal ProfitLoss => Quantity.HasValue && BuyPrice.HasValue 
        ? (CurrentPrice - BuyPrice.Value) * Quantity.Value 
        : 0;
}
