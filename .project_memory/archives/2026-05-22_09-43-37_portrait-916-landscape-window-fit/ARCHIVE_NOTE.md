# Archive Note

## Reason for Archive
Portrait 9:16 native booth opens too tall/sluggish on horizontal Windows displays; preserve current portrait app files before fitting the window shell to landscape desktops.

## User Request
Make the vertical version testable on a normal Windows horizontal display because it currently opens and feels stuck/sluggish.

## Files Saved
- apps/booth-windows-native-portrait-916/MainWindow.xaml
- apps/booth-windows-native-portrait-916/MainWindow.xaml.cs
- Launch-Photobooth-Native-9x16.cmd

## Current Known Working State
Portrait app builds and launches after prior startup fix, but default portrait window is 900x1600 with high minimum height, making it unsuitable for landscape desktop testing.

## Planned Change
Keep 9:16 preview/output behavior, but resize the app shell to a landscape-friendly desktop window and keep controls in a scrollable side panel.

## Rollback Instructions
Copy the archived files back to their original paths under D:\rocket\photobooth.

## Codex Rollback Instructions
Read ARCHIVE_INDEX.md, locate this archive, restore listed files only, and update PROJECT_MEMORY.md and CHAT_LOG.md.
