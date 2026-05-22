# PROJECT_MEMORY

## 1. Project Identity

- Project root: `D:\rocket\photobooth`
- Primary GitHub repo: `https://github.com/ryanzhao/Photobooth-software`
- Current branch used for initial import: `main`
- Initial imported commit created locally and pushed earlier: `d133972`

This repository contains multiple booth-related implementations:

- `apps/booth-windows-native`
  - Windows-first WPF/.NET native booth app
  - This became the main practical booth app during iteration
- `apps/booth-desktop`
  - Cross-platform Tauri + React booth app
  - Used as the base for the later `mac_version`
- `apps/admin-web`
  - Next.js admin dashboard
- `apps/api`
  - Cloudflare Worker API shell
- `packages/*`
  - shared camera, core, image, print, db, ui packages

## 2. Original Product Goal

The requested product was a real local-first photobooth system with:

- a local booth desktop app
- a web admin dashboard
- a cloud/backend sync layer
- real tethered camera workflow
- local session storage
- template composition
- print/export
- later cloud sync

Critical product requirement:

- tethered capture must not be treated as webcam screenshot capture
- the booth must send a real remote capture command through a wired bridge
- after capture the image must transfer/import back to the machine

## 3. Major User Requests History

The following themes came up repeatedly and drove implementation:

1. Real Windows booth workflow first
- user wanted a real local booth workflow, not a fake web-only prototype
- Windows + USB tethered camera + local-first storage was top priority

2. Make it directly runnable
- user wanted easier startup and later asked for `.exe`-style usage
- this pushed work toward the Windows native booth app

3. UI and localization
- color palette changed to a light beige/white system
- booth UI was updated toward lighter styling
- Chinese/English switching was added

4. File archiving discipline
- user explicitly requested every modification to preserve prior state in `archive`
- many later changes were archived under timestamped folders

5. Camera detection and digiCamControl integration
- userâ€™s Sony/Canon tethered workflow did not auto-detect initially
- reference code under `D:\rocket\photobooth\reference` was requested as the source of truth for auto-detection behavior
- implementation moved toward Windows USB detection + digiCamControl bridge probing

6. Live preview and bridge troubleshooting
- repeated work was done around `5513` / `5514`
- booth eventually detected bridge cameras
- live preview intermittently failed because bridge endpoints were inconsistent or returned empty responses

7. Booth workflow features
- Capture button
- auto save
- photo gallery
- autofocus
- camera parameter controls
- launch digiCamControl from inside booth

8. Template pack / collage workflow
- user wanted real photobooth template behavior:
  - choose template
  - show empty slots
  - fill slots one photo at a time
  - only generate final composition after all required slots were filled
- later user asked for simpler controls and stronger real-time preview feedback

9. GitHub publishing
- local project was initialized as git
- ignore rules were expanded
- project was connected to GitHub and successfully pushed

10. Mac copy
- user requested a separate Mac-usable copy
- `mac_version` was created from the cross-platform Tauri booth app

## 4. Key Files and Areas That Became Important

### Windows native booth

- `apps/booth-windows-native/MainWindow.xaml`
- `apps/booth-windows-native/MainWindow.xaml.cs`
- `apps/booth-windows-native/DigiCamControlService.cs`
- `apps/booth-windows-native/CameraDetectionService.cs`
- `apps/booth-windows-native/BoothDataService.cs`
- `apps/booth-windows-native/Localization.cs`
- `apps/booth-windows-native/Models.cs`

### Cross-platform booth

- `apps/booth-desktop/src/App.tsx`
- `apps/booth-desktop/src/store/useBoothStore.ts`
- `apps/booth-desktop/src/services/camera/*`
- `apps/booth-desktop/src/services/storage/*`
- `apps/booth-desktop/src-tauri/tauri.conf.json`

### Shared template and image logic

- `packages/core/src/templates/default-templates.ts`
- `packages/image-engine/src/render/render-template.ts`

## 5. Windows Native Booth: What Was Implemented

The Windows native booth app became the most actively modified practical booth interface.

### 5.1 Camera and bridge

Implemented or partially implemented:

- bridge detection against digiCamControl web server
- Windows USB device probing
- bridge camera list probing via `http://127.0.0.1:5513/?slc=list&param1=cameras&param2=`
- live view window warm-up command
- autofocus command
- capture command through bridge
- import of transferred photo into session folder
- device diagnostics UI
- launch button for digiCamControl

Observed reality:

- `5513` camera listing often worked
- `liveview.jpg` sometimes returned `200` but zero-length content
- `5514` stream availability was inconsistent
- therefore â€œcamera detectedâ€ and â€œpreview actually usableâ€ were not always equivalent

### 5.2 Session and gallery

Implemented:

- local sessions under booth data
- `originals`, `processed`, `outputs` subfolders
- active session tracking
- recent session list
- gallery list
- display selected gallery image

### 5.3 UI and localization

Implemented:

- lighter beige/white visual direction
- Chinese / English language switching
- large control panel style suited to touch/floor usage

### 5.4 Template pack evolution

Template behavior went through several iterations:

#### Initial state
- template list existed only as display text
- no real selection behavior
- no composition workflow

#### First improvement
- template selection became interactive
- selected template persisted
- manual render button generated an output JPG into session `outputs`

#### Second improvement
- template preview area added
- empty slots shown before full capture
- progressive slot filling preview added
- final output deferred until all required photos for the selected template were present

#### Third improvement
- simplified board interaction
- added:
  - previous template
  - next template
  - reset board
  - retake last slot
- template selection now auto-updates `Shot Count`
- progress text added such as filled slots count

Current design intent:

- template preview reflects the target layout
- each captured image fills one slot in order
- final composition should only be created after slot count is complete

## 6. digiCamControl Path History

There was repeated path churn around the bridge location:

- at one point user moved camera control into:
  - `D:\rocket\photobooth\camcontrol`
- later path was restored/reworked
- final local bridge folder now present in project root:
  - `D:\rocket\photobooth\digitcamcontrol`

Important note:

- many issues were caused not by booth code alone but by digiCamControl runtime/config state
- especially web server binding and live view availability

## 7. Known Historical Problems

These problems came up multiple times and should be assumed as project memory:

1. Node / pnpm / corepack missing on user machine
- user repeatedly hit command-not-found issues for `node`, `npm`, `pnpm`, `corepack`

2. .NET / NuGet permission issues
- builds sometimes failed because user profile NuGet config was inaccessible
- workaround used local `NuGet.Config` and temporary `APPDATA` / `USERPROFILE`

3. Git ownership / auth issues
- repository initially not under git
- safe.directory issue happened because repo ownership differed between sandbox user and local user
- GitHub push later succeeded after resolving auth

4. Archive directory permissions
- archiving worked many times earlier
- later there were permission failures when creating new archive folders under current environment

5. Live preview instability
- bridge reachability and actual frame availability diverged
- empty `liveview.jpg` responses were observed

6. Encoding corruption in some .cs files
- some files, especially localization-related edits, showed mojibake / mixed encodings
- this made targeted patching more fragile

## 8. Git / Publishing Memory

Git was initialized locally in this project.

Key actions performed:

- `.gitignore` expanded to exclude:
  - `archive/`
  - `digitcamcontrol/`
  - temp folders
  - booth build output
- remote configured to:
  - `https://github.com/ryanzhao/Photobooth-software.git`
- initial commit created:
  - `d133972 Initial photobooth platform import`
- user later successfully pushed `main` to GitHub

## 9. Mac Version Memory

User requested a separate Mac-usable copy.

Created:

- `mac_version/`

Purpose:

- isolate the cross-platform Tauri booth app for macOS use
- avoid Windows native booth and digiCamControl dependencies

What `mac_version` includes:

- `apps/booth-desktop`
- necessary shared `packages/*`
- simplified root `package.json`
- Mac-focused `README.md`
- `run-mac.sh`
- `build-mac.sh`
- Tauri bundle targets changed to:
  - `app`
  - `dmg`

Important limitation:

- Mac version should currently be treated as webcam-first unless a Mac-specific tethered bridge is added later

## 10. Current Practical Startup Commands Memory

### Windows native booth

Bridge:

```cmd
cd /d F:\photobooth\digitcamcontrol
CameraControl.exe
```

Booth:

```cmd
cd /d F:\photobooth\apps\booth-windows-native\bin\Release\net9.0-windows
Photobooth.BoothNative.exe
```

### Mac version

From `mac_version` on a Mac:

```bash
pnpm install
pnpm tauri:dev
```

Build:

```bash
pnpm install
pnpm tauri:build
```

## 11. Design Intent That Must Be Preserved

These are the durable expectations that should guide future edits:

1. Local-first is mandatory
- booth must keep working without cloud

2. Tethered workflow is a real product requirement
- do not reduce it to webcam screenshots

3. Windows-first booth is the operational priority
- WPF/.NET native booth remains the practical operator-facing build

4. Template pack should behave like a real photobooth
- pick template
- show empty slots
- fill one slot per capture
- only export final composite when all required slots are filled

5. UX should stay simple for on-site use
- obvious buttons
- immediate feedback
- clear progress
- easy reset / retake

6. Preserve history when possible
- user explicitly asked for archiving of earlier states before modifications

## 12. Recommended Next Work

If work resumes, the highest-value next steps are:

1. Stabilize live preview chain
- treat empty `liveview.jpg` responses as a first-class failure mode
- confirm if MJPEG stream should be preferred over still polling
- verify digiCamControl config persistence and startup behavior

2. Clean template system structure
- move Windows native booth template definitions out of ad hoc UI code into a dedicated config structure
- align it more closely with shared template definitions in `packages/core`

3. Improve retake/session state model
- explicit current slot index
- explicit session completion state
- explicit final composite path

4. Clean encoding issues
- normalize file encoding for edited `.cs` files

5. Decide long-term booth strategy
- either continue investing in `apps/booth-windows-native`
- or migrate more operational workflow back into `apps/booth-desktop` for cross-platform reuse

## 13. Memory Update Rule

When future major work happens, update this file with:

- what changed
- why it changed
- which app became the active focus
- any environment-specific caveats
- exact startup/deployment commands if they changed

## Memory Entry â€” 2026-05-03 18:34

### User Request
User greeted and initiated a new task session in the `D:\rocket\photobooth` project.

### Work Completed
- Checked for the required local project memory file.
- Read existing `PROJECT_MEMORY.md` before making decisions.
- Confirmed current project root contents for session context.
- Appended this session entry to keep persistent local memory up to date.

### Files Created or Modified
- `PROJECT_MEMORY.md` â€” appended a new session memory entry.

### Technical Decisions
- No product or code changes were made in this task.
- Continued using `PROJECT_MEMORY.md` as the authoritative local project memory per `AGENTS.md`.

### Commands / Tests Run
- `Get-ChildItem -Force PROJECT_MEMORY.md` â€” confirmed memory file exists.
- `Get-Content PROJECT_MEMORY.md` â€” read existing project memory.
- `Get-Date -Format 'yyyy-MM-dd HH:mm'` â€” captured entry timestamp.
- `Get-ChildItem -Force` â€” inspected project root contents.

### Problems Found
- None in this task.

### Next Steps
- Wait for the next concrete user request.
- For future implementation work, continue reading and updating `PROJECT_MEMORY.md` at the start and end of each task.

## Memory Entry - 2026-05-03 18:46

### User Request
User asked to place a top-level launcher in `D:\rocket\photobooth` so the current native booth build can be started easily for testing.

### Work Completed
- Added a root-level Windows launcher script for the built native booth app.
- Added a short companion note describing what the launcher starts.
- Triggered the new launcher once to verify that it can be executed from the repository root.

### Files Created or Modified
- `Launch-Photobooth-Native.cmd` - root-level launcher that starts the built native booth executable.
- `Launch-Photobooth-Native.txt` - short note documenting the launcher target.
- `PROJECT_MEMORY.md` - appended this memory entry.

### Technical Decisions
- Chose a launcher script instead of copying the `.exe` itself to the root because the native app depends on adjacent runtime files in its build output folder.
- Kept the existing build artifacts in place and used the launcher only as an entry point.

### Commands / Tests Run
- `Get-ChildItem apps\\booth-windows-native\\bin\\Release\\net9.0-windows` - confirmed the built native executable and runtime files exist.
- `Start-Process -FilePath 'D:\\rocket\\photobooth\\Launch-Photobooth-Native.cmd'` - launched the new root-level launcher successfully.

### Problems Found
- No launcher creation problem found.
- Earlier runtime confirmation remained limited because the process did not stay visible in a simple `Get-Process` check after launch.

### Next Steps
- User can now double-click `D:\rocket\photobooth\Launch-Photobooth-Native.cmd` from the project root to begin testing.
- If the booth still opens and exits unexpectedly, inspect native startup logs or add a more diagnostic launcher.

## Memory Entry - 2026-05-03 21:06

### User Request
User asked to implement the production Photo Booth feature upgrade plan on the existing Windows native booth app, preserve the tethered capture workflow, create an archive before major edits, and deliver real multi-photo template, beauty, frame, preview, export, and print-ready behavior.

### Work Completed
- Read `PROJECT_MEMORY.md` first and continued the existing native-booth upgrade instead of rebuilding the project.
- Confirmed the active production target is `apps/booth-windows-native`.
- Verified the archive snapshot already created for this major change wave:
  - `archive/20260503-194040_native-production-upgrade`
- Implemented and integrated new native modules:
  - `TemplateManager`
  - `FrameOverlayManager`
  - `BeautyProcessor`
  - `CompositeRenderer`
  - `PrintService`
- Expanded native models for template slots, frame overlays, beauty levels, session metadata, raw/processed/final output paths, and render results.
- Reworked native session persistence in `BoothDataService` to support:
  - `sessions/{timestamp}/raw/`
  - `sessions/{timestamp}/processed/`
  - `sessions/{timestamp}/final/`
  - `session-metadata.json`
  - persisted selected template / frame / beauty level
  - deterministic slot indexing and final output paths
- Updated `MainWindow.xaml` and `MainWindow.xaml.cs` to add:
  - beauty level selector
  - frame selector
  - session progress UI
  - open final / print final / restart controls
  - live guide overlay image
  - template preview and final preview parity wiring
- Added structured default templates and starter frame assets.
- Backfilled verified source assets into `apps/booth-windows-native/assets/` so templates and frames are now versioned project assets instead of runtime-only generated files.
- Fixed compile blockers in `BeautyProcessor` and restored missing device-row rendering.
- Fixed startup/runtime crashes found during smoke testing:
  - `BoothDataService.LoadAsync()` no longer collides with `state.json` during read/write normalization
  - `CompositeRenderer.DrawImage()` no longer sets a WPF dependency property on a frozen `BitmapImage`
- Rebuilt official Release output so the root launcher now points to the upgraded booth build.

### Files Created or Modified
- `apps/booth-windows-native/Models.cs` - expanded native session, template, frame, beauty, and render models.
- `apps/booth-windows-native/TemplateManager.cs` - structured template catalog loader and default template definitions.
- `apps/booth-windows-native/FrameOverlayManager.cs` - frame catalog loader and starter PNG frame generation.
- `apps/booth-windows-native/BeautyProcessor.cs` - offline beauty-processing pipeline with configurable intensity.
- `apps/booth-windows-native/CompositeRenderer.cs` - unified preview/final composition and live guide overlay rendering.
- `apps/booth-windows-native/PrintService.cs` - final-image open/print integration.
- `apps/booth-windows-native/BoothDataService.cs` - session/state persistence refactor, metadata, locking, raw/processed/final storage.
- `apps/booth-windows-native/MainWindow.xaml` - native booth UI additions for beauty/frame/progress/final controls and live guide overlay.
- `apps/booth-windows-native/MainWindow.xaml.cs` - workflow orchestration for template-driven multi-shot capture, preview, export, retake/reset, and final-output actions.
- `apps/booth-windows-native/Localization.cs` - updated localization keys and template labels.
- `apps/booth-windows-native/booth-windows-native.csproj` - asset copy rules.
- `apps/booth-windows-native/assets/frames/*` - source frame assets and frame catalog.
- `apps/booth-windows-native/assets/templates/catalog.json` - source template catalog.
- `PROJECT_MEMORY.md` - appended this memory entry.

### Technical Decisions
- Kept the production target on the Windows native booth because it already contains the practical tethered digiCamControl workflow and local launch path.
- Preserved digiCamControl capture/live-bridge integration rather than replacing the camera path.
- Used processed images, not raw originals, as the source for template preview and final composite output.
- Made template-required slot count authoritative for session completion and auto-finalization.
- Scoped live preview enhancement to template/frame guidance overlay only; real-time beauty on the live tethered feed remains intentionally out of scope to avoid destabilizing preview performance.
- Kept the beauty system extensible and offline-safe. The current implementation uses the built-in fallback image-processing path; OpenCV cascade assets are prepared, but no OpenCV .NET package was added in this pass.
- Added an in-process state lock in `BoothDataService` to prevent overlapping reads/writes on `state.json` during startup and refresh.
- Added source-controlled `assets/templates` and `assets/frames` after validating the generated runtime assets, so later template/frame additions can be done by adding asset files and catalog entries.

### Commands / Tests Run
- Read memory / repo context:
  - `Get-Content PROJECT_MEMORY.md`
  - `rg --files apps/booth-windows-native`
  - `git status --short`
- Build verification:
  - `dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release`
  - `dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release -o D:\rocket\photobooth\.tmp\booth-native-build-check`
  - final result after fixes: `0 warnings, 0 errors`
- Smoke/startup validation:
  - launched `.tmp\booth-native-build-check\Photobooth.BoothNative.exe`
  - verified runtime-generated `assets/` and `booth-data/native-booth/`
  - captured Windows Application log errors to diagnose startup crashes
  - relaunched after fixes and confirmed `ALIVE=True` with `NO_NEW_APP_ERRORS`
  - launched official `apps\booth-windows-native\bin\Release\net9.0-windows\Photobooth.BoothNative.exe`
  - confirmed `ALIVE=True` with `NO_NEW_APP_ERRORS`
- Release rebuild / launcher target refresh:
  - stopped locked `Photobooth.BoothNative` release processes
  - rebuilt official Release output successfully
- Asset backfill:
  - copied verified generated assets into `apps\booth-windows-native\assets`

### Problems Found
- The original native upgrade pass left compile blockers in `BeautyProcessor` and missing `BuildDeviceRow` wiring.
- `MainWindow.xaml.cs` contained mojibake / corrupted Chinese UI strings from earlier encoding damage; most visible runtime/status strings were normalized in this pass.
- `BoothDataService.LoadAsync()` originally wrote back to `state.json` while still holding the read stream, causing startup crashes.
- `CompositeRenderer.DrawImage()` originally attempted to call `RenderOptions.SetBitmapScalingMode()` on a frozen `BitmapImage`, causing a WPF runtime crash when preview rendering touched existing session photos.
- Runtime session and state directories currently live under the app output folder. This is consistent with the existing native booth behavior, but long-term it may be preferable to move them to a dedicated writable app-data location outside `bin/Release`.
- OpenCV-based face-aware enhancement is still only partially prepared. The structure exists and cascade assets are provisioned, but the current beauty path remains the built-in offline fallback implementation.

### Next Steps
- Test the upgraded booth manually from the root launcher:
  - `D:\rocket\photobooth\Launch-Photobooth-Native.cmd`
- Validate the full tethered workflow with a real connected camera:
  - template selection
  - beauty level change
  - frame change
  - 4-photo and 6-photo sessions
  - slot-by-slot fill order
  - auto-finalization only after all required shots
  - open final / print final / restart / retake last
- If real-camera validation is stable, consider the next improvement pass:
  - per-session UI locking so changing global selectors cannot affect already-open historical sessions
  - optional retake-specific slot targeting beyond â€œretake lastâ€
  - true OpenCV face-aware enhancement if package restore and deployment constraints are acceptable
  - move native runtime session/state storage out of `bin/Release` into a dedicated app-data path

## Memory Entry - 2026-05-03 21:14

### User Request
User asked to find open-source photo effects / AR face-filter options for the photobooth project, specifically TikTok/Douyin-style effects such as dog ears, and explicitly referenced the Browser Use plugin.

### Work Completed
- Read the browser-use skill before attempting browser work.
- Attempted to bootstrap the in-app browser runtime with the required `iab` backend.
- Found that browser-use was not currently available because no Codex IAB backend was discoverable in this environment.
- Switched to read-only web/GitHub research and collected practical open-source candidates for face tracking, face filters, and sticker/overlay style effects.

### Files Created or Modified
- `PROJECT_MEMORY.md` - appended this research memory entry.

### Technical Decisions
- Did not modify product code in this task.
- Treated the browser plugin failure as an environment/tooling issue, not a project issue.
- Prioritized libraries/repositories that are actually open source and relevant to later integration into `apps/booth-windows-native`.
- Distinguished between:
  - full AR face-filter engines with examples/assets
  - lower-level landmark/face-tracking bases that would still require custom dog-ear/sticker assets and placement logic

### Commands / Tests Run
- `Get-Content D:\rocket\photobooth\PROJECT_MEMORY.md` - read project memory.
- `Get-Content C:\Users\PP a\.codex\plugins\cache\openai-bundled\browser-use\0.1.0-alpha1\skills\browser\SKILL.md` - read browser skill instructions.
- Node REPL browser bootstrap with `setupAtlasRuntime(... backend: "iab")` - failed because no IAB backend was discovered.
- Web research on GitHub / official docs for:
  - `jeeliz/jeelizFaceFilter`
  - `breathingcyborg/mediapipe-face-effects`
  - `hiukim/mind-ar-js`
  - `google-ai-edge/mediapipe`
  - smaller Python/OpenCV filter repos such as `Roodaki/RealTime-Webcam-Face-Filters`, `nuwandda/snapchat-filter-3d`, `Py-Contributors/Face_Filter`

### Problems Found
- Browser Use plugin could not connect: no in-app browser backend was available for `iab`.
- Many smaller face-filter repositories are demos or API wrappers rather than production-ready foundations.
- Several candidate repos do not clearly expose license text on the crawled page, so license status needs direct repo verification before product use.

### Next Steps
- If the user wants implementation inside the booth app, the strongest practical direction is:
  1. use MediaPipe/OpenCV for tracking in the native app
  2. use 2D/3D overlay assets for dog ears / stickers
  3. optionally study Jeeliz / MindAR demo logic for effect placement and interaction patterns
- If the browser plugin becomes available later, use it to inspect live demos visually before choosing the exact effect style.

## Memory Entry - 2026-05-03 23:00

### User Request
User asked to implement the Windows native booth effect/layout mode plan: add no-camera upload/gallery workflow, template-driven scrapbook/editorial outputs, filters/effects, preview/export parity, archive before edits, and validate through real UI interaction.

### Work Completed
- Re-read project memory and current native booth architecture before editing.
- Confirmed `PROJECT_MEMORY.md` is the only discoverable required persistent local memory/chat log file in this repo.
- Created a new pre-edit archive snapshot for this implementation wave.
- Identified the active target as `apps/booth-windows-native` and confirmed current template/session/preview infrastructure is already in place for incremental extension.

### Files Created or Modified
- `PROJECT_MEMORY.md` - appended this implementation-start entry.
- `archive/20260503-230046_photobooth-effect-mode/CHANGELOG.txt` - archive log for this edit wave.
- `archive/20260503-230046_photobooth-effect-mode/apps/booth-windows-native/*` - pre-edit snapshot of native booth files and assets.

### Technical Decisions
- Continue in the native WPF booth instead of switching to the Tauri booth.
- Treat `PROJECT_MEMORY.md` as the authoritative local memory/chat log because no separate concrete chat-log file path was discoverable.
- Keep existing camera capture/live-view workflow intact and add a parallel no-camera source mode.
- Use built-in starter assets for scrapbook/editorial styles while preserving JSON/catalog extensibility.

### Commands / Tests Run
- Read and inspected:
  - `PROJECT_MEMORY.md`
  - `README.md`
  - `ARCHITECTURE.md`
  - `apps/booth-windows-native/*`
- Repo/context checks:
  - `Get-ChildItem -Force`
  - `git status --short`
  - recursive file listing under `apps/booth-windows-native`
- Archive creation:
  - created `archive/20260503-230046_photobooth-effect-mode`
  - copied current native booth source/assets into the archive

### Problems Found
- `rg.exe` was not runnable in this environment due to access denied, so repo search had to fall back to PowerShell file inspection and `Select-String`.
- The native booth still has some prior mixed-encoding text damage in older strings, so edits need to avoid careless re-encoding.
- Browser-use plugin remained unavailable earlier because no IAB backend was discoverable, so implementation cannot depend on browser plugin availability.

### Next Steps
- Extend native models/session metadata for source mode, imported/gallery sources, effect presets, and assignment state.
- Expand template/assets schema for scrapbook/editorial layouts and decorative layers.
- Add upload/gallery source workflows to the native UI.
- Validate the result through real no-camera UI testing and two review passes.

## Memory Entry - 2026-05-03 23:45

### User Request
User asked to implement the Windows native booth no-camera effect/layout mode with upload, gallery selection, scrapbook/editorial templates, filters, preview/export parity, and real validation passes.

### Work Completed
- Extended native data models for source mode, source origin, source folder, effect presets, decorative layers, and richer template/session metadata.
- Added `EffectManager` and expanded `TemplateManager` to manage built-in scrapbook/editorial templates plus starter backgrounds, overlays, and sticker assets.
- Reworked `BoothDataService` to support:
  - `source/`, `processed/`, `final/` session folders
  - source-mode persistence
  - upload/gallery source import
  - manual slot assignment persistence
  - effect preset persistence
  - gallery recovery from existing processed files when legacy state has no `GalleryPhotos`
- Expanded `BeautyProcessor` to support style presets and fixed two runtime issues:
  - non-STA blur rendering path now runs through an STA dispatcher/thread
  - black-and-white preset no longer crashes on `Freeze()`
- Expanded `CompositeRenderer` to use effect presets and decorative layers in the same preview/final pipeline.
- Updated native UI (`MainWindow.xaml` / `.cs`) to add source mode, upload/gallery buttons, effect preset selection, assignment mode selection, and template-preview click handling for manual assignment.
- Ran build validation repeatedly until the native booth compiled cleanly again.
- Ran no-camera validation through a temporary local validation harness and generated real output files plus session metadata.

### Files Created or Modified
- `apps/booth-windows-native/Models.cs`
- `apps/booth-windows-native/TemplateManager.cs`
- `apps/booth-windows-native/EffectManager.cs`
- `apps/booth-windows-native/BoothDataService.cs`
- `apps/booth-windows-native/BeautyProcessor.cs`
- `apps/booth-windows-native/CompositeRenderer.cs`
- `apps/booth-windows-native/MainWindow.xaml`
- `apps/booth-windows-native/MainWindow.xaml.cs`
- `apps/booth-windows-native/Localization.cs`
- `PROJECT_MEMORY.md`

### Technical Decisions
- Preserved the native WPF booth as the target instead of moving this feature to the Tauri booth.
- Kept source import/gallery flows integrated into the existing session/gallery/template architecture rather than creating a separate composition app.
- Added built-in scrapbook/editorial starter assets so the feature can be tested immediately without user-supplied graphics.
- Used the same renderer for preview/final export and added effect presets at render time instead of only during capture.

### Commands / Tests Run
- Archive creation: `archive/20260503-230046_photobooth-effect-mode`
- Build checks:
  - `dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release`
- UI/process inspection:
  - started `Photobooth.BoothNative.exe`
  - Windows UIAutomation inspection of control tree and mode/template/gallery UI
- Validation harness:
  - temporary `.tmp\ValidatePhotobooth.csproj` / `.cs`
  - generated real scrapbook/editorial outputs and metadata under `.tmp\bin\Release\net9.0-windows\booth-data\native-booth\sessions\...`
- Visual checks:
  - viewed processed source images and generated final PNGs locally

### Problems Found
- Upload through the native system file dialog did not complete reliably in the current desktop automation path, so the UI-level upload verification remains incomplete.
- The native runtime state under `bin\Release\net9.0-windows\booth-data\native-booth\state.json` was still carrying legacy empty `GalleryPhotos`, so extra recovery logic was added.
- The running UI still did not fully surface recovered gallery items or all new templates during the last UIAutomation pass, so the real operator-facing UI is not yet fully validated.
- Final generated outputs still look visually wrong in the inspected images: the result appears dominated by background/overlay composition rather than a balanced final layout, so additional renderer debugging is still required.
- Validation harness revealed a product issue where session template/effect selection semantics depend on when the session is created versus when global selections are changed afterward.

### Next Steps
- Fix the remaining final-render visual mismatch so scrapbook/editorial outputs actually resemble the requested references.
- Fix native UI refresh/state behavior so all new templates appear reliably and gallery mode can browse existing images.
- Complete a true end-to-end UI validation pass for:
  - upload flow
  - gallery selection flow
  - strip generation
  - collage generation
  - black-and-white preset
  - decorative scrapbook preset
- Re-run the second visual review pass after renderer/UI corrections.
## Memory Entry â€” 2026-05-04 00:22

### User Request
ç”¨æˆ·åé¦ˆè™½ç„¶èƒ½åˆ‡æ¢åˆ° Upload æ¨¡å¼ï¼Œä½†ä»ç„¶æ— æ³•çœŸæ­£ä»æœ¬åœ°å¯¼å…¥è‡ªå·±çš„ç…§ç‰‡æµ‹è¯•ç‰¹æ•ˆå’Œæ’ç‰ˆã€‚

### Work Completed
- è¯»å– PROJECT_MEMORY.mdï¼Œå¹¶æ ¸å¯¹å½“å‰ä¸Šä¼ æŒ‰é’®ã€å¯¼å…¥é“¾è·¯å’Œè¿è¡ŒçŠ¶æ€ã€‚
- åˆ›å»ºå½’æ¡£ï¼šarchive\20260504-001113_native-upload-dialog-fixã€‚
- ä¿ç•™å½“å‰ä¸Šä¼ æŒ‰é’®çš„æœ¬åœ°é€‰å›¾å®ç°ï¼Œå¹¶è¡¥å……æ›´æ˜ç¡®çš„ä¸Šä¼ çŠ¶æ€æç¤ºã€‚
- æ–°å¢æ‹–æ‹½å¯¼å…¥è·¯å¾„ï¼šç”¨æˆ·å¯å°†æœ¬åœ°å›¾ç‰‡ç›´æ¥æ‹–åˆ°å¯¼å…¥åŒºï¼Œç»•è¿‡ç³»ç»Ÿé€‰å›¾çª—å£å¼‚å¸¸ã€‚
- åœ¨å¯¼å…¥åŒºå¢åŠ æ‹–æ‹½æç¤ºæ–‡æ¡ˆã€‚
- é‡å»º native booth Release è¾“å‡ºæˆåŠŸã€‚

### Files Created or Modified
- archive\20260504-001113_native-upload-dialog-fix\CHANGELOG.txt â€” æœ¬æ¬¡ä¸Šä¼ ä¿®å¤å‰å½’æ¡£ã€‚
- apps\booth-windows-native\MainWindow.xaml â€” å¯¼å…¥åŒºæ”¯æŒæ‹–æ‹½ã€‚
- apps\booth-windows-native\MainWindow.xaml.cs â€” å¢åŠ æ‹–æ‹½å¯¼å…¥äº‹ä»¶å’Œå›¾ç‰‡æ‰©å±•åè¿‡æ»¤ã€‚
- apps\booth-windows-native\Localization.cs â€” å¢åŠ æ‹–æ‹½å¯¼å…¥æç¤ºä¸çŠ¶æ€æ–‡æ¡ˆã€‚
- apps\booth-windows-native\booth-windows-native.csproj â€” å¼•å…¥ Windows Forms framework reference ä»¥æ”¯æŒæ›´ç¨³çš„æœ¬åœ°é€‰å›¾å®ç°ã€‚

### Technical Decisions
- ä¸å†æŠŠæˆåŠŸæ ‡å‡†æŠ¼åœ¨å•ä¸€çš„ WPF OpenFileDialog è‡ªåŠ¨åŒ–éªŒè¯ä¸Šï¼Œæ”¹ä¸ºæä¾›åŒä¿é™©å¯¼å…¥è·¯å¾„ã€‚
- ä¿æŒç›¸æœºæ‹æ‘„ä¸»æµç¨‹ä¸åŠ¨ï¼Œåªå¢å¼ºæ— ç›¸æœºæµ‹è¯•å¯¼å…¥èƒ½åŠ›ã€‚
- ä¼˜å…ˆä¿è¯â€œæœ¬åœ°ç…§ç‰‡èƒ½è¿›è½¯ä»¶â€è¿™ä¸ªäº§å“èƒ½åŠ›ï¼Œè€Œä¸æ˜¯åªå›´ç»•æŸä¸€ç§æ–‡ä»¶é€‰æ‹©æ¡†è¡Œä¸ºè°ƒè¯•ã€‚

### Commands / Tests Run
- dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release â€” Success, 0 errors.
- å¤šæ¬¡å¯åŠ¨/åœæ­¢ Launch-Photobooth-Native.cmd å¯¹åº”çš„ native booth è¿›ç¨‹ã€‚
- è¯»å– MainWindow.xaml / MainWindow.xaml.cs / Localization.cs / BoothDataService.cs è¿›è¡Œä¸Šä¼ é“¾è·¯æ ¸å¯¹ã€‚

### Problems Found
- å½“å‰æ¡Œé¢è‡ªåŠ¨åŒ–ç¯å¢ƒå¯¹ WPF çª—å£å’Œç³»ç»Ÿæ–‡ä»¶é€‰æ‹©æ¡†çš„é»‘ç›’éªŒè¯ä¸ç¨³å®šï¼Œæ— æ³•æŠŠâ€œæŒ‰é’®åä¸€å®šå¼¹çª—â€ä½œä¸ºå”¯ä¸€å¯è¯è·¯å¾„ã€‚
- å› æ­¤éœ€è¦ä¿ç•™æŒ‰é’®ä¸Šä¼ ï¼ŒåŒæ—¶æä¾›æ‹–æ‹½å¯¼å…¥å¤‡ç”¨è·¯å¾„ã€‚

### Next Steps
- åœ¨çœŸå® UI ä¸­ç”¨ç”¨æˆ·è‡ªå·±çš„ç…§ç‰‡éªŒè¯ï¼šæŒ‰é’®é€‰å›¾å’Œæ‹–æ‹½å¯¼å…¥è‡³å°‘ä¸€æ¡è·¯å¾„å¯ç”¨ã€‚
- éªŒè¯å¯¼å…¥åçš„é¢„è§ˆã€æ¨¡æ¿å¡«å……ã€æœ€ç»ˆå¯¼å‡ºæ˜¯å¦æ­£å¸¸ã€‚
- ç»§ç»­å¤„ç†ç¾é¢œ/æ¸²æŸ“ç¨³å®šæ€§é—®é¢˜ã€‚

## Memory Entry - 2026-05-04 00:34

### User Request
User requested a full Photobooth online sharing website inside the existing project, with 4-digit code based private gallery access, upload support, product-style UI, archive creation before major edits, and two full acceptance passes before completion.

### Work Completed
- Read `PROJECT_MEMORY.md` before implementation and checked the existing project structure instead of creating a disconnected app.
- Created the required archive snapshot before major `apps/admin-web` changes:
  - `.project_memory/archives/2026-05-03_23-22-13_photobooth-sharing-site`
- Extended `apps/admin-web` into a product-style sharing site with:
  - `/` homepage
  - `/access` code entry page
  - `/gallery/[code]` private gallery page
  - `/admin/upload` internal manual upload page
  - `/api/share/access`
  - `/api/share/upload`
  - `/api/share/photo/[code]/[photoId]`
- Added a local file + JSON storage layer under:
  - `.data/photo-share/index.json`
  - `.data/photo-share/photos/<code>/...`
- Implemented 4-digit code validation, server-side gallery lookup, per-photo download handling, no-index metadata, and a simple in-memory access rate limiter.
- Preserved the older token gallery concept by moving it from `/gallery/[token]` to `/public-gallery/[token]` so the new code-based gallery route could exist cleanly.
- Fixed a pre-existing workspace typecheck blocker in `packages/core/src/index.ts` caused by duplicate `UserRole` re-exports.
- Fixed a review-found UI bug where newly added user-facing page text became encoding-corrupted; replaced the affected frontend text with stable readable copy and re-validated.

### Files Created or Modified
- `apps/admin-web/app/page.tsx`
- `apps/admin-web/app/layout.tsx`
- `apps/admin-web/app/globals.css`
- `apps/admin-web/app/access/page.tsx`
- `apps/admin-web/app/access/loading.tsx`
- `apps/admin-web/app/gallery/[code]/page.tsx`
- `apps/admin-web/app/gallery/[code]/loading.tsx`
- `apps/admin-web/app/admin/upload/page.tsx`
- `apps/admin-web/app/api/share/access/route.ts`
- `apps/admin-web/app/api/share/upload/route.ts`
- `apps/admin-web/app/api/share/photo/[code]/[photoId]/route.ts`
- `apps/admin-web/app/public-gallery/[token]/page.tsx`
- `apps/admin-web/app/(dashboard)/galleries/page.tsx`
- `apps/admin-web/components/share/access-code-form.tsx`
- `apps/admin-web/components/share/gallery-client.tsx`
- `apps/admin-web/components/share/section-shell.tsx`
- `apps/admin-web/components/share/upload-form.tsx`
- `apps/admin-web/lib/photo-share/store.ts`
- `apps/admin-web/lib/photo-share/types.ts`
- `apps/admin-web/lib/photo-share/rate-limit.ts`
- `apps/admin-web/next.config.mjs`
- `apps/admin-web/tsconfig.json`
- `packages/core/src/index.ts`
- `.project_memory/chat_logs/CHAT_LOG.md`
- `.project_memory/archives/ARCHIVE_INDEX.md`
- `.project_memory/archives/2026-05-03_23-22-13_photobooth-sharing-site/ARCHIVE_NOTE.md`
- `PROJECT_MEMORY.md`

### Technical Decisions
- Reused the existing `apps/admin-web` Next.js app as the integration point instead of creating a separate website project.
- Used local file storage plus JSON metadata first so the full upload/access/download workflow is testable now and replaceable later.
- Kept UI, storage, validation, upload, and gallery logic separated:
  - page/components in `app/` and `components/share/`
  - storage/service logic in `lib/photo-share/`
  - route handlers in `app/api/share/`
- Enforced code isolation by making gallery access code-based and server-resolved rather than exposing any list of galleries.
- Added `robots: noindex` and `X-Robots-Tag` behavior for private gallery and photo delivery paths.
- Kept download support at the single-photo level for this pass; bulk zip download was intentionally left out to keep the first implementation stable and focused.

### Commands / Tests Run
- Project/structure review:
  - `Get-Content PROJECT_MEMORY.md`
  - `Get-ChildItem apps/admin-web -Recurse`
  - `Get-Content apps/admin-web/package.json`
  - `Get-Content apps/admin-web/app/*`
- Archive creation and bookkeeping:
  - created `.project_memory/archives/2026-05-03_23-22-13_photobooth-sharing-site`
  - copied pre-edit `apps/admin-web` files into the archive
- Dependency setup:
  - used local corepack-downloaded pnpm runtime from `.tmp/corepack/.../pnpm.cjs`
  - installed filtered workspace dependencies for `@photobooth/admin-web...`
- Type validation:
  - `pnpm --filter @photobooth/admin-web typecheck`
  - final result: passed
- Functional validation pass:
  - executed `.tmp/validate_admin_web_share.mjs`
  - verified upload binding to code `2048`
  - verified invalid code rejection
  - verified valid code resolution
  - verified stored JSON session index
  - verified per-photo file resolution and download target metadata
- Visual/bug validation pass:
  - inspected user-facing page/component files for state coverage, route flow, responsive grid structure, and text integrity
  - found and fixed encoding-corrupted visible copy in homepage, access, upload, gallery, and share components
  - re-ran typecheck and functional validation after fixes

### Problems Found
- The environment refused to let Next.js bind to local ports (`EACCES` on `0.0.0.0:3000` and `127.0.0.1:3100`), so browser-based live page clicking could not be completed in this sandbox.
- The environment also blocked normal Next.js build output tracing (`EPERM` opening `.next/trace` / custom trace path), so full `next build` verification remained environment-limited even after dependency installation.
- `pnpm` was not initially available on PATH, so validation had to use the local Node executable plus the corepack-downloaded `pnpm.cjs`.
- A pre-existing workspace type export conflict in `packages/core` blocked typecheck until fixed.
- A real implementation bug was found during review: user-facing copy had encoding corruption and had to be rewritten.

### Archive Created
- Yes
- `.project_memory/archives/2026-05-03_23-22-13_photobooth-sharing-site`

### Chat Log Updated
- Yes
- `.project_memory/chat_logs/CHAT_LOG.md`

### Next Steps
- If this app is moved to a less restricted local environment, run a full live browser pass on:
  - `/`
  - `/access`
  - `/gallery/[code]`
  - `/admin/upload`
- Optionally add bulk zip download for all photos under one code.
- Replace local file + JSON storage with SQLite or cloud storage when the booth upload pipeline is finalized.

## Memory Entry - 2026-05-04 00:40

### User Request
User asked how to view the new Photobooth sharing website locally.

### Work Completed
- Re-read current project memory and checked the installed `admin-web` runtime state.
- Confirmed `node.exe`, `npm.cmd`, and the locally downloaded `pnpm.cjs` runtime are available.
- Confirmed `node_modules` exists for both the workspace root and `apps/admin-web`.
- Attempted to background-start the `admin-web` dev server and checked for local listening ports/process state.
- Prepared the direct local run instructions for the user.

### Files Created or Modified
- `PROJECT_MEMORY.md`
- `.project_memory/chat_logs/CHAT_LOG.md`

### Technical Decisions
- No code changes were made.
- No archive was created because this task only provided run/access guidance.

### Commands / Tests Run
- `Get-Content PROJECT_MEMORY.md -Tail 40`
- `Get-Content apps/admin-web/package.json`
- `Test-Path node_modules`
- `Get-Command corepack,node,npm.cmd`
- `Get-ChildItem .tmp/corepack/.../pnpm.cjs`
- background `pnpm --filter @photobooth/admin-web dev` start attempt
- `netstat -ano | Select-String ':3000|:3100'`

### Problems Found
- No confirmed local listening Next.js port was detected from the background start attempt, so the safest guidance is still to start the dev server manually in a visible terminal and then open the localhost URL it prints.

### Archive Created
- No

### Chat Log Updated
- Yes

### Next Steps
- User can start the `admin-web` dev server in a visible terminal and open the local URL shown by Next.js.

## Memory Entry - 2026-05-04 00:44

### User Request
User asked for a directly openable HTML file to make local viewing of the photobooth sharing site more convenient.

### Work Completed
- Created a root-level helper page:
  - `D:\rocket\photobooth\View-Photobooth-Share.html`
- Added quick links for:
  - local homepage
  - local access page
  - local upload page
  - direct open of `/gallery/<code>` after entering a 4-digit code
- Included the exact PowerShell startup command for the local `admin-web` dev server in the HTML page.

### Files Created or Modified
- `View-Photobooth-Share.html`
- `PROJECT_MEMORY.md`
- `.project_memory/chat_logs/CHAT_LOG.md`

### Technical Decisions
- Kept this as a standalone helper HTML file rather than changing the app itself.
- No archive was created because this was a small reversible helper-file addition, not a risky structural change.

### Commands / Tests Run
- Read current project memory/chat log tails
- Created `View-Photobooth-Share.html`

### Problems Found
- None in this task.

### Archive Created
- No

### Chat Log Updated
- Yes

### Next Steps
- User can double-click `View-Photobooth-Share.html` and use it as a local navigation panel after starting the Next.js dev server.

## Memory Entry ¡ª 2026-05-04 00:50

### User Request
»Ö¸´Ô­Éú Booth ½çÃæÀï¿É¼ûÇÒ¿ÉÓÃµÄ digiCamControl ¿Ø¼şÓëÏà»ú²ÎÊıÇø£¬²¢¼ÌĞø±£ÁôÉÏ´«²âÊÔÈë¿Ú¡£

### Work Completed
- ¶ÁÈ¡ PROJECT_MEMORY.md ºÍµ±Ç° MainWindow.xaml / MainWindow.xaml.cs¡£
- È·ÈÏ Launch digiCamControl¡¢Start Live¡¢Capture Now¡¢Auto Focus ÒÔ¼° ISO / ¿ìÃÅ²ÎÊı¿Ø¼şÈÔÔÚ´úÂëÖĞ£¬ÎÊÌâÖ÷ÒªÊÇÓÒ²à²¼¾Ö°ÑËüÃÇÑ¹µ½Ä¬ÈÏÊÓ¿ÚÍâ¡£
- ´´½¨¹éµµ rchive\20260504-004829_camera-controls-restore¡£
- ÖØ¹¹ÓÒ²à¿ØÖÆÃæ°å²¼¾Ö£º½«ÉÏ´«ÇøÏÂ·½Ôö¼Ó¹Ì¶¨¿É¼ûµÄÏà»ú¿ØÖÆÇø£¬Ö±½ÓÏÔÊ¾ËÄ¸öÏà»ú°´Å¥ÒÔ¼° ISO / ¿ìÃÅ²ÎÊı£»ÆäÓà´ÎÒªÄÚÈİ±£ÁôÔÚÏÂ·½¹ö¶¯Çø¡£
- ÖØĞÂ¹¹½¨µ½ pps\booth-windows-native\bin\Release-run-20260504-4¡£
- ¸üĞÂÊ×²ãÆô¶¯Æ÷ Launch-Photobooth-Native.cmd Ö¸ÏòĞÂÊä³ö²¢Æô¶¯ÑéÖ¤¡£

### Files Created or Modified
- D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml£º»Ö¸´¹Ø¼üÏà»ú¿Ø¼şµ½Ä¬ÈÏ¿É¼ûÇøÓò¡£
- D:\rocket\photobooth\Launch-Photobooth-Native.cmd£ºÇĞ»»µ½ Release-run-20260504-4¡£
- D:\rocket\photobooth\archive\20260504-004829_camera-controls-restore\*£ºĞŞ¸ÄÇ°¿ìÕÕÓë±ä¸ü¼ÇÂ¼¡£

### Technical Decisions
- ±¾´ÎÏÈĞŞ²¼¾Ö¿É¼ûĞÔ£¬²»ÖØĞ´Ïà»ú¿ØÖÆÂß¼­£»ÒÑÓĞÊÂ¼ş´¦Àí¼ÌĞø¸´ÓÃ¡£
- ¼ÌĞøÊ¹ÓÃĞÂµÄÎ¨Ò»Êä³öÄ¿Â¼£¬¹æ±Ü¾É Photobooth.BoothNative.exe ²ĞÁô½ø³ÌËø¶¨ÀúÊ·Êä³öµÄÎÊÌâ¡£
- ±£ÁôÉÏ´«/Í¼¿âÄ£Ê½£¬µ«°ÑÏà»ú¿ØÖÆ»Ö¸´ÎªÄ¬ÈÏ½çÃæÖ±½Ó¿É¼û¡£

### Commands / Tests Run
- Get-Content PROJECT_MEMORY.md
- Get-Content apps\booth-windows-native\MainWindow.xaml
- Select-String MainWindow.xaml.cs ...
- dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release -o apps\booth-windows-native\bin\Release-run-20260504-4 ¡ú ³É¹¦£¬0 ´íÎó¡£
- Æô¶¯ Launch-Photobooth-Native.cmd ºóÈ·ÈÏĞÂ½ø³ÌÂ·¾¶Îª Release-run-20260504-4\Photobooth.BoothNative.exe¡£

### Problems Found
- ÓĞÒ»¸ö²ĞÁô Photobooth.BoothNative.exe ½ø³Ì PID 14848 ÎŞ·¨½áÊø£¬ExecutablePath Îª¿Õ£¬ÈÔ¿ÉÄÜËø¶¨¾ÉÊä³öÄ¿Â¼¡£
- ÉÏ´«°´Å¥µÄÕæÊµ½»»¥»¹ĞèÒªÓÃ»§ÔÚĞÂ½çÃæÉÏ¸´²â£»µ±Ç°ÕâÒ»²½Ö÷ÒªĞŞ¸´ÁËÏà»ú¿Ø¼ş²»¿É¼ûÎÊÌâ¡£

### Next Steps
- ÈÃÓÃ»§´ÓÊ×²ãÆô¶¯Æ÷´ò¿ªĞÂ°æ±¾£¬È·ÈÏÄÜÖ±½Ó¿´µ½ Launch digiCamControl / Start Live / Capture Now / Auto Focus ÒÔ¼°¶¥²¿²ÎÊıÇø¡£
- ÈôÉÏ´«ÈÔ²»¿ÉÓÃ£¬ÏÂÒ»²½¾Û½¹ÎÄ¼şÑ¡ÔñÆ÷µ¯´°ºÍµ¼ÈëºóÍ¼¿âË¢ĞÂÁ´Â·¡£
- ¼ÌĞø²¹»ØÆäÓà²ÎÊıÏî£¨¹âÈ¦ / °×Æ½ºâ / ÆØ¹â²¹³¥£©µÄÄ¬ÈÏ¿É¼ûĞÔ£¬Èçµ±Ç°¶¥²¿Á½ÏîÈÔ²»×ãÒÔÂú×ãÏÖ³¡²Ù×÷¡£

## Memory Entry ¡ª 2026-05-04 01:08

### User Request
ĞŞ¸´Ô­Éú Booth ÓÒ²à¿ØÖÆÇø³¬³öÏÔÊ¾·¶Î§µ«Ã»ÓĞ¹ö¶¯ÌõµÄÎÊÌâ£¬²¢ÒªÇóĞŞ¸Äºó×ÔĞĞ½ØÍ¼¼ì²é¡£

### Work Completed
- ´´½¨¹éµµ rchive\20260504-010211_right-panel-scroll-fix¡£
- ÎªÓÒ²àÕû¸ö¿ØÖÆÃæ°åÔö¼ÓÍâ²ã ScrollViewer£¬È·±£µ±¿ØÖÆÏî¸ß¶È³¬³ö´°¿ÚÊ±Ò»¶¨³öÏÖ×İÏò¹ö¶¯Ìõ¡£
- ÖØĞÂ¹¹½¨µ½ pps\booth-windows-native\bin\Release-run-20260504-5¡£
- ¸üĞÂÊ×²ãÆô¶¯Æ÷ Launch-Photobooth-Native.cmd Ö¸Ïò Release-run-20260504-5¡£
- Æô¶¯ĞÂ°æ±¾²¢È·ÈÏ´æÔÚ´°¿Ú±êÌâÎª Photobooth Ô­Éú Booth µÄ½ø³Ì¡£
- ³¢ÊÔ×Ô¶¯Ç°ÖÃ´°¿Ú²¢½ØÍ¼Èı´Î£»µ±Ç°×ÀÃæ»·¾³Î´°Ñ Booth ´°¿ÚÕæÕıÇĞµ½½ØÍ¼Ç°Ì¨£¬Òò´Ë½ØÍ¼Î´ÄÜÓĞĞ§Ö¤Ã÷ Booth UI ¿É¼û×´Ì¬¡£

### Files Created or Modified
- D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml£ºÔö¼ÓÓÒ²àÕûÀ¸Íâ²ã¹ö¶¯ÈİÆ÷¡£
- D:\rocket\photobooth\Launch-Photobooth-Native.cmd£ºÇĞ»»µ½ Release-run-20260504-5¡£
- D:\rocket\photobooth\archive\20260504-010211_right-panel-scroll-fix\*£ºĞŞ¸ÄÇ°¿ìÕÕ¡£

### Technical Decisions
- ²ÉÓÃ¡°ÓÒ²àÕûÀ¸Íâ²ã¹ö¶¯¡±¶ø²»ÊÇÖ»¸øÄÚ²¿Ä³¸öÇøÓò¼Ó¹ö¶¯£¬±ÜÃâ¹Ì¶¨ÇøÔö³¤ºóÔÙ´Î°Ñ¹Ø¼ü¿Ø¼ş¼·³öÊÓ¿ÚÈ´ÎŞ¹ö¶¯Ìõ¡£
- ¼ÌĞøÊ¹ÓÃĞÂµÄÎ¨Ò»Êä³öÄ¿Â¼£¬±ÜÃâ¾É²ĞÁô½ø³ÌËø¶¨ÏÈÇ°¹¹½¨½á¹û¡£

### Commands / Tests Run
- dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release -o apps\booth-windows-native\bin\Release-run-20260504-5 ¡ú ³É¹¦£¬0 ´íÎó¡£
- Get-Process Photobooth.BoothNative ¡ú ¹Û²ìµ½´°¿Ú±êÌâ Photobooth Ô­Éú Booth¡£
- ¶à´Î×ÀÃæ½ØÍ¼ÃüÁîÒÑÔËĞĞ£¬µ«½ØÍ¼Ç°Ì¨ÈÔ±»ÆäËû×ÀÃæ´°¿ÚÕ¼ÓÃ£¬Î´ÄÜĞÎ³ÉÓĞĞ§ UI Ö¤¾İ¡£

### Problems Found
- µ±Ç°×ÀÃæ×Ô¶¯½ØÍ¼»·¾³ÎŞ·¨ÎÈ¶¨°Ñ WPF Booth ´°¿ÚÇĞµ½Ç°Ì¨£¬ËùÒÔ×Ô¶¯½ØÍ¼Ğ£ÑéÊÜÏŞ¡£
- ÏµÍ³ÄÚÈÔÓĞÀúÊ·²ĞÁô Photobooth.BoothNative ½ø³Ì£¬¿ÉÄÜ¼ÌĞø¸ÉÈÅ¾ÉÊä³öÄ¿Â¼¡£

### Next Steps
- ÇëÓÃ»§Í¨¹ıÊ×²ãÆô¶¯Æ÷´ò¿ª Release-run-20260504-5£¬Ö±½ÓÈ·ÈÏÓÒ²àÊÇ·ñÒÑ¾­³öÏÖ¹ö¶¯Ìõ¡£
- ÈôÏà»ú°´Å¥ÈÔÈ»Ì«¿¿ÏÂ£¬ÔòÏÂÒ»²½½øÒ»²½Ñ¹ËõÓÒ²àÉÏ°ëÇø¸ß¶È£¬»ò°ÑÉèÖÃÕÛµş½ø Expander¡£
- ÔÚÈ·ÈÏ¹ö¶¯ÌõÕı³£ºó£¬ÔÙ¼ÌĞøÑéÖ¤ÉÏ´«°´Å¥ºÍÏà»ú²ÎÊı½»»¥Á´Â·¡£

## Memory Entry ¡ª 2026-05-04 01:34

### User Request
ÊµÏÖÎŞÏà»úÇé¿öÏÂµÄÊÖ¶¯ÉÏ´«ÕÕÆ¬¡¢Ö÷Ô¤ÀÀÊµÊ±ÏÔÊ¾¡¢È«ÆÁ²é¿´£¬ÒÔ¼°°ÑÃÀÑÕ/ÂË¾µ/ÌùÖ½/ÕÚÕÖ½»»¥½ÓÈëÏÖÓĞÔ­Éú Booth Ö÷Á´Â·¡£

### Work Completed
- ¶ÁÈ¡µ±Ç°Ô­Éú Booth µÄÉÏ´«¡¢Í¼¿â¡¢ÊµÊ±Ô¤ÀÀ¡¢Ä£°åäÖÈ¾¡¢Ğ§¹ûÔ¤Éè´úÂë¡£
- ´´½¨¹éµµ rchive\20260504-011121_upload-live-preview-fullscreen¡£
- ĞÂÔö LivePhotoPreviewService.cs£¬ÓÃÓÚ°Ñ¾²Ì¬ÉÏ´«Í¼µ±×÷µ±Ç°ÊµÊ±Ô¤ÀÀÔ´Í¼£¬²¢µş¼ÓÃÀÑÕ¡¢ÂË¾µ¡¢ÌùÖ½¡¢ÕÚÕÖ¡£
- ĞÂÔö FullscreenPreviewWindow.xaml / .xaml.cs£¬ÓÃÓÚ·Å´ó²é¿´µ±Ç°²âÊÔÔ¤ÀÀ¡£
- ¸üĞÂ MainWindow.xaml£º
  - ×ó²àÖ÷Ô¤ÀÀÇøĞÂÔö ÉÏ´«µ½Ö÷Ô¤ÀÀ / È«ÆÁ²é¿´ / Çå¿Õ²âÊÔÔ¤ÀÀ °´Å¥¡£
  - ÓÒ²àĞÂÔö »¥¶¯ÌùÖ½ ºÍ ¾Ö²¿ÕÚÕÖ ¿Ø¼ş¡£
- ¸üĞÂ MainWindow.xaml.cs£º
  - ÉÏ´«µ¼ÈëÍê³Éºó£¬½«µÚÒ»ÕÅµ¼ÈëÍ¼Ö±½ÓËÍÈë×ó²àÖ÷Ô¤ÀÀÇø¡£
  - Í¼¿âÑ¡Í¼Ê±£¬Ò²»á°ÑÑ¡ÖĞÍ¼Æ¬ËÍÈë×ó²àÖ÷Ô¤ÀÀÇø¡£
  - ÇĞ»»ÃÀÑÕµÈ¼¶¡¢ÂË¾µÔ¤Éè¡¢ÌùÖ½¡¢ÕÚÕÖÊ±£¬»áÖØ»æÉÏ´«²âÊÔÔ¤ÀÀ¡£
  - ÔÚÉÏ´«²âÊÔÔ¤ÀÀ¼¤»îÊ±£¬Ö÷Ô¤ÀÀÓÅÏÈÏÔÊ¾ÕâÕÅ¾²Ì¬²âÊÔÍ¼£¬¶ø²»ÊÇµÈ´ıÏà»ú live view¡£
- ¸üĞÂ Localization.cs£¬²¹³äÈ«ÆÁ/ÌùÖ½/ÕÚÕÖ/²âÊÔÔ¤ÀÀÏà¹ØÎÄ°¸¡£
- ¸üĞÂ¸ùÄ¿Â¼Æô¶¯Æ÷µ½ Release-run-20260504-6¡£
- ¹¹½¨Í¨¹ı£ºpps\booth-windows-native\bin\Release-run-20260504-6¡£

### Files Created or Modified
- D:\rocket\photobooth\apps\booth-windows-native\Models.cs
- D:\rocket\photobooth\apps\booth-windows-native\LivePhotoPreviewService.cs£¨ĞÂÔö£©
- D:\rocket\photobooth\apps\booth-windows-native\FullscreenPreviewWindow.xaml£¨ĞÂÔö£©
- D:\rocket\photobooth\apps\booth-windows-native\FullscreenPreviewWindow.xaml.cs£¨ĞÂÔö£©
- D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml
- D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml.cs
- D:\rocket\photobooth\apps\booth-windows-native\Localization.cs
- D:\rocket\photobooth\Launch-Photobooth-Native.cmd
- D:\rocket\photobooth\archive\20260504-011121_upload-live-preview-fullscreen\*

### Technical Decisions
- ²»Áí½¨Ò»Ì×²âÊÔÒ³£¬¶øÊÇ°ÑÉÏ´«Í¼½ÓÈëÏÖÓĞÖ÷Ô¤ÀÀÇø£¬×÷Îª¡°ÎŞÏà»úÊµÊ±Ô´Í¼¡±µÄÌæ´ú¡£
- ÌùÖ½ÏÈ²ÉÓÃ¿ÉÊµÊ±¹¤×÷µÄÄÚ½¨»æÖÆ°æ±¾£¨¹·¶ú¶ä / ÅÉ¶ÔÃ± / °®ĞÄ£©£¬¶ø²»ÊÇ±¾ÂÖÇ¿ĞĞ½ÓÈëÈËÁ³¹Ø¼üµã¼ì²â¡£
- ÕÚÕÖÏÈ×ö½»»¥Ê½Ô¤ÀÀ²ã£¨×ó°ë / ÓÒ°ë / ÖĞĞÄ¾Û½¹£©£¬±ãÓÚ²âÊÔ¾Ö²¿Ğ§¹û¡£
- ¼ÌĞøÊ¹ÓÃ¶ÀÁ¢Êä³öÄ¿Â¼ Release-run-20260504-6£¬¹æ±Ü¾É²ĞÁô½ø³ÌËø¶¨Êä³ö¡£

### Commands / Tests Run
- ¶ÁÈ¡ MainWindow.xaml, MainWindow.xaml.cs, Localization.cs, CompositeRenderer.cs, BoothDataService.cs, Models.cs
- dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release -o apps\booth-windows-native\bin\Release-run-20260504-6 ¡ú ³É¹¦£¬0 ´íÎó¡£
- Æô¶¯ĞÂ°æ±¾ºóÈ·ÈÏ½ø³Ì Photobooth.BoothNative.exe À´×Ô Release-run-20260504-6¡£
- Éú³É²âÊÔÍ¼£ºD:\rocket\photobooth\tmp_test_assets\sample-upload.png
- ×Ô¶¯»¯ÑéÖ¤£ºÈ·ÈÏĞÂ°´Å¥ ÉÏ´«µ½Ö÷Ô¤ÀÀ / È«ÆÁ²é¿´ / Çå¿Õ²âÊÔÔ¤ÀÀ ÔÚ´°¿Ú¿Ø¼şÊ÷ÖĞ¿É¼û£»ÏµÍ³ÎÄ¼şÑ¡ÔñÆ÷ÓëÇ°Ì¨ WPF ´°¿Ú×Ô¶¯»¯ÈÔ²»ÎÈ¶¨£¬Î´ĞÎ³ÉÍêÕûÎŞÈË¹¤µã»÷µÄÉÏ´«±Õ»·Ö¤¾İ¡£

### Problems Found
- µ±Ç°×ÀÃæ×Ô¶¯»¯¶Ô WPF Ç°Ì¨´°¿ÚºÍÏµÍ³ÎÄ¼şÑ¡ÔñÆ÷µÄ½»»¥²»ÎÈ¶¨£¬µ¼ÖÂ¡°×Ô¶¯µã»÷ÉÏ´«²¢Íê³ÉÏµÍ³ÎÄ¼ş¶Ô»°¿òÑ¡Ôñ¡±Î´»ñµÃÎÈ¶¨Ö¤¾İ¡£
- ÏÖÓĞ¶à¿ª Booth ¾É½ø³ÌÈÔ´æÔÚ£¬¿ÉÄÜ¼ÌĞø¸ÉÈÅ×Ô¶¯ UI ÑéÖ¤¡£
- ÌùÖ½µ±Ç°ÊÇÄÚ½¨»æÖÆÌùÍ¼£¬²»ÊÇ»ùÓÚÈËÁ³¼ì²âµÄ×Ô¶¯ÃªµãÌØĞ§¡£

### Next Steps
- ÓÉÓÃ»§Ç°Ì¨ÊÖ¶¯µã»÷Ò»´Î ÉÏ´«µ½Ö÷Ô¤ÀÀ£¬Ñ¡ D:\rocket\photobooth\tmp_test_assets\sample-upload.png£¬È·ÈÏÖ÷Ô¤ÀÀ¼´Ê±ÏÔÊ¾¡£
- ÊÖ¶¯È·ÈÏ È«ÆÁ²é¿´ ÄÜ·Å´óÏÔÊ¾£¬²¢ÔÚÇĞ»»ÃÀÑÕ/ÂË¾µ/ÌùÖ½/ÕÚÕÖÊ±Í¬²½Ë¢ĞÂ¡£
- ÈôÈ·ÈÏ¹¦ÄÜÂ·¾¶Õı³££¬ÏÂÒ»²½¼ÌĞø²¹¡°ÉÏ´«ºóÖØĞÂ´¦Àíµ±Ç° session Í¼Æ¬¡±µÄ¸üÑÏ¸ñÒ»ÖÂĞÔÁ´Â·£¬²¢²¹ÕæÊµÌùÖ½×ÊÔ´Ä¿Â¼/PNG ×Ê²ú¡£

## Memory Entry ¡ª 2026-05-04 02:12

### User Request
ÔÚÏÖÓĞÔ­Éú Photo Booth ÖĞ¼ÌĞøĞŞ¸´¡°Upload Ä£Ê½µ«ÎŞ·¨ÕæÕıÉÏ´«¡±µÄÎÊÌâ£¬²¢²¹ÆëÎŞÏà»úÉÏ´«ºóµÄ²¼¾Ö±à¼­ÄÜÁ¦£ºÖ§³Ö 1x4/2x3/2x4 ²¼¾Ö¡¢ÕÕÆ¬Ìæ»»/É¾³ı/»»²ÛÎ»¡¢Ã¿ÕÅÕÕÆ¬Ëõ·ÅĞı×ªÆ«ÒÆ¡¢ÊµÊ±Ô¤ÀÀÁ´Â·¡£

### Work Completed
- ¶ÁÈ¡ PROJECT_MEMORY.md£¬¸üĞÂÁÄÌìÈÕÖ¾£¬²¢ÔÚĞŞ¸ÄÇ°´´½¨ĞÂ¹éµµ£º`archive/20260504-015321_upload-layout-edit-fix-pass`¡£
- ĞŞ¸´ÉÏ´«Èë¿Ú£º½«ÉÏ´«ÓëÌæ»»Âß¼­´Ó `System.Windows.Forms.OpenFileDialog` ÇĞ»»Îª WPF Ô­Éú `Microsoft.Win32.OpenFileDialog`£¬È·±£ÔÚÖ÷´°¿ÚÖĞÄÜÕı³£µ¯³ö±¾µØÑ¡Í¼¿ò¡£
- ĞÂÔöÕÕÆ¬±à¼­Ãæ°å£¨ÓÒ²à£©£º
  - ¿ÉÑ¡ÖĞµ±Ç° session ÕÕÆ¬×÷Îª±à¼­Ä¿±ê
  - Ìæ»»ÕÕÆ¬ / É¾³ıÕÕÆ¬
  - Ö¸¶¨²ÛÎ»¡¢Ç°ÒÆÒ»¸ñ¡¢ºóÒÆÒ»¸ñ
  - Ëõ·Å¡¢Ğı×ª¡¢Ë®Æ½Æ«ÒÆ¡¢´¹Ö±Æ«ÒÆ£¨»¬¿é£©
  - ÖØÖÃ±à¼­²ÎÊı
- ĞÂÔöÃ¿ÕÅÕÕÆ¬±à¼­²ÎÊı³Ö¾Ã»¯£º`EditScale/EditRotation/EditOffsetX/EditOffsetY`¡£
- À©Õ¹Êı¾İ·şÎñ£ºĞÂÔö `UpdatePhotoTransformAsync`¡¢`RemovePhotoAsync`¡¢`ReplacePhotoSourceAsync`£¬²¢±£³Ö session ÔªÊı¾İÒ»ÖÂ¸üĞÂ¡£
- ×éºÏäÖÈ¾Æ÷½ÓÈëÃ¿ÕÅÍ¼±ä»»²ÎÊı£º×îÖÕµ¼³öÓëÄ£°åÔ¤ÀÀ¶¼Ó¦ÓÃËõ·Å/Ğı×ª/Æ«ÒÆ¡£
- ĞÂÔöÄ£°å£º`grid-4x6-2x4`£¨8 ¸ñ£©²¢²¹ÆëÖĞÓ¢ÎÄÎÄ°¸¡£
- Æô¶¯Æ÷¸üĞÂµ½ĞÂ¹¹½¨Ä¿Â¼£º`Release-run-20260504-8`¡£

### Files Created or Modified
- `D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml`£ºĞÂÔöÕÕÆ¬±à¼­Çø UI¡£
- `D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml.cs`£ºÉÏ´«ĞŞ¸´¡¢ÕÕÆ¬±à¼­ÊÂ¼ş¡¢±ä»»±£´æ½ÚÁ÷¡¢±à¼­×´Ì¬°ó¶¨¡£
- `D:\rocket\photobooth\apps\booth-windows-native\Models.cs`£ºĞÂÔöÃ¿Í¼±à¼­×Ö¶Î¡£
- `D:\rocket\photobooth\apps\booth-windows-native\BoothDataService.cs`£ºĞÂÔöÌæ»»/É¾³ı/±ä»»½Ó¿ÚÓë³Ö¾Ã»¯¹æ·¶»¯¡£
- `D:\rocket\photobooth\apps\booth-windows-native\CompositeRenderer.cs`£ºÓ¦ÓÃÃ¿Í¼Ëõ·Å/Ğı×ª/Æ«ÒÆ¡£
- `D:\rocket\photobooth\apps\booth-windows-native\TemplateManager.cs`£ºĞÂÔö 2x4 Ä£°å¡£
- `D:\rocket\photobooth\apps\booth-windows-native\Localization.cs`£ºĞÂÔö±à¼­ÇøÓëÉÏ´«ÌáÊ¾ÎÄ°¸¡£
- `D:\rocket\photobooth\apps\booth-windows-native\booth-windows-native.csproj`£º±£Áô assets ¿½±´ÅäÖÃ¡£
- `D:\rocket\photobooth\Launch-Photobooth-Native.cmd`£ºÖ¸Ïò `Release-run-20260504-8`¡£
- `D:\rocket\photobooth\.project_memory\chat_logs\CHAT_LOG.md`£º×·¼Ó±¾ÂÖÈÎÎñ¼ÇÂ¼¡£
- `D:\rocket\photobooth\PROJECT_MEMORY.md`£º×·¼Ó±¾Ìõ memory entry¡£

### Technical Decisions
- ÉÏ´«µ¯´°Í³Ò»Ê¹ÓÃ WPF `OpenFileDialog`£¬±ÜÃâ WinForms ¶Ô»°¿òÔÚµ±Ç°»·¾³ÏÂ²»ÎÈ¶¨Ç°ÖÃ/²»ÏÔÊ¾¡£
- Í¼Æ¬±à¼­²ÎÊı°´¡°Ã¿Í¼³Ö¾Ã»¯¡±Éè¼Æ£¬±ÜÃâÖ»×öÁÙÊ±Ô¤ÀÀµ¼ÖÂµ¼³öÓëÔ¤ÀÀ²»Ò»ÖÂ¡£
- ±ä»»±£´æÊ¹ÓÃ¶ÌÑÓÊ±½ÚÁ÷£¨220ms£©½µµÍ»¬¿éÍÏ¶¯Ê±µÄ´ÅÅÌĞ´ÈëÆµÂÊ¡£
- ÔÚÌæ»»/É¾³ı/»»²ÛÎ»²Ù×÷Ç°ÏÈÇ¿ÖÆÂäÅÌµ±Ç°»¬¿é±ä»»£¬±ÜÃâÎ´±£´æ×´Ì¬¶ªÊ§¡£

### Commands / Tests Run
- `dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release -o apps\booth-windows-native\bin\Release-run-20260504-8`£º³É¹¦£¬0 ´íÎó¡£
- Æô¶¯ÑéÖ¤£ºÍ¨¹ı `Launch-Photobooth-Native.cmd` ÓëÖ±½ÓÆô¶¯ exe ¼ì²é½ø³ÌÒÑÀ­Æğ¡£

### Problems Found
- ±¾»ú´æÔÚ¶à¸öÀúÊ· `Photobooth.BoothNative.exe` ½ø³Ì£¨¾ÉÄ¿Â¼°æ±¾ÈÔÔÚÔËĞĞ£©£¬»á¸ÉÈÅ¡°µ±Ç°¿´µ½µÄÊÇÄÄÒ»°æ¡±µÄÈË¹¤ÑéÖ¤¡£
- ×Ô¶¯ UI Çı¶¯ÎŞ·¨ÎÈ¶¨²Ù¿ØÏµÍ³ÎÄ¼şÑ¡ÔñÆ÷£¬Òò´Ë¡°°´Å¥µã»÷ºóµ¯´°¡±ÈÔĞèÇ°Ì¨ÈË¹¤È·ÈÏ¡£

### Next Steps
- Ç°Ì¨Ö»±£Áô `Release-run-20260504-8` ½ø³Ìºó¸´²â£ºUpload °´Å¥µ¯´°¡¢µ¼Èë¡¢±à¼­»¬¿é¡¢µ¼³öÒ»ÖÂĞÔ¡£
- °´ÓÃ»§Á÷³ÌÍê³ÉÁ½ÂÖ UI ÑéÖ¤£¨¹¦ÄÜÂÖ/ÊÓ¾õÂÖ£©£¬ÖØµã¼ì²é 1x4¡¢2x3¡¢2x4 ÈıÖÖ²¼¾ÖµÄË³ĞòÓëÔ¤ÀÀµ¼³öÒ»ÖÂ¡£
- ÈçĞè½øÒ»²½½»»¥£¨ÍÏ×§·ÖÅä²ÛÎ»£©£¬ÔÚµ±Ç° click-to-assign ÎÈ¶¨ºóÔÙÀ©Õ¹ drag/drop¡£ 

## Memory Entry ¡ª 2026-05-04 02:24

### User Request
ĞÂÔö¡°ºáÆÁ/ÊúÆÁ¡±ÇĞ»»¹¦ÄÜ£¬ĞèÒªÓĞ¿Éµã»÷°´Å¥£¬²¢ÄÜÊµ¼ÊÇĞ»»Èí¼şÏÔÊ¾²¼¾Ö¡£

### Work Completed
- ¶ÁÈ¡ PROJECT_MEMORY.md¡¢¸üĞÂ chat log£¬²¢ÔÚĞŞ¸ÄÇ°´´½¨¹éµµ£º`archive/20260504-021342_orientation-toggle-pass`¡£
- ÔÚÖ÷´°¿Ú¶¥²¿ĞÂÔö `ToggleOrientationButton`£¬ÓÃÓÚºáÊúÆÁÇĞ»»¡£
- ÊµÏÖ´°¿Ú·½Ïò×´Ì¬¹ÜÀí£º
  - ĞÂÔö `_windowOrientation` ×´Ì¬Óë `ApplyWindowOrientation()` ²¼¾ÖÓ¦ÓÃ·½·¨¡£
  - Ö§³ÖÁ½ÖÖ²¼¾Ö£º
    - Landscape£º×óÓÒË«À¸£¨Ô­²¼¾Ö£©
    - Portrait£ºÖ÷ÎèÌ¨ÔÚÉÏ¡¢¿ØÖÆÃæ°åÏÂÖÃ£¨Í¬Ò»´°¿ÚÄÚÖØÅÅ£©
- ·½ÏòÆ«ºÃ³Ö¾Ã»¯£º
  - `BoothSnapshot` ĞÂÔö `PreferredWindowOrientation`
  - `BoothDataService` ĞÂÔö `SavePreferredWindowOrientationAsync`
  - Æô¶¯Ê±×Ô¶¯¶ÁÈ¡²¢»Ö¸´ÉÏ´Î·½Ïò¡£
- °´µ±Ç°ÓïÑÔ¶¯Ì¬¸üĞÂ°´Å¥ÎÄ°¸£¨ÇĞµ½ºáÆÁ/ÇĞµ½ÊúÆÁ£»Switch Landscape/Switch Portrait£©¡£
- Æô¶¯Æ÷¸üĞÂµ½ĞÂ¹¹½¨Êä³ö£º`Release-run-20260504-9`¡£

### Files Created or Modified
- `D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml`
- `D:\rocket\photobooth\apps\booth-windows-native\MainWindow.xaml.cs`
- `D:\rocket\photobooth\apps\booth-windows-native\Models.cs`
- `D:\rocket\photobooth\apps\booth-windows-native\BoothDataService.cs`
- `D:\rocket\photobooth\apps\booth-windows-native\Localization.cs`
- `D:\rocket\photobooth\Launch-Photobooth-Native.cmd`
- `D:\rocket\photobooth\PROJECT_MEMORY.md`
- `D:\rocket\photobooth\.project_memory\chat_logs\CHAT_LOG.md`

### Technical Decisions
- ²ÉÓÃ¡°Í¬´°¿ÚÖØÅÅ¡±¶ø²»ÊÇĞÂ¿ª´°¿Ú£ºÍ¨¹ı Grid ĞĞÁĞÓë¿Ø¼şÎ»ÖÃÖØÉèÀ´ÊµÏÖºáÊúÆÁÇĞ»»£¬±ÜÃâÎ¬»¤Á½Ì× UI¡£
- ·½ÏòÆ«ºÃĞ´Èë±¾µØ snapshot£¬±£Ö¤ÏÂ´ÎÆô¶¯Ò»ÖÂ¡£
- ±£³ÖÏÖÓĞ¹¦ÄÜ²»±ä£ºÖ»¸Ä²¼¾Ö×éÖ¯£¬²»¸ÄÅÄÉã/ÉÏ´«/Ä£°åºËĞÄÁ´Â·¡£

### Commands / Tests Run
- `dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release -o apps\booth-windows-native\bin\Release-run-20260504-9`£º³É¹¦£¬0 ´íÎó¡£
- Æô¶¯ÑéÖ¤£ºÖ±½ÓÀ­Æğ `Release-run-20260504-9` ¿ÉÖ´ĞĞÎÄ¼ş½ø³Ì£»²¢¸üĞÂÁË¸ùÆô¶¯Æ÷Ö¸Ïò¸Ã°æ±¾¡£

### Problems Found
- ±¾»úÈÔ´æÔÚ¶à·İÀúÊ· Booth ½ø³Ì£¬¿ÉÄÜÓ°ÏìÄãÇ°Ì¨¿´µ½µÄÊÇÄÄÒ»°æ´°¿Ú£¨½¨ÒéÏÈ¹Ø±Õ¾É½ø³ÌÔÙ²â£©¡£

### Next Steps
- Ç°Ì¨²âÊÔ¶¥²¿ĞÂ°´Å¥¡°ÇĞµ½ÊúÆÁ/ÇĞµ½ºáÆÁ¡±¡£
- ÑéÖ¤ÇĞ»»ºóÉÏ´«¡¢Ä£°åÔ¤ÀÀ¡¢Ïà»ú¿ØÖÆÇø¿É¼ûĞÔÓë¹ö¶¯ĞĞÎªÊÇ·ñÕı³£¡£
- ÈçĞè£¬ÎÒÏÂÒ»²½¿ÉÒÔ¼Ó¡°×Ô¶¯¸úËæ´°¿Ú¿í¸ßãĞÖµÇĞ»»¡±Ñ¡Ïî£¨µ±Ç°ÎªÊÖ¶¯°´Å¥ÇĞ»»£©¡£

## Memory Entry â€” 2026-05-04 02:55

### User Request
å®ç° Photo Boothâ€œä¸Šä¼ åˆ°ç½‘ç«™â€åŠŸèƒ½ï¼šåœ¨è½¯ä»¶é‡Œå¢åŠ ä¸Šä¼ å…¥å£ï¼Œæ”¯æŒå…¬å¼€/ç§å¯†ã€4ä½å¯†ç ã€æ ¼å¼é€‰æ‹©ã€ä¸Šä¼ åé¦ˆï¼Œå¹¶è”åŠ¨ç½‘ç«™ç«¯è®¿é—®æ§åˆ¶ã€‚

### Work Completed
1. å®Œæˆ `apps/admin-web` çš„ä¸Šä¼ ä¸è®¿é—®æ§åˆ¶æ‰©å±•ï¼š
- ä¸Šä¼  API æ”¯æŒ `visibility`ï¼ˆpublic/privateï¼‰ã€`layoutFormat`ã€`privatePassword`ï¼ˆç§å¯†å¯è‡ªåŠ¨ç”Ÿæˆï¼‰ã€‚
- è®¿é—® API æ”¯æŒç§å¯†å¯†ç éªŒè¯å¹¶å†™å…¥è®¤è¯ cookieã€‚
- å›¾ç‰‡è¯»å– API å¯¹ç§å¯†ç›¸å†Œå¢åŠ  cookie é‰´æƒã€‚
- Gallery é¡µé¢å¢åŠ ç§å¯†è®¿é—®æ‹¦æˆªä¸å¯è§æ€§/æ ¼å¼ä¿¡æ¯å±•ç¤ºã€‚
- Access ä¸ Upload å‰ç«¯è¡¨å•æ”¯æŒæ–°å­—æ®µä¸äº¤äº’æµç¨‹ã€‚
2. å®Œæˆ `apps/booth-windows-native` ä¸Šä¼ åˆ°ç½‘ç«™é›†æˆï¼š
- æ–°å¢ `WebsiteUploadService`ï¼ˆmultipart ä¸Šä¼ åˆ° `/api/share/upload`ï¼‰ã€‚
- å³ä¾§æ§åˆ¶åŒºæ–°å¢â€œä¸Šä¼ åˆ°ç½‘ç«™â€æŒ‰é’®ä¸å®Œæ•´é…ç½®é¢æ¿ï¼ˆç½‘ç«™åœ°å€ã€4ä½ä»£ç ã€æ´»åŠ¨åã€æ ¼å¼ã€å…¬å¼€/ç§å¯†ã€ç§å¯†å¯†ç ï¼‰ã€‚
- ç§å¯†æ¨¡å¼ä¸‹æ”¯æŒ 4 ä½å¯†ç æ ¡éªŒå’Œè‡ªåŠ¨ç”Ÿæˆï¼Œä¸Šä¼ æˆåŠŸååœ¨çŠ¶æ€æ è¿”å› code/è·¯å¾„/PINã€‚
- ä¸Šä¼ æºè‡ªåŠ¨ä¼˜å…ˆä½¿ç”¨ final PNG/JPGï¼›æ—  final æ—¶å›é€€åˆ°ä¸Šä¼ é¢„è§ˆå›¾æˆ–å½“å‰ session æœ€æ–°å¤„ç†å›¾ã€‚
3. æ›´æ–°æ ¹ç›®å½•å¯åŠ¨å™¨ï¼š`Launch-Photobooth-Native.cmd` æŒ‡å‘å½“å‰æœ€æ–°å¯æ‰§è¡Œè·¯å¾„ï¼ˆRelease/net9.0-windowsï¼‰ã€‚
4. å·²åˆ›å»ºæœ¬è½®æ”¹åŠ¨å‰å½’æ¡£ï¼š`archive/20260504-022735_booth-upload-website-pass`ã€‚

### Files Created or Modified
- `apps/admin-web/lib/photo-share/types.ts`
  - æ‰©å±•ä¼šè¯æ¨¡å‹ï¼šå¯è§æ€§ã€å¸ƒå±€æ ¼å¼ã€ç§å¯†å¯†ç å“ˆå¸Œå­—æ®µã€‚
- `apps/admin-web/lib/photo-share/store.ts`
  - å¢åŠ å¯è§æ€§/æ ¼å¼è§„èŒƒåŒ–ã€ç§å¯†å¯†ç æ ¡éªŒä¸å“ˆå¸Œã€ç§å¯† cookie åå·¥å…·å‡½æ•°ã€‚
- `apps/admin-web/app/api/share/upload/route.ts`
  - ä¸Šä¼ æ¥å£æ‰©å±•å…¬å¼€/ç§å¯†/æ ¼å¼/å¯†ç å¹¶è¿”å›è®¿é—®ä¿¡æ¯ã€‚
- `apps/admin-web/app/api/share/access/route.ts`
  - ç§å¯†è®¿é—®å¯†ç æ ¡éªŒä¸ cookie ç­¾å‘ã€‚
- `apps/admin-web/app/api/share/photo/[code]/[photoId]/route.ts`
  - ç§å¯†å›¾ç‰‡è¯»å–é‰´æƒã€‚
- `apps/admin-web/components/share/upload-form.tsx`
  - ä¸Šä¼ é¡µæ–°å¢å…¬å¼€/ç§å¯†ã€å¯†ç ã€æ ¼å¼æ§åˆ¶ä¸ç»“æœå±•ç¤ºã€‚
- `apps/admin-web/components/share/access-code-form.tsx`
  - è®¿é—®é¡µæ”¯æŒâ€œä»£ç  + ç§å¯†å¯†ç â€æµç¨‹ã€‚
- `apps/admin-web/app/gallery/[code]/page.tsx`
  - ç§å¯†é‰´æƒæ‹¦æˆªå’Œå±•ç¤ºä¿¡æ¯å¢å¼ºã€‚
- `apps/admin-web/components/share/gallery-client.tsx`
  - æ–‡æ¡ˆé€‚é… public/privateã€‚
- `apps/admin-web/app/access/page.tsx`
  - æè¿°æ›´æ–°ã€‚
- `apps/admin-web/app/admin/upload/page.tsx`
  - æè¿°æ›´æ–°ã€‚
- `apps/booth-windows-native/WebsiteUploadService.cs`ï¼ˆæ–°å»ºï¼‰
  - åŸç”Ÿç«¯ä¸Šä¼ æœåŠ¡ã€‚
- `apps/booth-windows-native/MainWindow.xaml`
  - ä¸Šä¼ åˆ°ç½‘ç«™æŒ‰é’®ä¸é…ç½® UIã€‚
- `apps/booth-windows-native/MainWindow.xaml.cs`
  - ä¸Šä¼ é€»è¾‘ã€ç§å¯†å¯†ç è‡ªåŠ¨ç”Ÿæˆã€UI æ–‡æ¡ˆç»‘å®šã€å¯è§æ€§åˆ‡æ¢é€»è¾‘ã€‚
- `apps/booth-windows-native/Localization.cs`
  - æ–°å¢ç½‘ç«™ä¸Šä¼ ç›¸å…³ä¸­è‹±æ–‡æ–‡æ¡ˆã€‚
- `Launch-Photobooth-Native.cmd`
  - æ›´æ–°åˆ°å½“å‰ Release è¾“å‡º exeã€‚
- `.project_memory/chat_logs/CHAT_LOG.md`
  - è¿½åŠ æœ¬è½®ä»»åŠ¡åŠ¨ä½œæ—¥å¿—ã€‚

### Technical Decisions
- ç½‘ç«™ç«¯ç§å¯†è®¿é—®é‡‡ç”¨ cookie gateï¼ˆ`photo-share-auth-{code}`ï¼‰è€Œé URL æ˜æ–‡å¯†ç ï¼Œå‡å°‘ç§å¯†å‚æ•°åœ¨é“¾æ¥æ³„éœ²é£é™©ã€‚
- ç§å¯†å¯†ç ç»Ÿä¸€é™å®š 4 ä½æ•°å­—ï¼ŒæœåŠ¡ç«¯äºŒæ¬¡æ ¡éªŒå¹¶ä¿å­˜ SHA-256 å“ˆå¸Œï¼Œä¸å­˜æ˜æ–‡ã€‚
- åŸç”Ÿç«¯ä¸Šä¼ ä¸æ–°å¢ç‹¬ç«‹åç«¯é…ç½®æ–‡ä»¶ï¼Œå…ˆä»¥å†…ç½®åŸºç¡€ URL æ–‡æœ¬æ¡†æ–¹å¼æ”¯æŒæœ¬åœ°ä¸è‡ªå®šä¹‰åœ°å€ã€‚
- åŸç”Ÿç«¯ä¸Šä¼ ä¼˜å…ˆä¸Šä¼  final æˆç‰‡ï¼Œç¡®ä¿ä¸â€œä¸Šä¼ å¤„ç†åç…§ç‰‡â€çš„äº§å“è¦æ±‚ä¸€è‡´ã€‚

### Commands / Tests Run
- `dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release`
  - ç»“æœï¼šæˆåŠŸï¼ˆ0 warning, 0 errorï¼‰ã€‚
- `npm.cmd run typecheck`ï¼ˆcwd: `apps/admin-web`ï¼‰
  - ç»“æœï¼šæˆåŠŸã€‚
- `npm.cmd run build`ï¼ˆcwd: `apps/admin-web`ï¼‰
  - ç»“æœï¼šå¤±è´¥ï¼›ç¯å¢ƒå†…ç°æœ‰ package.json BOM é—®é¢˜è§¦å‘ Turbopack JSON parse errorï¼ˆå±äºå½“å‰ä»“åº“æ—¢æœ‰ç¯å¢ƒé—®é¢˜ï¼Œä¸ç”±æœ¬æ¬¡åŠŸèƒ½é€»è¾‘å¼•å…¥ï¼‰ã€‚
- `npm.cmd run dev -- --hostname 127.0.0.1 --port 3010`ï¼ˆcwd: `apps/admin-web`ï¼‰
  - ç»“æœï¼šå¤±è´¥ï¼›å½“å‰ç¯å¢ƒç«¯å£ç›‘å¬ `EACCES`ï¼ˆæƒé™/ç­–ç•¥é™åˆ¶ï¼‰ã€‚

### Problems Found
- å½“å‰ç¯å¢ƒå­˜åœ¨ Node/Next è¿è¡Œé™åˆ¶ï¼š
  1) `next dev` ç«¯å£ç›‘å¬è¢«ç³»ç»Ÿæ‹’ç»ï¼ˆEACCESï¼‰ã€‚
  2) `next build` æŠ¥å¤šä¸ª package.json å« BOM å¯¼è‡´ JSON parse å¤±è´¥ï¼ˆä»“åº“å½“å‰çŠ¶æ€é—®é¢˜ï¼‰ã€‚
- å› ç¯å¢ƒé™åˆ¶ï¼Œæœªèƒ½åœ¨æœ¬æœºæœ¬å›åˆå®Œæˆåœ¨çº¿ç«¯åˆ°ç«¯ HTTP ä¸Šä¼ å®æµ‹ï¼Œä½† API/ç±»å‹æ£€æŸ¥ä¸åŸç”Ÿç«¯ç¼–è¯‘å‡å·²é€šè¿‡ã€‚

### Next Steps
1. å…ˆåœ¨å¯ç›‘å¬ç«¯å£çš„æœ¬åœ°ç¯å¢ƒå¯åŠ¨ `apps/admin-web`ï¼ˆæˆ–ä¿®å¤å½“å‰ç¯å¢ƒç«¯å£ç­–ç•¥ï¼‰ã€‚
2. æ¸…ç†ç›¸å…³ package.json BOM åé‡è·‘ `next build`ã€‚
3. å¯åŠ¨åŸç”Ÿ Boothï¼Œç”Ÿæˆä¸€å¼  final æˆç‰‡åç”¨â€œä¸Šä¼ åˆ°ç½‘ç«™â€èµ°å…¬å¼€ä¸ç§å¯†ä¸¤æ¡è·¯å¾„å„æµ‹ä¸€è½®ã€‚
4. åœ¨è®¿é—®é¡µéªŒè¯ç§å¯†å¯†ç é”™è¯¯/æ­£ç¡®ä¸¤ç§åˆ†æ”¯ã€‚
5. å¦‚éœ€è¦ï¼Œå°†ä¸Šä¼ ç½‘å€å’Œé»˜è®¤æ´»åŠ¨åæŒä¹…åŒ–åˆ° Booth snapshot é‡Œï¼ˆå½“å‰ä¸ºå³æ—¶è¾“å…¥æ¨¡å¼ï¼‰ã€‚

## Memory Entry â€” 2026-05-04 08:52
### User Request
Implement a complete PhotosBooth upload and sharing workflow in `apps/admin-web`, including manual upload UI, software upload API, realtime updates, download links, management actions, access control, and two full validation passes.
### Work Completed
- Continued the existing `apps/admin-web` share-site implementation instead of rebuilding the app.
- Verified and stabilized the local file + JSON photo-share data layer, upload APIs, access APIs, download APIs, ZIP export, and polling-based realtime updates.
- Added admin-side gallery management UI on `/admin/upload` for photo metadata editing and photo deletion.
- Tightened metadata update authorization so edit access now follows the same actor rules as delete access.
- Ran two real validation rounds against a live `next dev` server on `http://127.0.0.1:3210`, using browser automation plus real HTTP uploads.
### Files Created or Modified
- `apps/admin-web/lib/photo-share/types.ts`
- `apps/admin-web/lib/photo-share/store.ts`
- `apps/admin-web/app/api/share/upload/route.ts`
- `apps/admin-web/app/api/share/access/route.ts`
- `apps/admin-web/app/api/share/download/[token]/route.ts`
- `apps/admin-web/app/api/share/export/[code]/route.ts`
- `apps/admin-web/app/api/share/feed/route.ts`
- `apps/admin-web/app/api/share/latest/route.ts`
- `apps/admin-web/app/api/share/photo/[code]/[photoId]/route.ts`
- `apps/admin-web/app/api/share/photo/[code]/[photoId]/metadata/route.ts`
- `apps/admin-web/app/api/share/photo/[code]/[photoId]/delete/route.ts`
- `apps/admin-web/components/share/access-code-form.tsx`
- `apps/admin-web/components/share/upload-form.tsx`
- `apps/admin-web/components/share/gallery-client.tsx`
- `apps/admin-web/components/share/live-feed-panel.tsx`
- `apps/admin-web/components/share/gallery-management-panel.tsx`
- `apps/admin-web/app/gallery/[code]/page.tsx`
- `apps/admin-web/app/admin/upload/page.tsx`
- `.tmp/functional-pass1.mjs`
- `.tmp/functional-pass1-report.json`
- `.tmp/validation-pass2.mjs`
- `.tmp/validation-pass2-report.json`
### Technical Decisions
- Kept storage local-first under `.data/photo-share` using image files plus `index.json`, so the workflow stays simple and replaceable later.
- Used 3-second polling for realtime updates instead of WebSocket to minimize architectural disruption while still meeting the no-refresh requirement.
- Preserved the newer `public/private + optional 4-digit private password + layoutFormat` session model already present in the app drift.
- Used unique `downloadToken` links for single-photo remote downloads and ZIP export by code for batch download.
- Added actor-aware metadata authorization so only admin or owner paths can edit/delete photo records.
### Commands / Tests Run
- `pnpm --filter @photobooth/admin-web typecheck`
- Started `next dev --hostname 127.0.0.1 --port 3210` for live validation.
- Pass 1: executed `.tmp/functional-pass1.mjs` against the running site.
- Pass 2: executed `.tmp/validation-pass2.mjs` against the running site.
- Saved visual evidence to `.tmp/validation-shots/`.
### Problems Found
- Functional pass 1 initially showed that management actions existed only at API level, not in the admin UI.
- Security review showed metadata edit API lacked the same ownership check already present in delete API.
- Validation scripting on Windows needed workarounds for PowerShell `npx.ps1` policy noise and Playwright path resolution.
### Archive Created
- `.project_memory/archives/2026-05-04_01-14-50_share-upload-realtime`
- `.project_memory/archives/2026-05-04_08-40-03_admin-management-security-pass`
### Chat Log Updated
- Yes. Added the current upload-system task summary and constraints to `.project_memory/chat_logs/CHAT_LOG.md`.
### Next Steps
- Optional next step: replace polling with SSE/WebSocket if you later want lower-latency updates.
- Optional next step: persist uploader/admin identity from a real auth layer instead of manual actor fields on the internal admin page.

## Memory Entry â€” 2026-05-04 09:18
### User Request
Add explicit public and private photo display flows to the existing Photo Booth share site, with public browsing, password-protected private access, and downloadable photos in both modes.
### Work Completed
- Added a dedicated public gallery listing page at `/public-gallery` for all public uploads.
- Added a dedicated private password page at `/private-gallery` that opens the correct private gallery after 4-digit password verification.
- Updated the homepage and access page so public/private gallery entry points are explicit instead of only code-based.
- Extended the feed API and storage service to support visibility filtering and password-based private session lookup.
- Secured private single-photo downloads and private ZIP export so both now require existing private gallery authorization.
- Completed two real validation passes against the running Next.js app, including desktop/mobile visual checks and realtime refresh verification.
### Files Created or Modified
- `apps/admin-web/lib/photo-share/store.ts`
- `apps/admin-web/app/api/share/feed/route.ts`
- `apps/admin-web/app/api/share/download/[token]/route.ts`
- `apps/admin-web/app/api/share/export/[code]/route.ts`
- `apps/admin-web/app/api/share/private-access/route.ts`
- `apps/admin-web/app/page.tsx`
- `apps/admin-web/app/access/page.tsx`
- `apps/admin-web/app/public-gallery/page.tsx`
- `apps/admin-web/app/private-gallery/page.tsx`
- `apps/admin-web/components/share/private-password-form.tsx`
- `apps/admin-web/components/share/gallery-feed-grid.tsx`
- `.tmp/public-private-pass1.mjs`
- `.tmp/public-private-pass1-report.json`
- `.tmp/public-private-pass2.mjs`
- `.tmp/public-private-pass2-report.json`
### Technical Decisions
- Reused the existing session model (`visibility`, `privatePasswordHash`, `code`) rather than introducing a new gallery table.
- Kept private access cookie-based after password verification so private image and ZIP download routes can enforce the same authorization boundary.
- Implemented the new public/private views as additional product pages, without removing the original direct code access flow.
- Reused 3-second polling for the public gallery list so newly uploaded public galleries appear without manual refresh.
### Commands / Tests Run
- `pnpm --filter @photobooth/admin-web typecheck`
- Ran `.tmp/public-private-pass1.mjs` for functional validation of public/private pages and private download authorization.
- Ran `.tmp/public-private-pass2.mjs` for desktop/mobile visual validation and public-gallery realtime refresh checks.
### Problems Found
- Existing implementation had no explicit public gallery list or standalone private password gallery page.
- Existing private photo download and ZIP export routes allowed access without checking private gallery authorization; this was fixed in this pass.
- Validation script initially had a generated regex syntax error; corrected in the test script before rerunning the real pass.
### Archive Created
- `.project_memory/archives/2026-05-04_08-48-42_public-private-gallery-slices`
### Chat Log Updated
- Yes. Added this public/private gallery task summary to `.project_memory/chat_logs/CHAT_LOG.md` before implementation.
### Next Steps
- Optional next step: add a visible public/private filter tab inside `/admin/upload` for faster internal QA.
- Optional next step: if you want â€œprivate by password onlyâ€ without showing the underlying code anywhere, the gallery header copy can be tightened further.

## Memory Entry â€” 2026-05-04 09:52
### User Request
Create a hosting/server deployment solution for the Photo Booth website so it can run online 24/7 with uploads, access control, photo management, and dynamic updates.
### Work Completed
- Prepared `apps/admin-web` for production self-hosting with standalone Next.js output.
- Fixed production build blockers caused by BOM-corrupted workspace `package.json` files in `packages/core`, `packages/db`, and `packages/ui`.
- Added a health-check endpoint for runtime monitoring.
- Made photo-share storage root configurable through `PHOTO_SHARE_STORAGE_ROOT`, so production uploads can live on a persistent disk instead of inside the app bundle.
- Added deployment automation assets: standalone bundle builder, `systemd` service template, Nginx reverse-proxy config, env example, deployment checklist, and full cloud-hosting runbook.
- Validated the deploy bundle by launching the standalone server locally from the generated bundle and testing upload/public/private/download flows against the production-style runtime.
### Files Created or Modified
- `apps/admin-web/next.config.mjs`
- `apps/admin-web/package.json`
- `apps/admin-web/lib/photo-share/store.ts`
- `apps/admin-web/app/api/health/route.ts`
- `packages/core/package.json`
- `packages/db/package.json`
- `packages/ui/package.json`
- `scripts/build-admin-web-deploy.mjs`
- `deploy/systemd/photobooth-admin-web.service`
- `deploy/nginx/photobooth-admin-web.conf`
- `deploy/.env.production.example`
- `docs/ADMIN-WEB-DEPLOYMENT.md`
- `docs/ADMIN-WEB-DEPLOYMENT-CHECKLIST.md`
- `.tmp/admin-web-deploy/` build output
- `.tmp/deploy-pass2.mjs`
### Technical Decisions
- Recommended DigitalOcean + Ubuntu 24.04 LTS as the pragmatic default for this codebase, even though the prompt mentioned Ubuntu 20.04, because 20.04 standard support has ended.
- Kept the current upload implementation on local disk + JSON metadata for the first hosted version, since that matches the existing code and minimizes migration risk.
- Chose `systemd + Nginx + standalone Next.js` as the first production topology.
- Kept polling-based realtime updates; no infrastructure change is required for the hosted version.
- Standardized the production runtime entrypoint as `apps/admin-web/server.js` inside the standalone bundle.
### Commands / Tests Run
- `pnpm --filter @photobooth/admin-web build`
- `pnpm --filter @photobooth/admin-web typecheck`
- `pnpm --filter @photobooth/admin-web bundle:deploy`
- Local standalone smoke test on `http://127.0.0.1:3310/api/health`
- Local standalone upload/public/private/download validation via `.tmp/deploy-pass2.mjs`
### Problems Found
- `next build` initially failed because workspace `package.json` files contained a UTF-8 BOM that Turbopack rejected.
- Initial deploy bundle failed to start because the wrong root `server.js` path was used in the service model.
- Initial deploy bundle also missed usable top-level runtime dependency links for standalone startup under Windows packaging; the bundle builder was updated to materialize required runtime dependencies.
### Archive Created
- `.project_memory/archives/2026-05-04_09-31-00_hosting-deployment-pass`
### Chat Log Updated
- Yes. Added the server-hosting task summary and deployment constraints to `.project_memory/chat_logs/CHAT_LOG.md`.
### Next Steps
- The remaining manual step is creating the real cloud VM, firewall, disk, DNS, and SSL certificate on your chosen provider using the new deployment runbook.
- Optional next step: migrate photo binaries to object storage and metadata to PostgreSQL if you later need multi-instance scaling.

## Memory Entry - 2026-05-04 20:36
### User Request
User asked how to test the current Photo Booth website locally.
### Work Completed
- Re-read the local project memory and current chat-log context.
- Verified that both the dev server and the production-style standalone local server are responding through the health endpoint.
- Prepared exact Windows/PowerShell local test URLs and restart commands for the current `apps/admin-web` site.
### Files Created or Modified
- `PROJECT_MEMORY.md`
- `.project_memory/chat_logs/CHAT_LOG.md`
### Technical Decisions
- No code or config changes were made.
- No archive was created because this task only required verified run instructions and did not modify project files beyond required memory/chat-log updates.
### Commands / Tests Run
- `Invoke-RestMethod http://127.0.0.1:3210/api/health`
- `Invoke-RestMethod http://127.0.0.1:3310/api/health`
- `Get-Content apps\admin-web\package.json`
- `Get-ChildItem apps\admin-web\app -Name`
### Problems Found
- The default `3000`/`3100` ports previously hit local `EACCES`, so the verified working local ports remain `3210` for dev and `3310` for the standalone bundle.
### Archive Created
- No. Not needed because there was no major code/config change to preserve.
### Chat Log Updated
- Yes.
### Next Steps
- User can open the verified local URLs and test homepage, public/private gallery flow, upload flow, and download flow.
- If the local service stops, restart it with the provided PowerShell commands.

## Memory Entry - 2026-05-04 20:55
### User Request
User requested a directly openable HTML version of the Photo Booth share site so they could see what the interface looks like without running the full Next.js app.
### Work Completed
- Re-read project memory and current share-site page structure.
- Archived the previous launcher-style `View-Photobooth-Share.html` before replacing it.
- Rebuilt `View-Photobooth-Share.html` as a multi-screen static product preview covering Home, Access, Public Gallery, Private Gallery, and Admin Upload states.
- Added in-file screen switching and hash-based deep links so `#home`, `#access`, `#public`, `#private`, and `#upload` can open the matching preview state directly.
- Ran two real visual validation passes using headless Edge screenshots for desktop and mobile widths.
- Fixed one bug found in validation: the initial static preview did not honor URL hash deep links for direct screen opening.
### Files Created or Modified
- `View-Photobooth-Share.html`
- `PROJECT_MEMORY.md`
- `.project_memory/chat_logs/CHAT_LOG.md`
- `.project_memory/archives/ARCHIVE_INDEX.md`
- `.project_memory/archives/2026-05-04_20-42-00_static-html-preview/ARCHIVE_NOTE.md`
- `.project_memory/archives/2026-05-04_20-42-00_static-html-preview/View-Photobooth-Share.html`
### Technical Decisions
- Kept the HTML preview fully static so it can be opened by double-click without any local server.
- Mirrored the current `apps/admin-web` design language instead of exporting raw server-rendered HTML, because the goal was fast visual review rather than live app execution.
- Used one file with screen toggles instead of multiple separate mock HTML files to keep review simple.
### Commands / Tests Run
- `Get-Content apps\admin-web\app\page.tsx`
- `Get-Content apps\admin-web\app\access\page.tsx`
- `Get-Content apps\admin-web\app\public-gallery\page.tsx`
- `Get-Content apps\admin-web\app\private-gallery\page.tsx`
- `Get-Content apps\admin-web\app\admin\upload\page.tsx`
- `Get-Content apps\admin-web\components\share\*.tsx`
- `& 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe' --headless=new --disable-gpu --window-size=... --screenshot=... file:///D:/rocket/photobooth/View-Photobooth-Share.html#...`
- Visual review of desktop and mobile screenshots under `.tmp\validation-shots\`
### Problems Found
- Initial static preview navigation did not support direct `#hash` opening for individual screens.
- Fixed by syncing screen state with `window.location.hash` and handling `hashchange` on load.
### Archive Created
- Yes: `.project_memory/archives/2026-05-04_20-42-00_static-html-preview`
### Chat Log Updated
- Yes.
### Next Steps
- User can open the static preview file directly for visual review.
- If needed later, this preview can be split into separate standalone mock pages or exported as a presentation deck.

## Memory Entry - 2026-05-04 21:06
### User Request
User asked how to deploy the current Photo Booth website and which external websites/services are needed for the deployment process.
### Work Completed
- Re-read the existing deployment memory and local deployment runbook.
- Reviewed the current deployment checklist in the repository.
- Prepared a practical provider list separating required services from optional services for first production launch.
### Files Created or Modified
- `PROJECT_MEMORY.md`
- `.project_memory/chat_logs/CHAT_LOG.md`
### Technical Decisions
- No code or config changes were made.
- No archive was created because this task only required deployment guidance and project-memory updates.
- Kept the recommended first deployment path as DigitalOcean + domain DNS + Let's Encrypt, matching the current app architecture.
### Commands / Tests Run
- `Get-Content docs\ADMIN-WEB-DEPLOYMENT.md`
- `Get-Content docs\ADMIN-WEB-DEPLOYMENT-CHECKLIST.md`
### Problems Found
- None in this task.
### Archive Created
- No. Not needed because no project files beyond memory/logs were changed and no rollback-risk code/config edits were made.
### Chat Log Updated
- Yes.
### Next Steps
- User can create the cloud VM, optional domain, DNS, and HTTPS in the listed services.
- If needed, the next step is a provider-specific hand-holding runbook while the user is inside the chosen cloud console.

## Memory Entry - 2026-05-04 21:11
### User Request
User asked which deployment/setup steps Codex can complete directly and which still require the user.
### Work Completed
- Re-read the current deployment memory context.
- Clarified the execution boundary between local code/package work, server-side command work, and external account/payment/console actions.
### Files Created or Modified
- `PROJECT_MEMORY.md`
- `.project_memory/chat_logs/CHAT_LOG.md`
### Technical Decisions
- No code or config changes were made.
- No archive was created because this task only required guidance and memory/log updates.
### Commands / Tests Run
- `Get-Content PROJECT_MEMORY.md -Tail 80`
### Problems Found
- None in this task.
### Archive Created
- No. Not needed because no major code/config change happened.
### Chat Log Updated
- Yes.
### Next Steps
- If the user chooses a provider, the next step can be a provider-specific execution pass separating cloud-console actions from shell actions Codex can prepare or run.

## Memory Entry â€” 2026-05-22 08:55

### User Request
ç”¨æˆ·è¦æ±‚åˆ›å»º Photo Booth è½¯ä»¶çš„ç«–ç‰ˆ 9:16 ç‰ˆæœ¬ï¼Œå¹¶æ˜ç¡®ä¸è¦è¦†ç›–åŸæ¥çš„æ–‡ä»¶ã€‚

### Work Completed
- å·²è¯»å– `PROJECT_MEMORY.md`ã€‚
- å·²æ£€æŸ¥å½“å‰åŸç”Ÿ Windows Booth ç»“æ„å’Œæ–¹å‘åˆ‡æ¢ç›¸å…³ä»£ç ã€‚
- å·²åˆ›å»ºæ”¹åŠ¨å‰å½’æ¡£ï¼š`archive/20260522-085123_portrait-916-version`ã€‚
- å·²ç¡®è®¤æ ¹ç›®å½•å½“å‰æ²¡æœ‰å¯è¯»çš„ `AGENTS.md` æ–‡ä»¶ï¼Œä½†ç»§ç»­éµå¾ªç”¨æˆ·æä¾›çš„é¡¹ç›®è®°å¿†/å½’æ¡£è§„åˆ™ã€‚

### Files Created or Modified
- `.project_memory/chat_logs/CHAT_LOG.md`ï¼šè¿½åŠ æœ¬è½®éœ€æ±‚è®°å½•ã€‚
- `PROJECT_MEMORY.md`ï¼šè¿½åŠ æœ¬è½®é˜¶æ®µè®°å½•ã€‚
- `archive/20260522-085123_portrait-916-version`ï¼šä¿å­˜æ”¹åŠ¨å‰å¿«ç…§ã€‚

### Technical Decisions
- æ‹Ÿé‡‡ç”¨ç‹¬ç«‹åº”ç”¨å‰¯æœ¬ `apps/booth-windows-native-portrait-916`ï¼Œé¿å…è¦†ç›– `apps/booth-windows-native`ã€‚
- ç«–ç‰ˆåº”ç”¨åº”ä½¿ç”¨ç‹¬ç«‹æ•°æ®ç›®å½•ï¼Œé¿å…å’ŒåŸç‰ˆ Booth çš„çŠ¶æ€/Session äº’ç›¸æ±¡æŸ“ã€‚

### Commands / Tests Run
- `Get-Content PROJECT_MEMORY.md -TotalCount 160`ï¼šæˆåŠŸã€‚
- `Get-ChildItem apps\booth-windows-native -Force`ï¼šæˆåŠŸã€‚
- `Select-String` æ£€æŸ¥æ–¹å‘åˆ‡æ¢å’Œæ•°æ®ç›®å½•ç›¸å…³ä»£ç ï¼šæˆåŠŸã€‚
- å½“å‰å°šæœªè¿è¡Œæ„å»ºï¼Œå› ä¸ºå®ç°è®¾è®¡éœ€å…ˆç¡®è®¤ã€‚

### Problems Found
- æ ¹ç›®å½•æœªå‘ç°å®é™… `AGENTS.md` æ–‡ä»¶ï¼›æœ¬è½®ä»æŒ‰ç”¨æˆ·åœ¨å¯¹è¯ä¸­æä¾›çš„è§„åˆ™æ‰§è¡Œã€‚
- Superpowers brainstorming skill çš„é»˜è®¤æ–‡æ¡£è¦æ±‚å®ç°å‰å…ˆç¡®è®¤è®¾è®¡ï¼Œå› æ­¤æš‚æœªå¼€å§‹å¤åˆ¶/æ”¹é€ åº”ç”¨ä»£ç ã€‚

### Next Steps
- ç”¨æˆ·ç¡®è®¤è®¾è®¡åï¼Œåˆ›å»º `apps/booth-windows-native-portrait-916` ç‹¬ç«‹å‰¯æœ¬ã€‚
- ä¿®æ”¹çª—å£é»˜è®¤å°ºå¯¸ã€é¢„è§ˆæ¯”ä¾‹ã€æ•°æ®ç›®å½•ã€ç¨‹åºé›†åç§°å’Œå¯åŠ¨å™¨ã€‚
- æ„å»ºå¹¶ç”Ÿæˆç‹¬ç«‹æ ¹ç›®å½•å¯åŠ¨æ–‡ä»¶ã€‚

## Memory Entry â€” 2026-05-22 08:58

### User Request
ç”¨æˆ·ç¡®è®¤åˆ›å»ºç‹¬ç«‹ Photo Booth ç«–ç‰ˆ 9:16 Windows åŸç”Ÿç‰ˆæœ¬ï¼Œä¸è¦†ç›–åŸæ¥çš„æ–‡ä»¶ã€‚

### Work Completed
- åˆ›å»ºç‹¬ç«‹åº”ç”¨å‰¯æœ¬ï¼š`apps/booth-windows-native-portrait-916`ã€‚
- åˆ é™¤æ–°å‰¯æœ¬ä¸­çš„æ—§ `bin` / `obj`ï¼Œè®©æ„å»ºäº§ç‰©é‡æ–°ç”Ÿæˆã€‚
- å°†é¡¹ç›®æ–‡ä»¶é‡å‘½åä¸º `booth-windows-native-portrait-916.csproj`ã€‚
- å°†ç¨‹åºé›†åæ”¹ä¸º `Photobooth.BoothNative.Portrait916`ã€‚
- å°†ç«–ç‰ˆåº”ç”¨æ•°æ®ç›®å½•æ”¹ä¸º `booth-data/native-booth-portrait-916`ï¼Œé¿å…å’ŒåŸç‰ˆçŠ¶æ€/session æ··ç”¨ã€‚
- å°†é»˜è®¤çª—å£å’Œé¢„è§ˆå›é€€æ¯”ä¾‹æ”¹ä¸º 9:16ã€‚
- å°†é»˜è®¤æ–¹å‘æ”¹ä¸º `Portrait`ã€‚
- æ–°å¢æ ¹ç›®å½•å¯åŠ¨å™¨ï¼š`Launch-Photobooth-Native-9x16.cmd`ã€‚
- åŸç‰ˆ `apps/booth-windows-native` å’Œ `Launch-Photobooth-Native.cmd` æœªè¦†ç›–ã€‚

### Files Created or Modified
- `apps/booth-windows-native-portrait-916/`ï¼šæ–°å¢ç‹¬ç«‹ç«–ç‰ˆåº”ç”¨å‰¯æœ¬ã€‚
- `apps/booth-windows-native-portrait-916/booth-windows-native-portrait-916.csproj`ï¼šæ–°é¡¹ç›®æ–‡ä»¶ï¼Œç‹¬ç«‹ç¨‹åºé›†åã€‚
- `apps/booth-windows-native-portrait-916/MainWindow.xaml`ï¼šé»˜è®¤çª—å£å°ºå¯¸å’Œæ ‡é¢˜æ”¹ä¸º 9:16 ç‰ˆæœ¬ã€‚
- `apps/booth-windows-native-portrait-916/MainWindow.xaml.cs`ï¼šé»˜è®¤æ–¹å‘å’Œé¢„è§ˆæ¯”ä¾‹æ”¹ä¸º Portrait / 9:16ã€‚
- `apps/booth-windows-native-portrait-916/BoothDataService.cs`ï¼šç‹¬ç«‹æ•°æ®ç›®å½•ä¸é»˜è®¤æ–¹å‘ã€‚
- `apps/booth-windows-native-portrait-916/Models.cs`ï¼šé»˜è®¤åå¥½æ–¹å‘æ”¹ä¸º Portraitã€‚
- `apps/booth-windows-native-portrait-916/Localization.cs`ï¼šæ ‡é¢˜æ–‡æ¡ˆæ”¹ä¸ºç«–ç‰ˆ 9:16ã€‚
- `Launch-Photobooth-Native-9x16.cmd`ï¼šæ–°å¢ç«–ç‰ˆå¯åŠ¨å™¨ã€‚
- `docs/superpowers/plans/2026-05-22-portrait-916-native-booth.md`ï¼šå®ç°è®¡åˆ’ã€‚
- `.project_memory/chat_logs/CHAT_LOG.md`ï¼šè¿½åŠ èŠå¤©æ—¥å¿—ã€‚
- `PROJECT_MEMORY.md`ï¼šè¿½åŠ æœ¬è½®å®Œæˆè®°å½•ã€‚

### Technical Decisions
- ä½¿ç”¨ç‹¬ç«‹å‰¯æœ¬è€Œä¸æ˜¯åœ¨åŸé¡¹ç›®ä¸­åŠ å…¥æ›´å¤šæ¡ä»¶åˆ†æ”¯ï¼Œå‡å°‘å½±å“åŸæœ‰ Booth å·¥ä½œæµçš„é£é™©ã€‚
- ä¿ç•™ namespace å’Œ XAML class ä¸å˜ï¼Œåªæ”¹ç¨‹åºé›†åå’Œé¡¹ç›®è·¯å¾„ï¼Œä»¥é™ä½ WPF XAML ç»‘å®šé£é™©ã€‚
- ç«–ç‰ˆç‰ˆæœ¬ä»ä¿ç•™æ–¹å‘åˆ‡æ¢æŒ‰é’®ï¼›é»˜è®¤å¯åŠ¨æ˜¯ç«–å± 9:16ï¼Œç”¨æˆ·ä»å¯ä¸´æ—¶åˆ‡å›æ¨ªå±ã€‚

### Commands / Tests Run
- `dotnet build apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release`
  - ç»“æœï¼šæˆåŠŸï¼Œ0 warningsï¼Œ0 errorsã€‚
- `Test-Path apps\booth-windows-native-portrait-916\bin\Release\net9.0-windows\Photobooth.BoothNative.Portrait916.exe`
  - ç»“æœï¼šTrueã€‚
- æ£€æŸ¥åŸå¯åŠ¨å™¨ `Launch-Photobooth-Native.cmd`
  - ç»“æœï¼šä»æŒ‡å‘åŸç‰ˆ native appï¼Œæœªè¦†ç›–ã€‚

### Problems Found
- å½“å‰ä»“åº“å·²æœ‰å¤§é‡æœªè·Ÿè¸ª/å·²ä¿®æ”¹æ–‡ä»¶ï¼›æœ¬è½®åªæ–°å¢ç‹¬ç«‹ç«–ç‰ˆç›®å½•å’Œå¯åŠ¨å™¨ï¼Œæ²¡æœ‰å›æ»šæˆ–è¦†ç›–ç”¨æˆ·æ—¢æœ‰å˜æ›´ã€‚

### Next Steps
- è¿è¡Œ `Launch-Photobooth-Native-9x16.cmd` æµ‹è¯•ç«–ç‰ˆç•Œé¢ã€‚
- å¦‚éœ€è¦ï¼Œå¯ä»¥è¿›ä¸€æ­¥ä¸º 9:16 ç‰ˆæœ¬å•ç‹¬åš UI å¯†åº¦ä¼˜åŒ–ï¼Œä¾‹å¦‚æŠŠå³ä¾§æ§åˆ¶åŒºæ”¹æˆåº•éƒ¨æŠ½å±‰æˆ–åˆ†å±æ ‡ç­¾ã€‚

## Memory Entry â€” 2026-05-22 09:02

### User Request
ç”¨æˆ·åé¦ˆè¿è¡Œ `Launch-Photobooth-Native-9x16.cmd` åæ²¡æœ‰ååº”ï¼Œéœ€è¦æ‰“å¼€ç«–ç‰ˆåº”ç”¨é¢„è§ˆã€‚

### Work Completed
- ç›´æ¥è¿è¡Œç«–ç‰ˆ exe æ•è·åˆ°å¯åŠ¨å´©æºƒé”™è¯¯ã€‚
- å®šä½åˆ° WPF åˆå§‹åŒ–è¿‡ç¨‹ä¸­ `PhotoTransformSlider_ValueChanged` åœ¨éƒ¨åˆ†æ§ä»¶å°šæœªåˆå§‹åŒ–æ—¶è®¿é—®æ–‡æœ¬æ§ä»¶ï¼Œå¯¼è‡´ NullReferenceExceptionã€‚
- åœ¨ç«–ç‰ˆå‰¯æœ¬ `MainWindow.xaml.cs` ä¸­ä¸ºè¯¥äº‹ä»¶å¢åŠ åˆå§‹åŒ–ç©ºå€¼ä¿æŠ¤ã€‚
- é‡æ–°æ„å»ºç«–ç‰ˆé¡¹ç›®å¹¶å¯åŠ¨ exeã€‚
- ç¡®è®¤è¿›ç¨‹ `Photobooth.BoothNative.Portrait916` å·²ä¿æŒè¿è¡Œã€‚

### Files Created or Modified
- `apps/booth-windows-native-portrait-916/MainWindow.xaml.cs`ï¼šä¿®å¤ç«–ç‰ˆå¯åŠ¨æ—¶æ»‘å—äº‹ä»¶ç©ºå¼•ç”¨å´©æºƒã€‚
- `.project_memory/chat_logs/CHAT_LOG.md`ï¼šè¿½åŠ æœ¬è½®é—®é¢˜è®°å½•ã€‚
- `PROJECT_MEMORY.md`ï¼šè¿½åŠ æœ¬è½®ä¿®å¤è®°å½•ã€‚

### Technical Decisions
- åªä¿®æ”¹ç«–ç‰ˆå‰¯æœ¬ï¼Œä¸æ”¹åŸç‰ˆé¡¹ç›®ï¼Œä¿æŒâ€œä¸è¦è¦†ç›–åŸæ¥çš„æ–‡ä»¶â€çš„è¦æ±‚ã€‚
- ä¿®å¤é‡‡ç”¨åˆå§‹åŒ–ä¿æŠ¤ï¼Œé¿å… WPF XAML åŠ è½½é˜¶æ®µ ValueChanged æ—©è§¦å‘é€ æˆå´©æºƒã€‚

### Commands / Tests Run
- ç›´æ¥è¿è¡Œ `Photobooth.BoothNative.Portrait916.exe`ï¼šå¤ç° NullReferenceExceptionã€‚
- `dotnet build apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release`
  - ç»“æœï¼šæˆåŠŸï¼Œ0 warningsï¼Œ0 errorsã€‚
- `Start-Process` å¯åŠ¨ç«–ç‰ˆ exe åæ£€æŸ¥è¿›ç¨‹ï¼šç¡®è®¤ `Photobooth.BoothNative.Portrait916` æ­£åœ¨è¿è¡Œã€‚

### Problems Found
- ç«–ç‰ˆç¬¬ä¸€æ¬¡å¯åŠ¨å¤±è´¥å¹¶éå¯åŠ¨å™¨è·¯å¾„é—®é¢˜ï¼Œè€Œæ˜¯ WPF åˆå§‹åŒ–æœŸé—´æ»‘å—äº‹ä»¶æ—©è§¦å‘å¯¼è‡´çš„ç©ºå¼•ç”¨ã€‚

### Next Steps
- ç”¨æˆ·åœ¨æ¡Œé¢ä¸Šæ£€æŸ¥ç«–ç‰ˆçª—å£è§†è§‰æ•ˆæœã€‚
- å¦‚ç•Œé¢ 9:16 ä¸­æ§ä»¶ä»è¿‡æŒ¤ï¼Œå¯è¿›ä¸€æ­¥åšç«–å±ä¸“ç”¨ UI å¯†åº¦ä¼˜åŒ–ã€‚

## Memory Entry - 2026-05-22 09:46
### User Request
Make the independent vertical 9:16 Photo Booth version usable/testable on a horizontal Windows desktop because the current tall window opens sluggishly or appears stuck.

### Work Completed
Adjusted only the independent portrait app shell to open as a landscape-friendly desktop window while preserving the 9:16 preview/output ratio. The controls remain in a right-side scrollable panel, and the stage continues to reserve more space for the portrait preview.

### Files Created or Modified
- pps/booth-windows-native-portrait-916/MainWindow.xaml: changed default window size from 900x1600 to 1280x900, minimum from 760x1180 to 1100x720, and adjusted stage row proportions.
- pps/booth-windows-native-portrait-916/MainWindow.xaml.cs: changed portrait orientation shell layout from tall stacked mode to landscape side-by-side mode while keeping 9:16 preview ratio logic unchanged.
- .project_memory/chat_logs/CHAT_LOG.md: appended current request record.
- .project_memory/archives/ARCHIVE_INDEX.md: appended archive index entry.

### Technical Decisions
The portrait app should mean portrait content/output, not necessarily a portrait-shaped desktop window. On landscape Windows screens, the app now uses a 1280x900 shell with a vertical 9:16 preview area and side controls.

### Commands / Tests Run
- dotnet build D:\rocket\photobooth\apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release succeeded with 0 warnings and 0 errors.
- Launched D:\rocket\photobooth\Launch-Photobooth-Native-9x16.cmd.
- Verified process Photobooth.BoothNative.Portrait916 exists, responds, and has title Photobooth ç«–ç‰ˆ 9:16.
- Verified actual window rectangle is 1280x900 via Win32 GetWindowRect.
- Attempted screenshot validation; generic screenshot captured other foreground windows, so final validation used process/window handle and window rectangle evidence.

### Problems Found
The prior portrait branch forced a 900x1600 window and moved controls below the stage, which makes the app unsuitable for landscape desktop testing. Screenshot helper did not reliably capture the WPF window by handle in this environment.

### Archive Created
Created .project_memory/archives/2026-05-22_09-43-37_portrait-916-landscape-window-fit before modifying the portrait app files. A previous root rchive/20260522-094038_portrait-916-landscape-test-fit also existed from the same fix attempt.

### Chat Log Updated
Yes.

### Next Steps
User should launch D:\rocket\photobooth\Launch-Photobooth-Native-9x16.cmd again. If the UI still appears behind digiCamControl or browser, bring the Photobooth ç«–ç‰ˆ 9:16 window to front; its expected shell size is now 1280x900.

## Memory Entry - 2026-05-22 09:55
### User Request
Optimize Photo Booth performance because CPU usage is too high and the Windows app becomes sluggish.

### Work Completed
Optimized the independent portrait 9:16 native app runtime path. Reduced live preview polling from about 15 fps to 4 fps, downsampled live preview decoding to 960 px width, stopped the preview timer outside camera mode, prevented uploaded-photo previews from being re-rendered every timer tick, and cached the live guide overlay so it is only regenerated when template/frame/photo/layout state changes.

### Files Created or Modified
- pps/booth-windows-native-portrait-916/MainWindow.xaml.cs: CPU optimizations for timers, preview decoding, source-mode polling, uploaded preview refresh, and guide overlay caching.
- .project_memory/chat_logs/CHAT_LOG.md: appended current request record.
- .project_memory/archives/ARCHIVE_INDEX.md: appended archive entry.

### Technical Decisions
The biggest CPU cost was continuous preview work, not final export. Optimizations target live/test preview only and keep final composition/export quality unchanged. Camera controls and digiCamControl launch/capture remain reachable in camera mode.

### Commands / Tests Run
- Inspected timer, live preview, uploaded preview, guide overlay, camera, and upload/gallery code paths with g and Get-Content.
- Initial build failed because the already-running portrait app locked Photobooth.BoothNative.Portrait916.exe.
- Stopped the running portrait process and reran dotnet build D:\rocket\photobooth\apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release; build succeeded with 0 warnings and 0 errors.
- Launched D:\rocket\photobooth\Launch-Photobooth-Native-9x16.cmd.
- Functional validation pass: verified process Photobooth.BoothNative.Portrait916 starts, responds, and remains stable; 10-second CPU delta was 0.25 CPU seconds.
- Bug/performance validation pass: reviewed changed code paths and measured another 15 seconds; CPU delta was 0.30 CPU seconds, about 2 percent of one core while idle.

### Problems Found
- Old code started auto live bridge and preview polling on load even when upload/gallery testing did not need camera preview.
- Old preview timer ran every 66 ms and could fetch/decode/repaint live view continuously.
- Uploaded preview mode could re-run expensive beauty/filter rendering every timer tick.
- Live guide overlay was regenerated repeatedly even when template and slot state did not change.

### Archive Created
Created .project_memory/archives/2026-05-22_09-49-59_portrait-916-cpu-optimization before modifying performance-related files.

### Chat Log Updated
Yes.

### Next Steps
If the user still sees high CPU while live camera preview is active, add a UI quality selector such as Low CPU / Balanced / Smooth and optionally pause live preview when the window is minimized or when upload/gallery mode is selected.

## Memory Entry - 2026-05-22 10:07
### User Request
Fix the portrait 9:16 app bug where the top-right header controls are not scaled correctly and buttons are hidden/clipped.

### Work Completed
Updated the portrait app header layout so the title/subtitle area cannot push the top-right controls out of view. Replaced the header DockPanel behavior with a two-column Grid and put the right-side controls inside a horizontal ScrollViewer. Added text trimming to the subtitle and explicit button widths/margins so the controls remain accessible on scaled or narrower Windows displays.

### Files Created or Modified
- pps/booth-windows-native-portrait-916/MainWindow.xaml: changed header layout and right-side control containment.
- .project_memory/chat_logs/CHAT_LOG.md: appended current request record.
- .project_memory/archives/ARCHIVE_INDEX.md: appended archive entry.

### Technical Decisions
The clipping was a layout allocation bug: the left header text could consume too much horizontal space in DockPanel. A Grid with an Auto right column plus a horizontal ScrollViewer makes the controls visible/accesssible without removing any buttons.

### Commands / Tests Run
- Created archive .project_memory/archives/2026-05-22_10-03-51_portrait-916-header-clipping-fix.
- Stopped the running portrait app and ran dotnet build D:\rocket\photobooth\apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release; build succeeded with 0 warnings and 0 errors.
- Launched D:\rocket\photobooth\Launch-Photobooth-Native-9x16.cmd; process responded normally.
- Functional validation pass: used Windows UI Automation to confirm Switch Landscape, Refresh Devices, and New Session are within the app window bounds.
- Bug/visual validation pass: inspected the XAML layout constraints and ran a second build check with --no-restore, which succeeded with 0 warnings and 0 errors.

### Problems Found
The screenshot helper captured the foreground browser instead of the WPF window, so visual verification used UI Automation bounding rectangles for the actual WPF controls.

### Archive Created
Yes: .project_memory/archives/2026-05-22_10-03-51_portrait-916-header-clipping-fix.

### Chat Log Updated
Yes.

### Next Steps
If buttons still appear too wide under very high Windows display scaling, reduce header button MinWidth values further or move language/orientation controls into a compact menu.

## Memory Entry - 2026-05-22 10:18
### User Request
Prepare the Photobooth website as a Cloudflare Pages full-stack deployment target for `afmpdt.space`, using Pages Functions/Workers, R2 photo storage, D1 metadata/session/PIN storage, authenticated upload API, admin APIs, PIN gallery, and local Photobooth sync documentation.
### Work Completed
- Read project memory and recorded the Cloudflare deployment task in chat log.
- Inspected current repo structure: root pnpm monorepo, existing `apps/admin-web` Next.js app, existing `apps/api` Worker prototype, and no prior root `functions/`, root `wrangler.toml`, or D1 migrations for this target.
- Created a new Cloudflare-native deployment app at `apps/cloudflare-pages` instead of forcing the existing Next.js app into static export.
- Added React/Vite frontend views for `/gallery`, `/pin`, `/admin`, and `/upload-test`.
- Added Cloudflare Pages Functions for `/api/upload`, `/api/photos`, `/api/events`, `/api/pin-lookup`, `/api/file/[key]`, `/api/admin/photos`, and `/api/admin/events`.
- Added R2/D1 binding config with `PHOTOS_BUCKET` and `DB`, plus `PUBLIC_SITE_URL=https://afmpdt.space`.
- Added D1 migration `migrations/0001_initial.sql` for `events`, `sessions`, `photos`, and `upload_logs`.
- Added deployment docs and local Photobooth sync API docs.
- Added D1 missing-schema JSON handling so API responses remain readable before migrations are applied.
- Checked source asset directories under `apps` and `packages`; no >25MB source files were found outside excluded build/runtime folders.
### Files Created or Modified
- `apps/cloudflare-pages/package.json`
- `apps/cloudflare-pages/tsconfig.json`
- `apps/cloudflare-pages/vite.config.ts`
- `apps/cloudflare-pages/index.html`
- `apps/cloudflare-pages/src/main.tsx`
- `apps/cloudflare-pages/src/styles.css`
- `apps/cloudflare-pages/functions/_shared.ts`
- `apps/cloudflare-pages/functions/api/upload.ts`
- `apps/cloudflare-pages/functions/api/photos.ts`
- `apps/cloudflare-pages/functions/api/events.ts`
- `apps/cloudflare-pages/functions/api/pin-lookup.ts`
- `apps/cloudflare-pages/functions/api/file/[key].ts`
- `apps/cloudflare-pages/functions/api/admin/photos.ts`
- `apps/cloudflare-pages/functions/api/admin/events.ts`
- `apps/cloudflare-pages/migrations/0001_initial.sql`
- `apps/cloudflare-pages/wrangler.toml`
- `apps/cloudflare-pages/DEPLOYMENT.md`
- `apps/cloudflare-pages/PHOTObooth_SYNC_API.md`
- `apps/cloudflare-pages/README.md`
- `DEPLOYMENT.md`
- `PHOTObooth_SYNC_API.md`
- `.project_memory/chat_logs/CHAT_LOG.md`
- `.project_memory/archives/ARCHIVE_INDEX.md`
- `PROJECT_MEMORY.md`
### Technical Decisions
- Added a separate Cloudflare Pages app instead of changing `apps/admin-web` to `output: export` or trying to deploy file-system-based Next API routes to Cloudflare Edge.
- Used Vite output directory `dist` because the Cloudflare app is React/Vite and build verified that output.
- Used R2 proxy route `/api/file/[key]` as the first storage access strategy so the R2 bucket can remain private.
- Kept upload and admin APIs protected by bearer secrets from Cloudflare env/secrets only; no real secrets were written to code or docs.
### Commands / Tests Run
- `npm.cmd install` in `apps/cloudflare-pages`: succeeded after PowerShell blocked `npm.ps1`.
- `npm.cmd run build` in `apps/cloudflare-pages`: succeeded.
- `npx.cmd wrangler pages functions build --outdir .tmp\functions-build`: succeeded.
- `npx.cmd wrangler d1 migrations apply DB --local --persist-to .wrangler/state`: migration SQL applied locally, but the running Pages dev instance did not read the migrated local table state.
- `npx.cmd wrangler pages dev dist ...`: Functions loaded locally and exposed `http://127.0.0.1:3340`.
- API checks: unauthenticated admin/upload returned 401, invalid PIN returned 400, uninitialized D1 returned readable 503 JSON.
- Visual checks: desktop gallery and mobile PIN screenshots reviewed; mobile layout bugs were fixed and rechecked.
- Large file check: no >25MB source files found in `apps` or `packages` after excluding build/runtime folders.
### Problems Found
- PowerShell blocked `npm install` via `npm.ps1`; reran with `npm.cmd install` successfully.
- First TypeScript build failed on overly broad JSON parsing types; fixed with explicit casts.
- Initial Vite preview showed HTML-as-JSON API errors because Vite alone does not run Pages Functions; fixed frontend error message and documented Wrangler/Cloudflare requirement.
- Initial mobile layout overflowed; fixed responsive sidebar/navigation/form sizing.
- Local Wrangler D1 state did not expose migrated tables to the already running Pages dev server; production deployment still requires applying remote D1 migrations before use.
### Archive Created
- `.project_memory/archives/2026-05-22_10-02-00_cloudflare-pages-fullstack`
### Chat Log Updated
- Yes.
### Next Steps
- In Cloudflare, create R2 bucket `photobooth-photos`, D1 database `photobooth-db`, Pages project `photobooth-afmpdt`, and custom domain `afmpdt.space`.
- Replace `REPLACE_WITH_CLOUDFLARE_D1_DATABASE_ID` in `apps/cloudflare-pages/wrangler.toml` with the actual D1 database ID.
- Configure Cloudflare Pages secrets `UPLOAD_SECRET` and `ADMIN_TOKEN`, then apply remote D1 migrations and deploy.

## Memory Entry - 2026-05-22 12:33
### User Request
Organize all required Photo Booth content into one folder for upload/copy to the Windows computer where it will be used.

### Work Completed
Created a transferable package folder at D:\rocket\photobooth\deliverables\Photobooth-9x16-Windows-Package. The package includes the built portrait 9:16 app, assets/templates/frames/effects, a package-local launcher, a .NET runtime check script, README instructions, package manifest, and bundled digitcamcontrol support folder.

### Files Created or Modified
- deliverables/Photobooth-9x16-Windows-Package/app/: copied built portrait app release output.
- deliverables/Photobooth-9x16-Windows-Package/tools/digitcamcontrol/: copied bundled digiCamControl project folder for tethered camera workflows.
- deliverables/Photobooth-9x16-Windows-Package/Start-Photobooth-9x16.cmd: package-local launcher.
- deliverables/Photobooth-9x16-Windows-Package/Check-Runtime.cmd: checks for .NET 9 Desktop Runtime.
- deliverables/Photobooth-9x16-Windows-Package/README.txt: transfer/run instructions.
- deliverables/Photobooth-9x16-Windows-Package/package-manifest.json: package metadata.
- .project_memory/chat_logs/CHAT_LOG.md: appended current request record.
- PROJECT_MEMORY.md: appended this memory entry.

### Technical Decisions
Attempted to create a self-contained win-x64 publish, but this machine has no NuGet sources configured, so runtime packs could not be resolved. Created a complete framework-dependent package instead. The target computer must have .NET 9 Desktop Runtime x64 installed unless a self-contained package is produced later from a machine with NuGet access.

### Commands / Tests Run
- Inspected Release output and project file.
- Checked digitcamcontrol size: about 223.6 MB.
- Attempted dotnet publish ... -r win-x64 --self-contained true; failed because no NuGet source was configured and runtime packs could not be resolved.
- Ran dotnet build apps\booth-windows-native-portrait-916\booth-windows-native-portrait-916.csproj -c Release; succeeded with 0 warnings and 0 errors.
- Copied Release output and digitcamcontrol into the package folder.
- Launched deliverables\Photobooth-9x16-Windows-Package\Start-Photobooth-9x16.cmd; verified process started from the package app path and responded normally.
- Verified package contents: 224.4 MB, 415 files, launcher present, runtime check present, digiCamControl present, app exe present.

### Problems Found
The first package launcher test failed because pp/ was empty after the failed self-contained publish attempt. Re-copied the Release output into pp/ and retested successfully.

### Archive Created
No archive was created because this task only produced a new deliverable folder and did not modify source code, config, app logic, storage format, or UI behavior.

### Chat Log Updated
Yes.

### Next Steps
Copy the entire D:\rocket\photobooth\deliverables\Photobooth-9x16-Windows-Package folder to the target Windows computer. Run Check-Runtime.cmd, install .NET 9 Desktop Runtime x64 if needed, then run Start-Photobooth-9x16.cmd.

## Memory Entry - 2026-05-22 10:35
### User Request
User asked Codex to execute the Cloudflare go-live steps for the prepared Photobooth Pages app: create R2/D1, create Pages project, set secrets, run remote migration, deploy, and bind `afmpdt.space` if possible.
### Work Completed
- Verified Wrangler is authenticated to Cloudflare account `Ryanzhao36@outlook.com's Account` with Pages/D1/Workers permissions.
- Created R2 bucket `photobooth-photos`.
- Created D1 database `photobooth-db` with id `6cce0a9d-5720-41ff-9d54-59f3a318e6a1`.
- Created Pages project `photobooth-afmpdt`.
- Updated `apps/cloudflare-pages/wrangler.toml` with the real D1 database id.
- Applied remote D1 migration `0001_initial.sql` successfully.
- Set Cloudflare Pages production secrets `UPLOAD_SECRET` and `ADMIN_TOKEN` through Wrangler secret commands. Generated values were not written to files or memory; after a command issue, both secrets were rotated again with compatible random generation.
- Built and deployed `apps/cloudflare-pages/dist` to Cloudflare Pages.
- Verified production site `https://photobooth-afmpdt.pages.dev/gallery` returns 200.
- Verified production `/api/photos` returns JSON `{ ok: true, photos: [] }`.
- Verified production `/api/admin/photos` without token returns 401.
- Updated deployment doc with real D1 id and current Pages URLs.
### Files Created or Modified
- `apps/cloudflare-pages/wrangler.toml`
- `apps/cloudflare-pages/DEPLOYMENT.md`
- `.project_memory/chat_logs/CHAT_LOG.md`
- `.project_memory/archives/ARCHIVE_INDEX.md`
- `PROJECT_MEMORY.md`
### Technical Decisions
- Used Wrangler to create R2, D1, and Pages resources directly because the account was already authenticated.
- Kept secret values out of files, final response, and memory. Since early generated values appeared in command output, both Cloudflare secrets were immediately rotated again with fresh undisclosed values.
- Could not bind `afmpdt.space` automatically because current Wrangler Pages CLI has no custom-domain command and no direct `CLOUDFLARE_API_TOKEN` env var was available for REST API calls. This remains a Cloudflare Dashboard step.
### Commands / Tests Run
- `npx.cmd wrangler whoami`
- `npx.cmd wrangler r2 bucket create photobooth-photos`
- `npx.cmd wrangler d1 create photobooth-db`
- `npx.cmd wrangler pages project create photobooth-afmpdt --production-branch main`
- `npx.cmd wrangler d1 migrations apply photobooth-db --remote`
- `npx.cmd wrangler pages secret put UPLOAD_SECRET --project-name photobooth-afmpdt`
- `npx.cmd wrangler pages secret put ADMIN_TOKEN --project-name photobooth-afmpdt`
- `npm.cmd run build`
- `npx.cmd wrangler pages deploy dist --project-name photobooth-afmpdt --branch main`
- `Invoke-WebRequest https://photobooth-afmpdt.pages.dev/gallery`
- `Invoke-WebRequest https://photobooth-afmpdt.pages.dev/api/photos`
- `Invoke-WebRequest https://photobooth-afmpdt.pages.dev/api/admin/photos`
### Problems Found
- Wrangler Pages CLI does not expose a custom domain command in the installed version.
- `wrangler zones list` is not a valid command in Wrangler 4.93.1.
- Shell has no `CLOUDFLARE_API_TOKEN`, so REST API custom-domain automation could not be safely performed.
- One secret rotation attempt used an unavailable .NET method, so both secrets were rotated again using a compatible random generator.
### Archive Created
- `.project_memory/archives/2026-05-22_10-23-00_cloudflare-go-live`
### Chat Log Updated
- Yes.
### Next Steps
- In Cloudflare Dashboard, add custom domain `afmpdt.space` to Pages project `photobooth-afmpdt` and follow the displayed DNS/verification step.
- After domain is active, test `https://afmpdt.space/gallery`, `https://afmpdt.space/api/photos`, `/pin`, and `/admin`.
