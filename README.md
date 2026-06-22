# UMBRAL

UMBRAL is an academic software engineering project for operating real-time interactive experiences under exactly two game modes: Trivia and Busqueda del Tesoro / BDT.

## Migration State

The documentation doctrine has changed before code migration. Read `docs/02-project-context/documentation-migration-status.md` before using root, client, gateway or service-folder guidance.

Current target doctrine:

- backend services: `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`;
- mandatory YARP gateway as the single entry point for web and mobile clients;
- React web serves `Administrador` and `Operador` flows;
- React Native mobile serves `Participante` and `Lider de equipo` acting as participant flows;
- legacy implementation folders may remain as migration debt and are not active service-boundary doctrine.

Do not infer endpoint payloads, queue names, routing keys or SignalR shapes from old folders. Use current documentation under `docs/02-project-context/`, `docs/03-microservices/`, `docs/04-sdd/`, `docs/05-decisions/ADR-0008-documentation-doctrine-replacement.md` and `contracts/` before planning new work.
