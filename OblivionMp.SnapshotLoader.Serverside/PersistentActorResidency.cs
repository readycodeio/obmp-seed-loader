using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Common.Archetypes;
using OblivionMP.SDK.Serverside.World;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Makes a discovered npc or creature a resident when the seed lists it as persistent, so it stays on the server when
/// its cell leaves memory. An actor walks, so it is looked up among the persistent actors of the whole seed and not
/// in the cell that reported it.
/// </summary>
[Service]
public sealed partial class PersistentActorResidency(
    WorldSnapshot snapshot,
    WorldRefs worldRefs,
    IEntities entities,
    ILogger logger
)
{
    private void Start()
        => worldRefs.OnActorDiscovered += OnActorDiscovered;

    private void Stop()
        => worldRefs.OnActorDiscovered -= OnActorDiscovered;

    private void OnActorDiscovered(Npc npc)
    {
        if (!snapshot.PersistentActors.Contains(npc.RefId))
            return;

        entities.MakeResident(npc);
        logger.LogInformation("Actor {RefId:X8} is persistent in the seed, it became a resident", npc.RefId);
    }
}
