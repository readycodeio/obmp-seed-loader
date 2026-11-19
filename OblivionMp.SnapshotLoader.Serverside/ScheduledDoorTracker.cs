using ReadyM.Api.Idents;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// The schedule change each door last took, so a change is applied once and a key or a pick holds until the next. A
/// door is its ref in a dimension: each dimension's copy of a door takes the change on its own.
/// </summary>
public sealed class ScheduledDoorTracker
{
    private readonly Dictionary<(DimensionId Dimension, uint RefId), double> _takenAt = [];

    /// <summary>
    /// True when the door has not taken this change yet; it is then taken for the door and for the other door of its
    /// load door pair in the same dimension, which shares the lock. A <paramref name="linkedRefId"/> of 0 is a door
    /// with no pair.
    /// </summary>
    public bool TryTake(DimensionId dimension, uint refId, uint linkedRefId, LockScheduleChange change)
    {
        if (_takenAt.TryGetValue((dimension, refId), out var takenAt) && takenAt == change.At)
            return false;

        _takenAt[(dimension, refId)] = change.At;
        if (linkedRefId != 0)
            _takenAt[(dimension, linkedRefId)] = change.At;

        return true;
    }

    /// <summary>Forgets the doors of every dimension but these, so a dimension that is gone leaves nothing behind.</summary>
    public void KeepOnly(IReadOnlySet<DimensionId> dimensions)
    {
        foreach (var key in _takenAt.Keys.ToList())
        {
            if (!dimensions.Contains(key.Dimension))
                _takenAt.Remove(key);
        }
    }
}
