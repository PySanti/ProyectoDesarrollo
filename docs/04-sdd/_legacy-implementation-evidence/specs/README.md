# Active SDD Specs

This folder must contain only active SDD specs from:

```txt
docs/04-sdd/SPECS-LIST.md
```

## Current rule

Do not implement from any spec folder unless:

1. its folder name appears in `docs/04-sdd/SPECS-LIST.md`;
2. it has `spec.md`, `design.md`, `tasks.md` and `acceptance.md`;
3. none of those files contains unresolved TODO sections;
4. the owning service is one of:
   - Identity Service
   - Team Service
   - Trivia Game Service
   - BDT Game Service.

## Deprecated specs

Old specs must be moved to:

```txt
docs/04-sdd/specs/_deprecated/
```

Do not read deprecated specs for implementation.
