namespace OblivionMp.SnapshotLoader.Serverside.Tests;

public sealed class ScheduledDoorTrackerTests
{
    private const uint Door = 0x2C23A;
    private const uint Partner = 0x2C3DB;
    private const uint OtherDoor = 0x2C3DD;

    private static readonly LockScheduleChange Opened = new(130, IsLocked: false);
    private static readonly LockScheduleChange Closed = new(142, IsLocked: true);

    [Fact]
    public void A_change_is_taken_once_so_a_key_or_a_pick_holds_until_the_next_one()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Door, Partner, Opened));
        Assert.False(tracker.TryTake(Door, Partner, Opened));
        Assert.True(tracker.TryTake(Door, Partner, Closed));
        Assert.False(tracker.TryTake(Door, Partner, Closed));
    }

    [Fact]
    public void Taking_a_change_for_one_door_of_a_pair_takes_it_for_the_other()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Door, Partner, Opened));
        Assert.False(tracker.TryTake(Partner, Door, Opened));
        Assert.True(tracker.TryTake(Partner, Door, Closed));
        Assert.False(tracker.TryTake(Door, Partner, Closed));
    }

    [Fact]
    public void Doors_that_are_not_a_pair_take_the_same_change_each()
    {
        var tracker = new ScheduledDoorTracker();

        Assert.True(tracker.TryTake(Door, 0, Opened));
        Assert.True(tracker.TryTake(OtherDoor, 0, Opened));
        Assert.False(tracker.TryTake(OtherDoor, 0, Opened));
    }
}
