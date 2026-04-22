namespace TradingSystem.Api.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}

public class ExportRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string DataType { get; set; } = "MarketData"; // MarketData, Backtest
    public int Days { get; set; } = 30;
}
