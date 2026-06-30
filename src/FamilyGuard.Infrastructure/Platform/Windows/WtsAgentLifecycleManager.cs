using System.Diagnostics;
using System.Collections.Concurrent;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Manages per-user FamilyGuard.Agent processes. In production, this would use
/// WTSQueryUserToken + CreateProcessAsUser to launch agents in user sessions.
/// This implementation uses a simpler Process.Start approach suitable for
/// same-session testing and development.
/// </summary>
public sealed class WtsAgentLifecycleManager : IAgentLifecycleManager
{
    private readonly string _agentExePath;
    private readonly ILogger<WtsAgentLifecycleManager> _logger;
    private readonly ConcurrentDictionary<int, Process> _agents = new();

    public WtsAgentLifecycleManager(string agentExePath, ILogger<WtsAgentLifecycleManager> logger)
    {
        _agentExePath = agentExePath;
        _logger = logger;
    }

    public void LaunchAgent(SessionId sessionId)
    {
        if (_agents.ContainsKey(sessionId.Value))
        {
            _logger.LogDebug("Agent already running for session {SessionId}", sessionId.Value);
            return;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _agentExePath,
                    Arguments = $"--session-id {sessionId.Value}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (_, _) =>
            {
                _logger.LogWarning("Agent for session {SessionId} exited (code {ExitCode})",
                    sessionId.Value, process.ExitCode);
                _agents.TryRemove(sessionId.Value, out _);
            };

            process.Start();
            _agents[sessionId.Value] = process;

            _logger.LogInformation("Launched agent for session {SessionId} (PID {Pid})",
                sessionId.Value, process.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch agent for session {SessionId}", sessionId.Value);
        }
    }

    public void StopAgent(SessionId sessionId)
    {
        if (!_agents.TryRemove(sessionId.Value, out var process))
            return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            _logger.LogInformation("Stopped agent for session {SessionId}", sessionId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping agent for session {SessionId}", sessionId.Value);
        }
        finally
        {
            process.Dispose();
        }
    }

    public bool IsAgentRunning(SessionId sessionId)
    {
        if (!_agents.TryGetValue(sessionId.Value, out var process))
            return false;

        if (process.HasExited)
        {
            _agents.TryRemove(sessionId.Value, out _);
            return false;
        }

        return true;
    }

    public IReadOnlyList<SessionId> GetRunningAgentSessions()
    {
        // Clean up exited processes
        foreach (var kvp in _agents)
        {
            if (kvp.Value.HasExited)
                _agents.TryRemove(kvp.Key, out _);
        }

        return _agents.Keys.Select(id => new SessionId(id)).ToList();
    }
}
