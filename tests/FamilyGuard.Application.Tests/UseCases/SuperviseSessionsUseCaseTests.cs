using Shouldly;
using NSubstitute;
using Xunit;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Domain.ValueObjects;

namespace FamilyGuard.Application.Tests.UseCases;

public class SuperviseSessionsUseCaseTests
{
    private readonly ISessionMonitor _sessions = Substitute.For<ISessionMonitor>();
    private readonly IAgentLifecycleManager _agents = Substitute.For<IAgentLifecycleManager>();
    private readonly SuperviseSessionsUseCase _useCase;

    public SuperviseSessionsUseCaseTests()
    {
        _useCase = new SuperviseSessionsUseCase(_sessions, _agents);
    }

    [Fact]
    public void Execute_LaunchesAgentForNewSession()
    {
        _sessions.GetActiveInteractiveSessions().Returns([new SessionId(1)]);
        _agents.IsAgentRunning(new SessionId(1)).Returns(false);
        _agents.GetRunningAgentSessions().Returns([]);

        _useCase.Execute();

        _agents.Received(1).LaunchAgent(new SessionId(1));
    }

    [Fact]
    public void Execute_SkipsAlreadyRunningAgent()
    {
        _sessions.GetActiveInteractiveSessions().Returns([new SessionId(1)]);
        _agents.IsAgentRunning(new SessionId(1)).Returns(true);
        _agents.GetRunningAgentSessions().Returns([new SessionId(1)]);

        _useCase.Execute();

        _agents.DidNotReceive().LaunchAgent(Arg.Any<SessionId>());
    }

    [Fact]
    public void Execute_StopsAgentForDepartedSession()
    {
        _sessions.GetActiveInteractiveSessions().Returns([]);
        _agents.GetRunningAgentSessions().Returns([new SessionId(2)]);

        _useCase.Execute();

        _agents.Received(1).StopAgent(new SessionId(2));
    }

    [Fact]
    public void Execute_MultipleSessions_LaunchesAndStopsCorrectly()
    {
        _sessions.GetActiveInteractiveSessions().Returns([new SessionId(1), new SessionId(3)]);
        _agents.IsAgentRunning(new SessionId(1)).Returns(true);
        _agents.IsAgentRunning(new SessionId(3)).Returns(false);
        _agents.GetRunningAgentSessions().Returns([new SessionId(1), new SessionId(2)]);

        _useCase.Execute();

        _agents.DidNotReceive().LaunchAgent(new SessionId(1));
        _agents.Received(1).LaunchAgent(new SessionId(3));
        _agents.Received(1).StopAgent(new SessionId(2));
    }

    [Fact]
    public void Execute_NoSessions_NoAgents_DoesNothing()
    {
        _sessions.GetActiveInteractiveSessions().Returns([]);
        _agents.GetRunningAgentSessions().Returns([]);

        _useCase.Execute();

        _agents.DidNotReceive().LaunchAgent(Arg.Any<SessionId>());
        _agents.DidNotReceive().StopAgent(Arg.Any<SessionId>());
    }

    [Fact]
    public void ShutdownAll_StopsAllRunning()
    {
        _agents.GetRunningAgentSessions().Returns([new SessionId(1), new SessionId(2)]);

        _useCase.ShutdownAll();

        _agents.Received(1).StopAgent(new SessionId(1));
        _agents.Received(1).StopAgent(new SessionId(2));
    }
}
