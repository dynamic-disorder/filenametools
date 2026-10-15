---
name: spike
description: Time-boxed technical research on a topic, written up in local_user_files/.
---

# Spike

Time-boxed research into the topic the request names — architecture, a library, performance or
an implementation approach. Ask if the topic is missing.

Assume .NET/C# fluency. Read the code before theorising: find the existing implementations, map
the components and their dependencies, and check git history for how the area evolved.

Cover what the code does today and why, what alternatives existed, where it holds up and where it
hurts, and how the problem is normally solved elsewhere. Be explicit about anything you could not
establish.

Write the result to `local_user_files/YYYY-MM-DD_spike_<topic>.md` (kebab-case topic; the
directory is git-ignored): findings with file paths and line numbers, then the recommendations
worth acting on, quick wins first.

Do not change any code.
