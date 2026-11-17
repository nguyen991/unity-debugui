# `com.ng.haptics`: haptic feedback and vibration for Android and iOS

## Why

Casual games need small, crisp physical feedback on three kinds of moments: **UI taps**,
**collisions** (stronger hit → stronger feedback), and **scoring** (often many times in quick
succession). Unity only offers `Handheld.Vibrate()`, a fixed ~400 ms buzz with no control — wrong
for all three. This package gives one C# API that plays the right native effect on each platform and
degrades gracefully on cheap Android hardware.

## Goals

- One static C# API for **semantic haptics**: `Impact` (5 styles + intensity 0–1), `Selection`,
  `Notification` (success / warning / error).
- A plain **`Vibrate(ms, amplitude)`** and **`Cancel()`** for longer buzzes.
- **Fallback chain** inside the native code so every call does the best the device can.
- **Capability query** so a game (and the test sample) can see what the device supports.
- Global **`Enabled`** switch for the game's settings menu and a global **throttle** so rapid
  scoring does not turn into a continuous buzz.
- **No per-call GC allocation**, no noticeable latency between the call and the feel.
- **Respect the user's system settings**: if they turned system haptics / touch feedback off, the
  semantic calls stay silent.
- A **tester sample** built with `com.ng.debugui` to feel every effect on a real device.

## Non-goals

- Custom pattern authoring: Core Haptics patterns / AHAP files, multi-step Android compositions,
  intensity/sharpness curves. Deferred to a later version if needed.
- Gamepad rumble, audio-synced haptics, WebGL `navigator.vibrate`, desktop.
- ScriptableObject presets or an editor UI for designing haptics.
- Persisting `Enabled` — the game owns its settings storage.

## Anti-goals (would be a failure even if it "works")

- A light tap turning into a long, mushy buzz on a cheap Android phone (ERM motor).
- GC allocation or JNI churn on every call — these fire on every collision.
- Haptics playing when the user has disabled system haptics.
- Requiring the game to edit its AndroidManifest or Xcode project by hand.

## Constraints

- Unity **6000.0+** (this project: 6000.6). Package folder: `Packages/com.ng.haptics`, embedded in
  this repo next to `com.ng.debugui`, same layout (`Runtime/`, `Tests/`, `Samples~/`, `README.md`).
- Android: works on **API 26+** (this project's minSdk); anything below falls back to legacy
  `vibrate(ms)` if Unity still builds for it. Richer paths gated at runtime by API level and
  device support.
- iOS: **13+** (Unity 6 minimum; this project targets 15). iPads have no Taptic Engine → no-op.
- Editor and all other platforms: no-op.

## Approaches considered

| | Approach | Trade-off | Decision |
|---|---|---|---|
| A | Java class + Obj-C++ file inside the package; C# is a thin facade making one native call per effect | Two extra languages, but fallback logic lives next to the native APIs, calls are cheap, capability checks cached natively | **picked** |
| B | Android driven entirely from C# via `AndroidJavaObject`; `.mm` for iOS | All logic in C#, but many JNI round-trips per call, verbose, easy to allocate | dropped |
| C | Wrap a third-party haptics library | Fastest, but third-party dependency/license and an API we don't control | dropped |

## API

Namespace `Ng.Haptics`, assembly `Ng.Haptics.Runtime`.

```csharp
public enum HapticImpact { Light, Medium, Heavy, Soft, Rigid }
public enum HapticNotification { Success, Warning, Error }

[Flags]
public enum HapticCapabilities
{
    None             = 0,
    Vibrator         = 1 << 0, // device can vibrate at all
    AmplitudeControl = 1 << 1, // Android: hasAmplitudeControl(); iOS: Core Haptics available
    RichHaptics      = 1 << 2, // iOS: Taptic Engine / Core Haptics; Android: composition primitives supported
}

public static class Haptics
{
    public static bool Enabled { get; set; } = true;          // false → every call is a no-op (Cancel still works)
    public static float MinInterval { get; set; } = 0.05f;    // seconds; Impact/Selection closer than this to the last played one are dropped
    public static HapticCapabilities Capabilities { get; }    // queried once from native, cached
    public static bool IsSupported => Capabilities != HapticCapabilities.None;

    public static void Impact(HapticImpact style = HapticImpact.Medium, float intensity = 1f);
    public static void Selection();
    public static void Notification(HapticNotification type);

    public static void Vibrate(int milliseconds, float amplitude = 1f);
    public static void Cancel();
}
```

```csharp
// UI button
Haptics.Selection();
// collision
Haptics.Impact(HapticImpact.Heavy, Mathf.InverseLerp(2f, 15f, collision.relativeVelocity.magnitude));
// scoring (throttle stops a combo from becoming one long buzz)
Haptics.Impact(HapticImpact.Light);
Haptics.Notification(HapticNotification.Success);
// settings menu
Haptics.Enabled = settings.hapticsOn;
```

## Behavior (C# layer)

- `intensity` / `amplitude` clamped to 0–1; **0 → no-op**. `milliseconds <= 0` → no-op.
- `Capabilities` is informational only (sample readout, game options UI). The C# layer **never skips
  calls based on it** — the native side decides what to play; on an unsupported device that's a
  no-op. *(Design call.)*
- **Priority by type** *(decided with the user)*. Two tiers:
  - **Light tier** — `Impact`, `Selection`: the calls that fire in bursts.
  - **Important tier** — `Notification`, `Vibrate`: never dropped by the throttle.
- **Throttle (light tier only):** one timestamp (`Time.realtimeSinceStartup`) of the last
  light-tier call that played. A light-tier call within `MinInterval` of it is dropped (not queued).
  `MinInterval = 0` disables it. Important-tier calls ignore the throttle and do not reset its
  timestamp. `Cancel` is never throttled.
- **No interrupting important effects:** while a `Notification` or `Vibrate` is still playing, a
  light-tier call is dropped. Needed on Android, where a new vibration cancels the current one; on
  iOS UIKit taps and Core Haptics overlap, so the rule is only enforced on Android. The Android
  backend tracks a "busy until" time (now + the effect's total duration of the path actually used)
  when it plays an important effect; `Cancel` clears it. A new important call always plays and
  replaces the current one.
- Example: tap-tap-tap during a combo, then `Notification(Success)` → the taps are thinned to one per
  `MinInterval`, Success always plays, and taps that land during Success are skipped.
- **Backgrounding:** on `Application.focusChanged(false)` call `Cancel()` so a long `Vibrate`
  doesn't continue while the app is in the background.
- **Backend seam:** an internal `IHapticsBackend` (Android / iOS / Null) chosen at first use; tests
  swap in a fake via `InternalsVisibleTo`. The Null backend is used in the Editor and on other
  platforms and reports `Capabilities.None`.

## Platform mapping

### iOS (`Runtime/Plugins/iOS/NgHaptics.mm`, `extern "C"` + `[DllImport("__Internal")]`)

| Call | Native |
|---|---|
| `Impact(style, i)` | cached `UIImpactFeedbackGenerator(style)` → `impactOccurredWithIntensity:i` |
| `Selection()` | cached `UISelectionFeedbackGenerator` → `selectionChanged` |
| `Notification(t)` | cached `UINotificationFeedbackGenerator` → `notificationOccurred:t` |
| `Vibrate(ms, a)` | Core Haptics: one continuous event, duration ms, intensity a, sharpness 0.5. No Core Haptics → `AudioServicesPlaySystemSound(kSystemSoundID_Vibrate)` (fixed length) |
| `Cancel()` | stop the Core Haptics player / engine |
| Capabilities | `Vibrator` on every iPhone (`UIUserInterfaceIdiomPhone`, so iPhone 7 with Taptic Engine but no Core Haptics still counts); plus `AmplitudeControl | RichHaptics` when `CHHapticEngine.capabilitiesForHardware.supportsHaptics`; `None` on iPad / simulator |

- Generators are created lazily, `prepare` is called on creation and again after each fire to keep
  latency low.
- The Core Haptics engine is created lazily and restarted on its `stoppedHandler` / `resetHandler`
  (it stops when the app backgrounds).
- UIKit generators already respect the "System Haptics" setting — nothing extra needed.

### Android (`Runtime/Plugins/Android/`, Java class `com.ng.haptics.NgHaptics`)

Each semantic call tries, in order, the first path the device supports (support checks cached once):

1. **Composition primitive** (API 30+, `areAllPrimitivesSupported`), scaled by intensity.
2. **Predefined effect** (API 29+). Cannot be scaled; intensity only gates 0 vs non-0.
3. **One-shot** (API 26+) with short duration and amplitude × intensity (amplitude ignored without
   `hasAmplitudeControl`).
4. **Legacy** `vibrate(ms)`.

Starting values (to be **tuned by feel on real devices**; keep one-shot durations short, ≤ 30 ms,
to avoid the mushy-buzz anti-goal):

| Call | Primitive (scale) | Predefined | One-shot ms / amplitude 0–255 |
|---|---|---|---|
| Impact Light | TICK (0.5·i) | EFFECT_TICK | 10 / 80·i |
| Impact Medium | CLICK (0.7·i) | EFFECT_CLICK | 20 / 150·i |
| Impact Heavy | THUD if supported, else CLICK (1.0·i) | EFFECT_HEAVY_CLICK | 30 / 255·i |
| Impact Soft | LOW_TICK if supported, else TICK (0.6·i) | EFFECT_TICK | 15 / 60·i |
| Impact Rigid | CLICK (1.0·i) | EFFECT_CLICK | 10 / 255·i |
| Selection | TICK (0.3) | EFFECT_TICK | 5 / 60 |
| Success | CLICK, CLICK after 80 ms | EFFECT_DOUBLE_CLICK | waveform 2 × 15 ms |
| Warning | CLICK, THUD/CLICK after 120 ms | EFFECT_DOUBLE_CLICK | waveform 2 × 20 ms |
| Error | CLICK ×3, 60 ms apart | EFFECT_HEAVY_CLICK | waveform 3 × 20 ms |

- `Vibrate(ms, a)` → `createOneShot(ms, a·255)` (legacy `vibrate(ms)` below 26). `Cancel()` → `vibrator.cancel()`.
- **Usage attributes:** semantic calls use touch usage (`VibrationAttributes.USAGE_TOUCH` on 33+,
  `AudioAttributes.USAGE_ASSISTANCE_SONIFICATION` before) so the system "touch feedback" setting
  applies. `Vibrate` uses media usage.
- **System setting on API 26–32 is best effort** *(design call)*: the platform only partly applies
  the touch-feedback toggle to `Vibrator` calls there. We do **not** read
  `Settings.System.HAPTIC_FEEDBACK_ENABLED` ourselves — it is deprecated and some OEMs report it
  wrong, which would silence haptics for users who have them on. README documents this; the game's
  `Haptics.Enabled` switch is the reliable off-switch.
- **`Vibrate` is exempt from the system haptics setting** on both platforms *(design call)*: it is
  the gameplay-rumble equivalent of `Handheld.Vibrate()`, controlled by `Haptics.Enabled`. iOS
  behavior with System Haptics off is whatever Core Haptics does; README notes the possible
  platform difference.
- Vibrator obtained via `VibratorManager` on 31+, `Context.VIBRATOR_SERVICE` before. Context comes
  from C# at init.
- **Permission:** `android.permission.VIBRATE` must reach the merged manifest with no action from
  the game (e.g. ship the Java source inside a `.androidlib` folder with its own `AndroidManifest.xml`).
- **C# → Java calls** go through `AndroidJNI` with cached class / static method IDs and reused
  `jvalue[]` buffers — no `AndroidJavaObject` per call, no allocation. Java exposes a handful of
  static methods taking ints/floats (effect id, intensity, ms, amplitude) plus `capabilities()`.

## Package layout

```
Packages/com.ng.haptics/
  package.json            name com.ng.haptics, displayName "Ng Haptics", unity 6000.0, no dependencies
  README.md               install, usage, platform notes (iPad no-op, system settings, tuning)
  Runtime/
    Ng.Haptics.Runtime.asmdef
    Haptics.cs, HapticImpact.cs, HapticNotification.cs, HapticCapabilities.cs
    Backends/  IHapticsBackend.cs, AndroidHapticsBackend.cs, IosHapticsBackend.cs, NullHapticsBackend.cs
    Plugins/Android/…     Java source + manifest (VIBRATE)
    Plugins/iOS/NgHaptics.mm
  Tests/Runtime/          C# layer tests against a fake backend
  Samples~/HapticsTester/ DebugUI panel: every effect as a button, intensity slider, Vibrate ms/amp,
                          Enabled / MinInterval controls, capability readout
```

- The sample's asmdef references `DebugUI.Runtime` and uses a `versionDefines` +
  `defineConstraints` pair on `com.ng.debugui`, so importing it without DebugUI compiles nothing
  instead of erroring. README says the sample needs DebugUI.
- The sample is also wired into this project (under `Assets/`) so it can be built to a device here.

## Testing

- **Automated (Editor):** C# layer with a fake backend and an injectable clock — clamping,
  0-intensity no-op, `Enabled`, light-tier throttle (drop within interval, `MinInterval = 0`),
  important tier never throttled and not resetting the throttle, `Cancel` never throttled, correct
  backend call per API, Null backend in Editor reports `None`.
- The Android "busy until" rule lives in Java, so it is checked manually on device (tap spam during
  Success / Error / a long `Vibrate`).
- **Manual (device):** the tester sample on at least one iPhone, one recent Android with LRA
  (primitives), one cheap Android (one-shot / legacy path). Check feel, latency, system-setting
  respect, background cancel. Tune the Android table from this.

## Left to the builder

- Exact Android packaging mechanism, as long as the permission lands without user action and it
  works when installed via git URL.
- Final Android tuning values, after device testing.
- Internal file/class naming beyond the public API above.
