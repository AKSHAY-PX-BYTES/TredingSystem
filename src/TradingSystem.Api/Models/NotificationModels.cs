namespace TradingSystem.Api.Models;

public class NotificationDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePriceAlertRequest
{
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal ThresholdPercent { get; set; } = 5.0m;
    public string Direction { get; set; } = "Above";
}

public class PriceAlertDto
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal ThresholdPercent { get; set; }
    public string Direction { get; set; } = string.Empty;
    public bool IsTriggered { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AiSignalDto
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public List<string>? Suggestions { get; set; }
    public List<AiSignalDto>? RelatedSignals { get; set; }
}

public class LocaleInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string Direction { get; set; } = "ltr";
}
