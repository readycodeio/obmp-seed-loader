using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Common.Archetypes;
using OblivionMP.SDK.Common.Archetypes.Mixins;
using OblivionMP.SDK.Serverside.Archetypes;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server.Entities;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Creates the entity of one seeded ref: furniture, a container, a plant, a door, an activator or an item. In a cell
/// it is that cell's content. With no cell it is in no scope, for the caller to make a resident.
/// </summary>
public sealed class SnapshotEntitySeeder(WorldSnapshot snapshot, SeedLoaderConfig config, IEntities entities, ILogger logger)
{
    private const float NoInstanceOverride = -1f;

    public static bool IsSeeded(RefRecord reference) => SeededFormTypes.IsSeeded(reference.BaseType);

    /// <summary>False for a ref that gets no entity: a plant that gives no ingredient.</summary>
    public bool TrySeed(RefRecord reference, FullCellId cellId, Cell? cell, out EntityHandle entity)
    {
        switch (reference.BaseType)
        {
            case FormType.Furniture:
                entity = EntityHandle.Of(Create<Furniture>(cell));
                SetPlaced(entity, reference);
                return true;
            case FormType.Container:
                entity = SeedContainer(cellId, cell, reference);
                return true;
            case FormType.Flora:
                if (reference.IngredientId is null)
                {
                    logger.LogDebug("Plant {RefId:X} in {Cell} gives no ingredient, it is not seeded", reference.RefId, cellId);
                    entity = default;
                    return false;
                }

                entity = EntityHandle.Of(Create<Flora>(cell));
                SetPlaced(entity, reference);
                return true;
            case FormType.Door:
                entity = SeedDoor(cell, reference);
                return true;
            case FormType.Activator:
                entity = EntityHandle.Of(Create<WorldActivator>(cell));
                SetPlaced(entity, reference);
                return true;
            default:
                entity = SeedItem(cell, reference);
                return true;
        }
    }

    private T Create<T>(Cell? cell) where T : struct, IArchetype
        => cell is { } host ? entities.Create<T>(Scope.Of(host)) : entities.Create<T>();

    private EntityHandle SeedContainer(FullCellId cellId, Cell? cell, RefRecord reference)
    {
        var template = entities.Create<ContainerTemplate>();
        template.RefId = reference.RefId;
        template.IsOwnedByMerchant = reference.IsOwnedByMerchant == true;
        template.Respawns = reference.Respawns == true;
        template.MerchantRefId = reference.MerchantRefId ?? 0;
        template.BarterGold = reference.BarterGold ?? 0;
        foreach (var item in reference.Items ?? [])
        {
            if (item.Kind is not { } kind)
            {
                logger.LogError("Container {RefId:X} in {Cell}: base item {ItemId:X8} has no kind in the seed, it is left out", reference.RefId, cellId, item.ItemId);
                continue;
            }

            if (kind == FormType.LeveledItem)
                LeveledListSeeding.Ensure(snapshot, entities, logger, item.ItemId);

            template.AddItems(new BaseContainerItem(new FormId(item.ItemId), item.Count, kind));
        }

        // Not rolled here: no player is in the cell yet. CellRespawnSystem rolls it at the first master's level.
        var container = EntityHandle.Of(Create<Container>(cell));
        SetPlaced(container, reference);
        SeedLock(container, reference);
        return container;
    }

    private EntityHandle SeedDoor(Cell? cell, RefRecord reference)
    {
        var door = Create<Door>(cell);
        var entity = EntityHandle.Of(door);
        SetPlaced(entity, reference);
        SeedLock(entity, reference);
        var isOpenByDefault = reference.IsOpenByDefault == true;
        var seedOrigin = entity.As<DoorwaySeedOrigin>();
        seedOrigin.IsOpenByDefault = isOpenByDefault;
        seedOrigin.LinkedRefId = reference.Teleport?.DestinationRefId ?? 0;
        entity.As<Doorway>().IsOpen = isOpenByDefault;
        if (config.DoorSchedule.Enabled && reference.LockSchedule is { } changes)
        {
            var schedule = entity.As<LockSchedule>();
            schedule.IsEnabled = true;
            foreach (var change in changes)
                schedule.AddChanges(new LockScheduleChange(change.Hour, change.IsLocked));
        }

        // The other door of the pair may have been seeded and unlocked long before this cell. A door seeded with no
        // cell is seeded with the world, when nobody has unlocked anything yet
        if (cell is not null && seedOrigin.LinkedRefId != 0 && TryFindDoor(seedOrigin.LinkedRefId, out var partner))
            CopyLock(partner, door);

        return entity;
    }

    private bool TryFindDoor(uint refId, out Door door)
    {
        foreach (var candidate in entities.Query<Door>())
        {
            if (candidate.RefId == refId)
            {
                door = candidate;
                return true;
            }
        }

        door = default;
        return false;
    }

    private static void CopyLock(Door from, Door to)
    {
        var source = EntityHandle.Of(from).As<LockState>();
        var target = EntityHandle.Of(to).As<LockState>();
        target.IsLocked = source.IsLocked;
        target.Level = source.Level;
    }

    private static void SeedLock(EntityHandle holder, RefRecord reference)
    {
        if (reference.Lock is not { } lockRecord)
            return;

        var lockState = holder.As<LockState>();
        lockState.IsLocked = true;
        lockState.Level = lockRecord.Level;
        var seedOrigin = holder.As<LockSeedOrigin>();
        seedOrigin.Key = new FormId(lockRecord.KeyId ?? 0);
        seedOrigin.IsLeveled = lockRecord.IsLeveled == true;
    }

    private EntityHandle SeedItem(Cell? cell, RefRecord reference)
    {
        var item = Create<DroppedItem>(cell);
        var entity = EntityHandle.Of(item);
        SetPlaced(entity, reference);
        item.StackSize = reference.Count ?? 1;
        var extraData = entity.As<ItemExtraData>();
        extraData.Durability = NoInstanceOverride;
        extraData.Charge = NoInstanceOverride;
        var seedOrigin = entity.As<SeedOrigin>();
        seedOrigin.SeededCount = reference.Count ?? 1;
        seedOrigin.IsPersistent = reference.Persistent == true;
        return entity;
    }

    private static void SetPlaced(EntityHandle entity, RefRecord reference)
    {
        entity.As<RefIdMixin>().RefId = reference.RefId;
        entity.As<BaseIdMixin>().BaseId = new FormId(reference.BaseId);
        var transform = entity.As<Transform>();
        transform.Position = TesUnrealSpace.ToUnrealLocation(new(reference.Position.X, reference.Position.Y, reference.Position.Z));
        transform.Rotation = TesUnrealSpace.ToUnrealYawDegrees(reference.Rotation.Z);
        entity.As<Presence>().IsDisabled = reference.InitiallyDisabled == true;
    }
}
