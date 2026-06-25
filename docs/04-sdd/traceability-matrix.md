# Current Traceability Matrix

## Rule

Traceability rows must reference the current source documents, target service ownership, current contracts, tests, and acceptance evidence.

| Feature | Requirement | Owning service | Supporting services | SDD folder | Contracts | Status |
|---|---|---|---|---|---|---|
| Partida/Juego model + Partidas configuration (SP-2) | Create a Partida header and add fully-formed Juegos (Trivia `Pregunta`/`Opcion`, BDT `EtapaBDT`) in sequential order; review queries (detail + list); `EstadoPartida` nullable until publish (SP-3 SEAM) | Partidas | — (SP-3 runtime / SP-4 scoring consume downstream) | docs/superpowers/specs/2026-06-24-sp2-partida-juego-model-partidas-config-design.md · docs/superpowers/plans/2026-06-24-sp2-partida-juego-model-partidas-config.md | contracts/http/partidas-config.md | Implemented — 102/102 green (91 unit + 5 integration + 6 contract); R1 structural gate passed; whole-branch review READY TO INTEGRATE; doctrine-conformance audit (6-dim multi-agent) = **CompliantWithDeviations** (0 Critical / 0 Important / 7 Minor + 1 disputed; ver `.superpowers/sdd/sp2-doctrine-conformance-report.md`). **7 Minor resueltos** (M-1..M-7, commits `038cab1..fa66451`): vocab SP-3 neutralizado, validación movida a `ValidationBehavior` (pipeline MediatR), `HealthControllerTests` reubicado en `Api/`, y cubiertos los huecos 409/Location/orden<1/colisión-BDT. M-8 (clasificación DDD de `Opcion`) dejado como **deuda documentada** (existencia de `OpcionId` plan-mandated por contrato). Suite **106/106 verde** (94 unit + 5 integration + 7 contract). |
