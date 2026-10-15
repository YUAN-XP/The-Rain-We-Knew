[CmdletBinding(DefaultParameterSetName = 'Publish')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Publish')][string]$BuildPath,
    [Parameter(Mandatory = $true, ParameterSetName = 'Publish')][string]$ReleaseLabel,
    [Parameter(Mandatory = $true, ParameterSetName = 'Publish')][string]$ValidationNote,
    [Parameter(Mandatory = $true, ParameterSetName = 'Verify')][switch]$VerifyOnly,
    [string]$ReleaseRoot = 'D:\游戏设计\正式游戏',
    [string]$DesktopLinkPath = (Join-Path ([Environment]::GetFolderPath('Desktop')) 'The Rain We Knew.lnk')
)

# Deployment only. This script never builds Unity, changes assets, or touches saves.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$owner = 'ProjectMystery.DesktopRelease.v1'
$markerName = 'ProjectMystery_Release.json'
$playerName = 'The Rain We Knew'
function Full-Path([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd([char[]]'\/')
}
$releaseBase = Full-Path $ReleaseRoot
if (-not $releaseBase.StartsWith('D:\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Release files must remain on drive D.'
}
$current = Join-Path $releaseBase 'ProjectMystery'
$previous = Join-Path $releaseBase 'ProjectMystery.previous'
$linkPath = Full-Path $DesktopLinkPath
if ([IO.Path]::GetExtension($linkPath) -ne '.lnk') { throw 'DesktopLinkPath must be a .lnk file.' }

function Assert-PlainDirectory([string]$Path) {
    $entry = Get-Item -LiteralPath $Path -Force
    if (-not $entry.PSIsContainer) { throw "Not a directory: $Path" }
    # Do not traverse junctions, links, or a linked ancestor during copy/removal.
    $ancestor = $entry
    while ($null -ne $ancestor) {
        if ($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked directory is not supported: $($ancestor.FullName)" }
        $ancestor = $ancestor.Parent
    }
    $linked = @(Get-ChildItem -LiteralPath $Path -Force -Recurse | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint })
    if ($linked.Count -gt 0) { throw "Linked content is not supported: $($linked[0].FullName)" }
}
function Get-Inventory([string]$Path) {
    $base = Full-Path $Path
    return @(Get-ChildItem -LiteralPath $base -File -Force -Recurse |
        Where-Object { $_.FullName -ne (Join-Path $base $markerName) } |
        Sort-Object FullName | ForEach-Object {
            [pscustomobject]@{
                Path = $_.FullName.Substring($base.Length + 1).Replace('\', '/')
                Bytes = $_.Length
                SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            }
        })
}
function Assert-Inventory([object[]]$Expected, [object[]]$Actual) {
    if ($Expected.Count -ne $Actual.Count) { throw "File count mismatch: expected $($Expected.Count), found $($Actual.Count)." }
    $byPath = @{}
    foreach ($row in $Actual) { $byPath[$row.Path] = $row }
    foreach ($row in $Expected) {
        if (-not $byPath.ContainsKey($row.Path)) { throw "Missing release file: $($row.Path)" }
        $found = $byPath[$row.Path]
        if ($found.Bytes -ne $row.Bytes -or $found.SHA256 -ne $row.SHA256) { throw "Release file changed: $($row.Path)" }
    }
}
function Read-ManagedRelease([string]$Path) {
    Assert-PlainDirectory $Path
    $marker = Join-Path $Path $markerName
    if (-not (Test-Path -LiteralPath $marker -PathType Leaf)) { throw "Unmanaged release directory; refusing to replace it: $Path" }
    $data = Get-Content -LiteralPath $marker -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($data.PublisherId -ne $owner -or (Full-Path $data.ReleaseRoot) -ne $releaseBase) { throw "Release ownership mismatch: $Path" }
    Assert-Inventory @($data.Files) @(Get-Inventory $Path)
    return $data
}
function Assert-NoRunningRelease {
    foreach ($process in @(Get-Process -Name ProjectMystery, 'The Rain We Knew' -ErrorAction SilentlyContinue)) {
        $exe = $process.Path
        if (-not $exe) { throw 'Cannot identify a running ProjectMystery instance; close the game before publishing.' }
        if ($exe.StartsWith($current + '\', [StringComparison]::OrdinalIgnoreCase) -or
            $exe.StartsWith($previous + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The desktop release is running. Close the game before publishing; it will not be terminated automatically.'
        }
    }
}
function Assert-DesktopLink {
    if (-not (Test-Path -LiteralPath $linkPath -PathType Leaf)) { throw "Desktop shortcut is missing: $linkPath" }
    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($linkPath)
    if ((Full-Path $link.TargetPath) -ne (Join-Path $current ($playerName + '.exe')) -or
        (Full-Path $link.WorkingDirectory) -ne $current -or $link.Arguments -ne '') {
        throw 'Desktop shortcut does not open the normal, current release.'
    }
}
function Remove-ManagedDirectory([string]$Path) {
    $absolute = Full-Path $Path
    if (-not $absolute.StartsWith($releaseBase + '\', [StringComparison]::OrdinalIgnoreCase) -or $absolute -eq $current) {
        throw "Unsafe cleanup target: $absolute"
    }
    $null = Read-ManagedRelease $absolute
    Remove-Item -LiteralPath $absolute -Recurse -Force
}

if ($VerifyOnly) {
    $release = Read-ManagedRelease $current
    Assert-DesktopLink
    [pscustomobject]@{ Result = 'Verified'; Release = $release.ReleaseLabel; Executable = (Join-Path $current ($playerName + '.exe')); FileCount = @($release.Files).Count; AssemblySHA256 = $release.AssemblySHA256; DesktopShortcut = $linkPath }
    return
}

$source = Full-Path $BuildPath
if ($source -eq $releaseBase -or $source.StartsWith($releaseBase + '\', [StringComparison]::OrdinalIgnoreCase) -or
    $releaseBase.StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Source build and managed release directory must be separate.'
}
Assert-PlainDirectory $source
$sourcePlayer = $playerName
if (-not (Test-Path -LiteralPath (Join-Path $source ($sourcePlayer + '.exe')) -PathType Leaf)) { $sourcePlayer = 'ProjectMystery' }
$sourceData = $sourcePlayer + '_Data'
foreach ($required in @(($sourcePlayer + '.exe'), 'UnityPlayer.dll', ($sourceData + '/Managed/Assembly-CSharp.dll'))) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $required) -PathType Leaf)) { throw "Incomplete Unity build: $required" }
}
# LZ4 player builds put globalgamemanagers and the scenes inside data.unity3d.
# Accept that complete player layout as well as the earlier loose-file layout.
$looseData = Join-Path $source ($sourceData + '/globalgamemanagers')
$packedData = Join-Path $source ($sourceData + '/data.unity3d')
if (-not (Test-Path -LiteralPath $looseData -PathType Leaf)) {
    if (-not (Test-Path -LiteralPath $packedData -PathType Leaf)) { throw 'Incomplete Unity build: missing loose or packed player data.' }
    $dataStream = [IO.File]::OpenRead($packedData)
    try {
        $signature = New-Object byte[] 8
        if ($dataStream.Length -lt 32 -or $dataStream.Read($signature, 0, 8) -ne 8 -or
            [Text.Encoding]::ASCII.GetString($signature) -ne "UnityFS`0") {
            throw 'Incomplete Unity build: packed player data has an invalid header.'
        }
    } finally { $dataStream.Dispose() }
}
if (Test-Path -LiteralPath (Join-Path $source $markerName)) { throw 'Publish from the tested build output, not from another managed release.' }
Assert-NoRunningRelease
if (-not (Test-Path -LiteralPath (Split-Path -Parent $linkPath) -PathType Container)) { throw 'The shortcut parent directory is missing.' }
$files = @(Get-Inventory $source)
$totalBytes = [long](($files | Measure-Object -Property Bytes -Sum).Sum)
if ((Get-PSDrive -Name D).Free -lt ($totalBytes + 536870912)) { throw 'Not enough free space on D for a staged copy and 512 MiB reserve.' }

$null = New-Item -ItemType Directory -Path $releaseBase -Force
Assert-PlainDirectory $releaseBase
$lockPath = Join-Path $releaseBase '.publish.lock'
$lock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$stage = Join-Path $releaseBase ('.ProjectMystery.staging-' + [Guid]::NewGuid().ToString('N'))
$shortcutBackup = Join-Path $releaseBase 'ProjectMystery.shortcut.before-latest-publish.lnk'
$hadLink = Test-Path -LiteralPath $linkPath -PathType Leaf
$hadCurrent = Test-Path -LiteralPath $current
$movedCurrent = $false
$promoted = $false
$shortcutTouched = $false
$committed = $false
try {
    if ($hadCurrent) { $null = Read-ManagedRelease $current }
    if (Test-Path -LiteralPath $previous) { $null = Read-ManagedRelease $previous }
    if ($hadLink) { Copy-Item -LiteralPath $linkPath -Destination $shortcutBackup -Force }

    Write-Verbose "Copying $($files.Count) build files to a staging directory."
    Copy-Item -LiteralPath $source -Destination $stage -Recurse -Force
    Assert-PlainDirectory $stage
    Assert-Inventory $files @(Get-Inventory $stage)
    # Rename the executable and its matching Unity data directory together.
    # Keep the internal product/save namespace unchanged so existing saves survive.
    if ($sourcePlayer -ne $playerName) {
        Rename-Item -LiteralPath (Join-Path $stage ($sourcePlayer + '.exe')) -NewName ($playerName + '.exe')
        Rename-Item -LiteralPath (Join-Path $stage $sourceData) -NewName ($playerName + '_Data')
        $files = @(Get-Inventory $stage)
    }
    $assembly = $files | Where-Object { $_.Path -eq ($playerName + '_Data/Managed/Assembly-CSharp.dll') }
    $manifest = [ordered]@{
        PublisherId = $owner
        ReleaseRoot = $releaseBase
        ReleaseLabel = $ReleaseLabel
        PublishedUtc = [DateTime]::UtcNow.ToString('O')
        SourceBuild = $source
        ValidationNote = $ValidationNote
        Bytes = $totalBytes
        AssemblySHA256 = $assembly.SHA256
        Files = $files
    }
    [IO.File]::WriteAllText((Join-Path $stage $markerName), ($manifest | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding($true)))
    Assert-NoRunningRelease
    # Keep one previous version. Only an owned, hash-verified release can be removed.
    if (Test-Path -LiteralPath $previous) { Remove-ManagedDirectory $previous }
    if ($hadCurrent) { Move-Item -LiteralPath $current -Destination $previous; $movedCurrent = $true }
    Move-Item -LiteralPath $stage -Destination $current
    $promoted = $true

    $shell = New-Object -ComObject WScript.Shell
    $link = $shell.CreateShortcut($linkPath)
    $link.TargetPath = Join-Path $current ($playerName + '.exe')
    $link.WorkingDirectory = $current
    $link.Arguments = ''
    $link.IconLocation = (Join-Path $current ($playerName + '.exe')) + ',0'
    $link.Description = 'The Rain We Knew - current tested desktop release'
    $shortcutTouched = $true
    $link.Save()
    $null = Read-ManagedRelease $current
    Assert-DesktopLink
    $committed = $true
    [pscustomobject]@{ Result = 'Published and verified'; Release = $ReleaseLabel; Executable = (Join-Path $current ($playerName + '.exe')); FileCount = $files.Count; Bytes = $totalBytes; AssemblySHA256 = $assembly.SHA256; DesktopShortcut = $linkPath; PreviousRelease = $(if ($movedCurrent) { $previous } else { $null }) }
}
catch {
    $failure = $_
    if (-not $committed) {
        if ($promoted) { Move-Item -LiteralPath $current -Destination $stage }
        if ($movedCurrent) { Move-Item -LiteralPath $previous -Destination $current }
        if ($shortcutTouched) {
            if ($hadLink) { Copy-Item -LiteralPath $shortcutBackup -Destination $linkPath -Force }
            elseif (Test-Path -LiteralPath $linkPath) { Remove-Item -LiteralPath $linkPath -Force }
        }
    }
    throw $failure
}
finally {
    # This unique directory was created by this invocation; never clean other folders.
    if (Test-Path -LiteralPath $stage) {
        if (-not (Full-Path $stage).StartsWith($releaseBase + '\.ProjectMystery.staging-', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staging cleanup target.' }
        Assert-PlainDirectory $stage
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
    $lock.Dispose()
    Remove-Item -LiteralPath $lockPath -Force -ErrorAction SilentlyContinue
}
