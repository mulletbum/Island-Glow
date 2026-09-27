param(
    [switch]$Editor,
    [switch]$Verify,
    [switch]$Headless,
    [switch]$Prototype
)
$ErrorActionPreference = 'Stop'
if ($Editor -and $Verify) { throw 'Choose either -Editor or -Verify.' }
$projectRoot = $PSScriptRoot
$localDotnet = Join-Path $projectRoot '.tools/dotnet'
$godot = Join-Path $projectRoot '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
if ($env:GODOT4) { $godot = $env:GODOT4 }
if (-not (Test-Path -LiteralPath $godot)) {
    throw 'Godot 4.7.2 .NET is required. Run Setup.ps1 or set GODOT4 to its executable.'
}
if (Test-Path -LiteralPath (Join-Path $localDotnet 'dotnet.exe')) {
    $env:DOTNET_ROOT = $localDotnet
    $env:PATH = "$localDotnet;$env:PATH"
}
Push-Location $projectRoot
try {
    & dotnet build IslandGlow.csproj --nologo
    if ($LASTEXITCODE -ne 0) { throw 'C# build failed.' }
    & $godot --headless --path $projectRoot --editor --import
    if ($LASTEXITCODE -ne 0) { throw 'Godot import failed.' }
    $runArgs = @('--path', $projectRoot)
    if ($Editor) { $runArgs += '--editor' }
    if ($Headless) { $runArgs += '--headless' }
    if ($Prototype) { $runArgs += 'res://Scenes/Ship.tscn' }
    if ($Verify) {
        & dotnet run --project 'Tests/IslandGlow.Checks.csproj'
        if ($LASTEXITCODE -ne 0) { throw 'Core simulation checks failed.' }
        if ($Prototype) { $runArgs += @('--fixed-fps', '60', '--', '--verify') }
        else { $runArgs += @('--fixed-fps', '60', '--', '--alpha-check') }
    }
    & $godot @runArgs
    if ($LASTEXITCODE -ne 0) { throw 'Godot exited with an error.' }
}
finally { Pop-Location }
