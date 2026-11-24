using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Oblivion.ECS.Values;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Server.Seeding;

namespace OblivionMp.SnapshotLoader.Serverside;

/// <summary>
/// Fills a new cell from the world snapshot: furniture, containers, plants and items. The server calls it only for a
/// cell with no save, so nothing of the cell exists yet. No player is in it: what it creates is the server's until the
/// first player enters, and containers are rolled at that player's level by <c>CellRespawnSystem</c>, which also marks
/// the cell seeded. The cell's persistent refs are not its content: <see cref="SnapshotWorldSeeder"/> made them with
/// the world.
/// </summary>
[Service]
public sealed partial class SnapshotCellSeeder(WorldSnapshot snapshot, SnapshotEntitySeeder entitySeeder, ILogger logger)
    : ICellSeeder
{
    public void Seed(Cell cell)
    {
        var cellId = cell.FullCellId;
        if (!snapshot.TryLoadCell(cellId, out var file))
            return;

        var furniture = 0;
        var containers = 0;
        var flora = 0;
        var doors = 0;
        var activators = 0;
        var items = 0;
        foreach (var reference in file.References)
        {
            if (!SnapshotEntitySeeder.IsSeeded(reference) || reference.Persistent == true || !entitySeeder.TrySeed(reference, cellId, cell, out _))
                continue;

            switch (reference.BaseType)
            {
                case FormType.Furniture:
                    furniture++;
                    break;
                case FormType.Container:
                    containers++;
                    break;
                case FormType.Flora:
                    flora++;
                    break;
                case FormType.Door:
                    doors++;
                    break;
                case FormType.Activator:
                    activators++;
                    break;
                default:
                    items++;
                    break;
            }
        }

        if (furniture + containers + flora + doors + activators + items > 0)
            logger.LogInformation("Seeded {Furniture} furniture, {Containers} containers, {Flora} plants, {Doors} doors, {Activators} activators and {Items} items in {Cell}", furniture, containers, flora, doors, activators, items, cellId);
    }
}
