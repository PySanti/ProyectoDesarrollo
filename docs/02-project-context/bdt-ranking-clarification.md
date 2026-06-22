# BDT Ranking Clarification

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Decision

BDT native ranking is based on accumulated points from won stages.

Ordering:

1. Higher accumulated BDT points ranks higher.
2. If tied, lower accumulated time across won stages ranks higher.

`EtapasGanadas` may be retained as informative data but is not the primary sort key.

## Forbidden Active Assumption

Do not state that BDT ranking is primarily ordered by number of stages won in current doctrine.

## Context

Each `EtapaBDT` (configured in **Partidas**) carries an operator-set `Puntaje`. A stage is won by the participant or team that first validates the expected QR text during the live game (run by **Operaciones de Sesion**). The won stage grants its `Puntaje`; stages nobody wins grant nothing. **Puntuaciones** accumulates these points per `JuegoBDT`, computes the native `RankingBDT`, and contributes the total to the consolidated partida ranking. The native BDT ranking and the per-game points feed the consolidated ranking (number of `Juego`s won → total accumulated points → lowest total time).

Relevant events: `EtapaBDTGanada` (carries `Puntaje`) and `RankingBDTActualizado`.
