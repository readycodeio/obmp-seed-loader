using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Common.Archetypes.Mixins;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Server.Events;
using ReadyM.Native.Container;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Tells every client which plugins the world was seeded from, in load order, on every start. The order is not
/// saved with the world, the seed is the one place it comes from.
/// </summary>
[Service]
public sealed partial class SnapshotLoadOrder(WorldSnapshot snapshot, IEntities entities, IServerEvents events, ILogger logger)
{
    private void Start()
        => events.OnWorldCreated += OnWorldCreated;

    private void Stop()
        => events.OnWorldCreated -= OnWorldCreated;

    private void OnWorldCreated(ReadyM.SDK.Archetypes.Core.World created)
    {
        var world = entities.World;
        while (world.PluginsCount > 0)
            world.RemoveAtPlugins(world.PluginsCount - 1);

        foreach (var plugin in snapshot.LoadOrder)
            world.AddPlugins(new NativeString256(plugin, useWide: false));

        logger.LogInformation("The world's load order is {LoadOrder}", string.Join(", ", snapshot.LoadOrder));
    }
}
