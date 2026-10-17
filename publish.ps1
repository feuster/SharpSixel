# Single run, three deliverables (Release only, nothing is uploaded anywhere):
#   publish\SharpSixel.exe     standalone single-file CLI (win-x64, self-contained)
#   publish\README.txt         user manual, copied from Docs\README.txt
#   publish\nuget\*.nupkg      local NuGet packages SharpSixel and SharpSixel.Skia
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

if (-not (Test-Path Docs\README.txt)) { throw 'Docs\README.txt is missing.' }

Remove-Item publish -Recurse -Force -ErrorAction SilentlyContinue

Invoke-Step 'publish' { dotnet publish src/SharpSixel.Cli -c Release -p:PublishProfile=win-x64 --nologo -v q @versionArgs }
foreach ($project in 'src/SharpSixel', 'src/SharpSixel.Skia') {
    Invoke-Step "pack $project" { dotnet pack $project -c Release -o publish/nuget --nologo -v q @versionArgs }
}

Get-ChildItem publish -Recurse -File | ForEach-Object {
    '{0}  {1:N1} MB' -f $_.FullName.Substring((Resolve-Path publish).Path.Length + 1), ($_.Length / 1MB)
}
