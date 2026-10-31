# com.aqlaan.cozykit

The systems Aqlaan's cozy games share (`/unity-shared/README.md`). A system moves here only once it works in a game; these came from Gahwit Sitti on 2026-10-31 (games/gahwit-sitti/missing/24).

| Module | Assembly | What's in it |
|---|---|---|
| Core | `Aqlaan.CozyKit.Core` | `TimeService` (server time moved on by the phone's uptime clock, so changing the clock gains nothing), `DeviceClock` with its iOS uptime plugin, `SaveStore` (small text slots in the app's data folder), `IAnalytics` |
| Platform | `Aqlaan.CozyKit.Platform` | `PlatformClient` for the shared platform (`/platform`): guest and Apple/Google sign-in, cloud save, rewards, gem spending, purchases, remote config, events and social. `CloudSync`, `DeviceSecrets` (iOS Keychain plugin), `AppleSignIn` (iOS plugin), `PlatformAnalytics` (queued events), `PlatformConfig` (each game's `Resources/Config/platform.json`) |

Each game adds it to `Packages/manifest.json` as a local package, and lists it under `testables` so its tests run in the game's Test Runner:

```json
"com.aqlaan.cozykit": "file:../../../../unity-shared/com.aqlaan.cozykit"
```

The iOS plugins keep their items under the app's bundle id. Sign in with Apple also needs the capability and `AuthenticationServices.framework`, which each game's iOS post-build step adds.
