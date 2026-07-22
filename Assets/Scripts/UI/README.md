# UI

UI Toolkit + Design System. Prefer UXML assets and DS classes from `Assets/DesignSystem/Docs/COMPONENTS.md` — do not build trees in C# unless you have to.

## Mental model

Two layers:

| Layer | Purpose | Escape? |
| --- | --- | --- |
| **Modal stack** (`IGameUi`) | Settings, Pause, OptionsPopup, ShipLibrary, NewCampaign, … | Yes — per-panel `ActionOnEscape` |
| **Toast chrome** | Non-blocking notifications | No |

Scene “roots” (MainMenu, ShipFactory, ShipStatus) stay in the scene and register with `SetRoot`. Overlays are **prefabs** pushed onto the stack.

```
[toast host — always on top]
[OptionsPopup]     ← Depth 3
[ProgressionSlots] ← Depth 2
[MainMenuController root — unkillable]
```

## `IGameUi`

Inject `[Inject] IGameUi _gameUi` — do not SerializeField panel prefabs on controllers.

```csharp
_gameUi.SetRoot(this); // once, from scene root Start
_gameUi.PushById<SettingsOverlayController>(UIPanelPrefabConstants.Settings);
_gameUi.ShowOptions("Title?", "…", onOption, options…);
_gameUi.Notify("Saved", PopupLevel.Info);
_gameUi.Pop(); // never removes root
```

- Prefab IDs: [`UIPanelPrefabConstants`](../Core/Constants/UIPanelPrefabConstants.cs)
- Prefabs bound in `GameProjectInstaller` (`WithId` + serialized slots)
- Implementation: [`GameUi`](Stack/GameUi.cs) on `Game.prefab` → Services (via `GameUiInstaller`)
- Scene roots set `ActionOnEscape` on their panel: `None` (MainMenu), `PushPause` (ShipFactory / MainGame / BattleShipPicker)

Wiring steps: [Stack/MANUAL_INTEGRATION.md](Stack/MANUAL_INTEGRATION.md).

## Panels (`PanelRendererBase`)

Each panel GameObject: `PanelRenderer` + controller (+ View if MVCVM).

- Starts **closed** (`IsOpen = false`). Visibility comes only from `Show()` / `Hide()`.
- `GameUi.Push` / `SetRoot` always call `Show()` — no manual `Show()` needed after push.
- `ActionOnEscape` (default `Pop`) — `None` to ignore Escape, `PushPause` to open the pause overlay.
- Close / Back should call `_gameUi.Pop()`, not only `Hide()`.

Lifecycle ([`PanelRendererLifecycle`](Common/PanelRendererLifecycle.cs)):

1. `OnEnable` → register PanelRenderer reload callback → `BindUI`
2. `OnDisable` → **`UnbindUI` then clear root** (unsubscribe events!)

Without Unbind on disable, a destroyed stack panel can keep listening to repositories and NRE into dead VisualElements.

## MVCVM

```csharp
[RequireComponent(typeof(FooView))]
public class FooController : Controller<FooModel, FooView, FooViewModel> { … }
```

- View is **on the prefab** — `GetComponent`, never `AddComponent` at runtime.
- View inherits `PanelRendererBase` (binds UXML, raises UI events).
- Controller owns model + wiring; `Show()`/`Hide()` forward to View.

Generator: Tools → UI Toolkit → Create MVCVM Screen.

## Design System

- Dedicated UXML per screen/overlay; USS only for screen-specific bits.
- Prefer DS classes (`.ds-btn`, `.ds-dialog`, `.ds-backdrop`, …).
- UI Toolkit gaps: no `gap`, limited `calc` — use margins/padding.

## Lessons learned

1. **No `showByDefault`** — stack always shows what it pushes; roots call `Show()` via `SetRoot`.
2. **Unbind on disable** — Pop destroys the GO; unbind must clear model/UI subscriptions or you get “works on second try” NREs (e.g. ProgressionSlots after NewCampaign Save).
3. **PanelRenderer bind is async** — something may call into a panel before `BindUiCore` (E2E defeat in ~0.3s). Queue pending state and apply in `BindUiCore` (see `MissionResultUIController`).
4. **Toasts ≠ modal stack** — use `Notify`, not a second Escape-aware stack.
5. **Scene objects for 3D preview** — NewCampaign’s `DesignShip` stays in the scene; inject by ID via `DesignShipInstaller`, don’t put it on the UI prefab.
6. **Fail hard** — missing prefab IDs, missing View, missing `IGameUi` → throw. Silent nulls hide wiring mistakes.
7. **Don’t hand-edit `.meta`** — let Unity generate them.
8. **No bootstrapper** — present manual integration steps; place `GameUi` / installers yourself.

## Adding a new overlay

1. UXML (+ USS if needed) under the relevant `Views/` folder.
2. Controller (`PanelRendererBase` or MVCVM). Prefer Pop on Close.
3. Prefab under `Assets/Prefabs/UI/Panels/` with PanelRenderer + components.
4. Add ID to `UIPanelPrefabConstants` and a SerializeField slot in `GameProjectInstaller`.
5. Push via `_gameUi.PushById<T>(…)` from the caller.
6. PlayMode test for open/close and Escape if it matters.

## Folder map

| Path | Role |
| --- | --- |
| `Stack/` | `IGameUi`, `GameUi`, installers |
| `Common/` | Panel base, Settings, Pause |
| `Components/` | Shared widgets (OptionsPopup, Notification) |
| `MVCVM/` | Controller / View base |
| `Scenes/` | Per-scene screens |
| `Tests/` | UI.Tests |
| `../Prefabs/UI/Panels/` | Instantiable panel prefabs |
