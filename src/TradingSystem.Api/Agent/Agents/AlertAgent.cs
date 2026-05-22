using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Agents;

public class AlertAgent : BaseAgent
{
    private readonly HttpClient _httpClient;
    private readonly AgentOrchestrator _orchestrator;

    public override string Name => "Alert Agent";
    public override string Description => "Sends email/notification alerts on high-confidence signals";
    public override TimeSpan Interval => TimeSpan.FromMinutes(5);

    private readonly HashSet<string> _sentAlerts = new();

    public AlertAgent(IHttpClientFactory httpClientFactory, AgentOrchestrator orchestrator, ILogger<AlertAgent> logger)
        : base(logger)
    {
        _httpClient = httpClientFactory.CreateClient("AgentClient");
        _orchestrator = orchestrator;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var signals = _orchestrator.GetRecentSignals(10);
        var alertsSent = 0;

        foreach (var signal in signals.Where(s => s.Confidence >= 75 && s.Signal != SignalType.Hold))
        {
            var alertKey = $"{signal.Symbol}_{signal.Signal}_{signal.GeneratedAt:yyyyMMddHH}";
            if (_sentAlerts.Contains(alertKey)) continue;

            try
            {
                var notification = new
                {
                    type = "ai_signal",
                    title = $"🤖 AI Signal: {signal.Signal.ToString().ToUpper()} {signal.Symbol}",
                    message = $"{signal.Reason} (Confidence: {signal.Confidence:F0}%)",
                    symbol = signal.Symbol
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(notification), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/notification/broadcast", content, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _sentAlerts.Add(alertKey);
                    alertsSent++;
                    _orchestrator.IncrementAlerts();
                    _logger.LogInformation("[AlertAgent] Sent alert: {Signal} {Symbol} ({Confidence}%)", 
                        signal.Signal, signal.Symbol, signal.Confidence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[AlertAgent] Failed to send alert for {Symbol}: {Error}", signal.Symbol, ex.Message);
            }
        }

        var cutoff = DateTime.UtcNow.AddHours(-2).ToString("yyyyMMddHH");
        _sentAlerts.RemoveWhere(k => k.CompareTo(cutoff) < 0);

        State.LastMessage = $"Sent {alertsSent} alerts, tracking {_sentAlerts.Count} deduplication keys";
    }
}
