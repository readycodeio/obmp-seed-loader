# OblivionMP seed loader

Fills the [OblivionMP](https://obmp.readym.io) world from the seed, a snapshot of the game's data, so a fresh server
is not empty. When a cell is created for the first time, it gets its furniture, containers, plants and items. When a
new world is created, it gets the item values that trade checks against and the NPC templates that inventories are
rolled from. Anything that is already saved is left alone. It also opens and closes the doors the game keeps on a
schedule, see [Door schedules](#door-schedules).

It is an optional, server-only mod with no API of its own. Install it and it does its job.

The seed is one zip, `seed.zip`, in the `content/` folder of the mod's own `server/` folder:

```
<server>/mods/OblivionMp.SeedLoader/server/content/seed.zip
```

The mod reads it in place, starting at `world/index.json` inside it, and reads each cell only when the cell is first
created. It is never extracted.

Without it, or with one written for another schema version, the mod logs a warning and seeds nothing.

## Door schedules

In the single-player game, shops, guild halls and some houses unlock in the morning and lock at night, because their
owners' AI packages say so. OblivionMP does not run that part of the game: the server owns every lock. The seed carries
each such door's own hours, read off its owners' packages, and this mod puts them on the door when it seeds it, as the
SDK's `LockSchedule`: the hours of the game day the lock changes at, and what it changes to. A shop with a midday break
has four changes a day, a plain one two. Packages with conditions (a quest stage, a day of the week) are not part of it.

From there the SDK follows the schedule, by the world's game clock. A change is applied once, when the clock passes its
hour, so a door picked open at night stays open until the next closing, also across a server restart. The doors change
through the SDK's `LockChangeEvent`, raised by the server with the source `Schedule`: a mod's handler can refuse it, and
a mod that claims a door's ref keeps it to itself. To take one door off its schedule, set `IsEnabled` of its
`LockSchedule` to false, or write other hours into it.

`config.json` in the mod's `server/` folder sets it:

```json
{
  "DoorSchedule": {
    "Enabled": true
  }
}
```

- `Enabled`: `false` seeds every door with no schedule, and leaves the locks to your own server logic.

The hours are part of the seed from schema v10, and only a world seeded from it has them.

## The seed

The seed is in this repository, in [Git LFS](https://git-lfs.com):
`OblivionMp.SnapshotLoader.Serverside/seed/seed.zip`. Install Git LFS before cloning (Git for Windows includes it)
and the clone brings the zip. A clone made without it holds a small text pointer in the zip's place; run
`git lfs pull` to replace it with the zip.

A new seed is a commit: replace `seed/seed.zip` and commit it, and Git LFS stores the new zip. Nothing else names
the file.

Its files are under `world/` in the zip, compressed with Deflate, which .NET reads (unlike LZMA or BZip2, which some
7-Zip settings use). To make one from an extracted `seed/world/` folder, which git ignores, in PowerShell 7:

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

Use `Debug` instead of `Release` to include `.pdb` symbol files. The seed goes into the mod folder as
`server/content/seed.zip`.

Copy `Output/mods/OblivionMp.SeedLoader/` into your server's `mods/` directory, next to the `OblivionMP.SDK` mod,
and restart the server.
