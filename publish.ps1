# Single run, two deliverables (nothing is uploaded anywhere):
#   publish\sixel.exe        standalone single-file CLI (win-x64, self-contained)
#   publish\nuget\*.nupkg    local NuGet packages Sixel and Sixel.Skia
# Optional: .\publish.ps1 -Version 0.2.0 sets the version of the exe and of both packages.
param([string]$Version)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Invoke-Step([string]$Name, [scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed (exit code $LASTEXITCODE)" }
}

$versionArgs = @()
if ($Version) { $versionArgs = @("-p:Version=$Version") }

Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue

Invoke-Step 'publish' { dotnet publish src/Sixel.Cli -p:PublishProfile=win-x64 --nologo -v q @versionArgs }
foreach ($project in 'src/Sixel', 'src/Sixel.Skia') {
    Invoke-Step "pack $project" { dotnet pack $project -c Release -o publish/nuget --nologo -v q @versionArgs }
}

Get-ChildItem publish -Recurse -File | ForEach-Object {
    '{0}  {1:N1} MB' -f $_.FullName.Substring((Resolve-Path publish).Path.Length + 1), ($_.Length / 1MB)
}
