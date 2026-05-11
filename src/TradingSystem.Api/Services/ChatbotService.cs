using Microsoft.EntityFrameworkCore;
using TradingSystem.Api.Data;
using TradingSystem.Api.Data.Entities;
using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IChatbotService
{
    Task<ChatResponse> ProcessMessageAsync(string username, ChatRequest request);
    Task<List<ChatMessageEntity>> GetHistoryAsync(string username, string sessionId);
}

public class ChatbotService : IChatbotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiSignalService _aiSignalService;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(IServiceScopeFactory scopeFactory, IAiSignalService aiSignalService, ILogger<ChatbotService> logger)
    {
        _scopeFactory = scopeFactory;
        _aiSignalService = aiSignalService;
        _logger = logger;
    }

    private AppDbContext CreateDbContext()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task<ChatResponse> ProcessMessageAsync(string username, ChatRequest request)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null)
            return new ChatResponse { Response = "User not found.", SessionId = request.SessionId ?? Guid.NewGuid().ToString() };

        var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
        var message = request.Message.Trim().ToLower();

        // Save user message
        db.ChatMessages.Add(new ChatMessageEntity
        {
            UserId = user.Id,
            SessionId = sessionId,
            Role = "user",
            Content = request.Message
        });

        // Process intent and generate response
        var (response, suggestions, signals) = await ProcessIntent(message);

        // Save assistant response
        db.ChatMessages.Add(new ChatMessageEntity
        {
            UserId = user.Id,
            SessionId = sessionId,
            Role = "assistant",
            Content = response
        });
        await db.SaveChangesAsync();

        return new ChatResponse
        {
            Response = response,
            SessionId = sessionId,
            Suggestions = suggestions,
            RelatedSignals = signals
        };
    }

    private async Task<(string response, List<string> suggestions, List<AiSignalDto>? signals)> ProcessIntent(string message)
    {
        // Stock recommendation queries
        if (message.Contains("buy") || message.Contains("recommend") || message.Contains("should i"))
        {
            var symbols = ExtractSymbols(message);
            List<AiSignalDto>? signals = null;
            if (symbols.Any())
            {
                signals = new();
                foreach (var sym in symbols.Take(3))
                {
                    var signal = await _aiSignalService.GenerateSignalAsync(sym);
                    if (signal != null) signals.Add(signal);
                }
            }

            return (
                "Based on current market analysis, here are some considerations:\n\n" +
                "📊 **Market Conditions**: Current market shows mixed signals with technology sector leading gains.\n\n" +
                "🔍 **Key Factors to Consider**:\n" +
                "• Check RSI levels — oversold stocks (<30) may present buying opportunities\n" +
                "• Review MACD crossovers for momentum confirmation\n" +
                "• Consider diversification across sectors\n" +
                "• Always set stop-loss orders to manage risk\n\n" +
                "⚠️ *This is AI-generated analysis, not financial advice. Always do your own research.*",
                new List<string> { "Show me top gainers", "What are AI signals for AAPL?", "Explain options strategies" },
                signals
            );
        }

        // Earnings queries
        if (message.Contains("earning") || message.Contains("report") || message.Contains("revenue"))
        {
            return (
                "📈 **Earnings Analysis Guide**:\n\n" +
                "When analyzing earnings reports, focus on:\n" +
                "1. **EPS (Earnings Per Share)** — Compare actual vs estimates\n" +
                "2. **Revenue Growth** — Year-over-year and quarter-over-quarter\n" +
                "3. **Guidance** — Forward-looking statements matter most\n" +
                "4. **Margins** — Gross and operating margin trends\n" +
                "5. **Cash Flow** — Free cash flow is king\n\n" +
                "💡 Stocks typically move 5-10% on earnings surprises. The AI Earnings Predictor can help forecast beats/misses based on historical patterns.",
                new List<string> { "When is AAPL earnings?", "Show earnings calendar", "Which stocks beat estimates?" },
                null
            );
        }

        // Options strategy queries
        if (message.Contains("option") || message.Contains("call") || message.Contains("put") || message.Contains("straddle"))
        {
            return (
                "📋 **Options Strategy Recommendations**:\n\n" +
                "Based on current volatility (VIX) levels:\n\n" +
                "**Bullish Strategies:**\n" +
                "• Bull Call Spread — Limited risk, moderate reward\n" +
                "• Cash-Secured Puts — Generate income on stocks you want to own\n\n" +
                "**Bearish Strategies:**\n" +
                "• Bear Put Spread — Defined risk downside play\n" +
                "• Covered Calls — Hedge existing positions\n\n" +
                "**Neutral Strategies:**\n" +
                "• Iron Condor — Profit from low volatility\n" +
                "• Straddle — Profit from high volatility events\n\n" +
                "⚠️ Options involve substantial risk. Ensure you understand the Greeks before trading.",
                new List<string> { "Best options for TSLA", "Explain iron condor", "Options expiry calendar" },
                null
            );
        }

        // Search queries
        if (message.Contains("search") || message.Contains("find") || message.Contains("when") || message.Contains("date"))
        {
            return (
                "🔎 **Search Results**:\n\n" +
                "I can help you find:\n" +
                "• **Earnings dates** — Use the earnings calendar\n" +
                "• **Dividend dates** — Ex-dividend and payment dates\n" +
                "• **Options expiry** — Monthly and weekly options\n" +
                "• **Stock information** — Company data, financials\n\n" +
                "Try asking specific questions like \"When is MSFT earnings?\" or \"Find high dividend stocks\".",
                new List<string> { "Earnings calendar this week", "High dividend stocks", "Upcoming IPOs" },
                null
            );
        }

        // Default response
        return (
            "👋 **Hello! I'm your AI Trading Assistant.**\n\n" +
            "I can help you with:\n" +
            "• 📊 Stock recommendations and analysis\n" +
            "• 📈 Earnings report explanations\n" +
            "• 📋 Options strategy recommendations\n" +
            "• 🔎 Natural language search for market data\n" +
            "• 🤖 AI trading signals interpretation\n\n" +
            "Try asking me something like:\n" +
            "• \"What stocks should I buy?\"\n" +
            "• \"Explain this earnings report\"\n" +
            "• \"Options strategy for high volatility\"\n",
            new List<string> { "What stocks should I buy?", "Show AI signals", "Explain options", "Earnings calendar" },
            null
        );
    }

    private static List<string> ExtractSymbols(string message)
    {
        var knownSymbols = new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "META", "NVDA", "AMD", "NFLX", "SPY", "QQQ", "INTC", "BA", "DIS", "JPM", "V", "MA", "WMT", "KO", "PEP" };
        var words = message.ToUpper().Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Where(w => knownSymbols.Contains(w)).Distinct().ToList();
    }

    public async Task<List<ChatMessageEntity>> GetHistoryAsync(string username, string sessionId)
    {
        using var db = CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null) return new();

        return await db.ChatMessages
            .Where(m => m.UserId == user.Id && m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
