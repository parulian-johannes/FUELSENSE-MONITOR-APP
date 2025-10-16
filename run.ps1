# FuelSense Monitor App Runner
Write-Host "Building and running FuelSense Monitor App..." -ForegroundColor Green

Set-Location "d:\FUELSENSE MONITOR APP"

Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build FuelsenseMonitorApp.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Starting application..." -ForegroundColor Green
    dotnet run --project FuelsenseMonitorApp.csproj
} else {
    Write-Host "Build failed! Please check errors above." -ForegroundColor Red
    Read-Host "Press Enter to exit"
}