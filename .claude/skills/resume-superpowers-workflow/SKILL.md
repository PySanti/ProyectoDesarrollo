---
name: resume-superpowers-workflow
description: >-
  Resume an in-flight superpowers-driven implementation in a fresh session —
  the kind that got cut off when the 5-hour token window ran out mid-execution.
  Use this whenever you (or the user) say things like "continue the workflow",
  "pick up where we left off", "I have my tokens back", "the 5h window reset",
  "resume the plan", "keep going on the migration", "finish the SDD run", or
  point at a half-done subagent-driven-development / executing-plans run, a
  paused Workflow() orchestration (resumeFromRunId), or an implementation that
  was about to hit finishing-a-development-branch. Reach for this BEFORE
  re-reading random files or guessing the state by hand: it reconstructs the
  exact resume point from the durable ledger, the SDD workspace, git, and any
  persisted Workflow run scripts, then re-enters the correct superpowers skill.
---

# Resume a superpowers workflow

Superpowers implementations (`subagent-driven-development`, `executing-plans`)
are deliberately built to survive a dead session: each task is committed, and
progress is written to a durable ledger as it goes. The 5-hour token window
running out is exactly the failure they were designed for. So resuming is **not
guesswork** — it's reading the breadcrumbs back and stepping into the next task.

Your job here is to find the resume point precisely, confirm it against git
(commits are ground truth — the ledger can lag), then hand back to the right
superpowers skill. Do not re-plan, re-implement completed tasks, or invent a new
approach: continue the existing run.

## Step 1 — Orient (run the snapshot script)

Run the bundled read-only snapshot. It touches nothing; it just reads git, the
ledger, the SDD workspace, the plans, and any persisted `Workflow()` run scripts
for this repo:

```bash
bash ~/.claude/skills/resume-superpowers-workflow/scripts/orient.sh
```

(Pass a repo path as `$1` if you're not inside the target repo.)

Read its whole output. The sections you care about most:
- **RECENT COMMITS** — the *real* "last completed" boundary. Trust these over prose.
- **PROGRESS LEDGER** — the task-by-task narrative; tail is shown, read the full
  file for the authoritative state and any "Next:" / "Open Minors" markers.
- **SDD WORKSPACE** — the newest `task-N-report.md` ≈ last task that finished.
- **PLANS** — newest-modified plan is almost always the active one.
- **WORKFLOW() TOOL RUN SCRIPTS** — `scriptPath` + `runId` you'd need to resume a
  paused multi-agent Workflow orchestration.

Then **read two files in full**: the primary `progress.md` ledger and the active
plan (its task list / checkboxes). That pair tells you what's done and what's next.

## Step 2 — Classify what was in flight

Match the state to one of these and resume accordingly. (A single branch can hit
these in sequence — e.g. finish the last SDD task, *then* go to finishing.)

| Signal in the orient output | In-flight work | Resume via |
|---|---|---|
| Ledger lists plan tasks, some `complete`, at least one not — and HEAD matches the last "complete" commit | A `subagent-driven-development` run (this session) or `executing-plans` (separate session) | **Step 3A** |
| A `*-wf_<id>.js` script exists and its run never produced a final synthesis / shows partial failures | A paused `Workflow()` orchestration (audit/review/migration fan-out) | **Step 3B** |
| Ledger says all tasks `complete` + final review done / "Next: finishing-a-development-branch", tests green | Implementation done, integration pending | **Step 3C** |
| No ledger, but a plan exists with unchecked tasks and matching commits | A plan being executed without a ledger | **Step 3A** (reconstruct progress from commits + checkboxes) |

If two apply (e.g. an SDD run *and* a leftover Workflow review script), finish
the implementation first, then the review, then finishing.

## Step 3A — Resume an SDD / plan execution

1. Identify **the next task**: first ledger task not marked complete, or first
   unchecked checkbox in the plan, cross-checked against the last commit.
2. Re-enter the owning skill — invoke `superpowers:subagent-driven-development`
   (same-session, fresh subagent per task) or `superpowers:executing-plans`
   (separate-session with review checkpoints). Match whichever the ledger says
   was being used (the orient "Execution:" line records it).
3. Feed the subagent its task with the existing tooling so context stays lean.
   Generate the one-shot brief the implementer reads, from the **repo root**:
   ```bash
   SP=$(ls -d ~/.claude/plugins/cache/*/superpowers/*/skills/subagent-driven-development | sort | tail -1)
   "$SP/scripts/task-brief" "<active-plan.md>" "<next-task-number>"
   # → writes <repo>/.superpowers/sdd/task-<N>-brief.md for the implementer to read
   ```
   (`task-brief` / `review-package` / `sdd-workspace` live under the superpowers
   plugin and are not on `$PATH`; call them by full path. They default their
   output to `<repo>/.superpowers/sdd/`.)
4. After each task: run the task review (two-stage: spec-conformance, then
   quality), commit, and **append a line to the same ledger** so the next
   resume is just as clean.
5. Keep going task-by-task until the plan is exhausted, then go to **Step 3C**.

## Step 3B — Resume a paused Workflow() orchestration

The `Workflow()` tool journals every completed `agent()` call. Re-running with
the same script + run id replays the cache and only re-runs what died:

```text
Workflow({ scriptPath: "<path from orient>", resumeFromRunId: "<wf_… from orient>" })
```

Completed agents return cached results instantly; the agents that hit the
session limit (or threw) re-run live. If you *edited* the script, the longest
unchanged prefix still caches; the first changed `agent()` and everything after
it re-runs. When it finishes, surface the synthesis to the user.

## Step 3C — Implementation complete → integrate

When every task is done, the final whole-branch review is green, and tests pass,
invoke `superpowers:finishing-a-development-branch` and present its options
(merge / PR / keep-as-is / discard). Do **not** merge, push, or discard on your
own initiative — that's the user's call. If more slices remain on the same
branch (e.g. SP-2..SP-5 after SP-0), "keep as-is and continue" is usually the
natural choice; say so but let the user pick.

## Step 4 — Verify before you claim it's resumed

Before telling the user "we're back on track," confirm with evidence, per
`superpowers:verification-before-completion`:
- The next task you're about to do is genuinely the next one (not one already
  committed — re-doing a done task is the classic resume bug).
- The build/tests are green at the current HEAD (run the suite the plan names).
  A resume that starts from a broken tree wastes the recovered window.

## State sources (where the breadcrumbs live)

| What | Canonical location | Notes |
|---|---|---|
| Progress ledger | `<repo>/.superpowers/sdd/progress.md` **or** `<repo>/.git/sdd/progress.md` | Some repos keep a hand-maintained durable ledger under `.git/sdd/`; superpowers' own workspace default is `.superpowers/sdd/`. Check both. |
| Task briefs / reports / review diffs | `<repo>/.superpowers/sdd/` | `task-N-report.md` = task N finished. Self-`.gitignore`d. |
| Plans / specs | `<repo>/docs/superpowers/{plans,specs}/` | Newest-modified = active. |
| SDD scripts | `<superpowers-plugin>/skills/subagent-driven-development/scripts/{task-brief,review-package,sdd-workspace}` | Not on `$PATH`; call by full path. |
| Workflow run scripts | `~/.claude/projects/<path-slug>/<session>/workflows/scripts/*-wf_<id>.js` | `<path-slug>` = repo path with `/`→`-`. Needed for `resumeFromRunId`. |

## Pitfalls

- **Git is truth, the ledger is a narrative.** If they disagree, believe the
  commits and fix the ledger.
- **Don't re-plan.** The plan and spec already exist and were already reviewed.
  Resuming ≠ re-deciding. Only touch the plan if a prior task's review told you to.
- **Don't redo a committed task.** The single most common resume mistake.
- **Workflow resume needs the *same* run id.** A fresh `Workflow()` call (no
  `resumeFromRunId`) starts cold and re-burns the whole budget.
- **Stay lean.** You came back to a fresh window for a reason — push task text to
  subagents via `task-brief`, don't paste whole plans into your own context.

## Preventing the next cutoff (optional, mention once)

If the user keeps losing runs to the 5h window, the durable ledger is what makes
this skill cheap — encourage keeping `subagent-driven-development`'s habit of
committing each task and appending one ledger line per task. The smaller each
task, the less is ever lost when the window dies mid-task.
