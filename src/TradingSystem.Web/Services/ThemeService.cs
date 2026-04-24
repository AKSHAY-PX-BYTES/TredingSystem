using Microsoft.JSInterop;

namespace TradingSystem.Web.Services;

public interface IThemeService
{
    event Action? OnThemeChanged;
    string CurrentTheme { get; }
    bool IsDarkMode { get; }
    Task InitializeAsync();
    Task ToggleThemeAsync();
    Task SetThemeAsync(string theme);
}

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "trading_theme";
    private string _currentTheme = "dark";
    private bool _isInitialized = false;

    public event Action? OnThemeChanged;
    public string CurrentTheme => _currentTheme;
    public bool IsDarkMode => _currentTheme == "dark";

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            _currentTheme = savedTheme ?? "dark";
            await ApplyThemeAsync();
        }
        catch
        {
            _currentTheme = "dark";
        }
        _isInitialized = true;
    }

    public async Task ToggleThemeAsync()
    {
        _currentTheme = _currentTheme == "dark" ? "light" : "dark";
        await SaveAndApplyAsync();
    }

    public async Task SetThemeAsync(string theme)
    {
        if (theme != "dark" && theme != "light") return;
        _currentTheme = theme;
        await SaveAndApplyAsync();
    }

    private async Task SaveAndApplyAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, _currentTheme);
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task ApplyThemeAsync()
    {
        await _jsRuntime.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{_currentTheme}')");
    }
}
