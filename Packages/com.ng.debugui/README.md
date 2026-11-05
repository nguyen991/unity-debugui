# Ng Debug UI

In-game debug UI for Unity, built on UI Toolkit. Declare panels in code with lambdas, then tweak and
watch values while the game runs — on desktop (mouse) and mobile (touch). No prefabs, no scene setup.

## Install

The package lives in `Packages/com.ng.debugui`. To use it in another project, copy that folder into the
project's `Packages/` folder (or add it via *Package Manager → Install package from disk*).
Requires Unity 6000.0 or newer.

## Usage

```csharp
using Ng.DebugTools;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int hp = 100, maxHp = 100, gold;
    public float speed = 5;

    void OnEnable()
    {
        // `this` owns the panel: it disappears when this object is destroyed.
        DebugUI.Panel(this, "Player", p => p
            .Label("HP", () => hp)                                    // read-only, follows the game
            .Slider("Speed", () => speed, v => speed = v, 0, 20)
            .Field("Gold", () => gold, v => gold = v)                 // int, float, string or bool
            .Horizontal(h => h
                .Button("Heal", () => hp = maxHp, Color.green)
                .Button("Kill", () => hp = 0, Color.red))
            .Foldout("Advanced", f => f
                .Slider("Gravity", () => Physics2D.gravity.y, v => Physics2D.gravity = new(0, v), -30, 0)));
    }
}
```

Systems that are not MonoBehaviours pass `null` as the owner and remove the panel themselves:

```csharp
DebugUI.Panel(null, "Audio", p => p.Slider("Volume", () => AudioListener.volume, v => AudioListener.volume = v, 0, 1));
DebugUI.Remove("Audio");
```

### Controls

| Call | Shows | Notes |
|---|---|---|
| `Label(text)` | static text | |
| `Label(label, getter)` | `label   value` | any type, `ToString()` only when the value changes |
| `Button(text, onClick, color)` | button | `color` is optional (e.g. `Color.red`); text turns dark/light to stay readable. Exceptions are caught and logged |
| `Slider(label, get, set, min, max)` | slider + number box | `float` or `int` |
| `Field(label, get, set)` | input box | `int`, `float`, `string`; `bool` shows a toggle. Setter runs on Enter / leaving the box |
| `Vertical(b => …)`, `Horizontal(b => …)` | layout | children of a horizontal row share its width |
| `Each(() => list, (item, b) => …)` | one entry per list item | rebuilt only when items are added/removed/replaced; see below |
| `Line()` | thin separator | horizontal in a column, vertical inside `Horizontal` |
| `Space(size = 8)` | empty space | height in a column, width inside `Horizontal` |
| `Foldout(title, b => …, open)` | collapsible section | closed by default |

Leave the setter out (or pass `null`) to make a control read-only.

Every builder method returns the builder, so calls chain: `p.Label("Build 1.2.3").Label("HP", () => hp)`.
`Vertical` / `Horizontal` / `Foldout` return the *outer* builder, so the chain continues after the
layout at the same level. Writing one statement per line (`p.Label(...); p.Label(...);`) works too.

### Lists that change at runtime: `Each`

```csharp
DebugUI.Panel(this, "Enemies", p => p
    .Each(() => enemies, (e, b) => b
        .Label(e.name, () => e.hp)
        .Button("Kill", () => e.hp = 0, Color.red))
    .Button("Spawn", SpawnEnemy));
```

- The source is any `IReadOnlyList<T>` (`List<T>`, arrays); `null` shows nothing.
- Only the `Each` section is rebuilt, and only when the list's items change (count, or an item replaced —
  compared with `Equals`, i.e. by reference for classes). A change *inside* an item (`e.hp`) is shown by
  that item's getters without a rebuild, and an unchanged list costs no allocations.
- Don't call `DebugUI.Panel` every frame to change content — that rebuilds the whole panel (closing
  foldouts, dropping focus). Use `Each` for content that grows and shrinks.
- If the list changes while the tester is typing in a field *inside* that list, the typing is lost
  (that section is rebuilt).

### Built-in log panel

```csharp
DebugUI.LogPanel();                          // a "Logs" tab, keeps the newest 500 entries
DebugUI.LogPanel("Console", capacity: 2000); // custom tab name / size
```

- Log / Warning / Error filter toggles with counts, **Clear**, and a text search (case-insensitive).
- Oldest first; follows new entries while you are scrolled to the bottom. Warnings are yellow,
  errors/exceptions red. Tap an entry to see the full message and stack trace (selectable to copy).
- Captures from the moment you call it, from any thread — call it early (e.g. the first scene's
  `Awake`). Logs keep being captured while the debug UI is closed.
- Stripped from release builds like `Panel` (same `[Conditional]` rules).
- Style hooks: `ngdbg-log`, `ngdbg-log-toolbar`, `ngdbg-log-filter`, `ngdbg-log-search`, `ngdbg-log-list`,
  `ngdbg-log-row` (`--warning`, `--error`), `ngdbg-log-details`.

### How values stay in sync

- Each panel is built **once**. While the debug UI is open, the getters of the **visible** panel are
  polled every frame and the UI only updates when a value changes. Controls inside a closed foldout and
  panels in other tabs are not polled.
- While the debug UI is **closed, no getter runs**.
- A control the user is editing (dragging a slider, typing in a field) is not overwritten.
- After the user edits a value, the control shows the game's value again on the next refresh, so a setter
  that rejects or clamps the input (e.g. `v => gold = Mathf.Max(0, v)`) is reflected in the UI.
- A slider whose getter returns a value outside `[min, max]` shows it clamped, but does **not** call the
  setter — the game value is left untouched.
- If a getter throws, that control turns red (`ngdbg-error`) until the getter works again; the exception
  is logged once per control. If a setter throws, the exception is logged once and the control goes back
  to showing the game's value.

### Opening the UI

- Tap / click the round **DBG** button (bottom-right), or press **`** (backquote) when the
  Input System package is installed.
- Change or turn off the shortcut (Input System only):

```csharp
DebugUI.ToggleKey = Key.F1;    // using UnityEngine.InputSystem;
DebugUI.ToggleKey = Key.None;  // no shortcut, DBG button only
```

- From code: `DebugUI.Open()`, `DebugUI.Close()`, `DebugUI.Toggle()`, `DebugUI.IsOpen`.
- `DebugUI.Width` / `DebugUI.Height` size the window as a fraction of the screen, from `0.5f` (half)
  to `1f` (full, the default). The window is anchored to the top-right; the DBG button floats over its
  bottom-right corner.

```csharp
DebugUI.Width = 0.5f;   // right half of the screen
DebugUI.Height = 0.6f;  // top 60%
```

- `DebugUI.IsPointerOverUI` tells gameplay code that the pointer is over the debug UI, so a tap on the
  debug UI does not also count as a tap in the game (requires the Input System package):

```csharp
if (Pointer.current.press.wasPressedThisFrame && !DebugUI.IsPointerOverUI)
    Shoot();
```

## Behavior details

- **Same panel name again** — registering a name that already exists replaces that panel in place (same
  tab, same position), so calling `DebugUI.Panel` again from `OnEnable` after a re-enable or scene reload
  never duplicates it.
- **Owner destroyed** — the panel is removed at the latest on the next frame; if it was the open tab, the
  first remaining tab is selected.
- **`build` throws** — the controls built before the exception stay, followed by a red
  "Build failed: …" line; the exception is logged and other panels are unaffected. Inside `Each`, an item
  whose build throws shows "Item failed: …" and the other items are still built.
- **Null values** — `Label(label, getter)` shows `null`; a `string` `Field` shows an empty box.
- **Safe area** — the window always stays inside `Screen.safeArea` (notches, rounded corners).
- **Drawn on top of the game's UI** — `DebugUI.SortingOrder` (default `40000`) orders the debug UI among
  UI Toolkit panels and *Screen Space – Overlay* canvases; higher draws on top. The default is above the
  largest possible `Canvas.sortingOrder` (32767), so no uGUI canvas can cover it. Raise it if a UI Toolkit
  panel of the game uses a higher `PanelSettings.sortingOrder`; lower it if a game popup must appear above
  the debug UI. *Screen Space – Camera* and *World Space* canvases are drawn with the camera, below it.
- **Phone-width screens** (< 600 panel px, class `ngdbg-narrow`) — slider labels move above the track so
  the track stays wide enough to drag. `Width` / `Height` still apply.
- **`Each` inside `Horizontal`** — the items are laid out side by side and share the row's width.
- **Several log panels** — every `LogPanel` call shares one log buffer; the `capacity` of the latest call
  applies.
- **Settings reset** — `Width`, `Height`, `Scale`, `SortingOrder`, `ToggleKey` and added style sheets go back to their defaults when the
  app starts or Play Mode is entered (also with *Enter Play Mode Options* / no domain reload), so set them
  from startup code.

## Styling

### Size of everything: `DebugUI.Scale`

```csharp
DebugUI.Scale = 1.5f;   // text, controls and touch targets 50% bigger (range 0.5 – 3, default 1)
```

The UI already scales with the screen's DPI, so a control has about the same physical size on a
phone and a monitor; `Scale` multiplies that. Prefer it over a bigger `font-size` alone, which can
overflow the 32px-high controls.

### Fonts, colors, spacing: your own USS

Create a `.uss` file in your project and add it on top of the built-in theme:

```css
/* Assets/DebugUI/MyDebugUI.uss */
.ngdbg-root   { font-size: 16px; }                         /* all text */
.ngdbg-window { background-color: rgba(0, 0, 0, 0.8); }
.ngdbg-button { background-color: rgb(40, 90, 60); }
.ngdbg-tab--selected { background-color: rgb(200, 80, 40); }
```

```csharp
[SerializeField] StyleSheet debugStyle;

void Awake() => DebugUI.AddStyleSheet(debugStyle);   // DebugUI.RemoveStyleSheet(debugStyle) to undo
```

Your rules win over the built-in ones with the same selector. If a built-in rule is more specific
(see `Runtime/Resources/NgDebugUI/DebugUI.uss`), use at least the same selector, e.g.
`.ngdbg-root Label { color: … }`.

| Class | Element |
|---|---|
| `ngdbg-root` | full-screen container (set `font-size`, `color` here to affect everything) |
| `ngdbg-window` | the debug window |
| `ngdbg-tabbar`, `ngdbg-tab`, `ngdbg-tab--selected` | tab bar and tabs |
| `ngdbg-content` | scrolling area holding the current panel |
| `ngdbg-panel` | root of each panel |
| `ngdbg-toggle` | the round DBG button |
| `ngdbg-text` | `Label(text)` |
| `ngdbg-row`, `ngdbg-row-label`, `ngdbg-value` | `Label(label, getter)` row, its name and its value |
| `ngdbg-button` | `Button` |
| `ngdbg-button--colored` | a `Button` given a color (hover/press feedback via `opacity`) |
| `ngdbg-slider` | `Slider` |
| `ngdbg-field` | `Field` (all types) |
| `ngdbg-vertical`, `ngdbg-horizontal`, `ngdbg-foldout` | layouts |
| `ngdbg-each` | the container of an `Each` section |
| `ngdbg-line`, `ngdbg-space` | `Line()`, `Space()` (e.g. `.ngdbg-line { background-color: …; }`) |
| `ngdbg-narrow` | added to `ngdbg-root` on phone-width screens (< 600 panel px) |
| `ngdbg-error` | a control whose getter/setter threw |

Inside controls you can also target UI Toolkit's own classes, e.g. `.ngdbg-slider .unity-base-slider__dragger`.

## Builds

| Build | Debug UI |
|---|---|
| Editor | on |
| Development Build | on |
| Release build | **removed**: `DebugUI.Panel(...)`, `DebugUI.LogPanel(...)` and `DebugUI.Remove(...)` calls are stripped by the compiler, lambdas and strings included |
| Release build with scripting define `DEBUG_UI_ENABLED` | on (e.g. a release build for testers) |
| Any build with scripting define `DEBUG_UI_DISABLED` | off everywhere (calls compile but do nothing) |

The other members — `Open()`, `Close()`, `Toggle()`, `IsOpen`, `IsPointerOverUI`, `Width`, `Height`,
`Scale`, `SortingOrder`, `ToggleKey` (with the Input System), `AddStyleSheet()`, `RemoveStyleSheet()` — always compile. When the debug UI is off they never
create any UI: `Open()` / `Close()` / `Toggle()` do nothing, `IsOpen` and `IsPointerOverUI` return `false`,
and the settings are only stored.

## Known limitations

- **Unity 6.6+ Development Builds** — Unity 6.6 deprecates the `DEVELOPMENT_BUILD` define (warning UAC0009,
  silenced inside the package). It is still the only define that marks a development player on every
  supported version, so the package keeps using it. If a Development Build on 6.6+ shows no debug UI, add
  the scripting define `DEBUG_UI_ENABLED` to that build.
- **Shortcut while typing** — the shortcut key also toggles the UI when typed into a text field; pick a
  key you don't type (e.g. `DebugUI.ToggleKey = Key.F1`).
- **No gamepad / keyboard navigation** — the UI is made for mouse and touch.
- **Swiping over a number field's label** (`Gold`, `Jump`…) may change the value (UI Toolkit's
  drag-to-change) instead of scrolling the panel — swipe over the input box or empty space to scroll.
- **Demo scene** — uses URP's 2D sprite material; in a Built-in RP project the square shows as missing
  material (the debug UI itself works with any render pipeline).

## Running the package tests

Add the package to `testables` in `Packages/manifest.json`, then run the **PlayMode** tests
(*Window → General → Test Runner*):

```json
"testables": ["com.ng.debugui"]
```

## Sample

*Package Manager → Ng Debug UI → Samples → Demo* imports a scene that uses every control.
