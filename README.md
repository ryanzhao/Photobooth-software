# Photobooth Platform

Production-oriented, local-first photobooth monorepo with a Tauri desktop booth app, a Next.js admin dashboard, and a Cloudflare Worker API.

## What Is In This Repo

- `apps/booth-desktop`: original Tauri booth implementation kept for cross-platform architecture reference.`r`n- `apps/booth-windows-native`: Windows-native WPF booth executable that can be launched directly as an `.exe`.
- `apps/admin-web`: protected admin dashboard with seeded auth, sessions/devices/sync/templates/views, and public gallery pages.
- `apps/api`: Hono-based Worker API for login, session sync, admin snapshot reads, and public gallery responses.
- `packages/core`: shared domain models, session helpers, sync helpers, and default template definitions.
- `packages/db`: D1-ready schema, SQL migration, and seed/demo snapshot data.
- `packages/camera-core` / `packages/camera-adapters`: provider contracts plus webcam, digiCamControl, Sony bridge, and vendor scaffolds.
- `packages/image-engine`: browser-side template renderer with QR generation and non-destructive filter pipeline.
- `packages/print-engine`: JPG/PDF artifact builder and browser print preview helper.
- `packages/ui`: shared Tailwind/shadcn-style UI primitives.

## Prerequisites

- Node.js 22+
- pnpm 10+
- Rust toolchain + Cargo
- Tauri prerequisites for Windows/macOS packaging
- Cloudflare account + Wrangler for API/web deployment later
- For real Windows tethered capture: digiCamControl installed and its local web server enabled, or a Sony Camera Remote SDK bridge you supply separately

## Local Setup

1. Copy environment files:
   - `cp .env.example .env`
   - `cp apps/booth-desktop/.env.example apps/booth-desktop/.env`
   - `cp apps/admin-web/.env.example apps/admin-web/.env.local`
2. Install dependencies:
   - `pnpm install`
3. Prepare local folders:
   - `node ./scripts/seed-local.mjs`
4. Start the API:
   - `pnpm dev:api`
5. Start the admin dashboard:
   - `pnpm dev:admin`
6. Start the booth shell in browser mode:
   - `pnpm dev:booth`
7. Start the booth app in Tauri desktop mode when Rust/Tauri are installed:
   - `pnpm --filter @photobooth/booth-desktop tauri:dev`

## Local Review Steps

1. Open the booth UI at `http://localhost:1420` or run the Tauri desktop app.
2. Refresh devices.
3. For webcam MVP validation, connect the webcam provider and start preview.
4. For tethered validation, run digiCamControl, enable its web server, connect your camera by USB, then refresh devices and connect the `Windows Tethered (digiCamControl)` provider.
5. Trigger capture. The booth app creates a local session, waits through countdown, captures/imports, then surfaces the images for editing.
6. Adjust brightness/contrast/saturation/warmth/beauty controls.
7. Render the selected template, open print preview, and queue sync.
8. Open the admin dashboard at `http://localhost:3000`, log in with the seeded admin account, and review sessions/devices/sync/templates.
9. Open the public gallery route from the Galleries page.

## Seed Login Accounts

- Admin: `admin@photobooth.local` / `admin1234`
- Operator: `operator@photobooth.local` / `operator1234`

## Booth App Usage

- Device discovery and connection are provider-based.
- Webcam provider is fully runnable for local MVP testing.
- digiCamControl provider is the Windows-first real tethered path and uses its local command/web interface.
- Sony provider is a first-class bridge path that becomes active when you supply a local Sony bridge service.
- Session data is stored locally first; sync jobs are queued separately and do not block booth operation.

## API Usage

- `POST /auth/login`: returns a JWT for seeded accounts.
- `GET /admin/overview`: protected admin snapshot.
- `POST /sync/session`: booth session sync ingestion.
- `GET /public/gallery/:token`: public gallery payload.

## Environment Variables

Root `.env.example`:
- `NEXT_PUBLIC_API_BASE_URL`
- `VITE_API_BASE_URL`
- `VITE_SESSION_ROOT`
- `VITE_DIGICAMCONTROL_BASE_URL`
- `VITE_SONY_SDK_BRIDGE_URL`
- `JWT_SECRET`
- `CF_ACCOUNT_ID`
- `CF_DATABASE_ID`
- `CF_R2_BUCKET`

Booth desktop `apps/booth-desktop/.env.example`:
- `VITE_API_BASE_URL`
- `VITE_SESSION_ROOT`
- `VITE_DIGICAMCONTROL_BASE_URL`
- `VITE_SONY_SDK_BRIDGE_URL`

Admin web `apps/admin-web/.env.example`:
- `NEXT_PUBLIC_API_BASE_URL`
- `JWT_SECRET`

## Cloudflare Deployment Steps

### API Worker

1. Authenticate Wrangler: `npx wrangler login`
2. Create D1 and R2 resources.
3. Update `apps/api/wrangler.jsonc` with the real `database_id` and bucket name.
4. Apply `packages/db/migrations/0000_initial.sql` to D1.
5. Deploy the API:
   - `pnpm --filter @photobooth/api build`
   - `pnpm --filter @photobooth/api dev` for local Worker validation
   - `npx wrangler deploy --config apps/api/wrangler.jsonc`

### Admin Web

Recommended target: Cloudflare Workers with OpenNext rather than static Pages export, because this repo uses SSR and protected routes.

1. Install the Cloudflare Next.js deployment adapter you choose.
2. Build the Next.js app with the Cloudflare adapter.
3. Point `NEXT_PUBLIC_API_BASE_URL` to the deployed Worker API.
4. Deploy the built admin Worker/app using your selected OpenNext-on-Cloudflare workflow.

## Windows Tethered Testing Steps

1. Install digiCamControl on the booth PC.
2. Connect the Sony or other supported camera over USB.
3. In digiCamControl, enable the local web server.
4. Confirm the bridge responds:
   - `http://127.0.0.1:5513/?slc=list&param1=cameras&param2=`
5. Start the booth app and refresh devices.
6. Connect `Windows Tethered (digiCamControl)`.
7. Start preview.
8. Trigger capture. The app sends a real tethered command, waits for imported files, then stores the capture under the session folder.
9. Review the imported files under the local session path shown in the booth UI.

## Packaging Steps

### Windows

1. Install Rust and Tauri prerequisites.
2. Run `pnpm install`.
3. Run `pnpm --filter @photobooth/booth-desktop tauri:build`.
4. Collect the generated `.msi` under `apps/booth-desktop/src-tauri/target/release/bundle/msi/`.

### macOS

1. Install Xcode command-line tools and Tauri prerequisites.
2. Run `pnpm install`.
3. Run `pnpm --filter @photobooth/booth-desktop tauri:build` on macOS.
4. Collect the generated `.dmg` bundle.

## Real vs Scaffolded

Implemented and locally reviewable now:`r`n- Monorepo structure and shared types`r`n- Windows-native WPF booth executable shell`r`n- Tauri booth shell
- Webcam provider
- Windows-first digiCamControl tethered provider path
- Local session storage and history
- Non-destructive edit controls
- JSON template definitions and renderer
- Print preview and sync queue handling
- Admin dashboard and public gallery
- Cloudflare Worker API shell, schema, and migration

Still hardware/vendor specific:
- Sony Camera Remote SDK bridge executable/service
- Canon/Nikon/Fujifilm vendor bridges
- Native OS print spooler integration beyond browser/system print preview
- Production D1/R2 persistence wiring and admin-to-API auth unification


