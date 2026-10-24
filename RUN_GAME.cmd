@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Install the .NET 8 SDK, then reopen this file.
  echo https://dotnet.microsoft.com/en-us/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet run --project "OrbitDrive3D\OrbitDrive3D.csproj" --configuration Release
if errorlevel 1 (
  echo.
  echo Build or launch failed. See the error above and START_HERE_TAMIL.txt.
  pause
  exit /b 1
)
