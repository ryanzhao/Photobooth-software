# Archive Note

## Reason for Archive
User reports high CPU and UI lag on Windows when using the portrait 9:16 Photo Booth version.

## User Request
Optimize the software because CPU usage is too high and the app becomes sluggish on Windows.

## Files Saved
- apps/booth-windows-native-portrait-916/MainWindow.xaml.cs
- apps/booth-windows-native-portrait-916/LivePhotoPreviewService.cs
- apps/booth-windows-native-portrait-916/DigiCamControlService.cs
- apps/booth-windows-native-portrait-916/CameraDetectionService.cs

## Current Known Working State
Portrait app builds and launches; window is landscape-test-friendly at 1280x900, but preview polling/rendering may cause high CPU.

## Planned Change
Reduce live preview polling cost, downsample preview decoding, cache guide overlays, avoid camera live polling in upload/gallery modes, and keep original capture/control behavior reachable.

## Rollback Instructions
Copy the archived files back to their original paths under D:\rocket\photobooth.

## Codex Rollback Instructions
Read ARCHIVE_INDEX.md, locate this archive, restore listed files only, then update PROJECT_MEMORY.md and CHAT_LOG.md.
