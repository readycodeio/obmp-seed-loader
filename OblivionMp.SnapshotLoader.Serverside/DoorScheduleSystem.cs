using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Common.Archetypes;
using OblivionMP.SDK.Common.Archetypes.Mixins;
using OblivionMP.SDK.Common.Events;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion.Rpc;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Server.Dimensions;
using ReadyM.SDK.Server.Entities;
using AreaTime = OblivionMP.SDK.Common.Archetypes.Mixins.Time;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Unlocks the doors the seed lists as scheduled at the configured opening hour and locks them at the closing hour,
/// by each door's own game clock. Off in the config, it leaves every door to the server's own logic. Every dimension's
/// copy of a door follows the schedule on its own, by its own area's clock.
/// </summary>
[Service]
public sealed partial class DoorScheduleSystem(
    SeedLoaderConfig config,
    WorldSnapshot snapshot,
    IEntities entities,
    IDimensions dimensions,
    INetworkedEvents events,
    ILogger logger
)
{
    private const float ScanIntervalSeconds = 1.0f;

    private readonly ScheduledDoorTracker _tracker = new();
    private readonly Dictionary<(DimensionId Dimension, uint RefId), Door> _doorByKey = [];
    private readonly HashSet<DimensionId> _withDoors = [];
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
        _doorByKey.Clear();
        _withDoors.Clear();
        foreach (var door in entities.Query<Door>())
        {
            if (!snapshot.ScheduledDoors.Contains(door.RefId))
                continue;

            var dimension = dimensions.GetEntityDimension(EntityHandle.Of(door));
            _doorByKey[(dimension, door.RefId)] = door;
            _withDoors.Add(dimension);
        }

        _tracker.KeepOnly(_withDoors);
        foreach (var ((dimension, _), door) in _doorByKey)
            Follow(schedule, dimension, door);
    }

    private void Follow(DailyLockSchedule schedule, DimensionId dimension, Door door)
    {
        var linkedRefId = EntityHandle.Of(door).As<DoorwaySeedOrigin>().LinkedRefId;
        if (LatestGameHours(dimension, door, linkedRefId) is not { } gameHours)
            return;

        var change = schedule.LastChange(gameHours);
        if (!_tracker.TryTake(dimension, door.RefId, linkedRefId, change))
            return;

        if (EntityHandle.Of(door).As<LockState>().IsLocked == change.IsLocked)
            return;

        // No subject: the ref names the door, so the event goes in the door's dimension
        events.RaiseIn(dimension, new LockChangeEvent { RefId = door.RefId, IsLocked = change.IsLocked, Source = LockChangeSource.Script });
        logger.LogInformation("Door {RefId:X8} in {Dimension} {Change} by its schedule", door.RefId, dimension, change.IsLocked ? "locked" : "unlocked");
    }

    // An area nobody is in keeps the time it had when the last player left, so a load door pair goes by the later clock
    private double? LatestGameHours(DimensionId dimension, Door door, uint linkedRefId)
    {
        var own = GameHours(dimension, door);
        var linked = linkedRefId != 0 && _doorByKey.TryGetValue((dimension, linkedRefId), out var partner)
            ? GameHours(dimension, partner)
            : null;
        return own is { } a && linked is { } b ? Math.Max(a, b) : own ?? linked;
    }

    private double? GameHours(DimensionId dimension, Door door)
    {
        if (!entities.TryGetScope(door, out Cell cell) || !entities.TryLookup(cell.FullCellId.AreaId, dimension, out Area area))
            return null;

        var time = area.As<AreaTime>();
        // Zero is an area no player has sent a clock for yet
        if (time.GameDaysPassed == 0 && time.GameHour == 0)
            return null;

        return Math.Floor(time.GameDaysPassed) * DailyLockSchedule.HoursPerDay + time.GameHour;
    }
}
