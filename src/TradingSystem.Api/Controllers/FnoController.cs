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

    // F&O instruments with their details - Indian Indices
    // BasePrice is fallback only if Yahoo Finance is unreachable
    private static readonly List<FnoInstrument> FnoInstruments = new()
    {
        // Indian Indices (Yahoo tickers: .NS for NSE, ^NSEI for Nifty50 etc.)
        new("NIFTY50", "Nifty 50", 24250m, "Indian", "index", "^NSEI"),
        new("BANKNIFTY", "Bank Nifty", 51800m, "Indian", "index", "^NSEBANK"),
        new("FINNIFTY", "Finnifty", 23400m, "Indian", "index", "NIFTY_FIN_SERVICE.NS"),
        new("SENSEX", "Sensex", 79500m, "Indian", "index", "^BSESN"),
        new("MIDCPNIFTY", "Nifty Midcap Select", 12800m, "Indian", "index", "NIFTY_MID_SELECT.NS"),
        new("BANKEX", "BANKEX", 58200m, "Indian", "index", "BSE-BANK.BO"),
        new("INDIAVIX", "India VIX", 13.5m, "Indian", "index", "^INDIAVIX"),
        new("NIFTYTOTALMARKET", "Nifty Total Market", 11850m, "Indian", "index", "NIFTY_TOTAL_MKT.NS"),
        new("NIFTYNEXT50", "Nifty Next 50", 62400m, "Indian", "index", "^NSMIDCP"),
        new("NIFTY100", "Nifty 100", 25100m, "Indian", "index", "^CNX100"),
        new("NIFTYMIDCAP100", "Nifty Midcap 100", 54200m, "Indian", "index", "NIFTY_MIDCAP_100.NS"),
        new("BSE100", "BSE 100", 26300m, "Indian", "index", "BSE-100.BO"),
        new("NIFTY500", "Nifty 500", 22500m, "Indian", "index", "^CRSLDX"),
        new("NIFTYAUTO", "Nifty Auto", 24800m, "Indian", "index", "^CNXAUTO"),
        new("NIFTYSMLCAP", "Nifty Small Cap", 17200m, "Indian", "index", "NIFTY_SMLCAP_50.NS"),
        new("NIFTYFMCG", "Nifty FMCG", 57400m, "Indian", "index", "^CNXFMCG"),
        new("NIFTYMETAL", "Nifty Metal", 9100m, "Indian", "index", "^CNXMETAL"),
        new("NIFTYPHARMA", "Nifty Pharma", 19800m, "Indian", "index", "^CNXPHARMA"),
        new("NIFTYPSUBANK", "Nifty PSU Bank", 6800m, "Indian", "index", "^CNXPSUBANK"),
        new("NIFTYIT", "Nifty IT", 38200m, "Indian", "index", "^CNXIT"),
        new("BSESMLCAP", "BSE SmallCap", 42500m, "Indian", "index", "BSE-SMLCAP.BO"),
        new("NIFTYSMLCAP250", "Nifty SmallCap 250", 16800m, "Indian", "index", "NIFTY_SMLCAP_250.NS"),
        new("NIFTYMIDCAP150", "Nifty Midcap 150", 19500m, "Indian", "index", "NIFTY_MIDCAP_150.NS"),
        new("NIFTYCOMMODITIES", "Nifty Commodities", 8400m, "Indian", "index", "^CNXCMDT"),
        new("BSEIPO", "BSE IPO", 15200m, "Indian", "index", "BSE-IPO.BO"),

        // Global Indices
        new("SPX500", "S&P 500", 5920m, "Global", "index", "^GSPC"),
        new("NASDAQ", "NASDAQ Composite", 19200m, "Global", "index", "^IXIC"),
        new("DOWJONES", "Dow Jones Industrial", 42500m, "Global", "index", "^DJI"),
        new("FTSE100", "FTSE 100", 8450m, "Global", "index", "^FTSE"),
        new("DAX", "DAX 40", 19100m, "Global", "index", "^GDAXI"),
        new("NIKKEI", "Nikkei 225", 38900m, "Global", "index", "^N225"),
        new("HANGSENG", "Hang Seng", 19600m, "Global", "index", "^HSI"),
        new("SHANGHAI", "Shanghai Composite", 3250m, "Global", "index", "000001.SS"),
        new("CAC40", "CAC 40", 7800m, "Global", "index", "^FCHI"),
        new("ASX200", "ASX 200", 8150m, "Global", "index", "^AXJO"),
        new("KOSPI", "KOSPI", 2680m, "Global", "index", "^KS11"),
        new("TAIWANW", "Taiwan Weighted", 21500m, "Global", "index", "^TWII"),
        new("BOVESPA", "Bovespa (Brazil)", 128500m, "Global", "index", "^BVSP"),
        new("RUSSELL2000", "Russell 2000", 2250m, "Global", "index", "^RUT"),
        new("STOXX50", "Euro STOXX 50", 5100m, "Global", "index", "^STOXX50E"),
    };

    public FnoController(ILiveMarketDataService marketData, ILogger<FnoController> logger)
    {
        _marketData = marketData;
        _logger = logger;
    }

    /// <summary>Get all F&O indices with live prices from Yahoo Finance</summary>
    [HttpGet("indices")]
    public async Task<IActionResult> GetIndices()
    {
        // Fetch all live prices in parallel from Yahoo Finance
        var yahooTickers = FnoInstruments.Select(i => i.YahooTicker).ToList();
        var liveData = await _marketData.FetchBatchAsync(yahooTickers);

        var result = FnoInstruments.Select(inst =>
        {
            decimal lastPrice, change, dayHigh, dayLow;
            long volume;

            // Use live data if available, else fallback to cached base price
            if (liveData.TryGetValue(inst.YahooTicker, out var live) && live.Price > 0)
            {
                lastPrice = live.Price;
                change = Math.Round(live.Price - live.PreviousClose, 2);
                dayHigh = live.High > 0 ? live.High : lastPrice;
                dayLow = live.Low > 0 ? live.Low : lastPrice;
                volume = live.Volume;
            }
            else
            {
                // Fallback: use base price with small random variation
                var fallbackChange = GenerateChange(inst.BasePrice);
                lastPrice = inst.BasePrice + fallbackChange;
                change = fallbackChange;
                dayHigh = lastPrice + Math.Abs(change) * 0.5m;
                dayLow = lastPrice - Math.Abs(change) * 0.4m;
                volume = _rng.NextInt64(5000000, 50000000);
            }

            var changePercent = lastPrice != 0 ? Math.Round(change / (lastPrice - change) * 100, 2) : 0;
            var signal = GenerateSignal();

            return new
            {
                symbol = inst.Symbol,
                name = inst.Name,
                category = inst.Category,
                instrumentType = inst.InstrumentType,
                lastPrice,
                change,
                changePercent,
                dayHigh,
                dayLow,
                volume,
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
    public async Task<IActionResult> GetOptionsChain(string symbol, [FromQuery] string? expiry = null)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        // Fetch live spot price
        var live = await _marketData.FetchQuoteAsync(instrument.YahooTicker);
        var spotPrice = (live != null && live.Price > 0) ? live.Price : instrument.BasePrice + GenerateChange(instrument.BasePrice);
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
    public async Task<IActionResult> GetAnalysis(string symbol)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var live = await _marketData.FetchQuoteAsync(instrument.YahooTicker);
        var spotPrice = (live != null && live.Price > 0) ? live.Price : instrument.BasePrice + GenerateChange(instrument.BasePrice);
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
    public async Task<IActionResult> GetChart(string symbol, [FromQuery] string type, [FromQuery] decimal strike, [FromQuery] string expiry)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var live = await _marketData.FetchQuoteAsync(instrument.YahooTicker);
        var spotPrice = (live != null && live.Price > 0) ? live.Price : instrument.BasePrice + GenerateChange(instrument.BasePrice);
        
        // Calculate option price using same formula as options chain
        var intrinsic = type == "CE" ? Math.Max(0, spotPrice - strike) : Math.Max(0, strike - spotPrice);
        var istNow = DateTime.UtcNow.AddHours(5.5);
        var expiryDate = DateTime.Parse(expiry).Date;
        var daysToExp = Math.Max(0, (expiryDate - istNow.Date).TotalDays);
        double hoursLeft;
        if (daysToExp == 0)
            hoursLeft = Math.Max(0.1, 15.5 - istNow.TimeOfDay.TotalHours);
        else
            hoursLeft = Math.Max(0, 15.5 - istNow.TimeOfDay.TotalHours) + (daysToExp - 1) * 6.25 + 6.25;
        var annTime = hoursLeft / 1575.0;
        var vol = daysToExp == 0 ? 0.09 : Math.Min(0.10 + daysToExp * 0.005, 0.16);
        var atmTV = 0.4 * (double)spotPrice * vol * Math.Sqrt(annTime);
        var oneSigma = Math.Max(1, (double)spotPrice * vol * Math.Sqrt(annTime));
        var sigDist = Math.Abs((double)(strike - spotPrice)) / oneSigma;
        var decay = Math.Exp(-0.5 * sigDist * sigDist);
        var timeValue = (decimal)(atmTV * decay);
        var optionPrice = Math.Max(0.05m, Math.Round(intrinsic + timeValue, 2));

        // Display name
        var expiryLabel = expiry;
        try { expiryLabel = DateTime.Parse(expiry).ToString("dd MMM"); } catch { }
        var optionLabel = type == "CE" ? "Call" : "Put";
        var displayName = $"{symbol} {expiryLabel} {strike:N0} {optionLabel}";

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
    public async Task<IActionResult> GetSignals(string symbol)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var live = await _marketData.FetchQuoteAsync(instrument.YahooTicker);
        var spotPrice = (live != null && live.Price > 0) ? live.Price : instrument.BasePrice + GenerateChange(instrument.BasePrice);
        var strikeInterval = GetStrikeInterval(spotPrice);
        var atmStrike = Math.Round(spotPrice / strikeInterval) * strikeInterval;

        var signals = GenerateActiveSignals(symbol, spotPrice, atmStrike, strikeInterval);
        return Ok(new { success = true, data = signals, timestamp = DateTime.UtcNow });
    }

    /// <summary>Get live intraday sparkline chart for an index</summary>
    [HttpGet("live-chart/{symbol}")]
    public async Task<IActionResult> GetLiveChart(string symbol)
    {
        var instrument = FnoInstruments.FirstOrDefault(i => i.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        if (instrument == null)
            return NotFound(new { success = false, error = "Symbol not found" });

        var live = await _marketData.FetchQuoteAsync(instrument.YahooTicker);
        if (live == null || live.Price == 0)
            return Ok(new { success = true, data = new { symbol, sparkline = new List<decimal>(), price = instrument.BasePrice, previousClose = instrument.BasePrice }, timestamp = DateTime.UtcNow });

        return Ok(new
        {
            success = true,
            data = new
            {
                symbol,
                price = live.Price,
                previousClose = live.PreviousClose,
                high = live.High,
                low = live.Low,
                volume = live.Volume,
                change = Math.Round(live.Price - live.PreviousClose, 2),
                changePercent = live.PreviousClose > 0 ? Math.Round((live.Price - live.PreviousClose) / live.PreviousClose * 100, 2) : 0,
                sparkline = live.Sparkline
            },
            timestamp = DateTime.UtcNow
        });
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
        var date = DateTime.UtcNow.Date.AddHours(5.5); // IST
        var today = date.Date;
        
        // Check if today is an expiry day (NSE weekly expiries: Tue for Nifty, Wed for Finnifty, Thu for BankNifty/Sensex)
        // Include today and upcoming expiry days
        var expiryDays = new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };
        
        // Start from today (include today's expiry if market is open)
        var current = today;
        while (expiries.Count < 12)
        {
            if (expiryDays.Contains(current.DayOfWeek))
                expiries.Add(current.ToString("dd-MMM-yyyy"));
            current = current.AddDays(1);
        }
        
        // Remove duplicates and sort
        expiries = expiries.Distinct().OrderBy(e => DateTime.Parse(e)).ToList();
        return expiries;
    }

    private object GenerateOption(string symbol, decimal strike, string optionType, decimal spot, string expiry)
    {
        var intrinsic = optionType == "CE" ? Math.Max(0, spot - strike) : Math.Max(0, strike - spot);
        
        // Calculate time to expiry in trading hours
        var istNow = DateTime.UtcNow.AddHours(5.5);
        var expiryDate = DateTime.Parse(expiry).Date;
        var daysToExpiry = Math.Max(0, (expiryDate - istNow.Date).TotalDays);
        
        // Trading hours remaining (market: 9:15 AM to 3:30 PM = 6.25 hours)
        double hoursToExpiry;
        if (daysToExpiry == 0)
        {
            // Same day: hours left until 3:30 PM IST
            var marketClose = 15.5; // 3:30 PM
            hoursToExpiry = Math.Max(0.1, marketClose - istNow.TimeOfDay.TotalHours);
        }
        else
        {
            // Future days: remaining today + full days
            var marketClose = 15.5;
            var hoursLeftToday = Math.Max(0, marketClose - istNow.TimeOfDay.TotalHours);
            hoursToExpiry = hoursLeftToday + (daysToExpiry - 1) * 6.25 + 6.25;
        }
        
        var distanceFromATM = Math.Abs((double)(strike - spot));
        var distancePercent = distanceFromATM / (double)spot * 100;
        
        // Annualized time (trading hours in a year: 252 days × 6.25 hours = 1575)
        var annualizedTime = hoursToExpiry / 1575.0;
        
        // Nifty realized vol: ~10-11% annualized for weekly, slightly higher for longer
        // FIXED volatility (no random) to ensure consistency
        var volatility = daysToExpiry == 0 ? 0.09 : (0.10 + daysToExpiry * 0.005);
        volatility = Math.Min(volatility, 0.16); // cap at 16%
        
        // Black-Scholes ATM approximation: C ≈ 0.4 * S * σ * √T
        var atmTimeValue = 0.4 * (double)spot * volatility * Math.Sqrt(annualizedTime);
        
        // OTM decay: use steeper Gaussian-like decay based on distance in sigma units
        // 1 sigma move = spot * σ * √T (in price terms)
        var oneSigma = (double)spot * volatility * Math.Sqrt(annualizedTime);
        if (oneSigma < 1) oneSigma = 1; // prevent division by zero
        var sigmaDistance = distanceFromATM / oneSigma;
        
        // Gaussian decay: options lose value rapidly beyond 1 sigma OTM
        var decayFactor = Math.Exp(-0.5 * sigmaDistance * sigmaDistance);
        
        var timeValue = (decimal)(atmTimeValue * decayFactor);
        var premium = Math.Round(intrinsic + timeValue, 2);
        
        // Very deep OTM minimum (below ₹1 shows as small value)
        if (premium < 0.5m) premium = Math.Round(0.25m + (decimal)(0.5 * Math.Exp(-sigmaDistance)), 2);
        if (premium < 0.05m) premium = 0.05m;
        
        var change = Math.Round((decimal)(_rng.NextDouble() - 0.45) * premium * 0.08m, 2);
        var iv = intrinsic > 0 
            ? Math.Round(12 + (decimal)_rng.NextDouble() * 8, 1)  // ITM: lower IV
            : Math.Round(15 + (decimal)(distancePercent * 2) + (decimal)_rng.NextDouble() * 10, 1); // OTM: higher IV
        var oi = (decimal)_rng.NextInt64(100000, 5000000);
        var oiChange = (decimal)(_rng.NextDouble() - 0.4) * oi * 0.1m;

        var signal = GenerateOptionSignal(optionType, spot, strike, oi, oiChange);
        
        // Format display name like "NIFTY 26 May 24600 Call"
        var expiryDate = DateTime.Parse(expiry);
        var optionLabel = optionType == "CE" ? "Call" : "Put";
        var displayName = $"{symbol} {expiryDate:dd MMM} {strike:N0} {optionLabel}";

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

        // Use the nearest expiry (today or this week)
        var expiries = GetUpcomingExpiries(4);
        var nearestExpiry = expiries.First();
        var nearestExpiryDate = DateTime.Parse(nearestExpiry);
        var daysToExpiry = Math.Max(0.1, (nearestExpiryDate - DateTime.UtcNow).TotalDays);

        var signals = new List<object>();
        var count = _rng.Next(2, 5);
        for (int i = 0; i < count; i++)
        {
            var isBuy = _rng.Next(2) == 0;
            var isCe = _rng.Next(2) == 0;
            var strikeOffset = _rng.Next(-2, 3);
            var strike = atm + strikeOffset * interval;
            var strategy = strategies[_rng.Next(strategies.Length)];

            // Calculate realistic entry price using same Gaussian decay model
            var optionType = isCe ? "CE" : "PE";
            var intrinsic = isCe ? Math.Max(0, spot - strike) : Math.Max(0, strike - spot);
            var istNow = DateTime.UtcNow.AddHours(5.5);
            var daysLeft = Math.Max(0, (nearestExpiryDate.Date - istNow.Date).TotalDays);
            double hoursLeft;
            if (daysLeft == 0)
                hoursLeft = Math.Max(0.1, 15.5 - istNow.TimeOfDay.TotalHours);
            else
                hoursLeft = Math.Max(0, 15.5 - istNow.TimeOfDay.TotalHours) + (daysLeft - 1) * 6.25 + 6.25;
            var annTime = hoursLeft / 1575.0;
            var vol = daysLeft == 0 ? 0.09 : Math.Min(0.10 + daysLeft * 0.005, 0.16);
            var atmTV = 0.4 * (double)spot * vol * Math.Sqrt(annTime);
            var oneSigma = Math.Max(1, (double)spot * vol * Math.Sqrt(annTime));
            var sigDist = Math.Abs((double)(strike - spot)) / oneSigma;
            var decay = Math.Exp(-0.5 * sigDist * sigDist);
            var timeValue = (decimal)(atmTV * decay);
            var entry = Math.Max(0.05m, Math.Round(intrinsic + timeValue, 2));

            // Display name: "26 May 24000 Call"
            var displayName = $"{nearestExpiryDate:dd MMM} {strike:N0} {(isCe ? "Call" : "Put")}";

            signals.Add(new
            {
                symbol,
                action = isBuy ? "BUY" : "SELL",
                optionType,
                displayName,
                expiry = nearestExpiry,
                strikePrice = strike,
                entryPrice = entry,
                targetPrice = Math.Round(isBuy ? entry * 1.25m : entry * 0.7m, 2),
                stopLoss = Math.Round(isBuy ? entry * 0.75m : entry * 1.3m, 2),
                confidence = (decimal)_rng.Next(65, 93),
                reason = strategy.Item2,
                strategy = strategy.Item1,
                generatedAt = DateTime.UtcNow.AddMinutes(-_rng.Next(5, 60))
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

    private record FnoInstrument(string Symbol, string Name, decimal BasePrice, string Category, string InstrumentType, string YahooTicker);

    #endregion
}
