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
        DebugUI.Panel(this, "Player", p =>
        {
            p.Label("HP", () => hp);                                   // read-only, follows the game
            p.Slider("Speed", () => speed, v => speed = v, 0, 20);
            p.Field("Gold", () => gold, v => gold = v);                // int, float, string or bool
            p.Horizontal(h =>
            {
                h.Button("Heal", () => hp = maxHp);
                h.Button("Kill", () => hp = 0);
            });
            p.Foldout("Advanced", f =>
            {
                f.Slider("Gravity", () => Physics2D.gravity.y, v => Physics2D.gravity = new(0, v), -30, 0);
            });
        });
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
| `Button(text, onClick)` | button | exceptions are caught and logged |
| `Slider(label, get, set, min, max)` | slider + number box | `float` or `int` |
| `Field(label, get, set)` | input box | `int`, `float`, `string`; `bool` shows a toggle. Setter runs on Enter / leaving the box |
| `Vertical(b => …)`, `Horizontal(b => …)` | layout | children of a horizontal row share its width |
| `Foldout(title, b => …, open)` | collapsible section | closed by default |

Leave the setter out (or pass `null`) to make a control read-only.

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
