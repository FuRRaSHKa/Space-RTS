# CLAUDE.md

**Read [AGENTS.md](AGENTS.md) first, at the start of every task, and follow it.** It is the
entry point to the agent rules in [Docs/Agents/](Docs/Agents/README.md):

- [Docs/Agents/WORKFLOW.md](Docs/Agents/WORKFLOW.md) — the required process and its gates
- [Docs/Agents/PROJECT_CONTEXT.md](Docs/Agents/PROJECT_CONTEXT.md) — this project's systems and conventions
- [Docs/Agents/UNITY_RULES.md](Docs/Agents/UNITY_RULES.md) — the Unity rule set
- [Docs/Agents/GIT_RULES.md](Docs/Agents/GIT_RULES.md) — what you may do to the repository
- [Docs/Agents/templates/completion-summary.md](Docs/Agents/templates/completion-summary.md) — the report format

Those documents override any general habit or default style. If something here and something
there disagree, `Docs/Agents/` wins; if the docs and the code disagree, the code wins and the
doc gets fixed.

## Project at a glance

Space-RTS — a real-time strategy prototype in **Unity 2022.3.62f2**, URP, new Input System,
no asmdefs (everything in `Assembly-CSharp`), no tests, no save system.

- Composition root: `Assets/Scripts/Management/ProviderBuilder.cs` → `RegisterServices()`
- Reusable engine layer: `Assets/Scripts/Architecture/**` (`HalloGames.Architecture.*`)
- Game code: `Assets/Scripts/Gameplay/**`, `Assets/Scripts/Management/**` (`HalloGames.SpaceRTS.*`)
- Configs: `Assets/Scripts/SO/**` (types) + `Assets/Data/**` (assets)
- Gameplay scene: `Assets/Scenes/SampleScene/SampleScene.unity`

Namespaces do **not** follow folder paths — copy the `namespace` line from a neighbouring file.

## Reminders that bite most often here

- C# only — never hand-edit `.prefab` / `.unity` / `.asset` / `.meta`.
- New `[SerializeField]` = a manual wiring step for a human; list it and null-guard it.
- Renaming a serialized field needs `[FormerlySerializedAs]`; enums are append-only.
- Use `RoutineManager` for coroutines and `PoolManager` for anything spawned repeatedly.
- **Do not commit, stage or push — ever, including when asked directly.** This overrides the
  usual "commit when the user asks" default: in this repo the agent edits the working tree and
  reports what changed; git itself is the human's business. The ban is enforced by `.claude/settings.json`
  (deny rules + a `PreToolUse` hook), so a refused git command is the rule working correctly —
  report it, don't route around it. Details and the sanctioned way to change it:
  [GIT_RULES.md §0](Docs/Agents/GIT_RULES.md).
- End every task with the completion-summary format, including *Manual editor steps*,
  *How to test manually* and the *Working tree* block.
