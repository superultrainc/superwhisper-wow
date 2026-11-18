param([string]$Output = "$PSScriptRoot/dist/package")
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $Output | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot/dist/SuperwhisperWoW.exe","$PSScriptRoot/install.ps1","$PSScriptRoot/uninstall.ps1","$PSScriptRoot/wow-vocabulary.csv" -Destination $Output -Force
Copy-Item -LiteralPath "$PSScriptRoot/../SuperwhisperWoW" -Destination $Output -Recurse -Force
