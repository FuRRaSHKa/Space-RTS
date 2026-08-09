# Agent workflow — Space-RTS

The mandatory process for any code task in this repository. Five phases, each with a gate you
must pass before moving on. Skipping a gate is the failure mode this document exists to prevent.

```
1 ORIENT  →  2 PLAN  →  3 IMPLEMENT  →  4 SELF-CHECK  →  5 REPORT
   ↑                                          │
   └──────────── found a wrong assumption ────┘
```

---

## Phase 1 — Orient (before touching anything)

0. Run `git status` and `git log --oneline -20`. Know what was already dirty **before** you
   touched anything — you will have to separate your changes from pre-existing ones in Phase 5,
   and Unity dirties `Packages/`, `ProjectSettings/` and `*.csproj` on its own.
1. Read [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) — engine version, DI entry point, existing
   systems, what does *not* exist here.
2. Read the sections of [UNITY_RULES.md](UNITY_RULES.md) your task touches (serialization for
   data changes, §4 for new systems, §5 for anything in `Update`).
3. Locate the code. Read the **whole** file you are about to change plus its callers — not just
   the matched lines.
4. Read the `namespace` line of a neighbouring file. Never infer it from the folder path.

**Gate:** you can name, out loud, (a) which files you will edit, (b) which serialization surface
they touch, (c) which existing system already does 80% of what you need, (d) what was already
dirty in the working tree before you started.

---

## Phase 2 — Plan

1. Prefer extending an existing system (`ServiceProvider`, `PoolManager`, `RoutineManager`,
   `AbstractInitilizer`, factories) over introducing a new one. A new singleton or a new static
   helper is almost always the wrong answer here.
2. Decide the **serialization impact** up front:
   - New `[SerializeField]` → who wires it, in which prefab/asset?
   - New ScriptableObject type → who creates the asset(s)?
   - New pooled prefab → a `PoolPair` row on `PoolManager` in the scene.
   - Renaming an existing serialized field → `[FormerlySerializedAs]`, kept forever.
3. Keep the change **focused**. Unrelated cleanup, renames and "modernization" are out of scope
   by default — collect them for the *Flagged, not fixed* section instead.
4. If two readings of the task lead to materially different work, ask **before** implementing.
   Otherwise pick the sensible reading, state the assumption in the summary, and continue.

**Gate:** the plan lists every manual editor step it will create. If that list is empty, be sure
it is really empty.

---

## Phase 3 — Implement

- **C# only.** No hand-edits to `.prefab`, `.unity`, `.asset`, `.meta`, `ProjectSettings/`,
  `Packages/manifest.json`, or `Assets/Data/Input/PlayerInputMaps.cs`. If the task genuinely
  requires one of those, stop and say so — that part is a human step.
- Match the house style of the file you are in ([UNITY_RULES.md §6](UNITY_RULES.md#6-code-style)):
  `[SerializeField] private _camelCase`, guard clauses, aligned declaration groups, small
  single-purpose methods, existing spelling quirks preserved.
- Null-guard every new `[SerializeField]` and every lookup result before member access.
- New service? Register it in `ProviderBuilder.RegisterServices()` in the same change.
- New pooled/spawned object? Reset its state in `EnableObject()` / `DisableObject()` and stop any
  running `IStopable` routine.
- Never delete or rewrite commented-out code, `//HACK` markers, or anything unrelated to the task.

**Gate:** every file you touched is one you read in full first.

---

## Phase 4 — Self-check

Walk this list literally; it is short on purpose.

- [ ] **Compiles.** No `UnityEditor` reference from runtime code (or it is inside
      `#if UNITY_EDITOR`, `using` line included).
- [ ] **Serialization:** no renamed field without `[FormerlySerializedAs]`, no reordered enum,
      no changed field type without a new field.
- [ ] **Nulls:** every new serialized reference is guarded; no member access on an unchecked
      lookup result.
- [ ] **Lifecycle:** every `+=` has a matching `-=` on the same method reference, in the
      symmetric callback.
- [ ] **Pooling:** nothing new is `Instantiate`d/`Destroy`ed per frame or per shot; reused
      objects reset all mutable state.
- [ ] **Perf:** no LINQ / closures / allocations / `GetComponent` in `Update` or hot loops.
- [ ] **Scope:** the diff contains nothing the task did not ask for.
- [ ] **Git:** `git status --porcelain` reviewed — you can name every changed path and separate
      yours from what was already dirty. Nothing was staged, committed or pushed.
      ([GIT_RULES.md §0, §4](GIT_RULES.md))
- [ ] **`.meta`:** if `Assets/` changed, `git status --porcelain Assets/ | sort` shows each asset
      with its `.meta` twin — or the gap is explained (new `.cs` files get their `.meta` from
      Unity on next editor focus). ([GIT_RULES.md §2](GIT_RULES.md#2-the-meta-rule))
- [ ] **Docs:** if you discovered that `PROJECT_CONTEXT.md` or `GIT_RULES.md` is wrong, you fixed it.

**Gate:** any unchecked box is either fixed or explicitly reported in Phase 5.

---

## Phase 5 — Report

Answer with [templates/completion-summary.md](templates/completion-summary.md), filled in.
Three sections are non-negotiable:

- **Manual editor steps** — the task is *not done* without them; the human has to wire prefabs,
  create assets, add pool rows. List each one as a checkbox with the exact object and field.
- **How to test manually** — scene, action, expected result. There is no test suite; this is the
  only verification that exists.
- **Git handoff** — your paths vs. pre-existing dirt, `.meta` status, and a **proposed** commit
  command in the project's message style. You do not run it
  ([GIT_RULES.md §9](GIT_RULES.md#9-handoff-checklist)).

Say plainly what you did not do and why. A skipped part reported honestly is fine; a skipped part
implied to be done is not.

---

## Working agreements

**When you are unsure**
Document the uncertainty in the summary; don't invent architecture to paper over it. If
proceeding either way would be unsafe or would waste the work if wrong, ask instead.

**When docs and code disagree**
The code wins. Fix the doc in the same change and mention it.

**When you find an unrelated bug or a rule violation**
Flag it in *Flagged, not fixed*. Do not repair it in a drive-by edit — a focused diff is worth
more than an opportunistic fix.

**When the task needs editor work you cannot do**
Do the entire C# side, then hand over a precise, ordered checklist. "Wire it up" is not a
checklist; "`Prefabs/Ships/SmallShip.prefab` → `ShipWeaponsController._muzzlePoints` → drag the
two `Muzzle_L/R` transforms" is.

**Multi-agent / parallel work**
Every agent, main or sub, follows this same document. Sub-agents doing read-only exploration
report file paths and findings, not opinions about style; the agent that writes code is the one
responsible for Phase 4 and Phase 5.

**Commits**
Never — not even when explicitly asked. Agents change files, humans change history: propose the
`git add` / `git commit` commands and the message, and stop there. Full rules and the reasoning:
[GIT_RULES.md](GIT_RULES.md).
