using Shouldly;
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

        context.State.ShouldBe(PresenceState.Unknown);
    }

    [Fact]
    public void NewContext_HasZeroInactiveSeconds()
    {
        var context = new PresenceContext();

        context.InactiveSeconds.ShouldBe(0);
    }

    [Fact]
    public void UpdateState_ChangesState()
    {
        var context = new PresenceContext();
        var now = DateTimeOffset.UtcNow;

        context.UpdateState(PresenceState.Present, now);

        context.State.ShouldBe(PresenceState.Present);
        context.LastStateChangeAt.ShouldBe(now);
    }

    [Fact]
    public void UpdateState_TracksLastActivityAt()
    {
        var context = new PresenceContext();
        var activityTime = DateTimeOffset.UtcNow;

        context.RecordActivity(activityTime);

        context.LastActivityAt.ShouldBe(activityTime);
    }

    [Fact]
    public void RecordActivity_UpdatesInactiveSeconds()
    {
        var context = new PresenceContext();
        var activityTime = DateTimeOffset.UtcNow.AddSeconds(-30);
        var now = DateTimeOffset.UtcNow;

        context.RecordActivity(activityTime);

        ((double)context.GetInactiveSeconds(now)).ShouldBe(30, tolerance: 1);
    }

    [Fact]
    public void UpdateState_SameState_DoesNotChangeTimestamp()
    {
        var context = new PresenceContext();
        var first = DateTimeOffset.UtcNow;
        var second = first.AddSeconds(10);

        context.UpdateState(PresenceState.Present, first);
        context.UpdateState(PresenceState.Present, second);

        context.LastStateChangeAt.ShouldBe(first);
    }

    [Fact]
    public void UpdateState_DifferentState_UpdatesTimestamp()
    {
        var context = new PresenceContext();
        var first = DateTimeOffset.UtcNow;
        var second = first.AddSeconds(10);

        context.UpdateState(PresenceState.Present, first);
        context.UpdateState(PresenceState.Away, second);

        context.LastStateChangeAt.ShouldBe(second);
        context.State.ShouldBe(PresenceState.Away);
    }

    [Fact]
    public void Context_TracksSessionLocked()
    {
        var context = new PresenceContext();

        context.SessionLocked = true;

        context.SessionLocked.ShouldBeTrue();
    }

    [Fact]
    public void Context_TracksMicUnmuted()
    {
        var context = new PresenceContext();

        context.MicUnmuted = true;

        context.MicUnmuted.ShouldBeTrue();
    }
}
