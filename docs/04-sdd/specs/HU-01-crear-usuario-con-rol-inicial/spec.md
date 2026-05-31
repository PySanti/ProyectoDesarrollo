# HU-01 — Crear usuario con rol inicial

## User story

Como **Administrador**, quiero **crear usuarios en la plataforma y asignarles un rol inicial**, para **establecer y controlar los accesos seguros al sistema**.

## Source references

- HU: `docs/01-project-source/historias-de-usuario.md`
- RF: `RF-01` in `docs/01-project-source/srs.md`
- RB: `RB-U01`, `RB-U02`, `RB-U03`, `RB-U04`, `RB-U05`, `RB-U06`, `RB-U07`
- RNF: `RNF-01`, `RNF-13`, `RNF-14`
- Ownership: `docs/03-microservices/service-ownership.md`
- Traceability: `docs/04-sdd/traceability-matrix.md`
- Service context: `services/identity-service/service-context.md`
- Contract draft: `contracts/http/identity-api.md`

## Actor

- `Administrador`

## User goal

Permitir al administrador crear usuarios desde UMBRAL, asignando un rol inicial y coordinando la identidad con Keycloak sin almacenar contrasenas en la base local.

## Scope

Included:
- Crear usuario desde cliente React web.
- Validar autorizacion de administrador.
- Asignar rol inicial durante creacion.
- Crear identidad en Keycloak.
- Persistir usuario local con referencia `KeycloakId`.
- Retornar datos del usuario creado.

Excluded:
- Cambio de rol posterior a la creacion.
- Edicion y consulta de usuarios existentes.
- Desactivacion de usuarios.
- Gestion de equipos o flujos de juego.

## Preconditions

- El actor autenticado tiene permisos de `Administrador`.
- Keycloak esta disponible para la operacion de alta.
- El request contiene nombre, correo y rol inicial validos.
- El correo no esta duplicado segun reglas del servicio.

## Postconditions

- Existe un usuario creado en Keycloak.
- Existe un usuario local asociado con `KeycloakId`.
- El rol inicial queda asignado.
- El estado inicial del usuario es `Activo`.
- No se almacena contrasena ni credencial sensible en UMBRAL.

## Business rules

- `RB-U01`: Keycloak gestiona autenticacion.
- `RB-U02`: los roles base son `Administrador`, `Operador`, `Participante`.
- `RB-U03`: UMBRAL no almacena contrasenas.
- `RB-U04`: UMBRAL almacena referencia local del identificador de Keycloak.
- `RB-U05`: el administrador puede crear usuarios desde UMBRAL.
- `RB-U06`: el rol inicial se asigna durante creacion.
- `RB-U07`: el rol no puede modificarse desde UMBRAL despues de la creacion.

## Related requirements

- `RF-01`
- `RNF-01`
- `RNF-13`
- `RNF-14`

## Acceptance criteria

1. Un administrador puede crear un usuario con nombre, correo y rol inicial valido.
2. El usuario creado queda asociado a una referencia local de Keycloak.
3. El rol inicial se asigna en creacion y queda persistido.
4. Si el correo ya existe, la operacion falla con conflicto.
5. Si el actor no es administrador, la operacion falla por autorizacion.
6. Si falla la integracion con Keycloak, no debe quedar persistencia local inconsistente.
7. UMBRAL no almacena contrasena ni credenciales sensibles del usuario creado.

## Open questions

- Ninguna indispensable para esta historia. Los detalles finales de payload y errores se confirman en `design.md` y `contracts/http/identity-api.md`.
