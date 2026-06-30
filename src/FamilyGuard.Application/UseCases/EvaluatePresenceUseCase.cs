using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.StateMachine;

namespace FamilyGuard.Application.UseCases;

public sealed class EvaluatePresenceUseCase
{
    private readonly IPresenceDetector _detector;
    private readonly PresenceStateMachine _stateMachine;
    private readonly TimeProvider _clock;

    public EvaluatePresenceUseCase(
        IPresenceDetector detector,
        PresenceStateMachine stateMachine,
        TimeProvider clock)
    {
        _detector = detector;
        _stateMachine = stateMachine;
        _clock = clock;
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
