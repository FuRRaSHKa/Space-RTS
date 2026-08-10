# Ship death / destruction system — DRAFT design

> **DRAFT — design only. Nothing here is implemented.** No code, prefab, config or asset was
> changed while writing this document; the only file created is this one. Every proposal is
> either anchored to a type that exists today (named with its path) or explicitly marked **NEW**.
> Choices that are mine rather than the project's are marked **[DRAFT DECISION]**; everything
> else follows an existing convention and says which one.
>
> Verified against the working tree at commit `b93186d` (clean tree, 2026-08-09/10).
> Where docs and code disagree, the code is treated as "what is" and the docs as "what was
> intended" — see [§4](#4-contradictions-found-between-docs-and-code).

---

## 1. Summary

- **There is no death pipeline today.** The entire implementation is
  `ShipStatsController.Death()` (three lines): set `_isDead`, raise `OnDeath`, null the event.
  Nothing despawns, unregisters, stops, hides or reports the ship.
- **Exactly one subscriber exists in the whole codebase** —
  `ShipWeaponsController.cs:59` (`ShipWeaponsController.Shoot`), which makes *attackers* stop
  firing at a target that dies. That is the only observable consequence of death in the game.
- The dead ship keeps **its own** guns firing, keeps its `NavMeshAgent`, stays selectable and
  commandable, keeps blocking navigation, and keeps absorbing damage (`DealDamage` re-enters
  `Death()` on every subsequent hit).
- **"Dies exactly once" is accidental today**, not guaranteed: the guarantee comes from
  `OnDeath = null` after the raise, not from a guard. `Death()` itself re-runs on every further
  hit. Any consequence added to `Death()` as written would fire repeatedly.
- The seams the design needs **already exist and are mostly unused**: `IDeathHandler`,
  `AbstractInitilizer.DeInitialize()` + `IDeInitializable` (**zero implementers project-wide**),
  `ShipTeamObserver.StartObserving()`/`ShipDeath()` (empty stubs shaped exactly for this),
  `ShipsManager.AllDies()`/`ResetTeams()` (empty), `PoolManager`, `RoutineManager`.
- **The hardest constraint is not the ship, it is what points at the ship.** In-flight
  `ProjectileWrapper`s hold `ITargetable` strongly; `RocketsController.CalculateRocketMove`
  dereferences `rocket.TargetPos` (→ `target.TargetTransform.position`) **every frame**, and
  `RayVisualizer.Update` dereferences a cached `Transform _target` while a beam fades.
  `SetActive(false)` on the ship is safe for all three; `Destroy(gameObject)` is not.
- `Destroy` is called **exactly once** in the entire project (`MonoSingleton.cs:17`, the duplicate
  guard). Destroying a ship would be the first runtime destruction of a gameplay object here.
- Pooling ships is **not** a Phase-1 option: `ShipStatsController.Init` does `_stats.Add(...)`
  without clearing, so a second `Init` throws `ArgumentException`, and `StatBar.Start` subscribes
  with a **lambda** that can never be unsubscribed.
- **No audio, no AoE, no wreckage, no crew/cargo/docking, no scoring, no save exist** in code or
  data. Those parts of the brief are answered with "deliberately out of scope + here is the hook",
  not with speculative infrastructure.
- Recommended shape: one new prefab component (`ShipDeathController`) owning an
  `Alive → Dying → Dead → Removed` sequence, driven by the existing `IDeathHandler.OnDeath`,
  timed by `RoutineManager`, never by a particle callback — plus filling in the two existing team
  stubs. Phase 1 is genuinely small; the expensive parts (pooling, explosions, attribution) are
  deferred with reasons.

---

## 2. Sources consulted

### Documentation

| Path | What it gave this design |
|---|---|
| [`CLAUDE.md`](../CLAUDE.md) | Entry point, doc conventions, the "no commits / no YAML edits" rules |
| [`AGENTS.md`](../AGENTS.md) | Non-negotiables: additive serialization, `[SerializeField]` = manual step, reuse existing systems |
| [`Docs/Agents/WORKFLOW.md`](Agents/WORKFLOW.md) | The 5-phase process; "document the uncertainty, don't invent architecture" |
| [`Docs/Agents/PROJECT_CONTEXT.md`](Agents/PROJECT_CONTEXT.md) | Boot chain, `ServiceProvider`, `PoolManager`, `RoutineManager`, `AbstractInitilizer`, data-asset layout, namespace drift |
| [`Docs/Agents/UNITY_RULES.md`](Agents/UNITY_RULES.md) | §4.4 event/lifecycle symmetry and pooled-object reset; §4.1 humble object; §5 perf; §6 style/member order |
| [`Docs/Agents/GIT_RULES.md`](Agents/GIT_RULES.md) | `.meta` pairing, handoff format |
| [`Docs/Agents/templates/completion-summary.md`](Agents/templates/completion-summary.md) | Report format |
| [`Docs/SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) | **The most relevant prior art.** W-1 (no death pipeline), W-2 (retarget subscription leak), W-5/W-7 (damage path), W-12 (ship pooling deferred), §2.7 lifecycle, §8 recommendation "Fix #1: death pipeline, natural owner `ShipTeamObserver`" |
| [`Docs/INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) | W5 (dead ships stay selectable/commandable), owner-resolved policies (enemy selection is a feature; move never stops guns; Stop = movement only, Hold Fire separate), P1 `SelectionService`, P4 order objects — and its §7 open question #1, which is *this* document's subject |
| [`Docs/TESTING_READINESS_RESEARCH.md`](TESTING_READINESS_RESEARCH.md) | §5.8 frame-order/lifecycle coupling, `MonoSingleton` never clears `Instance`, `PoolObject`'s one-frame deferred disable as the historical bug source |

### Code (read in full)

Ship: [`ShipEntity.cs`](../Assets/Scripts/Gameplay/Ship/ShipEntity.cs),
[`ShipStatsController.cs`](../Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs),
[`Stat.cs`](../Assets/Scripts/Gameplay/Ship/ShipStats/Stat.cs),
[`ShipTarget.cs`](../Assets/Scripts/Gameplay/Ship/ShipTarget.cs),
[`ShipControlInterpreter.cs`](../Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs),
[`ShipInitilizer.cs`](../Assets/Scripts/Gameplay/Ship/ShipInitilizer.cs),
[`ShipModelRotator.cs`](../Assets/Scripts/Gameplay/Ship/ShipModelRotator.cs),
[`ShipUiStats.cs`](../Assets/Scripts/Gameplay/Ship/ShipUiStats.cs),
[`SelectGFXController.cs`](../Assets/Scripts/Gameplay/SelectGFXController.cs),
[`ShipMovementController.cs`](../Assets/Scripts/Gameplay/Control/Easy/ShipMovementController.cs),
[`StatBar.cs`](../Assets/Scripts/UI/StatBar.cs).

Weapons/projectiles: [`ShipWeaponsController.cs`](../Assets/Scripts/Gameplay/Weapon/ShipWeaponsController.cs),
[`WeaponController.cs`](../Assets/Scripts/Gameplay/Weapon/WeaponController.cs),
[`WeaponFactory.cs`](../Assets/Scripts/Gameplay/Weapon/WeaponFactory.cs),
[`RayShooter.cs`](../Assets/Scripts/Gameplay/Weapon/Ray/RayShooter.cs),
[`RayVisualizer.cs`](../Assets/Scripts/Gameplay/Weapon/Ray/RayVisualizer.cs),
[`SimpleWeaponTargeter.cs`](../Assets/Scripts/Gameplay/Weapon/Ray/SimpleWeaponTargeter.cs),
[`AdvanceWeaponTargeter.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/AdvanceWeaponTargeter.cs),
[`SequenceProjectileShooter.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/SequenceProjectileShooter.cs),
[`ProjectileVisual.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/ProjectileVisual.cs),
[`ProjectileWrapper.cs`](../Assets/Scripts/Gameplay/Weapon/ProjectileWrapper.cs),
[`ProjectileObject.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/ProjectileObject.cs),
[`BulletObject.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/BulletObject.cs),
[`RocketObject.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/RocketObject.cs),
[`BulletsController.cs`](../Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs)
(holds `ProjectileController<T>`),
[`RocketsController.cs`](../Assets/Scripts/Gameplay/Weapon/Launcher/RocketsController.cs),
[`BulletSpawner.cs`](../Assets/Scripts/Management/BulletSpawner.cs).

Management/architecture: [`ProviderBuilder.cs`](../Assets/Scripts/Management/ProviderBuilder.cs),
[`GameInitiliazer.cs`](../Assets/Scripts/Management/GameInitiliazer.cs),
[`GameController.cs`](../Assets/Scripts/Management/GameController.cs),
[`ShipSpawner.cs`](../Assets/Scripts/Management/ShipSpawner.cs),
[`ShipsManager.cs`](../Assets/Scripts/Management/ShipsManager.cs),
[`ShipTeamObserver.cs`](../Assets/Scripts/Management/ShipTeamObserver.cs),
[`AbstractInitilizer.cs`](../Assets/Scripts/Architecture/AbstractInitilizer.cs),
[`PoolManager.cs`](../Assets/Scripts/Architecture/Pools/PoolManager.cs),
[`ObjectPool.cs`](../Assets/Scripts/Architecture/Pools/ObjectPool.cs),
[`PoolObject.cs`](../Assets/Scripts/Architecture/Pools/PoolObject.cs),
[`RoutineManager.cs`](../Assets/Scripts/Architecture/CoroutineManagement/RoutineManager.cs),
[`Routine.cs`](../Assets/Scripts/Architecture/CoroutineManagement/Routine.cs),
[`RoutineExtension.cs`](../Assets/Scripts/Architecture/CoroutineManagement/RoutineExtension.cs),
[`MonoSingleton.cs`](../Assets/Scripts/Architecture/Singletons/MonoSingleton.cs).

Input/selection: [`ShipInput.cs`](../Assets/Scripts/Gameplay/Input/ShipInput.cs),
[`ObjectClicker.cs`](../Assets/Scripts/Gameplay/Control/ObjectClicker.cs).

Data: [`ShipData.cs`](../Assets/Scripts/SO/Ships/ShipData.cs),
[`ShipHullData.cs`](../Assets/Scripts/SO/Ships/ShipHullData.cs),
[`WeaponData.cs`](../Assets/Scripts/SO/Ships/WeaponData.cs),
[`TeamData.cs`](../Assets/Scripts/SO/Ships/TeamData.cs),
[`SideData.cs`](../Assets/Scripts/SO/ENUMS/SideData.cs).

### Read-only inspections (no edits)

- `Assets/Scenes/SampleScene/SampleScene.unity` — `PoolManager._poolPairs` holds exactly **two**
  rows: `BulletPrefab` (prebaked 20) and `RocketPrefab` (prebaked 5). **No VFX pool exists.**
  GUID searches confirm `FPS.cs`, `ShipsHandler.cs` and `ShipUiStats.cs` are referenced **0 times**
  in the scene.
- `Assets/Prefabs/Ships/SmallShip.prefab` — 9 `MonoBehaviour`s, 1 `NavMeshAgent`, 1 `Rigidbody`,
  1 `CapsuleCollider`, 1 `MeshCollider`, no `PoolObject`.
- `ProjectSettings/ProjectVersion.txt` — **2022.3.62f2**; `Packages/manifest.json` — Input System
  **1.14.0**, URP **14.0.12** (see [§4](#4-contradictions-found-between-docs-and-code)).
- `git status --porcelain` — clean; `git log --oneline -20` — HEAD `b93186d`.

---

## 3. Current state — what exists today regarding ship death

### 3.1 The whole of it

```csharp
// Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs:97-103
private void Death()
{
    _isDead = true;
    OnDeath?.Invoke();

    OnDeath = null;
}
```

Called from `DealDamage` (`ShipStatsController.cs:92-94`) when the sum of all stats with
`DamageOrder > 0` reaches `<= 0`. That is the only death trigger in the project.

### 3.2 The contract that already exists

```csharp
// Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs:23-31
public interface IDeathHandler
{
    public bool IsDead { get; }
    public event Action OnDeath;
}
```

Reachable two ways, both already used:

- `ShipEntity.DeathHandler` (`ShipEntity.cs:28`), cached in `Awake` via `GetComponent<IDeathHandler>()`.
- `ITargetable.TargetDataObservable.DeathHandler` — `ShipTarget.Start` builds a `ShipDataObserver`
  wrapping the death handler and the movement controller (`ShipTarget.cs:31`). This is how an
  *attacker* reaches its *target's* death.

### 3.3 What actually happens when health reaches zero

| Subsystem | What happens today | Where |
|---|---|---|
| Attacker's weapons | ✅ Stop firing at the dead ship | `ShipWeaponsController.cs:59` (`OnDeath += StopShooting`) |
| New attack orders on a dead ship | ✅ Refused | `ShipWeaponsController.cs:55` (`if (...IsDead) return;`) |
| **The dead ship's own weapons** | ❌ Keep firing — nothing calls its own `StopShooting` | — |
| Movement | ❌ `NavMeshAgent` untouched; the ship completes its last order and keeps occupying the NavMesh | — |
| Selection | ❌ Still selectable (`ObjectClicker` raycast still hits the hull collider); a selected dead ship keeps its selection ring | `ShipInput.ChooseClick` has no death filter |
| Orders | ❌ Still commandable — `ShipControlInterpreter.IsEnableToControl` (`:39`) checks side only | `ShipControlInterpreter.cs:39` |
| Damage | ❌ Keeps taking hits; each hit re-enters `Death()` (event already null, so no visible effect) | `ShipStatsController.cs:92` |
| Despawn / pooling | ❌ Nothing. The GameObject lives until the scene unloads | — |
| Team bookkeeping | ❌ `ShipTeamObserver.StartObserving()` is an empty `foreach`; `ShipDeath()` is empty; `OnAllDies` is never raised or subscribed | `ShipTeamObserver.cs:21,29` |
| Win/lose | ❌ `ShipsManager.AllDies(SideData)` is private, empty, never called; `ResetTeams()` empty | `ShipsManager.cs:46,50` |
| VFX / audio / animation | ❌ None. No death effect of any kind; **no audio code exists anywhere in the project** | — |
| UI | ❌ Stat bars keep rendering at 0; `ShipUiStats` is an empty stub on no prefab | `StatBar.cs`, `ShipUiStats.cs` |
| AI | n/a — **there is no AI** | — |
| Scoring / objectives / save | n/a — none exist | — |

### 3.4 Subsystems with hooks death would have to use

| Hook | State | Notes |
|---|---|---|
| `IDeathHandler.OnDeath` | Live, 1 subscriber | The natural trigger fan-out point |
| `IWeaponController.StopShooting()` | Live | Exists on the ship root; nothing calls it on self-death |
| `IMovementController` | Live but **has no `Stop()`** — only `CurrentVelocity` and `MoveTo` | A stop needs a new interface member (also wanted by [INPUT_SYSTEM_RESEARCH P4](INPUT_SYSTEM_RESEARCH.md)'s `StopOrder`) |
| `ISelectable.DeSelect()` | Live (`SelectGFXController`) | Turns the ring off locally; does **not** clear `ShipInput._currentObject` (private, no seam) |
| `AbstractInitilizer.DeInitialize()` + `IDeInitializable` | **Exists, zero implementers** | The intended teardown/reset seam, never used |
| `PoolManager` / `ObjectPool` / `PoolObject` | Live for projectiles only | Two pool rows in the scene; **no VFX pool** |
| `RoutineManager` / `Routine` + `IStopable` | Live | The project's only timing mechanism; `RocketObject.DisableObject` is the reference pattern |
| `ShipTeamObserver` / `ShipsManager` / `GameController` | Stubs, wired into the boot chain | `ShipsManager` already receives the full `List<ShipEntity>` per team from `ShipSpawner.CreateShips` |
| `ShipSpawner` (`IShipsFactory`) | Live | `Instantiate` only, no despawn counterpart, ships parented to `_shipParent` |

### 3.5 The references that outlive the ship (the real design constraint)

| Holder | What it holds | Behaviour if the ship is `SetActive(false)` | Behaviour if the ship is `Destroy`ed |
|---|---|---|---|
| `ProjectileWrapper.target` (`ProjectileWrapper.cs:19`) | `ITargetable` (a `ShipTarget` component) | Safe — `ExecuteHit` → `DealDamage` on a disabled component is a plain method call | **NRE / MissingReference** on hit |
| `ProjectileWrapper._colliderInstanceId` | `int`, snapshot at construction | Safe — a disabled collider simply never appears in a `RaycastCommand` result, so the projectile expires by lifetime | Safe (it is an int), but the hit path above still breaks |
| `RocketWrapper.TargetPos` → `target.TargetTransform.position`, dereferenced **every frame** in `RocketsController.CalculateRocketMove` (`:31`) | `Transform` | Safe — an inactive `Transform` still has a valid `position` | **NRE every frame** for every live rocket |
| `RayVisualizer._target` (`:33`, used in `Update` `:57`) | `Transform` | Safe | **NRE** while the beam fades (`_duration`) |
| `WeaponController._target`, `IWeaponTargeter._targetable` | `ITargetable` | Safe; cleared by `StopShooting` via the `OnDeath` subscription | Safe if `StopShooting` ran first |
| `ShipsManager._teams[].._ships` | `List<ShipEntity>` | Stale entry (leak-ish) | Unity fake-null entry in the list |
| `ShipInput._currentObject` | `IControllable` | Stale, still commandable | `MissingReferenceException` on the next order |

**This table is the single most important input to the removal decision** ([D1](#d1--removal-strategy)).

### 3.6 Facts that block naive reuse (pooling)

- `ShipStatsController.Init` does `_stats.Add(statData.StatData, stat)` with **no `Clear()`** →
  a second `Init` on the same instance throws `ArgumentException`.
- `ShipStatsController.Init` subscribes `stat.OnStatChange += () => { ... }` — a **lambda**,
  unsubscribable by contract ([UNITY_RULES §4.4](Agents/UNITY_RULES.md)).
- `StatBar.Start` subscribes `_shipEntity.StatsController.OnStatChange += (data, value) => ...` —
  another lambda, and it runs in `Start`, i.e. once per instantiation, not once per spawn.
- `ShipWeaponsController.Init` `Instantiate`s one gun per mount and **appends** to `_weaponList` →
  a second `Init` doubles the guns.
- Ship prefabs carry **no `PoolObject`** component, and `PoolManager` in `SampleScene` has no row
  for them.

---

## 4. Contradictions found between docs and code

Code is treated as "what is". None of these were fixed here — this task creates one document only.

| # | Doc says | Code says | Severity for this design |
|---|---|---|---|
| C-1 | [`PROJECT_CONTEXT.md`](Agents/PROJECT_CONTEXT.md) §1: Unity **2021.3.2f1**, URP **12.1.6**, Input System **1.3.0**. [`CLAUDE.md`](../CLAUDE.md) repeats 2021.3.2f1 | `ProjectSettings/ProjectVersion.txt` = **2022.3.62f2**; manifest = URP **14.0.12**, Input System **1.14.0** | **The one that worries me.** The upgrade [`TESTING_READINESS_RESEARCH.md`](TESTING_READINESS_RESEARCH.md) §9 Q1 flagged as *uncommitted* has since been committed and the docs never caught up. Low impact on death specifically, high impact on trusting `PROJECT_CONTEXT` as a baseline |
| C-2 | `PROJECT_CONTEXT.md` §4 (Pooling): "Projectiles, **VFX** and anything spawned per-shot come from a pool" | Only `BulletPrefab` and `RocketPrefab` have `PoolPair` rows. All VFX today are `ParticleSystem` references living **on the gun prefab** and replayed in place (`ProjectileVisual._hitSystem`, `RayVisualizer._particleSystem`, `RocketObject._trails`) | Direct: a death explosion would be the **first pooled VFX in the project**, so "just use the existing VFX pool" is not available — a new `PoolPair` row is a manual editor step |
| C-3 | `PROJECT_CONTEXT.md` §7: verify by selecting ships with "`ObjectClicker` / `ShipsHandler`" and watching the `FPS.cs` counter | `ShipsHandler` has zero callers and **zero references in `SampleScene`**; `FPS.cs` has **zero references in `SampleScene`** | Minor, but it means "how to test manually" cannot cite an FPS readout. Already flagged in [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §4.8 and still true |
| C-4 | `SHIP_SYSTEM_RESEARCH.md` §2.7: "`Death()` … then nulls the event" — presented as the cleanup | Correct, but the doc does not note that `Death()` **itself is not guarded** — `DealDamage` on an already-dead ship calls `Death()` again (harmlessly today, because the event is null) | Direct: the "dies once" invariant must be *added*, not assumed |
| C-5 | `SHIP_SYSTEM_RESEARCH.md` §9 Q1 / `PROJECT_CONTEXT.md` §6: the naming cleanup was "in progress", typo spellings "still API" | Resolved — `b93186d` committed the cleanup and `PROJECT_CONTEXT` §6 now documents the retained spellings accurately | None; noted so the next reader does not re-open it |
| C-6 | `INPUT_SYSTEM_RESEARCH.md` §7 open question 1: "nothing despawns/disables dead ships … Selection auto-removal design in P1 depends on whether dead ships will be destroyed, pooled, or become wrecks" | Still exactly true | This document is the answer to that question; [D1](#d1--removal-strategy) and [D9](#d9--selection-removal) are where it gets decided |

No contradiction was found between the docs and the code **about death behaviour itself** — the
docs describe the gap accurately. The gaps above are about the environment around it.

---

## 5. Proposed design

### 5.1 Shape in prose

Death splits into three concerns that today are fused into one three-line method:

1. **Decision** — "this ship is dead". Stays exactly where it is:
   `ShipStatsController.DealDamage` → `Death()`. It owns the stats, so it owns the verdict.
   The only change is an explicit re-entry guard.
2. **Sequence** — "what happens now, in what order, over how long". **NEW**: a
   `ShipDeathController` component on the ship prefab root, implementing
   `IInitializable<ShipInitializationData>` so it receives config through the existing
   `ShipInitilizer` broadcast (the project's standard way to add a prefab component —
   [`PROJECT_CONTEXT.md`](Agents/PROJECT_CONTEXT.md) §4). It subscribes to
   `ShipEntity.DeathHandler.OnDeath` and drives the state model below.
3. **Fan-out** — "who else needs to know". Two audiences with different needs:
   - *Systems that already watch this ship* (attacking ships) — served by the existing
     `IDeathHandler.OnDeath`, unchanged.
   - *Systems that own the ship* (team bookkeeping, later: selection, scoring) — served by
     `ShipTeamObserver`, whose empty `StartObserving()`/`ShipDeath()` stubs were written for
     precisely this and which already holds the `List<ShipEntity>` per side.

**Gameplay resolution is synchronous and complete before any presentation starts.** The `Dying`
phase exists only so the ship can be *seen* to die; nothing downstream waits for it. Its duration
comes from config and its end is scheduled by `RoutineManager`, never by a particle-system
callback. If the config is missing or the duration is `0`, the ship goes `Alive → Dying → Dead`
within one frame and the game is unaffected.

### 5.2 State model

Four states. `Dying` and `Dead` are separate because "the gameplay entity is gone" and "the visual
is gone" happen at different times; `Dead` and `Removed` are separate because the GameObject must
survive long enough for the references in [§3.5](#35-the-references-that-outlive-the-ship-the-real-design-constraint)
to be released. **[DRAFT DECISION]** — the project has no existing state-model convention for
entities (`QuequeStateMachine` is an unused *sequential queue*, not an FSM, and is not a fit; the
same conclusion was reached in [`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) P3 for input
contexts).

```
                    ┌──────────────────────────────────────────────────────────┐
                    │                        ALIVE                             │
                    │  full behaviour; IDeathHandler.IsDead == false           │
                    └───────────────┬──────────────────────────────────────────┘
                                    │
        trigger (exactly one wins, see §5.3):
          • ShipStatsController.DealDamage → sum(stats) <= 0
          • Kill()            [NEW, instant-kill / scripted]
          • Despawn()         [NEW, silent removal — skips DYING]
                                    │
                                    ▼
   ┌────────────────────────────────────────────────────────────────────────────┐
   │  ENTER DYING  —  ALL OF THIS RUNS SYNCHRONOUSLY, IN THIS ORDER, ONCE       │
   │                                                                            │
   │  1. _isDead = true                        ShipStatsController.Death()      │
   │  2. raise IDeathHandler.OnDeath           → attackers StopShooting()       │
   │                                           → ShipTeamObserver.ShipDeath()   │
   │  3. own weapons off                       IWeaponController.StopShooting() │
   │  4. own movement off                      IMovementController.Stop()  [NEW]│
   │  5. targetability off                     hull Collider.enabled = false    │
   │       └─ side effects, both wanted:                                        │
   │            • ObjectClicker raycast no longer hits it → unselectable        │
   │            • in-flight bullets can no longer hit it → expire by lifetime   │
   │  6. selection ring off                    ISelectable.DeSelect()           │
   │  7. UI off                                ShipCanvas SetActive(false)      │
   │  8. (Phase 3) explosion damage            single, non-chaining pass        │
   │  9. (Phase 3) fire-and-forget VFX         pooled prefab, NOT awaited       │
   │ 10. schedule the exit                     RoutineManager.Wait(duration)    │
   └───────────────────────────────┬────────────────────────────────────────────┘
                                   │  _deathDuration (config; 0 ⇒ same frame)
                                   ▼
   ┌────────────────────────────────────────────────────────────────────────────┐
   │  DEAD  —  the hull is hidden, the entity is inert                          │
   │    • GFX root SetActive(false)                                             │
   │    • the GameObject still EXISTS and its Transform is still valid,         │
   │      because rockets and ray beams may still dereference it                │
   │    • schedule the exit: RoutineManager.Wait(_referenceGraceTime)           │
   └───────────────────────────────┬────────────────────────────────────────────┘
                                   │  grace ≥ max(projectile lifetime, beam duration)
                                   │  — OR immediately, if D3/Phase 2 projectile
                                   │    invalidation is implemented instead
                                   ▼
   ┌────────────────────────────────────────────────────────────────────────────┐
   │  REMOVED  —  gone from the world                                           │
   │    Phase 1: gameObject.SetActive(false)          (recommended)             │
   │    later  : Destroy(gameObject)  |  PoolObject.DisableObject()   → see D1  │
   │    ShipTeamObserver drops it from its list; OnAllDies if the side is empty │
   └────────────────────────────────────────────────────────────────────────────┘

   Interruptions:
     scene unload / play-mode exit → routines die with RoutineManager; no cleanup owed
     Time.timeScale = 0            → Routine.Wait uses WaitForSeconds (scaled) ⇒ the
                                     sequence freezes with the game. Correct by default.
     pool exhaustion (VFX)         → ObjectPool grows on demand (ObjectPool.SpawnObject);
                                     cannot fail, cannot block the sequence
     ship destroyed mid-sequence   → cannot happen: only this component removes it
```

### 5.3 Death triggers and the once-only mechanism

| Trigger | Exists today? | Proposal |
|---|---|---|
| Health `<= 0` | ✅ `ShipStatsController.DealDamage:92` | Unchanged |
| Instant kill / scripted removal | ❌ | **NEW** `Kill()` on the death handler (or a separate `IKillable`) — enters `Dying` with the full sequence. Needed by debug tooling and by any future ability |
| Owner despawn / team reset | ❌ (`ShipsManager.ResetTeams()` is an empty stub) | **NEW** `Despawn()` — jumps straight to `Removed`, no VFX, no team event. This is what `ResetTeams` would call |
| Environmental / out-of-bounds | ❌ no bounds or environment-damage concept exists | **Deliberately out of scope.** `Kill()` is the hook if it ever arrives |
| Collision damage | ❌ ships have colliders and a `Rigidbody` but no collision damage code | Out of scope |

**Once-only mechanism — three layers, because today there are zero:**

1. `ShipStatsController.Death()` gains a guard as its first line:
   ```csharp
   private void Death()
   {
       if (_isDead)
           return;

       _isDead = true;
       OnDeath?.Invoke();
       ...
   }
   ```
   This is the authoritative guard. It is set **before** the event is raised, so a re-entrant
   `DealDamage` from inside an `OnDeath` handler (an explosion hitting the exploder) is a no-op.
2. `ShipDeathController` keeps its own `ShipDeathState _state` and ignores any transition that is
   not a forward step. Belt and braces, and it also makes `Kill()`-after-death safe.
3. The existing `OnDeath = null` line — see [D5](#d5--keep-or-drop-ondeath--null); it is currently
   doing this job by accident and should stop being the mechanism either way.

---

## 6. Types and integration points

### 6.1 New types

| Name | Kind | Responsibility | Plugs into |
|---|---|---|---|
| `ShipDeathController` | `MonoBehaviour`, ship prefab root | Owns the death sequence and the state. Subscribes to `IDeathHandler.OnDeath`, disables subsystems in order, schedules `Dying → Dead → Removed` via `RoutineManager` | Implements `IInitializable<ShipInitializationData>` (`AbstractInitilizer.cs:35`) so `ShipInitilizer` feeds it `ShipData`. Reads siblings through `ShipEntity` (`ShipEntity.cs:25-30`) |
| `ShipDeathState` | `enum` (runtime only, never serialized) | `Alive, Dying, Dead, Removed` | — (if it is ever serialized it becomes append-only per [UNITY_RULES §1.4](Agents/UNITY_RULES.md)) |
| `IShipDeathObserver` *(optional)* | interface | `event Action<ShipEntity> OnShipRemoved` — lets owners react at `Removed`, not at `Dying` | Consumed by `ShipTeamObserver`; only needed if [D3](#d3--how-consequences-are-delivered) picks the event-driven variant |
| `ShipDeathData` *(optional)* | `ScriptableObject`, `Assets/Scripts/SO/Ships/` → `Assets/Data/Ships/` | `_dyingDuration`, `_deadGraceTime`, `_deathVfxPrefab`, `_explosionRadius`, `_explosionDamage` | Referenced from `ShipHullData` or `ShipData` — see [D8](#d8--where-death-config-lives) |
| `ShipDeathVisual` *(Phase 3)* | `MonoBehaviour`, ship prefab | Spawns the pooled explosion, hides the GFX root. Subscribes to the controller's `OnDying` | Mirrors the existing `ProjectileVisual` / `RayVisualizer` pattern: presentation subscribes to gameplay events and gameplay never calls it |

Sketch — **not production code**, and note the namespace is copied from `ShipEntity.cs`, not
inferred from the folder ([`PROJECT_CONTEXT.md`](Agents/PROJECT_CONTEXT.md) §3):

```csharp
namespace HalloGames.SpaceRTS.Gameplay.Ship
{
    public enum ShipDeathState { Alive, Dying, Dead, Removed }

    public class ShipDeathController : MonoBehaviour, IInitializable<ShipInitializationData>
    {
        [SerializeField] private Collider   _hullCollider;   // manual wiring step
        [SerializeField] private GameObject _gfxRoot;        // manual wiring step
        [SerializeField] private GameObject _uiRoot;         // manual wiring step (ShipCanvas)

        private ShipEntity  _shipEntity;
        private IStopable   _stopable;
        private ShipDeathState _state = ShipDeathState.Alive;

        public ShipDeathState State => _state;

        public event Action<ShipEntity> OnDying;    // presentation + team bookkeeping
        public event Action<ShipEntity> OnRemoved;  // owners drop their reference here

        private void Awake() => _shipEntity = GetComponent<ShipEntity>();

        public void Init(ShipInitializationData data)
        {
            _state = ShipDeathState.Alive;
            _shipEntity.DeathHandler.OnDeath += EnterDying;   // method ref, not a lambda
        }

        private void OnDestroy()
        {
            _stopable?.Stop();
            if (_shipEntity != null && _shipEntity.DeathHandler != null)
                _shipEntity.DeathHandler.OnDeath -= EnterDying;
        }

        private void EnterDying()          { /* steps 1-10 of §5.2, then schedule EnterDead */ }
        private void EnterDead()           { /* hide GFX, schedule EnterRemoved */ }
        private void EnterRemoved()        { /* SetActive(false) | Destroy | pool — see D1 */ }
        public  void Kill()                { /* scripted death → EnterDying */ }
        public  void Despawn()             { /* silent → EnterRemoved */ }
    }
}
```

### 6.2 Modifications to existing types

| File / member | Change | Why |
|---|---|---|
| `ShipStatsController.Death()` (`:97`) | `if (_isDead) return;` as the first line | The once-only invariant; today it is accidental (C-4) |
| `ShipStatsController.Death()` (`:102`) | Decide the fate of `OnDeath = null` | [D5](#d5--keep-or-drop-ondeath--null) |
| `ShipWeaponsController.Shoot()` (`:53-65`) | Unsubscribe the **previous** target's `OnDeath` before subscribing the new one; also unsubscribe in `StopShooting()` | **Prerequisite, not optional.** W-2 in [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md): kill T1 after retargeting to T2 and the ship silently stops shooting T2. A death system whose main consequence is "attackers stop shooting" cannot ship on top of a broken subscription |
| `IMovementController` (`ShipMovementController.cs:8-16`) | Add `void Stop();` → `_navMesh.isStopped = true` / `ResetPath()` | Nothing can stop a ship today. Also required by [`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) P4's `StopOrder` — build it once |
| `ShipTeamObserver.StartObserving()` (`:21`) | Fill the empty `foreach`: subscribe each ship's death | The stub exists for exactly this; [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §8 already names it the natural owner |
| `ShipTeamObserver.ShipDeath()` (`:29`) | Remove the ship from `_ships`; if empty → `OnAllDies?.Invoke()` | Same |
| `ShipsManager.StartObserving()` (`:38`) / `AllDies()` (`:46`) | Subscribe `team.OnAllDies` → `AllDies(side)`; surface it | The private empty `AllDies(SideData)` is the intended sink |
| `IShipsManager` (`ShipsManager.cs:10`) | Add `event Action<SideData> OnTeamEliminated` (name TBD) | `GameController` currently has no way to learn anything |
| `GameController` (`:24`) | Subscribe and do… something | **Nothing exists to do.** See [D10](#d10--what-happens-when-a-team-is-eliminated) — a `Debug.Log` is an honest Phase-2 answer |
| `ShipControlInterpreter.IsEnableToControl()` (`:39`) | Add a death check | Fixes W5 in [`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) (dead ships stay commandable). Cheap and local |
| `ProjectileWrapper` (`:40-57`) | Phase 2/3: treat a dead target as an expiry condition | The rocket NRE in [§3.5](#35-the-references-that-outlive-the-ship-the-real-design-constraint); only strictly required if removal ever becomes `Destroy` or pooling |
| `PoolManager` in `SampleScene` | Phase 3: a `PoolPair` row for the explosion prefab | Manual editor step; the project's **first VFX pool row** (C-2) |
| `ShipSpawner` | Phase 3 only, if pooling is chosen | Would need a despawn counterpart to `CreateShips` |

### 6.3 Integration diagram

```
ShipStatsController.DealDamage(int)
        │  sum(damageable stats) <= 0
        ▼
ShipStatsController.Death()          ← guard: if (_isDead) return;
        │  _isDead = true
        │  OnDeath?.Invoke()
        ├──────────────────────────────► ShipWeaponsController.StopShooting()   [attackers]
        │                                (subscribed in Shoot(), :59)
        │
        └──────────────────────────────► ShipDeathController.EnterDying()       [NEW]
                                                │
                    ┌───────────────────────────┼───────────────────────────┐
                    ▼                           ▼                           ▼
        own IWeaponController      own IMovementController        hull Collider.enabled
          .StopShooting()             .Stop()  [NEW member]          = false
                    │                           │                           │
                    └───────────────────────────┴───────────────────────────┘
                                                │
                                 ISelectable.DeSelect(), UI off
                                                │
                                 OnDying ─────► ShipDeathVisual  (pooled VFX, fire & forget)
                                                │
                                 RoutineManager.CreateRoutine(this).Wait(d, EnterDead).Start()
                                                │
                                          EnterDead() → GFX off
                                                │
                                 RoutineManager … Wait(grace, EnterRemoved)
                                                │
                                          EnterRemoved()
                                                │
                                 OnRemoved ────► ShipTeamObserver.ShipDeath()
                                                        │ _ships.Remove(ship)
                                                        │ if (_ships.Count == 0)
                                                        ▼
                                                   OnAllDies ──► ShipsManager.AllDies(side)
                                                                        │
                                                                        ▼
                                                                 GameController  (D10)
```

---

## 7. Invariants

The system must uphold all of these. They are the acceptance criteria for any implementation.

| # | Invariant | Enforced by |
|---|---|---|
| I-1 | **A ship resolves death exactly once.** Repeated `DealDamage`, a `Kill()` on a dying ship, or a re-entrant call from inside an `OnDeath` handler must all be no-ops | `if (_isDead) return;` set **before** the event is raised, plus the controller's forward-only state check |
| I-2 | **No gameplay consequence depends on VFX, audio, animation or particle completion.** Every gameplay effect is applied synchronously on entering `Dying`; timers come from config via `RoutineManager` | Sequence order in §5.2 (gameplay = steps 1-8, presentation = step 9) |
| I-3 | **A dead ship deals no damage.** Its own weapons stop in the same frame the death resolves | Step 3, `IWeaponController.StopShooting()` |
| I-4 | **A dead ship accepts no orders and shows no selection** | Steps 5-6 + the `IsEnableToControl` death check |
| I-5 | **A ship's `Transform` stays valid for as long as any live projectile or beam references it** — or those references are invalidated first. One of the two, never neither | The `Dead → Removed` grace time, **or** projectile/visual invalidation (Phase 2) |
| I-6 | **After `Removed`, no system holds a reference to the ship** — team lists, selection, targeters, weapon controllers | `OnRemoved` fan-out + `ShipTeamObserver.ShipDeath()` |
| I-7 | **Death of A never resolves death of B re-entrantly inside A's sequence.** Explosion damage may kill B, but B's sequence must not run inside A's stack in a way that can loop back to A | I-1's guard is sufficient for the A→B→A case; chain *depth* is a separate policy — [D7](#d7--death-explosion) |
| I-8 | **Presentation is optional.** Missing VFX config, duration `0`, or a missing pool row must not change any gameplay outcome — only how it looks | Null-guard every new `[SerializeField]` ([AGENTS.md](../AGENTS.md) non-negotiable #3) |
| I-9 | **Every `+=` has a matching `-=` on the same method reference.** No lambdas in the death path | [UNITY_RULES §4.4](Agents/UNITY_RULES.md). Note the existing lambdas in `ShipStatsController.Init` and `StatBar.Start` are pre-existing violations that block pooling ([§3.6](#36-facts-that-block-naive-reuse-pooling)) |
| I-10 | **The sequence is interruptible only by scene teardown.** Any running `Routine` is stopped in `OnDestroy` / on re-init, keeping the `IStopable` per [PROJECT_CONTEXT §4](Agents/PROJECT_CONTEXT.md) | `_stopable?.Stop()` before every new routine — the `RocketObject.DisableObject` pattern |
| I-11 | **Death costs nothing per frame while alive.** No new `Update`, no polling | Event-driven entry only |

---

## 8. Edge cases and how each is handled

| # | Edge case | Handling |
|---|---|---|
| E-1 | Two projectiles from different ships land in the same frame and both drive health `<= 0` | I-1's guard. The second `DealDamage` still runs `Stat.ChangeStat` (clamped to 0) but `Death()` returns immediately |
| E-2 | A ship dies **inside** an `OnDeath` handler of another ship (explosion chain) | I-1 + I-7. Depth policy in [D7](#d7--death-explosion); default recommendation is that explosion damage cannot itself trigger an explosion |
| E-3 | A rocket is in flight when its target dies | The rocket's `TargetTransform` stays valid until `Removed`. With the collider disabled it can never hit, so it expires by `RocketData.Lifetime`. **If removal becomes `Destroy`, this NREs every frame** in `RocketsController.CalculateRocketMove` — hence I-5 |
| E-4 | A ray beam is mid-fade when its target dies | `RayVisualizer._target` stays valid for the same reason; the beam finishes over `_duration` and stops. Same `Destroy` hazard |
| E-5 | A bullet is in flight and the target's collider is disabled | The `RaycastCommand` batch no longer reports that collider, the Burst filter (`RaycastResultJob`) never matches, the bullet expires by `BulletData.Lifetime`. Already correct — no change needed |
| E-6 | The player had the ship selected when it died | `ISelectable.DeSelect()` turns the ring off. `ShipInput._currentObject` still holds it (private field, no seam) — see [D9](#d9--selection-removal). With the `IsEnableToControl` death check, orders are refused, so the residual state is invisible |
| E-7 | The player right-clicks a ship that is already dead | Already handled today: `ShipWeaponsController.Shoot:55` returns early on `IsDead` |
| E-8 | The player right-clicks with a dead ship selected | Refused once `IsEnableToControl` checks death (today: the dead ship happily moves) |
| E-9 | An attacking ship dies while shooting at a target | Its own `StopShooting()` runs (step 3), clearing `_currentTarget` and every gun's targeter. Its subscription to the *target's* `OnDeath` must be released — this is why the W-2 fix is a prerequisite |
| E-10 | Both ships in a duel die in the same frame | Each runs its own sequence; each `StopShooting` fires. Order does not matter because neither sequence reads the other's state |
| E-11 | The last ship of a side dies | `ShipTeamObserver` empties → `OnAllDies` → `ShipsManager.AllDies(side)` → `GameController` ([D10](#d10--what-happens-when-a-team-is-eliminated)). Must fire **once**: guard with a `bool` in the observer, because `ShipDeath` can be reached from a despawn path too |
| E-12 | `Time.timeScale = 0` during the sequence | `Routine.Wait` uses `WaitForSeconds`, which is scaled → the sequence freezes with the game and resumes correctly. This is the desired behaviour; stated so nobody "fixes" it into `WaitForSecondsRealtime`. No pause feature exists today ([`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) §7 Q4) |
| E-13 | Scene unload / exit play mode mid-sequence | Coroutines are hosted by the component itself (`RoutineManager.CreateRoutine(this)`), so they die with it. `OnDestroy` stops the `IStopable`. Nothing is owed |
| E-14 | VFX pool exhausted | `ObjectPool.SpawnObject` grows the pool on demand (`ObjectPool.cs:38-40`) — it cannot fail. Worst case is one `Instantiate` spike. The sequence never checks the result for success (I-8) |
| E-15 | Death VFX prefab not wired / no `PoolPair` row for it | Null-guard → skip presentation, gameplay identical (I-8). This *will* happen: the row is a manual editor step |
| E-16 | `ShipDeathController` not added to a ship prefab | That ship reverts to today's behaviour (zombie). Detectable only by playing. A one-line `Debug.LogWarning` from `ShipSpawner` when the component is missing is the cheapest guard — **[DRAFT DECISION]**, and it fits the missing-null-guard theme of W-9 |
| E-17 | A ship dies before `Init` ran (impossible today: `ShipSpawner` instantiates and initializes in the same frame, `Awake` runs inside `Instantiate`) | Noted as an assumption, not defended. If spawning ever becomes deferred, the subscription in `Init` must move or become idempotent |
| E-18 | The Undead test ships (5,000,000 HP, `UndeadShip*` — orphaned, in no `TeamData`) | Nothing special: they die like anything else if damaged enough. If they are meant to be *unkillable* test dummies, that is a new flag — [§11 Q2](#11-open-questions) |
| E-19 | Overkill damage (10,000 damage to a 300 HP ship) | Unchanged by this design. The existing `DealDamage` loop and its `break` conditions are untouched here; their edge cases are [`TESTING_READINESS_RESEARCH.md`](TESTING_READINESS_RESEARCH.md) §5.2's problem, not this document's |
| E-20 | Damage arrives during `Dying` (a bullet already in flight lands the frame after death) | Collider is off, so bullets cannot land. A ray shooter that already resolved this frame could still call `DealDamage` → stats clamp at 0, `Death()` returns on the guard. Safe |
| E-21 | The ship is re-spawned / re-`Init`ed on the same instance (pooling) | **Currently throws** — `ShipStatsController.Init`'s `_stats.Add` on a populated dictionary. See [§3.6](#36-facts-that-block-naive-reuse-pooling); this is why pooling is Phase 3+ and gated on `IDeInitializable` |
| E-22 | A dead ship's wreck blocking the NavMesh | Not an issue while the ship is `SetActive(false)`. It becomes a design question the moment wrecks are wanted — [D6](#d6--wreckage--debris) |

---

## 9. Open design decisions

Each is a real fork. Options side by side, with a recommendation — **the choice is yours.**

### D1 — Removal strategy

| | A. `SetActive(false)`, keep the instance | B. `Destroy(gameObject)` | C. Pool the ship (`PoolObject` + `PoolPair`) |
|---|---|---|---|
| Work | Trivial | Trivial | Large — see [§3.6](#36-facts-that-block-naive-reuse-pooling) |
| Safe for the references in [§3.5](#35-the-references-that-outlive-the-ship-the-real-design-constraint) | ✅ Yes, all of them | ❌ No — requires projectile + visual invalidation first | ❌ No, and worse: a recycled instance means an old projectile can damage a *new* ship |
| Memory | 8 dead GameObjects per match — irrelevant at this scale | Cleanest | Cleanest for waves/restart |
| Precedent in project | `SelectGFXController`, `RocketObject._gfx`, `PoolObject` | **`Destroy` is called exactly once project-wide** (`MonoSingleton:17`) | The established pattern — for projectiles |
| Blocks | Nothing | Nothing, once E-3/E-4 are fixed | Needs `IDeInitializable` implementations, `_stats.Clear()`, the two lambda subscriptions removed, `_weaponList` teardown |

**Recommendation: A for Phase 1**, revisit at C only when respawn/waves exist — which is exactly
what [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §8 "Do not touch (for now)" says about
W-12. B is a trap: it looks tidier and silently breaks rockets.

### D2 — Who owns the sequence

| | A. New `ShipDeathController` component | B. Extend `ShipStatsController` | C. Central `DeathService` (registered in `ProviderBuilder`) |
|---|---|---|---|
| Fits conventions | ✅ Composition-over-inheritance is the ship system's core strength ([`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §5.1); new behaviour = new component | ⚠️ Grows a class that is currently a clean "humble object" and the project's best unit-test target | ⚠️ No per-entity service precedent; would need a ship registry that does not exist |
| Cost | One `[SerializeField]`-carrying component × 3 ship prefabs (manual wiring) | Zero editor work | Zero editor work, more architecture |
| Testability | Component, needs a GameObject | Same | Better in isolation, worse in reality |
| Per-ship config | Natural (`IInitializable<ShipInitializationData>`) | Natural | Awkward — must look up config per ship |

**Recommendation: A.** It is the only option that matches how every other ship behaviour is added.
C becomes attractive only if death ever needs global arbitration (kill feeds, scoring, replay).

### D3 — How consequences are delivered

| | A. Direct calls from `ShipDeathController` | B. Events, subscribers do the work | C. Hybrid (recommended) |
|---|---|---|---|
| Shape | The controller calls `StopShooting()`, `Stop()`, `DeSelect()`… | The controller raises `OnDying`; each component listens | Direct calls **inside the prefab**, events **across prefab boundaries** |
| Order guarantee | ✅ Explicit and readable | ❌ Subscription order is implicit | ✅ Where it matters |
| Coupling | The controller must know its siblings — but it reaches them through `ShipEntity`'s cached interfaces, which is the existing pattern | Loose | Matches the project: `ShipEntity` is already a facade of interfaces, and presentation already uses events (`IShooter.OnShooting`) |
| Debuggability | Step through one method | Scattered | Good |

**Recommendation: C.** It is what the codebase already does — gameplay is direct
(`ShipControlInterpreter` → `WeaponController.Shoot`), presentation is evented
(`ProjectileVisual`, `RayVisualizer`).

### D4 — Kill attribution

Today damage is **anonymous**: `ITargetable.DealDamage(int)`, `IStatsController.DealDamage(int)`,
`ProjectileWrapper.ExecuteHit` → `target.DealDamage(_damage)`, `RayShooter.Shoot` →
`targetable.DealDamage(_damage)`. There is no attacker anywhere in the chain.

| | A. No attribution now | B. Add a source to the damage API | C. "Last damager" recorded out-of-band |
|---|---|---|---|
| Blast radius | Zero | `ITargetable`, `IStatsController`, `ShipTarget`, `ShipStatsController`, `RayShooter`, `ProjectileWrapper`, `BulletSpawner`/`RocketSpawner` (the wrapper would need the shooter), `SequenceProjectileShooter` — 8+ files, two interfaces | Small but lying: with batched projectiles, "last damager" is ambiguous |
| Enables | Nothing | Kill credit, kill feed, scoring, damage-source resistances, friendly-fire tracking | A weak version of the same |
| Dead-killer problem | n/a | Must hold a *weak* notion of the killer — the killer may already be `Removed`. Store `SideData` + a display name, **not** a `ShipEntity` reference | Same |

**Recommendation: A for now**, and design the `OnDying` event so a source parameter can be added
later without changing its shape (i.e. pass a small `struct DeathContext` from day one, even if it
only carries the cause enum). **[DRAFT DECISION]**: nothing in the project consumes kill credit —
no score, no UI, no AI — so B would be infrastructure for an empty room. It becomes worth it the
same day scoring or a kill feed does.

### D5 — Keep or drop `OnDeath = null`

| | A. Keep it | B. Drop it, require symmetric unsubscription |
|---|---|---|
| Pros | Self-cleaning; guarantees no double-notify even with buggy subscribers | Matches [UNITY_RULES §4.4](Agents/UNITY_RULES.md); required if the ship is ever reused (pooling); lets a subscriber that arrives *after* death still be handled coherently |
| Cons | Hides the W-2 class of bug (subscribers never notice they leaked); a pooled/reused ship would silently have no death event on its second life; makes "the event fires once" an accident rather than a guarantee | Every subscriber must be correct — today there is exactly one, and it is buggy (W-2) |

**Recommendation: B, but only together with the W-2 fix, in the same change.** Dropping it while
the retarget leak exists would turn a latent bug into a visible one.

### D6 — Wreckage / debris

| A. None (ship vanishes) | B. Static non-interactive wreck left behind | C. Physics debris |
|---|---|---|
| Zero cost; consistent with the current arcade feel | Cheap visually; but needs a decision about NavMesh blocking (E-22) and about whether wrecks are ever cleaned up | Needs a debris prefab family, a pool, rigidbodies — the project has one `Rigidbody` on ships and no physics gameplay |

**Recommendation: A for Phase 1–2**, B as a Phase 3 visual if the explosion alone reads as
unsatisfying. C is out of proportion to the project's current maturity.

### D7 — Death explosion

The project has **no AoE of any kind** — projectiles are deliberately single-target by collider-ID
filter (W-11 in [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md), still an open design
question there). An explosion would be the first area query in the game.

| | A. No explosion damage (visual only) | B. Explosion damages nearby ships, chain depth capped at 1 | C. Full chaining |
|---|---|---|---|
| Cost | Zero | One `Physics.OverlapSphereNonAlloc` on the target layer + a filter; one flag on the sequence | Same code, no cap |
| Risk | None | Bounded: I-1 makes A→B→A impossible; the depth cap makes the total work finite | A dense fleet can cascade; with no attribution (D4) the log is unreadable |
| Friendly fire | n/a | Must be decided — and it contradicts W-11's "no friendly fire, ever" arcade stance | Same |

**Recommendation: A for Phase 1–2; B if you want it, as a Phase 3 opt-in** driven entirely by
config (`_explosionRadius = 0` ⇒ no query, so existing ships are unaffected). Note that B forces an
answer to W-11's open question, which is currently *"is single-target collision a design decision
or a shortcut?"*

### D8 — Where death config lives

| | A. Fields on `ShipHullData` | B. Fields on `ShipData` | C. New `ShipDeathData` SO, referenced from `ShipHullData` |
|---|---|---|---|
| Semantics | Death is about the physical hull — the same hull dying the same way across variants is right | Death per ship *variant* | Reusable across hulls; composable |
| Migration cost | 3 existing hull assets need values filled by hand | 5 existing ship assets | 3 hull assets + N new assets to create |
| Precedent | `ShipHullData` already owns prefab + movement | `ShipData` already owns stats + weapon | `WeaponData`/`ProjectileData` show the split-config pattern — and W-4 flags that split as a *problem* |

**Recommendation: A.** Fewest assets to touch, and it puts death next to the hull prefab it
belongs to. C only if two hulls must share one death profile, which is not true at 3 hulls.
Either way this is the manual-editor-step-heavy part: **every existing asset of the type needs the
new values** ([PROJECT_CONTEXT §5](Agents/PROJECT_CONTEXT.md)).

### D9 — Selection removal

| | A. Local `DeSelect()` only (Phase 1) | B. Also clear `ShipInput._currentObject` | C. Wait for `SelectionService` ([`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) P1) |
|---|---|---|---|
| Cost | One line | Needs a new seam on `ShipInput` (an `IControllable`-aware "this went away" notification) — new API on a class the input refactor plans to gut | Zero now |
| Correctness | Ring off, orders refused (with the `IsEnableToControl` check) — the residual state is invisible | Fully correct | Fully correct, later |

**Recommendation: A now, C later.** Building B means adding API to a class that
[`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) P1 already proposes replacing. When
`SelectionService` lands, `OnRemoved` is exactly the event it should subscribe to — that document's
P1 sketch already says "subscribes each selected ship's `OnDeath` → auto-remove".

### D10 — What happens when a team is eliminated

Nothing exists to receive this: no game-over UI, no state machine, no restart, no scoring, and
`ShipsManager.ResetTeams()` is empty.

| A. `Debug.Log` + the event, stop there | B. A minimal game-over hook on `GameController` | C. Full end-of-match flow |
|---|---|---|
| Honest; unblocks the plumbing; nothing pretends to be finished | A serialized `UnityEvent`/callback the human can wire to whatever exists later | Out of proportion — needs UI, restart, `ResetTeams` implemented, ship pooling (D1-C) |

**Recommendation: A.** The value of Phase 2 is that the *signal* exists and is correct; what
consumes it is a separate feature with its own design.

---

## 10. Phased implementation sketch

Effort scale: **S** ≈ under an hour, **M** ≈ half a day, **L** ≈ multi-session.
Every phase ends with a manual editor checklist and a manual test — there is no test suite
([PROJECT_CONTEXT §7](Agents/PROJECT_CONTEXT.md)).

### Phase 1 — the smallest thing that correctly kills a ship and cleans it up

**Goal:** a ship at 0 HP stops fighting, stops moving, stops being selectable, disappears, and
breaks nothing.

| Item | Files | Effort | Risk |
|---|---|---|---|
| Once-only guard | `ShipStatsController.cs` (`Death()`) | S | Very low |
| **W-2 fix — prerequisite** | `ShipWeaponsController.cs` (`Shoot`, `StopShooting`) | S | Low; changes live combat behaviour (for the better) |
| `IMovementController.Stop()` | `ShipMovementController.cs` | S | Low |
| `ShipDeathController` (states, sequence, routines, `Kill`/`Despawn`) | **new** `Assets/Scripts/Gameplay/Ship/ShipDeathController.cs` | M | Low — additive |
| Dead ships refuse orders | `ShipControlInterpreter.cs` (`IsEnableToControl`) | S | Low |

- **Manual editor steps this creates:** add `ShipDeathController` to `SmallShip.prefab`,
  `LargeShip.prefab`, `RocketShip.prefab`; wire `_hullCollider`, `_gfxRoot`, `_uiRoot` on each
  (9 wirings). No new assets, no pool rows.
- **Verifiable when done:** in `SampleScene`, order the player fleet onto `Team 2`'s LargeShip.
  When its bars empty: its guns stop, it stops moving, it can no longer be selected or ordered,
  and it disappears after the configured delay. The attacking ships stop firing and their turrets
  return to default. **No console errors** — specifically no NREs from rockets in flight at the
  moment of death (fire the RocketShip at it and kill it with the LargeShips simultaneously).
- **Deliberately not in Phase 1:** VFX, explosion, team bookkeeping, kill credit, pooling.

### Phase 2 — consequences and integration

**Goal:** the rest of the game learns about death.

| Item | Files | Effort | Risk |
|---|---|---|---|
| Team bookkeeping | `ShipTeamObserver.cs` (fill both stubs), `ShipsManager.cs` (`StartObserving`, `AllDies`, `IShipsManager`) | S–M | Low — the classes are empty today, nothing can regress |
| Team-eliminated signal → `GameController` | `GameController.cs` | S | Low ([D10](#d10--what-happens-when-a-team-is-eliminated)) |
| Projectile/visual target invalidation (unlocks `Destroy`/pooling later, and removes the grace-time hack) | `ProjectileWrapper.cs`, possibly `RocketsController.cs`, `RayVisualizer.cs` | M | **Medium** — touches the batched Jobs path, the project's most performance-sensitive code. Do it as its own change with its own manual test |
| Selection auto-removal | — (deferred to `SelectionService`, [D9](#d9--selection-removal)) | — | — |
| Debug `Kill()` entry point for testing | wherever debug tooling lands ([UNITY_RULES §9.4](Agents/UNITY_RULES.md)) | S | Low |

- **Verifiable when done:** kill every ship of `Team 2` → a single "team eliminated" signal fires
  once (log it). Kill a ship while 5+ rockets are in flight → they expire cleanly, no NRE, no
  damage applied to anything. Frame rate unchanged during a full fleet fight.

### Phase 3 — presentation and per-ship configuration

**Goal:** death looks like something, and designers can tune it per hull.

| Item | Files / assets | Effort | Risk |
|---|---|---|---|
| Death config | `ShipHullData.cs` (+3 assets to fill by hand) | S code / M data | Low, but **every existing hull asset needs values** |
| `ShipDeathVisual` + pooled explosion prefab | **new** `ShipDeathVisual.cs`; new VFX prefab (human); **new `PoolPair` row in `SampleScene`** | M | Low code, real editor work. First VFX pool in the project (C-2) |
| Explosion damage (optional, [D7](#d7--death-explosion)) | `ShipDeathController.cs` | M | **Medium** — first AoE query; forces an answer to W-11 |
| Audio | — | — | **Out of scope: no audio system exists anywhere in the project.** `OnDying` is the hook when one arrives |
| Ship pooling ([D1](#d1--removal-strategy) C) | `ShipStatsController`, `StatBar`, `ShipWeaponsController`, `ShipSpawner`, `ShipInitilizer`, all reset via the unused `IDeInitializable` | **L** | **High.** Only worth it when respawn/waves exist. See [§3.6](#36-facts-that-block-naive-reuse-pooling) for the full blocker list |

- **Verifiable when done:** each hull explodes with its own effect and timing; setting a hull's
  death duration to `0` makes it vanish instantly with no other change; clearing the VFX reference
  produces no errors and no gameplay difference (I-8).

### What is deliberately left out, and why

| Not designed | Why |
|---|---|
| Loot / salvage / resources | No economy exists in code or data |
| Crew / cargo / carried units / docking | No such concept anywhere ([`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §2.6) |
| AI target re-acquisition on death | **There is no AI.** Attackers already stop via `OnDeath` |
| Formations, control groups, order queues | None exist; [`INPUT_SYSTEM_RESEARCH.md`](INPUT_SYSTEM_RESEARCH.md) P1/P4 own that ground |
| Scoring, objectives, kill feed | Nothing consumes them; drives [D4](#d4--kill-attribution) |
| Save/persistence of death state | **No save system** ([PROJECT_CONTEXT §1](Agents/PROJECT_CONTEXT.md)) |
| A generic entity state machine | `QuequeStateMachine` is an unused sequential queue, not an FSM; one enum on one component is proportional |
| Respawn / waves | Would drive D1 to pooling; not requested and not scoped |

---

## 11. Open questions

Things only you can answer. None of them block Phase 1; all of them shape Phase 2–3.

1. **What should death *look* like?** Instant vaporize, a short explosion, or a drift-and-break-up
   over seconds? This sets `_dyingDuration` and decides whether [D6](#d6--wreckage--debris) B
   matters. Everything in §5.2 works for any of them, but the number is a feel decision.
2. **Are the `UndeadShip*` configs (5,000,000 HP, in no `TeamData`) meant to be genuinely
   unkillable test dummies?** If yes, that is a new "immortal" flag, not a big HP number, and it
   belongs in the death design. [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §9 Q2 asked
   whether to keep them at all; this is the follow-on.
3. **Are respawn / waves / match restart planned?** This is the single biggest input to
   [D1](#d1--removal-strategy). If yes, ship pooling stops being "someday" and Phase 3's L-effort
   item moves up — and `ShipsManager.ResetTeams()` finally gets a body.
4. **Should a destroyed ship block navigation as a wreck?** Related to [D6](#d6--wreckage--debris)
   and E-22. Today the answer is implicitly "no" because the GameObject is deactivated.
5. **Is friendly fire acceptable for death explosions?** Answering this also answers
   [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) W-11's standing question about
   single-target collision — the two decisions have to agree.
6. **Do Health and Shield die differently?** They are numerically identical twins today with equal
   `DamageOrder` (W-7), no regen, no resistances. If shields are meant to break visibly before the
   hull, that is a *pre-death* feedback system that would share `ShipDeathVisual`'s plumbing.
   [`SHIP_SYSTEM_RESEARCH.md`](SHIP_SYSTEM_RESEARCH.md) §9 Q5 asked the same thing and it is still
   unanswered.
7. **Should `PROJECT_CONTEXT.md` be corrected for C-1 (Unity 2022.3.62f2 / URP 14 / Input System
   1.14) in its own task?** The "code wins, fix the doc in the same change" rule applies, but this
   task is constrained to creating one document. It is a two-minute fix that stops the next agent
   from designing against a stale baseline.
8. **Does anything need to happen at "team eliminated" beyond a log?** [D10](#d10--what-happens-when-a-team-is-eliminated).
   If a game-over screen is coming, its owner should design the signal shape, not this document.
