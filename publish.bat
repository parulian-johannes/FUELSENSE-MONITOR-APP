@echo off
setlocal enabledelayedexpansion

REM Publish Engine Monitoring App as a self-contained, single-file EXE for Windows x64

set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

set PROFILE=Properties\PublishProfiles\win-x64-selfcontained-singlefile.pubxml

if /I "%1"=="/clean" (
  echo Cleaning previous publish output...
  if exist publish\win-x64 rd /s /q publish\win-x64
)

echo Publishing Engine Monitoring App (self-contained, single-file)...
 dotnet publish EngineMonitoring.csproj -p:PublishProfile=%PROFILE%
if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

set OUTDIR=%SCRIPT_DIR%publish\win-x64
if not exist "%OUTDIR%" (
  echo Publish folder not found: %OUTDIR%
  exit /b 1
)

for /f "delims=" %%F in ('dir /b /a:-d "%OUTDIR%\*.exe" ^| findstr /i ".exe"') do set EXE=%%F & goto :found
:found
if not defined EXE (
  echo EXE not found in %OUTDIR%
  exit /b 1
)

echo Publish complete:
echo   EXE: %OUTDIR%\%EXE%

echo Zip this folder and share it: %OUTDIR%
endlocal
exit /b 0
