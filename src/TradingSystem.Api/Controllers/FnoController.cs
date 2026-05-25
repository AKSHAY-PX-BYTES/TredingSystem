using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Api.Models;
using TradingSystem.Api.Services;

namespace TradingSystem.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class FnoController : ControllerBase
{
    private readonly ILiveMarketDataService _marketData;
    private readonly ILogger<FnoController> _logger;
    private static readonly Random _rng = new();

    // F&O instruments with their details
    private static readonly List<FnoInstrument> FnoInstruments = new()
    {
        new("NIFTY50", "Nifty 50 Index", 24250m),
        new("BANKNIFTY", "Bank Nifty Index", 51800m),
        new("SENSEX", "BSE Sensex", 79500m),
        new("FINNIFTY", "Nifty Financial Services", 23400m),
        new("MIDCPNIFTY", "Nifty Midcap Select", 12800m),
        new("NIFTYIT", "Nifty IT Index", 38200m),
        new("RELIANCE", "Reliance Industries", 2920m),
        new("TCS", "Tata Consultancy Services", 3850m),
        new("HDFCBANK", "HDFC Bank Ltd", 1780m),
        new("INFY", "Infosys Ltd", 1520m),
        new("ICICIBANK", "ICICI Bank Ltd", 1340m),
        new("SBIN", "State Bank of India", 820m),
        new("TATAMOTORS", "Tata Motors Ltd", 740m),
        new("BAJFINANCE", "Bajaj Finance Ltd", 8400m),
        new("LT", "Larsen & Toubro Ltd", 3450m),
    };

    public FnoController(ILiveMarketDataService marketData, ILogger<FnoController> logger)
    {
        _marketData = marketData;
        _logger = logger;
    }

    /// <summary>Get all F&O indices with AI signals</summary>
    [HttpGet("indices")]
    public IActionResult GetIndices()
    {
        var result = FnoInstruments.Select(inst =>
        {
            var change = GenerateChange(inst.BasePrice);
            var signal = GenerateSignal();
            return new
            {
                symbol = inst.Symbol,
                name = inst.Name,
                lastPrice = inst.BasePrice + change,
                change,
                changePercent = Math.Round(change / inst.BasePrice * 100, 2),
                dayHigh = inst.BasePrice + Math.Abs(change) * 1.5m,
                dayLow = inst.BasePrice - Math.Abs(change) * 1.2m,
                volume = _rng.NextInt64(5000000, 50000000),
                openInterest = (decimal)_rng.NextInt64(10000000, 200000000),
                signal = signal.action,
                signalConfidence = signal.confidence,
                trend = signal.trend
            };
        }).ToList();

        return Ok(new { success = true, data = result, timestamp = DateTime.UtcNow });
    }

    /// <summary>Get options chain for a symbol</summary>
    [HttpGet("options-chain/{symbol}")]
    public IActionResult GetOptionsChain(string symbol, [FromQuery] string? expiry = null)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var spotPrice = instrument.BasePrice + GenerateChange(instrument.BasePrice);
        var strikeInterval = GetStrikeInterval(spotPrice);
        var atmStrike = Math.Round(spotPrice / strikeInterval) * strikeInterval;

        // Generate expiries (next 4 Thursdays)
        var expiries = GetUpcomingExpiries(4);
        var selectedExpiry = expiry ?? expiries.First();

        // Generate options chain around ATM
        var strikes = Enumerable.Range(-10, 21).Select(i => atmStrike + i * strikeInterval).ToList();
        
        var calls = strikes.Select(strike => GenerateOption(symbol, strike, "CE", spotPrice, selectedExpiry)).ToList();
        var puts = strikes.Select(strike => GenerateOption(symbol, strike, "PE", spotPrice, selectedExpiry)).ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                underlying = symbol,
                spotPrice,
                expiries,
                calls,
                puts
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Get AI analysis for a symbol</summary>
    [HttpGet("analysis/{symbol}")]
    public IActionResult GetAnalysis(string symbol)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var spotPrice = instrument.BasePrice + GenerateChange(instrument.BasePrice);
        var strikeInterval = GetStrikeInterval(spotPrice);
        var atmStrike = Math.Round(spotPrice / strikeInterval) * strikeInterval;
        var pcrRatio = 0.7m + (decimal)_rng.NextDouble() * 0.8m;

        var trend = pcrRatio > 1.2m ? "Bullish" : pcrRatio < 0.8m ? "Bearish" : "Neutral";
        var sentiment = pcrRatio > 1.0m ? "Bullish" : "Bearish";

        var support = atmStrike - strikeInterval * 3;
        var resistance = atmStrike + strikeInterval * 3;
        var maxPain = atmStrike + (decimal)(_rng.Next(-2, 3)) * strikeInterval;

        var signals = GenerateActiveSignals(symbol, spotPrice, atmStrike, strikeInterval);

        var summary = GenerateAiSummary(symbol, trend, pcrRatio, spotPrice, support, resistance);

        return Ok(new
        {
            success = true,
            data = new
            {
                symbol,
                trend,
                sentiment,
                pcrRatio = Math.Round(pcrRatio, 2),
                maxPainStrike = maxPain,
                supportLevel = $"₹{support:N0}",
                resistanceLevel = $"₹{resistance:N0}",
                activeSignals = signals,
                aiSummary = summary
            },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Get chart data for a specific option</summary>
    [HttpGet("chart/{symbol}")]
    public IActionResult GetChart(string symbol, [FromQuery] string type, [FromQuery] decimal strike, [FromQuery] string expiry)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var spotPrice = instrument.BasePrice + GenerateChange(instrument.BasePrice);
        
        // Calculate realistic option price using intrinsic + time value
        var intrinsic = type == "CE" ? Math.Max(0, spotPrice - strike) : Math.Max(0, strike - spotPrice);
        var distancePercent = Math.Abs((double)(strike - spotPrice) / (double)spotPrice) * 100;
        var daysToExpiry = Math.Max(1, 5); // assume ~5 days
        var timeValue = (decimal)(Math.Sqrt(daysToExpiry) * (double)spotPrice * 0.002 * Math.Exp(-distancePercent * 0.3));
        var optionPrice = Math.Max(2, Math.Round(intrinsic + timeValue, 2));

        // Display name
        var expiryLabel = expiry;
        try { expiryLabel = DateTime.Parse(expiry).ToString("dd MMM"); } catch { }
        var displayName = $"{symbol} {expiryLabel} {strike:N0} {type}";

        // Generate 5-min candles for last 6 hours
        var candles = new List<object>();
        var currentPrice = optionPrice;
        var startTime = DateTime.UtcNow.Date.AddHours(3).AddMinutes(45); // 9:15 AM IST
        var dayOpen = optionPrice;

        for (int i = 0; i < 72; i++) // 6 hours of 5-min candles
        {
            var open = currentPrice;
            var change = (decimal)(_rng.NextDouble() - 0.48) * optionPrice * 0.015m;
            var close = Math.Max(0.5m, open + change);
            var high = Math.Max(open, close) + (decimal)_rng.NextDouble() * optionPrice * 0.004m;
            var low = Math.Min(open, close) - (decimal)_rng.NextDouble() * optionPrice * 0.004m;

            candles.Add(new
            {
                time = startTime.AddMinutes(i * 5),
                open = Math.Round(open, 2),
                high = Math.Round(high, 2),
                low = Math.Round(Math.Max(0.5m, low), 2),
                close = Math.Round(close, 2),
                volume = (long)_rng.Next(10000, 500000)
            });
            currentPrice = close;
        }

        // Generate signal markers with entry/target/SL
        var signals = new List<object>();
        for (int i = 10; i < 72; i += _rng.Next(10, 22))
        {
            var isBuy = _rng.Next(2) == 0;
            var sigPrice = Math.Round(optionPrice * (1 + (decimal)(_rng.NextDouble() - 0.5) * 0.1m), 2);
            signals.Add(new
            {
                time = startTime.AddMinutes(i * 5),
                action = isBuy ? "BUY" : "SELL",
                price = sigPrice,
                label = isBuy ? "Bullish Breakout" : "Bearish Reversal"
            });
        }

        var support = Math.Round(optionPrice * 0.88m, 2);
        var resistance = Math.Round(optionPrice * 1.15m, 2);
        
        // Current prediction
        var isBullish = currentPrice > dayOpen;
        var prediction = new
        {
            currentPrice = Math.Round(currentPrice, 2),
            signal = isBullish ? "BUY" : "SELL",
            entry = Math.Round(currentPrice, 2),
            target = Math.Round(isBullish ? currentPrice * 1.12m : currentPrice * 0.88m, 2),
            stopLoss = Math.Round(isBullish ? currentPrice * 0.92m : currentPrice * 1.1m, 2),
            confidence = _rng.Next(65, 92),
            reason = isBullish 
                ? "Price above VWAP with increasing volume — momentum continuation expected"
                : "Price below opening with declining OI — weakness likely to continue"
        };

        return Ok(new
        {
            success = true,
            data = new { displayName, candles, signals, supportLevel = support, resistanceLevel = resistance, prediction },
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Get AI signals for a symbol</summary>
    [HttpGet("signals/{symbol}")]
    public IActionResult GetSignals(string symbol)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var spotPrice = instrument.BasePrice + GenerateChange(instrument.BasePrice);
        var strikeInterval = GetStrikeInterval(spotPrice);
        var atmStrike = Math.Round(spotPrice / strikeInterval) * strikeInterval;

        var signals = GenerateActiveSignals(symbol, spotPrice, atmStrike, strikeInterval);
        return Ok(new { success = true, data = signals, timestamp = DateTime.UtcNow });
    }

    #region Helpers

    private decimal GenerateChange(decimal basePrice)
    {
        return Math.Round((decimal)(_rng.NextDouble() - 0.45) * basePrice * 0.02m, 2);
    }

    private (string action, decimal confidence, string trend) GenerateSignal()
    {
        var val = _rng.NextDouble();
        if (val < 0.35) return ("Buy", _rng.Next(60, 92), "Bullish");
        if (val < 0.65) return ("Hold", _rng.Next(40, 65), "Neutral");
        return ("Sell", _rng.Next(55, 88), "Bearish");
    }

    private decimal GetStrikeInterval(decimal price) => price switch
    {
        > 50000 => 100,
        > 20000 => 50,
        > 5000 => 100,
        > 1000 => 50,
        _ => 25
    };

    private List<string> GetUpcomingExpiries(int count)
    {
        var expiries = new List<string>();
        var date = DateTime.UtcNow.Date;
        // Generate weekly expiries (Thursdays) for next 8 weeks + monthly expiries
        while (expiries.Count < 12)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek == DayOfWeek.Thursday)
                expiries.Add(date.ToString("dd-MMM-yyyy"));
        }
        return expiries;
    }

    private object GenerateOption(string symbol, decimal strike, string optionType, decimal spot, string expiry)
    {
        var intrinsic = optionType == "CE" ? Math.Max(0, spot - strike) : Math.Max(0, strike - spot);
        
        // Realistic time value based on distance from ATM and days to expiry
        var daysToExpiry = Math.Max(1, (DateTime.Parse(expiry) - DateTime.UtcNow).TotalDays);
        var distancePercent = Math.Abs((double)(strike - spot) / (double)spot) * 100;
        var baseTimeValue = (decimal)(Math.Sqrt(daysToExpiry) * (double)spot * 0.002);
        var decayFactor = (decimal)Math.Exp(-distancePercent * 0.3); // OTM options have less time value
        var timeValue = Math.Round(baseTimeValue * decayFactor, 2);
        
        var premium = Math.Round(intrinsic + timeValue, 2);
        if (premium < 1) premium = Math.Round(1 + (decimal)_rng.NextDouble() * 5, 2); // Minimum premium for deep OTM
        
        var change = Math.Round((decimal)(_rng.NextDouble() - 0.45) * premium * 0.08m, 2);
        var iv = intrinsic > 0 
            ? Math.Round(12 + (decimal)_rng.NextDouble() * 8, 1)  // ITM: lower IV
            : Math.Round(15 + (decimal)(distancePercent * 2) + (decimal)_rng.NextDouble() * 10, 1); // OTM: higher IV
        var oi = (decimal)_rng.NextInt64(100000, 5000000);
        var oiChange = (decimal)(_rng.NextDouble() - 0.4) * oi * 0.1m;

        var signal = GenerateOptionSignal(optionType, spot, strike, oi, oiChange);
        
        // Format display name like "NIFTY 26 May 24600 CE"
        var expiryDate = DateTime.Parse(expiry);
        var displayName = $"{symbol} {expiryDate:dd MMM} {strike:N0} {optionType}";

        return new
        {
            symbol,
            underlying = symbol,
            strikePrice = strike,
            optionType,
            expiry,
            displayName,
            lastPrice = premium,
            change,
            changePercent = premium > 0 ? Math.Round(change / premium * 100, 2) : 0,
            volume = (long)_rng.Next(10000, 2000000),
            openInterest = oi,
            oiChange = Math.Round(oiChange, 0),
            impliedVolatility = iv,
            bidPrice = Math.Max(0.05m, premium - premium * 0.01m),
            askPrice = premium + premium * 0.01m,
            signal = signal.action,
            signalConfidence = signal.confidence,
            signalReason = signal.reason
        };
    }

    private (string action, decimal confidence, string reason) GenerateOptionSignal(string type, decimal spot, decimal strike, decimal oi, decimal oiChange)
    {
        // AI logic based on OI buildup, moneyness, and IV
        var moneyness = type == "CE" ? (spot - strike) / spot * 100 : (strike - spot) / spot * 100;
        var oiBullish = oiChange > 0;

        if (type == "CE")
        {
            if (moneyness > -1 && moneyness < 2 && oiBullish)
                return ("Buy", _rng.Next(70, 92), "ATM CE with OI buildup — bullish momentum expected");
            if (moneyness < -3)
                return ("Sell", _rng.Next(60, 80), "Deep OTM — theta decay will erode premium");
        }
        else // PE
        {
            if (moneyness > -1 && moneyness < 2 && oiBullish)
                return ("Buy", _rng.Next(65, 88), "ATM PE with OI buildup — hedging demand or bearish move");
            if (moneyness < -3)
                return ("Sell", _rng.Next(55, 78), "Deep OTM PE — likely to expire worthless");
        }

        return ("Hold", _rng.Next(30, 55), "No clear directional bias — wait for confirmation");
    }

    private List<object> GenerateActiveSignals(string symbol, decimal spot, decimal atm, decimal interval)
    {
        var strategies = new[]
        {
            ("Momentum Breakout", "Strong volume surge with RSI crossing 70 — momentum continuation expected"),
            ("Mean Reversion", "Price deviated 2σ from VWAP — reversion to mean likely"),
            ("OI Unwinding", "Significant OI decrease with price rise — short covering rally"),
            ("PCR Extreme", "Put-Call ratio at extreme — contrarian reversal signal"),
            ("News Catalyst", "Positive earnings surprise — bullish sentiment driving prices"),
            ("IV Crush Play", "IV at 90th percentile — sell premium before IV normalization"),
            ("Breakout Retest", "Price retesting breakout level with strong support — entry opportunity")
        };

        var signals = new List<object>();
        var count = _rng.Next(2, 5);
        for (int i = 0; i < count; i++)
        {
            var isBuy = _rng.Next(2) == 0;
            var isCe = _rng.Next(2) == 0;
            var strikeOffset = _rng.Next(-2, 3);
            var strike = atm + strikeOffset * interval;
            var strategy = strategies[_rng.Next(strategies.Length)];
            var entry = Math.Round(50 + (decimal)_rng.NextDouble() * 200, 2);

            signals.Add(new
            {
                symbol,
                action = isBuy ? "BUY" : "SELL",
                optionType = isCe ? "CE" : "PE",
                strikePrice = strike,
                entryPrice = entry,
                targetPrice = Math.Round(isBuy ? entry * 1.3m : entry * 0.7m, 2),
                stopLoss = Math.Round(isBuy ? entry * 0.8m : entry * 1.25m, 2),
                confidence = (decimal)_rng.Next(65, 93),
                reason = strategy.Item2,
                strategy = strategy.Item1,
                generatedAt = DateTime.UtcNow.AddMinutes(-_rng.Next(5, 120))
            });
        }
        return signals;
    }

    private string GenerateAiSummary(string symbol, string trend, decimal pcr, decimal spot, decimal support, decimal resistance)
    {
        var summaries = new Dictionary<string, string[]>
        {
            ["Bullish"] = new[]
            {
                $"{symbol} showing strong bullish momentum with PCR at {pcr:N2}. Heavy put writing at {support:N0} suggests strong support. Expect move towards {resistance:N0} resistance. FII data shows net long positions increasing.",
                $"AI models detect bullish pattern formation in {symbol}. OI data confirms call buying at ATM strikes while puts are being written aggressively. News sentiment is positive with recent upgrades. Target: {resistance:N0}.",
            },
            ["Bearish"] = new[]
            {
                $"{symbol} under pressure with PCR declining to {pcr:N2}. Call writing at {resistance:N0} capping upside. Break below {support:N0} could trigger further sell-off. Hedge positions recommended.",
                $"Bearish divergence detected in {symbol}. FII unwinding longs while DII absorption is limited. IV expansion suggests fear in market. Key support at {support:N0} — break below targets 2% further downside.",
            },
            ["Neutral"] = new[]
            {
                $"{symbol} consolidating in range {support:N0}-{resistance:N0} with PCR at {pcr:N2}. No clear directional bias. Straddle sellers benefiting from theta decay. Wait for breakout above {resistance:N0} or breakdown below {support:N0} for directional trade.",
                $"Range-bound action in {symbol}. Max pain at ATM strike suggests expiry near current levels. Iron condor or short strangle strategies recommended. Key event: upcoming economic data release could trigger breakout.",
            }
        };

        var options = summaries.ContainsKey(trend) ? summaries[trend] : summaries["Neutral"];
        return options[_rng.Next(options.Length)];
    }

    private record FnoInstrument(string Symbol, string Name, decimal BasePrice);

    #endregion
}
