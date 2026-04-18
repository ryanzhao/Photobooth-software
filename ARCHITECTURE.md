# Architecture

## Monorepo

- `apps/booth-desktop`: Tauri + React booth operator surface
- `apps/admin-web`: Next.js App Router admin surface
- `apps/api`: Hono Worker API for Cloudflare/local dev
- `packages/core`: shared domain model and session/template/sync logic
- `packages/db`: D1 schema, migration, seeds, and dashboard snapshot
- `packages/camera-core`: provider contracts and registry
- `packages/camera-adapters`: concrete provider implementations
- `packages/image-engine`: rendering and editing pipeline
- `packages/print-engine`: print/export helpers
- `packages/ui`: reusable UI components

## Local-First Flow

1. Booth operator launches desktop app.
2. Camera provider discovery runs locally.
3. Operator connects a provider/device.
4. Preview starts via webcam media stream or tethered live view bridge.
5. Capture is triggered:
   - webcam provider takes a local frame capture
   - tethered provider sends a remote capture command and waits for imported files
6. Imported photos are associated with a locally stored session.
7. Operator adjusts non-destructive settings.
8. Template renderer produces a high-resolution layout locally.
9. Print preview/export occurs locally.
10. Sync jobs are queued for later upload to the Worker API.

## Camera Architecture

### Provider Contract

Providers implement:
- `listDevices()`
- `connect()` / `disconnect()`
- `getStatus()`
- `startLiveView()` / `stopLiveView()`
- `capturePhoto()` for webcam-only paths
- `triggerRemoteShutter()` for real tethered capture
- `setParameter()` / `getSupportedParameters()`

### Concrete Providers

- `SonyRemoteSdkProvider`
  - first-class path for a user-supplied Sony bridge service
  - intended to expose real Sony SDK live view, remote trigger, and parameter control
- `DigicamControlProvider`
  - Windows-first real tethered path today
  - talks to digiCamControl over its local web/command server
  - supports discovery, camera selection, live view URL, trigger, file transfer, and parameter inspection
- `WebcamCameraProvider`
  - fully working fallback
  - intentionally separated from tethered capture logic
- `Canon/Nikon/Fujifilm Scaffold Providers`
  - explicit placeholders with diagnostics and unsupported capability flags

## Local Storage Strategy

### Booth App

- Metadata: IndexedDB via Dexie
- Session assets: filesystem session folders under the configured booth session root
- Rendered outputs: stored locally first, then queued for sync

### Cloud Target

- D1: relational sync/admin data
- R2: originals, rendered assets, thumbnails
- Worker API: auth, sync ingest, gallery reads, analytics aggregation

## Data Model

Implemented schema tables:
- `users`
- `booths`
- `devices`
- `events`
- `sessions`
- `session_photos`
- `templates`
- `rendered_outputs`
- `print_jobs`
- `upload_jobs`
- `public_galleries`
- `share_tokens`
- `audit_logs`

See `packages/db/src/schema.ts` and `packages/db/migrations/0000_initial.sql`.

## Sync Model

- Booth creates upload jobs locally.
- Sync jobs are retried separately from booth capture.
- Failed uploads remain visible in booth UI and admin UI.
- Booth capture/edit/render/print continue even if the API is down.

## Deployment Shape

- `apps/api` deploys directly to Cloudflare Workers.
- `apps/admin-web` is intended for Cloudflare Workers/OpenNext rather than static-only Pages.
- `apps/booth-desktop` packages through Tauri into Windows/macOS installers.
