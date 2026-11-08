# Latest completed task

## 2026-11-08 — Linux command safety

- Remove OpenAI keys from child environments and treat recursive `rm` operations as critical.
- Keep Linux command descendants reachable during cancellation and output timeouts.
- Reject ambiguous Unix PATH entries before installation and allow removal of existing entries.

Public update: HelpMe keeps OpenAI credentials out of child processes, treats recursive rm commands as a critical risk, and stops descendant processes when you cancel a Linux command.
