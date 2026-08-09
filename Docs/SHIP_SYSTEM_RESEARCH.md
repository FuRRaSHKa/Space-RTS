# Ship system — completeness, architecture and quality audit

Research-only analysis of the ship system in Space-RTS. No code, prefab, config or asset was
changed. All statements were verified against the **working tree on 2026-08-09** — that is
commit `9d6ddeb` **plus the uncommitted, in-progress naming cleanup** (`Initilize` →
`Initialize` etc.) that touches every `.cs` file. The cleanup was verified to be rename-only
(121 insertions / 113 deletions across 32 files, all identifier spelling). Anything that could
not be verified is in [§9 Open questions](#9-open-questions).

---

## 1. Summary

- A "ship" is a **prefab of ~9 small components glued by interfaces**, configured entirely from
  ScriptableObjects (`ShipData` → hull + weapon + stats). There are **zero ship subclasses**; a
  new ship variant needs assets only, no code. This composition-over-inheritance core is the
  system's biggest strength and worth preserving through any refactor.
- **5 ship configs exist, 3 are reachable in game** (`SmallShip`, `LargeShip`, `RocketShip`).
  The two `UndeadShip*` configs (5,000,000 HP test dummies) are in no team — orphaned.
- **No ship is "Done".** All three playable ships are blocked by the same systemic gap:
  **there is no death pipeline.** A ship at 0 HP fires an event, stays in the scene forever,
  remains selectable and movable, and its own guns keep firing ("zombie ship"). This is a
  missing system, not missing content.
- **There is no AI at all.** Enemy ships never acquire targets and never shoot; combat happens
  only when the player right-clicks. Win/lose tracking (`ShipTeamObserver`, `ShipsManager`) is
  stubbed empty.
- **There is no audio code anywhere in the project**, and no ship UI beyond the two overhead
  stat bars (`StatBar` works; `ShipUiStats` is an empty stub on no prefab).
- The weapon layer is the most mature part: three weapon types (ray / projectile / rocket),
  batched Jobs+Burst projectile simulation with pooling, and a proper ballistic-lead targeter
  (`AdvanceWeaponTargeter`). One weapon prefab (`Plasm`) is orphaned **and** would crash if
  wired, because `WeaponFactory` hardcodes `GetComponent<SequenceProjectileShooter>()`.
- One live bug found in shared code: retargeting leaks a death subscription in
  `ShipWeaponsController.Shoot()` — kill the *old* target and the ship silently stops shooting
  the *new* one.
- Weapon balance data is **split across three places** (damage in `WeaponData`, projectile
  speed in `ProjectileData` referenced twice per gun prefab, mount count in the ship prefab) —
  balance iteration requires touching assets and prefabs together.
- Adding a new ship that reuses existing weapon types is cheap (2 assets + 1 prefab + 1 team
  row, no code). Adding a new weapon *type* is expensive: enum append + `WeaponFactory` edit +
  new shooter/controller/service + scene and pool wiring (~6 files).
- The docs have drifted: `Docs/Agents/PROJECT_CONTEXT.md` §6 still declares the old typo
  spellings API (the working tree renamed them) and §7 references an `FPS.cs` counter that is
  not in the scene. Not fixed here — this task is constrained to creating this document only.

---

## 2. Architecture overview

### 2.1 What a ship technically is

A ship is a **prefab** (`Assets/Prefabs/Ships/*.prefab`) whose root carries nine
MonoBehaviours, plus a nested `ShipCanvas` prefab for stat bars. Identity and numbers come
from ScriptableObjects; the prefab supplies structure (mount points, colliders, GFX). There is
no ECS, no per-ship class, no scene-placed ships — everything is spawned at runtime by
`ShipSpawner` from `TeamData` lists.

### 2.2 Assembly diagram

```
GameController._teams (scene)                     ── data ──────────────────────────────
  └─ TeamData ("Team Player", "Team 2")             SideData (Player/Enemy)
       ├─ SideData                                  ShipData
       └─ List<ShipData>  ──────────────────────►     ├─ ShipHullData ── ShipSize
                                                      │    └─ _hullPrefab ─┐ (the ship prefab)
GameInitiliazer.Initialize()                          ├─ WeaponData        │
  └─ new ShipsManager(IShipsFactory)                  │    └─ _weaponPrefab│ (gun prefab)
       └─ ShipSpawner.CreateShips()                   └─ List<StatStruct>  │
            │  Instantiate(hullPrefab) ◄──────────────────────────────────┘
            │  GetComponent<ShipInitilizer>()
            │  .InitServices(IWeaponFactory)        ── ship prefab root ────────────────
            │  .Initialize(ShipInitializationData)    ShipEntity            (facade)
            │        │                                ShipInitilizer        (broadcaster)
            │        ▼ broadcast to children          ShipMovementController + NavMeshAgent
            │  IInitializable<ShipInitializationData> ShipStatsController   (HP/shield, death)
            │  found via GetComponentsInChildren:     ShipWeaponsController (gun mounts)
            │    ShipEntity, ShipStatsController,     ShipTarget + hull Collider
            │    ShipMovementController,              ShipControlInterpreter(orders)
            │    ShipWeaponsController, StatBar ×2    SelectGFXController   (selection ring)
            │                                         ShipModelRotator      (banking visual)
            └─ ShipWeaponsController.Init():          └─ ShipCanvas (nested): StatBar ×2
                 foreach _gunPositions[i]:
                   WeaponFactory.CreateWeapon(WeaponData, mount)
                     │ Instantiate(weaponData.Prefab) under mount
                     │ WeaponInitilizer.Initialize(WeaponData) broadcast:
                     │   WeaponController (fire timing) + IWeaponTargeter (aiming)
                     │   + IShooter (RayShooter | SequenceProjectileShooter | Burst…)
                     └ if Projectile/Rocket: inject BulletSpawner/RocketSpawner
                          └─ PoolManager[ProjectileData.ProjectilePrefab].SpawnObject()
                               └─ BulletsController / RocketsController (batched Jobs update)
```

### 2.3 Core classes and responsibilities

| Class | File | Responsibility |
|---|---|---|
| `ShipEntity` | `Assets/Scripts/Gameplay/Ship/ShipEntity.cs` | Facade: caches sibling interfaces (`IMovementController`, `IStatsController`, `IWeaponController`, `ITargetable`, `IDeathHandler`) in `Awake`, holds `SideData`/`ShipData` |
| `ShipInitilizer` | `Assets/Scripts/Gameplay/Ship/ShipInitilizer.cs` | `AbstractInitilizer<ShipInitializationData>` — broadcasts `Init(data)` to all `IInitializable` children; also forwards `IWeaponFactory` |
| `ShipStatsController` | `Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs` | Builds `Stat` dictionary from `ShipData.StatDatas`, damage overflow logic, raises `OnDeath` |
| `ShipMovementController` | `Assets/Scripts/Gameplay/Control/Easy/ShipMovementController.cs` | Thin `NavMeshAgent` wrapper; speeds come from `ShipHullData` |
| `ShipWeaponsController` | `Assets/Scripts/Gameplay/Weapon/ShipWeaponsController.cs` | Spawns one gun per `_gunPositions` mount via `WeaponFactory`; fans out `Shoot`/`StopShooting` |
| `ShipTarget` | `Assets/Scripts/Gameplay/Ship/ShipTarget.cs` | `ITargetable` implementation: collider ID for projectile filtering, damage entry point, velocity/death observer for lead calculation |
| `ShipControlInterpreter` | `Assets/Scripts/Gameplay/Ship/ShipControlInterpreter.cs` | Translates player orders (`Target`, `TargetPosition`) into movement/weapon calls; side check |
| `WeaponController` | `Assets/Scripts/Gameplay/Weapon/WeaponController.cs` | Per-gun fire loop: cooldown, angle deviation gate, range gate |
| `ProjectilelController<T>` | `Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs` | Batched per-frame simulation of all live projectiles: movement + `RaycastCommand` batch + Burst filter job |
| `ShipSpawner` | `Assets/Scripts/Management/ShipSpawner.cs` | `IShipsFactory`: instantiates hull prefabs, runs the init chain, places ships in spawn zones |
| `ShipsManager` / `ShipTeamObserver` | `Assets/Scripts/Management/ShipsManager.cs`, `ShipTeamObserver.cs` | Team bookkeeping — **largely stubs** (see §6) |

### 2.4 Inheritance vs composition

Inheritance is used only where it earns its keep, and stays one level deep:

- `AbstractInitilizer<TData>` → `ShipInitilizer`, `WeaponInitilizer`
- `ProjectilelController<TProjectile>` → `BulletsController`, `RocketsController`
- `ProjectileData` → `BulletData`, `RocketData`; `ProjectileObject` → `BulletObject`, `RocketObject`
- `ProjectileWrapper` → `BulletWrapper`, `RocketWrapper`

Ships themselves have **no hierarchy at all** — differences between ships are 100% data +
prefab structure. Nothing in the shared systems branches on a ship name or ID; there are no
hardcoded ship strings anywhere in `Assets/Scripts`. Weapon-type branching exists in exactly
one place (`WeaponFactory.CreateWeapon`, see §6 W-3).

### 2.5 Data and configuration

Single source of truth per concern, but **split across four asset types plus two prefabs**:

| Value | Lives in | Notes |
|---|---|---|
| Hull prefab ref, move/turn/accel speeds, size tag | `ShipHullData` (`Assets/Data/Ships/ShipHulls/*.asset`) | applied to `NavMeshAgent` at init |
| Weapon choice, damage, fire rate, range, turret turn speed, max deviation | `WeaponData` (`Assets/Data/Weapons/*.asset`) | one weapon type per ship (all mounts identical) |
| Stat pools (Health/Shield start values, damage order) | `ShipData._statsDatas` | `StatData` assets act as enum keys |
| Projectile speed, lifetime, prefab; rocket steering | `BulletData`/`RocketData` (`Assets/Data/Bullets/*.asset`) | referenced **from the gun prefab**, not from `WeaponData` |
| Gun mount count/placement | ship prefab `ShipWeaponsController._gunPositions` | LargeShip 6, RocketShip 4, SmallShip 1 |
| Muzzle count per gun | gun prefab `_spawnPoints` | ProjGun2: 2, RocketGun: 1 |

Hardcoded in code (magic numbers): spawn scatter radius `4` in `ShipSpawner.GetSpawnPos`,
turret elevation clamp `(-.1f, .7f)` duplicated in both `SimpleWeaponTargeter.Rotate` and
`AdvanceWeaponTargeter.Rotate`, raycast distances `30` in `ObjectClicker.Update`, rocket
despawn grace `1f` in `RocketObject.DisableObject`.

**Variants/tiers/upgrades:** expressed only by duplicating `ShipData` assets
(`UndeadShipLarge` = `LargeShip` with 5M HP). There is no upgrade mechanism; `Stat` clamps to
its constructor max, so even runtime buffs to max HP are impossible today.

### 2.6 Subsystem coupling map

| Subsystem | Attachment | Coupling assessment |
|---|---|---|
| Movement | `ShipMovementController` + `NavMeshAgent` on root | Clean behind `IMovementController`; NavMesh is an implementation detail except `ShipModelRotator` also serializes a `NavMeshAgent` field (unused in code — see §4) |
| Weapons | Gun prefabs instantiated under mount transforms at runtime | Clean behind `IWeapon`/`IShooter`/`IWeaponTargeter`, except the `WeaponFactory` concrete-type lookup (§6 W-3) |
| Health/damage | `ShipStatsController` + `ShipTarget` | Clean; generic stat dictionary. Damage order data-driven but currently ambiguous (§4) |
| Destruction | **absent** | `OnDeath` event exists; no subscriber removes the ship (§6 W-1) |
| Abilities/modules | **absent** | no concept in code or data |
| Crew/cargo | **absent** | no concept in code or data |
| Visuals | `ShipModelRotator` (banking), `SelectGFXController` (ring), gun-level `ProjectileVisual`/`RayVisualizer` | Decoupled via events (`IShooter.OnShooting`/`OnDealDamage`) — good pattern. No damage states, no death VFX |
| Audio | **absent** | zero audio code in `Assets/Scripts` |
| UI | Nested `ShipCanvas` prefab, 2× `StatBar` driven by `OnStatChange` | Works; `ShipUiStats` is an unused empty stub. No selection panel/tooltips/icons |
| AI | **absent** | enemy ships idle forever; no target acquisition for either side |

### 2.7 Lifecycle

- **Spawn:** `Instantiate` per ship (not pooled — acceptable at 8 ships, see §6 W-12).
  Init order is correct and explicit: `InitServices` (weapon factory) before
  `Initialize(data)`, and `ShipEntity.Awake` caches siblings before any `Init` runs.
- **Death:** `ShipStatsController.Death()` sets `_isDead`, invokes `OnDeath`, then nulls the
  event. Attackers' `ShipWeaponsController` stop shooting (they subscribed). **Nothing else
  happens** — no despawn, no unregistration, no VFX, and the dead ship's own weapons are never
  stopped.
- **Cleanup:** ships are never destroyed, so today's missing unsubscriptions (`StatBar`'s
  lambda, `ShipWeaponsController`'s target subscription, `RayVisualizer`'s `OnShooting`) don't
  leak *yet* — they become real leaks the day ships despawn or pool.
- **Projectiles:** properly pooled (`PoolManager` rows: BulletPrefab ×20, RocketPrefab ×5 in
  `SampleScene`) and reset in `EnableObject`/`DisableObject` (`BulletObject` clears its trail;
  `RocketObject` stops its routine correctly).
- **Persistence:** none, by design (documented in `PROJECT_CONTEXT.md`).

---

## 3. Ship completeness table

Weapons column = the weapon the config references resolves end-to-end (asset → gun prefab →
shooter → pool). "Systemic" = blocked by a missing system shared by all ships (death pipeline,
AI, audio), not by this ship's own content.

| Ship | Config | Prefab | Model/VFX | Weapons | Abilities | AI | UI/Icon | Audio | Balance values | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| SmallShip | `Assets/Data/Ships/Ships/SmallShip.asset` | `Assets/Prefabs/Ships/SmallShip.prefab` (1 mount) | hull + banking + muzzle/ray FX | RayGun via `DefaultWeapon` ✓ | — (systemic) | — (systemic) | stat bars only | — (systemic) | 300/300; range 20 vs enemies' 50 — placeholder | **Playable but incomplete** |
| LargeShip | `Assets/Data/Ships/Ships/LargeShip.asset` | `Assets/Prefabs/Ships/LargeShip.prefab` (6 mounts) | hull + banking + projectile FX | ProjGun2 via `ProjWeapon` ✓ | — | — | stat bars only | — | 5000/5000, dmg 1 @ 0.1s | **Playable but incomplete** |
| RocketShip | `Assets/Data/Ships/Ships/RocketShip.asset` | `Assets/Prefabs/Ships/RocketShip.prefab` (4 mounts) | hull + banking + rocket trails | RocketGun via `RocketWeapon` ✓ | — | — | stat bars only | — | 300/300 = copy of SmallShip | **Playable but incomplete** |
| UndeadShipSmall | `Assets/Data/Ships/Ships/UndeadShipSmall.asset` | reuses SmallShip hull | (reused) | `ProjWeapon` (not SmallShip's ray) | — | — | — | — | 5,000,000/5,000,000 — test dummy | **Orphaned** (in no `TeamData`) |
| UndeadShipLarge | `Assets/Data/Ships/Ships/UndeadShipLarge.asset` | reuses LargeShip hull | (reused) | `ProjWeapon` ✓ | — | — | — | — | 5,000,000/5,000,000 — test dummy | **Orphaned** (in no `TeamData`) |

Weapon-layer completeness (ships depend on it):

| Weapon | Data asset | Gun prefab | Status |
|---|---|---|---|
| Ray | `Assets/Data/Weapons/DefaultWeapon.asset` | `Assets/Prefabs/Weapons/RayGun.prefab` | Working (hitscan; `RayVisualizer` beam) |
| Projectile | `Assets/Data/Weapons/ProjWeapon.asset` | `Assets/Prefabs/Weapons/ProjGun2.prefab` | Working (pooled, batched, lead-targeting) |
| Rocket | `Assets/Data/Weapons/RocketWeapon.asset` | `Assets/Prefabs/Weapons/RocketGun.prefab` | Working (homing, per commit `900d57b` "Rockets complete") |
| — | none | `Assets/Prefabs/Weapons/Plasm.prefab` | **Orphaned + would break**: zero references anywhere, and its `BurstProjectileShooter` is incompatible with `WeaponFactory`'s hardcoded `GetComponent<SequenceProjectileShooter>()` (NRE on wire-up) |
| `RayWeapon` (class) | — | — | **Stub**: empty class in `Assets/Scripts/Gameplay/Weapon/SimpleGun.cs`, on no prefab |

Deployment (who actually spawns): `Team Player` = 1× RocketShip, 2× SmallShip, 4× LargeShip;
`Team 2` (Enemy) = 1× LargeShip. Both teams wired into `GameController._teams` in
`SampleScene.unity`.

Count summary: **Done 0 · Playable but incomplete 3 · Stub 0 · Broken 0 · Orphaned 2.**

---

## 4. Data/asset inconsistencies

1. **Orphaned configs:** `UndeadShipSmall`/`UndeadShipLarge` are referenced by no `TeamData`,
   no scene object, no code. Reachable only by hand-editing a team asset. Clearly test
   dummies; keep or move to a "Testing" folder, but decide.
2. **Orphaned weapon prefab:** `Assets/Prefabs/Weapons/Plasm.prefab` — zero references in any
   scene/prefab/asset, and structurally incompatible with `WeaponFactory` (see §6 W-3). It is
   also the only user of `BurstProjectileShooter`, making that class dead in practice.
3. **`DefaultWeapon.asset` has no `_weaponType` field serialized** — it predates the field and
   was never re-saved. It works only because the missing value deserializes to `0 == Ray`,
   which happens to be correct. Fragile: reordering `WeaponType` (forbidden, but…) or
   re-purposing the asset silently changes behavior. Same asset still stores `_rotaionSpeed`
   under the pre-rename key (covered by `[FormerlySerializedAs]`, cleaned on next re-save).
4. **Copy-paste stats:** RocketShip's 300/300 is identical to SmallShip's; both Undead ships
   are 5M/5M. All four Health/Shield pairs in all five ships use the *same* value for both
   stats — no ship differentiates shield from hull yet. Placeholder balance throughout.
5. **`DamageOrder` is 1 for every stat of every ship**, so `ShipStatsController.DealDamage`'s
   `OrderByDescending(DamageOrder)` is a tie for all entries and the shield-before-health
   behavior currently rests on `Dictionary.Values` enumeration order (insertion order in
   practice, **undefined by contract**). The data field exists precisely to control this and
   is unused: shields should presumably be `2`.
6. **`UndeadShipSmall` carries `ProjWeapon`, not SmallShip's ray weapon** — on the SmallShip
   hull with 1 mount. Works, but if it's meant to be "SmallShip but immortal" it's not an
   exact variant. Consistent with a throwaway test asset.
7. **Bullet data referenced twice per gun prefab:** `ProjGun2.prefab` references
   `BulletData.asset` from both `SequenceProjectileShooter._bulletData` (what it fires) and
   `AdvanceWeaponTargeter._bulletData` (speed for lead calculation). Nothing enforces they
   match; editing one field and not the other silently mis-aims every shot.
8. **Docs vs tree:** `Docs/Agents/PROJECT_CONTEXT.md` §6 lists `Initilize`/`Initiliazer` as
   API spellings; the working tree has renamed them (`HalloGames.Architecture.Initializer`,
   `IInitializable<T>`, `ShipInitializationData` — class names `ShipInitilizer`,
   `WeaponInitilizer`, `AbstractInitilizer`, `GameInitiliazer` still carry the typo). §7 says
   selection is "`ObjectClicker` / `ShipsHandler`" and mentions an FPS counter; `ShipsHandler`
   is unreferenced dead code and `FPS.cs` is not in `SampleScene`. (Not fixed here — task
   constraint; flagged in §9.)

---

## 5. Strengths

Decisions worth preserving through any refactor:

1. **Pure composition for ships** — `ShipEntity` (`Assets/Scripts/Gameplay/Ship/ShipEntity.cs`)
   caches sibling components behind five narrow interfaces; not a single per-ship subclass
   exists. New behavior = new component on the prefab, not a hierarchy change.
2. **Push-based initialization** — `AbstractInitilizer<TData>` +
   `IInitializable<ShipInitializationData>`: data flows down once, components never hunt for
   owners with `GetComponentInParent` at runtime. `StatBar` deep in the nested canvas gets its
   config through the same broadcast as the root components.
3. **Data-driven ship identity** — `ShipData` → `ShipHullData` + `WeaponData` + stat list.
   Adding a ship variant is asset work only. Teams (`TeamData`) and sides (`SideData`) are
   assets too; the scene references two team assets and nothing else about fleet composition.
4. **Batched projectile simulation** — `ProjectilelController<T>`
   (`Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs`) simulates all live
   projectiles in one `Update` with `RaycastCommand.ScheduleBatch` + a Burst-compiled filter
   job, and projectiles are plain pooled objects (`ProjectileWrapper` holds the state, not a
   per-bullet MonoBehaviour `Update`). This scales to hundreds of bullets and is unusually
   solid for a prototype.
5. **Real ballistic lead solve** — `AdvanceWeaponTargeter.CalculatePos` solves the quadratic
   intercept against target velocity (with a sane fallback when the discriminant is
   negative). Turrets actually aim ahead.
6. **Event-decoupled visuals** — `ProjectileVisual` and `RayVisualizer` subscribe to
   `IShooter.OnShooting`/`OnDealDamage`; gameplay code never calls a VFX method directly.
   Swapping gun art requires no logic change.
7. **Stats as asset-keyed dictionary** — `StatData` ScriptableObjects as keys +
   `StatStruct{StartValue, DamageOrder}` means a third stat (armor, energy) is an asset + a
   list row, and `StatBar` picks it up by reference, not by index.
8. **One composition root** — `ProviderBuilder.RegisterServices()` is genuinely the single
   binding site; factories (`ShipSpawner`, `WeaponFactory`, `BulletSpawner`, `RocketSpawner`)
   are resolved through it, and per-shot spawning goes through `PoolManager` as the house
   rules require.

---

## 6. Weaknesses

Severity-sorted. "Cost" names what it blocks or degrades.

| # | Severity | What | Where | Cost |
|---|---|---|---|---|
| W-1 | **Critical** | **No death pipeline.** `ShipStatsController.Death()` raises `OnDeath` and stops there: the dead ship is never despawned or unregistered, stays selectable/movable (`ShipControlInterpreter` never checks `IsDead`), keeps blocking the NavMesh, and — worst — **its own weapons keep firing** (nothing calls its `ShipWeaponsController.StopShooting`). `ShipTeamObserver.StartObserving` is an empty loop, `OnAllDies` never fires, `ShipsManager.AllDies`/`ResetTeams` are empty. | `Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs:97`, `Assets/Scripts/Management/ShipTeamObserver.cs:21`, `Assets/Scripts/Management/ShipsManager.cs:46` | Blocks every gameplay loop: no kill feedback, no win/lose, zombie combat state; every future system (AI, scoring, respawn) has nothing to hook |
| W-2 | **High** | **Stale death subscription on retarget.** `ShipWeaponsController.Shoot` does `DeathHandler.OnDeath += StopShooting` per call and never unsubscribes. Retarget from T1 to T2, then T1 dies → this ship silently stops shooting T2. Repeated right-clicks on the same target stack duplicate subscriptions. | `Assets/Scripts/Gameplay/Weapon/ShipWeaponsController.cs:59` | Live combat bug the player can trigger today; class of bug multiplies once AI issues orders |
| W-3 | **High** | **`WeaponFactory` hardcodes the shooter type and branches on `WeaponType`.** `GetComponent<SequenceProjectileShooter>()` for both Projectile and Rocket — any gun using `BurstProjectileShooter` (i.e. `Plasm.prefab`) NREs; every new weapon type means editing this if/else ladder and appending the enum. The gun prefab already knows its own `IShooter`; the factory duplicates that knowledge badly. | `Assets/Scripts/Gameplay/Weapon/WeaponFactory.cs:30-39` | Content-production trap: gun prefabs that look valid crash at spawn; weapon variety gated on code edits |
| W-4 | **High** | **Weapon balance split across 3 surfaces + duplicated reference.** Damage/fire-rate in `WeaponData`, projectile speed/lifetime in `ProjectileData` referenced from *two* components of the gun prefab (shooter + targeter, must agree), mount count in the ship prefab. A single "how hard does LargeShip hit" answer touches 2 assets and 2 prefabs. | `Assets/Data/Weapons/*`, `Assets/Prefabs/Weapons/ProjGun2.prefab`, ship prefabs | Balance iteration speed; silent mis-aim if the two `_bulletData` refs diverge (§4.7) |
| W-5 | Medium | **Per-hit LINQ allocation chain in the damage path.** `DealDamage` runs `Where().OrderByDescending().ToList()` + `Sum()` on every projectile impact — with `ProjWeapon` at 0.1s × 2 muzzles × 6 mounts × 5 LargeShips this is hundreds of allocations/second in combat. | `Assets/Scripts/Gameplay/Ship/ShipStats/ShipStatsController.cs:77-92` | GC spikes in the hottest combat path; violates the project's own no-alloc-in-hot-loop rule |
| W-6 | Medium | **`RayVisualizer` instantiates a material every frame while a beam fades** (`_lineRenderer.material` getter clones; the clone is never destroyed). | `Assets/Scripts/Gameplay/Weapon/Ray/RayVisualizer.cs:63-71` | Per-shot material leak + allocation; use `material` once cached, or `SetColor` on a cached instance/MPB |
| W-7 | Medium | **Damage order is data-ambiguous** — all `DamageOrder` values equal (see §4.5); shield-first behavior rides on dictionary enumeration order. | all `Assets/Data/Ships/Ships/*.asset` + `ShipStatsController.DealDamage` | Balance behavior can flip on a Unity/runtime change; the knob exists and nobody set it |
| W-8 | Medium | **Dead scaffolding misleads readers**: `ShipsHandler` (box-select, internally buggy `Bounds` math, on no scene object, zero callers), `ShipUiStats` (empty methods, on no prefab), `RayWeapon` (empty stub class), `BurstProjectileShooter` (unreachable via factory), unused structs `ShipsSpawnData`, `WeaponSpawnData`, unused `ProjectileVisual._weaponController` field, `ShipModelRotator._navMesh` serialized but never read. | `Assets/Scripts/Gameplay/Control/ShipsHandler.cs`, `Assets/Scripts/Gameplay/Ship/ShipUiStats.cs`, `Assets/Scripts/Gameplay/Weapon/SimpleGun.cs`, … | Onboarding cost; docs reference some of it as if live (PROJECT_CONTEXT §7) |
| W-9 | Medium | **No null-guards on the spawn path.** `ShipSpawner.SpawnShip` chains `Instantiate(...).GetComponent<ShipEntity>()` unguarded (a hull prefab missing the component NREs deep in a loop); `GetSpawnPos` does `.Find(...).Zone` — a side without a spawn zone NREs. `StatBar.Init` does `Find(...)` then dereferences — a ship config missing that stat NREs on spawn. | `Assets/Scripts/Management/ShipSpawner.cs:46,59`, `Assets/Scripts/UI/StatBar.cs:36` | Content-creation errors surface as cryptic runtime NREs instead of clear messages |
| W-10 | Medium | **Every weapon/targeter runs `Update` while idle** (cooldown timer and `RotateToDefault` tick on all ~40 spawned guns regardless of combat), plus `WeaponController.Update`'s time check runs before the null-target check, so `_currentTime` accumulates pointlessly. Fine at 8 ships; linear waste as fleets grow. | `Assets/Scripts/Gameplay/Weapon/WeaponController.cs:49`, both targeters | Per-frame cost scales with total mounts, not active combat |
| W-11 | Low | **Projectiles only collide with their intended target** (`colliderInstanceID` filter in the Burst job) — dodged bullets fly through other ships and terrain. Cheap and deliberate-looking, but undocumented; friendly fire, accidental hits and terrain cover are all impossible. | `Assets/Scripts/Gameplay/Weapon/Projectile/BulletsController.cs:133-144` | Design ceiling, worth a written decision |
| W-12 | Low | **Ships bypass pooling and `ResetTeams` is a stub** — fine for one battle per play session, but the moment restart/waves exist, ship spawn must be revisited together with W-1. | `Assets/Scripts/Management/ShipSpawner.cs:46` | Deferred cost, coupled to the death pipeline design |
| W-13 | Low | `MouseInput.Dispose` never called; `IInput.OnErase` never raised (already documented in `Docs/INPUT_SYSTEM_RESEARCH.md`). Affects ships indirectly via orders. | `Assets/Scripts/Gameplay/Input/MouseInput.cs` | Minor lifecycle debt |

### 6.1 Structural risks (what gets worse with ship #20)

- **Everything missing lands on the same empty slots.** Death, AI, audio, abilities all need
  per-ship hooks; because those systems don't exist, every new ship shipped now is a ship that
  must be *revisited* when they arrive. The cheapest time to define the death/AI component
  contract is before the content multiplies.
- **One `WeaponData` per ship.** `ShipData._weaponData` is a single reference applied to all
  mounts. Ship #20 with mixed armament (point defense + main battery) forces either a schema
  change (`List<WeaponData>` aligned with mounts) or per-mount overrides — decide before many
  configs exist, because every existing asset gets migrated.
- **`WeaponFactory`'s if/else ladder** (W-3) grows per weapon type, and each new projectile
  family currently also means: enum append + new `ProjectileData` subclass + new wrapper +
  new controller + scene component + two `ProviderBuilder` registrations + a `PoolManager`
  row. That's the real per-weapon-type price today (~6 files + scene wiring).
- **Prefab-by-hand assembly.** Nine root components + nested canvas + mount transforms +
  collider wiring per ship prefab, with no validation (W-9). At 3 prefabs this is fine; at 20
  it's a checklist that will be gotten wrong. A prefab variant base (or an editor validation
  script later) caps this cost.
- **`Team Player` duplicates `ShipData` entries per unit count** (4 identical LargeShip rows).
  Fleet definitions of `{ShipData, count}` pairs will read and diff better once fleets grow.

---

## 7. How to add a new ship — the current real procedure

Assuming the ship reuses an existing weapon type (the cheap case):

1. **Model:** import/pick a hull model (no pipeline for damage states; one static GFX child).
2. **Prefab** (the expensive step): duplicate `Assets/Prefabs/Ships/SmallShip.prefab` (safer
   than assembling from scratch), then per copy:
   - replace the GFX child; keep `ShipModelRotator._model` pointing at it;
   - keep/verify all nine root components (`ShipEntity`, `ShipInitilizer`,
     `ShipMovementController` + `NavMeshAgent`, `ShipStatsController`,
     `ShipWeaponsController`, `ShipTarget`, `ShipControlInterpreter`,
     `SelectGFXController`, `ShipModelRotator`);
   - re-wire `ShipTarget._hullCollider` to the new hull collider (layer must match
     `ObjectClicker._targetLayer`);
   - place N gun-mount transforms and re-populate `ShipWeaponsController._gunPositions`;
   - verify the nested `ShipCanvas` instance's two `StatBar._shipEntity` overrides survived
     the duplicate;
   - verify `ShipInitilizer._shipWeapons` still points at the root controller.
3. **Hull asset:** `Assets/Data/Ships/ShipHulls/` → create `Data/Ships/ShipHullData`; set
   `_hullPrefab` (the new prefab), `_shipSize` (`Small`/`Large` asset), 3 movement floats.
4. **Ship asset:** `Assets/Data/Ships/Ships/` → create `Data/Ships/ShipData`; set hull,
   weapon (`DefaultWeapon`/`ProjWeapon`/`RocketWeapon`), and the stat list — *both* `Health`
   and `Shield` rows are effectively mandatory (StatBar NREs on a missing one, W-9), each
   with `StartValue` and `DamageOrder` (use distinct orders; see W-7).
5. **Team:** add the ship asset to `Assets/Data/Ships/Team Player.asset` or `Team 2.asset`
   (one row per spawned unit).
6. **Test:** play `SampleScene` — ships spawn scattered (radius 4) in their side's zone.

No code changes: **2 assets, 1 prefab, 1 team edit.** The prefab step dominates and is
validated by nothing except runtime NREs.

**New weapon for an existing type** (e.g. a second projectile gun): gun prefab
(`WeaponController` + `AdvanceWeaponTargeter` + `SequenceProjectileShooter` — *not* Burst,
W-3 — + `ProjectileVisual` + mount/muzzle transforms, `_bulletData` set **in two components**)
+ `WeaponData` asset + optionally a new `BulletData` + pooled projectile prefab + a
`PoolPair` row on `PoolManager` in the scene.

**New weapon *type*** (e.g. beam-over-time, mines): append `WeaponType`, edit
`WeaponFactory.CreateWeapon`, write `IShooter` (+ wrapper + `ProjectilelController` subclass +
scene component + spawner service + 2 `ProviderBuilder` registrations if it needs simulation),
pool row. ~6 files + scene wiring — this is the expensive axis.

---

## 8. Recommendations

Prioritized; no implementation was started.

### Finish first (content closest to done)

1. **The three playable ships share one missing piece: death.** They are otherwise
   content-complete for what the systems support. Finishing *any* ship means building the
   death pipeline (below, Fix #1) — there is no per-ship content shortcut.
2. **Decide the Undead pair's fate** — either wire a "sandbox" `TeamData` that spawns them
   for weapon testing, or park them under a clearly-named test folder. Zero effort, removes
   the largest data/asset mismatch.
3. **RocketShip balance pass** — it currently copies SmallShip's 300/300 while carrying 4
   rocket mounts (dmg 30 @ 3s each). Whether that's intended is a design call; the numbers
   look untouched since creation.

### Fix (defects in existing code/content)

1. **Death pipeline (W-1)** — subscribe to each spawned ship's `IDeathHandler.OnDeath` (the
   natural owner is `ShipTeamObserver`, whose `StartObserving` stub already exists for
   exactly this): stop own weapons, disable control/selection, unregister from the team,
   despawn (plain `Destroy` is fine until pooling is wanted), fire `OnAllDies` →
   `ShipsManager` → win/lose hook in `GameController`. This is one focused change with
   existing seams; **S/M effort**, unblocks everything.
2. **Retarget subscription leak (W-2)** — unsubscribe the previous target's `OnDeath` in
   `Shoot`/`StopShooting`. **S**.
3. **`DamageOrder` data (W-7)** — set Shield rows to 2 in the five ship assets (asset-only
   change) so the code's ordering contract is actually exercised. **S**.
4. **`DealDamage` allocations (W-5)** and **`RayVisualizer` material clone (W-6)** — cache
   the sorted damageable list at `Init`, cache the material once. **S** each.
5. **Re-save `DefaultWeapon.asset`** so `_weaponType: 0` and `_rotationSpeed` are explicit
   (§4.3) — a human editor step, seconds.

### Refactor (structural, each with trade-offs)

1. **Let gun prefabs declare their own shooter (kills W-3).**
   *Problem:* factory hardcodes `SequenceProjectileShooter` + type ladder. *Approach:*
   `weapon.GetComponent<IShooter>()` and give shooters an `IProjectileCreator`-consuming
   interface; map `WeaponType → IProjectileCreator` via a small registry (or resolve
   `BulletSpawner`/`RocketSpawner` behind keyed services). *Migration:* incremental — one
   factory method rewrite, prefabs untouched. *Blast radius:* `WeaponFactory` only.
   *Effort:* **S**. *Trade-off:* none significant; alternative (status quo) keeps Plasm-class
   traps.
2. **Unify weapon ballistic data (kills W-4/§4.7).**
   *Option A:* move the `ProjectileData` reference from the two gun-prefab components into
   `WeaponData` and push it through `Init` (data-driven, one source; migration touches 4 gun
   prefabs + 3 weapon assets + 3 scripts). *Option B:* keep it on the prefab but have the
   targeter read it from the shooter at `Awake` (smaller change, still prefab-resident).
   *Effort:* **M** (A) / **S** (B). *Trade-off:* A centralizes balance in assets (faster
   iteration, better diffs); B is cheaper but leaves speed data outside `WeaponData`.
3. **Fleet schema: `{ShipData, count}` rows in `TeamData`, and (bigger) per-mount weapon
   lists in `ShipData`.** *Problem:* §6.1 growth costs. *Approach:* additive fields with
   `[FormerlySerializedAs]`-style care (new list field, keep old until assets migrate).
   *Migration:* incremental, assets one by one. *Blast radius:* `ShipsManager`,
   `ShipWeaponsController.Init`, team/ship assets. *Effort:* **M**. *Trade-off:* do it only
   when a mixed-armament ship or large fleets are actually planned — premature now at 3
   ships.
4. **Delete or quarantine dead scaffolding (W-8).** *Approach:* remove `ShipsHandler`,
   `ShipUiStats`, `RayWeapon`, unused structs/fields — or keep `BurstProjectileShooter` only
   if Refactor #1 lands (it becomes usable then). *Blast radius:* zero runtime; docs must be
   updated in the same change (PROJECT_CONTEXT §7). *Effort:* **S**. *Trade-off:* history
   shows these are intended future features; quarantining under a `WIP/` folder note is the
   conservative alternative to deletion. House rules say don't drive-by-delete — this needs
   its own approved task.

### Do not touch (for now)

- **Ship spawning without pooling (W-12)** — 8 `Instantiate` calls at scene start are
  irrelevant; pooling ships only pays once respawn/waves exist, and it must be co-designed
  with the death pipeline anyway.
- **Per-frame weapon `Update`s (W-10)** and **`ObjectClicker`'s 2 raycasts/frame** — real but
  invisible at current scale; batching order systems are a later optimization with a
  measurable trigger (fleet size).
- **Single-target collision filter (W-11)** — it's a coherent arcade design choice and the
  Jobs pipeline depends on it; revisit only if friendly fire / terrain cover become design
  goals.
- **The remaining typo'd class names** (`ShipInitilizer`, `GameInitiliazer`,
  `AbstractInitilizer`) — they're serialized into prefabs/scene; renaming is prefab surgery
  for zero behavior. The in-flight naming cleanup already took the safe subset (namespaces,
  interfaces, methods).

---

## 9. Open questions

1. **Is the naming cleanup in the working tree finished?** It was *in progress while this
   audit ran* (files changed between reads). This document describes the tree as last read;
   `Docs/Agents/PROJECT_CONTEXT.md` §6's "typos are API" list is now stale either way and
   needs updating in the same commit as the cleanup.
2. **Are the Undead ships a deliberate test fixture** to keep, or leftovers to delete? No
   code references them; only a human knows the intent.
3. **Is `Plasm.prefab`/`BurstProjectileShooter` a planned weapon** (shotgun-style burst)?
   If yes, Refactor #1 is its prerequisite; if no, it's W-8 cleanup.
4. **Is single-target-only projectile collision (W-11) a design decision** or an
   implementation shortcut? Determines whether the Jobs filter is a keeper or a placeholder.
5. **Intended difference between Health and Shield?** Today they are numerically identical
   twin pools with equal damage order — no regen, no resistances. The stat system supports
   more; the design intent is unrecorded.
6. **`ShipModelRotator._navMesh` is serialized but unused in code** — was velocity-based
   banking planned? Field looks like leftover intent.
7. **Why does `Team Player` field 7 ships against Team 2's single LargeShip?** Looks like a
   hand-tuned test skirmish; worth confirming before anyone treats team assets as balance
   references.
8. **`FPS.cs` is not in `SampleScene`** despite PROJECT_CONTEXT §7 pointing testers at it —
   was it removed from the scene intentionally?
