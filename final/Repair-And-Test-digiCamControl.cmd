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
  "$temp=$env:TEMP; $live=Join-Path $temp 'photobooth-liveview.jpg'; $preview=Join-Path $temp 'photobooth-preview.jpg'; try { Invoke-WebRequest 'http://127.0.0.1:5513/liveview.html?CMD=LiveViewWnd_Show' -UseBasicParsing -TimeoutSec 5 | Out-Null } catch {}; Start-Sleep -Seconds 1; try { Invoke-WebRequest 'http://127.0.0.1:5513/liveview.jpg' -UseBasicParsing -TimeoutSec 5 -OutFile $live; $liveItem=Get-Item $live; if($liveItem.Length -gt 128){ Write-Output ('LIVEVIEW_FRAME_OK ' + $liveItem.Length + ' bytes -> ' + $liveItem.FullName) } else { Write-Output ('LIVEVIEW_FRAME_EMPTY ' + $liveItem.Length + ' bytes. Bridge is running, but the camera Live View stream is not outputting frames.') } } catch { Write-Output ('LIVEVIEW_FAILED ' + $_.Exception.Message) }; try { Invoke-WebRequest 'http://127.0.0.1:5513/preview.jpg' -UseBasicParsing -TimeoutSec 5 -OutFile $preview; $previewItem=Get-Item $preview; Write-Output ('PREVIEW_STATIC ' + $previewItem.Length + ' bytes -> ' + $previewItem.FullName) } catch { Write-Output ('PREVIEW_FAILED ' + $_.Exception.Message) }"

echo.
echo Starting Photo Booth...
start "" "%~dp0app\Photobooth.BoothNative.exe"
echo.
echo If BRIDGE_FAILED appears above, digiCamControl did not start its web bridge.
echo If LIVEVIEW_FRAME_EMPTY appears, the files are present but the camera is not outputting live frames.
echo In that case, open digiCamControl, select the camera, start Live View there once, then restart Photo Booth.
pause
