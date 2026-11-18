param([string]$OutDir = "$PSScriptRoot/../dist")
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/../build.ps1" -OutDir $OutDir
$csc = "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
$sources = (Get-ChildItem "$PSScriptRoot/../*.cs").FullName
& $csc /nologo /target:exe /platform:x64 /main:SuperwhisperWoW.Unit /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$OutDir/Unit.exe" $sources "$PSScriptRoot/Unit.cs"
if ($LASTEXITCODE) { throw "Unit compilation failed" }
& "$OutDir/Unit.exe"
if ($LASTEXITCODE) { throw "Unit checks failed" }
