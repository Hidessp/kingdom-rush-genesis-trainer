$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
& $compiler /nologo /target:winexe /optimize+ /out:"$PSScriptRoot\KR6Trainer.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "$PSScriptRoot\src\Trainer.cs"
if ($LASTEXITCODE -ne 0) { throw 'Trainer build failed' }
