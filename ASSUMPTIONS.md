# Assumptions

1. The booth workflow must be real and local-first, so the desktop booth app is the system of record during capture even when the cloud API is unavailable.
2. Tauri is the correct desktop shell even though it requires a Rust backend; TypeScript remains the dominant application language in all shared/product layers.
3. digiCamControl is the most realistic Windows-first tethered bridge available without bundling proprietary vendor SDK binaries directly in this repo.
4. Sony remains the highest-priority vendor path, but a real Sony implementation still requires a separate local bridge based on Sony Camera Remote SDK components that are not shipped here.
5. Canon/Nikon/Fujifilm support is scaffolded intentionally and surfaced as unsupported until vendor bridge work is completed.
6. Local metadata uses IndexedDB in the booth UI for immediate offline operation; the cloud relational schema lives in D1 and is shared/documented through `packages/db`.
7. Print preview/export is real today; deep native spooler integration is left for a later packaging/hardening phase.
8. The admin dashboard uses seeded auth/data locally so the product can be reviewed without first provisioning Cloudflare.
9. Cloudflare deployment is a later phase, so the API and schema are shaped for Workers/D1/R2 but production secrets/resources are not embedded here.
10. The current machine used for this coding session does not have Node/pnpm/Rust on PATH, so the codebase was scaffolded and documented without executing installs or builds in-session.
