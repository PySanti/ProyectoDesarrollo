# ADR-0008 - Documentation Doctrine Replacement

## Status

Accepted

## Context

The project documentation has been updated with a new target doctrine. The repository still contains documentation and implementation evidence from an older service decomposition.

## Decision

Active documentation is regenerated from `docs/01-project-source/` and `CLAUDE.md`.

The target backend services are:

- `Identity`
- `Partidas`
- `Operaciones de Sesion`
- `Puntuaciones`

The YARP gateway is mandatory and is the single backend entry point for web and mobile clients.

The previous active service doctrine using `Team Service`, `Trivia Game Service`, and `BDT Game Service` is superseded.

BDT native ranking is point-based: accumulated points from won stages, tie-break by lower accumulated time across won stages.

Old SDDs and contracts may be preserved only as historical implementation evidence.

## Supersedes

- `ADR-0002-service-per-business-capability.md` where it conflicts with this service model.
- `ADR-0006-four-service-topology.md` where it defines the old four-service topology.
- Any previous documentation statement that rejects point-based BDT ranking as active doctrine.

## Consequences

- Active documentation must not direct new work to the old physical services.
- New contracts must be organized around the target services and gateway.
- New SDDs must be created under the current doctrine.
- Existing code migration is separate future work.
