param([string]$OutDir = "$PSScriptRoot/../dist")
$ErrorActionPreference = 'Stop'
$csc = "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
& $csc /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$OutDir/Wow.exe" "$PSScriptRoot/FakeGame.cs"
if ($LASTEXITCODE) { throw "Fixture compilation failed" }
& $csc /nologo /target:exe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$OutDir/DesktopTest.exe" "$PSScriptRoot/../Desktop.cs" "$PSScriptRoot/../Recording.cs" "$PSScriptRoot/DesktopTest.cs"
if ($LASTEXITCODE) { throw "Driver compilation failed" }
& "$OutDir/DesktopTest.exe" $OutDir
if ($LASTEXITCODE) { throw "Desktop fixture failed" }
