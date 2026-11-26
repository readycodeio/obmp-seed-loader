using ReadyM.Api.Idents;

namespace OblivionMp.SnapshotLoader.Serverside.Tests;

public sealed class ScheduledDoorTrackerTests
{
    private const uint Door = 0x2C23A;
    private const uint Partner = 0x2C3DB;
    private const uint OtherDoor = 0x2C3DD;

    private static readonly DimensionId Main = DimensionId.Main;
    private static readonly DimensionId Instance = new(3);

    private static readonly LockScheduleChange Opened = new(130, IsLocked: false);
    private static readonly LockScheduleChange Closed = new(142, IsLocked: true);

    [Fact]
    public void A_change_is_taken_once_so_a_key_or_a_pick_holds_until_the_next_one()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Main, Door, Partner, Opened));
        Assert.False(tracker.TryTake(Main, Door, Partner, Opened));
        Assert.True(tracker.TryTake(Main, Door, Partner, Closed));
        Assert.False(tracker.TryTake(Main, Door, Partner, Closed));
    }

    [Fact]
    public void Taking_a_change_for_one_door_of_a_pair_takes_it_for_the_other()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Main, Door, Partner, Opened));
        Assert.False(tracker.TryTake(Main, Partner, Door, Opened));
        Assert.True(tracker.TryTake(Main, Partner, Door, Closed));
        Assert.False(tracker.TryTake(Main, Door, Partner, Closed));
    }

    [Fact]
    public void Doors_that_are_not_a_pair_take_the_same_change_each()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Main, Door, 0, Opened));
        Assert.True(tracker.TryTake(Main, OtherDoor, 0, Opened));
        Assert.False(tracker.TryTake(Main, OtherDoor, 0, Opened));
    }

    /// <summary>Each dimension has its own copy of a door, so each copy takes the change once.</summary>
    [Fact]
    public void The_same_door_in_two_dimensions_takes_the_same_change_once_each()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Main, Door, 0, Opened));
        Assert.True(tracker.TryTake(Instance, Door, 0, Opened));
        Assert.False(tracker.TryTake(Instance, Door, 0, Opened));
    }

    [Fact]
    public void A_pair_is_taken_together_only_in_its_own_dimension()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Main, Door, Partner, Opened));
        Assert.True(tracker.TryTake(Instance, Partner, Door, Opened));
    }

    [Fact]
    public void A_dimension_that_is_gone_is_forgotten()
    {
        var tracker = new ScheduledDoorTracker();
        tracker.TryTake(Main, Door, 0, Opened);
        tracker.TryTake(Instance, Door, 0, Opened);

        tracker.KeepOnly(new HashSet<DimensionId> { Main });

        Assert.False(tracker.TryTake(Main, Door, 0, Opened));
        Assert.True(tracker.TryTake(Instance, Door, 0, Opened));
    }
}
