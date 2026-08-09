# Unity rules for AI agents — Space-RTS

Adapted for this repository. Project-specific values live in
[PROJECT_CONTEXT.md](PROJECT_CONTEXT.md); the process around these rules is
[WORKFLOW.md](WORKFLOW.md).

---

## 0. The five things that break a Unity project silently

No compiler error, no exception, no test failure — just lost data or a dead reference.
Everything else in this file is detail; these five are the reason the file exists.

| # | Action | What silently breaks |
|---|--------|----------------------|
| 1 | Renaming a serialized field | Values wired in every prefab / scene / asset reset to default |
| 2 | Renaming a save-data field | Player loses that progress on the next load |
| 3 | Reordering / inserting enum members | Enums persist as **ints** — every saved value now means something else |
| 4 | Hand-editing `.prefab` / `.unity` / `.asset` YAML or deleting a `.meta` | GUID / structure corruption; "Missing script" everywhere |
| 5 | Adding a `[SerializeField]` and calling it done | Field is `null` at runtime until a human wires it in the editor |

**Default stance: additive only.** Add new fields, append enum members, keep old ones alive.

---

## 1. Serialization surfaces — identify the surface first

Decide which surface a field belongs to before touching it.

| Surface | Serializer | Rename protection | Location in this project |
|---|---|---|---|
| Prefabs / scenes (`[SerializeField]` + public fields on MonoBehaviours) | Unity YAML | `[FormerlySerializedAs]` **works** | `Assets/Prefabs/`, `Assets/Scenes/` |
| ScriptableObject config assets | Unity YAML | `[FormerlySerializedAs]` **works** | `Assets/Data/**` |
| Generated input bindings | Unity Input System codegen | n/a — regenerated | `Assets/Data/Input/PlayerInputMaps.*` |
| Player save data | — | — | **Does not exist in this project.** If you add one, add its rules to `PROJECT_CONTEXT.md` first. |

### Rule 1.1 — Never rename a serialized field without a migration story

```csharp
using UnityEngine.Serialization;

[FormerlySerializedAs("_oldName")]           // keep it forever — removing it re-breaks
[SerializeField] private Transform _newName; // assets that were never re-saved
```

- Attributes stack for a field renamed twice:
  `[FormerlySerializedAs("_rooms")] [FormerlySerializedAs("_seats")] [SerializeField] private Transform[] _queuePositions;`
- **Save-data fields (name-keyed JSON): renames are forbidden outright** — there is no rename
  attribute on that path, and `[FormerlySerializedAs]` there is decorative and ineffective.
- **Generated model fields: don't rename at all** — the file is regenerated from source data.

### Rule 1.2 — Public fields on components ARE serialized API

Legacy code here uses `public` fields in serializable structs (`PoolPair`, `StatStruct`).
Those values live in prefabs and `.asset` files. Renaming, retyping, removing, or changing
visibility (`public` → `private` without `[SerializeField]`) loses wired data — treat them
exactly like `[SerializeField]`. Write new code in the house style, but **do not "modernize"
existing public fields** in unrelated edits.

### Rule 1.3 — Type changes have no safety net

`[FormerlySerializedAs]` handles renames, **not** type changes. `int → float`,
`single → array`, `ClassA → ClassB`: Unity discards the old value with no warning.
If a type must change: add a **new** field with a **new** name, keep the old one for
migration, and flag the manual data-migration step in your summary.

### Rule 1.4 — Enums serialize as ints. Append only.

Never reorder, renumber, or delete members of a C# enum used in configs, prefabs or saves
(`WeaponType` and friends). Append new members at the end — always.

**In this project most "enums" are ScriptableObjects** (`SideData`, `ShipSize`, `StatData`).
Their identity is the asset GUID: adding one is a manual "create the asset" step, renaming the
*asset file* is safe, **deleting one breaks every reference that points at it**.

### Rule 1.5 — Generated / imported files are machine-owned

`Assets/Data/Input/PlayerInputMaps.cs` is regenerated from the `.inputactions` asset — edit the
asset, never the output. Third-party folders (`Assets/SpaceSkies Free/`) and package caches are
read-only: if a limitation there blocks you, work around it in game code and flag it.

---

## 2. Prefabs, scenes and `.meta` files — the hard boundary

- **Agents change C# only.** Never hand-edit `.prefab`, `.unity`, or `.asset` YAML.
- **Never delete or regenerate a `.meta`** of an existing asset — the GUID change breaks every
  reference to it. New files get their `.meta` from Unity, not from you.
- **Adding** a `[SerializeField]` is safe to deserialize (null/default), but the component is
  **not done** until a human wires it. Add a null guard so the unwired state doesn't crash.
- **Removing** a field orphans data harmlessly — but first check for `SendMessage`, animation
  events, and UnityEvent bindings (those bind to **method names as strings**).
- Renaming the **class** or moving the **file** is fine (references go by GUID), but a
  MonoBehaviour class name must match its file name or every prefab shows "Missing script".
- **Every prefab/scene hookup is a manual editor step you must list in the completion summary.**
  No list = the task is not finished.

---

## 3. Editor-only code must never reach player builds

This project has **no `Editor/` folders and no asmdefs** — everything lands in
`Assembly-CSharp`. So editor-only code needs an explicit guard, wrapping the `using` line too:

```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Thing : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Rebuild")]
    private void Rebuild() => EditorUtility.SetDirty(this);
#endif
}
```

An `EditorWindow` or a custom inspector must either live in a new `Editor/` folder (compiled into
the editor-only assembly automatically) or have the **entire file** wrapped. Runtime code must
never reference `UnityEditor` — it breaks player builds.

If you find existing violations: **flag them, do not fix them in a drive-by edit.**

---

## 4. Architecture

### 4.1 Humble Object — logic out of MonoBehaviours

Business logic goes in plain C# (POCO) classes. When logic must live on a component
(scene/prefab lifecycle, serialized fields, Unity callbacks), the MonoBehaviour is a **thin
humble wrapper**: it owns Unity-side concerns (wiring, lifecycle, coroutines) and delegates
every decision to the POCO it holds.

```csharp
public sealed class ThingLogic                 // engine-agnostic, testable
{
    private readonly IThingConfig _config;

    public ThingLogic(IThingConfig config)
    {
        _config = config;
        Assert.IsNotNull(_config, nameof(_config));
    }

    public bool CanActivate(int level) => level >= _config.MinLevel;
}
```

### 4.2 Dependency acquisition — earliest possible, cached always

Order of preference:

1. **Constructor injection** for plain C# classes — services get passed in, not grabbed.
2. If impossible (MonoBehaviours, lifecycle constraints): receive dependencies through the boot
   chain's `Initialize`/`Init` method, or resolve into private fields in `Awake()`.

| ❌ Don't | ✅ Do |
|---|---|
| Call `GetService<T>()` / `.Instance` mid-code in occasional methods | Resolve once into a private field, use the field |
| Resolve in field initializers or MonoBehaviour constructors | Resolve in `Awake()` / `Initialize()` — services may not be registered earlier |
| `GetComponentInParent<T>()` to acquire a dependency | `[SerializeField]` wired in the editor, or `IInitializable<T>` data push (list the hookup) |
| A new static helper / new singleton for new logic | `public sealed class` + constructor injection, registered in `ProviderBuilder` |

Cache singletons (`PoolManager.Instance`) in a field too — cheaper per call and it makes the
dependency explicit.

**DI entry point:** `Assets/Scripts/Management/ProviderBuilder.cs` → `RegisterServices()` is the
single file where all bindings live. A new injectable must be registered there **before**
anything resolves it. Binding is keyed by the compile-time generic `T` — bind and resolve with
the exact concrete type. Details and the full boot chain: [PROJECT_CONTEXT.md §4](PROJECT_CONTEXT.md).

### 4.3 No member access on an unchecked method result

Lookup methods (`GetService<T>`, `GetComponent<T>`, `Find...`, dictionary-style lookups)
routinely return `null`, and chaining onto them crashes with an uninformative NRE.

```csharp
// ❌
if (_shipEntity.GetComponent<IStatsController>().IsAlive) { ... }

// ✅
IStatsController statsController = _shipEntity.GetComponent<IStatsController>();
if (statsController == null)
    return;                       // or log / fallback appropriate to the call site
if (statsController.IsAlive) { ... }
```

`?.` is **not** a substitute for `UnityEngine.Object` types — it bypasses Unity's
destroyed-object check.

### 4.4 Events and lifecycle symmetry

- `Subscribe`/`+=` and `Unsubscribe`/`-=` must use the **same method reference** — never a
  lambda, or the unsubscribe silently does nothing.
- Subscribe/unsubscribe must be lifecycle-symmetric: `OnEnable`/`OnDisable` **or**
  `Awake`/`OnDestroy` — match the pattern of the class you are editing.
- **Pooled objects are reused**: everything hooked up in `EnableObject()` must be undone in
  `DisableObject()`, including running routines (`IStopable.Stop()`), target references and VFX.

### 4.5 Async

- **This project uses coroutines only**, through `RoutineManager` / `Routine` — not raw
  `StartCoroutine`, not `Task`, not UniTask. Keep the returned `IStopable` and stop it before
  starting a replacement.

---

## 5. Performance

- **Target 60 FPS.** This is an RTS: assume hundreds of ships, projectiles and impacts at once.
- No per-frame allocations: no LINQ, closures, or string concat in `Update`.
- Cache component lookups in `Awake`; never `GetComponent` in a loop or in `Update`.
- **Reuse the existing pools** (`PoolManager`) — don't `Instantiate`/`Destroy` per projectile,
  impact VFX, or UI list entry.
- **UI lists reuse elements:** fill pre-spawned/pooled elements and `SetActive(false)` the
  extras, instead of destroy-and-rebuild.
- Prefer squared-distance comparisons (`sqrMagnitude`) over `Vector3.Distance` in hot loops;
  `MathExt` (`Assets/Scripts/Extentions/`) is the home for shared math helpers.
- Physics queries (`Physics.Raycast` and friends in `ProjectileRaycaster`, `ObjectClicker`) use
  the non-allocating `...NonAlloc` variants with a cached buffer when they run per frame.

---

## 6. Code style

### Naming and layout

- Classes: PascalCase, suffixed by role — `*Controller`, `*Manager`, `*Handler`, `*Factory`,
  `*Spawner`, `*Data` (ScriptableObject configs), `*Object` (pooled prefab components),
  `*Initilizer` (note the project spelling).
- Prefer **generic naming over use-case naming** for reusable components: name by what the code
  does, not by the first feature that needed it.
- `[SerializeField] private Type _camelCase;` — leading underscore, private.
- **Serialized collections always get a default value:**
  `[SerializeField] private List<GameObject> _effects = new ();`
- **No public fields in new code.** MonoBehaviours: `[SerializeField] private` + an
  expression-bodied property when outside access is needed. POCOs: public fields forbidden.
  (Existing public fields in legacy serializable structs stay — see Rule 1.2.)
- One class per file, filename = class name. Exception, already established here: a small
  interface may sit in the same file as its primary implementation — follow the local pattern.
- Handlers named `On<Source><Action>` (`OnButtonLeftClick`); bool-returning attempts `Try...`;
  private consts `UPPER_SNAKE_CASE`.
- **Do not fix the remaining misspellings.** File-bound (renaming means moving the `.meta` too —
  a human step in Unity): `AbstractInitilizer`, `ShipInitilizer`, `WeaponInitilizer`,
  `GameInitiliazer`, `NoneLazySingletone`, `QuequeStateMachine`, `AdvanceWeaponTargeter`,
  `IStopable`, and the `Extentions/` folder. Not file-bound but left alone by choice: the
  `HalloGames.Architecture.Singletones` namespace. See
  [PROJECT_CONTEXT.md §6](PROJECT_CONTEXT.md).

### Member order inside a class

1. `static` fields and `const`s → 2. events → 3. serialized fields → 4. public fields →
5. private fields → 6. properties → 7. constructor → 8. methods (in MonoBehaviours: Unity
callbacks first in lifetime order `Awake`, `OnEnable`, `Start`, …, `OnDisable`, `OnDestroy`,
with `Update`/`LateUpdate`/`FixedUpdate` **last** of the callbacks; then everything else) →
9. inner classes → 10. enums.

### Formatting idioms

**Column-align declaration and assignment groups:**

```csharp
[SerializeField] private ParticleSystem[] _trails;
[SerializeField] private GameObject       _gfx;

private IStopable        _stopable;
private IWeaponController _weaponController;

_side     = data.SideData;
_shipData = data.ShipData;
```

**Guard clauses, never nesting.** Single-statement guards go brace-less on the next line:

```csharp
if (config == null)
    return null;
```

**Small single-purpose methods.** One `Refresh()` that calls `RefreshImage()`,
`RefreshButtonState()`, …; extract predicates into named methods.

**Expression bodies for one-liners:**

```csharp
private void OnClickLeft() => SwitchLocation(-1);
public  SideData Side      => _side;
```

**Plain C# classes:** `public sealed class`, `readonly` fields, constructor injection,
`Assert.IsNotNull(_field, nameof(_field));` per injected dependency.

### SRP — methods and classes

Every method does one thing. When adding behavior to an existing method, **do not inline** a
separate concern: extract it into its own named method and add a single call at the insertion
point (`PlayParticles();` — not `_particlesPlayer.Play()` inlined). Same at class level: a new
responsibility gets its own class/component rather than growing an existing one.

### Comments

Comments are a code smell — express intent through descriptive names instead. A comment is a
last resort for a constraint the code cannot express. Keep existing comments (including
non-English ones) intact and UTF-8; don't translate or reformat them in unrelated edits.

---

## 7. Leave the warts alone

- Commented-out code blocks and hack markers (`//Costil`, `//HACK`, …) are intentional history.
  Don't clean up, reformat, or delete code unrelated to your task.
- **Behavior-preserving is the default.** Flag suspicious code in the summary instead of
  "fixing" it.
- Keep changes focused. A refactor nobody asked for is a merge conflict plus a regression risk.

---

## 8. Platform and build

- This project currently has **no custom compile defines and no platform splits**.
- If one becomes necessary: platform splits go through **compile defines** (`UNITY_ANDROID`,
  `UNITY_IOS`, `UNITY_WEBGL`, `UNITY_STANDALONE`) and **factories wired at boot** — not inline
  `Application.platform` checks scattered through gameplay code. Platform SDK code stays entirely
  inside its define.
- Feature flags are **runtime**, not defines. Null-check the flag lookup before reading it — an
  absent flag is a normal state.

---

## 9. Verification — there is no test suite

"Verified" means **a human checked it in the editor**. Before claiming a task is done:

1. It compiles.
2. You listed **every** manual editor step (prefab/scene wiring, asset creation, pool rows,
   new `Data/` assets).
3. You stated **what to test manually**: which scene, which action, what to expect.
4. Debug/cheat tooling is the de-facto QA harness: for a hard-to-reach state, prefer extending it
   over ad-hoc debug code — and keep that code strippable from release builds.

Report with [templates/completion-summary.md](templates/completion-summary.md).

---

## 10. Pre-flight checklist

Before editing:

- [ ] Read [WORKFLOW.md](WORKFLOW.md) and [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).
- [ ] Read the `namespace` line of neighboring files — **never infer a namespace from the folder
      path**; mappings drift in this repo.
- [ ] Search existing usages before changing any signature or behavior.
- [ ] Is this file runtime or editor-only? Generated? Third-party/read-only?
- [ ] Which serialization surface do the fields belong to? (§1)
- [ ] Prefer existing managers / factories / pools / `RoutineManager` over duplicated logic.
- [ ] Null-check every lookup result into a temp var before member access. (§4.3)
- [ ] If docs and code disagree — **trust the code** and update the docs.
- [ ] If behavior is unclear — **document the uncertainty**, don't invent architecture.

Before modifying any serialized class specifically:

1. **Which surface?** Unity YAML → `[FormerlySerializedAs]` for renames. Generated → don't touch,
   change the source.
2. **Who references the field?** You can't reliably grep prefabs for field values — assume every
   prefab/scene/asset using the component has data in every serialized field.
3. **Is the type an enum used in data?** Append-only. Is it an SO "enum" asset? Deleting breaks
   references.
4. **Does it need editor wiring?** List it as a manual step.
5. **Added editor-only code?** `Editor/` folder or `#if UNITY_EDITOR` around **both** the `using`
   and the usage.
6. **Verification is manual.** State the scene / object / action.
