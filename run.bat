@echo off
echo Building and running FuelSense Monitor App...
cd /d "d:\FUELSENSE MONITOR APP"
dotnet build FuelsenseMonitorApp.csproj
if %errorlevel% equ 0 (
    echo Build successful! Starting application...
    dotnet run --project FuelsenseMonitorApp.csproj
) else (
    echo Build failed! Please check errors above.
    pause
)