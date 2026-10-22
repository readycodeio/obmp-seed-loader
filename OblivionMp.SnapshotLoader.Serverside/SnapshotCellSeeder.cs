using Microsoft.Extensions.Logging;
using OblivionMp.Sdk.Common.Archetypes;
using OblivionMp.Sdk.Common.Archetypes.Mixins;
using OblivionMp.Sdk.Serverside.Archetypes;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Server.Seeding;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Fills a new cell from the world snapshot: furniture, containers, plants and items. The server calls it only for a
/// cell with no save, so nothing of the cell exists yet. No player is in it: what it creates is the server's until the
/// first player enters, and containers are rolled at that player's level by <c>CellRespawnSystem</c>, which also marks
/// the cell seeded.
/// </summary>
[Service]
public sealed partial class SnapshotCellSeeder(WorldSnapshot snapshot, IEntities entities, ILogger logger)
    : ICellSeeder
{
    private const float NoInstanceOverride = -1f;

    public void Seed(Cell cell)
    {
        var cellId = cell.FullCellId;
        if (!snapshot.TryLoadCell(cellId, out var file) || !file.References.Exists(IsSeeded))
            return;

        var furniture = 0;
        var containers = 0;
        var flora = 0;
        var doors = 0;
        var items = 0;
        foreach (var reference in file.References)
        {
            if (!IsSeeded(reference))
                continue;

            if (reference.BaseType == FormType.Furniture)
            {
                SetPlaced(EntityHandle.Of(entities.Create<Furniture>(Scope.Of(cell))), reference);
                furniture++;
            }
            else if (reference.BaseType == FormType.Container)
            {
                SeedContainer(cell, reference);
                containers++;
            }
            else if (reference.BaseType == FormType.Flora)
            {
                if (reference.IngredientId is null)
                {
                    logger.LogDebug("Plant {RefId:X} in {Cell} gives no ingredient, it is not seeded", reference.RefId, cellId);
                    continue;
                }

                SetPlaced(EntityHandle.Of(entities.Create<Flora>(Scope.Of(cell))), reference);
                flora++;
            }
            else if (reference.BaseType == FormType.Door)
            {
                SeedDoor(cell, reference);
                doors++;
            }
            else
            {
                SeedItem(cell, reference);
                items++;
            }
        }

        logger.LogInformation("Seeded {Furniture} furniture, {Containers} containers, {Flora} plants, {Doors} doors and {Items} items in {Cell}", furniture, containers, flora, doors, items, cellId);
    }

    private void SeedContainer(Cell cell, RefRecord reference)
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
                logger.LogError("Container {RefId:X} in {Cell}: base item {ItemId:X8} has no kind in the seed, it is left out", reference.RefId, cell.FullCellId, item.ItemId);
                continue;
            }

            if (kind == FormType.LeveledItem)
                LeveledListSeeding.Ensure(snapshot, entities, logger, item.ItemId);

            template.AddItems(new BaseContainerItem(new FormId(item.ItemId), item.Count, kind));
        }

        // Not rolled here: no player is in the cell yet. CellRespawnSystem rolls it at the first master's level.
        var container = entities.Create<Container>(Scope.Of(cell));
        SetPlaced(EntityHandle.Of(container), reference);
        SeedLock(EntityHandle.Of(container), reference);
    }

    private void SeedDoor(Cell cell, RefRecord reference)
    {
        var door = entities.Create<Door>(Scope.Of(cell));
        SetPlaced(EntityHandle.Of(door), reference);
        SeedLock(EntityHandle.Of(door), reference);
        var isOpenByDefault = reference.IsOpenByDefault == true;
        EntityHandle.Of(door).As<DoorwaySeedOrigin>().IsOpenByDefault = isOpenByDefault;
        EntityHandle.Of(door).As<Doorway>().IsOpen = isOpenByDefault;
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

    private void SeedItem(Cell cell, RefRecord reference)
    {
        var item = entities.Create<DroppedItem>(Scope.Of(cell));
        SetPlaced(EntityHandle.Of(item), reference);
        item.StackSize = reference.Count ?? 1;
        var extraData = EntityHandle.Of(item).As<ItemExtraData>();
        extraData.Durability = NoInstanceOverride;
        extraData.Charge = NoInstanceOverride;
        var seedOrigin = EntityHandle.Of(item).As<SeedOrigin>();
        seedOrigin.SeededCount = reference.Count ?? 1;
        seedOrigin.IsPersistent = reference.Persistent == true;
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

    private static bool IsSeeded(RefRecord reference) => SeededFormTypes.IsSeeded(reference.BaseType);
}
