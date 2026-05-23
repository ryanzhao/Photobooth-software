Photo Booth Final Runtime Package
==================================

Target path on every computer:
D:\rocket\photobooth\final

Start:
1. Open CMD
2. cd /d D:\rocket\photobooth\final
3. Launch-Photobooth-Native.cmd

If Live View does not start:
1. Close Photo Booth and digiCamControl
2. cd /d D:\rocket\photobooth\final
3. Repair-And-Test-digiCamControl.cmd
4. Launch-Photobooth-Native.cmd

First run Windows Firewall prompt:
- If Windows asks whether to allow digiCamControl network access, click Allow.
- This is required for the local bridge at http://127.0.0.1:5513.
- If you click Cancel, close Photo Booth/digiCamControl and run Repair-And-Test-digiCamControl.cmd.

Live View test results:
- LIVEVIEW_FRAME_OK means realtime frames are being produced.
- LIVEVIEW_FRAME_EMPTY means the files and bridge are present, but the camera/digiCamControl Live View session is not outputting frames yet.
- PREVIEW_STATIC is not realtime Live View; it is only a static preview endpoint.

Included:
- app\Photobooth.BoothNative.exe
- app\*.dll runtime files
- app\assets templates/effects
- tools\digitcamcontrol\CameraControl.exe
- tools\digitcamcontrol\WebServer\liveview.html
- launcher and repair scripts

Do not move app or tools out of this folder. Keep this folder structure intact.
