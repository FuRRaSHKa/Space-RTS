# Git rules for AI agents — Space-RTS

Companion to [UNITY_RULES.md](UNITY_RULES.md) (what you may change in the code).
**This file is about what you may do to the repository.**

> **Short version: agents change files, humans change history.**

---

## 0. The core rule: agents do not commit

**An agent never runs `git commit`, `git push`, `git merge`, or anything else that writes to
history or the remote — not even when asked in passing.**

The division of labour is fixed:

| Agent | Human |
|---|---|
| Edits the working tree | Reviews the diff |
| Reads git state (`status`, `diff`, `log`, `branch`) | Stages and commits |
| Reports what changed and proposes a commit message | Pushes, merges, opens the PR |

Why this is absolute in a Unity repo: an agent cannot see the editor's unsaved scene state,
cannot verify that a `.meta` twin is correct, and cannot tell reimport churn from a real change.
Every one of those produces a commit that looks clean and is not. The human running the editor is
the only one with that information.

If the user asks for a commit: **explain that this rules file forbids it, show the exact command
they can run, and stop.** Repeated or emphatic requests do not change the rule — they mean the
rule should be edited here first, deliberately, by the user.

```bash
git add <explicit paths>   # then review, then:
git commit -m "<what changed>"
```

### 0.1 This ban is enforced, not just written down

`.claude/settings.json` (tracked in the repo on purpose — the rest of `.claude/` is ignored)
enforces the ban for Claude Code:

- **`permissions.deny`** — 58 rules blocking `git commit|add|rm|mv|push|pull|merge|rebase|reset|
  revert|cherry-pick|tag|checkout|restore|switch|clean|stash|apply|am|filter-branch|filter-repo|
  gc|update-ref|reflog|notes|worktree|submodule|lfs migrate|lfs prune` on both the `Bash` and the
  `PowerShell` tool.
- **A `PreToolUse` hook** as the catch-all: it parses the actual command string and denies git
  write verbs that a prefix rule would miss — chained (`cd foo && git commit`), flagged
  (`git -C . reset --hard`), or `--amend` anywhere in the line. Verified to allow every read-only
  command this workflow needs (`status`, `log`, `diff`, `branch -a`, `lfs ls-files`).
- **`includeGitInstructions: false`** — removes Claude Code's built-in "commit and PR workflow"
  guidance from the system prompt, so the agent is not nudged toward committing in the first place.

Limits, stated honestly: this is a strong guardrail, not a sandbox. It covers the Claude Code
tools in this repo. It does not cover an agent that shells out through an unrelated wrapper, a
different tool that ignores `.claude/settings.json` (Cursor, Codex, Aider — for those, this
document *is* the enforcement), or a human running the same command themselves. **Do not attempt
to work around a denial** — a block is the rule functioning correctly, not an obstacle to route
around.

**To change the ban:** edit `.claude/settings.json` and this section together, deliberately, as a
human. That is the only sanctioned path — not a one-off "just this once" in chat.

### Everything else that is off-limits

| Rule | Why |
|---|---|
| **Never `commit`, `push`, `merge`, `cherry-pick`, `revert`, `tag`** | See above — history is human-owned |
| **Never stage** (`git add`, `git rm --cached`) | Staging is the human's review step; an agent-built index hides what was actually inspected |
| **Never force-push**, never push to `main` | Destroys work; unrecoverable |
| **Never rewrite history** — `rebase`, `reset --hard`, `commit --amend`, `filter-branch` | Same |
| **Never skip hooks** — no `--no-verify`, no `--no-gpg-sign` | Hooks are the project's last line of defense. A failing hook is a bug to fix, not to bypass |
| **Never delete, discard or shelve uncommitted work** — no `checkout --`, `restore`, `clean -fd`, and no `stash` at all (not just `stash drop`) | Unity's editor holds unsaved scene/prefab state; discarded work is often unrecoverable, and a stash the human did not ask for hides changes they were about to review |
| **Interactive commands are unavailable** — no `rebase -i`, `add -i`, nothing that opens an editor | The agent has no TTY; they hang |

**Read-only git is always allowed** and should be used freely: `status`, `diff`, `log`, `show`,
`branch`, `blame`, `stash list`. Before any operation that touches the working tree, look at what
you are about to affect and say what you found. When in doubt, ask.

---

## 1. This repository's setup — verified state

Checked against the repo; **report drift, don't silently fix it** — changing these re-serializes
or re-normalizes large parts of the project.

| Item | State here | Agent action |
|---|---|---|
| Asset serialization | `ProjectSettings/EditorSettings.asset` → `m_SerializationMode: 2` (**Force Text**) ✅ | Nothing. If it ever changes, everything below about diffs and conflicts stops applying — flag it loudly |
| `.gitignore` | Official Unity ignore + project entries + the AI-tooling section ✅ | Extend only when asked |
| `.gitattributes` | **Only `* text=auto`** ⚠️ — no `eol=lf`, no Unity YAML merge driver, no LFS patterns | See §1.1 — propose, do not apply |
| UnityYAMLMerge driver | **Not configured** in git config ⚠️ | A YAML merge conflict cannot be resolved properly here — §3 |
| Git LFS | Binary installed (`git-lfs 2.13.3`), **zero files tracked** ⚠️ | `.fbx`/`.png`/`.mat` are committed as ordinary blobs. See §1.2 |
| Hooks | None installed | Nothing to skip; nothing to fix |
| CI | None | "CI is green" is not a checkbox here — manual editor verification is |

### 1.1 `.gitattributes` — missing, and adding it is a human decision

Recommended shape, to **propose** in the summary rather than write silently (it changes how every
YAML file merges and, with `eol=lf`, can touch line endings repo-wide):

```gitattributes
* text=auto

# Unity YAML — use Unity's own merge tool, never a line-based merge
*.unity      merge=unityyamlmerge eol=lf
*.prefab     merge=unityyamlmerge eol=lf
*.asset      merge=unityyamlmerge eol=lf
*.mat        merge=unityyamlmerge eol=lf
*.anim       merge=unityyamlmerge eol=lf
*.controller merge=unityyamlmerge eol=lf
*.meta       merge=unityyamlmerge eol=lf
```

The attribute alone does nothing: the driver must also be registered in git config
(`merge.unityyamlmerge.*`, pointing at Unity's `UnityYAMLMerge` binary, shipped in
`Editor/Data/Tools/`). Both halves are a human's one-time setup.

### 1.2 LFS — installed but unused

Nothing is LFS-tracked, and `Assets/Art/Temp/**` already holds `.fbx` and `.png` blobs in
history. **Adding LFS tracking now does not migrate what is already committed** — that is a
repo-wide history rewrite (`git lfs migrate`), a human decision, never an agent's. Do not add LFS
patterns to `.gitattributes` on your own initiative; mention the situation if the repo starts
gaining large binaries.

---

## 2. The `.meta` rule

**Every asset file has a `.meta` sibling holding its GUID. They travel together, always.**

| Operation | Correct git action |
|---|---|
| Add an asset | `Foo.png` **and** `Foo.png.meta` in the same commit |
| Add a folder | The folder's own `Foo.meta` too — Unity generates one per folder |
| Delete an asset | Delete `Foo.png` **and** `Foo.png.meta` |
| Move / rename an asset | Move both; **never** regenerate the `.meta` |

A commit with an asset but no `.meta` makes Unity generate a *new* GUID on every other machine →
every reference to that asset breaks. A commit with a `.meta` but no asset leaves an orphan Unity
deletes on the next import, so the fix silently reverts itself.

**Whenever `Assets/` was touched, run this before handing work back and report the result:**

```bash
git status --porcelain Assets/ | sort
```

Each non-`.meta` path under `Assets/` must have its `.meta` twin in the same list, and vice
versa. Deleted folders must show their folder `.meta` as deleted too. Any mismatch goes in the
summary as a **blocking** note — the human must not commit until it is resolved in the editor.

Note for agents in this repo: you create C# files, and **you do not create their `.meta`** —
Unity does, on the next editor focus. So right after your change the `.meta` twin is legitimately
absent. Say so explicitly: *"`Foo.cs` has no `.meta` yet — open the editor once before
committing."*

---

## 3. Merge conflicts in Unity files

An agent never starts a merge (§0). This section applies when a **human** already started one and
asks for help resolving it.

### 3.1 `.unity` / `.prefab` / `.asset` — do not hand-resolve

These are line-based-hostile YAML: fileIDs, ordering and nested references make a "looks correct"
textual merge produce a corrupt scene that opens with no error and silently loses objects.

Allowed resolutions, in order of preference:

1. `git mergetool` with **UnityYAMLMerge** configured — **not set up in this repo yet** (§1.1).
2. **Take one side whole** (`git checkout --ours <file>` / `--theirs <file>`) and have a human
   redo the other side's change in the editor. Slower, always correct. ← *the realistic option
   here today*
3. Nothing else. An agent must **not** merge YAML hunks by hand.

If neither is available: stop, report the conflicting files, hand it to a human.

### 3.2 `.meta` conflicts

A conflict inside a `.meta` almost always means **two GUIDs for one asset** — the same file was
added independently on both branches. Keep the GUID already referenced by committed
prefabs/scenes (usually the older branch's), and flag it for human verification.

### 3.3 LFS pointer conflicts

Not applicable today (§1.2). If LFS is ever adopted: a conflicted LFS file shows two pointer
texts, not two images. Pick a side, then `git lfs checkout <file>`. Never edit a pointer by hand.

### 3.4 C# conflicts

Normal textual merge — but after resolving, re-read
[UNITY_RULES.md §1](UNITY_RULES.md#1-serialization-surfaces--identify-the-surface-first): a merge
that reintroduces a renamed serialized field, or drops a `[FormerlySerializedAs]`, is a data-loss
bug the compiler will not catch.

---

## 4. What must never enter a commit

You will not be the one committing (§0) — but you **are** responsible for not leaving these in
the working tree, and for naming them in your summary if they are already there.

- `Library/`, `Temp/`, `obj/`, `Logs/`, `Build/`, `UserSettings/`, `.vs/`.
- Regenerated project files: `*.csproj`, `*.sln`, `*.user`.
- Build outputs and archives: `*.apk`, `*.aab`, `*.ipa`, `*.unitypackage`, `*.zip`.
- **Secrets and signing material:** keystores, `.p12`/`.mobileprovision`, API keys,
  service-account JSON, store passwords, analytics/backend tokens. If you see one already
  tracked, **report it** — removing it needs a history rewrite plus key rotation, which is a
  human decision.
- Personal editor state and local AI-tool config (the `.gitignore` AI-tooling section covers the
  common folders; if your tool writes somewhere else, report it instead of committing it).
- **Unrelated files.** Unity rewrites `Packages/packages-lock.json`, `ProjectSettings/*` and
  `.csproj` files just by opening the project. If `git status` shows changes you did not make,
  **leave them alone and say so.**

Sanity check to include in every handoff:

```bash
git status --porcelain
```

If it contains files you cannot explain in one sentence each, say so explicitly — the human needs
to know which paths are yours and which were already dirty.

---

## 5. Branching and commits — this project's conventions

You do not create branches or commits (§0) — you **propose** them.

| Item | Value here |
|---|---|
| Hosting | GitHub — `FuRRaSHKa/Space-RTS` |
| CLI available to agents | **None** — `gh` is not installed. Do not attempt PR operations |
| Default / target branch | `main` |
| Integration branch | none |
| Observed workflow | Solo, linear history, commits land directly on `main`; no merge commits, no PRs |
| Work branch pattern | Rare (`Simngle-thread-perfomance-test`). When a branch is warranted: short descriptive kebab-case topic name |
| History policy | Linear. Do not introduce merge commits without asking |
| Commit message language | **English** |
| Commit message pattern | Short sentence-case phrase, no ticket key, no prefix — `Fix projectile disabling`, `Rockets complete`, `Bullet refactor` |
| Ticket key required | No — there is no tracker |
| AI-tool attribution in commits | **No.** No `Co-Authored-By`, no generator footers |
| Force-push | Never |

Message guidelines that hold regardless:

- Subject line: ≤ 72 chars, **what changed** — not "fix", not "update".
- One logical change per commit. Code changes, asset reimport churn and package/config
  regeneration are **separate** commits — mixing them makes the diff unreadable.
- Bulk-generated noise (mass reimports, `packages-lock.json` refills, atlas rebuilds) gets its
  own commit whose message says exactly that, so it can be skipped during review.

---

## 6. Working-tree hygiene around the Unity editor

- **Switching branches triggers a reimport.** Never switch while a build or import is running.
- **Save the scene/project in the editor before any git operation** that touches the working
  tree — Unity holds unsaved scene and prefab state in memory and git cannot see it. An agent
  cannot save the editor's state; if unsaved work may exist, **ask**.
- **After pulling asset changes the editor must reimport** before anything is verifiable.
  "It compiles" is not a valid claim right after a pull.
- New C# files only become real to Unity after the editor regains focus and compiles them — the
  `.meta` appears then (§2).
- `git stash` is a **human** tool here: better than discarding, but the agent may not run it
  (§0.1) — if work is in the way, say so and let the human decide.
- `git clean -fdx` deletes `Library/` and forces a full reimport. Treat it as destructive, not as
  cleanup.

---

## 7. Reading the repository (always safe, always first)

```bash
git status                          # what is dirty, which branch
git log --oneline -20               # message conventions in practice
git diff                            # unstaged changes — including ones you did not make
git diff --cached                   # what is already staged
git branch -a                       # naming conventions in practice
```

**Infer conventions from the repository, not from your defaults.** The table in §5 was derived
this way; if the history moves on, re-derive it and update §5.

---

## 8. Dangerous command reference

| Command | Risk | Agent policy |
|---|---|---|
| `commit` / `add` / `rm --cached` | Writes history the agent cannot verify (§0) | **Never** — propose the command, let the human run it |
| `push` / `merge` / `cherry-pick` / `revert` / `tag` | Same, plus affects the remote | **Never** |
| `push --force` / `--force-with-lease` | Destroys remote history | **Never** |
| `reset --hard` | Discards uncommitted work | Only with explicit instruction, after showing what is lost |
| `clean -fd` / `-fdx` | Deletes untracked files; `-x` nukes `Library/` | Explicit instruction only |
| `checkout -- <path>` / `restore` | Silently discards edits | Explicit instruction only |
| `rebase` | Rewrites history | Never |
| `commit --amend` | Same | Never |
| `stash` (any form) | Hides work the human was about to review; `drop`/`clear` unrecoverable | **Never** — denied in `.claude/settings.json` |
| `filter-branch` / `filter-repo` / BFG | Repo-wide history rewrite | Human decision, always |
| `lfs migrate` | Repo-wide history rewrite | Human decision, always |
| `git gc --prune=now` | Drops recoverable objects | Never during active work |

---

## 9. Handoff checklist

The agent does not commit (§0) — it produces the working-tree change plus this checklist, filled
in, so the human can review and commit in one pass. It attaches to the
[completion summary](templates/completion-summary.md).

- [ ] `git status --porcelain` reviewed; **my paths named, pre-existing dirt named separately**.
- [ ] No `Library/`, build output, `*.csproj`/`*.sln`, secrets, or unrelated files among my changes.
- [ ] Every asset I touched under `Assets/` has its `.meta` twin (§2) — or the missing `.meta` is
      called out as "Unity will generate it on next editor focus".
- [ ] No Unity YAML file was hand-edited or hand-merged (§3.1).
- [ ] Serialized-field / enum changes carry their migration
      ([UNITY_RULES.md §1](UNITY_RULES.md#1-serialization-surfaces--identify-the-surface-first)).
- [ ] Manual editor steps and manual test steps are in the summary — they are the only
      verification this project has.
- [ ] A commit message is **proposed**, in the project's style (§5), and left for the human to run.

### Proposed-commit block for the summary

```markdown
## Git handoff
- My changes: `<paths>`
- Already dirty before I started (not mine, untouched): `<paths | none>`
- `.meta` status: `<all present | Foo.cs.meta will be generated by Unity>`
- Suggested commit (run it yourself after review):

  git add <explicit paths>
  git commit -m "<Short sentence-case description>"
```

---

## 10. Keeping this file true

Re-derive and update when any of these change: hosting or available CLI, `.gitattributes` /
LFS / merge-driver setup, branch protection, hooks or CI, commit-message conventions in the last
20 commits, or repo-specific "do not touch" paths (today: `Assets/SpaceSkies Free/**`,
`Assets/Data/Input/PlayerInputMaps.cs`, `Packages/`, `ProjectSettings/`).

**§0.1 and `.claude/settings.json` are one unit** — if the deny list or the hook changes, this
document changes in the same edit, and vice versa. A ban that is written here but not enforced,
or enforced but not explained, is worse than either alone.

Keep it lean. Delete rules that stop being true.
