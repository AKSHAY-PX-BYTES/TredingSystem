using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Agent.Core;
using TradingSystem.Agent.Models;

namespace TradingSystem.Api.Controllers;

[Authorize]
[ApiController]
[Route("agents")]
[Produces("application/json")]
public class AgentController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly ILogger<AgentController> _logger;

    public AgentController(IAgentOrchestrator orchestrator, ILogger<AgentController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get full agent dashboard data (all agent states, signals, sentiments, stats)
    /// </summary>
    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        var dashboard = _orchestrator.GetDashboard();
        return Ok(new { success = true, data = dashboard, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get all agent states
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var states = _orchestrator.GetAllStates();
        return Ok(new { success = true, data = states, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get recent AI-generated signals
    /// </summary>
    [HttpGet("signals")]
    public IActionResult GetSignals([FromQuery] int count = 20)
    {
        var signals = _orchestrator.GetRecentSignals(Math.Min(count, 100));
        return Ok(new { success = true, data = signals, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get recent sentiment scores
    /// </summary>
    [HttpGet("sentiments")]
    public IActionResult GetSentiments([FromQuery] int count = 20)
    {
        var sentiments = _orchestrator.GetRecentSentiments(Math.Min(count, 100));
        return Ok(new { success = true, data = sentiments, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get system stats
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var stats = _orchestrator.GetStats();
        return Ok(new { success = true, data = stats, timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Manually trigger an agent (Admin only)
    /// </summary>
    [HttpPost("trigger/{agentName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TriggerAgent(string agentName)
    {
        _logger.LogInformation("Manual trigger requested for agent: {Agent}", agentName);
        await _orchestrator.TriggerAgentAsync(agentName);
        return Ok(new { success = true, message = $"Agent '{agentName}' triggered successfully" });
    }

    /// <summary>
    /// Enable/disable an agent (Admin only)
    /// </summary>
    [HttpPost("toggle/{agentName}")]
    [Authorize(Roles = "Admin")]
    public IActionResult ToggleAgent(string agentName, [FromQuery] bool enabled)
    {
        _orchestrator.EnableAgent(agentName, enabled);
        return Ok(new { success = true, message = $"Agent '{agentName}' {(enabled ? "enabled" : "disabled")}" });
    }
}
