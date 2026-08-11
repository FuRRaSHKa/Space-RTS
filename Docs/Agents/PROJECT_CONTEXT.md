# Space-RTS — project context for agents

Project-specific facts that fill in the `[project]` placeholders of [UNITY_RULES.md](UNITY_RULES.md).
**If this file and the code disagree, trust the code and update this file.**

---

## 1. Engine and build

| | |
|---|---|
| Unity | **2022.3.62f2** (`ProjectSettings/ProjectVersion.txt`) |
| Render pipeline | **URP 14.0.12** (`Assets/Art/Pipeline/`) |
| C# language version | **9.0** (Unity 2022 LTS) — no `record`, no file-scoped namespaces, no `required` |
| Assembly layout | **No asmdefs** — everything compiles into `Assembly-CSharp` |
| Input | **New Input System 1.14.0** — `Assets/Data/Input/PlayerInputMaps.inputactions` (+ generated `PlayerInputMaps.cs`) |
| UI | uGUI + TextMeshPro 3.0.7 |
| Target platform | Desktop/standalone prototype. Target **60 FPS**. |
| Obfuscation | None |
| Custom compile defines | None |
| Async model | **Two tools, one boundary** (see §4): object-lifetime behaviour → coroutines via `RoutineManager`; operations (saves, loading, UI sequences) → **UniTask 2.5.11** (`com.cysharp.unitask`). No raw `Task` in gameplay code. |
| Tween library | None (no DOTween) |
| Localization | None |
| Save system | **None** — nothing is persisted between sessions. If you add one, add a section here first. |
| Tests | None. Verification is manual, in the editor. |

**Machine-generated, do not hand-edit:** `Assets/Data/Input/PlayerInputMaps.cs`
(regenerated from the `.inputactions` asset — change the asset, not the file).

**Third-party / read-only:** `Assets/SpaceSkies Free/**`.

---

## 2. Scenes

| Scene | Role |
|---|---|
| `Assets/Scenes/SampleScene/SampleScene.unity` | **The** gameplay scene — everything is tested here |
| `Assets/SpaceSkies Free/Demo/DemoScene.unity` | Third-party asset demo, ignore |

---

## 3. Namespaces — never infer them from the folder path

Root namespace is `HalloGames.*` and the mapping to folders **drifts**. Examples:

| Folder | Namespace |
|---|---|
| `Assets/Scripts/SO/Ships/` | `HalloGames.SpaceRTS.Data.Ships` |
| `Assets/Scripts/SO/ENUMS/` | `HalloGames.SpaceRTS.Data.Enums` |
| `Assets/Scripts/Gameplay/Weapon/Projectile/` | `HalloGames.SpaceRTS.Gameplay.Projectile` |
| `Assets/Scripts/Gameplay/Weapon/` (factory) | `HalloGames.SpaceRTS.Management.Factories` |
| `Assets/Scripts/Architecture/Singletons/` | `HalloGames.Architecture.Singletones` |
| `Assets/Scripts/Architecture/Pools/` | `HalloGames.Architecture.PoolSystem` |

**Rule: open a neighbouring file in the same folder and copy its `namespace` line.**

`HalloGames.Architecture.*` is the reusable, game-agnostic layer (services, pools, routines,
singletons, state machine). `HalloGames.SpaceRTS.*` is game code. Do not make `Architecture`
depend on `SpaceRTS`.

---

## 4. Core systems — use these, don't build parallel ones

### Dependency injection — `ServiceProvider`

- Contract: `Assets/Scripts/Architecture/ServiceProvider.cs`
  (`IServiceProvider.AddService<T>() / GetService<T>()`; every service implements the marker
  interface `IService`).
- **All bindings live in one place:** `Assets/Scripts/Management/ProviderBuilder.cs` →
  `RegisterServices()`. A new injectable must be registered there **before** anything resolves it.
- Binding is keyed by the **compile-time generic `T`**. `AddService<IShipsFactory>(_shipSpawner)`
  and `AddService(_shipSpawner)` register **different** keys — bind and resolve with the exact
  same type.
- `GetService<T>()` throws `KeyNotFoundException` if the service is missing — resolve once during
  initialization, never lazily deep inside gameplay code.

### Initialization order — the boot chain

```
ProviderBuilder.Awake()            // scene component, the composition root
  ├─ new ServiceProvider()
  ├─ _shipSpawner.InitProvider(...)
  ├─ RegisterServices()            // ← register new services HERE
  └─ _gameInitiliazer.Initialize(provider)
        ├─ InitInput()             // ShipInput, CameraMover
        ├─ InitializeShips()        // new ShipsManager(...)
        └─ InitGameController()
```

Anything that needs a service gets it **pushed in** through this chain (an `Initialize`/`Init`
method), it does not pull it from a static. Prefer extending the chain over adding a singleton.

### Per-entity initialization — `AbstractInitilizer<TData>`

`Assets/Scripts/Architecture/AbstractInitilizer.cs`. Prefabs (ships, weapons) are composed of
components implementing `IInitializable<TData>` / `IDeInitializable`; the initializer on the root
(`ShipInitilizer`, `WeaponInitilizer`) broadcasts `Init(data)` to children found in `Awake`.
**A new prefab component that needs data implements `IInitializable<T>` — it does not go hunting
for its owner with `GetComponentInParent`.**

### Frame ticks — `CentralTicker`

`Assets/Scripts/Architecture/Frames/`. One `CentralTicker` component in the scene owns the frame
loop; game code implements a tick interface instead of writing its own `Update()`.

Three independent channels, each with its **own method name** so one class can sit on two of them:

| Interface | Method | Driven by | `deltaTime` you get |
|---|---|---|---|
| `IUpdatable` | `UpdateTick(float)` | `Update` | `Time.deltaTime` — varies per frame |
| `ILogicTickable` | `Tick(float)` | fixed accumulator, `_ticksPerSecond` (30) | **constant** `1/30` |
| `IFixedUpdatable` | `FixedUpdateTick(float)` | `FixedUpdate` | `Time.fixedDeltaTime` |

Pick the channel by what the code does: **anything rendered** (rotation, lerp, camera, VFX,
projectile movement) goes on `IUpdatable`, because the logic channel runs at 30 Hz and the
stepping is visible. **Decisions** (reload timers, AI) go on `ILogicTickable` — they then become
frame-rate independent, at the price of quantising to the 33 ms step. `IFixedUpdatable` has no
consumers yet; it exists for physics.

Never read `Time.deltaTime` inside a tick — use the parameter. On the logic channel they are
different numbers, and `AccumulateLogic` deliberately drops the accumulator when a frame overruns
`_maxTicksPerFrame` (spiral-of-death guard), so `Time.deltaTime` there is simply wrong.

**Access is static, through `TickManager`** — the ticker is *not* a service and is **not** in
`ProviderBuilder`. `CentralTicker` is a `MonoSingleton` that owns the three dispatchers;
`TickManager` is a plain static facade over `CentralTicker.Instance`, exactly the way
`RoutineManager` fronts `Routine`. This is deliberate: the ticker is engine-level infrastructure
like `RoutineManager` and `PoolManager`, not game logic, so §"Singletons" allows it.

```csharp
private void OnEnable()  => TickManager.RegisterUpdate(this);
private void OnDisable() => TickManager.UnregisterUpdate(this);
```

Six methods, one pair per channel: `RegisterUpdate`/`RegisterLogic`/`RegisterFixed` and their
`Unregister…` twins. Names are explicit rather than overloaded on purpose — a class implementing
two tick interfaces would make `Register(this)` ambiguous.

**Register / unregister rule — the honest `OnEnable`/`OnDisable` pair.** Because access is static,
the dependency exists from the first frame and there is nothing to inject. That means a component
disabled with `SetActive(false)` correctly stops ticking and resumes when re-enabled — the same
semantics as a plain `Update()`. `CentralTicker` carries `[DefaultExecutionOrder(-1000)]` so its
`Instance` is set before anything registers.

`Register…` asserts that an instance exists — a scene without a `CentralTicker` is a broken scene
and should fail loudly. `Unregister…` is a silent no-op when the instance is already gone, because
teardown order is not guaranteed and must not throw.

`TickDispatcher.Run` also drops entries whose `UnityEngine.Object` has been destroyed, so a missed
`Unregister` degrades to a wasted list slot instead of a `MissingReferenceException`. That is a
safety net, not a licence to skip the pair.

**Migration status: done for gameplay.** Every live gameplay component is on the ticker — do not
add a new `Update()`, implement a tick interface and register in `OnEnable`. Three leftovers still
declare one and are **not** bugs to fix in passing:

- `CentralTicker` itself — it *is* the loop.
- `Utils/FPS.cs` — a standalone debug counter, referenced by nothing (see §7). Deliberately left
  on `Update()`: it has no injection point, so putting it on the ticker would mean it silently
  does nothing when someone drags it into a scene to measure something.
- the four empty `Gameplay/Ship/ShipStates/*` template stubs — someone else's WIP.

Coroutines are a separate clock and still read `Time.deltaTime` inside `RoutineExtension` — that
is by design, see below.

`WeaponController` is the one consumer of the logic channel: its reload timer and the
shoot/don't-shoot decision run at 30 Hz, which makes the rate of fire frame-rate independent and
quantises it to the 33 ms step. Its targeter sits on the frame channel, and that ordering is
correct — `CentralTicker.Update` runs `_updateTicker` (turret turns) **before** `AccumulateLogic`
(weapon decides), so the decision always sees this frame's angle.

Prefab components need **no wiring at all** — they call `TickManager` themselves in
`OnEnable`/`OnDisable`. Factories and initializers (`WeaponFactory`, `ShipInitilizer`) know
nothing about ticking and must not be given a dispatcher to hand out; a new tickable component on
any prefab works the moment it implements the interface and registers itself.

### Coroutines — `RoutineManager` / `Routine`

`Assets/Scripts/Architecture/CoroutineManagement/`. Fluent API, cancellable via `IStopable`:

```csharp
_stopable?.Stop();
_stopable = RoutineManager.CreateRoutine(this)
    .Wait(1f, base.DisableObject)
    .Start();
```

Use it instead of raw `StartCoroutine`. **Always keep the `IStopable` and stop the previous
routine before starting a new one** on the same object — pooled objects are reused and a
leftover routine will fire on a recycled instance.

`Routine` is the tool for **behaviour bound to a scene object's lifetime** — it stops
automatically with the object, which is exactly what pooled visuals need. For *operations*, see
UniTask below; the boundary between the two is
[UNITY_RULES.md §4.5](UNITY_RULES.md#45-async--two-tools-one-boundary).

### Async operations — UniTask

**UniTask 2.5.11** (`com.cysharp.unitask`, UPM git dependency pinned to the release tag in
`Packages/manifest.json`) is the tool for **operations**: save/load I/O, asset and scene loading
(incl. Addressables when they arrive), UI sequences — anything that returns a result, composes
(`WhenAll`, chains), or leaves the main thread. Raw `Task` in gameplay code is forbidden.

The full rule set — mandatory `CancellationToken`s, the per-spawn-cycle `CancellationTokenSource`
on pooled objects, `.Forget()`, await-once, thread-pool hygiene — lives in
[UNITY_RULES.md §4.5](UNITY_RULES.md#45-async--two-tools-one-boundary) and is not optional.

Boundary in one line: **dies with the object → `Routine`; returns a result / composes →
UniTask; runs every frame → `CentralTicker`.**

Existing `Routine` call sites (`RocketObject`, `PoolObject`) stay as they are — do not migrate
them for style. Debugging pending/leaked tasks: editor window **Window → UniTask Tracker**.

### Pooling — `PoolManager` / `ObjectPool` / `PoolObject`

`Assets/Scripts/Architecture/Pools/`. Projectiles, VFX and anything spawned per-shot come from a
pool (`PoolManager.Instance[prefab]`), keyed by the `PoolObject` prefab reference. Pools are
declared on the `PoolManager` component in the scene (`PoolPair[]`).
**Never `Instantiate`/`Destroy` per shot/impact.** A new pooled prefab is a manual editor step:
"add a `PoolPair` row to `PoolManager`".

Pooled objects implement `EnableObject()` / `DisableObject()` — reset **all** mutable state there
(trails, gfx, timers, target refs), because the instance comes back dirty.

### Singletons — existing only

`MonoSingleton<T>` (override `OverriddenAwake`, **not** `Awake`), `SOSingleton`,
`NoneLazySingletone` live in `Assets/Scripts/Architecture/Singletons/`.
They exist for engine-level infrastructure (`RoutineManager`, `PoolManager`).
**Do not add new singletons for game logic** — register a service in `ProviderBuilder` instead.

### Other

- `QuequeStateMachine` / `IState` — `Assets/Scripts/Architecture/StateMachine/`.
- `IFactory` — `Assets/Scripts/Architecture/Factories/`; concrete factories
  (`ShipSpawner`, `WeaponFactory`) are registered as services.

---

## 5. Data assets

ScriptableObject configs live under `Assets/Data/` and are created via
`[CreateAssetMenu(menuName = "Data/...")]`:

| Asset type | Script | Assets |
|---|---|---|
| `ShipData`, `ShipHullData`, `TeamData`, `WeaponData` | `Assets/Scripts/SO/Ships/` | `Assets/Data/Ships/` |
| `BulletData`, `RocketData` | `Assets/Scripts/SO/Weapon/` | `Assets/Data/Bullets/` |
| `SideData`, `ShipSize`, `StatData` | `Assets/Scripts/SO/ENUMS/` | `Assets/Data/…` |

**"Enums" here are ScriptableObjects, not C# enums** (`SideData`, `ShipSize`, `StatData`) —
identity is the asset GUID, so adding a new one is a manual "create the asset" step, and
*deleting* one breaks every reference. Real C# enums also exist (e.g. `WeaponType`) and are
**append-only** — see [UNITY_RULES.md §1.4](UNITY_RULES.md#rule-14--enums-serialize-as-ints-append-only).

Adding a `[SerializeField]` to a ScriptableObject means **every existing asset of that type**
needs the new value filled in by hand. List each asset in the summary.

---

## 6. House style, as actually written here

- `[SerializeField] private Type _camelCase;` — everywhere in new code.
- **Prefer `var` for local variables** — this is the owner's explicit preference and it wins over
  the surrounding code. Much of the existing codebase declares locals with explicit types
  (`GameObject chosenObject = …`, `Vector3 pos = …`); that is legacy, **not** a pattern to copy,
  and equally **not** something to rewrite in a drive-by edit. Write new locals with `var`, leave
  old ones alone until you are editing that line for another reason. Fields, parameters and
  return types are unaffected — `var` is not legal there.
- Public fields survive only in legacy serializable structs (`PoolPair.prefab`,
  `StatStruct.StatData`). **Do not "modernize" them** — the values live in prefabs and assets.
- **`protected` is properties and methods — never a field.** No exceptions: a base class exposes
  behaviour to its subclasses, not storage. A protected field is part of the contract a subclass
  sees, yet nothing stops that subclass from writing to it whenever it likes;
  `protected float DeltaTime { get; private set; }` states "the base writes, you read" *and* has
  the compiler enforce it. Applied in `ProjectileController` / `ProjectileWrapper`.
  **First ask whether the member needs to be `protected` at all** — in `ProjectileController`
  three of six were only ever touched by the base class and simply became `private`, which is what
  made a rule without exceptions possible. When state genuinely must reach a subclass, keep the
  field `private` and expose it:
  - **Serialized data** → `private` backing field + `protected` read-only property. Unity
    serializes fields and never properties, so the field itself has to stay a field:
    `[SerializeField] private LayerMask _layerMask;` + `protected int LayerMaskValue => _layerMask.value;`.
    Renaming that field on the way needs `[FormerlySerializedAs]`, kept forever.
  - **Value types holding internal state** (`NativeList<T>` and friends) → `private` field +
    `protected` **method** that performs the operation, e.g. `AddRaycastCommand(...)`. A property
    would hand back a *copy* of the struct: `_filtered.Capacity = n` through it fails to compile
    (CS1612), while `_results.ResizeUninitialized(n)` compiles fine but reallocates the copy and
    leaves the field pointing at freed memory. Never expose these through a property.
  `Architecture` (`AbstractInitilizer`, `QuequeStateMachine`) still uses `protected _camelCase`
  fields and was left alone deliberately — do not convert it in a drive-by edit.
- Interfaces are frequently declared in the same file as their main implementation
  (`IWeaponFactory` + `WeaponFactory`, `IService`/`IServiceProvider` + `ServiceProvider`).
  Follow the local pattern of the file you edit.
- Components acquire sibling components in `Awake` via `GetComponent<IInterface>()`
  (`ShipEntity`) — that is the established pattern for *composition inside one prefab*; it is
  **not** the way to acquire a service or a cross-prefab dependency.
- **Some misspellings are load-bearing.** Member-level spelling was normalized in 2026-08
  (`Initialize`, `IInitializable`, `OverriddenAwake`, `InstallTeams`, `ScheduleMoving`,
  `StartFollowing`, `ProjectileController`, …), but these **keep the old spelling** because the
  name is tied to a file name or a folder, and moving a file means moving its `.meta` — a human
  step in Unity, never an agent's:
  `AbstractInitilizer`, `ShipInitilizer`, `WeaponInitilizer`, `GameInitiliazer`,
  `NoneLazySingletone`, `QuequeStateMachine`, `AdvanceWeaponTargeter`, `IStopable`, and the
  `Assets/Scripts/Extentions/` folder.
  Two more are still misspelled but are **not** file-bound and could be renamed in C# alone —
  the `HalloGames.Architecture.Singletones` namespace (its folder is spelled `Singletons`) and
  the `shipInitilizer` / `_gameInitiliazer` identifiers that mirror their retained type names.
  They were left alone deliberately. Match the surrounding spelling; do not fix these in a
  drive-by edit.
- Comments are rare and English. Keep the existing ones.

---

## 7. Manual verification — where to look

There is no test suite and no debug console. To verify anything:

1. Open `Assets/Scenes/SampleScene/SampleScene.unity`, enter play mode.
2. Select ships with the mouse (`ObjectClicker` / `ShipInput`), right-click to move/attack.
   (`ShipsHandler` is dead code — referenced by nothing and absent from `SampleScene`.)
3. For frame cost, use the **Stats overlay or the Profiler** — `Assets/Scripts/Utils/FPS.cs`
   exists but is referenced by **nothing**: zero hits in `SampleScene` and in every prefab. To use
   it you have to drag it onto an object and wire its `_text` field yourself. Whether it was
   dropped from the scene on purpose is still an open question for the owner; until that is
   answered, do not cite an in-scene FPS readout in a test plan.

State this explicitly in every summary: which scene, which action, what to expect.
