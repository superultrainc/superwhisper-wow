param([string]$OutDir)
$ErrorActionPreference = 'Stop'
$testDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Split-Path -Parent $testDir
if (!$OutDir) { $OutDir = Join-Path $sourceDir 'dist' }
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$fake = Join-Path $testDir 'FakeGame.cs'
$driver = Join-Path $testDir 'DesktopTest.cs'
$desktop = Join-Path $sourceDir 'Desktop.cs'
$recording = Join-Path $sourceDir 'Recording.cs'
& $csc /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$OutDir\Wow.exe" $fake
if ($LASTEXITCODE) { throw "Fixture compilation failed" }
& $csc /nologo /target:exe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$OutDir\DesktopTest.exe" $desktop $recording $driver
if ($LASTEXITCODE) { throw "Driver compilation failed" }
& (Join-Path $OutDir 'DesktopTest.exe') $OutDir
if ($LASTEXITCODE) { throw "Desktop fixture failed" }
