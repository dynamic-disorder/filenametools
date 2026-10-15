---
name: issue-investigate
description: Trace a bug to its root cause and write the analysis to local_user_files/.
---

# Issue investigation

Investigate the bug or unexpected behaviour described in the request. If the request is empty,
infer the area from `git diff` and `git log --oneline -10`, then ask.

Assume .NET/C# fluency. Work from symptom to source:

1. Find the code path that produces the behaviour and read it end to end; check recent commits
   that touched those files.
2. Pin the root cause down with file paths and line numbers, noting implicit assumptions, hidden
   dependencies and non-obvious decisions along that path.
3. Say what shares the affected code, where else the same cause could surface, and how risky a
   fix would be — including concurrency, ordering and state concerns.
4. Recommend a concrete fix and, when there are real alternatives, their tradeoffs.

Write the findings to `local_user_files/YYYY-MM-DD_<slug>_bug_analysis.md` (kebab-case slug; the
directory is git-ignored), keeping it to the point: summary, root cause, affected code, impact,
suggested fix. Add an ASCII diagram only when the flow isn't clear from the code.

Do not change any code. If you cannot pin the root cause down, say so and list what you would
need to confirm it.
