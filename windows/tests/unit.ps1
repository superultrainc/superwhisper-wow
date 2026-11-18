param([string]$OutDir)
$ErrorActionPreference = 'Stop'
$testDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Split-Path -Parent $testDir
if (!$OutDir) { $OutDir = Join-Path $sourceDir 'dist' }
$sources = @((Get-ChildItem -LiteralPath $sourceDir -Filter '*.cs').FullName)
$sources += (Get-Item -LiteralPath (Join-Path $testDir 'Unit.cs')).FullName
& (Join-Path $sourceDir 'build.ps1') -OutDir $OutDir
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$unit = Join-Path $OutDir 'Unit.exe'
Write-Output ("Unit sources: " + ($sources -join ', '))
& $csc /nologo /target:exe /platform:x64 /main:SuperwhisperWoW.Unit /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$unit" @sources
if ($LASTEXITCODE) { throw "Unit compilation failed" }
& $unit
if ($LASTEXITCODE) { throw "Unit checks failed" }
