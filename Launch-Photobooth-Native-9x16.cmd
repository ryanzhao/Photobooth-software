@echo off
setlocal
cd /d "%~dp0"
set "TARGET=D:\rocket\photobooth\apps\booth-windows-native-portrait-916\bin\Release\net9.0-windows\Photobooth.BoothNative.Portrait916.exe"
if not exist "%TARGET%" (
  echo Target executable not found:
  echo %TARGET%
  echo Build it first with:
  echo dotnet build apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release
  pause
  exit /b 1
)
start "" "%TARGET%"
exit /b 0
