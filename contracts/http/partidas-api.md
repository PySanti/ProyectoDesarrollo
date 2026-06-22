# Partidas HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- Creation and configuration of a `Partida`.
- Sequential `Juego` configuration, including order, type and modality inherited from the partida.
- Trivia question configuration with options, correct answer, assigned score and time limit.
- BDT stage configuration with expected QR text, per-stage score and time limit.
- Configuration queries for partidas, games, Trivia questions and BDT stages.

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Partida and game configuration commands/queries | Defined by SDD | Defined by SDD | Partidas | Not registered |
