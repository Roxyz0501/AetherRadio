param([string]$EntryPath = (Join-Path $PSScriptRoot '../release/repository-entry.template.json'))
$ErrorActionPreference = 'Stop'
$entry = Get-Content -LiteralPath $EntryPath -Raw | ConvertFrom-Json
$repo = Invoke-WebRequest -Uri $entry.RepoUrl
if ($repo.StatusCode -ne 200) { throw 'Repository must be publicly accessible' }
$icon = Invoke-WebRequest -Uri $entry.IconUrl
if ($icon.StatusCode -ne 200 -or [string]$icon.Headers['Content-Type'] -notmatch '^image/') { throw 'Icon must return HTTP 200 and an image Content-Type' }
foreach ($url in @($entry.DownloadLinkInstall, $entry.DownloadLinkUpdate, $entry.DownloadLinkTesting) | Select-Object -Unique) {
    $download = Invoke-WebRequest -Uri $url -Method Head
    if ($download.StatusCode -ne 200) { throw 'Release download not accessible' }
}
Write-Output 'Public repository, image and download URLs verified.'
