@echo off
setlocal
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET is not installed.
  echo Install .NET 9 Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/9.0
  pause
  exit /b 1
)
dotnet --list-runtimes | findstr /i "Microsoft.WindowsDesktop.App 9." >nul
if errorlevel 1 (
  echo .NET 9 Desktop Runtime is missing.
  echo Install it from: https://dotnet.microsoft.com/download/dotnet/9.0
  pause
  exit /b 1
)
echo .NET Desktop Runtime found.
pause
