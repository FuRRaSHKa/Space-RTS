# Projectile system — architecture, strengths, weaknesses and latent bugs

Research-only analysis of the projectile system in Space-RTS. No code, prefab, config or asset
was changed. All statements were verified against the working tree on **2026-08-09** (commit
`b93186d`, clean tree). Findings that could not be fully verified from code are marked
**Suspected** and state what a human would need to check in the editor.

Related prior research: [SHIP_SYSTEM_RESEARCH.md](SHIP_SYSTEM_RESEARCH.md) (death pipeline gap,
weapon factory hardcoding — both intersect this system and are cross-referenced, not re-litigated).

---

## 1. Summary

- A projectile is a **hybrid of three objects**: a pooled visual GameObject
  (`ProjectileObject` / `BulletObject` / `RocketObject`), a per-shot plain-C# simulation state
  object (`ProjectileWrapper` / `BulletWrapper` / `RocketWrapper`), and a central scene manager
  (`ProjectileController<T>` → `BulletsController`, `RocketsController`) that batch-simulates
  every live projectile in one `Update` using **Jobs + Burst raycasts**. This split is the
  system's core strength.
- **Tunnelling protection exists and is correct**: movement is applied as a per-frame segment,
  and a `RaycastCommand` covers exactly that segment (`moveDelta.magnitude`), so no speed or
  frame hitch can skip a collider.
- **A projectile can only ever hit its own designated target.** A Burst job filters raycast
  hits to the collider instance ID captured at fire time. No friendly fire, no self-hit, no
  blockers — and also no incidental hits: bullets phase through everything that is not their
  target, including ships standing in the line of fire.
- **Critical confirmed bug: `RocketShip` is immune to all bullets and rockets.** Its hull
  `MeshCollider` sits on layer 0 (Default), but both projectile controllers raycast only layer
  8 (`ShipHull`). Only ray weapons can damage it.
- **High latent bug**: the moment a death pipeline starts `Destroy()`ing ships (the known #1
  missing feature), `RocketWrapper.TargetPos` will throw every frame and freeze **every rocket
  in flight** — the exception aborts the shared controller `Update`.
- **High latent bug**: `ProjectileController` allocates its `NativeList`s in `Awake` but
  disposes them in `OnDisable`. Disable + re-enable the controller once and every subsequent
  `Update` operates on disposed native memory.
- Pooled-state reset is **essentially complete** — by construction: all mutable simulation
  state lives in the per-shot wrapper, which is never reused. The visual resets (trail
  `Clear()`, particle `Stop()`/`Play()`) cover the rest. The cost is one heap allocation +
  delegate subscription per shot.
- Time handling is uniform (`Update` + `Time.deltaTime` everywhere); `timeScale = 0` freezes
  movement, hits and lifetimes correctly. Simulation is **not deterministic** (frame-rate
  dependent integration) — replays/lockstep would require a fixed-tick refactor.
- Bullet-vs-rocket logic is duplicated across two controller subclasses and two near-identical
  spawner classes; a third projectile type would multiply that again.
- The agent docs have drifted from reality: the project is on **Unity 2022.3.62f2 / URP
  14.0.12** (`ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`), while
  `CLAUDE.md` and `Docs/Agents/PROJECT_CONTEXT.md` still say 2021.3.2f1 / URP 12.1.6. Not fixed
  here — this task is constrained to creating this document only.

---

## 2. Architecture overview

### 2.1 The three-object model

| Object | Kind | Lives | Responsibility |
|---|---|---|---|
| `ProjectileObject` (`BulletObject`, `RocketObject`) | MonoBehaviour on pooled prefab | reused via pool | visuals only: trail/particles/gfx reset on enable/disable, pool return |
| `ProjectileWrapper` (`BulletWrapper`, `RocketWrapper`) | plain C# class | one per shot, GC'd after death | all simulation state: lifetime, damage, target, velocity/homing params, hit resolution |
| `ProjectileController<T>` (`BulletsController`, `RocketsController`) | scene MonoBehaviour, `IService` | scene lifetime | batch movement + batched Jobs raycasts + lifetime bookkeeping for every live wrapper |

Key files:

- `Assets/Scripts/Gameplay/Weapon/Projectile/ProjectileObject.cs` (+ `BulletObject`, `RocketObject`)
- `Assets/Scripts/Gameplay/Weapon/ProjectileWrapper.cs` (all three wrappers + `RocketMovementStruct`)
- `Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs` (base `ProjectileController<T>`, `BulletsController`, `RaycastResultJob`)
- `Assets/Scripts/Gameplay/Weapon/Launcher/RocketsController.cs`
- `Assets/Scripts/Gameplay/Weapon/ProjectileRaycaster.cs` (static job scheduler)
- `Assets/Scripts/Management/BulletSpawner.cs` (`IProjectileCreator`, `BulletSpawner`, `RocketSpawner`)
- Data: `Assets/Scripts/SO/Weapon/Bullets/BulletData.cs` (`ProjectileData` base), `Assets/Scripts/SO/Weapon/Rockets/RocketData.cs`
- Firing side: `Assets/Scripts/Gameplay/Weapon/WeaponController.cs`,
  `Assets/Scripts/Gameplay/Weapon/Projectile/SequenceProjectileShooter.cs`,
  `Assets/Scripts/Gameplay/Weapon/Projectile/BurstProjectileShooter.cs`,
  `Assets/Scripts/Gameplay/Weapon/WeaponFactory.cs`
- Ray weapons bypass this system entirely: `Assets/Scripts/Gameplay/Weapon/Ray/RayShooter.cs`
  calls `ITargetable.DealDamage` directly, no projectile is ever spawned.

There is **no** per-type `if`/`switch` in shared code; type variation is handled by the
generic base class + subclass `ScheduleMoving()`. There is **no AoE, no piercing, no bounce,
no status effects** anywhere in the system.

### 2.2 Lifecycle diagram

```
SPAWN (WeaponController.Update → Shoot)
  WeaponController.Shoot()                          cooldown + angle + range gate
    └─ SequenceProjectileShooter.Shoot(target)
         └─ BulletSpawner.InstantiateProjectile()   (RocketSpawner for rockets)
              ├─ PoolManager.Instance[prefab].SpawnObject()   ← pooled GameObject
              ├─ transform.position/rotation = muzzle          (BulletSpawner.cs:32-33)
              ├─ new BulletWrapper(...)                        per-shot heap object
              │    └─ ctor: captures target.ColliderID,
              │             calls ProjectileObject.EnableObject()  (trail.Clear / particles.Play)
              ├─ BulletsController.AddProjectile(wrapper)
              └─ shooter subscribes wrapper.OnHit += BulletHit    (hit VFX)

MOVE + COLLIDE (ProjectileController.Update, every frame, Time.deltaTime)
  ClearData → CollectData → ScheduleMoving → Raycasts → UpdateLifeTime
    ├─ ScheduleMoving: per wrapper, build RaycastCommand(pos, moveDelta,
    │     moveDelta.magnitude, layerMask=ShipHull) — the exact segment moved this
    │     frame — then transform.position += moveDelta        (tunnel-proof)
    ├─ ProjectileRaycaster.Raycasts: RaycastCommand.ScheduleBatch (maxHits 1)
    │     + RaycastResultJob (Burst): keep hit only if
    │     hit.colliderInstanceID == wrapper.ColliderInstanceId  (own target only)
    └─ for each surviving hit: wrapper.ExecuteHit(point, normal)

RESOLVE (ProjectileWrapper.ExecuteHit, ProjectileWrapper.cs:40-45)
  OnHit(point, normal)          → shooter → ProjectileVisual hit particle
  target.DealDamage(damage)     → ShipStatsController.DealDamage
  Death(): _toReturn = true, ProjectileObject.DisableObject()

DESPAWN (two paths, both end in the pool)
  hit:      Death() as above
  lifetime: UpdateLifeTime() — _currentTime += Time.deltaTime, > _lifeTime → Death()
  then: controller removes wrapper from list (same frame), wrapper is garbage
  BulletObject:  DisableObject → PoolObject.ForceDisable → SetActive(false) immediately
  RocketObject:  DisableObject → gfx off, particles Stop, RoutineManager Wait(1f)
                 → base.DisableObject → ForceDisable   (1 s trail-linger grace)

REUSE (next SpawnObject on the same instance)
  PoolObject.Spawn → SetActive(true) → spawner repositions → wrapper ctor →
  EnableObject → BulletObject: _trail.Clear()  /  RocketObject: gfx on, particles Play
```

Not covered by any despawn path: owner death (fire-and-forget by design — projectiles from a
dead ship keep flying, which is fine), and scene teardown (`PoolManager.ReturnPools()` exists
but has **no callers**; there is only one scene, so today this path is dead code).

### 2.3 Scene/config wiring (verified)

- `BulletsController` and `RocketsController` sit in `SampleScene.unity`, both with
  `layerMask = 256` = layer 8 = `ShipHull` (`ProjectSettings/TagManager.asset`).
- `ProviderBuilder.RegisterServices()` (`Assets/Scripts/Management/ProviderBuilder.cs:39-57`)
  registers both controllers plus `BulletSpawner`/`RocketSpawner` keyed by **concrete type**;
  `WeaponFactory` resolves `GetService<BulletSpawner>()` / `GetService<RocketSpawner>()`.
- `Assets/Prefabs/Bullet/BulletPrefab.prefab`: `PoolObject` + `BulletObject` with `_poolObject`
  and `_trail` wired. `RocketPrefab.prefab`: `_poolObject`, `_gfx`, `_trails` wired.
- Hull colliders: `LargeShip.prefab` — layer 8 directly; `SmallShip.prefab` — layer 8 via a
  nested-prefab `m_Layer` override; `RocketShip.prefab` — **no override, layer 0** (see bug B1).

---

## 3. Strengths

Worth preserving through any refactor:

1. **Sim/visual separation.** All mutable projectile state lives in a per-shot wrapper; the
   pooled GameObject carries only visuals. This is why the pool-reuse bug class (stale timers,
   stale targets, stale velocity) is structurally absent here — the state object is never
   reused. (`ProjectileWrapper.cs`)
2. **Centralized batched simulation.** One `Update`, one `RaycastCommand.ScheduleBatch`, one
   Burst filter job for all projectiles of a type — instead of N `Update()`s and N
   `Physics.Raycast` calls. This is the right shape for an RTS with hundreds of projectiles.
   (`BulletsController.cs:35-45`, `ProjectileRaycaster.cs`)
3. **Correct tunnelling protection.** The raycast covers exactly the segment moved this frame
   (`BulletsController.cs:122-128`, `RocketsController.cs:16-21`); even a 1-second frame hitch
   cannot skip a target.
4. **Hit-once guarantee.** A hit wrapper is flagged `_toReturn` and removed from the
   controller list in the same `Update` (`BulletsController.cs:96-106`); one raycast per
   projectile per frame; no path re-enters `ExecuteHit`. Double damage is structurally
   impossible for a single projectile.
5. **Deliberate no-friendly-fire model.** Filtering hits by target collider instance ID
   (`RaycastResultJob`, `BulletsController.cs:133-144`) makes self-hit and ally-hit impossible
   without any team/layer bookkeeping. (Trade-off in W4.)
6. **Trail reset on reuse is handled** — `BulletObject.EnableObject → _trail.Clear()` runs
   after the spawner repositions the instance (`BulletSpawner.cs:32-35`), so there is no
   teleport streak; `RocketObject`'s 1-second delayed pool return lets its world-space smoke
   fade in place instead of being cut off.
7. **Data-driven tuning.** Speed/lifetime/homing parameters come from `ProjectileData` /
   `RocketData` ScriptableObjects; a new bullet variant is an asset, not code.

---

## 4. Weaknesses

| # | Weakness | Where | Cost | Severity |
|---|---|---|---|---|
| W1 | No death pipeline integration: wrappers hold a raw `ITargetable` and never check it. Works only because dead ships currently stay in the scene forever ("zombie ships", see ship research). Every future death implementation (Destroy or pooling) breaks projectiles in flight (bugs B2, B9). | `ProjectileWrapper.cs:19,43,92` | bug risk, blocks the #1 missing feature | **Critical** |
| W2 | `WeaponFactory.CreateWeapon` hardcodes `GetComponent<SequenceProjectileShooter>()` instead of the `IShooter` interface the prefab actually implements. `BurstProjectileShooter` can never be wired; the orphaned `Plasm` weapon prefab would NRE at creation. | `WeaponFactory.cs:30-39` | content speed — new shooter types need factory edits | **High** |
| W3 | Balance data for one gun is split across three serialized references: damage in `WeaponData`, speed/lifetime in the shooter's `_bulletData`, and a **second, independent** `_bulletData` on `AdvanceWeaponTargeter` used for lead calculation. Point them at different assets and aim is silently wrong. | `SequenceProjectileShooter.cs:16`, `AdvanceWeaponTargeter.cs:11` | silent aim errors, balance iteration friction | **High** |
| W4 | Only-hits-own-target is undocumented and double-edged: projectiles phase through blocking ships, corpses and terrain; a "missed" bullet can never hit anything else. Fine for an RTS, but it is a design decision living implicitly in one Burst job. | `BulletsController.cs:141` | design opacity; player-visible phasing | Medium |
| W5 | Bullet/rocket duplication: two controllers duplicate the command-build/move loop, two spawners duplicate pool-fetch/position/wrap/register verbatim. A third projectile type (beam? flak? torpedo) copies it again. | `BulletsController.cs:116-130` vs `RocketsController.cs:9-23`; `BulletSpawner.cs:25-39` vs `:51-67` | maintainability | Medium |
| W6 | Silent early-returns hide misconfiguration: `InstantiateProjectile` returns `null` on a data-type mismatch instead of erroring loudly — and the callers immediately dereference it (bug B4). | `BulletSpawner.cs:28-29,54-55` | confusing NRE far from root cause | Medium |
| W7 | `ProjectileController` container lifecycle is asymmetric: allocate in `Awake`, dispose in `OnDisable` (bug B3). | `BulletsController.cs:23-28,108-111` | latent crash | Medium |
| W8 | Per-shot GC pressure: wrapper `new`, `OnHit` delegate subscription, `GetComponent<ProjectileObject>()`, LINQ in `ObjectPool.SpawnObject`, LINQ + `ToList` in `ShipStatsController.DealDamage` per hit. | `BulletSpawner.cs:35`, `ObjectPool.cs:34-36`, `ShipStatsController.cs:77` | GC spikes at scale | Medium |
| W9 | `RaycastCommand(pos, dir, dist, mask)` legacy constructor is obsolete on the actual engine version (2022.3); it still works but warns and pins legacy trigger semantics. | `BulletsController.cs:125`, `RocketsController.cs:18` | tech debt | Low |
| W10 | Rocket rotation uses last frame's `currentRotationSpeed` (the freshly computed value is only stored for next frame), so turn rate is one frame behind and frame 1 has turn rate 0. Reads like an accident but behaves like spin-up. | `RocketsController.cs:30-31` | subtle frame-rate coupling | Low |
| W11 | `ShipWeaponsController.Shoot` leaks `OnDeath += StopShooting` subscriptions on retarget — kill the old target and the ship stops shooting the new one. Weapons-side, already documented in ship research; listed here because the symptom looks like a projectile bug. | `ShipWeaponsController.cs:59` | player-visible combat bug | High (owned by ship system) |

Stylistic only (not defects): `deltaTime` assigned twice per frame (`BulletsController.cs:56,119`);
`RocketWrapper._direction` is written but never meaningfully read; `BulletObject._trail` and
`ProjectileObject._poolObject` have no null-guards (both are prefab-wired today).

---

## 5. Latent bugs

Confidence: **Confirmed** = the defect is provable from code/assets alone. **Likely** = code
path is real but needs one runtime condition to fire. **Suspected** = plausible, needs an
editor test.

| # | Issue | Trigger conditions | Location | Player-visible symptom | Confidence | Severity |
|---|---|---|---|---|---|---|
| B1 | **`RocketShip` cannot be damaged by bullets or rockets.** Its hull `MeshCollider` GameObject is on layer 0 (Default): the nested `TempRocketShip.fbx` instance has **no** `m_Layer: 8` override, unlike `SmallShip` (override present) and `LargeShip` (layer 8 directly). Both controllers raycast `layerMask = 256` (ShipHull only), and the raycast can never return the collider whose instance ID would pass the filter. | Any bullet/rocket weapon fires at a `RocketShip` | `Assets/Prefabs/Ships/RocketShip.prefab` (no `propertyPath: m_Layer` override) vs `SampleScene.unity` controller `m_Bits: 256` | Projectiles fly through the ship forever; only `RayGun` ships can kill it | **Confirmed** (asset-level; 2-min play-mode check recommended) | **Critical** |
| B2 | **All rockets freeze when any rocket's target is destroyed.** `RocketWrapper.TargetPos => target.TargetTransform.position` throws `MissingReferenceException` once a ship is `Destroy()`ed; the exception escapes `RocketsController.ScheduleMoving` inside the shared `Update`, aborting simulation for **every** rocket, every frame. Unreachable today only because nothing destroys ships. | A death pipeline lands and destroys ship GameObjects while ≥1 rocket is homing | `ProjectileWrapper.cs:92`; loop at `RocketsController.cs:13-22` | Every in-flight rocket hangs motionless in space; console error spam | Likely (certain once death exists) | **High** |
| B3 | **Disposed native containers after re-enable.** `Awake` allocates `colliderIDs`/`raycastCommands`/`results` once; `OnDisable` disposes them. Disable + re-enable the controller (or its GameObject) → every `Update` touches disposed `NativeList`s → `ObjectDisposedException`, projectile simulation dead for the rest of the session. | Anything toggles the controller component/GameObject once | `BulletsController.cs:23-28` vs `:108-111` | All shooting silently stops; console exception spam | Confirmed (code); requires the toggle to occur | **High** |
| B4 | **NRE on data/spawner mismatch.** `InstantiateProjectile` returns `null` when `ProjectileData` is not the expected subtype (e.g. a `RocketData` asset assigned to a shooter wired with `BulletSpawner`, which `WeaponFactory` chooses from the *separate* `WeaponData.WeaponType` enum). Both shooters immediately do `.OnHit +=` on the return value. | `WeaponData.WeaponType` disagrees with the shooter's `_bulletData` asset subtype — one dropdown + one drag in the editor | `BulletSpawner.cs:28-29`; `SequenceProjectileShooter.cs:43-44`, `BurstProjectileShooter.cs:44-45` | Ship throws NRE every shot attempt, never fires | Confirmed (code path); needs the misconfig to exist | Medium |
| B5 | **Double `Death()` in one frame.** A projectile that hits on the same frame its lifetime expires runs `Death()` twice: `UpdateLifeTime()` is evaluated before the `\|\| ToReturn` short-circuit and increments/checks time even for already-hit wrappers. `ForceDisable` runs twice → `OnPoolReturn`/`SetParent` twice; `RocketObject` restarts its 1 s routine. No damage is re-applied (`ExecuteHit` is raycast-driven, once). | Hit frame == lifetime-expiry frame | `BulletsController.cs:100`; `ProjectileWrapper.cs:47-57,59-63` | None today (benign redundancy) — but any future logic added to `Death()`/`DisableObject` runs twice | Confirmed (code) | Low |
| B6 | **Point-blank shots may pass through the target.** The segment raycast starts at the muzzle; if the muzzle is inside the target's convex hull collider (big ship overlapping a small attacker at zero range), the ray starts inside the collider and Unity reports no hit; the bullet exits the far side and lives out its lifetime. | Muzzle position inside the target's hull collider when firing | `BulletsController.cs:122-128`; hulls are convex `MeshCollider`s (e.g. `SmallShip.prefab`) | Occasional "immune at point-blank" reports | Suspected — needs a play-mode test with two overlapping ships | Medium |
| B7 | **Collider instance ID reuse re-targets in-flight projectiles.** The wrapper stores only an `int` collider ID. If ships are ever pooled/reused (stated project direction for spawned things), a recycled ship keeps the same collider instance → projectiles fired at the dead owner hit the respawned ship, regardless of team. | Ship pooling/reuse lands; projectile outlives the target's first life | `ProjectileWrapper.cs:35`; `ShipTarget.cs:22` | "New" ship takes phantom damage from old bullets | Likely (future) | Medium |
| B8 | **`ReturnAllToPools` during flight would double-own pooled instances.** `PoolManager.ReturnPools()` → `ObjectPool.ReturnAllToPools()` disables every pooled object, but live wrappers stay in the controller lists and keep moving the now-pool-owned transforms; on reuse, two wrappers drive one transform. Today the method has **zero callers** — dead code, but it is the documented scene-change hook. | Anything starts calling `ReturnPools` (scene reload feature) while projectiles fly | `PoolManager.cs:36-43`, `ObjectPool.cs:54-60`; lists in `BulletsController.cs:15` | Teleporting/jittering projectiles after scene change | Likely (future) | Medium |
| B9 | **Zombie-target damage sinks.** Because dead ships persist (no death pipeline), projectiles happily keep hitting and "damaging" dead hulls (`ExecuteHit` → `DealDamage` on a dead `ShipStatsController` drives stats further negative; second `Death()` is a no-op). Ammunition and DPS are wasted on corpses; `WeaponController` keeps firing at them since only `ShipWeaponsController.StopShooting` reacts to death — once — via the leaky subscription (W11). | Any ship reaches 0 HP | `ProjectileWrapper.cs:43`; `ShipStatsController.cs:75-103` | Ships pour fire into dead hulls | Confirmed (design gap, shared with ship system) | Medium |
| B10 | **Hit VFX teleports/restarts on rapid hits.** Each weapon owns exactly one `_hitSystem`; consecutive hits reposition and `Play()` the same particle system, cutting the previous burst. | ≥2 hits from one weapon within one particle lifetime | `ProjectileVisual.cs:25-30` | Impact flashes pop/teleport under sustained fire | Confirmed (code) | Low |
| B11 | **Rocket smoke can straddle a reuse.** `RocketObject` waits only 1 s before pool return; world-space smoke particles with lifetime > 1 s are still alive when the instance is reused and teleported (old smoke hangs where the rocket died — acceptable — but `Play()` on reuse continues the same systems). | Smoke particle lifetime > 1 s (check `RocketPrefab` particle settings in editor) | `RocketObject.cs:13-26` | Brief smoke ghost at the old death site on reuse | Suspected — needs a particle-lifetime check in the editor | Low |
| B12 | **Aim-lead math divides by ~zero.** `AdvanceWeaponTargeter.CalculatePos` guards the singular case with exact float equality (`velocity.magnitude == _bulletData.Speed`) then divides by `2*(targetV² − bulletV²)`; near-equal speeds produce huge/NaN lead times, and `Mathf.Max(firstTime, secondTime)` picks the *larger* root where the earlier intercept is usually wanted. Turret aim only — projectiles themselves are unaffected. | Target speed ≈ bullet speed (asset tuning) | `AdvanceWeaponTargeter.cs:66,85-88` | Turret aims at wild positions for fast targets | Suspected (math reading; needs a tuning test) | Low |

**Checked and found sound** (explicitly, per the task's checklist): tunnelling (protected, §3.3);
pooled-state reset (complete — see §1 and below); lifetime timers (fresh wrapper per shot,
correct delta); `timeScale == 0` (all movement, hits and timers freeze; only the rocket's 1 s
pool-return `WaitForSeconds` is deferred, harmless); double damage same-frame (impossible);
hit-before-init (wrapper is fully constructed before `AddProjectile`; controller cannot see it
earlier); float accumulation (worst case is `_currentTime` on a seconds-scale lifetime —
negligible); spawn/first-move ordering (whether the controller `Update` runs the spawn frame or
the next, the raycast still covers every moved segment; no gap).

**Pooled-state reset audit** (every field that survives reuse): `BulletObject` — trail cleared ✓,
position/rotation re-set by spawner ✓; `RocketObject` — gfx re-enabled ✓, particles
re-`Play()`ed ✓, `_stopable` holds a spent routine (stopped again before reuse via
`DisableObject`, harmless) ✓; `PoolObject` — `_enableToSpawn`/`_disableObjectRoutine` reset in
`Spawn()` ✓. Scale and parent are never mutated. No gameplay field survives because the wrapper
is per-shot. Gaps found: only the cosmetic B11.

---

## 6. Performance notes

Per frame, per controller (`BulletsController.cs:35-45`):

- **Good**: one `ScheduleBatch` + one Burst filter job for all projectiles; `AddNoResize` into
  pre-grown `NativeList`s; no LINQ, no closures, no `GetComponent` in the hot loop; list
  compaction is in-place `RemoveAt` with index rewind.
- **Native allocation per frame**: `filtered` (`Allocator.TempJob`) is created/disposed every
  `Update` even when nothing is hit (`BulletsController.cs:83-93`). Cheap but avoidable.
- **Main-thread transform writes**: `Transform.position +=` per projectile per frame is the
  scaling wall — the raycasts are jobified but movement is not. Rough expectation: 100
  projectiles trivial; 500 fine; ~2000 the transform loop plus managed-per-shot costs (below)
  dominate the frame, and `RemoveAt` compaction becomes O(n²) on mass expiry. Target is 60 FPS
  desktop (`PROJECT_CONTEXT.md` §1).
- **Per shot (managed)**: wrapper allocation, `OnHit` delegate, `GetComponent<ProjectileObject>()`
  (`BulletSpawner.cs:35`), LINQ enumerator in `ObjectPool.SpawnObject` + O(pool) scan
  (`ObjectPool.cs:34-36`). Per hit: LINQ `Where/OrderBy/ToList/Sum` in
  `ShipStatsController.DealDamage` (`ShipStatsController.cs:77-92`). At RTS fire rates this is
  steady GC feed — the eventual spike is the symptom to watch on the scene's FPS counter.
- **Blocking sync point**: `Raycasts` completes both jobs immediately after scheduling
  (`ProjectileRaycaster.cs:22-23`), so the main thread stalls for the batch; at high counts the
  standard pattern (schedule early, complete late in `LateUpdate`) would reclaim that time.

## 7. Determinism

Not deterministic, by three independent causes: variable `Time.deltaTime` integration
(`position += dir * v * dt`), rotation ramp integrated per-frame (`RocketsController.cs:30-36`),
and order-dependent float accumulation on transforms. There is **no RNG anywhere** in the
projectile path (no spread, no damage rolls), which is the one thing working in determinism's
favor. Replays or lockstep networking would require moving the simulation to a fixed tick with
positions owned by the wrappers (not `Transform`), plus deferring raycasts to the tick — a
structural change, not a patch. If those features are not on the roadmap, this is a non-issue.

---

## 8. Recommendations (research only — nothing implemented)

### Fix now (confirmed, minimal safe fix)

1. **B1 — RocketShip immunity.** Human editor step, no code: open `RocketShip.prefab`, set the
   hull-collider GameObject (the `TempRocketShip` child holding the `MeshCollider` that
   `ShipTarget._hullCollider` references) to layer `ShipHull`. Verify: play `SampleScene`,
   order a bullet ship to attack a RocketShip, HP must drop.
2. **B3 — disposed containers.** Move allocation to `OnEnable` (guarded) or dispose in
   `OnDestroy` instead of `OnDisable` — either restores symmetry; `OnDestroy` is the smaller
   diff.
3. **B4 — null deref on misconfig.** In both shooters, null-check the `InstantiateProjectile`
   result before `.OnHit +=` and `Debug.LogError` the weapon name — turns a per-shot NRE into
   one actionable message. (`Debug.LogError` in the spawner's type-check branch works too.)
4. **W11/B9 adjunct — the retarget subscription leak** is a one-line unsubscribe in
   `ShipWeaponsController` (already specified in the ship research doc).

### Harden (defensive invariants worth adding)

- **B2 pre-emption**: before any death pipeline lands, decide the contract for "target became
  invalid mid-flight" — options side by side:
  - (a) wrapper checks target aliveness each frame (`IDeathHandler.IsDead` snapshot at spawn) —
    simplest, one branch per projectile per frame; rockets need a "last known position" to fly to;
  - (b) death event unsubscription: controller listens for target death and clears/retargets
    affected wrappers — no per-frame cost, but adds subscription bookkeeping exactly where W11
    already went wrong once;
  - (c) targets are never destroyed, only flagged + collider disabled (pairs with pooled
    ships) — zero projectile changes, but institutionalizes zombie objects and walks into B7.
  (a) is the smallest and also neutralizes B9's endless corpse-fire if the shooter side checks
  the same flag; (c) requires solving B7's ID reuse (e.g. a generation counter next to the
  collider ID) either way.
- Cache `WaitForSeconds`/particle lifetimes aside, `RocketObject`'s hardcoded `1f` linger should
  derive from the actual particle max lifetime (kills B11 by construction).
- Assert-in-editor (`#if UNITY_EDITOR`) that a shooter's `_bulletData` subtype matches the
  spawner the factory chose, and that `AdvanceWeaponTargeter._bulletData` equals the shooter's
  (W3) — misconfig becomes a load-time error instead of silent bad aim.

### Refactor (each: problem / approach / migration / blast radius / effort / trade-offs)

- **Unify the spawners** (W5). Problem: `BulletSpawner`/`RocketSpawner` differ only in the cast
  and the wrapper ctor. Approach: one generic creator parameterized by a wrapper factory, or
  move wrapper construction onto `ProjectileData` subclasses (data knows its wrapper).
  Migration: introduce alongside, switch `WeaponFactory`, delete old. Blast radius:
  `ProviderBuilder`, `WeaponFactory`, both spawners. Effort **S**. Trade-off: SO-creates-runtime-
  object couples data assets to gameplay classes — some teams dislike that.
- **Factory wires `IShooter`, not a concrete class** (W2). Problem: `Plasm`/`BurstProjectileShooter`
  unusable. Approach: `weapon.GetComponent<IShooter>()` + an `InitProjectileCreator` member on
  `IShooter` (both shooters already implement it). Migration: interface change + two call sites.
  Blast radius: `WeaponFactory`, `IShooter`, `RayShooter` (needs a no-op impl). Effort **S**.
  Trade-off: `RayShooter` gains a meaningless method — or split a sub-interface, slightly more code.
- **Wrapper pooling / struct wrappers** (W8). Problem: per-shot GC. Approach: pool wrappers with
  an explicit `Reset(...)`, or convert to structs in a `NativeList` with indices as handles.
  Migration: incremental for pooling; the struct route rewrites the controllers. Blast radius:
  wrappers + controllers + spawners. Effort **M** (pooling) / **L** (structs). Trade-off:
  reintroduces exactly the stale-state bug class the current design structurally avoids —
  **not worth it until profiling shows the GC spike is real**.
- **Jobify movement / defer completion** (§6). Problem: main-thread transform loop + immediate
  `Complete()`. Approach: `TransformAccessArray` + `IJobParallelForTransform`; complete raycasts
  in `LateUpdate`. Blast radius: controller base class only. Effort **M**. Trade-off: hit
  resolution moves one frame later or into `LateUpdate` — ordering with weapon `Update`s must be
  re-checked.

### Leave alone

- **The instance-ID hit filter (W4)** — it is unusual but it is also the game's combat model
  (matches the recorded design intent that shooting never auto-retargets); document it in
  `PROJECT_CONTEXT.md` rather than "fixing" it into a general collision system nobody asked for.
- **W10 one-frame rotation lag** — behaves like missile spin-up; fixing it changes rocket feel
  for zero player-visible benefit.
- **B5 double-`Death()`** — harmless today; naturally disappears if `Death()` ever gets an
  idempotence guard during the B2 work. Not worth its own change.
- **`RocketWrapper._direction` dead-ish field, duplicated `deltaTime` assignment** — cosmetic;
  drive-by cleanup is out of scope by house rules.

---

## 9. Open questions

1. **Is RocketShip's layer wrong or is the mask wrong?** B1 has two valid fixes (hull → layer 8,
   or widen the controllers' mask). Layer 8 matches the other two ships, so the prefab fix looks
   intended — but only a human knows whether `ShipHull` was meant to exclude rocket ships for a
   reason.
2. **Roadmap: replays/networking ever?** Determines whether §7's fixed-tick refactor is "later"
   or "never".
3. **Ship death: Destroy or pool?** The answer picks between hardening options (a)/(b)/(c) and
   decides B7's priority.
4. **`RocketPrefab` smoke particle lifetime** (B11) and the **point-blank overlap case** (B6)
   need 5 minutes of editor time each — both are stated as Suspected until then.
5. **Doc drift**: `CLAUDE.md` + `PROJECT_CONTEXT.md` say Unity 2021.3.2f1 / URP 12.1.6 / Input
   System 1.3.0; the tree says 2022.3.62f2 / URP 14.0.12 / Input System 1.14.0. Per
   [WORKFLOW.md](Agents/WORKFLOW.md) the doc should be fixed with the next code change — this
   task was constrained to creating only this file.
