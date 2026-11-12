namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>Unlocks at one hour of every game day and locks at another.</summary>
public readonly record struct DailyLockSchedule
{
    public const int HoursPerDay = 24;

    public DailyLockSchedule(int openHour, int closeHour)
    {
        if (openHour is < 0 or >= HoursPerDay)
            throw new ArgumentOutOfRangeException(nameof(openHour), openHour, "An hour of the day is 0 to 23.");
        if (closeHour is < 0 or >= HoursPerDay)
            throw new ArgumentOutOfRangeException(nameof(closeHour), closeHour, "An hour of the day is 0 to 23.");
        if (openHour == closeHour)
            throw new ArgumentException($"A schedule that opens and closes at {openHour} never says which.", nameof(closeHour));

        OpenHour = openHour;
        CloseHour = closeHour;
    }

    public int OpenHour { get; }

    public int CloseHour { get; }

    /// <summary>The last opening or closing at or before <paramref name="gameHours"/>, counted in game hours since the game started.</summary>
    public LockScheduleChange LastChange(double gameHours)
    {
        var dayStart = Math.Floor(gameHours / HoursPerDay) * HoursPerDay;
        var opened = Latest(dayStart + OpenHour, gameHours);
        var closed = Latest(dayStart + CloseHour, gameHours);
        return closed > opened ? new LockScheduleChange(closed, IsLocked: true) : new LockScheduleChange(opened, IsLocked: false);
    }

    private static double Latest(double today, double gameHours) => today <= gameHours ? today : today - HoursPerDay;
}

/// <summary>A schedule's opening or closing: when, in game hours since the game started, and the lock it leaves.</summary>
public readonly record struct LockScheduleChange(double At, bool IsLocked);
