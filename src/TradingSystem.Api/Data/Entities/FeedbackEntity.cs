namespace TradingSystem.Api.Data.Entities;

public class FeedbackEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "Suggestion"; // Bug, Suggestion, Feedback
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed
    public string? AdminResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public UserEntity? User { get; set; }
}
