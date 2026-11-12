namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>The schedule change each door last took, so a change is applied once and a key or a pick holds until the next.</summary>
public sealed class ScheduledDoorTracker
{
    private readonly Dictionary<uint, double> _takenAtByRefId = [];

    /// <summary>
    /// True when the door has not taken this change yet; it is then taken for the door and for the other door of its
    /// load door pair, which shares the lock. A <paramref name="linkedRefId"/> of 0 is a door with no pair.
    /// </summary>
    public bool TryTake(uint refId, uint linkedRefId, LockScheduleChange change)
    {
        if (_takenAtByRefId.TryGetValue(refId, out var takenAt) && takenAt == change.At)
            return false;

        _takenAtByRefId[refId] = change.At;
        if (linkedRefId != 0)
            _takenAtByRefId[linkedRefId] = change.At;

        return true;
    }
}
