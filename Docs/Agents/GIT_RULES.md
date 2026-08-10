# Git rules for AI agents — Space-RTS

Companion to [UNITY_RULES.md](UNITY_RULES.md) (what you may change in the code).
**This file is about what you may do to the repository.**

> **Short version: agents change files, humans change history.**
>
> This file constrains the **agent**. It is not advice for the human on how to use git —
> branching, commit messages, merge strategy and repo setup are the human's business and are
> deliberately not covered here.

---

## 0. The core rule: agents do not commit

**An agent never runs `git commit`, `git push`, `git merge`, or anything else that writes to
history or the remote — not even when asked in passing.**

The division of labour is fixed:

| Agent | Human |
|---|---|
| Edits the working tree | Reviews the diff |
| Reads git state (`status`, `diff`, `log`, `branch`) | Decides what to stage, commit and push |
| Reports what it changed and what was already dirty | Runs git |

Why this is absolute in a Unity repo: an agent cannot see the editor's unsaved scene state,
cannot verify that a `.meta` twin is correct, and cannot tell reimport churn from a real change.
Every one of those produces a commit that looks clean and is not. The human running the editor is
the only one with that information.

If the user asks for a commit: **say that this rules file forbids it, and stop.** Repeated or
emphatic requests do not change the rule — they mean the rule should be edited here first,
deliberately, by the user.

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
| **Never delete, discard or shelve uncommitted work** — no `checkout --`, `restore`, `clean -fd`, and no `stash` at all | Unity's editor holds unsaved scene/prefab state; discarded work is often unrecoverable, and a stash the human did not ask for hides changes they were about to review |
| **Never touch repo setup** — `.gitattributes`, `.gitignore`, `git config`, remotes, LFS tracking | Repo-wide side effects (re-normalization, history rewrite). Report drift; changing it is the human's call |
| **No PR/hosting operations** — `gh` is not installed and must not be worked around | The remote is `FuRRaSHKa/Space-RTS` on GitHub; everything there is human-driven |
| **Interactive commands are unavailable** — no `rebase -i`, `add -i`, nothing that opens an editor | The agent has no TTY; they hang |

**Read-only git is always allowed** and should be used freely: `status`, `diff`, `log`, `show`,
`branch`, `blame`, `stash list`. When in doubt, ask.

---

## 1. This repository's setup — facts the agent needs

Checked against the repo. All of it is **read-only context**: report drift, never fix it.

| Item | State here | What it means for you |
|---|---|---|
| Asset serialization | `m_SerializationMode: 2` (**Force Text**) ✅ | Diffs and conflicts in YAML assets behave as §3 describes. If this ever changes, §3 stops applying — flag it loudly |
| `.gitignore` | Official Unity ignore + project entries + the AI-tooling section | Extend only when explicitly asked |
| `.gitattributes` | Only `* text=auto` — no Unity YAML merge driver | §3.1 option 1 is unavailable here |
| UnityYAMLMerge driver | Not configured in git config | Same |
| Git LFS | Installed, **zero files tracked**; `.fbx`/`.png` live in history as ordinary blobs | Never add LFS patterns on your own initiative — retro-tracking needs a history rewrite |
| Hooks / CI | None | "CI is green" is not a checkbox here — manual editor verification is |

---

## 2. The `.meta` rule

**Every asset file has a `.meta` sibling holding its GUID. They travel together, always.**

| Operation | Correct git action |
|---|---|
| Add an asset | `Foo.png` **and** `Foo.png.meta` land together |
| Add a folder | The folder's own `Foo.meta` too — Unity generates one per folder |
| Delete an asset | Delete `Foo.png` **and** `Foo.png.meta` |
| Move / rename an asset | Move both; **never** regenerate the `.meta` |

An asset without its `.meta` makes Unity generate a *new* GUID on every other machine → every
reference to that asset breaks. A `.meta` without its asset leaves an orphan Unity deletes on the
next import, so the fix silently reverts itself.

**Whenever `Assets/` was touched, run this before handing work back and report the result:**

```bash
git status --porcelain Assets/ | sort
```

Each non-`.meta` path under `Assets/` must have its `.meta` twin in the same list, and vice
versa. Deleted folders must show their folder `.meta` as deleted too. Any mismatch goes in the
summary as a **blocking** note.

Note for agents in this repo: you create C# files, and **you do not create their `.meta`** —
Unity does, on the next editor focus. So right after your change the `.meta` twin is legitimately
absent. Say so explicitly rather than presenting it as a problem.

---

## 3. Merge conflicts in Unity files

An agent never starts a merge (§0). This section applies only when a merge is already in progress
and the agent is asked to help.

### 3.1 `.unity` / `.prefab` / `.asset` — do not hand-resolve

These are line-based-hostile YAML: fileIDs, ordering and nested references make a "looks correct"
textual merge produce a corrupt scene that opens with no error and silently loses objects.

1. `git mergetool` with UnityYAMLMerge — **unavailable here** (§1).
2. Take one side whole (`--ours` / `--theirs`) — but the agent may not run those commands (§0),
   so this is a hand-off, not an action.
3. An agent must **not** merge YAML hunks by hand.

In practice: stop, report the conflicting files, hand it over.

### 3.2 `.meta` conflicts

A conflict inside a `.meta` almost always means **two GUIDs for one asset** — the same file was
added independently on both sides. Report which GUID is already referenced by committed
prefabs/scenes; do not pick one silently.

### 3.3 C# conflicts

Normal textual merge — but after resolving, re-read
[UNITY_RULES.md §1](UNITY_RULES.md#1-serialization-surfaces--identify-the-surface-first): a merge
that reintroduces a renamed serialized field, or drops a `[FormerlySerializedAs]`, is a data-loss
bug the compiler will not catch.

---

## 4. What must never be left in the working tree

You will not be the one committing (§0) — but you **are** responsible for not leaving these
behind, and for naming them in your summary if they are already there.

- `Library/`, `Temp/`, `obj/`, `Logs/`, `Build/`, `UserSettings/`, `.vs/`.
- Regenerated project files: `*.csproj`, `*.sln`, `*.user`.
- Build outputs and archives: `*.apk`, `*.aab`, `*.ipa`, `*.unitypackage`, `*.zip`.
- **Secrets and signing material:** keystores, `.p12`/`.mobileprovision`, API keys,
  service-account JSON, store passwords, analytics/backend tokens. If you see one already
  tracked, **report it** and stop there.
- Personal editor state and local AI-tool config (the `.gitignore` AI-tooling section covers the
  common folders; if your tool writes somewhere else, report it).
- **Unrelated files.** Unity rewrites `Packages/packages-lock.json`, `ProjectSettings/*` and
  `.csproj` files just by opening the project. If `git status` shows changes you did not make,
  **leave them alone and say so.**

```bash
git status --porcelain
```

If it contains files you cannot explain in one sentence each, say so explicitly — separating your
paths from what was already dirty is the whole point of the report.

---

## 5. Reading the repository (always safe, always first)

```bash
git status                          # what is dirty, which branch
git log --oneline -20               # what has been happening here
git diff                            # unstaged changes — including ones you did not make
git diff --cached                   # what is already staged
git branch -a
```

Run these at the start of a task, before the first edit, so you can tell your own changes from
pre-existing ones at the end.

---

## 6. Dangerous command reference

| Command | Risk | Agent policy |
|---|---|---|
| `commit` / `add` / `rm --cached` | Writes history the agent cannot verify (§0) | **Never** |
| `push` / `merge` / `cherry-pick` / `revert` / `tag` | Same, plus affects the remote | **Never** |
| `push --force` / `--force-with-lease` | Destroys remote history | **Never** |
| `reset` (any form) | Discards uncommitted work or rewrites history | **Never** |
| `clean -fd` / `-fdx` | Deletes untracked files; `-x` nukes `Library/` and forces a full reimport | **Never** |
| `checkout -- <path>` / `restore` / `switch` | Silently discards edits; branch switches trigger a reimport | **Never** |
| `rebase` / `commit --amend` | Rewrites history | **Never** |
| `stash` (any form) | Hides work the human was about to review | **Never** |
| `filter-branch` / `filter-repo` / BFG / `lfs migrate` | Repo-wide history rewrite | **Never** |
| `git gc --prune=now` | Drops recoverable objects | **Never** |
| `config` / `remote` / `.gitattributes` edits | Repo-wide setup change | **Never** — report instead |

All of the above are denied in `.claude/settings.json` (§0.1). A denial is the expected outcome,
not a problem to solve.

---

## 7. Handoff

The agent does not commit (§0) — it produces the working-tree change plus this checklist, filled
in, and stops. It attaches to the [completion summary](templates/completion-summary.md).

- [ ] `git status --porcelain` reviewed; **my paths named, pre-existing dirt named separately**.
- [ ] No `Library/`, build output, `*.csproj`/`*.sln`, secrets, or unrelated files among my changes.
- [ ] Every asset I touched under `Assets/` has its `.meta` twin (§2) — or the missing `.meta` is
      called out as "Unity will generate it on next editor focus".
- [ ] No Unity YAML file was hand-edited or hand-merged (§3.1).
- [ ] Serialized-field / enum changes carry their migration
      ([UNITY_RULES.md §1](UNITY_RULES.md#1-serialization-surfaces--identify-the-surface-first)).
- [ ] Manual editor steps and manual test steps are in the summary — they are the only
      verification this project has.

### Working-tree report block for the summary

```markdown
## Working tree
- Changed by me: `<paths>`
- Already dirty before I started (not mine, untouched): `<paths | none>`
- `.meta` status: `<all present | Foo.cs.meta will be generated by Unity on next editor focus>`
```

State the facts and stop there. Do not propose commit commands, messages, branch names or any
other git procedure — what to do with these changes is the human's decision, not the agent's
suggestion.

---

## 8. Keeping this file true

Re-derive and update when any of these change: `.gitattributes` / LFS / merge-driver setup, hooks
or CI, or repo-specific "do not touch" paths (today: `Assets/SpaceSkies Free/**`,
`Assets/Data/Input/PlayerInputMaps.cs`, `Packages/`, `ProjectSettings/`).

**§0.1 and `.claude/settings.json` are one unit** — if the deny list or the hook changes, this
document changes in the same edit, and vice versa. A ban that is written here but not enforced,
or enforced but not explained, is worse than either alone.

Keep it lean, and keep it about the agent. Delete rules that stop being true.
