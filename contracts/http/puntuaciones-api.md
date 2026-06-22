# Puntuaciones HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- Trivia native ranking queries.
- BDT native ranking queries.
- Consolidated partida ranking queries.
- Score and won-stage queries.
- Team-performance queries.
- Audit/history projection queries.
- Ranking real-time broadcasts through the gateway.

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Score, ranking, team-performance and history queries | Defined by SDD | Defined by SDD | Puntuaciones | Not registered |
