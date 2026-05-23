Write-Host "Packaging booth desktop app..." -ForegroundColor Cyan
pnpm install
pnpm --filter @photobooth/booth-desktop tauri:build
