using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface ISubscriptionService
{
    Task<SubscriptionInfo> GetSubscriptionAsync(string username);
    Task<UpgradeResponse> UpgradeAsync(string username, UpgradeRequest request);
    Task<UpgradeResponse> CancelAsync(string username);
    Task<bool> HasAccessAsync(string username);
    Task<bool> HasFeatureAccessAsync(string username, string feature);
    List<PlanTier> GetAvailablePlans();
}

public class SubscriptionService : ISubscriptionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionService> _logger;

    private static readonly Dictionary<string, PlanTier> PlanDefinitions = new()
    {
        ["Free"] = new PlanTier
        {
            Name = "Free",
            MonthlyPrice = 0,
            AnnualPrice = 0,
            MaxWatchlistItems = 5,
            MaxApiRequestsPerDay = 0,
            HasLiveQuotes = false,
            HasAiSignals = false,
            HasBacktesting = false,
            HasOptionsAnalysis = false,
            HasApiAccess = false,
            HasPrioritySupport = false,
            HasWhiteLabel = false,
            Features = new List<string>
            {
                "5 watchlist items",
                "Basic charts (5 years)",
                "Daily news digest",
                "7-day free trial"
            }
        },
        ["Pro"] = new PlanTier
        {
            Name = "Pro",
            MonthlyPrice = 9.99m,
            AnnualPrice = 95.88m,
            IsPopular = true,
            MaxWatchlistItems = -1, // unlimited
            MaxApiRequestsPerDay = 0,
            HasLiveQuotes = true,
            HasAiSignals = false,
            HasBacktesting = false,
            HasOptionsAnalysis = false,
            HasApiAccess = false,
            HasPrioritySupport = false,
            HasWhiteLabel = false,
            Features = new List<string>
            {
                "LIVE quotes (real-time)",
                "Unlimited watchlists",
                "Advanced charting",
                "Portfolio analysis + alerts",
                "No ads",
                "Email notifications"
            }
        },
        ["Premium"] = new PlanTier
        {
            Name = "Premium",
            MonthlyPrice = 19.99m,
            AnnualPrice = 191.88m,
            MaxWatchlistItems = -1,
            MaxApiRequestsPerDay = 100,
            HasLiveQuotes = true,
            HasAiSignals = true,
            HasBacktesting = true,
            HasOptionsAnalysis = true,
            HasApiAccess = true,
            HasPrioritySupport = true,
            HasWhiteLabel = false,
            Features = new List<string>
            {
                "Everything in PRO",
                "AI trading signals",
                "Strategy backtesting",
                "Options analysis toolkit",
                "API access (100 req/day)",
                "Priority support"
            }
        },
        ["Enterprise"] = new PlanTier
        {
            Name = "Enterprise",
            MonthlyPrice = 499m,
            AnnualPrice = 4788m,
            MaxWatchlistItems = -1,
            MaxApiRequestsPerDay = -1,
            HasLiveQuotes = true,
            HasAiSignals = true,
            HasBacktesting = true,
            HasOptionsAnalysis = true,
            HasApiAccess = true,
            HasPrioritySupport = true,
            HasWhiteLabel = true,
            Features = new List<string>
            {
                "Dedicated API",
                "White-label capability",
                "Custom integrations",
                "Unlimited API requests",
                "Custom data feeds",
                "Account manager"
            }
        }
    };

    public SubscriptionService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task<SubscriptionInfo> GetSubscriptionAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return new SubscriptionInfo { HasAccess = false };

        var plan = user.Plan;
        var trialEnds = user.TrialEndsAt;
        var now = DateTime.UtcNow;
        var isTrialActive = plan == "Free" && now < trialEnds;
        var isTrialExpired = plan == "Free" && now >= trialEnds;
        var hasAccess = plan != "Free" || isTrialActive;
        var daysRemaining = isTrialActive ? (int)(trialEnds - now).TotalDays : 0;

        var planDef = PlanDefinitions.GetValueOrDefault(plan, PlanDefinitions["Free"]);

        return new SubscriptionInfo
        {
            Plan = plan,
            PricePerMonth = planDef.MonthlyPrice,
            TrialEndsAt = trialEnds,
            IsTrialActive = isTrialActive,
            IsTrialExpired = isTrialExpired,
            HasAccess = hasAccess,
            DaysRemaining = daysRemaining,
            Features = planDef.Features
        };
    }

    public async Task<bool> HasAccessAsync(string username)
    {
        var sub = await GetSubscriptionAsync(username);
        return sub.HasAccess;
    }

    public async Task<bool> HasFeatureAccessAsync(string username, string feature)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return false;

        var plan = user.Plan;
        if (plan == "Free")
        {
            var now = DateTime.UtcNow;
            if (now >= user.TrialEndsAt) return false; // trial expired
        }

        var planDef = PlanDefinitions.GetValueOrDefault(plan, PlanDefinitions["Free"]);

        return feature.ToLower() switch
        {
            "live_quotes" => planDef.HasLiveQuotes,
            "ai_signals" => planDef.HasAiSignals,
            "backtesting" => planDef.HasBacktesting,
            "options_analysis" => planDef.HasOptionsAnalysis,
            "api_access" => planDef.HasApiAccess,
            "priority_support" => planDef.HasPrioritySupport,
            "white_label" => planDef.HasWhiteLabel,
            _ => true // basic features available to all with access
        };
    }

    public async Task<UpgradeResponse> UpgradeAsync(string username, UpgradeRequest request)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return new UpgradeResponse { Success = false, Error = "User not found" };

        if (!PlanDefinitions.ContainsKey(request.Plan))
            return new UpgradeResponse { Success = false, Error = "Invalid plan" };

        var oldPlan = user.Plan;
        user.Plan = request.Plan;

        if (request.Plan != "Free")
        {
            user.IsTrialUsed = true;
        }

        // Create subscription record
        var sub = new SubscriptionEntity
        {
            UserId = user.Id,
            Plan = request.Plan,
            PricePerMonth = PlanDefinitions[request.Plan].MonthlyPrice,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            PaymentMethod = request.PaymentMethod,
            TransactionId = request.TransactionId
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        _logger.LogInformation("User {Username} upgraded from {OldPlan} to {NewPlan}", username, oldPlan, request.Plan);

        var info = await GetSubscriptionAsync(username);
        return new UpgradeResponse { Success = true, Message = $"Upgraded to {request.Plan}", Subscription = info };
    }

    public async Task<UpgradeResponse> CancelAsync(string username)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return new UpgradeResponse { Success = false, Error = "User not found" };

        user.Plan = "Free";
        var activeSub = await db.Subscriptions
            .Where(s => s.UserId == user.Id && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeSub != null)
        {
            activeSub.IsActive = false;
            activeSub.CancelledAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        var info = await GetSubscriptionAsync(username);
        return new UpgradeResponse { Success = true, Message = "Subscription cancelled", Subscription = info };
    }

    public List<PlanTier> GetAvailablePlans()
    {
        return PlanDefinitions.Values.ToList();
    }
}
