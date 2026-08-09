# Input system — research and refactoring proposals

Research-only analysis of player input in Space-RTS, evaluated against RTS genre conventions.
No code was changed. All statements below were verified against the code as of commit `9d6ddeb`;
anything that could not be verified is in [§7 Open questions](#7-open-questions).

---

## 1. Summary

- Input uses **Unity's new Input System 1.3.0 exclusively** — no legacy `Input.*` calls anywhere.
  Bindings live in a data asset (`../Assets/Data/Input/PlayerInputMaps.inputactions`), which is a
  good foundation for rebinding later.
- The entire control surface is **five mouse actions**: left-click select, right-click
  move/attack, scroll zoom, middle-button-drag rotate, edge-scroll pan. **There is not a single
  keyboard binding in the project.**
- Selection is **strictly one unit at a time** (`ShipInput._currentObject`). Box selection exists
  only as dead, unwired, and internally buggy scaffolding (`ShipsHandler`).
- Raw input is read in **three unrelated places**: an event-driven `IInput` service
  (`MouseInput`), plus two classes that poll `Mouse.current` directly every frame
  (`ObjectClicker`, `CameraMover`) — the project's own abstraction is bypassed by half its users.
- Click resolution goes through a **per-frame raycast cache** (`ObjectClicker.Update()` runs two
  raycasts every frame, clicks or not) that is **one frame stale** when the click callback reads it.
- There is **no input context concept**: no mode state machine, no UI click-through guard
  (`EventSystem` is in the scene but `IsPointerOverGameObject` is never called), no pause
  handling.
- Commands are **direct method calls** (`Shoot`, `MoveTo` → `NavMeshAgent.SetDestination`) —
  no command objects, no queuing, nothing serializable for replays/networking.
- Of the 16 standard RTS input conventions checked, **~1.5 are implemented** (right-click context
  command, partially; data-driven bindings as rebinding groundwork). The rest are unimplemented,
  not broken — this is an early prototype.
- The most impactful fixes, in order: multi-unit selection service, on-demand click raycasting,
  an input-context layer. A command-object layer is the gateway to queuing/replays but only pays
  off once multi-select exists.
- Several lifecycle warts: `MouseInput.Dispose()` is never called (and disposes before
  disabling), `IInput.OnErase` is declared but never raised, dead ships stay selectable and
  commandable forever.

---

## 2. Current architecture

### 2.1 Input backend

**New Input System 1.3.0 only.** Verified: no `UnityEngine.Input` (legacy) usage anywhere under
`../Assets/Scripts/`. Two consumption styles are used *simultaneously*:

1. **Action callbacks** through the generated wrapper
   [`PlayerInputMaps.cs`](../Assets/Data/Input/PlayerInputMaps.cs) (machine-generated from
   [`PlayerInputMaps.inputactions`](../Assets/Data/Input/PlayerInputMaps.inputactions), do not
   hand-edit) — used by `MouseInput`.
2. **Direct device polling** via `Mouse.current.position.ReadValue()` — used by
   [`ObjectClicker.cs:24`](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs:24) and
   [`CameraMover.cs:109`](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs:109).

The action asset defines two maps, mouse-only, no control schemes:

| Map | Action | Binding | Notes |
|---|---|---|---|
| `Camera` | `MouseScrollDelta` | `<Mouse>/scroll/y` | `Normalize(min=-1,max=1)` processor |
| `Camera` | `MouseScroll` | `<Mouse>/middleButton` | button — rotation modifier |
| `Camera` | `MouseDelta` | `<Mouse>/delta` | polled via property, not callback |
| `Input` | `ChoosingClick` | `<Mouse>/leftButton` | `Press` interaction — fires on press |
| `Input` | `TargetingClick` | `<Mouse>/rightButton` | default interaction — fires on press |

Both clicks fire on button-*down*, so no press/drag/release distinction is possible with the
current actions — relevant for box selection later.

### 2.2 The classes

| Class | File | Kind | Update loop |
|---|---|---|---|
| `MouseInput` / `IInput` | [MouseInput.cs](../Assets/Scripts/Gameplay/Input/MouseInput.cs) | POCO service (`IService`, `IDisposable`) | event callbacks only |
| `ShipInput` | [ShipInput.cs](../Assets/Scripts/Gameplay/Input/ShipInput.cs) | MonoBehaviour (scene) | event callbacks only |
| `ObjectClicker` | [ObjectClicker.cs](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs) | `MonoSingleton` (scene) | `Update()` polling |
| `CameraMover` | [CameraMover.cs](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs) | MonoBehaviour (scene) | `Update()` polling + events |
| `ShipControlInterpreter` | [ShipControlInterpreter.cs](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs) | MonoBehaviour (per ship prefab) | none — called |
| `ShipsHandler` | [ShipsHandler.cs](../Assets/Scripts/Gameplay/Control/ShipsHandler.cs) | MonoBehaviour — **dead code** | none |

Wiring: `ProviderBuilder.RegisterServices()`
([ProviderBuilder.cs:43](../Assets/Scripts/Management/ProviderBuilder.cs:43)) creates
`MouseInput` and registers it as `IInput`; `GameInitiliazer.InitInput()`
([GameInitiliazer.cs:40](../Assets/Scripts/Management/GameInitiliazer.cs:40)) pushes it into
`ShipInput` and `CameraMover`. This follows the project's boot chain correctly.

### 2.3 Data flow

```
Mouse hardware
 │
 ├─(polled every frame)─ ObjectClicker.Update()            [Mouse.current.position]
 │       ├─ Physics.Raycast(_targetLayer, 30) ──► _currentObject   (cache)
 │       └─ Physics.Raycast(_backgroundLayer, 30) ► _pos           (cache)
 │
 ├─(polled every frame)─ CameraMover.Update() → MoveCenter()  [Mouse.current.position]
 │       └─ screen-edge test ──► _center.position (edge pan)
 │
 └─(event callbacks)── PlayerInputMaps (generated)
         └─ MouseInput : IInput           (service, created in ProviderBuilder)
              ├─ OnChoosingClick ──► ShipInput.ChooseClick()
              │      └─ reads ObjectClicker cache → IControllable.Select()/DeSelect()
              ├─ OnTargetingClick ──► ShipInput.TargetClick()
              │      └─ reads ObjectClicker cache
              │           ├─ hit ITargetable ──► ShipControlInterpreter.Target()
              │           │        └─ enemy side? ──► ShipWeaponsController.Shoot(target)
              │           └─ else ──► ShipControlInterpreter.TargetPosition(pos)
              │                    └─ ShipMovementController.MoveTo() → NavMeshAgent.SetDestination()
              ├─ OnScrollChange ──► CameraMover.ZoomCamera()
              ├─ OnScrollPressed ──► CameraMover.RotateChange()  (middle button = rotate mode)
              └─ MouseDelta (property, polled in Update) ──► CameraMover.RotateCamera()
```

Abstraction layers between device and game state: exactly one (`IInput`), and only for the
event-shaped inputs. Interpretation (what was clicked), decision (select vs. command) and
execution (mutate `NavMeshAgent` / weapons) all happen in one hop inside
`ShipInput` + `ShipControlInterpreter`, with `ShipInput` reaching into the `ObjectClicker.Instance`
singleton directly ([ShipInput.cs:25](../Assets/Scripts/Gameplay/Input/ShipInput.cs:25)).

### 2.4 Input modes / context

None exist. There is no selection-vs-targeting-vs-placement state, no pause, no cutscene, no UI
mode. No ad-hoc booleans either, except `CameraMover._isCameraRotation` (middle-button held →
edge pan off, rotate on — [CameraMover.cs:50](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs:50)).
`QuequeStateMachine` ([QuequeStateMachine.cs](../Assets/Scripts/Architecture/StateMachine/QuequeStateMachine.cs))
exists in the Architecture layer but has **zero users** in gameplay code — it is available
machinery, not an input feature.

UI click-through: the scene contains an `EventSystem` and canvases (world-space stat bars), but
`EventSystem.current.IsPointerOverGameObject()` is never called. Today there is no interactive
screen-space UI, so this is a latent gap, not an active bug.

### 2.5 Selection

- **Single click, single unit.** `ShipInput._currentObject` holds one `IControllable`; clicking
  another swaps, clicking empty space deselects
  ([ShipInput.cs:23-46](../Assets/Scripts/Gameplay/Input/ShipInput.cs)).
- **Side is not checked at selection time** — enemy ships can be selected (their selection GFX
  turns on); only *commands* are side-gated (`IsEnableToControl`,
  [ShipControlInterpreter.cs:39](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs:39)).
  **Confirmed by the owner: this is a feature** — StarCraft-style enemy inspection (select an
  enemy to check its stats). Note the payoff half is not built yet: nothing displays the selected
  ship's stats (`ShipUiStats` is an empty stub), so today the feature is selection GFX only.
- **Death is not checked at all** — nothing deselects or despawns a dead ship (no subscriber
  removes ships on `OnDeath`; ships stay in the scene), so a dead selected ship still accepts
  move orders. `ShipWeaponsController.Shoot` guards only against dead *targets*
  ([ShipWeaponsController.cs:55](../Assets/Scripts/Gameplay/Weapon/ShipWeaponsController.cs:55)).
- No click-vs-drag threshold, no box select, no modifiers (Shift/Ctrl/Alt do nothing — there are
  no keyboard bindings), no control groups, no sub-groups, no max selection size, no double-click.
- **Dead scaffolding:** `ShipsHandler.GetChosenShips(firstPos, secondPos, side)`
  ([ShipsHandler.cs:21](../Assets/Scripts/Gameplay/Control/ShipsHandler.cs:21)) is a
  world-space AABB query clearly written for box selection. It has **no callers**, the component
  is **not in `SampleScene`** (GUID absent from the scene file), and the math is wrong twice:
  `new Bounds(firstPos + size, size)` passes the *half*-diagonal as the full `Bounds.size`
  (box ends up half the intended extent), and nothing orders the two corners, so a drag toward
  −X/−Z produces a negative-size `Bounds` for which `Contains` fails. It also allocates
  (LINQ `Where(...).ToList()`) and would throw `KeyNotFoundException` for a side with no ships.

### 2.6 Command issuing

Right-click resolution ([ShipInput.cs:48-58](../Assets/Scripts/Gameplay/Input/ShipInput.cs:48)):

1. Selected object exists and `IsEnableToControl(_playerSide)` (side match — `_playerSide` is a
   `[SerializeField]` `SideData` on `ShipInput`).
2. Cached raycast hit an `ITargetable` → `Target(t)`; inside the interpreter, **enemy side →
   `Shoot`, own side → silent no-op** ([ShipControlInterpreter.cs:28-32](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs:28)).
   No follow, assist, repair, or board semantics.
3. Otherwise → `TargetPosition(backgroundHit)` → `NavMeshAgent.SetDestination`.

Notes:

- A move order does **not** stop an active attack, and an attack order does not stop movement —
  the two subsystems are fully orthogonal. A ship ordered away keeps firing while it leaves
  weapon range (turret-style, fire-at-will behavior). **Confirmed by the owner: deliberate** —
  move must not stop the guns. Any future order layer (P4) must preserve this policy.
- No attack-move, hold, patrol, stop, rally points, no order queue (Shift does nothing), no
  queue visualization, and no acknowledgment feedback of any kind (no move marker, no sound).

### 2.7 Camera

All in [CameraMover.cs](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs), a rig of
`_center` (pivot, moves/rotates) + `Camera.main` (child, zooms along its local offset, always
`LookAt(_center)`):

| Feature | Status |
|---|---|
| Edge scroll | ✅ `MoveCenter()`, 50 px trigger border (serialized), clamped to `_cameraBorders`, speed scaled by zoom via `Clamp01(_currentZoom / (_maxZoomDistance - _minZoomDistance))` — a ratio against the *range width*, an odd formula worth a design look |
| Zoom | ✅ scroll, lerped, clamped 1–20 (serialized scene values) |
| Rotation | ✅ middle-drag, pitch clamped; edge pan disabled while rotating |
| WASD / arrow pan | ❌ no keyboard bindings exist |
| Drag pan | ❌ |
| Bookmarks / follow / center-on-selection | ❌ |
| Minimap | ❌ no minimap at all |

Edge scroll runs whenever the scene runs — there is no application-focus or cursor-in-window
check. In windowed mode this behaves as: pan whenever the OS cursor is near/beyond the game
window edge while the window has focus.

### 2.8 Hotkeys & rebinding

Bindings are **data-driven** in the `.inputactions` asset — the right foundation. But: five
actions total, zero keyboard, no runtime rebinding UI, no conflict detection, no layout concept
(grid/classic). No hardcoded keycodes anywhere in gameplay code (verified by grep) — the only
hardcoded input references are the `Mouse.current` polls noted above.

### 2.9 Technical quality

- **Allocations:** the live input path is allocation-free per frame (single-hit
  `Physics.Raycast` does not allocate). The two dead/empty classes (`ShipsHandler`,
  `ShipStatsController.DealDamage`'s LINQ) allocate, but not on input paths.
- **Raycast cost:** 2 raycasts × 60 fps regardless of whether the player clicks — cheap in
  absolute terms but structurally wrong: cost is paid always for data needed rarely.
- **Latency / order-of-execution:** the Input System (default settings — no custom
  `InputSettings` asset found) processes events in the player-loop Update phase *before*
  `MonoBehaviour.Update`. So `ChoseClick` runs before `ObjectClicker.Update()` on the click
  frame, and the cached hit is from the **previous frame's** mouse position. One frame at 60 fps
  is imperceptible, but this is an implicit ordering contract that nothing documents or enforces;
  moving `ObjectClicker` logic or changing the input update mode changes click behavior silently.
- **Hardcoded raycast range 30** ([ObjectClicker.cs:29](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs:29))
  vs. serialized `_maxZoomDistance: 20` in the scene — works today, but the two numbers are
  coupled and nothing keeps them in sync; zooming the camera out past ~30 world units of hit
  distance would make every click miss. Also `rawPos.z = 5` before `ScreenPointToRay` is a no-op
  (the ray ignores z) — a leftover.
- **Determinism / replays:** inputs mutate game state directly (`SetDestination`, `Shoot`).
  There is no command object, no tick stamping, no serialization point — nothing a replay or
  lockstep layer could capture.
- **Testability:** `MouseInput` is a POCO but news up `PlayerInputMaps` in its constructor
  (needs the Input System runtime); `ShipInput` needs a scene (`MonoBehaviour` +
  `ObjectClicker.Instance` singleton + serialized `SideData`). Nothing in the input path is
  unit-testable today. (There is no test suite in the project at all.)
- **Lifetime:** `MouseInput.Dispose()` has no caller anywhere; the generated wrapper's finalizer
  asserts if maps were never disabled → expect leak warnings on domain reload. The body is also
  order-reversed — it calls `Dispose()` (destroys the asset) *before* `Disable()`
  ([MouseInput.cs:56-60](../Assets/Scripts/Gameplay/Input/MouseInput.cs:56)).
  `ShipInput`/`CameraMover` never unsubscribe from `IInput` — benign while both live for the
  whole scene, a footgun the moment scenes reload.
- **Dead API:** `IInput.OnErase` is declared twice (interface + class) and never raised or
  subscribed.

---

## 3. Strengths

- **Single input backend, data-driven bindings.** New Input System only, actions in an asset —
  no legacy/`KeyCode` scatter to clean up later
  ([PlayerInputMaps.inputactions](../Assets/Data/Input/PlayerInputMaps.inputactions)).
- **An abstraction seam already exists.** `IInput`
  ([MouseInput.cs:63](../Assets/Scripts/Gameplay/Input/MouseInput.cs:63)) is a service behind the
  project's DI (`ProviderBuilder.RegisterServices()`), pushed into consumers through the boot
  chain — the pattern a bigger input system should slot into is already established.
- **Clean command-target seam on the unit side.** `IControllable` / `ISelectable`
  ([ShipInput.cs:62-76](../Assets/Scripts/Gameplay/Input/ShipInput.cs:62)) keeps `ShipInput`
  ignorant of ship internals; `ShipControlInterpreter` is a thin adapter onto
  `ShipEntity`'s components. Multi-select can reuse these interfaces unchanged.
- **Event-driven clicks, not per-frame `wasPressedThisFrame` polling** — subscriptions are made
  once in `Initialize`, consistent with the codebase's push-initialization style.
- **Camera feel basics are in place**: zoom is lerped, pitch is clamped, pan speed scales with
  zoom, pan area is clamped to world borders — someone tuned this.
- **Layer-mask-scoped raycasts** (`_targetLayer` / `_backgroundLayer` serialized on
  `ObjectClicker`) rather than raycast-everything-and-filter.

---

## 4. Weaknesses

Sorted by severity. "Cost" abbreviations: feel = player-facing feel, maint = maintainability.

| # | Severity | What is wrong | Where | What it costs |
|---|---|---|---|---|
| W1 | **High** | Selection model is hard-wired to exactly one unit; no box select, no modifiers. The architecture (single `_currentObject` field) cannot express multi-select without rework | `ShipInput._currentObject`, `ChooseClick()` ([ShipInput.cs:12,23](../Assets/Scripts/Gameplay/Input/ShipInput.cs:12)) | Core RTS loop missing; every selection feature is blocked on this (feel, maint) |
| W2 | **High** | Always-on raycast cache, read one frame stale by click callbacks; implicit execution-order contract between Input System update and `ObjectClicker.Update()`; hardcoded range 30 silently coupled to camera zoom (scene value 20) | `ObjectClicker.Update()` ([ObjectClicker.cs:22-36](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs:22)) | Bug risk (order/zoom changes break clicks invisibly), wasted per-frame work, hidden latency |
| W3 | **High** | No input-context concept: no mode state, no UI click-through guard (`IsPointerOverGameObject` never called), no pause handling. First interactive UI panel will leak clicks into the world | absence — `ShipInput`, `CameraMover`; `EventSystem` present in `SampleScene` | Bug risk that grows with every UI/feature addition (bugs, maint) |
| W4 | **Medium** | No command layer: input mutates `NavMeshAgent`/weapons directly. Blocks order queuing, feedback, AI reuse of the same order path, replays/networking, and testing | `ShipInput.TargetClick()` → `ShipControlInterpreter.Target/TargetPosition` ([ShipControlInterpreter.cs:28-37](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs:28)) | Architecture ceiling (maint, future features) |
| W5 | **Medium** | Dead ships stay selectable and commandable; nothing deselects on death. (Enemy-ship selection is *by design* — inspect-enemy feature — but its payoff, a stat display, is an empty stub: `ShipUiStats`) | `ChooseClick` (no death filter), `IsEnableToControl` side-only ([ShipControlInterpreter.cs:39](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs:39)); no `OnDeath` → selection hook | Zombie-orders feel, confusing feedback (feel, bugs) |
| W6 | **Medium** | Raw input read in three places; two bypass the project's own `IInput` abstraction with direct `Mouse.current` polling | [ObjectClicker.cs:24](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs:24), [CameraMover.cs:109](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs:109) | Rebinding/replay/testing can never be complete while consumers go around the seam (maint) |
| W7 | **Medium** | `MouseInput` lifetime: `Dispose()` never called by anyone, and its body destroys the asset before disabling it; consumers never unsubscribe | [MouseInput.cs:56-60](../Assets/Scripts/Gameplay/Input/MouseInput.cs:56); `ShipInput`/`CameraMover` `Initialize` | Editor leak asserts, breaks on scene reload (bugs) |
| W8 | **Low** | Dead/misleading code: `ShipsHandler` unused + wrong Bounds math (half-size, unordered corners) + LINQ alloc + missing-key throw; `IInput.OnErase` never raised; `QuequeStateMachine` unused | [ShipsHandler.cs:21-31](../Assets/Scripts/Gameplay/Control/ShipsHandler.cs:21), [MouseInput.cs:16](../Assets/Scripts/Gameplay/Input/MouseInput.cs:16) | Misleads readers; box-select "exists" but doesn't (maint) |
| W9 | **Low** | Magic numbers / leftovers: raycast distance `30`, `rawPos.z = 5` no-op, zoom-to-pan-speed formula divides by the zoom *range width* | [ObjectClicker.cs:25,29](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs:25), [CameraMover.cs:126](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs:126) | Tuning traps (maint) |
| W10 | **Low** | Silent early-returns: right-click with nothing selected, on a friendly, or with an enemy selected does nothing with no feedback path even planned | [ShipInput.cs:50](../Assets/Scripts/Gameplay/Input/ShipInput.cs:50), [ShipControlInterpreter.cs:30](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs:30) | Feel; harder to debug "my click did nothing" (feel) |

God-class check: none of the input classes is a god-class today — the problem is the opposite
(the system is skeletal). The duplication smell is W6; the hardcoded-keycode smell does not apply
(no keyboard input exists); the order-of-execution smell is W2.

---

## 5. Genre-standard gap analysis

Reference conventions: StarCraft II, AoE IV, Company of Heroes, They Are Billions, WC3.
Verdict codes: **(a)** deliberate design choice, **(b)** unimplemented feature, **(c)** defect,
**(?)** cannot tell from the code.

| Convention | Implemented? | Notes / deviation | Verdict | Priority |
|---|---|---|---|---|
| Box (drag) selection | ❌ | Dead scaffolding only: `ShipsHandler.GetChosenShips`, uncalled, not in scene, math wrong (§2.5). Click actions fire on press, so drag detection needs action changes too | (b), scaffolding itself (c) | **High** |
| Single click select | ✅ | One unit only; enemy units selectable **by design** (StarCraft-style enemy inspection — stat display itself not built yet) | (a), stat UI (b) | — |
| Double-click select-all-of-type on screen | ❌ | No double-tap interaction anywhere | (b) | Medium |
| Shift append/remove from selection | ❌ | No keyboard bindings at all | (b) | **High** |
| Ctrl+click select all of type | ❌ | — | (b) | Low |
| Control groups 0–9 (assign/recall/append, double-tap center) | ❌ | — | (b) | **High** |
| Tab sub-group cycling | ❌ | No sub-group concept | (b) | Low |
| Attack-move | ❌ | No order types beyond implicit move/attack | (b) | **High** |
| Hold position | ❌ | — | (b) | Medium |
| Patrol | ❌ | — | (b) | Low |
| Stop | ❌ | Cannot cancel a move; cannot stop firing by hand (only by ordering another attack). Owner decision: **Stop halts movement only**; **Hold Fire is a separate button** (two independent orders, unlike StarCraft's combined Stop) | (b), scope decided (a) | Medium |
| Shift order queuing + queue visualization | ❌ | No command objects to queue (W4) | (b) | Medium |
| Rally points | ❌ | No production buildings exist — nothing to rally from | (a) for now | n/a |
| Idle-worker hotkey | ❌ | No economy/workers in the prototype | (a) for now | n/a |
| Minimap click-move + minimap orders | ❌ | No minimap exists | (b) | Medium |
| Camera bookmarks (Ctrl+F1…) | ❌ | — | (b) | Low |
| Right-click context command | ⚠️ partial | Enemy → attack, ground → move. No friendly interactions (follow/repair), no feedback marker, silent no-ops (W10) | (b) | **High** |
| Full rebinding UI | ❌ | Bindings are data-driven (good base); no runtime rebinding, no conflict checks, no layouts | (b) | Low (prototype) |

Overall: the deviations are overwhelmingly **(b) unimplemented** — consistent with an early
prototype — not defects. The two genuine defects found (`ShipsHandler` math, `MouseInput`
disposal) are in dead or unexercised code paths.

---

## 6. Refactoring proposals

Ranked. None of these were started. Where options exist they are presented side by side —
the choice is the owner's.

### Rank: do first

---

#### P1 — Multi-unit selection service

**Problem it solves:** W1, W5, W8 — the single biggest gap between this prototype and an RTS.

**Proposed architecture:** a plain-C# `SelectionService` registered in
`ProviderBuilder.RegisterServices()`, owning `List<IControllable>` (the existing
`ISelectable`/`IControllable` interfaces survive unchanged). `ShipInput` shrinks to translating
clicks into `SelectionService` calls. Box select = corner capture on press/release + a screen-space
rect test over the player's ships (world→`Camera.WorldToScreenPoint`, no physics), which needs a
registry of living player ships — either resurrect `ShipsHandler` (fixing its math, wiring it into
the scene, feeding it from `ShipSpawner`/death events) or fold the registry into
`SelectionService` and delete `ShipsHandler`.

```
click/drag ──► ShipInput ──► SelectionService ──► ISelectable.Select/DeSelect
                                  │  owns List<IControllable>, filters IsDead,
                                  │  subscribes each selected ship's OnDeath → auto-remove
                                  └──► exposed to command issuing (P4) and UI
```

Side-filtering policy (per the owner's decision that enemy selection is an inspect-enemy
feature, StarCraft-style): single click may select **any** ship including enemies; **box select
and commands apply to player-side ships only**; a mixed click-result resolves to player ships
first. That is exactly StarCraft's rule set and it keeps the inspect feature intact.

- **Files:** new `SelectionService.cs`; modified `ShipInput.cs`, `ProviderBuilder.cs`,
  `GameInitiliazer.cs`; `ShipsHandler.cs` either rewritten or deleted; a drag-rectangle UI
  visual (new uGUI element — manual scene step). The `.inputactions` asset needs press/release
  data for the left button (asset edit = human editor step; the current `Press` interaction only
  reports press).
- **Migration:** incremental — step 1: service with single-select behind it (behavior identical);
  step 2: death filtering + the side policy above; step 3: box select + visual.
- **Risk / blast radius:** medium-low. `ShipInput` is the only consumer of selection today.
  Scene wiring + asset edit are human steps.
- **Effort:** M.
- **Trade-offs:** the screen-rect approach selects by ship *center* (a ship half inside the box
  at screen edge may be missed) — the standard alternative (world-space frustum/OverlapBox) costs
  physics queries and needs the colliders on a selectable layer. Both are viable; pick one.

---

#### P2 — On-demand click raycasting (retire the per-frame cache)

**Problem it solves:** W2, part of W6, W9.

**Proposed architecture:** replace `ObjectClicker`'s cached `Update()` with a
`CursorRaycaster` (service or kept as the existing singleton — options below) that raycasts *at
the moment of the query*: `TryGetTarget(out GameObject)` / `TryGetGroundPoint(out Vector3)`.
Callers: `ShipInput` (and later P1/P3). Raycast distance becomes a serialized field or derives
from camera far distance instead of the literal `30`; the `rawPos.z = 5` leftover goes away.

```
ShipInput.TargetClick ──► CursorRaycaster.TryGetTarget(...)   (raycast happens NOW,
                                                               at this frame's mouse pos)
```

- **Options:**
  - **(2a)** New `ICursorRaycaster` service in `ProviderBuilder`, injected into `ShipInput` —
    consistent with the DI style, breaks the `ObjectClicker.Instance` singleton dependency,
    testable seam. Slightly more wiring.
  - **(2b)** Keep `ObjectClicker` (name, singleton, scene object) and just convert methods to
    raycast on demand — smallest diff, keeps the singleton smell.
- **Files:** `ObjectClicker.cs` (rewritten or replaced), `ShipInput.cs`; (2a) also
  `ProviderBuilder.cs`, `GameInitiliazer.cs`.
- **Migration:** single small step; behavior change is *positive* (click uses current-frame
  mouse position; execution-order dependency disappears).
- **Risk:** low — two call sites. If any future feature needs per-frame hover (cursor highlight),
  add an explicit cached hover query then, deliberately.
- **Effort:** S.
- **Trade-offs:** none significant. This is the highest value-per-risk item in the list.

---

### Rank: do later

---

#### P3 — Input context layer (modes + UI guard)

**Problem it solves:** W3.

**Proposed architecture:** an `InputContext` concept owned by a small `InputModeController`:
contexts like `Default`, `TargetingAbility`, `BuildingPlacement`, `UIModal`, `Paused`. Two
implementation options, side by side:

- **(3a) Action-map-per-context.** Each context = an action map in `PlayerInputMaps.inputactions`;
  entering a context enables its map, disables others (`asset.FindActionMap(...).Enable()`).
  The Input System does the routing; `MouseInput` exposes per-context events.
  *Pro:* rebinding/conflict UI later comes almost free; no `if (mode)` scatter.
  *Con:* every new context = asset edit (human step) + generated-file churn.
- **(3b) Context stack in code.** A plain stack (`Push(IInputContext)`/`Pop()`), where the top
  context interprets the same five raw events. The existing unused `QuequeStateMachine` is *not*
  a fit (it is a sequential queue, not a stack) — a purpose-built 40-line stack is cleaner.
  *Pro:* contexts are pure C#, testable, no asset churn. *Con:* routing logic is hand-rolled.

Either way, the UI guard is one rule at the top: pointer over interactive UI
(`EventSystem.current.IsPointerOverGameObject()` or `InputSystemUIInputModule` raycast check) →
world clicks suppressed.

- **Files:** new `InputModeController.cs` (+ context classes); modified `MouseInput.cs`,
  `ShipInput.cs`, `GameInitiliazer.cs`; (3a) also the `.inputactions` asset (human).
- **Migration:** incremental — introduce the `Default` context wrapping current behavior first,
  then add contexts as features (abilities, build mode, pause) actually appear.
- **Risk:** low-medium; it is additive until a second context exists.
- **Effort:** M.
- **Trade-offs:** speculative until there is a second real context. Recommended trigger: build it
  together with the first feature that needs it (first UI panel or ability targeting), not before.

---

#### P4 — Command objects for orders

**Problem it solves:** W4, W10; prerequisite for Shift-queue, order feedback, and P6.

**Proposed architecture:** reify orders: `MoveOrder(Vector3)`, `AttackOrder(ITargetable)`,
`StopOrder`, later `AttackMoveOrder`, … `ShipInput` *creates* orders; a per-ship `OrderQueue`
(on `ShipControlInterpreter` or a sibling component) *executes* them, owning order-interaction
policy in exactly one place, and raising events the UI/audio can consume for acknowledgment
feedback. The order-interaction policy is already decided by the owner: **movement and weapons
are fully independent channels** — a move order does not stop the guns (fire-at-will), a `Stop`
order halts movement only, and stopping fire is its own separate order (`HoldFireOrder`). This
matches how `ShipMovementController` and `ShipWeaponsController` are already split, so the order
set maps 1:1 onto the two channels: `MoveOrder`/`StopOrder` → movement,
`AttackOrder`/`HoldFireOrder` → weapons.

```
ShipInput ──► new MoveOrder(pos) ──► foreach selected: ship.OrderQueue.Set(order)
                                        │ (Shift: Enqueue instead of Set — later)
                                        └─► order.Execute(shipEntity) → MoveTo/Shoot/…
```

- **Files:** new `Orders/` classes + `OrderQueue`; modified `ShipControlInterpreter.cs`,
  `ShipInput.cs`. `IControllable` either gains `Issue(IOrder)` or is replaced by it.
- **Migration:** incremental — start with orders as a pass-through (same behavior), add queuing
  later.
- **Risk:** medium — touches the ship prefab's control path; blast radius is every `IControllable`.
- **Effort:** M (pass-through) to L (queuing + visualization).
- **Trade-offs:** indirection cost with no player-visible win until queuing/feedback ship.
  Sequencing note: P4 only pays off after P1 (multi-select) exists; doing it first would be
  architecture for one ship.

---

#### P5 — Keyboard bindings + input hygiene

**Problem it solves:** the Shift/Ctrl modifier column of §5, camera WASD, and W7/W8 cleanups.

**Proposed architecture:** no new structure — additions to the existing one. New actions in the
asset (`CameraPan` WASD/arrows composite, `Stop`, modifier keys), surfaced through `IInput`;
`CameraMover.MoveCenter` consumes pan input from `IInput` instead of polling `Mouse.current`
(closing W6 for the camera). Hygiene: call `_input.Dispose()` at teardown (e.g.
`OnDestroy` on `ProviderBuilder`, which created it), fix the Disable/Dispose order, delete or
implement `OnErase`, unsubscribe in consumers.

- **Files:** `MouseInput.cs`, `CameraMover.cs`, `ProviderBuilder.cs`; `.inputactions` asset
  (human step regenerates `PlayerInputMaps.cs`).
- **Migration:** each item independent, all small.
- **Risk:** low. **Effort:** S per item.
- **Trade-offs:** none; mostly debt payment. Rebinding UI itself stays out of scope until the
  prototype stabilizes — the data-driven asset keeps that door open.

---

### Rank: only if we go multiplayer / replays

---

#### P6 — Deterministic command pipeline

**Problem it solves:** §2.9 determinism — replays, networking, lockstep.

**Proposed architecture:** extends P4: every order becomes a serializable record
`(tick, shipId, orderType, payload)` flowing through a single `CommandIngress` service; the
simulation consumes commands only from that queue (local input and remote/replayed input become
indistinguishable). Requires stable entity IDs and a fixed-tick simulation step.

- **Files:** builds on P4's `Orders/`; new `CommandIngress`, ID assignment in `ShipSpawner`.
- **Migration:** only sane after P4; the ingress point is incremental on top of it.
- **Risk / honesty note:** the input layer is the *easy* half. The current simulation
  (`NavMeshAgent` pathing, frame-rate-dependent `Update` movement, float physics) is **not
  deterministic** and would need its own overhaul for lockstep. For replays-as-approximation or
  server-authoritative networking the pipeline alone is useful; for lockstep it is necessary but
  far from sufficient.
- **Effort:** L (pipeline) + XL (deterministic sim, out of input scope).
- **Trade-offs:** heavy machinery with zero single-player payoff beyond replays. Build only when
  multiplayer/replays are actually on the roadmap.

---

### Suggested sequence

```
P2 (S) ──► P1 (M) ──► P5 (S) ──► P4 (M) ──► P3 (M) ──► P6 (L, conditional)
   click fix    multi-select   keys+hygiene   orders      contexts     determinism
```

P3 can float earlier if UI/abilities arrive sooner; P2 and P5's hygiene items are safe any time.

---

## 7. Open questions

### Resolved by the owner (2026-08-09)

- **Enemy-ship selection is a feature** — StarCraft-style enemy inspection (select to check
  stats). P1 must keep enemies single-selectable; the missing piece is the stat display
  (`ShipUiStats` stub), not the selection.
- **A move order must not stop the guns** — fire-at-will is the intended behavior. P4's order
  layer treats movement and weapon state as independent channels.
- **Stop halts movement only; Hold Fire is a separate button.** Two independent orders mapping
  onto the two channels (`MoveOrder`/`StopOrder` vs. `AttackOrder`/`HoldFireOrder`) — unlike
  StarCraft's combined Stop.

### Still open

1. **Ship death end-state:** nothing despawns/disables dead ships (`ShipTeamObserver` and the
   death path are stubs). Selection auto-removal design in P1 depends on whether dead ships will
   be destroyed, pooled, or become wrecks.
2. **Is the raycast range 30 deliberate** (max command distance) or an arbitrary literal? It
   currently exceeds max zoom (20) so it never binds; P2 wants a single authoritative value.
3. **Right-click on a friendly ship** — reserved for future follow/assist/repair, or permanently
   nothing? Affects the smart-command resolution table in P4.
4. **Is a pause/game-speed feature planned?** Nothing in the codebase pauses; if yes, it becomes
   the first real consumer of P3's context layer (and edge-scroll/zoom must decide whether they
   run while paused).
5. **Edge-scroll behavior in windowed mode** (cursor leaving the window, alt-tab) — current code
   has no focus handling; acceptable for the prototype, or should P5 add a focus gate?
6. **`OnErase` intent** — the never-raised `IInput.OnErase` event suggests a planned
   erase/cancel input (Escape?). Implement in P5 or delete?
7. **Zoom-to-pan-speed formula** (`Clamp01(_currentZoom / (_maxZoomDistance - _minZoomDistance))`
   at [CameraMover.cs:126](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs:126)) — dividing
   by the range *width* looks accidental (with scene values: zoom 1–20 → factor 0.05–1.0). Tuned
   and liked, or a bug that tuning compensated for?
