@echo off
setlocal
cd /d "%~dp0"
set "TARGET=%~dp0app\Photobooth.BoothNative.exe"
if not exist "%TARGET%" (
  echo Target executable not found:
  echo %TARGET%
  pause
  exit /b 1
)
start "" "%TARGET%"
exit /b 0
