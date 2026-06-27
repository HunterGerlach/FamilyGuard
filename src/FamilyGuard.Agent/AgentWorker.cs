using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Agent;

/// <summary>
/// Per-user agent worker. Runs the presence monitor loop, evaluates policies,
/// and executes actions (e.g., mute mic) when conditions are met.
/// </summary>
public sealed class AgentWorker : BackgroundService
{
    private readonly EvaluatePresenceUseCase _evaluatePresence;
    private readonly EvaluatePolicyUseCase _evaluatePolicy;
    private readonly MuteMicrophoneUseCase _muteMicrophone;
    private readonly PresenceStateMachine _stateMachine;
    private readonly ISettingsRepository _settings;
    private readonly IEventStore _eventStore;
    private readonly ILogger<AgentWorker> _logger;

    private readonly string _windowsUser;
    private readonly SessionId _sessionId;

    // V1 ships with one rule; loaded from settings in Phase 1
    private readonly PolicyRule[] _rules;

    public AgentWorker(
        EvaluatePresenceUseCase evaluatePresence,
        EvaluatePolicyUseCase evaluatePolicy,
        MuteMicrophoneUseCase muteMicrophone,
        PresenceStateMachine stateMachine,
        ISettingsRepository settings,
        IEventStore eventStore,
        ILogger<AgentWorker> logger)
    {
        _evaluatePresence = evaluatePresence;
        _evaluatePolicy = evaluatePolicy;
        _muteMicrophone = muteMicrophone;
        _stateMachine = stateMachine;
        _settings = settings;
        _eventStore = eventStore;
        _logger = logger;

        _windowsUser = Environment.UserName;
        _sessionId = new SessionId(0); // Overridden by --session-id arg in production

        _rules =
        [
            new PolicyRule(
                id: "mute_unattended_microphone",
                name: "Mute unattended microphone",
                enabled: true,
                conditions: [PolicyCondition.MicUnmuted, PolicyCondition.PresenceAway],
                actions: [new PolicyAction(PolicyActionType.MuteMicrophone)])
        ];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "FamilyGuard.Agent starting for user {User}, session {SessionId}",
            _windowsUser, _sessionId.Value);

        _eventStore.Append(StructuredEvent.Create(
            EventType.AgentStarted, _windowsUser, _sessionId));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RunCycle();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent monitor loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _eventStore.Append(StructuredEvent.Create(
            EventType.AgentStopped, _windowsUser, _sessionId));

        _logger.LogInformation("FamilyGuard.Agent stopping");
    }

    private void RunCycle()
    {
        // 1. Update presence state
        _evaluatePresence.Execute();

        // 2. Evaluate policies against current state
        var results = _evaluatePolicy.Execute(_rules, _stateMachine.CurrentState, _windowsUser);

        // 3. Execute triggered actions
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
                _muteMicrophone.Execute(_windowsUser, _sessionId, rule.Id);
                break;

            case PolicyActionType.NotifyChild:
                _logger.LogInformation("Policy {PolicyId}: {Message}", rule.Id, action.Message);
                break;

            case PolicyActionType.LogEvent:
                _eventStore.Append(StructuredEvent.Create(
                    EventType.PolicyEnabled, _windowsUser, _sessionId, policyId: rule.Id));
                break;
        }
    }
}
