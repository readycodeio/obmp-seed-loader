using ReadyM.SDK.Attributes;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>The seed loader's settings, from <c>config.json</c> in the mod's server folder.</summary>
[ModConfig]
public sealed partial class SeedLoaderConfig
{
    public DoorScheduleConfig DoorSchedule { get; set; } = new();
}

/// <summary>Whether the doors the game keeps on a schedule get it from the seed.</summary>
public sealed class DoorScheduleConfig
{
    /// <summary>False seeds every door with no schedule, and leaves the locks to the server's own logic.</summary>
    public bool Enabled { get; set; } = true;
}
