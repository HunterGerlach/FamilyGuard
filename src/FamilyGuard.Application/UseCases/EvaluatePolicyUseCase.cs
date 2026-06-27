using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Application.UseCases;

public sealed class EvaluatePolicyUseCase
{
    private readonly IPolicyEngine _engine;
    private readonly IMicrophoneController _mic;

    public EvaluatePolicyUseCase(IPolicyEngine engine, IMicrophoneController mic)
    {
        _engine = engine;
        _mic = mic;
    }

    public IReadOnlyList<PolicyEvaluationResult> Execute(
        IReadOnlyList<PolicyRule> rules,
        PresenceState presenceState,
        string windowsUser)
    {
        var context = new PolicyEvaluationContext
        {
            PresenceState = presenceState,
            MicUnmuted = !_mic.IsMuted(),
            WindowsUser = windowsUser
        };

        return _engine.Evaluate(rules, context);
    }
}
