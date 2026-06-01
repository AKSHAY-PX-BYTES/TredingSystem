using System.ComponentModel.DataAnnotations;

namespace TradingSystem.Web.Models;

// API Response wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

// Market Data Models
public class StockData
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
}

public class StockQuote
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public decimal Week52High { get; set; }
    public decimal Week52Low { get; set; }
    public decimal MarketCap { get; set; }
    public long Volume { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime LastUpdated { get; set; }
    /// <summary>
    /// Native currency of the price from the exchange (e.g. "USD", "INR").
    /// </summary>
    public string PriceCurrency { get; set; } = "USD";
    public List<StockData> HistoricalData { get; set; } = new();
}

// Technical Indicators
public class TechnicalIndicators
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal SMA20 { get; set; }
    public decimal SMA50 { get; set; }
    public decimal SMA200 { get; set; }
    public decimal EMA12 { get; set; }
    public decimal EMA26 { get; set; }
    public decimal RSI { get; set; }
    public decimal MACD { get; set; }
    public decimal BollingerUpper { get; set; }
    public decimal BollingerMiddle { get; set; }
    public decimal BollingerLower { get; set; }
    public string Trend { get; set; } = string.Empty;
}

// News Models
public class NewsItem
{
    public string Headline { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? Symbol { get; set; }
}

public class NewsAnalysisRequest
{
    public List<string> Headlines { get; set; } = new();
    public string? Symbol { get; set; }
}

public class SentimentResult
{
    public string Headline { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal Confidence { get; set; }
}

public class NewsAnalysisResponse
{
    public List<SentimentResult> Results { get; set; } = new();
    public string OverallSentiment { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

// Prediction Models
public class PredictionResult
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal PredictedChange { get; set; }
    public decimal PredictedChangePercent { get; set; }
    public string Direction { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string TimeHorizon { get; set; } = string.Empty;
    public DateTime PredictedAt { get; set; }
    public Dictionary<string, decimal> FeatureImportance { get; set; } = new();
}

// Strategy Models
public class StrategyResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public TechnicalIndicators? Indicators { get; set; }
    public PredictionResult? Prediction { get; set; }
    public string? SentimentSummary { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<string> Reasons { get; set; } = new();
}

// Backtest Models
public class BacktestRequest
{
    public string Symbol { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
    public decimal InitialCapital { get; set; } = 10000m;
    public string Strategy { get; set; } = "Combined";
}

public class BacktestTrade
{
    public int TradeNumber { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public string Signal { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal? ProfitLoss { get; set; }
    public decimal? ProfitLossPercent { get; set; }
    public decimal PortfolioValue { get; set; }
}

public class BacktestResult
{
    public string Symbol { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public List<BacktestTrade> Trades { get; set; } = new();
    public List<EquityPoint> EquityCurve { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Strategy { get; set; } = string.Empty;
}

public class EquityPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

// Auth Models
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public UserInfo? User { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Plan { get; set; } = "Free";
    public DateTime? TrialEndsAt { get; set; }
    public bool IsTrialExpired { get; set; }
    public bool HasAccess { get; set; } = true;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Plan { get; set; } = "Free";

    public string? PhoneNumber { get; set; }
    public string? CountryCode { get; set; }
    public string? PaymentOrderId { get; set; }
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class SendOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class VerifyOtpResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class ChangePasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

// Currency Models
public class CurrencyInfo
{
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal RateFromUsd { get; set; }
}

public class CurrencyListResponse
{
    public List<CurrencyInfo> Currencies { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

// Market Exchange Models
public class MarketIndex
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ExchangeStock
{
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public long Volume { get; set; }
    public decimal MarketCap { get; set; }
    public List<decimal> PriceHistory { get; set; } = new();
}

public class ExchangeData
{
    public string ExchangeName { get; set; } = string.Empty;
    public string ExchangeCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public List<MarketIndex> Indices { get; set; } = new();
    public List<ExchangeStock> TopGainers { get; set; } = new();
    public List<ExchangeStock> TopLosers { get; set; } = new();
    public List<ExchangeStock> MostActive { get; set; } = new();
    public List<ExchangeStock> AllStocks { get; set; } = new();
    public bool IsLive { get; set; }
    public List<FnoContract> EquityFutures { get; set; } = new();
    public List<FnoContract> EquityOptions { get; set; } = new();
    public List<FnoContract> CommodityFutures { get; set; } = new();
    public List<FnoContract> CommodityOptions { get; set; } = new();
}

public class FnoContract
{
    public string Symbol { get; set; } = string.Empty;
    public string UnderlyingName { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string InstrumentType { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public decimal StrikePrice { get; set; }
    public decimal LastPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public long OpenInterest { get; set; }
    public long OIChange { get; set; }
    public decimal LotSize { get; set; }
    public string Signal { get; set; } = string.Empty;
    public decimal ImpliedVolatility { get; set; }
}

// Subscription Models
public class SubscriptionInfo
{
    public string Plan { get; set; } = "Free";
    public decimal PricePerMonth { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public bool IsTrialActive { get; set; }
    public bool IsTrialExpired { get; set; }
    public bool HasAccess { get; set; }
    public int DaysRemaining { get; set; }
    public List<string> Features { get; set; } = new();
}

public class PlanTier
{
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; }
}

public class UpgradeRequest
{
    public string Plan { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
}

public class UpgradeResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public SubscriptionInfo? Subscription { get; set; }
}

// Notification Models
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

public class CreatePriceAlertRequest
{
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public decimal ThresholdPercent { get; set; } = 5.0m;
    public string Direction { get; set; } = "Above";
}

// AI Models
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

// Payment Models
public class CreatePaymentOrderRequest
{
    public string Plan { get; set; } = string.Empty;
    public bool IsAnnual { get; set; }
}

public class AnonymousCreateOrderRequest
{
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsAnnual { get; set; }
}

public class CreatePaymentOrderResponse
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? RazorpayKeyId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Description { get; set; }
    public string? Error { get; set; }
}

public class VerifyPaymentRequest
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsAnnual { get; set; }
}

public class AnonymousVerifyRequest
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

public class VerifyPaymentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? TransactionId { get; set; }
}

public class PaymentHistoryItem
{
    public string TransactionId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// F&O Models
public class FnoIndex
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Indian"; // Indian / Global
    public string InstrumentType { get; set; } = "index";
    public decimal LastPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public long Volume { get; set; }
    public decimal OpenInterest { get; set; }
    public string Signal { get; set; } = "Hold"; // Buy/Sell/Hold
    public decimal SignalConfidence { get; set; }
    public string Trend { get; set; } = "Neutral"; // Bullish/Bearish/Neutral
}

public class OptionsContract
{
    public string Symbol { get; set; } = string.Empty;
    public string Underlying { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public decimal StrikePrice { get; set; }
    public string OptionType { get; set; } = string.Empty; // CE (Call) / PE (Put)
    public string Expiry { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal OpenInterest { get; set; }
    public decimal OiChange { get; set; }
    public decimal ImpliedVolatility { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public string Signal { get; set; } = "Hold";
    public decimal SignalConfidence { get; set; }
    public string SignalReason { get; set; } = string.Empty;
}

public class OptionsChain
{
    public string Underlying { get; set; } = string.Empty;
    public decimal SpotPrice { get; set; }
    public List<string> Expiries { get; set; } = new();
    public List<OptionsContract> Calls { get; set; } = new();
    public List<OptionsContract> Puts { get; set; } = new();
    // Data provenance: "LIVE" = real market data, "UNAVAILABLE" = no live feed, "ESTIMATED" = model-based (demo only)
    public string Source { get; set; } = string.Empty;
    public bool IsLive { get; set; }
    public string? DataMessage { get; set; }
}

public class FnoSignal
{
    public string Symbol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // BUY/SELL
    public string OptionType { get; set; } = string.Empty; // CE/PE
    public string DisplayName { get; set; } = string.Empty; // "26 May 24000 Call"
    public string Expiry { get; set; } = string.Empty;
    public decimal StrikePrice { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class FnoChartData
{
    public string DisplayName { get; set; } = string.Empty;
    public List<FnoCandle> Candles { get; set; } = new();
    public List<FnoSignalMarker> Signals { get; set; } = new();
    public decimal SupportLevel { get; set; }
    public decimal ResistanceLevel { get; set; }
    public FnoPrediction? Prediction { get; set; }
}

public class FnoPrediction
{
    public decimal CurrentPrice { get; set; }
    public string Signal { get; set; } = string.Empty;
    public decimal Entry { get; set; }
    public decimal Target { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class FnoCandle
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class FnoSignalMarker
{
    public DateTime Time { get; set; }
    public string Action { get; set; } = string.Empty; // BUY/SELL
    public decimal Price { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class FnoAnalysis
{
    public string Symbol { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public decimal PcrRatio { get; set; }
    public decimal MaxPainStrike { get; set; }
    public string SupportLevel { get; set; } = string.Empty;
    public string ResistanceLevel { get; set; } = string.Empty;
    public List<FnoSignal> ActiveSignals { get; set; } = new();
    public string AiSummary { get; set; } = string.Empty;
}

public class LiveChartData
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public List<decimal> Sparkline { get; set; } = new();
}
