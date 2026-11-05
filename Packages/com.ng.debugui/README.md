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

### How values stay in sync

- Each panel is built **once**. While the debug UI is open, the getters of the **visible** panel are
  polled every frame and the UI only updates when a value changes. Controls inside a closed foldout and
  panels in other tabs are not polled.
- While the debug UI is **closed, no getter runs**.
- A control the user is editing (dragging a slider, typing in a field) is not overwritten.
- If a getter or setter throws, that control turns red and the exception is logged once.

### Opening the UI

- Tap / click the round **DBG** button (bottom-right), or press **`** (backquote) when the
  Input System package is installed.
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
| Release build | **removed**: `DebugUI.Panel(...)` / `DebugUI.Remove(...)` calls are stripped by the compiler, lambdas and strings included |
| Release build with scripting define `DEBUG_UI_ENABLED` | on (e.g. a release build for testers) |
| Any build with scripting define `DEBUG_UI_DISABLED` | off everywhere (calls compile but do nothing) |

`DebugUI.IsOpen` / `Open()` / `IsPointerOverUI` always compile and simply do nothing / return `false` when
the debug UI is off.

## Sample

*Package Manager → Ng Debug UI → Samples → Demo* imports a scene that uses every control.
