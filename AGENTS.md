# AGENTS.md — Space-RTS

Instructions for every AI/code agent working in this repository (Claude Code, Cursor, Codex,
Copilot Workspace, Aider, and any sub-agent they spawn).

## These rules are mandatory

Before the first edit of every task, read — in this order:

1. **[Docs/Agents/WORKFLOW.md](Docs/Agents/WORKFLOW.md)** — the required 5-phase process
   (Orient → Plan → Implement → Self-check → Report) and its gates.
2. **[Docs/Agents/PROJECT_CONTEXT.md](Docs/Agents/PROJECT_CONTEXT.md)** — Unity 2022.3.62f2 / URP,
   the `ServiceProvider` DI entry point, the boot chain, pooling, routines, namespaces.
3. **[Docs/Agents/UNITY_RULES.md](Docs/Agents/UNITY_RULES.md)** — the Unity rule set:
   serialization, prefabs/meta, architecture, performance, code style.
4. **[Docs/Agents/GIT_RULES.md](Docs/Agents/GIT_RULES.md)** — what you may do to the repository:
   agents don't commit, `.meta` pairing, Unity YAML conflicts, the handoff checklist.

Finish every task with the report format in
**[Docs/Agents/templates/completion-summary.md](Docs/Agents/templates/completion-summary.md)**.
Full index: [Docs/Agents/README.md](Docs/Agents/README.md).

Following these documents is not optional and not conditional on task size. A one-line change
still goes through the checklist — the silent breakages they guard against are exactly the ones
that look like one-line changes.

## Non-negotiables (the rest is detail)

1. **Agents change C# only.** Never hand-edit `.prefab`, `.unity`, `.asset` YAML; never create,
   delete or regenerate a `.meta`; never touch `ProjectSettings/`, `Packages/manifest.json`, or
   `Assets/Data/Input/PlayerInputMaps.cs` (generated).
2. **Never rename a serialized field** without `[FormerlySerializedAs("_oldName")]` — kept
   forever. Never reorder or delete C# enum members; append only. Never change a serialized
   field's type — add a new field instead.
3. **Adding a `[SerializeField]` does not finish the job.** It is `null` until a human wires it:
   null-guard it, and list the wiring as a manual editor step.
4. **Every manual editor step goes in the completion summary.** No list = the task is not done.
5. **Reuse the existing systems** — `ServiceProvider` (bindings in
   `Assets/Scripts/Management/ProviderBuilder.cs`), `PoolManager`, `RoutineManager`,
   `AbstractInitilizer`, the factories. No new singletons, no new static helpers, no raw
   `StartCoroutine`, no per-shot `Instantiate`/`Destroy`.
6. **Match the surrounding code, including its warts.** Some misspellings are load-bearing and
   stay: the type names whose file name they must match (`AbstractInitilizer`, `ShipInitilizer`,
   `WeaponInitilizer`, `GameInitiliazer`, `NoneLazySingletone`, `QuequeStateMachine`,
   `AdvanceWeaponTargeter`, `IStopable`) and the `Assets/Scripts/Extentions/` folder — renaming
   any of them means moving a file and its `.meta`, which is a human step in Unity. The
   `HalloGames.Architecture.Singletones` namespace is still misspelled too, and is left alone by
   choice. Commented-out code and `//HACK` markers stay. No drive-by refactors, no unrequested
   cleanup.
7. **There is no test suite.** Verification is a human in the editor — state the scene, the
   action and the expected result, every time.
8. **Agents change files, humans change history.** Never `git commit`, `add`, `push`, `pull`,
   `merge`, `rebase`, `reset`, `checkout`, `restore`, `clean`, `stash`, `revert`, `cherry-pick`,
   `tag` or `--amend` — not even when asked in passing. Read-only git (`status`, `diff`, `log`,
   `branch`, `show`, `blame`) is free and encouraged. Report what changed and stop — the agent
   does not advise on commits, messages, branches or merges either.
   **This one is machine-enforced**, not just documented: `.claude/settings.json` denies those
   commands on both the `Bash` and `PowerShell` tools and adds a `PreToolUse` hook that catches
   chained and flagged forms (`cd x && git commit`, `git -C . reset --hard`). If you hit a
   denial, that is the rule working — report it and hand the command to the human, do not look
   for a way around it. See [GIT_RULES.md §0](Docs/Agents/GIT_RULES.md).
9. **When docs and code disagree, the code wins** — and you update the doc in the same change.

## Scope

- Runtime code: `Assets/Scripts/**` (single `Assembly-CSharp`, no asmdefs).
- Read-only: `Assets/SpaceSkies Free/**`, `Library/`, `Temp/`, `obj/`, package caches.
- Gameplay scene: `Assets/Scenes/SampleScene/SampleScene.unity`.
