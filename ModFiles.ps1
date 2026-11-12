#!powershell.exe -ExecutionPolicy Bypass -File

# The file lists MakeModFolder.ps1 uses to build
#
#   Output/mods/<modName>/manifest.json
#   Output/mods/<modName>/server/...
#
# This is a server-only mod: it has no client half, so players never download anything for it.

# Folder name of the packaged mod.
$modName = "OblivionMp.SeedLoader"

# The projects the halves build from. No client project: the folder gets no client/.
$clientProject = ""
$serverProject = "OblivionMp.SnapshotLoader.Serverside"

# --- Server half, from <serverProject>/bin/<Configuration>/net10.0 ---
$serverBuildFiles = @(
    "OblivionMp.SnapshotLoader.Serverside.dll"
)

# Copied from "Content" into the mod root.
$manifestFiles = @(
    "manifest.json"
)

# Copied from "Content" into server/.
$clientContentFiles = @()

$serverContentFiles = @(
    "config.json"
)

# Copied into server/content/, where the mod reads it: the seed, one zip in Git LFS rather than built. A folder made
# without it only warns; one made from a clone without Git LFS, which holds a pointer in its place, stops.
$serverDataFile = "OblivionMp.SnapshotLoader.Serverside/seed/seed.zip"

# Added only in Debug builds.
$clientBuildFiles = @()
$clientDebugBuildFiles = @()

$serverDebugBuildFiles = @(
    "OblivionMp.SnapshotLoader.Serverside.pdb"
)
