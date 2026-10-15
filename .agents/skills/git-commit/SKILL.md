---
name: git-commit
description: Stage, build-check and commit changes with a Conventional Commits message.
disable-model-invocation: true
---

# git-commit

Commit the paths named in the request, or whatever is already staged. If nothing is staged,
review `git status` and stage the files that belong to the same logical change; ask only when
the changes are genuinely unrelated.

## 1. Verify

If the change set contains `.cs` or `.csproj` files, run the `csharp-build-verify` skill first.
A failed build means no commit — show the errors and stop.

## 2. Message

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

### Never include

A commit message is published with the repository, so treat it as public text.

- **No machine or account identifiers.** Never a user folder or profile path, home directory,
  hostname or account name — not even partially masked or abbreviated.
- **No removed or redacted content.** Never name or describe what a cleanup removed, anonymised or
  replaced, and not as a category either. Say that detail was tightened, not what it was.
- **No restating the reason in sensitive terms.** Name the document or area, not the content.

## 3. Commit

`git commit -m ...`, using a heredoc when there is a body. Never amend, never pass `--no-verify`.
Report the subject line that was committed.
