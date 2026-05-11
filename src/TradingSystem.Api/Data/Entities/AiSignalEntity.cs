namespace TradingSystem.Api.Data.Entities;

public class AiSignalEntity
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty; // Buy, Sell, Hold, StrongBuy, StrongSell
    public decimal Confidence { get; set; }
    public string Source { get; set; } = string.Empty; // RSI_MACD, Sentiment, Earnings, Insider, Correlation
    public string Analysis { get; set; } = string.Empty;
    public string? Metadata { get; set; } // JSON
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

public class ChatMessageEntity
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // user, assistant
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserEntity? User { get; set; }
}
