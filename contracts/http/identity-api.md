# Identity HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- User creation with initial role, local user references and Keycloak mapping.
- User consultation, general-data editing and deactivation.
- Role modification for operators and participants, including promotion to administrator, propagated to Keycloak.
- Per-role functional permissions and governance privileges.
- Teams, team membership, leadership transfer and team deletion.
- Team invitations (`InvitacionEquipo`) and per-participant team-name history.
- Temporary-credential state and async email notification over RabbitMQ.

## Endpoint Registry

| Capability | Method | Gateway path | Owning service | Status |
|---|---|---|---|---|
| User, role, governance, team and invitation commands/queries | Defined by SDD | Defined by SDD | Identity | Not registered |
