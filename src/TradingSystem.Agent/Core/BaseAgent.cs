using Microsoft.Extensions.Logging;
using TradingSystem.Agent.Models;

namespace TradingSystem.Agent.Core;

public abstract class BaseAgent : IAgent
{
    protected readonly ILogger _logger;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract TimeSpan Interval { get; }
    public AgentState State { get; } = new();

    protected BaseAgent(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!State.IsEnabled) return;

        State.Status = AgentStatus.Running;
        State.AgentName = Name;
        State.Description = Description;
        State.Interval = Interval;

        try
        {
            _logger.LogInformation("[{Agent}] Starting execution...", Name);
            await RunAsync(cancellationToken);
            
            State.Status = AgentStatus.Success;
            State.LastRunAt = DateTime.UtcNow;
            State.NextRunAt = DateTime.UtcNow.Add(Interval);
            State.RunCount++;
            State.LastError = null;
            _logger.LogInformation("[{Agent}] Completed successfully. Run #{Count}", Name, State.RunCount);
        }
        catch (Exception ex)
        {
            State.Status = AgentStatus.Error;
            State.ErrorCount++;
            State.LastError = ex.Message;
            State.LastRunAt = DateTime.UtcNow;
            State.NextRunAt = DateTime.UtcNow.Add(Interval);
            _logger.LogError(ex, "[{Agent}] Failed: {Message}", Name, ex.Message);
        }
    }

    protected abstract Task RunAsync(CancellationToken cancellationToken);
}
