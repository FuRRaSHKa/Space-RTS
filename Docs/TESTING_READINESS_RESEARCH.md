# Automated testing readiness — research and action plan

Research-only audit of how ready Space-RTS is for automated tests. No code was changed, no
packages added, no `.asmdef` created. All statements were verified against the code as of commit
`9d6ddeb`; anything unverified is in [§9 Open questions](#9-open-questions).

> **Working-tree caveat:** the repository currently carries an *uncommitted* Unity upgrade
> (`ProjectSettings/ProjectVersion.txt` 2021.3.2f1 → 2022.3.62f2, `Packages/manifest.json`
> test-framework 1.1.31 → 1.1.33, Input System 1.3.0 → 1.14.0, code-coverage 1.2.6 added).
> Statements below name both states where they differ. Test infrastructure should target
> whichever state gets committed — see [§9 Q1](#9-open-questions).

---

## 1. Verdict

**Partially ready.** The architecture is unusually test-friendly for a Unity prototype — manual
constructor injection (`ShipsManager`, `WeaponFactory`, `BulletSpawner`), interfaces at nearly
every seam (`IInput`, `ITargetable`, `IStatsController`, `IShipsFactory`, …), and a handful of
plain-C# logic classes. But **not a single test can compile today**, because all 70 scripts live
in the predefined `Assembly-CSharp` assembly, which a test assembly definition cannot reference —
Unity only lets `.asmdef` assemblies reference other `.asmdef` assemblies. One contained
infrastructure decision (how tests reach `Assembly-CSharp`, [§6 B1](#6-blockers)) unlocks a
meaningful EditMode unit suite with **zero production-code changes**; the deeper refactors
(time injection, singleton seams) are optional value-adds, not prerequisites.

Scale: Ready ▢ · **Partially ready ▣** · Needs preparatory refactoring ▢ · Blocked ▢

---

## 2. Summary

- **Unity Test Framework is installed** (1.1.31 committed / 1.1.33 in the uncommitted upgrade)
  as a direct dependency in [`Packages/manifest.json`](../Packages/manifest.json); the Code
  Coverage package 1.2.6 rides along in the uncommitted state. **Zero tests exist** — no test
  scripts, no test asmdefs, no fakes, no mocking library, nothing under `Assets/` matching
  `*test*`.
- **There is no CI of any kind** — no `.github/`, no pipeline files. Tests today would run only
  locally (Test Runner window or `Unity.exe -batchmode -runTests`).
- **The single biggest blocker is structural, not architectural**: everything compiles into
  `Assembly-CSharp`, and a test `.asmdef` cannot reference a predefined assembly. Either
  production code gets at least one `.asmdef`, or the legacy "Enable playmode tests for all
  assemblies" toggle is used (`playModeTestRunnerEnabled: 0` today in
  [`ProjectSettings/ProjectSettings.asset:585`](../ProjectSettings/ProjectSettings.asset)).
  Options compared in [§8 Phase 1](#phase-1--minimal-infrastructure-unlock-compilation).
- **What already helps**: push-based DI through an explicit boot chain
  ([`ProviderBuilder`](../Assets/Scripts/Management/ProviderBuilder.cs) →
  [`GameInitiliazer`](../Assets/Scripts/Management/GameInitiliazer.cs)), interfaces on every
  cross-object dependency, and pure-logic classes (`Stat`, `ServiceProvider`, `StateMachine`,
  `ShipsManager`) that need no scene at all.
- **Singleton/static access is rare and localized** — exactly three offenders:
  `PoolManager.Instance` ([`BulletSpawner.cs:31,57`](../Assets/Scripts/Management/BulletSpawner.cs)),
  `RoutineManager.Instance` (via [`PoolObject.cs:21`](../Assets/Scripts/Architecture/Pools/PoolObject.cs)),
  `ObjectClicker.Instance` ([`ShipInput.cs:25,52,56`](../Assets/Scripts/Gameplay/Input/ShipInput.cs)).
- **Hidden global state**: `MonoSingleton<T>.Instance` is never cleared (no `OnDestroy` in
  [`MonoSingleton.cs`](../Assets/Scripts/Architecture/Singletons/MonoSingleton.cs)) — it would
  leak a destroyed-object reference between consecutive PlayMode tests. No `PlayerPrefs`, no
  file I/O, no network, no analytics calls anywhere in game code.
- **Time is not injectable**: `Time.deltaTime` is read directly in 12 files, including inside
  the plain (non-MonoBehaviour) class
  [`ProjectileWrapper.UpdateLifeTime()`](../Assets/Scripts/Gameplay/Weapon/ProjectileWrapper.cs:49).
  Randomness is one call, `Random.insideUnitCircle` in
  [`MathExt.cs:23`](../Assets/Scripts/Extentions/MathExt.cs) — globally seedable, low impact.
- **ScriptableObject configs are constructible in memory** (`ScriptableObject.CreateInstance`)
  but **not populatable**: every field is `private [SerializeField]` with getter-only properties
  ([`ShipData`](../Assets/Scripts/SO/Ships/ShipData.cs),
  [`WeaponData`](../Assets/Scripts/SO/Ships/WeaponData.cs), …). Test fixtures need a
  reflection-based builder (test-side only, zero production change) or production-side setters.
- **Highest-value first targets**: the damage pipeline
  ([`ShipStatsController.DealDamage`](../Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs:75)
  — multi-stat overflow logic with suspicious edge cases), the pool lifecycle
  ([`ObjectPool`](../Assets/Scripts/Architecture/Pools/ObjectPool.cs)/[`PoolObject`](../Assets/Scripts/Architecture/Pools/PoolObject.cs)
  — the recurring bug source per git history: "Fix projectile disabling"), and the lead-prediction
  math ([`AdvanceWeaponTargeter.CalculatePos`](../Assets/Scripts/Gameplay/Weapon/Projectile/AdvanceWeaponTargeter.cs:64)).
- **Not worth testing**: visuals, camera feel, thin input adapters, Burst/job internals, and the
  dead code (`QuequeStateMachine`, `ShipTeamObserver`, `ShipsHandler`) — details in
  [§8 "Not recommended"](#what-i-would-not-recommend-testing).

---

## 3. Current state of testing infrastructure

| Question | Answer |
|---|---|
| Unity Test Framework installed? | **Yes** — direct dependency. Committed: `com.unity.test-framework 1.1.31`; uncommitted upgrade: `1.1.33` (NUnit 3.5 either way). |
| Any tests today? | **No.** No EditMode or PlayMode tests anywhere. Glob `Assets/**/*[Tt]est*` matches nothing. |
| Test assembly definitions? | **None.** There are *no* `.asmdef` files under `Assets/` at all — the 141 asmdefs found in the project are all in `Library/PackageCache/`. |
| Test tooling (mocks, fixtures, fakes, test scenes)? | **None.** No NSubstitute/Moq/FluentAssertions, no NuGet infrastructure, no fake implementations, no test scenes. The only interface fakes possible today would be hand-rolled. |
| Code coverage tooling? | `com.unity.testtools.codecoverage 1.2.6` is present in the **uncommitted** package state (pulled in via `com.unity.feature.development`); its settings file [`ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json`](../ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json) is empty/default. Not present in the committed state. |
| CI? | **None.** No `.github/`, no GitLab/Azure/Jenkins files anywhere in the repo root. Remote is GitHub (`github.com/FuRRaSHKa/Space-RTS`), so GitHub Actions is the natural target. |
| Legacy "tests in predefined assemblies" toggle? | Off — `playModeTestRunnerEnabled: 0` in `ProjectSettings/ProjectSettings.asset:585`. |

---

## 4. Assembly structure map

The map is short because there is almost nothing to map:

```
Assembly-CSharp  (predefined — the only game assembly)
 ├─ Assets/Scripts/**            70 files, all HalloGames.* code
 │   ├─ Architecture layer       HalloGames.Architecture.*   (14 files — no dependency on game code; verified)
 │   ├─ Game code                HalloGames.SpaceRTS.*       (54 files)
 │   └─ Extensions               HalloGames.Extensions.Math  (MathExt.cs)
 └─ Assets/SpaceSkies Free/Demo/Scripts/   2 third-party demo files (LookCamera, SkyboxChanger)

Assembly-CSharp-Editor           (predefined — empty; no Editor/ folders exist in Assets/)
+ ~60 package assemblies         (URP, Input System, Burst, Collections, TextMeshPro, …)
```

- **Production code is not split into assemblies at all.** This is by explicit project
  convention ([AGENTS.md](../AGENTS.md) — "no asmdefs") — but it is the single biggest
  determinant of testability, because **a test `.asmdef` cannot list `Assembly-CSharp` as a
  reference** (Unity restriction: asmdef assemblies may only reference other asmdef assemblies
  and precompiled DLLs).
- **No circular-dependency problem exists in the code itself.** The layering rule
  "`Architecture` must not depend on `SpaceRTS`" holds: the 14 files under
  [`Assets/Scripts/Architecture/`](../Assets/Scripts/Architecture) import only `UnityEngine`,
  `System.*` and each other. The game layer depends on Architecture, never the reverse. If
  asmdefs were ever introduced, the split lines already exist.
- **Which assemblies could be tested in isolation TODAY without changes: none.** Not because
  the code is untestable, but because no test assembly can currently *reach* it (see
  [§6 B1](#6-blockers)). This is the gate everything else waits behind.
- Interleaving note: `Assembly-CSharp` code uses package types that live in asmdef assemblies
  (`Unity.Collections`, `Unity.Burst`, `Unity.Jobs` in the projectile controllers;
  `UnityEngine.AI` in `ShipMovementController`; generated Input System wrapper in `MouseInput`).
  Any future production asmdef must declare those references.

---

## 5. Testability analysis by system

Legend for "test type reachable": **U** = plain unit test (no scene, no GameObject),
**U+GO** = EditMode unit test using `new GameObject()`/`AddComponent`, **PM** = PlayMode test
required, **INT** = only meaningful as scene-level integration.

### 5.1 Service layer / boot chain — good

- [`ServiceProvider`](../Assets/Scripts/Architecture/ServiceProvider.cs) is a plain class behind
  `IServiceProvider` with dictionary semantics worth pinning (exact-generic-type keying —
  `AddService<IShipsFactory>(x)` and `AddService(x)` are different keys; `GetService` throws
  `KeyNotFoundException`; duplicate `AddService` throws `ArgumentException`). **U.**
- The boot chain is push-based:
  [`ProviderBuilder.Awake()`](../Assets/Scripts/Management/ProviderBuilder.cs:18) builds the
  provider and hands it down through `Initialize(...)` methods. Dependencies are *pushed in*, not
  pulled from statics — exactly the property tests want. The chain itself, however, is
  scene-wired via four `[SerializeField]`s, so testing *the chain* (not the parts) is **PM**
  against `SampleScene`.
- Caveat: [`ProviderBuilder.RegisterServices()`](../Assets/Scripts/Management/ProviderBuilder.cs:43)
  constructs `new MouseInput()` inline, which instantiates the generated `PlayerInputMaps` and
  subscribes to devices. A PlayMode boot test therefore touches the Input System; in `-batchmode
  -nographics` this is expected to work (Input System supports headless) but is unverified here
  ([§9 Q4](#9-open-questions)).

### 5.2 Stats / damage — best unit target in the project

- [`Stat`](../Assets/Scripts/Gameplay/Ship/ShipStats/Stat.cs) is a pure class: clamping,
  `OnStatChange` event. **U**, zero prerequisites.
- [`ShipStatsController`](../Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs) is a
  MonoBehaviour, but a *humble* one: no `Update`, no scene dependency, no singleton access.
  `Init` + `ChangeStat` + `DealDamage` + death event are all callable on an
  `AddComponent<ShipStatsController>()` instance in EditMode. **U+GO.**
- `DealDamage` ([`ShipStatsController.cs:75-95`](../Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs))
  contains real, bug-prone logic — damage overflow across stats ordered by `DamageOrder`, an
  early-`break` when `statValue > tempDamage`, a second `break` on `tempDamage < 0`, and a death
  check summing remaining damagable stats. Boundary cases (damage exactly equal to a stat,
  overkill spilling across three stats, zero-damage calls, `Init` never called) are precisely
  what unit tests are for.
- Prerequisite: `Init` consumes `ShipInitializationData` → `ShipData` (a ScriptableObject with
  no public setters). Needs the SO fixture builder ([§6 B2](#6-blockers)).

### 5.3 Projectiles — mixed

- [`ProjectileWrapper` / `BulletWrapper` / `RocketWrapper`](../Assets/Scripts/Gameplay/Weapon/ProjectileWrapper.cs)
  are plain classes — hit execution (`ExecuteHit` → `target.DealDamage`, `OnHit` event, death)
  is testable with a hand-rolled fake `ITargetable` plus an `AddComponent<ProjectileObject>()`
  stand-in. **U+GO.** Two warts: the constructor calls `_projectile.EnableObject()` (fine — it's
  a no-op virtual), and `Death()` calls `ProjectileObject.DisableObject()` which dereferences a
  serialized `_poolObject` — `null` on a bare `AddComponent` instance, so the fake setup must
  populate it or expect the NRE. And `UpdateLifeTime()` reads `Time.deltaTime` directly
  ([line 49](../Assets/Scripts/Gameplay/Weapon/ProjectileWrapper.cs:49)) — in EditMode that
  value is effectively 0, so lifetime expiry **cannot be driven deterministically without the
  Phase-2 time refactor**.
- [`ProjectileController<T>` / `BulletsController`](../Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs)
  and [`RocketsController`](../Assets/Scripts/Gameplay/Weapon/Launcher/RocketsController.cs)
  are `Update`-driven and built on `NativeList` + `RaycastCommand.ScheduleBatch` + a
  `[BurstCompile]` job. This is **INT/PM** territory only — physics raycasts need colliders and
  a physics world. The rocket steering *math* in
  [`CalculateRocketMove`](../Assets/Scripts/Gameplay/Weapon/Launcher/RocketsController.cs:25)
  is extractable (it already works off the `RocketMovementStruct` value type) but today reads
  `rocket.Transform` — **U after a Phase-2 extraction**, PM before.
- [`BulletSpawner` / `RocketSpawner`](../Assets/Scripts/Management/BulletSpawner.cs) take their
  controller via constructor (good) but hit `PoolManager.Instance` (lines 31, 57) — the
  hardest singleton dependency in the codebase. **PM** until Phase 2c.

### 5.4 Weapons — needs seams or PlayMode

- [`WeaponController`](../Assets/Scripts/Gameplay/Weapon/WeaponController.cs) holds the
  fire-gating logic (cooldown via `Update` + `Time.deltaTime`, angle deviation vs
  `_maxAngleDeviation`, range check). The decision logic is 6 lines inside `Update()` —
  testable today only by **PM** (or `U+GO` with reflection to force `_currentTime`, which is
  brittle). A Phase-2 "humble object" split (extract `bool CanShoot(angleDelta, distance)`)
  makes it **U**.
- [`AdvanceWeaponTargeter.CalculatePos`](../Assets/Scripts/Gameplay/Weapon/Projectile/AdvanceWeaponTargeter.cs:64)
  is the most valuable *math* in the project — quadratic intercept solution with a
  discriminant-negative fallback and a divide-by-zero hazard when target speed equals bullet
  speed (guarded only by an exact float equality at line 66). It is `private`, reads serialized
  `Transform`s and a serialized `BulletData`, and is buried in an `Update`-driven MonoBehaviour.
  **U only after extraction** (pure inputs: shooter pos, target pos, target velocity, bullet
  speed). High regression value — this is exactly the code you don't want to re-verify by eye.
- [`SequenceProjectileShooter`](../Assets/Scripts/Gameplay/Weapon/Projectile/SequenceProjectileShooter.cs)
  round-robins spawn points and wires `OnHit` → `BulletHit`. With a fake `IProjectileCreator`
  injected via `InitProjectileCreator` this is **U+GO** — a genuinely well-seamed component.
- [`WeaponFactory`](../Assets/Scripts/Gameplay/Weapon/WeaponFactory.cs) does
  `Object.Instantiate(weaponData.Prefab)` + `GetComponent` chains — **PM** (needs real prefabs).
- [`RayShooter`](../Assets/Scripts/Gameplay/Weapon/Ray/RayShooter.cs) is trivial (`DealDamage` +
  event) — **U+GO**, though there is little to get wrong.

### 5.5 Ships / teams — good seams, some dead code

- [`ShipsManager`](../Assets/Scripts/Management/ShipsManager.cs) takes `IShipsFactory` via
  constructor — **U** with a fake factory (needs `TeamData` SO builder). Note `ResetTeams()` and
  `AllDies()` are empty and [`ShipTeamObserver`](../Assets/Scripts/Management/ShipTeamObserver.cs)
  observes nothing (empty loop body, empty `ShipDeath`) — there is no behaviour to pin yet.
- [`ShipEntity`](../Assets/Scripts/Gameplay/Ship/ShipEntity.cs) is a component locator
  (`GetComponent` in `Awake`) — nothing to unit test.
- [`ShipsHandler.GetChosenShips`](../Assets/Scripts/Gameplay/Control/ShipsHandler.cs:21) is
  self-contained geometry (bounds construction from two corners) and — per
  [INPUT_SYSTEM_RESEARCH.md](INPUT_SYSTEM_RESEARCH.md) — both **unwired in the scene and
  internally buggy** (`Bounds` is given a half-size as `size`, and negative-extent bounds from
  unordered corners never `Contains` anything). If box-select is ever revived, writing the tests
  *first* would document intended behaviour. **U+GO** (needs `SideData` via `CreateInstance` —
  no fields to fill, so no builder needed).
- [`ShipMovementController`](../Assets/Scripts/Gameplay/Control/Easy/ShipMovementController.cs)
  delegates everything to a serialized `NavMeshAgent` — **INT** only (needs baked NavMesh).

### 5.6 Pooling / routines / singletons (Architecture layer) — the PlayMode core

- [`ObjectPool`](../Assets/Scripts/Architecture/Pools/ObjectPool.cs) is a plain class but calls
  `Object.Instantiate` — usable in EditMode tests (instantiation works there) yet its real
  behaviour is entangled with [`PoolObject`](../Assets/Scripts/Architecture/Pools/PoolObject.cs),
  whose `DisableObject()` defers deactivation by one frame through
  `RoutineManager.CreateRoutine(this).NextFrame(...)` — a static singleton call plus a
  coroutine, i.e. **PM**. Given the git history ("Fix projectile disabling", "Ref fixes"), the
  spawn → hit → next-frame-disable → respawn-same-frame cycle is the highest-value *PlayMode*
  test in the project.
- [`RoutineManager`](../Assets/Scripts/Architecture/CoroutineManagement/RoutineManager.cs) /
  [`Routine`](../Assets/Scripts/Architecture/CoroutineManagement/Routine.cs) /
  [`RoutineExtension`](../Assets/Scripts/Architecture/CoroutineManagement/RoutineExtension.cs):
  coroutines need a running player loop — **PM**. `Routine`'s state transitions
  (`Start` idempotence, `Stop` clearing `_onEndAction`) are shallow-testable in **U** since
  `Start()` silently no-ops when the host is null, but the interesting behaviour is temporal.
  Note [`RoutineExtension.SkipFrames`](../Assets/Scripts/Architecture/CoroutineManagement/RoutineExtension.cs:104)
  never decrements `frameCount` — an infinite loop that a PlayMode test would catch immediately
  (currently unused by game code).
- [`MonoSingleton<T>`](../Assets/Scripts/Architecture/Singletons/MonoSingleton.cs) sets a static
  `Instance` in `Awake` and **never clears it** — no `OnDestroy`. Between two PlayMode tests
  that each load a scene, the second scene's singleton `Awake` sees the stale (destroyed)
  `Instance`, hits the `Instance != null` guard (Unity's fake-null makes this comparison
  *false* for destroyed objects, so in practice it re-registers — but any *static consumer*
  holding the old reference is broken). This is the main test-to-test state-leak hazard.
  [`SOSingleton`](../Assets/Scripts/Architecture/Singletons/SOSingleton.cs) additionally couples
  to `Resources.Load` paths — unused by game code today.
- [`QuequeStateMachine` / `StateMachine`](../Assets/Scripts/Architecture/StateMachine/QuequeStateMachine.cs)
  — pure classes, **U**, trivially testable (Next/Prev wraparound via LINQ is worth pinning) —
  but **no game code references them**; value is speculative until they're used.

### 5.7 Input / camera — do not test (yet)

[`MouseInput`](../Assets/Scripts/Gameplay/Input/MouseInput.cs) is a thin adapter over the
generated wrapper; [`ObjectClicker`](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs) polls
`Mouse.current` + `Camera.main` + physics every frame; [`CameraMover`](../Assets/Scripts/Gameplay/Input/Camera/CameraMover.cs)
is feel-tuning. The Input System does ship a `TestFixture` package for device simulation
(`Unity.InputSystem.TestFramework` is in the cache), so input **can** be tested later — but
[INPUT_SYSTEM_RESEARCH.md](INPUT_SYSTEM_RESEARCH.md) already proposes refactoring this whole
area; testing it before that refactor would pin the wrong behaviour.

### 5.8 Determinism and side effects — summary

| Concern | Finding |
|---|---|
| Time | `Time.deltaTime` read directly in 12 files. Never injected. The only *plain-class* offender is `ProjectileWrapper.UpdateLifeTime` — the rest are MonoBehaviour `Update` bodies where it is idiomatic. `Time.timeScale` is never touched. |
| Randomness | One call: `Random.insideUnitCircle` in [`MathExt.cs:23`](../Assets/Scripts/Extentions/MathExt.cs), used only by `ShipSpawner.GetSpawnPos`. `UnityEngine.Random.InitState(seed)` in test setup is sufficient; no injectable RNG needed at this scale. |
| File I/O / network / analytics / platform SDKs | **None in game code.** (The uncommitted 2022 upgrade pulls Unity Services Core packages in, but nothing references them.) |
| Frame-order / lifecycle coupling | Real and PlayMode-relevant: `ShipTarget.Start` consumes what `ShipEntity.Awake` found; `AbstractInitilizer.Awake` caches children before external `Initialize` calls; `PoolObject` deactivates one frame late by design; `ShipSpawner` calls `Initialize` on a freshly instantiated prefab in the same frame (works because `Awake` runs inside `Instantiate`). PlayMode tests must `yield` a frame after spawn/kill actions before asserting. |
| Static/global state between tests | `MonoSingleton<T>.Instance` (see §5.6), `RoutineManager.Instance`, `PoolManager.Instance`, `ObjectClicker.Instance`. No static gameplay data anywhere else — `ServiceProvider` is instance-based. |

### 5.9 Data and configuration

- All balance data is ScriptableObjects under [`Assets/Data/`](../Assets/Data), loaded **only
  via serialized references** — no `Resources.Load`, no hardcoded asset paths (the sole
  `Resources.Load` in the codebase is inside the unused `SOSingleton`). This is good: nothing
  ties configs to the AssetDatabase at runtime.
- `ScriptableObject.CreateInstance<ShipData>()` works in tests, **but every config type exposes
  only getters** over `private [SerializeField]` fields — there is no way to author a fixture
  in memory without reflection. Options in [§6 B2](#6-blockers). Types with no fields worth
  setting (`SideData`, `ShipSize`, `StatData` — pure identity assets) can be `CreateInstance`d
  as-is.
- EditMode tests *may* also load real assets from `Assets/Data/` via
  `AssetDatabase.LoadAssetAtPath` — usable for smoke checks ("every `ShipData` has a hull and
  a weapon") but couples tests to live balance values; prefer built fixtures for logic tests.

---

## 6. Blockers

Sorted by severity. "Blocks" says which kind of test cannot exist until removed.

| # | Blocker | Where | Blocks | Effort | Severity |
|---|---|---|---|---|---|
| B1 | All code in predefined `Assembly-CSharp`; test asmdefs cannot reference it | project structure (no `.asmdef` under `Assets/`) | **all** NUnit tests, Edit and Play | S (settings toggle) / M (production asmdef) — see Phase 1 | **Critical** |
| B2 | SO configs not populatable in memory — `private [SerializeField]` + getter-only (`ShipData`, `WeaponData`, `ProjectileData`, `TeamData`, `ShipHullData`) | [`Assets/Scripts/SO/**`](../Assets/Scripts/SO) | unit tests of every config consumer (`ShipStatsController.Init`, `ShipsManager`, `WeaponController.Init`, …) | S (test-side reflection builder, zero prod change) | High |
| B3 | `PoolManager.Instance` inside `BulletSpawner.InstantiateProjectile` / `RocketSpawner.InstantiateProjectile` | [`BulletSpawner.cs:31,57`](../Assets/Scripts/Management/BulletSpawner.cs) | unit tests of projectile spawning; forces PM | M (inject a pool-lookup seam; Phase 2c) | High |
| B4 | `MonoSingleton<T>.Instance` never reset (no `OnDestroy`) → state leaks across PlayMode tests | [`MonoSingleton.cs`](../Assets/Scripts/Architecture/Singletons/MonoSingleton.cs) | reliable PlayMode suites (flakiness) | S (clear on destroy; Phase 2d) | High |
| B5 | `Time.deltaTime` read directly in the plain class `ProjectileWrapper.UpdateLifeTime` and in `WeaponController.Update` cooldown | [`ProjectileWrapper.cs:49`](../Assets/Scripts/Gameplay/Weapon/ProjectileWrapper.cs), [`WeaponController.cs:62`](../Assets/Scripts/Gameplay/Weapon/WeaponController.cs) | deterministic unit tests of lifetime/cooldown; forces PM + real waiting | S–M (pass `deltaTime` as parameter; Phase 2a) | Medium |
| B6 | `RoutineManager.Instance` + coroutine in `PoolObject.DisableObject` (one-frame deferred disable) | [`PoolObject.cs:21`](../Assets/Scripts/Architecture/Pools/PoolObject.cs) | unit tests of pool return logic; pool lifecycle is PM-only | M (would need routine abstraction — **not recommended**; test via PM instead) | Medium |
| B7 | Boot chain scene-wired via `[SerializeField]` (`ProviderBuilder`, `GameInitiliazer`, `ShipSpawner` spawn zones) | [`ProviderBuilder.cs:11-14`](../Assets/Scripts/Management/ProviderBuilder.cs) | isolated integration tests; forces full-`SampleScene` PM tests | M (test-scene variant or builder overload) — acceptable to live with | Medium |
| B8 | Lead-prediction math `private` inside an `Update`-driven MonoBehaviour with serialized deps | [`AdvanceWeaponTargeter.cs:64`](../Assets/Scripts/Gameplay/Weapon/Projectile/AdvanceWeaponTargeter.cs) | unit-testing the highest-value math | S–M (extract static pure function; Phase 2b) | Medium |
| B9 | `ObjectClicker.Instance` polled by `ShipInput` | [`ShipInput.cs:25,52,56`](../Assets/Scripts/Gameplay/Input/ShipInput.cs) | unit-testing selection flow | M — but deliberately deferred (input refactor pending, §5.7) | Low |
| B10 | Projectile movement is Burst jobs + `RaycastCommand` batch physics | [`BulletsController.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs), [`ProjectileRaycaster.cs`](../Assets/Scripts/Gameplay/Weapon/ProjectileRaycaster.cs) | unit-testing bullet movement — inherently; PM integration is the honest level | L (rewrite — **not worth it**) | Low |
| B11 | `UnityEngine.Random` (global) in `MathExt.GetRandomPosInsideCircle` | [`MathExt.cs:23`](../Assets/Scripts/Extentions/MathExt.cs) | none, given `Random.InitState` in setup | S | Low |
| B12 | No CI, and the repo's committed/working-tree Unity versions disagree (2021.3.2f1 vs 2022.3.62f2) | repo root / `ProjectSettings` | automated *running* of any tests; CI version pinning | M (GameCI workflow) — **blocked on Q1** | High (for automation, not for writing tests) |

---

## 7. Coverage opportunity map

Value = cost of an uncaught bug there. Sorted: zero-change targets first (these are the starting
point), then by value-to-effort. All "U"/"U+GO" rows assume Phase 1 (B1 resolved) — nothing
compiles before that.

**Zero production-code changes needed:**

| System / class | Test type | Value | Effort | Prerequisites |
|---|---|---|---|---|
| `Stat` (clamp, events) | Unit (EditMode) | High | S | Phase 1 only |
| `ShipStatsController.DealDamage` / `Init` / death event | Unit (EditMode, `AddComponent`) | **High** | S | Phase 1 + SO builder (B2, test-side) |
| `ServiceProvider` (keying, missing-service throw, duplicate add) | Unit (EditMode) | Med | S | Phase 1 only |
| `ProjectileWrapper.ExecuteHit` (damage routing, `OnHit`, `ToReturn`) | Unit (EditMode, fake `ITargetable`) | High | S | Phase 1 only (lifetime expiry excluded — B5) |
| `SequenceProjectileShooter` (round-robin, `OnHit` wiring) | Unit (EditMode, fake `IProjectileCreator`) | Med | S | Phase 1 only |
| `ShipsManager.InstallTeams` (fake `IShipsFactory`) | Unit (EditMode) | Med | S | Phase 1 + SO builder |
| `MathExt` (`ZeroClamp`; `GetRandomPosInsideCircle` with `Random.InitState`) | Unit (EditMode) | Low | S | Phase 1 only |
| `QuequeStateMachine` Next/Prev wraparound | Unit (EditMode) | Low (dead code) | S | Phase 1 only |
| `ShipsHandler.GetChosenShips` bounds math | Unit (EditMode, GameObjects) | Low today (unwired) → High if box-select revived | S | Phase 1 only |
| Data-asset sanity sweep (every `ShipData`/`TeamData` fully wired) | EditMode + `AssetDatabase` | Med | S | Phase 1 only |
| Boot smoke: load `SampleScene`, assert ships spawned per `TeamData`, no exceptions | **PlayMode** | **High** | M | Phase 1 + scene loading in tests (Q4) |
| Pool lifecycle: spawn → `DisableObject` → next frame inactive → respawn reuse; exhaustion grows pool | **PlayMode** | **High** | M | Phase 1 + a test scene with `RoutineManager`/`PoolManager` (B4 makes suites flaky until Phase 2d) |
| Combat integration: two ships, forced `Shoot`, target dies, `OnDeath` fires, shooting stops | **PlayMode** | High | M–L | Phase 1 + boot smoke working |

**Require targeted production refactors (Phase 2):**

| System / class | Test type | Value | Effort | Prerequisites |
|---|---|---|---|---|
| `AdvanceWeaponTargeter.CalculatePos` (intercept quadratic, discriminant<0 fallback, equal-speed guard) | Unit | **High** | M | Phase 2b extraction |
| `ProjectileWrapper.UpdateLifeTime` expiry | Unit | Med | S | Phase 2a (deltaTime param) |
| `WeaponController` fire gating (cooldown, angle, range) | Unit | High | M | Phase 2a + humble-object split |
| `BulletSpawner` / `RocketSpawner` (data → wrapper mapping) | Unit | Med | M | Phase 2c (pool seam) |
| `RocketsController.CalculateRocketMove` steering | Unit | Med | M | Phase 2e extraction |

---

## 8. Phased action plan

### Phase 0 — inventory and decisions (no code, S)

- **Objective:** remove the two decision blockers so later phases don't thrash.
- **Steps:**
  1. Decide **Q1** (which Unity version is canonical — commit or revert the 2022.3.62f2
     upgrade). Everything CI-related pins to this answer.
  2. Choose the **assembly access route** (the options and trade-offs are in Phase 1 below —
     this is an architectural decision the humans own).
  3. Agree the test folder convention (proposal: `Assets/Tests/EditMode/`,
     `Assets/Tests/PlayMode/`, `Assets/Tests/Shared/` for fakes and the SO builder).
- **Becomes testable:** nothing yet — this phase exists so Phase 1 is a one-shot.
- **Risk:** none.

### Phase 1 — minimal infrastructure (unlock compilation) (S–M)

- **Objective:** make the first test compile and run, with zero production-code changes.
- **The core decision — two viable routes** (presented side by side; neither is chosen here):

  | | Route A: legacy toggle | Route B: production asmdef |
  |---|---|---|
  | What | Enable "Enable playmode tests for all assemblies" (Test Runner window ⋮ menu → sets `playModeTestRunnerEnabled: 1`); tests live in ordinary game folders | Add one `Space-RTS.Runtime.asmdef` at `Assets/Scripts/` (covers all 70 files) + `Tests/EditMode` and `Tests/PlayMode` test asmdefs referencing it |
  | Prod changes | none (a `ProjectSettings` edit — **human-only** in this repo, and agents may not edit ProjectSettings anyway) | one meta-file-level change, but it **reverses the documented "no asmdefs" convention** (AGENTS.md, PROJECT_CONTEXT.md §1) — needs the docs updated in the same change |
  | Test isolation | tests compile into `Assembly-CSharp` → **ship in builds** unless wrapped in `#if UNITY_INCLUDE_TESTS`; NUnit-in-EditMode support via this route needs a spike (Q2) | clean: test code never in builds, standard UTF layout, coverage tooling understands it |
  | Risk | low mechanical risk; long-term smell (option is legacy/hidden for a reason) | recompile of everything; must declare references (`Unity.Collections`, `Unity.Burst`, `Unity.InputSystem`, `Unity.Mathematics`, AI module); generated `PlayerInputMaps.cs` sits under `Assets/Data/` — **outside** `Assets/Scripts/`, so it would stay in `Assembly-CSharp` and `MouseInput` would fail to compile unless the asmdef moves up a level or the generated file's location is changed via the `.inputactions` importer (human step). This is the main hidden cost of Route B. |
  | Verdict-relevant fact | works with tests anywhere, PlayMode certain, EditMode uncertain (Q2) | the standard, documented, future-proof route |

  A third route — putting EditMode tests in an `Editor/` folder to ride `Assembly-CSharp-Editor`
  — is **not** listed as viable: predefined assemblies don't reference NUnit without the toggle
  (same Q2 caveat), and it can't host PlayMode tests at all.
- **Other steps (either route):**
  1. Human creates the folders/asmdefs in the editor (agents must not create `.asmdef`/`.meta`).
  2. Hand-rolled fakes in `Tests/Shared`: `FakeTargetable`, `FakeProjectileCreator`,
     `FakeShipsFactory`, `FakeInput`. **Recommendation to decide:** hand-rolled fakes vs
     NSubstitute. Hand-rolled fits this repo (no package additions, ~5 tiny interfaces, C# 9);
     NSubstitute (via UPM git URL or vendored DLL) pays off only when interfaces multiply.
  3. `SOTestBuilder` (test-side): reflection helpers that `CreateInstance` a config SO and set
     its private serialized fields by name (`ShipData`, `WeaponData`, `TeamData`,
     `ProjectileData`). Zero production change; the alternative — production-side
     `#if UNITY_INCLUDE_TESTS` factory methods — is cleaner to read but touches every SO type
     (humans choose; the builder is the default because it's reversible).
  4. Document the local run commands: Test Runner window, and
     `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml`.
- **Becomes testable:** the entire zero-change block of §7 — realistically 30–50 EditMode cases
  across `Stat`, `ShipStatsController`, `ServiceProvider`, `ProjectileWrapper`,
  `SequenceProjectileShooter`, `ShipsManager`.
- **Risk / blast radius:** Route A: none at runtime (settings only). Route B: compile-graph
  change touching every script — no behaviour change, but a bad reference list breaks the
  build loudly until fixed.

### Phase 2 — targeted refactors that unlock the highest-value systems (M)

Each item is independent, small, and behaviour-preserving; do them in value order, each with its
tests in the same change. All of them are production-code changes → normal workflow gates apply.

- **2a. deltaTime as a parameter** (S): `ProjectileWrapper.UpdateLifeTime(float deltaTime)` —
  its only caller is `ProjectileController.UpdateLifeTime()` which already caches
  `deltaTime`. Same pattern for `WeaponController` (extract cooldown/gating into a method
  taking `deltaTime`). *Alternative:* an injectable `ITimeProvider` service — more general,
  but heavier than this codebase needs; parameter-passing matches the existing
  `deltaTime`-caching idiom. Unlocks: lifetime + cooldown unit tests.
- **2b. Extract intercept math** (S–M): `AdvanceWeaponTargeter.CalculatePos` → a `static`
  pure function (inputs: shooter pos, target pos, target velocity, bullet speed; output:
  predicted position), MonoBehaviour keeps a thin call. Unlocks: unit tests over the
  discriminant branches and the equal-speed edge case.
- **2c. Pool seam for spawners** (M): register `PoolManager` (or a narrow
  `IPoolProvider : IService` it implements) in `ProviderBuilder.RegisterServices()` and pass it
  to `BulletSpawner`/`RocketSpawner` constructors — the composition root already constructs
  both. Removes the only `PoolManager.Instance` uses in game code. *Alternative:* leave as-is
  and test spawners only in PlayMode — viable, cheaper, less isolating.
- **2d. Singleton hygiene** (S): `MonoSingleton` clears `Instance` in `OnDestroy` when it is
  the registered instance. Fixes PlayMode test-to-test leakage (B4) and is arguably a latent
  production bug for any future scene reload.
- **2e. Extract rocket steering** (M): `CalculateRocketMove` → pure function over
  `RocketMovementStruct` + position/rotation inputs. Lower priority than 2b.
- **Becomes testable:** the entire "Phase 2" block of §7.
- **Risk / blast radius:** 2a/2b/2e are mechanical extractions (low). 2c touches the
  composition root and both spawners — moderate; regression check is "bullets and rockets
  still fire in SampleScene". 2d changes singleton lifetime semantics — low but verify no code
  relies on stale `Instance`.

### Phase 3 — PlayMode suite and CI (M–L, ongoing)

- **Objective:** automated regression net for the things unit tests can't see (pooling,
  lifecycle, scene wiring), running on every push.
- **Steps:**
  1. PlayMode tests: boot smoke (`SampleScene` loads, services resolve, ships spawn per
     `TeamData`), pool lifecycle (§7), one combat integration path. Budget one `yield`-frame
     after every spawn/kill (§5.8 frame-order findings).
  2. A minimal test scene (human-made) with only `RoutineManager` + `PoolManager` + a dummy
     pool row, for pool tests that shouldn't pay full-scene cost.
  3. CI: GitHub Actions + [GameCI](https://game.ci/) `unity-test-runner` (the repo already
     lives on GitHub). Needs: a Unity license secret (`UNITY_LICENSE` — Personal licenses
     work; activation is a one-time manual step by the owner), the editor version pinned to
     the answer of Q1, and `Library/` caching (the first cold import of a URP project
     dominates run time). Insert as a single job running EditMode + PlayMode in `-batchmode
     -nographics`.
  4. Optional: Code Coverage package (already present in the upgraded manifest) →
     `-enableCodeCoverage` in the same CI job.
- **Estimated cost per full run** (this project: 70 scripts, one small scene): EditMode suite
  seconds; PlayMode boot + pool + combat ≈ 1–3 min including editor startup; **CI wall-clock
  ≈ 10–20 min cold** (URP import), **≈ 3–6 min warm** with `Library/` cached. Local batchmode
  ≈ 2–4 min end-to-end.
- **Risk / blast radius:** zero on gameplay; CI cost is maintenance of the license secret and
  version pin.

### What I would NOT recommend testing

- **`BulletsController`/`RocketsController` job internals** (B10): testing Burst scheduling and
  `RaycastCommand` batches means re-implementing the physics world; the PlayMode combat test
  covers the observable behaviour at 5% of the cost. A rewrite for testability would be pure
  cost.
- **`CameraMover`, `ShipModelRotator`, `RayVisualizer`, `ProjectileVisual`,
  `SelectGFXController`, `StatBar`, `FPS`, `ConstantCameraAngle`** — feel and visuals; asserting
  lerp intermediate values pins magic numbers, not correctness.
- **`MouseInput`, `ObjectClicker`, `ShipInput`** — pending the input refactor proposed in
  [INPUT_SYSTEM_RESEARCH.md](INPUT_SYSTEM_RESEARCH.md); tests written now would fossilize the
  behaviour that document recommends replacing (one-frame-stale clicks, single-selection).
- **Dead/empty code** — `QuequeStateMachine` (unreferenced), `ShipTeamObserver` (empty bodies),
  `ShipsManager.ResetTeams` (empty), `ShipsHandler` (unwired): there is no behaviour to
  protect. Write the tests when the behaviour arrives (and for `ShipsHandler`, write them
  first — the bounds math is already suspect).
- **`WeaponFactory` in isolation** — it is prefab-composition glue; the PlayMode combat test
  exercises it end-to-end where its real failure modes (missing components on prefabs) live.

---

## 9. Open questions

1. **Which Unity version is canonical?** The committed state is 2021.3.2f1 / UTF 1.1.31; the
   working tree is an uncommitted upgrade to 2022.3.62f2 / UTF 1.1.33 (+ Input System 1.14.0,
   + code coverage). CI pinning, package versions, and even which UTF API is available all
   hang on this. (Also: [PROJECT_CONTEXT.md](Agents/PROJECT_CONTEXT.md) still documents
   2021.3.2f1 / Input System 1.3.0 — whichever way this lands, the doc needs updating in that
   change, per the "code wins" rule.)
2. **Does the legacy `playModeTestRunnerEnabled` toggle make NUnit available to *EditMode*
   tests in predefined assemblies in this Unity version?** Confirmed mechanism for PlayMode;
   the EditMode half needs a 10-minute spike before Route A can be chosen. (I did not verify
   this against UTF source; it is the one load-bearing uncertainty in Phase 1.)
3. **Route B's generated-file problem:** can `PlayerInputMaps.cs` generation be pointed inside
   the asmdef folder via the `.inputactions` importer settings (a human/editor step), or would
   the asmdef need to sit above `Assets/Scripts/`? Determines Route B's exact shape.
4. **Headless viability:** does `SampleScene` boot cleanly under `-batchmode -nographics`
   (Input System device creation in `MouseInput`'s constructor, URP rendering, `Camera.main`
   in `ObjectClicker`)? A single local batchmode run answers it.
5. **Is a NavMesh baked into `SampleScene`** (required for any movement integration test)?
   Almost certainly yes — ships move in play mode — but I did not open the scene file to
   confirm, per the hand-edit ban.
6. **Does the owner have a Unity account/license usable for CI activation**, and is GitHub
   Actions acceptable as the CI host?
7. **Is box selection (`ShipsHandler`) coming back?** Decides whether its geometry tests are
   worth writing now (as executable documentation of intended behaviour) or not at all.
