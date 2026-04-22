using TradingSystem.Api.Models;

namespace TradingSystem.Api.Services;

public interface IBacktestService
{
    Task<BacktestResult> RunAsync(BacktestRequest request);
}

public class BacktestService : IBacktestService
{
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<BacktestService> _logger;

    public BacktestService(IMarketDataService marketDataService, ILogger<BacktestService> logger)
    {
        _marketDataService = marketDataService;
        _logger = logger;
    }

    public async Task<BacktestResult> RunAsync(BacktestRequest request)
    {
        _logger.LogInformation("Running backtest for {Symbol} over {Days} days with strategy {Strategy}",
            request.Symbol, request.Days, request.Strategy);

        var historicalData = await _marketDataService.GetHistoricalDataAsync(request.Symbol, request.Days + 50);
        if (historicalData.Count < 50)
        {
            throw new InvalidOperationException("Insufficient historical data for backtesting");
        }

        var trades = new List<BacktestTrade>();
        var equityCurve = new List<EquityPoint>();
        var capital = request.InitialCapital;
        var position = 0m; // shares held
        var entryPrice = 0m;
        var entryDate = DateTime.MinValue;
        var tradeNumber = 0;
        var peakCapital = capital;
        var maxDrawdown = 0m;
        var dailyReturns = new List<decimal>();
        var previousCapital = capital;

        var closes = historicalData.Select(d => d.Close).ToList();

        for (int i = 50; i < historicalData.Count; i++)
        {
            var current = historicalData[i];
            var pastCloses = closes.Take(i + 1).ToList();

            // Calculate signals based on strategy type
            var signal = GenerateSignal(pastCloses, request.Strategy);

            // Portfolio value at this point
            var portfolioValue = capital + (position * current.Close);

            // Track equity curve
            equityCurve.Add(new EquityPoint { Date = current.Date, Value = Math.Round(portfolioValue, 2) });

            // Daily returns for Sharpe ratio
            var dailyReturn = previousCapital > 0 ? (portfolioValue - previousCapital) / previousCapital : 0;
            dailyReturns.Add(dailyReturn);
            previousCapital = portfolioValue;

            // Max drawdown
            if (portfolioValue > peakCapital) peakCapital = portfolioValue;
            var drawdown = peakCapital > 0 ? (peakCapital - portfolioValue) / peakCapital : 0;
            if (drawdown > maxDrawdown) maxDrawdown = drawdown;

            // Execute trades
            if (signal == "BUY" && position == 0)
            {
                // Buy with all capital
                position = Math.Floor(capital / current.Close);
                if (position > 0)
                {
                    entryPrice = current.Close;
                    entryDate = current.Date;
                    capital -= position * current.Close;
                    tradeNumber++;
                }
            }
            else if (signal == "SELL" && position > 0)
            {
                // Sell all shares
                var exitPrice = current.Close;
                var proceeds = position * exitPrice;
                var pnl = proceeds - (position * entryPrice);
                var pnlPct = entryPrice > 0 ? (exitPrice - entryPrice) / entryPrice * 100 : 0;
                capital += proceeds;

                trades.Add(new BacktestTrade
                {
                    TradeNumber = tradeNumber,
                    EntryDate = entryDate,
                    ExitDate = current.Date,
                    Signal = "BUY→SELL",
                    EntryPrice = Math.Round(entryPrice, 2),
                    ExitPrice = Math.Round(exitPrice, 2),
                    ProfitLoss = Math.Round(pnl, 2),
                    ProfitLossPercent = Math.Round(pnlPct, 2),
                    PortfolioValue = Math.Round(capital, 2)
                });

                position = 0;
            }
        }

        // Close any open position at the end
        if (position > 0)
        {
            var lastPrice = historicalData.Last().Close;
            var proceeds = position * lastPrice;
            var pnl = proceeds - (position * entryPrice);
            var pnlPct = entryPrice > 0 ? (lastPrice - entryPrice) / entryPrice * 100 : 0;
            capital += proceeds;

            trades.Add(new BacktestTrade
            {
                TradeNumber = tradeNumber,
                EntryDate = entryDate,
                ExitDate = historicalData.Last().Date,
                Signal = "BUY→CLOSE",
                EntryPrice = Math.Round(entryPrice, 2),
                ExitPrice = Math.Round(lastPrice, 2),
                ProfitLoss = Math.Round(pnl, 2),
                ProfitLossPercent = Math.Round(pnlPct, 2),
                PortfolioValue = Math.Round(capital, 2)
            });
            position = 0;
        }

        var winningTrades = trades.Count(t => t.ProfitLoss > 0);
        var losingTrades = trades.Count(t => t.ProfitLoss <= 0);
        var totalReturn = capital - request.InitialCapital;
        var sharpeRatio = CalculateSharpeRatio(dailyReturns);

        return new BacktestResult
        {
            Symbol = request.Symbol.ToUpperInvariant(),
            TotalDays = request.Days,
            InitialCapital = request.InitialCapital,
            FinalCapital = Math.Round(capital, 2),
            TotalReturn = Math.Round(totalReturn, 2),
            TotalReturnPercent = Math.Round(totalReturn / request.InitialCapital * 100, 2),
            TotalTrades = trades.Count,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            WinRate = trades.Count > 0 ? Math.Round((decimal)winningTrades / trades.Count * 100, 1) : 0,
            MaxDrawdown = Math.Round(maxDrawdown * 100, 2),
            SharpeRatio = Math.Round(sharpeRatio, 3),
            Trades = trades,
            EquityCurve = equityCurve,
            StartDate = historicalData.Skip(50).First().Date,
            EndDate = historicalData.Last().Date,
            Strategy = request.Strategy
        };
    }

    private string GenerateSignal(List<decimal> closes, string strategy)
    {
        if (closes.Count < 50) return "HOLD";

        return strategy switch
        {
            "TechnicalOnly" => TechnicalSignal(closes),
            "SentimentOnly" => SentimentBasedSignal(closes),
            _ => CombinedSignal(closes) // "Combined"
        };
    }

    private string TechnicalSignal(List<decimal> closes)
    {
        var sma20 = MarketDataService.CalculateSMA(closes, 20);
        var sma50 = MarketDataService.CalculateSMA(closes, 50);
        var rsi = MarketDataService.CalculateRSI(closes, 14);
        var currentPrice = closes.Last();

        // Golden cross buy, death cross sell
        if (currentPrice > sma20 && sma20 > sma50 && rsi < 70)
            return "BUY";
        if (currentPrice < sma20 && sma20 < sma50 && rsi > 30)
            return "SELL";

        // RSI extremes
        if (rsi < 25) return "BUY";
        if (rsi > 75) return "SELL";

        return "HOLD";
    }

    private string SentimentBasedSignal(List<decimal> closes)
    {
        // Simulated sentiment-based approach using price momentum as proxy
        var shortMomentum = closes.TakeLast(5).Average() / closes.TakeLast(10).Average() - 1;
        if (shortMomentum > 0.02m) return "BUY";
        if (shortMomentum < -0.02m) return "SELL";
        return "HOLD";
    }

    private string CombinedSignal(List<decimal> closes)
    {
        var sma20 = MarketDataService.CalculateSMA(closes, 20);
        var sma50 = MarketDataService.CalculateSMA(closes, 50);
        var rsi = MarketDataService.CalculateRSI(closes, 14);
        var ema12 = MarketDataService.CalculateEMA(closes, 12);
        var ema26 = MarketDataService.CalculateEMA(closes, 26);
        var currentPrice = closes.Last();

        var score = 0;

        // Trend
        if (currentPrice > sma20) score++;
        else score--;

        if (sma20 > sma50) score++;
        else score--;

        // MACD
        if (ema12 > ema26) score++;
        else score--;

        // RSI
        if (rsi < 35) score += 2;
        else if (rsi > 65) score -= 2;

        // Momentum
        var momentum = closes.TakeLast(5).Average() / closes.TakeLast(20).Average() - 1;
        if (momentum > 0.01m) score++;
        else if (momentum < -0.01m) score--;

        if (score >= 3) return "BUY";
        if (score <= -3) return "SELL";
        return "HOLD";
    }

    private decimal CalculateSharpeRatio(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count < 2) return 0;

        var avgReturn = dailyReturns.Average();
        var stdDev = (decimal)Math.Sqrt((double)dailyReturns
            .Select(r => (r - avgReturn) * (r - avgReturn))
            .Average());

        if (stdDev == 0) return 0;

        // Annualized Sharpe (assuming 252 trading days, risk-free rate = 0 for simplicity)
        return avgReturn / stdDev * (decimal)Math.Sqrt(252);
    }
}
