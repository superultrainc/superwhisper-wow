$ErrorActionPreference = 'Stop'
$install = Join-Path $env:LOCALAPPDATA 'SuperwhisperWoW'
$manifest = Join-Path $install 'installation.json'
if (!(Test-Path -LiteralPath $manifest)) { return }
$old = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
$exe = Join-Path $install 'SuperwhisperWoW.exe'
$running = Get-Process -Name SuperwhisperWoW -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }
if ($running) {
    $event = [Threading.EventWaitHandle]::OpenExisting('Local\SuperwhisperWoW.Exit')
    try { [void]$event.Set() } finally { $event.Dispose() }
    $running | Wait-Process -Timeout 10
}
Remove-ItemProperty 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Run' -Name SuperwhisperWoW -ErrorAction SilentlyContinue
$addon = Join-Path $old.game 'Interface/AddOns/SuperwhisperWoW'
foreach ($name in $old.files) {
    if ([IO.Path]::GetFileName($name) -ne $name) { throw 'Invalid addon filename in manifest.' }
    $path = Join-Path $addon $name
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path }
}
if ((Test-Path -LiteralPath $addon) -and !(Get-ChildItem -Force -LiteralPath $addon)) { [IO.Directory]::Delete($addon) }
foreach ($name in 'SuperwhisperWoW.exe','uninstall.ps1','wow-vocabulary.csv','installation.json') {
    $path = Join-Path $install $name
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
}
if (!(Get-ChildItem -Force -LiteralPath $install)) { [IO.Directory]::Delete($install) }
