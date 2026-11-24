using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;

namespace OblivionMp.SnapshotLoader.Serverside;

[ModEntry]
public sealed partial class Mod(IDependencyContainer services)
{
    private void Init()
    {
        // The seed is the content folder in this mod's server folder, wherever the mod is installed.
        services.RegisterSingleton(new SeedSettings(Path.Combine(ModDirectory, "content")));

        // The cell and world seeders are [Service]s, which the SDK collects; they share the snapshot.
        services.RegisterSingleton<WorldSnapshot>();
        services.RegisterSingleton<SnapshotEntitySeeder>();
    }
}