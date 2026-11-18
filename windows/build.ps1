param([string]$OutDir = "$PSScriptRoot/dist")
$ErrorActionPreference = 'Stop'
$csc = "$env:WINDIR/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
New-Item -ItemType Directory -Force $OutDir | Out-Null
$sources = (Get-ChildItem "$PSScriptRoot/*.cs").FullName
& $csc /nologo /optimize+ /target:winexe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll "/out:$OutDir/SuperwhisperWoW.exe" $sources
if ($LASTEXITCODE) { throw "Companion compilation failed" }
