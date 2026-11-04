# OblivionMP seed loader

Fills the [OblivionMP](https://obmp.readym.io) world from the seed, a snapshot of the game's data, so a fresh server
is not empty. When a cell is created for the first time, it gets its furniture, containers, plants and items. When a
new world is created, it gets the item values that trade checks against and the NPC templates that inventories are
rolled from. Anything that is already saved is left alone.

It is an optional, server-only mod with no API of its own. Install it and it does its job.

The seed is one zip, `seed.zip`, in the `content/` folder of the mod's own `server/` folder:

```
<server>/mods/OblivionMp.SeedLoader/server/content/seed.zip
```

The mod reads it in place, starting at `world/index.json` inside it, and reads each cell only when the cell is first
created. It is never extracted.

Without it, or with one written for another schema version, the mod logs a warning and seeds nothing.

## The seed

The seed is hosted in blob storage, not in git:
[seed.zip](https://readycodestorage.blob.core.windows.net/public/obmp/seed.zip). Download it, without extracting it,
into `OblivionMp.SnapshotLoader.Serverside/seed/`, which git ignores, so that `seed/seed.zip` exists. With the
OblivionMP mod repository cloned next to this one, its `scripts/FetchSeed.ps1` fetches it.

Its files are under `world/` in the zip, compressed with Deflate, which .NET reads (unlike LZMA or BZip2, which some
7-Zip settings use). To make one from an extracted `seed/world/` folder, in PowerShell 7:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory("$PWD/OblivionMp.SnapshotLoader.Serverside/seed/world", "$PWD/OblivionMp.SnapshotLoader.Serverside/seed/seed.zip", 'SmallestSize', $true)
```

## Building

The project builds against the OblivionMP SDK package from NuGet, `ReadyM.SDK.Oblivion.Server`. Its version is set
once, as `OblivionSdkVersion` in `Directory.Build.props`.

```powershell
.\MakeModFolder.ps1 Release                  # the mod, into Output/mods/OblivionMp.SeedLoader
```

Use `Debug` instead of `Release` to include `.pdb` symbol files. The seed, when you have fetched it, goes into the
mod folder as `server/content/seed.zip`.

Copy `Output/mods/OblivionMp.SeedLoader/` into your server's `mods/` directory, next to the `OblivionMP.SDK` mod,
and restart the server.
