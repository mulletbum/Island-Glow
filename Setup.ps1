$ErrorActionPreference = 'Stop'
$toolsPath = Join-Path $PSScriptRoot '.tools'
New-Item -ItemType Directory -Path $toolsPath -Force | Out-Null
New-Item -ItemType File -Path (Join-Path $toolsPath '.gdignore') -Force | Out-Null
$godotZip = Join-Path $toolsPath 'godot-dotnet.zip'
$godotExe = Join-Path $toolsPath 'godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
if (-not (Test-Path -LiteralPath $godotExe)) {
    Invoke-WebRequest -UseBasicParsing -Uri 'https://downloads.godotengine.org/?flavor=stable&platform=windows.64&slug=mono_win64.zip&version=4.7.2' -OutFile $godotZip
    Expand-Archive -LiteralPath $godotZip -DestinationPath (Join-Path $toolsPath 'godot') -Force
}
$dotnetPath = Join-Path $toolsPath 'dotnet'
if (-not (Test-Path -LiteralPath (Join-Path $dotnetPath 'sdk/8.0.425'))) {
    $installer = Join-Path $toolsPath 'dotnet-install.ps1'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & $installer -Version '8.0.425' -InstallDir $dotnetPath -NoPath
    if ($LASTEXITCODE -ne 0) { throw '.NET SDK setup failed.' }
}
Write-Host 'Toolchain ready. Double-click Play.cmd or run ./Run.ps1.'
