param([string]$DalamudLibPath = "$env:APPDATA/XIVLauncher/addon/Hooks/dev/")
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'AetherRadio/AetherRadio.csproj'
$sdk = (Resolve-Path -LiteralPath $DalamudLibPath).Path + [IO.Path]::DirectorySeparatorChar
dotnet restore $project --locked-mode "-p:DalamudLibPath=$sdk"
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
dotnet build $project -c Release --no-restore --no-incremental "-p:DalamudLibPath=$sdk"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
dotnet run --project (Join-Path $root 'Tests/Tests.csproj') -c Release "-p:DalamudLibPath=$sdk"
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
$output = Join-Path $root 'AetherRadio/bin/Release'
$manifest = Get-Content -LiteralPath (Join-Path $output 'AetherRadio.json') -Raw | ConvertFrom-Json
if ($manifest.Author -ne 'Roxyz0501' -or !$manifest.RepoUrl -or !$manifest.IconUrl) { throw 'Missing manifest identity/URLs' }
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$destination = Join-Path $artifacts "AetherRadio-$($manifest.AssemblyVersion).zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination }
$zip = [IO.Compression.ZipFile]::Open($destination, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in @('AetherRadio.dll','AetherRadio.deps.json','AetherRadio.json','icon.png','LICENSE','THIRD_PARTY_NOTICES.md')) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $output $name), $name) | Out-Null
    }
    foreach ($license in Get-ChildItem -LiteralPath (Join-Path $root 'licenses') -Filter '*.txt') {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $license.FullName, "licenses/$($license.Name)") | Out-Null
    }
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $root 'README.md'), 'README.md') | Out-Null
    foreach ($imageName in @('icon.png','ui-preview.png')) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $root "images/$imageName"), "images/$imageName") | Out-Null
    }
} finally { $zip.Dispose() }
$zip = [IO.Compression.ZipFile]::OpenRead($destination)
try {
    $entry = $zip.GetEntry('AetherRadio.json')
    $reader = [IO.StreamReader]::new($entry.Open())
    try { $packed = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($packed.IconUrl -ne $manifest.IconUrl -or $packed.RepoUrl -ne $manifest.RepoUrl) { throw 'Packaged manifest URL mismatch' }
    if (!$zip.GetEntry('licenses/Orchestrion-MIT.txt')) { throw 'Missing upstream license' }
    $assemblyStream = [IO.MemoryStream]::new()
    $packedDll = $zip.GetEntry('AetherRadio.dll').Open()
    try {
        $packedDll.CopyTo($assemblyStream)
        $assembly = [Reflection.Assembly]::Load($assemblyStream.ToArray())
        $resources = $assembly.GetManifestResourceNames()
        foreach ($code in @('ja','en','de','fr','ko','zh-Hans','zh-Hant')) {
            if ("AetherRadio.Locales.$code.json" -notin $resources) { throw "Missing packaged locale: $code" }
        }
    } finally { $packedDll.Dispose(); $assemblyStream.Dispose() }
    if ($zip.Entries | Where-Object { $_.Name -in @('Dalamud.dll','Lumina.dll','FFXIVClientStructs.dll') }) { throw 'Host binaries must not be packaged' }
} finally { $zip.Dispose() }
$hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
Set-Content -LiteralPath "$destination.sha256" -Value "$hash  $([IO.Path]::GetFileName($destination))"
Write-Output "Package: $destination"
Write-Output "SHA256: $hash"
