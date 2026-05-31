# CrearUsuarioConRolInicialCommandHandler

Application handler definition for HU-01 in Identity Service.

## Responsibility

Handle `CrearUsuarioConRolInicialCommand` and coordinate:

1. validated input from command validator,
2. Keycloak user creation through application port,
3. local `Usuario` creation with domain invariants,
4. local persistence through repository port,
5. response mapping for the API layer.

## Inputs

- `CrearUsuarioConRolInicialCommand`
  - `Name`
  - `Email`
  - `InitialRole`

## Dependencies (application ports)

- `IUsuarioRepository` (to be defined in next task)
- `IKeycloakIdentityPort` (to be defined in next task)

## Handling flow

1. Receive validated command.
2. Request Keycloak identity creation and initial role assignment.
3. Build local `Usuario` aggregate/entity using domain rules:
   - creation invariants from
     `services/identity-service/domain/usuario-creation-invariants.md`
   - allowed role values from
     `services/identity-service/domain/rol-usuario-allowed-values.md`
4. Persist local user through repository port.
5. Return application response DTO/read model (defined in later task).

## Error handling boundaries

- Keycloak integration failures are surfaced to application/API mapping.
- Persistence failures are surfaced to application/API mapping.
- Duplicate email and business conflicts are handled by domain/application
  rules during later tasks.

## Scope note

This task defines the handler behavior and orchestration boundaries only.
Concrete repository, Keycloak port, and response model are intentionally left
to their dedicated pending tasks.
