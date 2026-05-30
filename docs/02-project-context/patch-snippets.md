# Patch snippets

## AGENTS.md — client topology snippet

```md
## Client topology

UMBRAL has two frontend clients:

1. Web React client:
   - Used by Administrador and Operador.
   - Owns administration, operation, lobby supervision, rankings, BDT geolocation map and history views.

2. Mobile React Native client:
   - Used by Participante.
   - Owns participant gameplay flows: listing games, filtering by modality, team membership actions, joining events, accepting/rejecting convocatorias, answering Trivia, uploading QR treasure images, receiving clues and sharing BDT geolocation.

Do not implement participant gameplay screens in the React web frontend unless an SDD explicitly says so.
Do not implement administrator/operator screens in the React Native mobile app unless an SDD explicitly says so.
```

## BDT service wording replacement

Replace active BDT scoring wording with:

```md
BDT ranking is based on stages won and accumulated time for won stages. The BDT Game Service must not calculate ranking from numeric accumulated score unless a future ADR changes this decision.
```

## opencode.json additional instructions

```json
"docs/02-project-context/adaptation-to-academic-brief.md",
"docs/02-project-context/mobile-participant-context.md",
"docs/02-project-context/bdt-ranking-clarification.md",
"mobile/mobile-context.md"
```
