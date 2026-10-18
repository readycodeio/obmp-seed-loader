using System.Text.Json.Serialization;
using ReadyM.Relay.Common.Oblivion.ECS.Values;

namespace OblivionMp.SnapshotLoader.Serverside;

public struct SnapshotIndex
{
    public int SchemaVersion;
    public List<string> LoadOrder;
    public List<AreaIndexRecord> Areas;
}

public struct AreaIndexRecord
{
    public string AreaId;
    public bool IsInterior;
    public List<CellIndexRecord> Cells;
}

public struct CellIndexRecord
{
    public int? X;
    public int? Y;
    public string File;
}

public struct CellFileRecord
{
    public List<RefRecord> References;
}

public struct RefRecord
{
    public uint RefId;
    public uint BaseId;
    [JsonConverter(typeof(JsonStringEnumConverter<FormType>))]
    public FormType BaseType;
    public string? BaseEditorId;
    public Vector3Record Position;
    public Vector3Record Rotation;
    public float? Scale;
    public int? Count;
    public uint? EnableParentRefId;
    public bool? InitiallyDisabled;
    public bool? Persistent;
    public int? LevelModifier;
    public LockRecord? Lock;
    public OwnerRecord? Owner;
    public TeleportRecord? Teleport;
    public uint? MerchantContainerRefId;
    public uint? MerchantRefId;
    public int? BarterGold;
    public bool? IsOwnedByMerchant;
    public bool? Respawns;
    public uint? HorseRefId;
    public uint? IngredientId;
    public List<ItemRecord>? Items;
}

public struct Vector3Record
{
    public float X;
    public float Y;
    public float Z;
}

public struct LockRecord
{
    public byte Level;
    public uint? KeyId;
    public bool? IsLeveled;
}

public struct OwnerRecord
{
    public uint OwnerId;
    public int? FactionRank;
    public uint? GlobalId;
}

public struct TeleportRecord
{
    public uint DestinationRefId;
    public Vector3Record Position;
    public Vector3Record Rotation;
}

public struct ItemRecord
{
    public uint ItemId;
    public int Count;
    [JsonConverter(typeof(JsonStringEnumConverter<FormType>))]
    public FormType? Kind;
}

public struct ItemValuesFileRecord
{
    public List<ItemValueRecord> Items;
}

public struct ItemValueRecord
{
    public uint ItemId;
    public int Value;
    public bool? HasComputedValue;
}

public struct NpcTemplatesFileRecord
{
    public List<NpcTemplateRecord> Npcs;
}

public struct NpcTemplateRecord
{
    public uint Id;
    public int Level;
    public bool PcLevelOffset;
    public ushort CalcMin;
    public ushort CalcMax;
    public List<ItemRecord>? Items;
}

public struct LeveledItemsFileRecord
{
    public List<LeveledItemRecord> Lists;
}

public struct LeveledItemRecord
{
    public uint Id;
    public string? EditorId;
    public byte ChanceNone;
    public bool CalcAllLevels;
    public bool CalcEachCount;
    public List<LeveledItemEntryRecord> Entries;
}

public struct LeveledItemEntryRecord
{
    public ushort Level;
    public ushort Count;
    public uint ItemId;
    [JsonConverter(typeof(JsonStringEnumConverter<FormType>))]
    public FormType? Kind;
}
