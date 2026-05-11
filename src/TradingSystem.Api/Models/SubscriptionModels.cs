namespace TradingSystem.Api.Models;

public class SubscriptionInfo
{
    public string Plan { get; set; } = "Free";
    public decimal PricePerMonth { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool IsTrialActive { get; set; }
    public bool IsTrialExpired { get; set; }
    public bool HasAccess { get; set; }
    public int DaysRemaining { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool AutoRenew { get; set; }
    public List<string> Features { get; set; } = new();
}

public class UpgradeRequest
{
    public string Plan { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
}

public class UpgradeResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public SubscriptionInfo? Subscription { get; set; }
}

public class PlanTier
{
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
    public int MaxWatchlistItems { get; set; }
    public int MaxApiRequestsPerDay { get; set; }
    public bool HasLiveQuotes { get; set; }
    public bool HasAiSignals { get; set; }
    public bool HasBacktesting { get; set; }
    public bool HasOptionsAnalysis { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasWhiteLabel { get; set; }
}
