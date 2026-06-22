# Operaciones de Sesion HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- Publishing a partida to lobby.
- Manual and automatic partida start.
- Runtime session queries for lobby, active question, active stage and session state.
- Partida-level inscriptions and convocatorias.
- Trivia answer submission and validation.
- BDT QR treasure upload and validation.
- Sequential game and stage advance.
- BDT clues, geolocation and reconnection support.
- User-visible session real-time communication through the gateway.

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| Runtime, inscription, convocatoria and live-session commands/queries | Defined by SDD | Defined by SDD | Operaciones de Sesion | Not registered |
