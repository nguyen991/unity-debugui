# Ng Debug UI

In-game debug UI for Unity, built on UI Toolkit. Declare panels in code with lambdas, then tweak and
watch values while the game runs — on desktop (mouse) and mobile (touch). No prefabs, no scene setup.

## Install

Requires Unity 6000.0 or newer. The keyboard shortcut and `DebugUI.IsPointerOverUI` also need the
Input System package (`com.unity.inputsystem`); everything else works without it.

### From a Git URL (recommended)

Git must be installed and on the `PATH` (restart Unity / Unity Hub after installing it).

1. *Window → Package Manager*.
2. **+** (top-left) → **Install package from git URL…**
3. Paste and press **Install**:

```
https://github.com/nguyen991/unity-debugui.git?path=/Packages/com.ng.debugui
```

Or add it to `Packages/manifest.json` by hand:

```json
{
  "dependencies": {
    "com.ng.debugui": "https://github.com/nguyen991/unity-debugui.git?path=/Packages/com.ng.debugui",
    ...
  }
}
```

`?path=` is needed because the package is in a sub-folder of the repository.

**Pin a version** by adding a tag, branch or commit hash after `#`, so every machine gets the same code:

```
https://github.com/nguyen991/unity-debugui.git?path=/Packages/com.ng.debugui#v0.1.0
```

**Update** — without `#…`, Unity records the commit it installed in `Packages/packages-lock.json` and
keeps using it. To get the latest `main`, click **Update** on the package in the Package Manager (or
delete the `com.ng.debugui` entry from `packages-lock.json`). With a pinned version, change the `#…` part.

### From a local copy

Copy the `Packages/com.ng.debugui` folder into the project's `Packages/` folder, or use
*Package Manager → + → Install package from disk…* and pick its `package.json`.

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
| `Toggles(label, get, set)` | row of toggle buttons | enum: pick one; `[Flags]` enum: pick several. Also `string[]` + `int` index or per-index `bool`; see [below](#toggle-buttons-toggles) |
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

### Toggle buttons: `Toggles`

A row of buttons, one per option; selected ones are highlighted. Buttons wrap to the next line when
the row is full.

```csharp
enum Difficulty { Easy, Normal, Hard }
[Flags] enum Cheats { None = 0, God = 1, InfiniteAmmo = 2, NoClip = 4 }

p.Toggles("Difficulty", () => difficulty, v => difficulty = v);         // enum: pick one
p.Toggles("Cheats", () => cheats, v => cheats = v);                     // [Flags] enum: pick several
p.Toggles("Weapon", new[] { "Sword", "Bow", "Staff" },
          () => weapon, v => weapon = v);                               // int index, -1 = none
p.Toggles("Show", new[] { "FPS", "Grid", "Hitbox" },
          i => show[i], (i, on) => show[i] = on, buttonWidth: 80);      // one bool per option
```

- **`allowNone`** — whether every button can be off. Defaults: `false` for the `int` index overload
  (tapping the selected button does nothing; with `true` it clears the selection and the setter gets
  `-1`), `true` for `[Flags]` enums and the per-index `bool` overload (with `false` the last selected
  button can't be turned off). A plain enum has no "nothing" value, so it always has one selected.
- **`[Flags]` enums** — one button per single-bit value; `None = 0` and combined values such as
  `All = A | B` get no button. Bits of the game's value that have no button are kept when the user
  changes the selection.
- **Plain enums** — one button per distinct value (aliases show the first name), ordered by value.
  A value matching no button shows nothing selected.
- **Index overload** — a getter value out of range (or `-1`) shows nothing selected.
- **Per-index `bool` overload** — the setter is called only for the options the user changed.
- **`buttonWidth`** — `0` (default) fits each button to its text; `> 0` gives every button that width
  in panel px (scaled with the rest of the UI by `DebugUI.ReferenceResolution`).
- At most **64** options (a UI Toolkit limit); more, or `null` options, shows "Build failed: …".
- Leave the setter out to show the selection read-only.

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
- Move or hide the DBG button:

```csharp
DebugUI.ButtonPosition = DebugButtonPosition.TopLeft;  // TopLeft, TopRight, BottomLeft, BottomRight (default)
DebugUI.ButtonPosition = DebugButtonPosition.None;     // hidden: open with the shortcut or DebugUI.Open()
DebugUI.ButtonSize = 64f;                              // diameter in panel px (24 – 200, default 48)
```

  Tabs and panel content keep clear of the button at any size and corner.

  With `None` on a touch device there is no keyboard shortcut, so open the UI from your own code
  (e.g. a secret gesture calling `DebugUI.Toggle()`).

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
- **Settings reset** — `Width`, `Height`, `ReferenceResolution`, `MatchWidthOrHeight`, `SortingOrder`, `ButtonPosition`, `ButtonSize`, `ToggleKey` and added style sheets go back to their defaults when the
  app starts or Play Mode is entered (also with *Enter Play Mode Options* / no domain reload), so set them
  from startup code.

## Styling

### Size of everything: `DebugUI.ReferenceResolution`

The debug UI scales with the screen size, like a uGUI *Canvas Scaler* set to *Scale With Screen Size*,
so it takes the same share of the screen on every device whatever its DPI.

```csharp
DebugUI.ReferenceResolution = new Vector2Int(540, 960); // default
DebugUI.MatchWidthOrHeight = 0.5f;                      // 0 = fit width, 1 = fit height (default 0.5)

DebugUI.ReferenceResolution = new Vector2Int(360, 640);   // everything 1.5× bigger
DebugUI.ReferenceResolution = new Vector2Int(1080, 1920); // everything half the size, fits more
```

On a screen of exactly `ReferenceResolution` one panel px is one screen pixel; on any other screen the
UI is scaled so the matched side stays the same. A smaller reference means bigger text, controls and touch
targets. Match `0` keeps the width the same on every portrait phone; `1` keeps the height (use it with a
landscape reference such as `960×540`); `0.5` (default) also keeps the size when the device rotates. Prefer
this over a bigger `font-size` alone, which can overflow the 32px-high controls.

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
| `ngdbg-toggle` | the round DBG button (`ngdbg-toggle--top-left`, `--top-right`, `--bottom-left`, `--bottom-right` for its corner) |
| `ngdbg-text` | `Label(text)` |
| `ngdbg-row`, `ngdbg-row-label`, `ngdbg-value` | `Label(label, getter)` row, its name and its value |
| `ngdbg-button` | `Button` |
| `ngdbg-button--colored` | a `Button` given a color (hover/press feedback via `opacity`) |
| `ngdbg-slider` | `Slider` |
| `ngdbg-field` | `Field` (all types) |
| `ngdbg-toggles` | `Toggles` (its buttons have Unity's `unity-button-group__button` class; selected ones the `:checked` state) |
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
`ReferenceResolution`, `MatchWidthOrHeight`, `SortingOrder`, `ButtonPosition`, `ButtonSize`, `ToggleKey` (with the Input System), `AddStyleSheet()`, `RemoveStyleSheet()` — always compile. When the debug UI is off they never
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
