using Microsoft.Extensions.Logging;
using OblivionMP.SDK.Serverside.Archetypes;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.SDK.Server.Entities;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Creates a leveled item list template from the snapshot the first time a seeder needs it, with the lists it nests.
/// The templates are Global. Shared by the cell and world seeders.
/// </summary>
internal static class LeveledListSeeding
{
    public static void Ensure(WorldSnapshot snapshot, IEntities entities, ILogger logger, uint listId)
    {
        foreach (var known in entities.Query<LeveledListTemplate>())
        {
            if (known.Id == listId)
                return;
        }

        if (!snapshot.TryGetLeveledItemList(listId, out var record))
        {
            logger.LogError("Leveled item list {ListId:X8} is not in the seed, containers rolling it get nothing from it", listId);
            return;
        }

        var template = entities.Create<LeveledListTemplate>();
        template.Id = new FormId(record.Id);
        template.ChanceNone = record.ChanceNone;
        template.CalcAllLevels = record.CalcAllLevels;
        template.CalcEachCount = record.CalcEachCount;
        foreach (var entry in record.Entries)
        {
            if (entry.Kind is not { } kind)
            {
                logger.LogError("Leveled item list {ListId:X8}: entry {ItemId:X8} has no kind in the seed, it is left out", listId, entry.ItemId);
                continue;
            }

            template.AddEntries(new LeveledListItem(entry.Level, entry.Count, new FormId(entry.ItemId), kind));
        }

        foreach (var entry in record.Entries)
        {
            if (entry.Kind == FormType.LeveledItem)
                Ensure(snapshot, entities, logger, entry.ItemId);
        }
    }
}
