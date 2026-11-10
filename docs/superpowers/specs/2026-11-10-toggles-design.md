# `Toggles` builder: a group of toggle buttons

## Why

Testers need to pick one option (difficulty, weapon) or switch several flags at once (cheats,
overlays) with one tap each. Today that takes a `Field(bool)` per flag or a slider over an index.
A row of tappable labels shows every option and the current choice at a glance.

## Goals

- One builder method, `Toggles`, with two families of overloads:
  - **Enum** — buttons from the enum's names; a `[Flags]` enum becomes multi-select.
  - **Strings + index** — buttons from a `string[]`; single-select binds an `int`,
    multi-select binds one `bool` per index.
- Option to allow **nothing selected** (`allowNone`).
- Buttons **wrap to the next line** when the row is full.
- Option to set the **button width** (`buttonWidth`); by default each button fits its text.

## Non-goals

- Per-item `bool` lambdas (`g.Item("FPS", () => fps, …)`) — considered and dropped (approach C).
- Icons, colors per button, keyboard/gamepad navigation.

## Approaches considered

| | Approach | Trade-off | Decision |
|---|---|---|---|
| A | Enum (`[Flags]` = multi) | short, type-safe; needs an enum | **picked** |
| B | `string[]` + `int` index / `(int, bool)` per index | flexible, no enum; indexes shift if the list is reordered | **picked** |
| C | One `bool` lambda per button | natural for loose flags, clumsy for single-select | dropped |

Both A and B are built on UI Toolkit's own `ToggleButtonGroup` (Unity 2023.2+, so fine for the
package's 6000.0 minimum), which already handles single/multi selection and empty selection.

## API

```csharp
// A — enum
IDebugBuilder Toggles<TEnum>(string label, Func<TEnum> get, Action<TEnum> set = null,
                             bool allowNone = true, float buttonWidth = 0) where TEnum : struct, Enum;

// B — single select: index, -1 = nothing selected
IDebugBuilder Toggles(string label, string[] options, Func<int> get, Action<int> set = null,
                      bool allowNone = false, float buttonWidth = 0);

// B — multi select: one bool per index
IDebugBuilder Toggles(string label, string[] options, Func<int, bool> isOn, Action<int, bool> set = null,
                      bool allowNone = true, float buttonWidth = 0);
```

```csharp
p.Toggles("Difficulty", () => difficulty, v => difficulty = v);
p.Toggles("Cheats", () => cheats, v => cheats = v, allowNone: false);
p.Toggles("Weapon", new[] { "Sword", "Bow", "Staff" }, () => weapon, v => weapon = v);
p.Toggles("Show", new[] { "FPS", "Grid", "Hitbox" }, i => show[i], (i, on) => show[i] = on, buttonWidth: 80);
```

## Behavior

- **Label** on the left like `Field`; on phone-width screens (`ngdbg-narrow`) it moves above the
  buttons, like `Slider`.
- **`allowNone`**
  - Single-select (non-`[Flags]` enum, `int` index): default `false` — tapping the selected button
    does nothing. With `true`, tapping it clears the selection (index setter gets `-1`). A
    non-`[Flags]` enum has no "nothing" value, so `allowNone` is ignored for it.
  - Multi-select (`[Flags]` enum, `(int, bool)`): default `true`. With `false`, the last selected
    button can't be turned off.
- **`[Flags]` enums** — one button per single-bit value; `0` (e.g. `None`) and combined values
  (e.g. `All = A | B`) get no button. Bits of the game's value that have no button are kept
  when writing back.
- **Non-`[Flags]` enums** — one button per distinct value (aliases show the first name); a game
  value matching no button shows nothing selected.
- **Index out of range** (single) shows nothing selected; the setter is not called.
- **Multi `(int, bool)` setter** is called only for the indexes that changed, in index order.
- **`buttonWidth`** — `0` (default): each button fits its text. `> 0`: every button gets that width
  in panel px (scaled by `DebugUI.Scale` like everything else).
- **More than 64 options** (the `ToggleButtonGroup` limit) or `options == null` → the build throws,
  which shows the usual red "Build failed: …" line.
- **Read-only** when `set` is `null`: buttons show the value but are disabled.
- Getter/setter errors, polling only while visible, and "not overwritten while editing" follow
  the existing rules for value controls.

## Styling

- `ngdbg-toggles` on the group; Unity's own button classes inside it can be targeted as usual.

## Also

- Add `Toggles` to the demo (`Assets/DebugUIDemo` and `Samples~/Demo`), the README Controls
  table and a short README section, and PlayMode tests.

## Left to the builder

- Exact USS for wrapping and the narrow-screen label.
- How the multi `(int, bool)` overload diffs old/new state.
