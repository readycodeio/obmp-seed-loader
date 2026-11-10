#!powershell.exe -ExecutionPolicy Bypass -File

param (
    [string] $Configuration,
    [switch] $NoExplorer
)

$ErrorActionPreference = 'Stop'

# Check params
if (-not $Configuration)
{
    Write-Host "Usage: .\MakeModFolder.ps1 <Debug|Release>"
    Exit 1
}

$scriptDir = $PSScriptRoot

# Source the mod file lists
$modFiles = Join-Path $scriptDir 'ModFiles.ps1'
try
{
    . $modFiles
}
catch
{
    Write-Host "Could not load $modFiles :"
    Write-Host $_.Exception.Message
    Write-Host ""
    Write-Host "If you unpacked this template from a downloaded ZIP, Windows marks the scripts as"
    Write-Host "remote and PowerShell refuses to load them. Clear the mark and try again:"
    Write-Host "  Get-ChildItem -Path '$scriptDir' -Recurse | Unblock-File"
    Exit 1
}

if (-not $modName -or -not $serverProject)
{
    Write-Host "$modFiles loaded but did not define the expected variables. Check it for edits."
    Exit 1
}

# Find the single .sln file
$solutionFiles = Get-ChildItem -Path $scriptDir -Filter *.sln
if ($solutionFiles.Count -eq 1)
{
    $solutionPath = $solutionFiles[0].FullName
}
else
{
    Write-Host "Error: Expected exactly one .sln file in $scriptDir, found $( $solutionFiles.Count )."
    Exit 1
}

# Build the whole solution (all three projects)
Write-Output "Building solution $solutionPath in configuration $Configuration..."
dotnet build $solutionPath -c $Configuration -v minimal /t:Rebuild | Tee-Object -FilePath (Join-Path $scriptDir 'build.log')
if ($LASTEXITCODE -ne 0)
{
    Write-Host "Build failed. See build.log for details."
    Exit 1
}

# Reset the Output directory
$outputRoot = Join-Path $scriptDir 'Output'
if (Test-Path $outputRoot)
{
    Remove-Item $outputRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

# One folder per mod, holding a client and a server half plus the shared manifest. Drop it into
# the server's "mods" directory as-is; connecting players receive the client half only.
$modRoot = Join-Path (Join-Path $outputRoot 'mods') $modName
$clientRoot = Join-Path $modRoot 'client'
$serverRoot = Join-Path $modRoot 'server'
New-Item -ItemType Directory -Path $serverRoot -Force | Out-Null

$tfm = 'net10.0'
$clientBuildDir = Join-Path $scriptDir "$clientProject/bin/$Configuration/$tfm"
$serverBuildDir = Join-Path $scriptDir "$serverProject/bin/$Configuration/$tfm"
$contentDir = Join-Path $scriptDir 'Content'

# Copies the named files from $BaseDir into $DestRoot, preserving any relative subpaths.
function Copy-BuildArtifacts
{
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]] $Files,

        [Parameter(Mandatory = $true)]
        [string] $BaseDir,

        [Parameter(Mandatory = $true)]
        [string] $DestRoot
    )

    foreach ($file in $Files)
    {
        $sourceFile = Join-Path -Path $BaseDir -ChildPath $file
        $destFile = Join-Path -Path $DestRoot -ChildPath $file

        if (Test-Path -Path $sourceFile -PathType Container)
        {
            Copy-Item -Path $sourceFile -Destination $destFile -Recurse -Force
            Write-Output "Copied $file/ to $( Split-Path $DestRoot -Leaf )."
        }
        elseif (Test-Path -Path $sourceFile)
        {
            $destDir = Split-Path -Parent $destFile
            if (-not (Test-Path -Path $destDir))
            {
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            }

            Copy-Item -Path $sourceFile -Destination $destFile -Force
            Write-Output "Copied $file to $( Split-Path $DestRoot -Leaf )."
        }
        else
        {
            Write-Warning "Source file not found: $sourceFile"
        }
    }
}

$clientFiles = $clientBuildFiles
$serverFiles = $serverBuildFiles
if ($Configuration -eq 'Debug')
{
    $clientFiles += $clientDebugBuildFiles
    $serverFiles += $serverDebugBuildFiles
}

Write-Output "`nPackaging $modName -> $modRoot"
# A server-only mod declares no client project, and its folder then has no client half.
if ($clientProject)
{
    New-Item -ItemType Directory -Path $clientRoot -Force | Out-Null
    Copy-BuildArtifacts -Files $clientFiles -BaseDir $clientBuildDir -DestRoot $clientRoot
    Copy-BuildArtifacts -Files $clientContentFiles -BaseDir $contentDir -DestRoot $clientRoot
}
Copy-BuildArtifacts -Files $serverFiles -BaseDir $serverBuildDir -DestRoot $serverRoot
Copy-BuildArtifacts -Files $serverContentFiles -BaseDir $contentDir -DestRoot $serverRoot

if ($serverDataFile)
{
    $data = Join-Path $scriptDir $serverDataFile
    # A clone made without Git LFS holds a small text pointer in the seed's place, which the mod would take for a
    # broken zip.
    $isLfsPointer = (Test-Path $data -PathType Leaf) -and (Get-Item $data).Length -lt 1024 -and
        (Get-Content $data -TotalCount 1) -eq 'version https://git-lfs.github.com/spec/v1'
    if ($isLfsPointer)
    {
        throw "$serverDataFile is a Git LFS pointer, not the seed. Install Git LFS and run 'git lfs pull' in $scriptDir."
    }
    elseif (Test-Path $data -PathType Leaf)
    {
        $contentRoot = Join-Path $serverRoot 'content'
        New-Item -ItemType Directory -Path $contentRoot -Force | Out-Null
        Copy-Item $data $contentRoot -Force
        Write-Host "Copied $serverDataFile to server/content."
    }
    else
    {
        Write-Warning "No $serverDataFile, so the mod folder has no seed (see README.md)."
    }
}
Copy-BuildArtifacts -Files $manifestFiles -BaseDir $contentDir -DestRoot $modRoot

# Open explorer to the output directory
if ($NoExplorer)
{
    # nothing to open, this run is scripted
}
elseif ($PSVersionTable.PSEdition -eq 'Core')
{
    Start-Process "explorer.exe" -ArgumentList "`"$outputRoot`""
}
else
{
    Invoke-Item $outputRoot
}
