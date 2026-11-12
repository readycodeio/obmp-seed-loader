namespace OblivionMp.SnapshotLoader.Serverside.Tests;

public sealed class DailyLockScheduleTests
{
    private const int Day = 24;
    private static readonly DailyLockSchedule Shop = new(10, 22);

    [Theory]
    [InlineData(10.0, 10.0, false)]
    [InlineData(15.5, 10.0, false)]
    [InlineData(21.99, 10.0, false)]
    [InlineData(22.0, 22.0, true)]
    [InlineData(23.9, 22.0, true)]
    public void During_a_day_the_last_change_is_that_days_opening_or_closing(double hour, double changedAt, bool isLocked)
    {
        var change = Shop.LastChange(5 * Day + hour);

        Assert.Equal(new LockScheduleChange(5 * Day + changedAt, isLocked), change);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.0)]
    [InlineData(9.99)]
    public void Before_opening_the_door_is_still_closed_from_the_evening_before(double hour)
    {
        Assert.Equal(new LockScheduleChange(4 * Day + 22, IsLocked: true), Shop.LastChange(5 * Day + hour));
    }

    [Fact]
    public void A_wait_from_before_opening_to_after_closing_ends_closed()
    {
        var before = Shop.LastChange(4 * Day + 9);
        var after = Shop.LastChange(4 * Day + 23);

        Assert.Equal(new LockScheduleChange(3 * Day + 22, IsLocked: true), before);
        Assert.Equal(new LockScheduleChange(4 * Day + 22, IsLocked: true), after);
    }

    [Fact]
    public void The_first_morning_of_the_game_counts_back_to_a_closing_before_it_began()
    {
        Assert.Equal(new LockScheduleChange(-2, IsLocked: true), Shop.LastChange(8.0));
    }

    [Theory]
    [InlineData(23.0, 22.0, false)]
    [InlineData(27.0, 22.0, false)]
    [InlineData(30.0, 30.0, true)]
    [InlineData(45.0, 30.0, true)]
    public void A_night_schedule_stays_open_across_midnight(double gameHours, double changedAt, bool isLocked)
    {
        var tavern = new DailyLockSchedule(22, 6);

        Assert.Equal(new LockScheduleChange(changedAt, isLocked), tavern.LastChange(gameHours));
    }

    [Theory]
    [InlineData(-1, 22)]
    [InlineData(24, 22)]
    [InlineData(10, -1)]
    [InlineData(10, 24)]
    public void An_hour_outside_the_day_is_refused(int openHour, int closeHour)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DailyLockSchedule(openHour, closeHour));
    }

    [Fact]
    public void Opening_and_closing_at_the_same_hour_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new DailyLockSchedule(12, 12));
    }
}
