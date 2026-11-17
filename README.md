# Open Rails for Android (early skeleton)

An experimental, community-style Android port seed for
[Open Rails](https://openrails.org/) — the open-source train simulator.
This is **not** a working simulator yet. It is a runnable MonoGame/.NET 10
Android baseline plus CI that produces an installable debug APK.

## Status

| Piece | State |
| --- | --- |
| MonoGame + .NET 10 Android app | working (rotating cube) |
| GitHub Actions debug APK build | working |
| Content parsers: `.trk`, `.eng`, `.wag`, `.con` | implemented, 11 unit tests |
| `.s` shape / `.ace` texture parsing | not started |
| 3D route/rolling-stock rendering | not started |
| Touch/cab controls | not started |

## Tests

The portable content core (`core/OpenRails.Core`) is plain .NET 10 with no
Android or MonoGame dependencies, so it runs anywhere:

```bash
dotnet run --project tests/OpenRails.Core.Tests/OpenRails.Core.Tests.csproj -c Release
```

CI runs this in the `test` job on every push, independently of the APK job.

## Build in CI

Push to `main` (public repo) or run the **build-apk** workflow manually.
The APK is uploaded as the `openrails-android-debug-apk` artifact.

Public-repo standard GitHub-hosted runners are free and do not consume
your monthly minutes. "Unlimited tokens" is not a GitHub Actions concept —
Actions is billed in **minutes**, and it has a 6-hour per-job cap.

## If the app crashes

The debug build now logs every startup step and shows failures on screen.

- On-device log: `/sdcard/Android/data/org.openrails.android/files/startup.log`
- If startup throws, the exception and that log are displayed in a scrollable,
  selectable text view instead of a blank crash.
- `adb logcat` (from Termux: `pkg install android-tools`, then Developer
  options → Wireless debugging → `adb pair` / `adb connect`):

```bash
adb logcat -d AndroidRuntime:E OpenRailsAndroid:V mono-rt:E DEBUG:E *:S
```

Recent yes/no checks already done: APK is a valid zip, both ABIs present
(`arm64-v8a`, `x86_64`), all packaged `.so` files are 16 KB-page aligned,
launcher activity resolves, target SDK 36.

## Build locally

Requires .NET 10 SDK, JDK 17, and Android SDK (API 34):

```bash
dotnet workload install android
dotnet build -c Debug -p:AndroidPackageFormat=apk
# -> bin/Debug/net8.0-android/org.openrails.android-Signed.apk
```

## Roadmap to a real port

1. **Content layer** — parse Open Rails route (`*.trk`), consist, `.eng` /
   `.wag`, shape (`.s`) and texture (`.ace`) formats into engine-neutral models.
2. **Renderer** — replace `BasicEffect` with a real 3D pipeline (instanced
   terrain, track, rolling stock). DirectX 9 code does not port; rewrite
   against OpenGL ES 3 / Vulkan.
3. **Simulation core** — reuse Open Rails' GPLv3 physics/route logic where
   it is portable, shimming Windows-only APIs.
4. **UI** — touch HUD, cab controls, in-game menus (WinForms editors must be
   replaced).
5. **Performance** — ARM profiling, LOD, asset streaming from storage.

## License

GPLv3 — see [LICENSE](LICENSE). Any distributed derivative of Open Rails
must remain GPLv3 with source available. Open Rails is © its contributors.
