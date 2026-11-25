using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using OblivionMp.SnapshotLoader.Serverside;

namespace OblivionMp.SnapshotLoader.Serverside.Tests;

/// <summary>
/// The snapshot's persistent actors are the npcs and creatures marked persistent in any cell of the seed. A persistent
/// ref of another kind, and an actor that is not persistent, are left out.
/// </summary>
public sealed class WorldSnapshotPersistentActorsTests : IDisposable
{
    private readonly string _contentFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose() => Directory.Delete(_contentFolder, true);

    [Fact]
    public void Persistent_npcs_and_creatures_of_every_cell_are_listed()
    {
        WriteSeed(new()
        {
            ["index.json"] = $$"""
                {"SchemaVersion":{{WorldSnapshot.SchemaVersion}},"LoadOrder":[],"Areas":[
                    {"AreaId":"Tavern","IsInterior":true,"Cells":[{"File":"Tavern/interior.json"}]},
                    {"AreaId":"Outside","IsInterior":false,"Cells":[{"X":1,"Y":2,"File":"Outside/1,2.json"}]}]}
                """,
            ["Tavern/interior.json"] = """
                {"References":[
                    {"RefId":10,"BaseId":1,"BaseType":"NPC","Persistent":true},
                    {"RefId":11,"BaseId":1,"BaseType":"NPC"},
                    {"RefId":12,"BaseId":2,"BaseType":"Door","Persistent":true}]}
                """,
            ["Outside/1,2.json"] = """
                {"References":[
                    {"RefId":20,"BaseId":3,"BaseType":"Creature","Persistent":true},
                    {"RefId":21,"BaseId":3,"BaseType":"Creature","Persistent":false}]}
                """,
            ["leveled-items.json"] = """{"Lists":[]}""",
            ["item-values.json"] = """{"Items":[]}""",
            ["npc-templates.json"] = """{"Npcs":[]}""",
        });

        var snapshot = new WorldSnapshot(NullLogger.Instance, new SeedSettings(_contentFolder));

        Assert.Equal([10u, 20u], snapshot.PersistentActors.Order());
    }

    [Fact]
    public void Low_process_refs_are_the_ones_the_seed_marks()
    {
        WriteSeed(new()
        {
            ["index.json"] = $$"""
                {"SchemaVersion":{{WorldSnapshot.SchemaVersion}},"LoadOrder":[],"Areas":[
                    {"AreaId":"Tavern","IsInterior":true,"Cells":[{"File":"Tavern/interior.json"}]}]}
                """,
            ["Tavern/interior.json"] = """
                {"References":[
                    {"RefId":10,"BaseId":1,"BaseType":"NPC","Persistent":true,"LowProcess":true},
                    {"RefId":11,"BaseId":1,"BaseType":"NPC","Persistent":true},
                    {"RefId":12,"BaseId":3,"BaseType":"Creature","Persistent":true,"LowProcess":true},
                    {"RefId":13,"BaseId":2,"BaseType":"Door","Persistent":true}]}
                """,
            ["leveled-items.json"] = """{"Lists":[]}""",
            ["item-values.json"] = """{"Items":[]}""",
            ["npc-templates.json"] = """{"Npcs":[]}""",
        });

        var snapshot = new WorldSnapshot(NullLogger.Instance, new SeedSettings(_contentFolder));

        Assert.Equal([10u, 12u], snapshot.LowProcessRefs.Order());
    }

    [Fact]
    public void No_seed_lists_no_persistent_actors()
    {
        Directory.CreateDirectory(_contentFolder);

        var snapshot = new WorldSnapshot(NullLogger.Instance, new SeedSettings(_contentFolder));

        Assert.Empty(snapshot.PersistentActors);
    }

    private void WriteSeed(Dictionary<string, string> files)
    {
        Directory.CreateDirectory(_contentFolder);
        using var seed = ZipFile.Open(Path.Combine(_contentFolder, "seed.zip"), ZipArchiveMode.Create);
        foreach (var (file, json) in files)
        {
            using var writer = new StreamWriter(seed.CreateEntry($"world/{file}").Open());
            writer.Write(json);
        }
    }
}
