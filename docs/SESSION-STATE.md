# Session state — resume here

Last updated: 2026-11-18 (UTC)

## What this is

Android port seed for Open Rails. Repo: https://github.com/Anmol1008-2009/openrails-android
(public, GPLv3). Remote `origin/main` is the source of truth — pull before working.

## Verified working

- `test` job: 17 xunit v3 tests pass (MSTS tokenizer/parser, `.trk`/`.tdb`/`.eng`/
  `.wag`/`.con` readers, plus the bundled sample route/consist).
- `apk` job: builds a self-contained debug APK, verifies manifest, embedded
  assemblies, and that the sample content assets are packaged.
- On device (before this change): installed and launched on Motorola Edge 50 Neo
  (Android 16 / API 36, arm64-v8a, Mali-G615). Renders at ~60 fps.
  Screenshot: `docs/device-running.png`.
- Startup now opens a **sample-content screen** (no 3D): it parses the bundled
  `.trk`, `.tdb`, and `.con` from assets and shows route metadata, track-node
  counts/positions, and the consist. "Start 3D demo" reveals the MonoGame cube.
  Device-verified 2026-11-18 on the same phone: screenshot
  `docs/device-content-screen.png`; startup log reports
  `tdbNodes=5 trackSections=5 consistVehicles=3`.

## Non-obvious gotchas (do not regress these)

1. `EmbedAssembliesIntoApk` **must be `True` with a capital T**. The .NET Android
   targets compare case-sensitively; `true` is treated as false and the APK then
   uses Fast Deployment, which aborts on sideload with:
   `No assemblies found in .../.__override__/arm64-v8a`.
   Also keep `AndroidEnableFastDeployment=false`.
2. CI fails if `lib/*/lib_OpenRailsAndroid.dll.so` is missing (regression guard).
3. Each CI run generates a **new debug keystore**, so installing over an old build
   fails with `INSTALL_FAILED_UPDATE_INCOMPATIBLE`. Run `adb uninstall org.openrails.android` first.
4. Do **not** use `android-actions/setup-android@v3` — it runs the removed
   `sdkmanager tools` and fails. Use the runner's preinstalled SDK (see workflow).
5. `.NET 8` is EOL and SDK 11 turns it into error NETSDK1202. Target
   `net10.0-android36.0`; `global.json` pins SDK 10.
6. MonoGame `3.8.6.1` ships only `net8.0-android34.0`; it is consumed fine from net10.
7. MSTS track geometry is **not** in `.trk`. `.trk` holds `RouteID`, `Name`,
   `FileName`, `RouteStart`, etc.; `TrackNode`/`TrVectorSection` data is in the
   route's `.tdb` (same base name as `.trk`). Do not look for TrackNode in `.trk`.

## Device / adb

- Package: `org.openrails.android`
- Activity: `crc64c40e553b9fef0da3.MainActivity`
- Wireless-debugging connect address (changes when wireless debugging restarts):
  last used `192.168.31.66:37411` — re-pair if it drops: Settings → Developer
  options → Wireless debugging → Pair with code, then
  `adb pair HOST:PAIRPORT CODE`. The pairing port is different from the connect
  port shown on the main Wireless-debugging screen.
- `adb mdns` is unavailable in this adb build (`unknown host service 'mdns:...'`).
  To rediscover the connect port, send an mDNS query for
  `_adb-tls-connect._tcp.local` to `224.0.0.251:5353` and read the SRV record
  port; it worked from inside this PRoot container.
- Pairing socket closes the moment the pairing dialog is dismissed, so the port
  must come from an open dialog (split-screen or a second device helps).
- Runtime log on device: `/sdcard/Android/data/org.openrails.android/files/startup.log`
  (`adb shell cat ...`). The error screen also has Copy log / save to Downloads.
- Actions are **non-root** (`adb shell` user). No emulator here: aarch64 proot,
  no `/dev/kvm`, no system images.

Useful commands:

```bash
adb install -r openrails-latest.apk
adb shell am start -n org.openrails.android/crc64c40e553b9fef0da3.MainActivity
adb logcat -c && adb logcat -d -b crash
adb exec-out screencap -p > screen.png
```

## Local tooling in this container

- .NET SDK 10.0.404 (musl) at `/root/.dotnet` (not in the repo):
  `export PATH=/root/.dotnet:$PATH`
- Tests: `dotnet run --project tests/OpenRails.Core.Tests/OpenRails.Core.Tests.csproj -c Release`
- Android build only works in CI (no Android workload/SDK here).

## Code layout

- `src/MainActivity.cs` — activity, startup tracing, clipboard/Downloads log export.
- `src/Game1.cs` — MonoGame rotating cube (placeholder renderer).
- `src/SampleContent.cs` — loads bundled `.trk`/`.tdb`/`.con` and builds the summary.
- `src/ContentScreen.cs` — on-device sample-content screen (Android views, no 3D).
- `core/OpenRails.Core/` — portable MSTS parsing: `Msts/MstsTokenizer.cs`,
  `Msts/MstsParser.cs`, `Msts/MstsBlock.cs`, `Formats/{Route,TrackDatabase,Engine,Wagon,Consist}File.cs`.
- `samples/` — synthetic, GPL-safe content: `SampleRoute/SampleRoute.trk`,
  `SampleRoute/SampleRoute.tdb`, `SampleConsist.con`. Packaged as Android assets.
- `tests/OpenRails.Core.Tests/` — xunit v3 (auto entry point, MTP).
- `.github/workflows/build-apk.yml` — `test` and `apk` jobs.

## Next steps (in order)

1. ~~Load an actual route: wire `RouteFile`/`ConsistFile` into a sample-data
   screen~~ — done; now includes `.tdb` track nodes/sections.
2. Replace the cube with a minimal 3D scene driven by the `.tdb` vector-section
   positions (or a generated test route); keep it data-driven.
3. Touch input: multi-touch slider/button overlay writing into an
   `ExternalDeviceState`-style adapter (see handoff doc for Open Rails'
   `WebDeviceState` path).
4. Camera controls, then rolling stock shapes (`.s`) / textures (`.ace`).
5. Only after that: port simulation/sound. Note Open Rails is GPLv3 — keep this
   repo GPLv3 and publish source.

## Open questions / risks

- MonoGame on net10 is newer than its release matrix; watch for runtime issues.
- No automated UI test possible (no emulator); device testing is manual via adb.
- Apache/MST S content is copyrighted — do not commit route/stock assets.
