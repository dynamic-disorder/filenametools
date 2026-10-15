#!/usr/bin/env bash
# PreToolUse hook — verify the build before a `git commit` is allowed through.
#
# Reads the tool-call JSON on stdin. Exit 2 blocks the call and hands stderr back as the reason;
# exit 0 lets it run. Anything that is not a commit, or a commit with no staged C#, exits 0 at once,
# so the common path costs nothing.
#
# Serves both harnesses:
#   * Claude Code passes this payload natively and matches the tool name `Bash`.
#   * DeepSeek Harness matches `bash` through its `dsh-hooks-claude-code` bridge, which sends the
#     same `{cwd, tool_name, tool_input}` JSON and also treats exit 2 as a block.
# Hence the settings.json matcher is the literal alternation `bash|Bash`.
set -uo pipefail

project_dir="${CLAUDE_PROJECT_DIR:-}"
if [ -z "$project_dir" ]; then
  project_dir="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
fi
cd "$project_dir" || exit 0

# The agent sandbox mounts $HOME read-only, so builds there need the repo-local cache redirect.
# Sourcing it is harmless outside the sandbox - it only points the caches inside the repository.
# The tracked script is the source of truth; the scratch file is its older incarnation, kept for
# trees that predate it.
for _env_script in local_dev/scripts/sandbox-env.sh local_user_files/nuget/env.sh; do
  if [ -f "$_env_script" ]; then
    # shellcheck source=/dev/null
    . "$_env_script"
    break
  fi
done
unset _env_script

# Two invocation modes:
#   * harness hook (Claude Code / DSH) — the tool call's JSON arrives on stdin, so the command is inspected;
#   * git hook via `core.hooksPath` — no payload at all, because git only calls it for a commit.
# Never read stdin unconditionally: a git hook run from a terminal inherits a TTY, and a bare `cat`
# would hang the commit until interrupted.
payload=""
if [ ! -t 0 ]; then
  payload="$(cat)"
fi

# jq is present on both target platforms; python3 is the fallback.
cwd=""
command_text=""
if [ -n "$payload" ]; then
  if command -v jq >/dev/null 2>&1; then
    cwd="$(printf '%s' "$payload" | jq -r '.cwd // empty' 2>/dev/null)"
    command_text="$(printf '%s' "$payload" | jq -r '.tool_input.command // empty' 2>/dev/null)"
  elif command -v python3 >/dev/null 2>&1; then
    cwd="$(printf '%s' "$payload" \
      | python3 -c 'import json,sys; print(json.load(sys.stdin).get("cwd",""))' 2>/dev/null)"
    command_text="$(printf '%s' "$payload" \
      | python3 -c 'import json,sys; print(json.load(sys.stdin).get("tool_input",{}).get("command",""))' 2>/dev/null)"
  fi
  # A payload we cannot parse means we cannot tell what is being run: stay out of the way rather
  # than build on every tool call.
  [ -n "$command_text" ] || exit 0
fi

if [ -n "$command_text" ]; then
  # Harness mode: only commits are interesting.
  case "$command_text" in
    *"git commit"*) ;;
    *) exit 0 ;;
  esac

  # The DSH bridge mounts this hook process-wide with one absolute configPath, so it belongs to this
  # repository: a commit made in another workspace must not trigger a build here.
  if [ -n "${CLAUDE_PROJECT_DIR:-}" ] && [ -n "$cwd" ]; then
    case "$cwd" in
      "$CLAUDE_PROJECT_DIR"|"$CLAUDE_PROJECT_DIR"/*) ;;
      *) exit 0 ;;
    esac
  fi
fi
# Git-hook mode (no payload) falls through to the check below: git only runs it for a commit.

# Only C#-relevant changes need a build.
staged="$(git diff --staged --name-only 2>/dev/null | grep -E '\.(cs|csproj|props|targets)$')"
[ -n "$staged" ] || exit 0

# Nearest solution at or above the first staged file, else the repo root.
sln=""
search_dir="$(dirname "$(printf '%s\n' "$staged" | head -1)")"
while [ -n "$search_dir" ] && [ "$search_dir" != "." ] && [ "$search_dir" != "/" ]; do
  found="$(find "$search_dir" -maxdepth 1 -name '*.sln' -print -quit 2>/dev/null)"
  if [ -n "$found" ]; then
    sln="$found"
    break
  fi
  search_dir="$(dirname "$search_dir")"
done
if [ -z "$sln" ]; then
  sln="$(find . -maxdepth 2 -name '*.sln' -not -path './local_user_files/*' -print -quit 2>/dev/null)"
fi
# A repository holding a single project has no solution. Gate that project rather than silently
# doing nothing, which is what this script used to do there.
if [ -z "$sln" ]; then
  sln="$(find . -maxdepth 2 -name '*.csproj' -not -path './local_user_files/*' -print -quit 2>/dev/null)"
fi
[ -n "$sln" ] || exit 0

# A harness can run this hook with a read-only view of the workspace (DSH's sandbox does). There a
# build cannot write bin/obj, so MSBuild fails with EROFS/MSB3061 and its verdict says nothing about
# the code. Check first: if the workspace is not writable, warn and allow rather than block a commit
# on an environment failure.
write_probe="$project_dir/.build-gate-write-probe.$$"
if ! ( : > "$write_probe" ) 2>/dev/null; then
  echo "[pre-commit] workspace is read-only here ($project_dir) — skipping the build check" >&2
  exit 0
fi
rm -f "$write_probe"

echo "[pre-commit] staged C# — building $sln" >&2
if dotnet build "$sln" --no-incremental --verbosity quiet >&2 2>&1; then
  echo "[pre-commit] build OK" >&2
  exit 0
fi

# One retry: a --no-incremental build can lose a race with the file lock on Windows.
echo "[pre-commit] build failed — retrying once" >&2
if dotnet build "$sln" --no-incremental --verbosity quiet >&2 2>&1; then
  echo "[pre-commit] build OK on retry" >&2
  exit 0
fi

echo "" >&2
echo "[pre-commit] build FAILED for $sln — the commit is blocked. Fix the errors above, then retry." >&2
exit 2
