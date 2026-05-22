# Archive Note

## Reason for Archive
Header controls in the portrait 9:16 app are clipped on the right when the window is scaled or narrow.

## User Request
Fix the bug where the top-right area is not correctly scaled and buttons are hidden.

## Files Saved
- apps/booth-windows-native-portrait-916/MainWindow.xaml

## Current Known Working State
Portrait app builds and runs; header top-right controls can be clipped because the DockPanel lets left header text consume too much space.

## Planned Change
Replace header DockPanel layout with a Grid and place right-side controls inside a horizontal ScrollViewer so controls remain accessible.

## Rollback Instructions
Copy the archived MainWindow.xaml back to apps/booth-windows-native-portrait-916/MainWindow.xaml.

## Codex Rollback Instructions
Read ARCHIVE_INDEX.md, locate this archive, restore the listed file, then update PROJECT_MEMORY.md and CHAT_LOG.md.
