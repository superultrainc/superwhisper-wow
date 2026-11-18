[CmdletBinding()]
param([Parameter(Mandatory)][string]$WowDirectory)
$ErrorActionPreference = 'Stop'
$game = (Get-Item -LiteralPath $WowDirectory).FullName
if (!(Test-Path -LiteralPath (Join-Path $game 'Wow.exe') -PathType Leaf)) {
    throw 'Choose the game version directory containing Wow.exe.'
}
$install = Join-Path $env:LOCALAPPDATA 'SuperwhisperWoW'
$addon = Join-Path $game 'Interface/AddOns/SuperwhisperWoW'
$exe = Join-Path $install 'SuperwhisperWoW.exe'
$manifest = Join-Path $install 'installation.json'
$old = if (Test-Path -LiteralPath $manifest) { Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json }
if ($old -and $old.game -ne $game) { throw 'Uninstall the previous game installation first.' }
if (!$old -and (Test-Path -LiteralPath $addon)) { throw 'Move the existing SuperwhisperWoW addon aside before installing.' }
$files = @(Get-ChildItem -File -LiteralPath "$PSScriptRoot/SuperwhisperWoW")
if (!$files.Count -or !(Test-Path "$PSScriptRoot/SuperwhisperWoW.exe")) { throw 'Extract the complete Windows package first.' }
$running = Get-Process -Name SuperwhisperWoW -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }
if ($running) {
    $event = [Threading.EventWaitHandle]::OpenExisting('Local\SuperwhisperWoW.Exit')
    try { [void]$event.Set() } finally { $event.Dispose() }
    $running | Wait-Process -Timeout 10
}
New-Item -Type Directory -Force $install,$addon | Out-Null
@{game=$game; files=@($files.Name)} | ConvertTo-Json | Set-Content -LiteralPath $manifest
foreach ($file in $files) { Copy-Item -LiteralPath $file.FullName -Destination $addon -Force }
foreach ($file in 'SuperwhisperWoW.exe','uninstall.ps1','wow-vocabulary.csv') {
    Copy-Item -LiteralPath "$PSScriptRoot/$file" -Destination $install -Force
}
$run = 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Run'
if (!(Test-Path $run)) { New-Item -Path $run -Force | Out-Null }
New-ItemProperty -Path $run -Name 'SuperwhisperWoW' -Value ('"' + $exe + '"') -PropertyType String -Force | Out-Null
Start-Process -FilePath $exe
Write-Output "Companion installed and enabled at sign-in. Vocabulary and uninstaller: $install"
