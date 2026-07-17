#!/usr/bin/env bash
# orient.sh — read-only snapshot of an in-flight superpowers implementation so a
# fresh session (e.g. after the 5h token window resets) can find where it left
# off. Touches nothing; only reads git, the SDD ledger/workspace, the plans, and
# any persisted Workflow()-tool run scripts for this repo.
#
# Usage: orient.sh [REPO_ROOT]
#   REPO_ROOT defaults to the git top-level of the current directory.
set -euo pipefail

root=${1:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}
cd "$root"

rule() { printf '\n========== %s ==========\n' "$1"; }

rule "REPO"
echo "root: $root"
branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo '(no git)')
echo "branch: $branch"
# ahead/behind vs the most likely integration branches
for base in main master develop; do
  if git rev-parse --verify -q "$base" >/dev/null 2>&1 && [ "$base" != "$branch" ]; then
    counts=$(git rev-list --left-right --count "$base...HEAD" 2>/dev/null || echo "? ?")
    echo "vs $base (behind ahead): $counts"
  fi
done

rule "UNCOMMITTED CHANGES (git status --short)"
git status --short 2>/dev/null || true
echo "--- last stash entries ---"
git stash list 2>/dev/null | head -5 || true

rule "RECENT COMMITS"
git log --oneline -15 2>/dev/null || true

# ---- SDD progress ledger (durable, hand- or controller-maintained) ----
# Known/likely locations, in priority order. .git/sdd is this project's custom
# durable spot; .superpowers/sdd is the superpowers v6 default workspace.
rule "PROGRESS LEDGER"
ledgers=$(ls -1t \
  .superpowers/sdd/progress.md \
  .git/sdd/progress.md \
  docs/superpowers/**/progress.md \
  2>/dev/null || true)
if [ -z "$ledgers" ]; then
  # fall back to a bounded search
  ledgers=$(find . -path ./node_modules -prune -o -name 'progress.md' -path '*sdd*' -print 2>/dev/null | head -5 || true)
fi
if [ -n "$ledgers" ]; then
  for L in $ledgers; do
    echo ">>> $L  (modified $(date -r "$L" '+%Y-%m-%d %H:%M' 2>/dev/null || echo '?'))"
  done
  primary=$(printf '%s\n' "$ledgers" | head -1)
  echo "--- tail of primary ledger ($primary) ---"
  tail -40 "$primary" 2>/dev/null || true
  echo "--- (read the full ledger for the authoritative task-by-task state) ---"
else
  echo "(no progress.md ledger found)"
fi

# ---- superpowers workspace: briefs/reports reveal the last completed task ----
rule "SDD WORKSPACE (briefs / reports)"
for ws in .superpowers/sdd .git/sdd; do
  [ -d "$ws" ] || continue
  echo ">>> $ws"
  # newest report = most recently completed task
  newest_report=$(ls -1t "$ws"/*report*.md 2>/dev/null | head -1 || true)
  [ -n "$newest_report" ] && echo "    newest report: $newest_report ($(date -r "$newest_report" '+%m-%d %H:%M' 2>/dev/null))"
  newest_brief=$(ls -1t "$ws"/*brief*.md 2>/dev/null | head -1 || true)
  [ -n "$newest_brief" ] && echo "    newest brief:  $newest_brief"
  newest_diff=$(ls -1t "$ws"/review-*.diff 2>/dev/null | head -1 || true)
  [ -n "$newest_diff" ] && echo "    newest review pkg: $newest_diff"
done

# ---- plans & specs ----
rule "PLANS (newest first = most likely active)"
for d in docs/superpowers/plans plans docs/plans; do
  [ -d "$d" ] || continue
  ls -1t "$d"/*.md 2>/dev/null | while read -r p; do
    echo "$(date -r "$p" '+%m-%d %H:%M')  $p"
  done
done

# ---- in-flight Workflow()-tool runs persisted for this repo ----
# Claude Code stores per-session workflow scripts under
# ~/.claude/projects/<path-slug>/<session>/workflows/scripts/*-wf_<id>.js
rule "WORKFLOW() TOOL RUN SCRIPTS (for resumeFromRunId)"
slug=$(printf '%s' "$root" | sed 's#/#-#g')
proj="$HOME/.claude/projects/$slug"
if [ -d "$proj" ]; then
  found=$(find "$proj" -path '*/workflows/scripts/*.js' -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -8 || true)
  if [ -n "$found" ]; then
    printf '%s\n' "$found" | while read -r ts path; do
      base=$(basename "$path")
      # run id is the wf_... suffix before .js
      rid=$(printf '%s' "$base" | grep -oE 'wf_[a-z0-9-]+' || true)
      echo "$(date -d "@${ts%.*}" '+%m-%d %H:%M' 2>/dev/null)  runId=$rid"
      echo "    scriptPath=$path"
    done
    echo "--- to resume one: Workflow({scriptPath, resumeFromRunId}) — completed agents return cached ---"
  else
    echo "(no persisted workflow scripts)"
  fi
else
  echo "(no claude project dir at $proj)"
fi

rule "DONE"
echo "Next: read the primary ledger + the active plan's tasks, identify the first"
echo "unchecked/next task, then re-enter the matching superpowers skill."
