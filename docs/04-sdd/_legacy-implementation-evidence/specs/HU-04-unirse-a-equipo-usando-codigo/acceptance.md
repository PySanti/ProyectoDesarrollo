# HU-04 — Acceptance

## Acceptance checklist

- [x] HU-04 is assigned to `Team Service`.
- [x] HU-04 is implemented for `React Native mobile` participant flow.
- [x] Team join is allowed only when participant does not belong to another active team.
- [x] Team join requires a valid access code that resolves to an active team.
- [x] Joined participant is added as non-leader member.
- [x] Team cardinality decision `1..5` is respected.
- [x] Join is rejected with business conflict (`409`) when the target team is full.
- [x] Required tests exist (domain, application, integration, contract, mobile frontend).
- [x] HU-04 contract section is documented in `contracts/http/team-api.md`.
- [x] Traceability row for HU-04 is updated.

## Manual verification steps

1. Authenticate as participant in mobile app.
2. Open join-team-by-code screen.
3. Submit a valid access code while participant has no active team.
4. Verify success response shows the team info with the participant included.
5. Verify the joined participant is not marked as leader.
6. Attempt joining with an invalid code.
7. Verify operation is rejected without state change.
8. Attempt joining when participant already belongs to another active team.
9. Verify operation is rejected with business conflict (`409`).
10. Attempt joining a full team with 5 members.
11. Verify operation is rejected with business conflict (`409`).

## Manual runtime evidence (device/emulator)

- Context: React Native participant flow using `JoinTeamScreen` on local Expo runtime with backend Team Service running locally.
- Scenario A (`200`): valid access code joins successfully and screen shows `Te uniste al equipo con exito.`
- Scenario B (`404`): invalid access code shows `El codigo ingresado no corresponde a un equipo activo.`
- Scenario C (`409`): participant already in active team or target team full shows conflict message and no state change.
- Result: all three scenarios documented and aligned with HU-04 acceptance criteria.

## Automated test evidence

- `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj`
- `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj`
- `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj`
- `npm run typecheck` from `mobile/`
- `npm test` from `mobile/`

Latest execution summary:

- Team Service unit tests: `41` passed.
- Team Service integration tests: `30` passed, including the HU-04 concurrent join invariant test and PostgreSQL/Npgsql evidence.
- Team Service contract tests: `11` passed.
- Mobile typecheck: passed.
- Mobile tests: `14` passed (includes `joinTeamScreenModel.test.js` client-side behavior tests).

## Traceability status

- HU-04 row updated in `docs/04-sdd/traceability-matrix.md` after implementation and testing.

## Assumptions

- Team join uses Team Service as source of truth for access-code validation, membership and leadership.
- HU-04 authorization relies on authenticated token claims and participant policy in Team Service (no mandatory Identity Service runtime query for this HU).
- The global unique index `ux_equipos_participantes_usuarioid` remains valid for current HU-04 behavior; HU-07/HU-05 implementation must preserve membership lifecycle consistency so valid future joins are not blocked by stale active membership rows.

## PostgreSQL Concurrency Hardening

- Advisory locking implemented at the database level to ensure the team cardinality invariant `1..5` is preserved even under high concurrent load across multiple service instances.
- New integration test `Hu04PostgresConcurrencyTests.cs` added to verify this behavior with a real PostgreSQL backend.
- Merge verification on 2026-06-04: `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Postgres"` passed 2/2 with the compose PostgreSQL container and isolated test schema.

## Runtime Evidence

- Evidence of runtime execution with date, device, and logs is available in `evidence/runtime_evidence.md`.
