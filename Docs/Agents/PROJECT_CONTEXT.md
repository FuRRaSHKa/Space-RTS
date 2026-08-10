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
| Async model | **Coroutines only** — via `RoutineManager` (see §4). No UniTask, no `Task`-based gameplay code. |
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
- Public fields survive only in legacy serializable structs (`PoolPair.prefab`,
  `StatStruct.StatData`). **Do not "modernize" them** — the values live in prefabs and assets.
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
2. Select ships with the mouse (`ObjectClicker` / `ShipsHandler`), right-click to move/attack.
3. `Assets/Scripts/Utils/FPS.cs` shows the frame counter — use it when the change touches
   `Update`, pooling, or spawn counts.

State this explicitly in every summary: which scene, which action, what to expect.
