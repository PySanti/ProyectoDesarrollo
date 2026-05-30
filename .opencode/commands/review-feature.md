# Review Feature

Use this command to review a completed or partially completed feature.

## Mandatory validation

1. Use `sdd-workflow`.
2. Use `testing`.
3. Use `contract-design` when contracts are involved.
4. Use `umbral-context`.
5. Confirm the feature appears in `docs/04-sdd/SPECS-LIST.md`.
6. Confirm the feature is not under `docs/04-sdd/specs/_deprecated/`.

## Check

1. SDD compliance.
2. Microservice ownership.
3. Clean/Hexagonal architecture.
4. CQRS/MediatR separation.
5. Domain rule placement.
6. HTTP and event contracts.
7. Tests and coverage evidence.
8. Acceptance criteria.
9. Traceability matrix.
10. Project-source requirements.
11. No references to inactive services:
    - Audit Service
    - Scoring Service
    - Trivia Service
    - Treasure Hunt Service
    - Notification Service.
12. No usage of generic mission/session/evidence vocabulary unless explicitly mapped to Trivia/BDT.

## Output findings as

- Blocker
- Major
- Minor
- Recommendation

Do not edit files unless explicitly asked.
