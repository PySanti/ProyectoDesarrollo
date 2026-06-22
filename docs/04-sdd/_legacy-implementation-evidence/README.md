# Legacy Implementation Evidence

This directory preserves SDDs, traceability, and evidence from the previous implementation doctrine.

## Status

Historical evidence only. Do not use these files as current planning input.

## Why This Exists

The project doctrine now uses the target service model documented in `CLAUDE.md` and the current source files under `docs/01-project-source/`.

The archived files may still help explain implemented behavior, prior tests, and migration debt, but they describe the old service decomposition and old contract assumptions.

## Rules

- Do not implement new features from this directory.
- Do not treat archived contracts, service ownership, or traceability rows as active doctrine.
- If old behavior is needed, create a new SDD under the current doctrine and cite this archive only as historical evidence.
