Write-Host "Photobooth Windows tethered checklist" -ForegroundColor Cyan
Write-Host "1. Install digiCamControl." 
Write-Host "2. Connect the camera via USB and verify the camera appears inside digiCamControl." 
Write-Host "3. Enable the digiCamControl local web server." 
Write-Host "4. Visit http://127.0.0.1:5513/?slc=list&param1=cameras&param2= and confirm the camera name appears." 
Write-Host "5. Start the booth app and refresh devices." 
Write-Host "6. Connect the Windows Tethered (digiCamControl) provider." 
Write-Host "7. Start preview and trigger capture." 
Write-Host "" 
Write-Host "Optional Sony path:" -ForegroundColor Yellow
Write-Host "- Supply a local Sony Camera Remote SDK bridge at the URL configured by VITE_SONY_SDK_BRIDGE_URL." 
