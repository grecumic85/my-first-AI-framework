param(
    [string]$Repository = (Split-Path $PSScriptRoot -Parent),
    [string]$OutputDirectory = '',
    [string]$ArchiveName = 'web-automation-starter-v2.zip'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repoPath = [IO.Path]::GetFullPath($Repository).TrimEnd('\','/')
if (-not $OutputDirectory) { $OutputDirectory = Split-Path $repoPath -Parent }
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if ([IO.Path]::GetFileName($ArchiveName) -ne $ArchiveName) { throw 'ArchiveName must be a filename.' }
[IO.Directory]::CreateDirectory($outputPath) | Out-Null
$archivePath = Join-Path $outputPath $ArchiveName
if (Test-Path -LiteralPath $archivePath) { throw "Archive already exists: $archivePath. Choose a new name." }
$files = @(Get-ChildItem -LiteralPath $repoPath -Recurse -Force -File | Where-Object {
    $relative = $_.FullName.Substring($repoPath.Length + 1).Replace('\','/')
    $relative -notmatch '(^|/)(bin|obj|artifacts|TestResults|\.git|\.vs|packages)/' -and
    $relative -notmatch '\.feature\.cs$' -and
    $relative -notmatch '(^|/)(settings\.local\.json|\.env|MANIFEST\.sha256)$' -and
    $_.FullName -ne $archivePath
} | Sort-Object FullName)
$manifest = @($files | ForEach-Object {
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.FullName.Substring($repoPath.Length + 1).Replace('\','/')
})
$manifestPath = Join-Path $repoPath 'MANIFEST.sha256'
[IO.File]::WriteAllLines($manifestPath, $manifest, [Text.UTF8Encoding]::new($false))
$files += Get-Item -LiteralPath $manifestPath
$archiveRoot = [IO.Path]::GetFileName($repoPath)
$zip = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($repoPath.Length + 1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, "$archiveRoot/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose() }
$expected = @{}
foreach ($line in $manifest) { $expected[$archiveRoot + '/' + $line.Substring(66)] = $line.Substring(0,64) }
$zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
$checked = 0
try {
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -eq "$archiveRoot/MANIFEST.sha256") { continue }
        if (-not $expected.ContainsKey($entry.FullName)) { throw "Unexpected ZIP entry: $($entry.FullName)" }
        $stream = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
        try { $actual = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant() }
        finally { $sha.Dispose(); $stream.Dispose() }
        if ($actual -ne $expected[$entry.FullName]) { throw "ZIP hash mismatch: $($entry.FullName)" }
        $checked++
    }
    if ($checked -ne $manifest.Count) { throw 'Archive is missing manifest entries.' }
} finally { $zip.Dispose() }
$zipHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($archivePath + '.sha256', "$zipHash  $ArchiveName" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
[ordered]@{ archive=$ArchiveName; bytes=(Get-Item -LiteralPath $archivePath).Length; entries=$files.Count; verifiedPayloadFiles=$checked; sha256=$zipHash } | ConvertTo-Json
