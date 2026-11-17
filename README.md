# Open Rails for Android (early skeleton)

An experimental, community-style Android port seed for
[Open Rails](https://openrails.org/) — the open-source train simulator.
This is **not** a working simulator yet. It is a runnable MonoGame/.NET 8
Android baseline plus CI that produces an installable debug APK.

## Status

| Piece | State |
| --- | --- |
| MonoGame + .NET 8 Android app | working (rotating cube) |
| GitHub Actions debug APK build | working |
| Open Rails content parsing (.trk/.eng/.wag/.s/.ace) | not started |
| 3D route/rolling-stock rendering | not started |
| Touch/cab controls | not started |

## Build in CI

Push to `main` (public repo) or run the **build-apk** workflow manually.
The APK is uploaded as the `openrails-android-debug-apk` artifact.

Public-repo standard GitHub-hosted runners are free and do not consume
your monthly minutes. "Unlimited tokens" is not a GitHub Actions concept —
Actions is billed in **minutes**, and it has a 6-hour per-job cap.

## Build locally

Requires .NET 8 SDK, JDK 17, and Android SDK (API 34):

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
