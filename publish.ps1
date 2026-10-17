# Builds the standalone single-file CLI into .\publish\sixel.exe (win-x64, self-contained).
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish src/Sixel.Cli -p:PublishProfile=win-x64 --nologo -v q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Get-ChildItem publish | ForEach-Object { '{0}  {1:N1} MB' -f $_.Name, ($_.Length / 1MB) }
