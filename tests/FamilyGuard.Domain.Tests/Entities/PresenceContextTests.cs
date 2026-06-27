using FluentAssertions;
using Xunit;
using FamilyGuard.Domain.Entities;
using FamilyGuard.Domain.Enums;

namespace FamilyGuard.Domain.Tests.Entities;

public class PresenceContextTests
{
    [Fact]
    public void NewContext_DefaultsToUnknown()
    {
        var context = new PresenceContext();

        context.State.Should().Be(PresenceState.Unknown);
    }

    [Fact]
    public void NewContext_HasZeroInactiveSeconds()
    {
        var context = new PresenceContext();

        context.InactiveSeconds.Should().Be(0);
    }

    [Fact]
    public void UpdateState_ChangesState()
    {
        var context = new PresenceContext();
        var now = DateTimeOffset.UtcNow;

        context.UpdateState(PresenceState.Present, now);

        context.State.Should().Be(PresenceState.Present);
        context.LastStateChangeAt.Should().Be(now);
    }

    [Fact]
    public void UpdateState_TracksLastActivityAt()
    {
        var context = new PresenceContext();
        var activityTime = DateTimeOffset.UtcNow;

        context.RecordActivity(activityTime);

        context.LastActivityAt.Should().Be(activityTime);
    }

    [Fact]
    public void RecordActivity_UpdatesInactiveSeconds()
    {
        var context = new PresenceContext();
        var activityTime = DateTimeOffset.UtcNow.AddSeconds(-30);
        var now = DateTimeOffset.UtcNow;

        context.RecordActivity(activityTime);

        context.GetInactiveSeconds(now).Should().BeApproximately(30, 1);
    }

    [Fact]
    public void UpdateState_SameState_DoesNotChangeTimestamp()
    {
        var context = new PresenceContext();
        var first = DateTimeOffset.UtcNow;
        var second = first.AddSeconds(10);

        context.UpdateState(PresenceState.Present, first);
        context.UpdateState(PresenceState.Present, second);

        context.LastStateChangeAt.Should().Be(first);
    }

    [Fact]
    public void UpdateState_DifferentState_UpdatesTimestamp()
    {
        var context = new PresenceContext();
        var first = DateTimeOffset.UtcNow;
        var second = first.AddSeconds(10);

        context.UpdateState(PresenceState.Present, first);
        context.UpdateState(PresenceState.Away, second);

        context.LastStateChangeAt.Should().Be(second);
        context.State.Should().Be(PresenceState.Away);
    }

    [Fact]
    public void Context_TracksSessionLocked()
    {
        var context = new PresenceContext();

        context.SessionLocked = true;

        context.SessionLocked.Should().BeTrue();
    }

    [Fact]
    public void Context_TracksMicUnmuted()
    {
        var context = new PresenceContext();

        context.MicUnmuted = true;

        context.MicUnmuted.Should().BeTrue();
    }
}
