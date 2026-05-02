using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;

namespace TradingSystem.Api.Services;

public interface IFeatureFlagService
{
    Task<List<FeatureFlagEntity>> GetAllFlagsAsync();
    Task<bool> IsFeatureEnabledAsync(string featureKey);
    Task<FeatureFlagEntity?> UpdateFlagAsync(string featureKey, bool isEnabled, string updatedBy);
    Task SeedDefaultFlagsAsync();
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeatureFlagService> _logger;

    // Default features that can be toggled by admin
    private static readonly List<(string Key, string Name, string Desc)> DefaultFeatures = new()
    {
        ("login", "Login", "Allow users to login to the system"),
        ("signup", "Sign Up", "Allow new user registration"),
        ("explore", "Explore Stocks", "Disable custom stock search/analyze — default chips still work"),
        ("markets", "Markets", "Access to Markets tab with live stock data"),
        ("markets_click", "Markets Clickable", "Allow clicking on stocks in Markets tab for details"),
        ("watchlist", "Watchlist", "Access to Watchlist / Portfolio tracker"),
        ("charts", "Charts", "Access to Advanced Charts with technical indicators"),
        ("compare", "Compare", "Access to Stock Comparison tool"),
        ("news", "News", "Access to News & Sentiment Dashboard"),
        ("backtest", "Backtest", "Access to Strategy Backtesting"),
        ("currency_change", "Currency Change", "Allow changing display currency"),
        ("phone_verification", "Phone Verification", "Require phone OTP verification during signup (if disabled, phone step is skipped)"),
    };

    public FeatureFlagService(IServiceScopeFactory scopeFactory, ILogger<FeatureFlagService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task SeedDefaultFlagsAsync()
    {
        using var db = CreateDbContext();

        foreach (var (key, name, desc) in DefaultFeatures)
        {
            if (!await db.FeatureFlags.AnyAsync(f => f.FeatureKey == key))
            {
                db.FeatureFlags.Add(new FeatureFlagEntity
                {
                    FeatureKey = key,
                    DisplayName = name,
                    Description = desc,
                    IsEnabled = true,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                });
            }
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Feature flags seeded.");
    }

    public async Task<List<FeatureFlagEntity>> GetAllFlagsAsync()
    {
        using var db = CreateDbContext();
        return await db.FeatureFlags.OrderBy(f => f.Id).ToListAsync();
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureKey)
    {
        using var db = CreateDbContext();
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.FeatureKey == featureKey);
        return flag?.IsEnabled ?? true; // Default to enabled if not found
    }

    public async Task<FeatureFlagEntity?> UpdateFlagAsync(string featureKey, bool isEnabled, string updatedBy)
    {
        using var db = CreateDbContext();
        var flag = await db.FeatureFlags.FirstOrDefaultAsync(f => f.FeatureKey == featureKey);
        if (flag == null) return null;

        flag.IsEnabled = isEnabled;
        flag.UpdatedAt = DateTime.UtcNow;
        flag.UpdatedBy = updatedBy;
        await db.SaveChangesAsync();

        _logger.LogInformation("Feature '{Feature}' set to {Enabled} by {User}", featureKey, isEnabled, updatedBy);
        return flag;
    }
}
