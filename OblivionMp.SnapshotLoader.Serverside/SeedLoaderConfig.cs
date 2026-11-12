using ReadyM.SDK.Attributes;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>The seed loader's settings, from <c>config.json</c> in the mod's server folder.</summary>
[ModConfig]
public sealed partial class SeedLoaderConfig
{
    public DoorScheduleConfig DoorSchedule { get; set; } = new();
}

/// <summary>When the doors the game keeps on a schedule unlock and lock, in game hours.</summary>
public sealed class DoorScheduleConfig
{
    /// <summary>False leaves every door to the server's own logic.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The hour of the day the doors unlock, 0 to 23.</summary>
    public int OpenHour { get; set; } = 10;

    /// <summary>The hour of the day the doors lock, 0 to 23.</summary>
    public int CloseHour { get; set; } = 22;
}
