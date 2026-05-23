@echo off
setlocal
cd /d "%~dp0"
set "DIGI=%~dp0tools\digitcamcontrol\CameraControl.exe"

echo === Photo Booth digiCamControl repair/test ===
echo Package folder: %~dp0
echo Expected digiCamControl: %DIGI%
echo.

if not exist "%DIGI%" (
  echo ERROR: CameraControl.exe was not found.
  echo Run: git lfs pull
  pause
  exit /b 1
)

echo Closing old CameraControl and Photo Booth processes...
taskkill /F /IM CameraControl.exe >nul 2>nul
taskkill /F /IM Photobooth.BoothNative.exe >nul 2>nul
timeout /t 2 /nobreak >nul

echo Starting digiCamControl from bundled tools folder...
start "digiCamControl" /D "%~dp0tools\digitcamcontrol" "%DIGI%"

echo Waiting for digiCamControl web bridge on port 5513...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ok=$false; for($i=0;$i -lt 30;$i++){ try { $r=Invoke-WebRequest 'http://127.0.0.1:5513/' -UseBasicParsing -TimeoutSec 2; if($r.StatusCode -eq 200){ $ok=$true; break } } catch {}; Start-Sleep -Seconds 1 }; if($ok){ 'BRIDGE_OK' } else { 'BRIDGE_FAILED' }"

echo.
echo Port check:
netstat -ano | findstr :5513
echo.

echo Camera list check:
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "try { Invoke-WebRequest 'http://127.0.0.1:5513/?slc=list^&param1=cameras^&param2=' -UseBasicParsing -TimeoutSec 5 | Select-Object -ExpandProperty Content } catch { $_.Exception.Message }"

echo.
echo Live View command check:
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "try { Invoke-WebRequest 'http://127.0.0.1:5513/?CMD=LiveViewWnd_Show' -UseBasicParsing -TimeoutSec 5 | Select-Object -ExpandProperty Content } catch { $_.Exception.Message }"

echo.
echo Image endpoint check:
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$temp=$env:TEMP; foreach($name in 'liveview.jpg','preview.jpg'){ $out=Join-Path $temp ('photobooth-' + $name); try { Invoke-WebRequest ('http://127.0.0.1:5513/' + $name) -UseBasicParsing -TimeoutSec 5 -OutFile $out; $item=Get-Item $out; Write-Output ($name + ' OK ' + $item.Length + ' bytes -> ' + $item.FullName) } catch { Write-Output ($name + ' FAILED ' + $_.Exception.Message) } }"

echo.
echo Starting Photo Booth...
start "" "%~dp0app\Photobooth.BoothNative.exe"
echo.
echo If BRIDGE_FAILED appears above, digiCamControl did not start its web bridge.
echo If image endpoints are small/gray, open digiCamControl and enable camera Live View there once.
pause
