@echo off
echo Building and running Engine Monitoring App...
cd /d "d:\FUELSENSE MONITOR APP"
dotnet build EngineMonitoring.csproj
if %errorlevel% equ 0 (
    echo Build successful! Starting application...
    dotnet run --project EngineMonitoring.csproj
) else (
    echo Build failed! Please check errors above.
    pause
)