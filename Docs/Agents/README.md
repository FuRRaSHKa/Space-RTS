# Agent documentation

Everything an AI/code agent needs to work in this repository lives here.
Human contributors may read it too — it is just the house rules, written down.

## Read order

| # | File | What it is | When to read |
|---|------|------------|--------------|
| 1 | [WORKFLOW.md](WORKFLOW.md) | The mandatory step-by-step process: pre-flight → implement → self-check → summary | **Every task, before the first edit** |
| 2 | [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) | Space-RTS specifics: Unity version, DI entry point, namespaces, systems, what does *not* exist here | Every task, together with WORKFLOW |
| 3 | [UNITY_RULES.md](UNITY_RULES.md) | The full Unity rule set — serialization, prefabs, architecture, performance, code style | Reference; the sections your task touches |
| 4 | [GIT_RULES.md](GIT_RULES.md) | What the agent may do to the repository — agents don't commit, `.meta` pairing, YAML conflicts, handoff | Any task that touches git state, and every handoff |
| 5 | [templates/completion-summary.md](templates/completion-summary.md) | The report format every task ends with | When writing the final answer |

## The short version

- **Agents change C# only.** Never hand-edit `.prefab` / `.unity` / `.asset` YAML, never touch `.meta`.
- **Agents change files, humans change history.** No `commit`, no `add`, no `push`, and no git advice — just a report of what changed. Enforced by `.claude/settings.json`, not just documented ([GIT_RULES.md §0.1](GIT_RULES.md)).
- **Additive by default.** New fields, appended enum members, old ones kept alive.
- **Every prefab/scene hookup is a manual editor step and must be listed in the summary.**
- **There are no tests.** "Verified" means a human checked it in the editor — so tell them exactly what to check.

## Maintaining these docs

- If the docs and the code disagree — **the code wins**; update the doc in the same task.
- Keep it lean. A rule that has become obvious gets deleted, not archived.
- New project-level facts (a new manager, a new define, a save system) go into
  [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md), not into the generic rule set.
