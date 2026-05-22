# Archive Index

## Archive Entry - 2026-05-03 23:22
- Archive Folder: `.project_memory/archives/2026-05-03_23-22-13_photobooth-sharing-site`
- Reason: Major `apps/admin-web` conversion into a 4-digit-code Photobooth sharing site
- Files Included: `apps/admin-web/package.json`, `apps/admin-web/app/page.tsx`, `apps/admin-web/app/layout.tsx`, `apps/admin-web/app/globals.css`, `apps/admin-web/app/gallery/[token]/page.tsx`, `apps/admin-web/app/(dashboard)/layout.tsx`, `apps/admin-web/lib/data.ts`, `apps/admin-web/lib/auth.ts`
- Safe Rollback Target: Restore archived files to their original app paths
- Notes: Archive created before implementing product frontend, upload APIs, local storage, and private gallery access

## Archive Entry - 2026-05-04 01:14
- Archive Folder: `.project_memory/archives/2026-05-04_01-14-50_share-upload-realtime`
- Reason: Major `apps/admin-web` upgrade for richer upload API, realtime gallery updates, and management features
- Files Included: `apps/admin-web/app/page.tsx`, `apps/admin-web/app/admin/upload/page.tsx`, `apps/admin-web/app/gallery/[code]/page.tsx`, `apps/admin-web/app/api/share/upload/route.ts`, `apps/admin-web/components/share/upload-form.tsx`, `apps/admin-web/components/share/gallery-client.tsx`, `apps/admin-web/lib/photo-share/store.ts`, `apps/admin-web/lib/photo-share/types.ts`
- Safe Rollback Target: Restore archived files to their original app paths
- Notes: Archive created before extending uploader metadata, polling-based live refresh, management, and download/export behavior

- Archive Folder: .project_memory/archives/2026-05-04_08-40-03_admin-management-security-pass
  - Reason: Add admin management UI and metadata auth guard after second-pass gap review
  - Files Included: apps/admin-web/lib/photo-share/store.ts; apps/admin-web/app/api/share/photo/[code]/[photoId]/metadata/route.ts; apps/admin-web/app/admin/upload/page.tsx
  - Safe Rollback Target: Restore these three files and rerun admin-web validation
  - Notes: Created after functional pass 1 exposed missing management UI and weak metadata authorization.


- Archive Folder: .project_memory/archives/2026-05-04_08-48-42_public-private-gallery-slices
  - Reason: Add explicit public/private gallery slices and secure private downloads
  - Files Included: apps/admin-web/app/page.tsx; apps/admin-web/app/access/page.tsx; apps/admin-web/lib/photo-share/store.ts; apps/admin-web/app/api/share/feed/route.ts; apps/admin-web/app/api/share/download/[token]/route.ts; apps/admin-web/app/api/share/export/[code]/route.ts; apps/admin-web/components/share/gallery-client.tsx
  - Safe Rollback Target: Restore these files and rerun admin-web validation
  - Notes: Created before implementing public gallery listing, private gallery password page, and tighter private asset authorization.


- Archive Folder: .project_memory/archives/2026-05-04_09-31-00_hosting-deployment-pass
  - Reason: Prepare production hosting, standalone build, and deployment automation for admin-web
  - Files Included: apps/admin-web/next.config.mjs; apps/admin-web/package.json; apps/admin-web/lib/photo-share/store.ts; packages/core/package.json; packages/db/package.json; packages/ui/package.json
  - Safe Rollback Target: Restore these files and rerun admin-web build/typecheck
  - Notes: Created before fixing BOM build blockers and adding cloud-hosting deployment assets.


## Archive Entry - 2026-05-04 20:42
- Archive Folder: `.project_memory/archives/2026-05-04_20-42-00_static-html-preview`
- Reason: Replace the old launcher-style HTML with a richer static Photo Booth website preview
- Files Included: `View-Photobooth-Share.html`
- Safe Rollback Target: Restore archived preview file to the project root
- Notes: Archive created before converting the file from a link launcher into a multi-screen visual mockup.

## Archive - 2026-05-22_09-43-37 portrait-916-landscape-window-fit
- Archive Folder: .project_memory/archives/2026-05-22_09-43-37_portrait-916-landscape-window-fit
- Reason: Preserve portrait 9:16 app files before making the app shell fit horizontal Windows desktops.
- Files Included: MainWindow.xaml, MainWindow.xaml.cs, Launch-Photobooth-Native-9x16.cmd
- Safe Rollback Target: Restore only the portrait 9:16 app and 9x16 launcher files listed in ARCHIVE_NOTE.md.
- Notes: Original horizontal native booth files are intentionally not included or modified.

## Archive - 2026-05-22_09-49-59 portrait-916-cpu-optimization
- Archive Folder: .project_memory/archives/2026-05-22_09-49-59_portrait-916-cpu-optimization
- Reason: Preserve portrait 9:16 performance-related files before CPU optimization.
- Files Included: MainWindow.xaml.cs, LivePhotoPreviewService.cs, DigiCamControlService.cs, CameraDetectionService.cs
- Safe Rollback Target: Restore only the portrait 9:16 performance-related files listed in ARCHIVE_NOTE.md.
- Notes: Original horizontal native booth files are intentionally not included or modified.

## Archive - 2026-05-22_10-03-51 portrait-916-header-clipping-fix
- Archive Folder: .project_memory/archives/2026-05-22_10-03-51_portrait-916-header-clipping-fix
- Reason: Preserve portrait 9:16 header layout before fixing clipped top-right controls.
- Files Included: MainWindow.xaml
- Safe Rollback Target: Restore only the portrait 9:16 MainWindow.xaml listed in ARCHIVE_NOTE.md.
- Notes: Original horizontal native booth files are intentionally not included or modified.

## Archive Entry - 2026-05-22 10:02
- Archive Folder: `.project_memory/archives/2026-05-22_10-02-00_cloudflare-pages-fullstack`
- Reason: Prepare Cloudflare Pages full-stack deployment target with Pages Functions, R2, and D1
- Files Included: `package.json`, `pnpm-workspace.yaml`, `turbo.json`, `PROJECT_MEMORY.md`
- Safe Rollback Target: Restore archived root config/memory files and remove newly added Cloudflare Pages app/docs if needed
- Notes: Created before adding the Cloudflare deployment app, Wrangler config, migrations, and docs.

## Archive Entry - 2026-05-22 10:23
- Archive Folder: `.project_memory/archives/2026-05-22_10-23-00_cloudflare-go-live`
- Reason: Live Cloudflare provisioning and deployment setup for `afmpdt.space`
- Files Included: `apps/cloudflare-pages/wrangler.toml`, `apps/cloudflare-pages/DEPLOYMENT.md`
- Safe Rollback Target: Restore archived config files locally; do not delete remote Cloudflare resources unless explicitly requested
- Notes: Created before writing real D1 database ID and deploying to Cloudflare Pages.
