---
name: git-commit
description: Collect the net change, split it into logical commits, and commit each with a Conventional Commits message.
disable-model-invocation: true
---

# git-commit

Commit the paths named in the request, or everything uncommitted. A commit describes the **net
change against HEAD**: work that was edited and then undone never enters it, and an earlier
attempt that a later edit replaced never enters it either.

## 1. Collect the context

```bash
node "<this skill's directory>/collect-context.mjs"    # groups, counts, budgeted diff
node … --summary                                       # counts and groups only
node … --path <file>                                   # one path; repeatable
node … --staged                                        # only what the index holds
```

Run this instead of `git status` and `git diff`, and do **not** open changed files to find out
what changed. The report already carries the net diff, capped at 60 lines per file and 400 lines
in total. It prints, per changed path: status, kind, line counts; one group per area with a
suggested `type` and `scope`; and a NOTES block for the cases that need care.

Read NOTES before staging anything — each entry is a way to commit the wrong thing:

- **Index and worktree both changed** — the report shows the worktree, while the index holds an
  older version of that file. Commit the worktree unless you deliberately pass `--staged`.
- **Nothing to commit for a path** — a path named in the request that is back in step with HEAD.
  Do not stage it; say so in the reply.
- **Whitespace-only, binary, unmerged** — decide them explicitly; do not let them ride along
  inside a group.
- **Generated paths** (`bin/`, `obj/`, …) that are tracked and have changed — normally a mistake
  from running a build. Leave them out and mention it.

If the budget elided a diff, re-run with `--path <file>` for that file only. Never page through
a file to reconstruct a diff the report already summarised.

## 2. Split into logical commits

The report groups by area. **One group is one commit**, unless the request says otherwise.

- Keep a group together when it is one change: project code with its tests and its docs.
- Split groups that share no reason, such as a skill change and a parser fix.
- Order commits so each stands alone — build and config before the code that needs it, tests
  with the code they cover, docs last.
- If a group swallows a stray unrelated edit, move that file into its own commit instead of
  bundling it.

Ask only when the grouping is genuinely ambiguous.

## 3. Verify

Two gates, both before any commit is created.

**Build gate** — if a commit's change set contains `.cs` or `.csproj` files, run the
`csharp-build-verify` skill first. A failed build means no commit — show the errors and stop. A
docs-only commit needs no build.

**Publication gate** — the commit is public text, so the staged content must contain no
machine-specific identifier. Run the repository's gate from the repository root:

Run in Bash:
```bash
bash local_dev/scripts/scan-publication-leaks.sh            # staged changes
```

Exit 0 is a pass. Exit 1 lists the offending lines: rewrite them before committing. `FAIL` lines are
identity or mount leaks — a profile path, a home directory, a mounted drive tree — and always mean
no commit. `warn` lines are drive-letter paths: keep a deliberate illustration such as a documented
example file name, rewrite anything that only exists on one machine. The replacement is
`%USERPROFILE%\…` on Windows, `~/…` on Linux and macOS, or the repository root variable
(`$REPO_ROOT`, exported by `local_dev/scripts/repo-env.sh`). A leak that was already in the file is
removed in the commit that touches it, never carried forward. When the fix changes the content of a
file the commit's message describes, re-check the message against §4.

## 4. Message

```
type(scope): imperative subject

Optional body: why the change was made, wrapped at 72 characters.
```

- **type** — `feat`, `fix`, `docs`, `refactor`, `test` or `chore`.
- **scope** — the project or area (`parser`, `build`, `docs`); omit it when the change spans the
  repository.
- **subject** — imperative ("Add", not "Added"), under 50 characters, no trailing period, and
  specific: never "update", "various improvements" or "misc fixes".

```
fix(parser): keep the extension when sanitizing a name
docs: describe the publish script in the README
```

The message describes the change that lands, not the route to it: leave out edits that were
reverted, false starts, and what the previous attempt looked like.

### Never include

A commit message is published with the repository, so treat it as public text.

- **No machine or account identifiers.** Never a user folder or profile path, home directory,
  hostname or account name — not even partially masked or abbreviated.
- **No removed or redacted content.** Never name or describe what a cleanup removed, anonymised or
  replaced, and not as a category either. Say that detail was tightened, not what it was.
- **No restating the reason in sensitive terms.** Name the document or area, not the content.

## 5. Commit

Stage one group at a time — `git add -- <paths>`, or `git add -A -- <paths>` when the group
deletes files — then commit with `-m`, or a heredoc when there is a body.

- Never `git add -A` or `git commit -a` across groups: that is what merges unrelated changes.
- Never amend, never pass `--no-verify`.
- Commit each group separately, then report every subject line, in the order committed.
