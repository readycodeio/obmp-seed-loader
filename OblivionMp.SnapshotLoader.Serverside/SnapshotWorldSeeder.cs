using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Common.Archetypes;
using OblivionMP.SDK.Serverside.Archetypes;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Entities;
using ReadyM.SDK.Server.Seeding;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Fills a new World from the world snapshot: the item values trade checks against, and the npc templates inventories
/// are rolled from, both Global, and every persistent ref as a resident of its cell. A World loaded from the save
/// already has them.
/// </summary>
[Service]
public sealed partial class SnapshotWorldSeeder(WorldSnapshot snapshot, SnapshotEntitySeeder entitySeeder, IEntities entities, ILogger logger)
    : IWorldSeeder
{
    public void Seed(World world)
    {
        SeedItemValues();
        SeedNpcTemplates();
        SeedPersistentRefs();
        SeedLowProcesses();
    }

    private void SeedLowProcesses()
    {
        foreach (var refId in snapshot.LowProcessRefs)
            entities.Create<NpcLowProcess>().RefId = refId;

        logger.LogInformation("Seeded {Count} low process refs", snapshot.LowProcessRefs.Count);
    }

    // A script or another cell can name a persistent ref whether or not anyone has been to its cell
    private void SeedPersistentRefs()
    {
        var seeded = 0;
        foreach (var cellId in snapshot.Cells)
        {
            if (!snapshot.TryLoadCell(cellId, out var file))
                throw new InvalidOperationException($"The seed lists cell {cellId} and has no file for it.");

            foreach (var reference in file.References)
            {
                if (reference.Persistent != true || !SnapshotEntitySeeder.IsSeeded(reference) || !entitySeeder.TrySeed(reference, cellId, null, out var entity))
                    continue;

                entities.MakeResident(entity, cellId);
                seeded++;
            }
        }

        logger.LogInformation("Seeded {Count} persistent refs as residents of their cells", seeded);
    }

    private void SeedItemValues()
    {
        if (snapshot.ItemValues.Count == 0)
        {
            logger.LogWarning("The seed has no item values, trade prices are checked against the client's values");
            return;
        }

        var values = entities.Create<ItemValues>();
        foreach (var record in snapshot.ItemValues)
            values.SetValues(record.ItemId, new ItemValue(record.Value, record.HasComputedValue == true));

        logger.LogInformation("Seeded {Count} item values", values.ValuesCount);
    }

    private void SeedNpcTemplates()
    {
        foreach (var record in snapshot.NpcTemplates)
        {
            var template = entities.Create<NpcTemplate>();
            template.Id = new FormId(record.Id);
            template.Level = record.Level;
            template.PcLevelOffset = record.PcLevelOffset;
            template.CalcMin = record.CalcMin;
            template.CalcMax = record.CalcMax;
            foreach (var item in record.Items ?? [])
            {
                if (item.Kind is not { } kind)
                {
                    logger.LogError("Npc template {BaseId:X8}: item {ItemId:X8} has no kind in the seed, it is left out", record.Id, item.ItemId);
                    continue;
                }

                if (kind == FormType.LeveledItem)
                    LeveledListSeeding.Ensure(snapshot, entities, logger, item.ItemId);

                template.AddItems(new BaseContainerItem(new FormId(item.ItemId), item.Count, kind));
            }
        }

        logger.LogInformation("Seeded {Count} npc templates", snapshot.NpcTemplates.Count);
    }
}
