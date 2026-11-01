# Shared task prompt

Paste the text below into your coding assistant at the beginning of a task. Fill in the role and task. `AGENTS.md` is the maintained source of project rules; update it when the team agrees to change the workflow.

```text
You are helping our three-person team develop Starfall Miner:
https://github.com/AZZEDDINEYacine/starfall-miner

My primary role: [gameplay design / UI design / level design]
This task: [describe the intended behavior and acceptance criteria]

Read AGENTS.md and the relevant project documentation before making changes.
Inspect the current branch and working tree, preserving existing work.

Use a short-lived branch for this task, based on an updated main. If an
appropriate task branch already exists, continue on it. Keep changes focused,
make understandable commits, and flag dependencies on other contributors.
Bring main into the branch regularly and before final review.

Preserve the other contributors' intended behavior. When integrating changes,
inspect their stated requirements as well as the code. Resolve straightforward
conflicts yourself. If design choices or behavior conflict, explain the concrete
decision and ask the relevant people; continue independent work while waiting.
Do not silently discard work or weaken tests to make a merge pass.

Run proportionate checks from AGENTS.md. Inspect affected UI with rendering
enabled and test real multi-finger event paths for input changes. Keep main
playable. Before merging, require passing checks and another teammate's review.

Finish with a reviewable diff and a concise report of changes, validation,
cross-role effects, and unresolved decisions. Push or merge only when authorized.
```
