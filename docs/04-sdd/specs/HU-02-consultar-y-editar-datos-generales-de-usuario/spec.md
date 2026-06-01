# HU-02 — Consultar y editar datos generales de usuario

## User story

Como **Administrador**, quiero **consultar y editar datos generales de usuario**, para **mantener actualizada y controlada la base de usuarios**.

## Source references

- HU: `HU-02` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`
- RF: `RF-01` en `docs/01-project-source/srs.md`
- RB: `RB-U01`, `RB-U03`, `RB-U07`, `RB-U08`, `RB-U09` en `docs/01-project-source/srs.md`
- RNF: `RNF-01`, `RNF-13`, `RNF-14` en `docs/01-project-source/srs.md`
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`
- Traceability: `docs/04-sdd/traceability-matrix.md`
- Service context: `services/identity-service/service-context.md`
- Contract base: `contracts/http/identity-api.md`

## Actor

- `Administrador`

## User goal

Permitir al administrador consultar usuarios existentes, ver su detalle, editar datos generales y desactivar usuarios, sin modificar roles ni almacenar credenciales sensibles en UMBRAL.

## Scope

Included:
- Consultar listado de usuarios.
- Consultar detalle de usuario por identificador.
- Editar datos generales permitidos (`name`, `email`).
- Desactivar usuario.
- Restringir endpoints a actor autenticado con rol `Administrador`.
- Mantener el rol del usuario sin cambios desde UMBRAL.

Excluded:
- Creacion de usuarios (HU-01).
- Cambio de rol posterior a la creacion.
- Gestion de contrasenas o credenciales de usuario.
- Reactivacion de usuarios.
- Flujos de equipos, Trivia o BDT.

## Preconditions

- El actor esta autenticado y autorizado como `Administrador`.
- Identity Service esta disponible.
- Para detalle/edicion/desactivacion, el usuario objetivo existe.
- Para actualizacion de correo, el nuevo correo no colisiona con otro usuario existente.

## Postconditions

- En consultas no se modifica estado.
- En edicion se actualizan solo los campos permitidos de datos generales.
- El rol se mantiene inmutable desde UMBRAL.
- En desactivacion, el usuario pasa a estado `Desactivado`.
- No se almacenan contrasenas ni credenciales sensibles en UMBRAL.

## Business rules

- `RB-U01`: Keycloak gestiona autenticacion.
- `RB-U03`: UMBRAL no almacena contrasenas ni credenciales sensibles.
- `RB-U07`: No se permite modificar el rol desde UMBRAL luego de la creacion.
- `RB-U08`: El administrador puede consultar, editar datos generales y desactivar usuarios.
- `RB-U09`: Un usuario desactivado no puede operar dentro del sistema.

## Related requirements

- `RF-01`
- `RNF-01`
- `RNF-13`
- `RNF-14`

## Acceptance criteria

1. Un administrador autenticado puede consultar el listado de usuarios.
2. Un administrador autenticado puede consultar el detalle de un usuario por `userId`.
3. Un administrador autenticado puede editar nombre y correo de un usuario existente.
4. El endpoint de edicion no permite cambiar `role`.
5. Si el correo ya existe en otro usuario, la edicion falla con conflicto de negocio (`409`).
6. Si el usuario no existe, detalle/edicion/desactivacion fallan con `404`.
7. Si el actor no es administrador, las operaciones fallan por autorizacion (`403`).
8. Un administrador autenticado puede desactivar un usuario existente.
9. El estado `Desactivado` queda reflejado en consultas posteriores.
10. No se almacenan contrasenas ni credenciales sensibles durante el flujo.

## Open questions

- Ninguna bloqueante para crear el SDD.

## Assumptions

- Por decision explicita del usuario en esta sesion, HU-02 incluye desactivacion ademas de consulta y edicion, aunque el titulo de la HU mencione solo consulta/edicion.
