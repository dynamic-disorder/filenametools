---
name: explain
description: Explain code — what it does, what's non-obvious, where to be careful.
disable-model-invocation: true
---

Explain what the request names.

Assume .NET/C# familiarity. Skip what's obvious from the code. Lead with a single sentence: what it does and why it exists.

Then cover only what causes friction:
- Non-obvious decisions or tradeoffs
- Gotchas, edge cases, hidden dependencies
- Where changes are risky and why

Add an ASCII flow diagram only if the control/data flow isn't clear from reading the code alone.

Be concise. No padding, no walkthrough of obvious lines.
