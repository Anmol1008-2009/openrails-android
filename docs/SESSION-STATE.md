# Session state — resume here

Last updated: 2026-11-18 (UTC)

## What this is

Android port seed for Open Rails. Repo: https://github.com/Anmol1008-2009/openrails-android
(public, GPLv3). Remote `origin/main` is the source of truth — pull before working.

## Current state / resume here

- `main` had reached `bde3048` ("Render data-driven .tdb track in the 3D scene")
  when this handoff was written.
- CI run `44244077282` for `bde3048`: **success** (`test` + `apk` jobs).
- The new APK was downloaded to `/workspace/openrails-latest.apk` (43.6 MB,
  from that CI run). `/workspace` is the durable copy; `/tmp` downloads may vanish.
- **Not yet done:** install that APK on the phone and visually verify the new 3D
  track scene. The phone's wireless debugging dropped before this could happen.
- Next concrete action: reconnect adb (below), then:
  ```bash
  adb uninstall org.openrails.android
  adb install /workspace/openrails-latest.apk
  adb shell am start -n org.openrails.android/crc64c40e553b9fef0da3.MainActivity
  # tap "Start 3D demo" (or after verifying) and screenshot:
  adb exec-out screencap -p > /tmp/track-scene.png
  adb shell cat /sdcard/Android/data/org.openrails.android/files/startup.log
  ```
  Expected startup log: `Sample content parsed: ... tdbNodes=6 trackSections=7
  consistVehicles=3` and no crash buffer entries. Save a good screenshot as
  `docs/device-track-scene.png`, then update README + this file.

## Verified working

- `test` job: 17 xunit v3 tests pass (MSTS tokenizer/parser, `.trk`/`.tdb`/`.eng`/
  `.wag`/`.con` readers, plus the bundled sample route/consist).
- `apk` job: builds a self-contained debug APK, verifies manifest, embedded
  assemblies, and that the sample content assets are packaged.
- On device: Motorola Edge 50 Neo (Android 16 / API 36, arm64-v8a, Mali-G615),
  installed/launched successfully; old cube screenshot `docs/device-running.png`.
- Sample-content screen is **device-verified** (commit `d039eec`): screenshot
  `docs/device-content-screen.png`; it parsed `tdbNodes=5 trackSections=5
  consistVehicles=3` at that time.
- Since then the sample grew to 6 track nodes / 7 vector sections and `Game1`
  now renders that data as a track scene. This newer build is CI-green and the
  APK is at `/workspace/openrails-latest.apk`, but it is **not yet
  device-verified**.

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
8. The namespace `OpenRails.Android` shadows the global `Android` namespace.
   `Android.Util.X` becomes `OpenRails.Android.Util.X` and fails to compile.
   Add `using Android.Util;` / `using Android.Graphics;` and use the short type
   name, or prefix with `global::Android...`. (CI caught this once.)
9. Startup order matters: `MainActivity` now loads `SampleContent` first, then
   constructs `new Game1(sample.TrackDatabase)`. Keep it that way.
10. CI Android builds work only in CI here: local JDK is broken under proot
    ("Failed to mark memory page as executable") and no Android SDK/workload is
    installed. Use `git push` -> GitHub Actions -> `gh run download`.

## Device / adb

- Package: `org.openrails.android`
- Activity: `crc64c40e553b9fef0da3.MainActivity`
- Phone IP has stayed `192.168.31.66`; **the port changes every time wireless
  debugging restarts**. Last successful connects: `:46667`, then `:43781`.
- Wireless debugging tends to turn off when the phone sleeps/locks. Re-enable
  Settings → Developer options → Wireless debugging. If a pairing code is
  needed, keep the dialog open and:
  `adb pair 192.168.31.66:PAIR_PORT CODE` (pairing port != connect port).
- `adb mdns` is unavailable in this adb build (`unknown host service 'mdns:...'`).
  Use the checked-in helper to rediscover the connect port:
  ```bash
  python3 scripts/adb-discover-port.py   # prints e.g. 192.168.31.66:43781
  ```
  It sends an mDNS query for `_adb-tls-connect._tcp.local` to
  `224.0.0.251:5353` and reads the SRV port. It worked from inside this PRoot
  container, even though arbitrary inbound TCP scans from the container don't.
- Once connected, keep the screen on: `adb shell svc power stayon true` and
  optionally `adb shell settings put system screen_off_timeout 1800000`.
  This did not fully prevent drops; re-enable as needed.
- Runtime log on device: `/sdcard/Android/data/org.openrails.android/files/startup.log`
  (`adb shell cat ...`). The error screen also has Copy log / save to Downloads.
- Actions are **non-root** (`adb shell` user). No emulator here: aarch64 proot,
  no `/dev/kvm`, no system images.
- The device is usually in Android desktop/freeform mode; `am start` may open the
  app in a floating window. Screenshots can be cropped for docs.

Useful commands:

```bash
python3 scripts/adb-discover-port.py
adb connect 192.168.31.66:PORT
adb uninstall org.openrails.android
adb install /workspace/openrails-latest.apk
adb shell am start -n org.openrails.android/crc64c40e553b9fef0da3.MainActivity
adb logcat -c && adb logcat -d -b crash
adb exec-out screencap -p > screen.png
```

## Local tooling in this container

- .NET SDK 10.0.404 (musl) at `/root/.dotnet` (not in the repo):
  `export PATH=/root/.dotnet:$PATH`
- Tests: `dotnet run --project tests/OpenRails.Core.Tests/OpenRails.Core.Tests.csproj -c Release`
- Android build only works in CI (see gotcha 10).
- `gh` is authenticated as `Anmol1008-2009` with `repo` + `workflow` scopes;
  pushing to `main` triggers the build. `gh run download RUN_ID -n
  openrails-android-debug-apk -D DIR` downloads the APK (artifact ~45 MB, can
  take several minutes on this connection).

## Code layout

- `src/MainActivity.cs` — activity, startup tracing, load sample content, create
  `Game1`, clipboard/Downloads log export.
- `src/Game1.cs` — **data-driven 3D track viewer**: centers/scales `.tdb` world
  points, draws ground grid, ballast ribbons, rails, centerline, and end/junction
  markers using `BasicEffect` + `VertexPositionColor`.
- `src/SampleContent.cs` — loads bundled `.trk`/`.tdb`/`.con` and builds the summary.
- `src/ContentScreen.cs` — on-device sample-content screen (Android views, no 3D).
- `core/OpenRails.Core/` — portable MSTS parsing: `Msts/MstsTokenizer.cs`,
  `Msts/MstsParser.cs`, `Msts/MstsBlock.cs`,
  `Formats/{Route,TrackDatabase,Engine,Wagon,Consist}File.cs`.
- `samples/` — synthetic, GPL-safe content: `SampleRoute/SampleRoute.trk`,
  `SampleRoute/SampleRoute.tdb` (6 nodes, 7 vector sections, curved main +
  branch), `SampleConsist.con`. Packaged as Android assets.
- `scripts/adb-discover-port.py` — mDNS wireless-debugging port discovery.
- `tests/OpenRails.Core.Tests/` — xunit v3 (auto entry point, MTP).
- `.github/workflows/build-apk.yml` — `test` and `apk` jobs.

## Next steps (in order)

1. ~~Load an actual route: wire `RouteFile`/`ConsistFile` into a sample-data
   screen~~ — done; includes `.tdb` track nodes/sections, device-verified.
2. ~~Replace the cube with a minimal 3D scene driven by `.tdb` vector-section
   positions~~ — implemented in `bde3048`, CI-green. **Remaining:** install
   `/workspace/openrails-latest.apk`, verify/screenshot the track scene, then
   mark this done in README/SESSION.
3. Touch input: multi-touch slider/button overlay writing into an
   `ExternalDeviceState`-style adapter (see handoff doc for Open Rails'
   `WebDeviceState` path).
4. Camera controls, then rolling stock shapes (`.s`) / textures (`.ace`).
5. Only after that: port simulation/sound. Note Open Rails is GPLv3 — keep this
   repo GPLv3 and publish source.

## Open questions / risks

- MonoGame on net10 is newer than its release matrix; watch for runtime issues.
- The 3D scene camera is currently fixed at `(105, 95, 130)` looking at origin;
  adjust after seeing the device screenshot.
- No automated UI test possible (no emulator); device testing is manual via adb.
- Apache/MST S content is copyrighted — do not commit route/stock assets.
