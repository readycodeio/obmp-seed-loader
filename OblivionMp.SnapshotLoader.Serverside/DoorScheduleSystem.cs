using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Common.Archetypes;
using OblivionMP.SDK.Common.Archetypes.Mixins;
using OblivionMP.SDK.Common.Events;
using ReadyM.Relay.Common.Oblivion.Rpc;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Server.Entities;
using AreaTime = OblivionMP.SDK.Common.Archetypes.Mixins.Time;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Unlocks the doors the seed lists as scheduled at the configured opening hour and locks them at the closing hour,
/// by each door's own game clock. Off in the config, it leaves every door to the server's own logic.
/// </summary>
[Service]
public sealed partial class DoorScheduleSystem(
    SeedLoaderConfig config,
    WorldSnapshot snapshot,
    IEntities entities,
    INetworkedEvents events,
    ILogger logger
)
{
    private const float ScanIntervalSeconds = 1.0f;

    private readonly ScheduledDoorTracker _tracker = new();
    private readonly Dictionary<uint, Door> _doorByRefId = [];
    private DailyLockSchedule? _schedule;
    private float _sinceScan = ScanIntervalSeconds;

    private void Start()
    {
        var settings = config.DoorSchedule;
        if (!settings.Enabled)
        {
            logger.LogInformation("Door schedules are off in the config, the server's own logic locks the doors");
            return;
        }

        try
        {
            _schedule = new DailyLockSchedule(settings.OpenHour, settings.CloseHour);
        }
        catch (ArgumentException e)
        {
            logger.LogWarning(e, "Door schedules are off: the config's hours {OpenHour} to {CloseHour} are not a schedule", settings.OpenHour, settings.CloseHour);
            return;
        }

        logger.LogInformation("{Count} scheduled doors unlock at {OpenHour}:00 and lock at {CloseHour}:00", snapshot.ScheduledDoors.Count, settings.OpenHour, settings.CloseHour);
    }

    private void Update()
    {
        if (_schedule is not { } schedule || snapshot.ScheduledDoors.Count == 0)
            return;

        _sinceScan += Time.DeltaTime;
        if (_sinceScan < ScanIntervalSeconds)
            return;

        _sinceScan = 0.0f;
        _doorByRefId.Clear();
        foreach (var door in entities.Query<Door>())
        {
            if (snapshot.ScheduledDoors.Contains(door.RefId))
                _doorByRefId[door.RefId] = door;
        }

        foreach (var door in _doorByRefId.Values)
            Follow(schedule, door);
    }

    private void Follow(DailyLockSchedule schedule, Door door)
    {
        var linkedRefId = EntityHandle.Of(door).As<DoorwaySeedOrigin>().LinkedRefId;
        if (LatestGameHours(door, linkedRefId) is not { } gameHours)
            return;

        var change = schedule.LastChange(gameHours);
        if (!_tracker.TryTake(door.RefId, linkedRefId, change))
            return;

        if (EntityHandle.Of(door).As<LockState>().IsLocked == change.IsLocked)
            return;

        events.Raise(new LockChangeEvent { RefId = door.RefId, IsLocked = change.IsLocked, Source = LockChangeSource.Script });
        logger.LogInformation("Door {RefId:X8} {Change} by its schedule", door.RefId, change.IsLocked ? "locked" : "unlocked");
    }

    // An area nobody is in keeps the time it had when the last player left, so a load door pair goes by the later clock
    private double? LatestGameHours(Door door, uint linkedRefId)
    {
        var own = GameHours(door);
        var linked = linkedRefId != 0 && _doorByRefId.TryGetValue(linkedRefId, out var partner) ? GameHours(partner) : null;
        return own is { } a && linked is { } b ? Math.Max(a, b) : own ?? linked;
    }

    private double? GameHours(Door door)
    {
        if (!entities.TryGetScope(door, out Cell cell) || !entities.TryLookup(cell.FullCellId.AreaId, out Area area))
            return null;

        var time = area.As<AreaTime>();
        // Zero is an area no player has sent a clock for yet
        if (time.GameDaysPassed == 0 && time.GameHour == 0)
            return null;

        return Math.Floor(time.GameDaysPassed) * DailyLockSchedule.HoursPerDay + time.GameHour;
    }
}
