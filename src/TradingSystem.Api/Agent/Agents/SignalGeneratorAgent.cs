using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Agents;

public class SignalGeneratorAgent : BaseAgent
{
    private readonly HttpClient _httpClient;
    private readonly AgentSignalStore _store;

    public override string Name => "Signal Generator";
    public override string Description => "Generates BUY/SELL/HOLD signals using AI analysis";
    public override TimeSpan Interval => TimeSpan.FromMinutes(15);

    private readonly string[] _symbols = { "AAPL", "MSFT", "GOOGL", "TSLA", "AMZN", "NVDA", "RELIANCE.NS", "TCS.NS" };

    public SignalGeneratorAgent(IHttpClientFactory httpClientFactory, AgentSignalStore store, ILogger<SignalGeneratorAgent> logger)
        : base(logger)
    {
        _httpClient = httpClientFactory.CreateClient("AgentClient");
        _store = store;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var signalsGenerated = 0;

        foreach (var symbol in _symbols)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/predict/{symbol}", cancellationToken);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.TryGetProperty("data", out var prediction))
                {
                    var signal = ParsePrediction(symbol, prediction);
                    if (signal != null)
                    {
                        _store.AddSignal(signal);
                        signalsGenerated++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[SignalGenerator] Failed for {Symbol}: {Error}", symbol, ex.Message);
            }

            await Task.Delay(2000, cancellationToken);
        }

        State.LastMessage = $"Generated {signalsGenerated} signals from {_symbols.Length} symbols";
    }

    private AgentSignal? ParsePrediction(string symbol, JsonElement prediction)
    {
        try
        {
            var recommendation = prediction.TryGetProperty("recommendation", out var rec) 
                ? rec.GetString() ?? "Hold" : "Hold";
            var confidence = prediction.TryGetProperty("confidence", out var conf) 
                ? conf.GetDecimal() : 50m;
            var reason = prediction.TryGetProperty("analysis", out var analysis) 
                ? analysis.GetString() ?? "" : "";

            var signalType = recommendation.ToLower() switch
            {
                "strong buy" or "buy" => SignalType.Buy,
                "strong sell" or "sell" => SignalType.Sell,
                _ => SignalType.Hold
            };

            if (confidence < 60 && signalType == SignalType.Hold) return null;

            return new AgentSignal
            {
                Symbol = symbol,
                Signal = signalType,
                Confidence = confidence,
                Reason = string.IsNullOrEmpty(reason) ? $"AI prediction: {recommendation}" : reason,
                Source = "SignalGenerator",
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch { return null; }
    }
}
