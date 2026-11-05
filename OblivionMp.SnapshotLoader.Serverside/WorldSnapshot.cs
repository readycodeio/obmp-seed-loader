using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Oblivion;

namespace OblivionMp.SnapshotLoader.Serverside;

public sealed class WorldSnapshot
{
    public const int SchemaVersion = 8;

    private const string SeedFileName = "seed.zip";
    private const string WorldFolder = "world";
    private const string IndexFileName = "index.json";
    private const string LeveledItemsFileName = "leveled-items.json";
    private const string ItemValuesFileName = "item-values.json";
    private const string NpcTemplatesFileName = "npc-templates.json";

    // Not thread-safe, which is fine: the seeders run on the game thread.
    private readonly ZipArchive? _seed;
    private readonly Dictionary<FullCellId, string> _fileByCell = [];
    private readonly Dictionary<uint, LeveledItemRecord> _leveledItemListById = [];
    private readonly List<ItemValueRecord> _itemValues = [];
    private readonly List<NpcTemplateRecord> _npcTemplates = [];
    private readonly List<string> _loadOrder = [];

    public WorldSnapshot(ILogger logger, SeedSettings settings)
    {
        var seedPath = Path.Combine(settings.ContentFolder, SeedFileName);
        if (!File.Exists(seedPath))
        {
            logger.LogWarning("No seed at {Path}, cells are not seeded", seedPath);
            return;
        }

        // Keep in RAM so we don't lock the file
        _seed = new ZipArchive(new MemoryStream(File.ReadAllBytes(seedPath)), ZipArchiveMode.Read);
        if (_seed.GetEntry(EntryName(IndexFileName)) is null)
        {
            logger.LogWarning("Seed at {Path} has no {Entry}, cells are not seeded", seedPath, EntryName(IndexFileName));
            return;
        }

        var index = Read(IndexFileName, WorldSnapshotJsonContext.Default.SnapshotIndex);
        if (index.SchemaVersion != SchemaVersion)
        {
            logger.LogWarning("Seed at {Path} has schema {Found}, expected {Expected}, cells are not seeded", seedPath, index.SchemaVersion, SchemaVersion);
            return;
        }

        foreach (var area in index.Areas)
        {
            var areaId = new AreaId(area.AreaId);
            foreach (var cell in area.Cells)
            {
                var cellId = area.IsInterior ? OblivionCellIds.Interior : OblivionCellIds.Exterior(cell.X!.Value, cell.Y!.Value);
                _fileByCell.Add(new FullCellId(areaId, cellId), cell.File);
            }
        }

        foreach (var list in Read(LeveledItemsFileName, WorldSnapshotJsonContext.Default.LeveledItemsFileRecord).Lists)
            _leveledItemListById.Add(list.Id, list);

        _itemValues.AddRange(Read(ItemValuesFileName, WorldSnapshotJsonContext.Default.ItemValuesFileRecord).Items);
        _npcTemplates.AddRange(Read(NpcTemplatesFileName, WorldSnapshotJsonContext.Default.NpcTemplatesFileRecord).Npcs);
        _loadOrder.AddRange(index.LoadOrder);

        logger.LogInformation("Seed with {Areas} areas, {Cells} cells, {Lists} leveled item lists, {Values} item values and {Npcs} npc templates, load order {LoadOrder}", index.Areas.Count, _fileByCell.Count, _leveledItemListById.Count, _itemValues.Count, _npcTemplates.Count, string.Join(", ", index.LoadOrder));
    }

    public IReadOnlyList<ItemValueRecord> ItemValues => _itemValues;

    public IReadOnlyList<NpcTemplateRecord> NpcTemplates => _npcTemplates;

    /// <summary>The plugins the snapshot was exported from, in load order, or none without a seed.</summary>
    public IReadOnlyList<string> LoadOrder => _loadOrder;

    public bool TryGetLeveledItemList(uint id, out LeveledItemRecord list) => _leveledItemListById.TryGetValue(id, out list);

    public bool TryLoadCell(FullCellId cellId, out CellFileRecord cell)
    {
        if (!_fileByCell.TryGetValue(cellId, out var file))
        {
            cell = default;
            return false;
        }

        cell = Read(file, WorldSnapshotJsonContext.Default.CellFileRecord);
        return true;
    }

    // Seed files are under world/ in the zip, and the index names cell files relative to it
    private static string EntryName(string file) => $"{WorldFolder}/{file}";

    private T Read<T>(string file, JsonTypeInfo<T> typeInfo) where T : struct
    {
        var entry = _seed!.GetEntry(EntryName(file)) ?? throw new InvalidDataException($"The seed has no {EntryName(file)}");
        using var stream = entry.Open();
        return JsonSerializer.Deserialize(stream, typeInfo);
    }
}