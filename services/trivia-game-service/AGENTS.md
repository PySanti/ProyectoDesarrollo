# Legacy Implementation Folder — Flow State

This file belongs to the previous implementation layout. It is retained as migration-debt context for old code/tests and is not active service doctrine. Current target services are `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`, behind the mandatory YARP gateway.

## Completed features
- HU-30 — Ver ranking durante Trivia (backend done, SignalR hub + notifier + ranking endpoint)
- HU-24 — Iniciar manualmente Trivia (ModoInicioAutomatico, ManualYAutomatico, todos los tasks hechos)

## Test counts
- Domain: 154 pass, 0 fail
- Application: 113 pass, 0 fail
- API: 58 pass, 1 fail (pre-existing InMemory isolation)
- Total: 325/326 pass

## Known issues
- `GetAll_NoGames_ReturnsEmptyList` fails because InMemory database is shared across tests within same fixture class

## Next steps
- Frontend operator ranking view (React web)
- Move to next first-sprint feature
