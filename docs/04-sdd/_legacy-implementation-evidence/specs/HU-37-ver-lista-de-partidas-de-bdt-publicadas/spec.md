# HU-37 - Ver lista de partidas de BDT publicadas

## User Story

Como **Operador**, quiero **ver la lista de partidas de busqueda de tesoro que fueron publicadas**, para **consultar su nombre y estado desde el panel de operador**.

## Source References

- HU: `HU-37` in `docs/01-project-source/srs.md` and `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-25`, `RF-27`, `RF-35`.
- RB: `RB-B01`, `RB-B02`, `RB-B03`, `RB-B12`.
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Web context: `frontend/frontend-context.md`.
- Contract base: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Operador` using React web.

## User Goal

Show the operator a read-only list of published BDT games, including each game name and current state, without changing game state or participant registrations.

## Scope

Included:

- React web operator list for BDT games published in first delivery.
- Backend query in BDT Game Service for operator-visible published BDT games.
- Minimum fields: `partidaId`, `nombre`, `modalidad`, `estado`, `areaBusqueda`, `cantidadEtapas`.
- Operator summary modal using only the same list response fields, without calling a full-detail endpoint.
- Loading, empty and error states in React web.
- Authentication and role authorization for operator access.

Out of scope:

- Creating BDT games; covered by HU-34.
- Viewing full BDT detail beyond the fields returned by the list query; HU-38 is not active in first delivery.
- Participant mobile listing; covered by HU-10 and HU-12.
- Joining BDT games; covered by HU-39 and HU-40.
- Operator lobby monitoring of joined participants; covered by HU-42.
- Starting games; covered by HU-43.
- Real-time list refresh; not required to close HU-37.

## Preconditions

- User is authenticated with base role `Operador`.
- BDT Game Service contains zero or more BDT games in published state.
- For first delivery, published BDT games are represented by state `Lobby`.

## Postconditions

- No BDT game state is modified.
- No participant is registered.
- The operator sees published BDT games or a clear empty state.

## Business Rules

- `RF-35`: queries must not modify system state.
- `RB-B12`: when the BDT lobby is created, the BDT is published.
- Only BDT Game Service owns BDT game listing.
- React web must not implement participant gameplay behavior.
- The query must not calculate BDT ranking or numeric score.

## Related Requirements

- `RF-25`
- `RF-27`
- `RF-35`
- `RNF-01`
- `RNF-02`
- `RNF-04`
- `RNF-06`
- `RNF-13`

## Acceptance Criteria

1. An authenticated operator can open the React web BDT published games list.
2. The web client calls a documented BDT Game Service HTTP query.
3. The response includes only BDT games published for operator supervision.
4. Each row shows at least name and state.
5. Each row also exposes modality, textual search area and stage count when returned by the contract.
5a. The operator can open a read-only summary modal for a listed game using the same row data.
6. If no BDT games are published, the web client shows a clear empty state.
7. If the query fails, the web client shows a clear error state.
8. The query does not change game state and does not create inscriptions.
9. The endpoint rejects unauthenticated users with `401`.
10. The endpoint rejects authenticated non-operator users with `403`.

## Assumptions

- `Publicada` maps to BDT state `Lobby` for the first-delivery list, consistent with HU-10/HU-12.
- HU-37 uses an operator-authorized endpoint separate from the participant listing endpoint because the actor and client are different.

## Open Questions

- None blocking for SDD review.
