param([switch]$SkipChecks)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$archive = Join-Path $projectRoot '.tools/Godot_v4.7.2-stable_mono_export_templates.tpz'
$templateRoot = Join-Path $projectRoot '.tools/export_templates'
$outputRoot = Join-Path $projectRoot 'artifacts/IslandGlow-alpha-win64'
$godot = Join-Path $projectRoot '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = Join-Path $projectRoot '.tools/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
if (-not (Test-Path -LiteralPath $godot)) { & (Join-Path $projectRoot 'Setup.ps1') }
if (-not (Test-Path -LiteralPath (Join-Path $templateRoot 'windows_release_x86_64.exe'))) {
    if (-not (Test-Path -LiteralPath $archive)) {
        Write-Host 'Downloading official .NET export templates (1.2 GB, first build only).'
        & curl.exe -L --fail --silent --show-error 'https://github.com/godotengine/godot/releases/download/4.7.2-stable/Godot_v4.7.2-stable_mono_export_templates.tpz' --output $archive
        if ($LASTEXITCODE -ne 0) { throw 'Template download failed.' }
    }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne '92f8681e349ef1f90891b792da95e3b2b0bd1ed610b78018c58feb2d87e15a9d') { throw 'Official template checksum mismatch.' }
    New-Item -ItemType Directory -Force -Path $templateRoot | Out-Null
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
    try {
        foreach ($name in @('windows_debug_x86_64.exe', 'windows_release_x86_64.exe', 'windows_debug_x86_64_console.exe', 'windows_release_x86_64_console.exe')) {
            $entry = $zip.GetEntry("templates/$name")
            if (-not $entry) { throw "Missing template: $name" }
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $templateRoot $name), $true)
        }
    } finally { $zip.Dispose() }
}
Push-Location $projectRoot
try {
    & dotnet build IslandGlow.csproj --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if (-not $SkipChecks) {
        & dotnet run --project Tests/IslandGlow.Checks.csproj
        if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed.' }
    }
    & $godot --headless --path $projectRoot --editor --import
    if ($LASTEXITCODE -ne 0) { throw 'Resource import failed.' }
    New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
    $exportLog = Join-Path $projectRoot 'artifacts/windows-export.log'
    & $godot --headless --path $projectRoot --export-release 'Windows Alpha' (Join-Path $outputRoot 'IslandGlow.exe') 2>&1 | Tee-Object -FilePath $exportLog
    if ($LASTEXITCODE -ne 0 -or (Select-String -LiteralPath $exportLog -Pattern 'ERROR:|error [A-Z]+[0-9]+:' -Quiet)) { throw 'Windows export failed. Inspect artifacts/windows-export.log.' }
    $oldDotnetRoot = $env:DOTNET_ROOT
    $oldPath = $env:PATH
    try {
        $env:DOTNET_ROOT = $null
        $env:PATH = (($oldPath -split ';') | Where-Object { $_ -ne (Join-Path $projectRoot '.tools/dotnet') }) -join ';'
        $checkLog = Join-Path $projectRoot 'artifacts/packaged-checks.log'
        $checkErrors = Join-Path $projectRoot 'artifacts/packaged-checks-errors.log'
        $check = Start-Process -FilePath (Join-Path $outputRoot 'IslandGlow.exe') -ArgumentList @('--headless', '--fixed-fps', '60', '--', '--alpha-check') -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput $checkLog -RedirectStandardError $checkErrors
        if ($check.ExitCode -ne 0 -or -not (Select-String -LiteralPath $checkLog -Pattern 'PASS: [0-9]+ alpha runtime checks' -Quiet) -or (Select-String -LiteralPath $checkErrors -Pattern 'ERROR:' -Quiet)) { throw 'Packaged runtime checks failed. Inspect artifacts/packaged-checks*.log.' }
    } finally { $env:DOTNET_ROOT = $oldDotnetRoot; $env:PATH = $oldPath }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/PLAYER_GUIDE.md') -Destination (Join-Path $outputRoot 'START-HERE.md')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD_PARTY_NOTICES.md') -Destination $outputRoot
    Copy-Item -LiteralPath (Join-Path $projectRoot 'ThirdParty') -Destination $outputRoot -Recurse -Force
    # Read sharing tolerates antivirus/OneDrive readers and an executable mapping after verification.
    # Publish a complete archive atomically rather than truncating the last good ZIP first.
    Add-Type -AssemblyName System.IO.Compression
    $finalZip = Join-Path $projectRoot 'artifacts/IslandGlow-0.2.0-alpha-win64.zip'
    $stagingZip = Join-Path $projectRoot 'artifacts/IslandGlow-0.2.0-alpha-win64.staging.zip'
    $archiveStream = [System.IO.File]::Open($stagingZip, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    $archiveWriter = New-Object System.IO.Compression.ZipArchive($archiveStream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $outputRoot -File -Recurse) {
            $relative = $file.FullName.Substring($outputRoot.Length + 1).Replace('\', '/')
            $entry = $archiveWriter.CreateEntry($relative, [System.IO.Compression.CompressionLevel]::Optimal)
            $entryStream = $entry.Open()
            $sourceStream = [System.IO.File]::Open($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
            try { $sourceStream.CopyTo($entryStream) } finally { $sourceStream.Dispose(); $entryStream.Dispose() }
        }
    } finally { $archiveWriter.Dispose(); $archiveStream.Dispose() }
    if (Test-Path -LiteralPath $finalZip) { [System.IO.File]::Replace($stagingZip, $finalZip, [NullString]::Value) }
    else { [System.IO.File]::Move($stagingZip, $finalZip) }
    Write-Host "Alpha ready: $outputRoot"
} finally { Pop-Location }
