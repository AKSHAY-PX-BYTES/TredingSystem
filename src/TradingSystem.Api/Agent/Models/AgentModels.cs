namespace TradingSystem.Agent.Models;

public enum AgentStatus
{
    Idle,
    Running,
    Success,
    Error,
    Disabled
}

public enum SignalType
{
    Buy,
    Sell,
    Hold
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public class AgentState
{
    public string AgentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AgentStatus Status { get; set; } = AgentStatus.Idle;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public int RunCount { get; set; }
    public int ErrorCount { get; set; }
    public string? LastMessage { get; set; }
    public string? LastError { get; set; }
    public TimeSpan Interval { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class AgentSignal
{
    public string Symbol { get; set; } = string.Empty;
    public SignalType Signal { get; set; }
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class SentimentScore
{
    public string Symbol { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Sentiment { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class RiskAssessment
{
    public string Symbol { get; set; } = string.Empty;
    public RiskLevel Risk { get; set; }
    public decimal Volatility { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

public class PortfolioRebalance
{
    public string Username { get; set; } = string.Empty;
    public List<RebalanceAction> Actions { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class RebalanceAction
{
    public string Symbol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public decimal CurrentWeight { get; set; }
    public decimal SuggestedWeight { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AgentDashboardResponse
{
    public List<AgentState> Agents { get; set; } = new();
    public List<AgentSignal> RecentSignals { get; set; } = new();
    public List<SentimentScore> RecentSentiments { get; set; } = new();
    public AgentSystemStats Stats { get; set; } = new();
}

public class AgentSystemStats
{
    public int TotalRuns { get; set; }
    public int TotalErrors { get; set; }
    public int SignalsGenerated { get; set; }
    public int AlertsSent { get; set; }
    public DateTime SystemStartedAt { get; set; }
    public TimeSpan Uptime => DateTime.UtcNow - SystemStartedAt;
}
