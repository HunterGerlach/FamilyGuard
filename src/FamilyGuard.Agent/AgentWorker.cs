using System.Diagnostics;
using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Agent;

public sealed class AgentWorker : BackgroundService
{
    private readonly EvaluatePresenceUseCase _evalPresence;
    private readonly EvaluatePolicyUseCase _evalPolicy;
    private readonly MuteMicrophoneUseCase _muteMic;
    private readonly PresenceStateMachine _stateMachine;
    private readonly IPolicyRepository _policyRepo;
    private readonly IMicrophoneController _mic;
    private readonly IEventStore _eventStore;
    private readonly INotificationSender _notifier;
    private readonly ISystemEventMonitor? _systemEvents;
    private readonly ILogger<AgentWorker> _logger;

    private readonly string _windowsUser;
    private readonly SessionId _sessionId;
    private IReadOnlyList<PolicyRule> _rules = [];
    private Process? _uiProcess;
    private DateTimeOffset _lastUiLaunchAttempt = DateTimeOffset.MinValue;
    private static readonly TimeSpan UiRestartCooldown = TimeSpan.FromSeconds(30);

    // Exposed for UI binding
    public PresenceState CurrentPresence => _stateMachine.CurrentState;
    public double InactiveSeconds => _stateMachine.InactiveSeconds;
    public bool MicMuted => _mic.IsMuted();
    public string? MicDeviceName => _mic.GetDefaultCommunicationsMicrophone()?.Name;
    public string? LastAction { get; private set; }

    public event Action? StateUpdated;

    public AgentWorker(
        EvaluatePresenceUseCase evalPresence,
        EvaluatePolicyUseCase evalPolicy,
        MuteMicrophoneUseCase muteMic,
        PresenceStateMachine stateMachine,
        IPolicyRepository policyRepo,
        IMicrophoneController mic,
        IEventStore eventStore,
        INotificationSender notifier,
        ILogger<AgentWorker> logger,
        ISystemEventMonitor? systemEvents = null)
    {
        _evalPresence = evalPresence;
        _evalPolicy = evalPolicy;
        _muteMic = muteMic;
        _stateMachine = stateMachine;
        _policyRepo = policyRepo;
        _mic = mic;
        _eventStore = eventStore;
        _notifier = notifier;
        _systemEvents = systemEvents;
        _logger = logger;

        _windowsUser = Environment.UserName;
        _sessionId = new SessionId(0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FamilyGuard.Agent starting for user {User}", _windowsUser);

        _eventStore.Append(StructuredEvent.Create(
            EventType.AgentStarted, _windowsUser, _sessionId));

        // Wire system events (sleep/wake, lock/unlock)
        if (_systemEvents is not null)
        {
            _systemEvents.SessionLocked += () => _stateMachine.SetSessionLocked(true);
            _systemEvents.SessionUnlocked += () => _stateMachine.SetSessionLocked(false);
            _systemEvents.SystemSuspending += () => _stateMachine.SetSessionLocked(true);
            _systemEvents.SystemResuming += () =>
            {
                _stateMachine.SetSessionLocked(false);
                _logger.LogInformation("System resumed — re-evaluating mic state");
            };
            _systemEvents.DefaultMicrophoneChanged += () =>
            {
                _logger.LogInformation("Default microphone changed — re-acquiring endpoint");
                _eventStore.Append(StructuredEvent.Create(
                    EventType.DeviceChanged, _windowsUser, _sessionId));
            };
            _systemEvents.Start();
        }

        // Load policy rules from database
        _rules = _policyRepo.LoadAll();
        _logger.LogInformation("Loaded {Count} policy rules", _rules.Count);

        // Launch tray UI process
        LaunchUiProcess();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RunCycle();
                EnsureUiRunning();
                StateUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent monitor loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        StopUiProcess();

        _eventStore.Append(StructuredEvent.Create(
            EventType.AgentStopped, _windowsUser, _sessionId));

        _logger.LogInformation("FamilyGuard.Agent stopping");
    }

    private void RunCycle()
    {
        _evalPresence.Execute();

        var results = _evalPolicy.Execute(_rules, _stateMachine.CurrentState, _windowsUser);

        foreach (var result in results)
        {
            foreach (var action in result.Actions)
            {
                ExecuteAction(action, result.Rule);
            }
        }
    }

    private void ExecuteAction(PolicyAction action, PolicyRule rule)
    {
        switch (action.ActionType)
        {
            case PolicyActionType.MuteMicrophone:
                _muteMic.Execute(_windowsUser, _sessionId, rule.Id);
                LastAction = $"Mic auto-muted at {DateTimeOffset.UtcNow:HH:mm:ss}";
                break;

            case PolicyActionType.NotifyChild:
                _notifier.ShowNotification("DAD", action.Message ?? "Policy action triggered.");
                break;

            case PolicyActionType.LogEvent:
                _eventStore.Append(StructuredEvent.Create(
                    EventType.PolicyEnabled, _windowsUser, _sessionId, policyId: rule.Id));
                break;
        }
    }

    public void ReloadRules()
    {
        _rules = _policyRepo.LoadAll();
        _logger.LogInformation("Reloaded {Count} policy rules", _rules.Count);
    }

    private void LaunchUiProcess()
    {
        _lastUiLaunchAttempt = DateTimeOffset.UtcNow;
        var uiExePath = Path.Combine(AppContext.BaseDirectory, "FamilyGuard.UI.exe");
        if (!File.Exists(uiExePath))
        {
            _logger.LogWarning("UI executable not found at {Path} — tray icon unavailable", uiExePath);
            return;
        }

        try
        {
            _uiProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = uiExePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            _uiProcess.Start();
            _logger.LogInformation("Launched UI process (PID {Pid})", _uiProcess.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch UI process");
        }
    }

    private void EnsureUiRunning()
    {
        if (_uiProcess is not null && !_uiProcess.HasExited)
            return;

        if (DateTimeOffset.UtcNow - _lastUiLaunchAttempt < UiRestartCooldown)
            return;

        _logger.LogInformation("UI process not running — restarting");
        LaunchUiProcess();
    }

    private void StopUiProcess()
    {
        if (_uiProcess is null || _uiProcess.HasExited)
            return;

        try
        {
            _uiProcess.Kill(entireProcessTree: true);
            _uiProcess.WaitForExit(3000);
            _logger.LogInformation("Stopped UI process");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping UI process");
        }
        finally
        {
            _uiProcess?.Dispose();
            _uiProcess = null;
        }
    }
}
