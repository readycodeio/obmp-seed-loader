using System.Text.Json.Serialization;

namespace OblivionMp.SnapshotLoader.Serverside;

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(SnapshotIndex))]
[JsonSerializable(typeof(CellFileRecord))]
[JsonSerializable(typeof(LeveledItemsFileRecord))]
[JsonSerializable(typeof(ItemValuesFileRecord))]
[JsonSerializable(typeof(NpcTemplatesFileRecord))]
[JsonSerializable(typeof(DoorSchedulesFileRecord))]
internal partial class WorldSnapshotJsonContext : JsonSerializerContext;
