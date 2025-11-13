# Engine Monitoring App Runner
Write-Host "Building and running Engine Monitoring App..." -ForegroundColor Green

Set-Location "d:\FUELSENSE MONITOR APP"

Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build EngineMonitoring.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Starting application..." -ForegroundColor Green
    dotnet run --project EngineMonitoring.csproj
} else {
    Write-Host "Build failed! Please check errors above." -ForegroundColor Red
    Read-Host "Press Enter to exit"
}