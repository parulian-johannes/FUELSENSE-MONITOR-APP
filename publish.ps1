# Publish FuelSense Monitor App as a self-contained, single-file EXE for Windows x64
param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

Write-Host "Publishing Engine Monitoring App (self-contained, single-file)..." -ForegroundColor Cyan

# Ensure we're in the project root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$publishProfile = ".\Properties\PublishProfiles\win-x64-selfcontained-singlefile.pubxml"

if ($Clean) {
    Write-Host "Cleaning previous publish output..." -ForegroundColor Yellow
    if (Test-Path .\publish\win-x64) { Remove-Item .\publish\win-x64 -Recurse -Force }
}

# Run publish
& dotnet publish .\EngineMonitoring.csproj -p:PublishProfile=$publishProfile

# Verify output
$outDir = Join-Path $scriptDir 'publish\\win-x64'
if (-not (Test-Path $outDir)) {
    throw "Publish folder not found: $outDir"
}

$exe = Get-ChildItem -Path $outDir -Filter '*.exe' | Select-Object -First 1
if ($null -eq $exe) {
    throw "EXE not found in $outDir"
}

Write-Host "Publish complete:" -ForegroundColor Green
Write-Host "  EXE: $($exe.FullName)" -ForegroundColor Green

Write-Host "Zip this folder and share it: $outDir" -ForegroundColor Yellow
