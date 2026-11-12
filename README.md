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
owners' AI packages say so. OblivionMP does not run that part of the game: the server owns every lock. This mod gives
those doors one schedule instead. The seed lists them in `world/door-schedules.json`; every one unlocks at
`OpenHour` and locks at `CloseHour`, by the game clock of the area the door is in. A load door pair goes by the later
of its two areas' clocks, since an area nobody is in keeps the time it had when the last player left.

A change is applied once, when the clock passes it, so a door picked open at night stays open until the next closing.
After a server restart each door gets the state its schedule says.

`config.json` in the mod's `server/` folder sets it:

```jsonc
{
  "DoorSchedule": {
    "Enabled": true,   // false leaves every door to your own server logic
    "OpenHour": 10,    // 0 to 23
    "CloseHour": 22    // 0 to 23, before OpenHour for a schedule that is open across midnight
  }
}
```

The doors change through the SDK's `LockChangeEvent`, raised by the server as a `Script` change, so a mod that claims a
door's ref keeps it to itself. A seed exported before door schedules has no list and no door follows one.

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
