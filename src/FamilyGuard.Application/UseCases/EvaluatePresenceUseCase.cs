using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.StateMachine;

namespace FamilyGuard.Application.UseCases;

public sealed class EvaluatePresenceUseCase
{
    private readonly IPresenceDetector _detector;
    private readonly PresenceStateMachine _stateMachine;
    public EvaluatePresenceUseCase(
        IPresenceDetector detector,
        PresenceStateMachine stateMachine)
    {
        _detector = detector;
        _stateMachine = stateMachine;
    }

    public void Execute()
    {
        var idleTime = _detector.GetIdleTime();
        var controllerActive = _detector.IsControllerActive();

        if (controllerActive)
        {
            _stateMachine.RecordControllerActivity();
            return;
        }

        if (idleTime.TotalSeconds < 1)
        {
            _stateMachine.RecordActivity();
            return;
        }

        _stateMachine.Evaluate();
    }
}
