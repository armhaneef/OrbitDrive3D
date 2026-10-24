@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Install the .NET 8 SDK first.
  pause
  exit /b 1
)
dotnet publish "OrbitDrive3D\OrbitDrive3D.csproj" -c Release -r win-x64 --self-contained true -o "publish-win-x64"
if errorlevel 1 (
  echo Publish failed. See the error above.
  pause
  exit /b 1
)
echo.
echo Ready: publish-win-x64\OrbitDrive3D.exe
echo Keep all files in publish-win-x64 together.
pause
