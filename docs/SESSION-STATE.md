# Session state — resume here

Last updated: 2026-11-17 (UTC)

## What this is

Android port seed for Open Rails. Repo: https://github.com/Anmol1008-2009/openrails-android
(public, GPLv3). Remote `origin/main` is the source of truth — pull before working.

## Verified working

- `test` job: 11 xunit v3 tests pass (MSTS tokenizer/parser + `.trk`/`.eng`/`.wag`/`.con` readers).
- `apk` job: builds a self-contained debug APK and verifies manifest + embedded assemblies.
- On device: installed and launched on Motorola Edge 50 Neo (Android 16 / API 36,
  arm64-v8a, Mali-G615). Renders at ~60 fps. Screenshot: `docs/device-running.png`.

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

## Device / adb

- Package: `org.openrails.android`
- Activity: `crc64c40e553b9fef0da3.MainActivity`
- Wireless-debugging connect address (changes when wireless debugging restarts):
  `192.168.31.66:35541` — re-pair if it drops: Settings → Developer options →
  Wireless debugging → Pair with code, then `adb pair HOST:PORT CODE`.
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
- `core/OpenRails.Core/` — portable MSTS parsing: `Msts/MstsTokenizer.cs`,
  `Msts/MstsParser.cs`, `Msts/MstsBlock.cs`, `Formats/{Route,Engine,Wagon,Consist}File.cs`.
- `tests/OpenRails.Core.Tests/` — xunit v3 (auto entry point, MTP).
- `.github/workflows/build-apk.yml` — `test` and `apk` jobs.

## Next steps (in order)

1. Load an actual route: wire `RouteFile`/`ConsistFile` into a sample-data screen
   (no 3D yet) to prove the parsers against real MSTS content.
2. Replace the cube with a minimal 3D scene: track from `.trk` nodes / a generated
   test route; keep it data-driven.
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
