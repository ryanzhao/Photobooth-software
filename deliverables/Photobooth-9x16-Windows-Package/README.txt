# Photobooth 9x16 Windows Package

This folder is the portable copy target for the portrait 9:16 Windows Photo Booth app.

## How to run

1. Copy this whole folder to the Windows computer you want to use.
2. Run `Check-Runtime.cmd` first.
3. If the check passes, run `Start-Photobooth-9x16.cmd`.

## Runtime requirement

This package is framework-dependent because this development machine has no NuGet package source configured, so a fully self-contained publish could not download the .NET runtime packs.

Install this on the target computer if needed:
- .NET 9 Desktop Runtime x64

## Included content

- `app/` - built portrait 9:16 Photobooth app and assets.
- `tools/digitcamcontrol/` - bundled digiCamControl folder from this project, used for tethered camera workflow when available.
- `Start-Photobooth-9x16.cmd` - launcher for this package.
- `Check-Runtime.cmd` - checks whether .NET 9 Desktop Runtime is installed.

## Notes

- Upload/gallery/photo-effect test mode can run without a camera.
- Camera capture/live view needs digiCamControl and a compatible tethered camera.
- Keep the folder structure unchanged when moving it to another computer.
