@echo off
setlocal
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET runtime was not found.
  echo Install .NET 9 Desktop Runtime x64 from Microsoft, then run Start-Photobooth-9x16.cmd again.
  pause
  exit /b 1
)
dotnet --list-runtimes | findstr /i "Microsoft.WindowsDesktop.App 9." >nul
if errorlevel 1 (
  echo .NET 9 Desktop Runtime is missing.
  echo Install .NET 9 Desktop Runtime x64 from Microsoft.
  pause
  exit /b 1
)
echo .NET 9 Desktop Runtime found.
echo You can run Start-Photobooth-9x16.cmd.
pause
