# Partidas — service context

Owns creation and configuration of a `Partida` and its `Juego`s (Trivia question config and
BDT stage config, per-stage `Puntaje`). Does NOT run the live session or compute scores/ranking.

Status: SP-0 shell — graded structure + `/health` + empty `PartidasDbContext` (→ `umbral_partidas`).
The Partida/Juego domain model and configuration endpoints arrive in SP-2.
