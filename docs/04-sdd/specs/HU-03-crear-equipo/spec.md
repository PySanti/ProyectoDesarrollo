# HU-03 — Crear equipo

## User story

Como **Participante**, quiero **crear un equipo**, para **participar en partidas (Trivia o BDT) de equipo**.

## Source references

- HU: `HU-03` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`
- RF: `RF-07` en `docs/01-project-source/srs.md`
- RB: `RB-E01`, `RB-E02`, `RB-E04`, `RB-E05`, `RB-E06`, `RB-E07` en `docs/01-project-source/srs.md`
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-14` en `docs/01-project-source/srs.md`
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`
- Traceability base: `docs/04-sdd/traceability-matrix.md`
- Service context: `services/team-service/service-context.md`
- Contract base: `contracts/http/team-api.md`, `contracts/events/team-events.md`
- Resolved decisions: `docs/02-project-context/known-ambiguities-and-decisions.md`

## Actor

- `Participante`

## User goal

Permitir que un participante cree un equipo global de UMBRAL cuando no pertenezca a otro equipo activo, quedando automáticamente como primer integrante y líder, con un código único de acceso.

## Scope

Included:
- Crear equipo desde la app móvil de participante (`React Native mobile`).
- Validar que el participante no pertenezca a otro equipo activo.
- Crear equipo en estado activo.
- Registrar al creador como primer integrante del equipo.
- Marcar al creador como líder del equipo.
- Generar código único de acceso para el equipo.
- Retornar datos necesarios del equipo creado para UI móvil.

Out of scope:
- Unirse a equipo por código (HU-04).
- Transferir liderazgo (HU-06).
- Salir de equipo (HU-07).
- Eliminar equipo (HU-05).
- Gestión administrativa de equipos por Administrador (HU-08).
- Inscripción de equipos en partidas.

## Preconditions

- Usuario autenticado con rol base `Participante`.
- Usuario activo en Identity Service.
- Usuario no pertenece actualmente a otro equipo activo.

## Postconditions

- Se crea un nuevo `Equipo` activo.
- El creador queda como integrante inicial del equipo.
- El creador queda marcado como líder del equipo.
- Se asigna un `CodigoAcceso` único.
- Se conserva la invariante de cardinalidad `1..5` (inicia en 1).

## Business rules

- `RF-07`: creación permitida solo si no pertenece a otro equipo, código único, límite de 5, equipos globales Trivia/BDT.
- `BR-E01`: un participante puede pertenecer a máximo un equipo activo.
- `BR-E02`: un equipo puede existir con `1..5` integrantes.
- `BR-E03`: el creador se registra automáticamente como primer integrante y líder.
- `BR-E04`: no exceder 5 integrantes.
- `BR-E10`: liderazgo es condición de dominio, no rol Keycloak.
- Decisión resuelta: `1 <= Equipo.Participantes.Count <= 5` (no mínimo 2).

## Related requirements

- `RF-07`
- `RNF-01`
- `RNF-02`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-14`

## Acceptance criteria

1. Un participante autenticado puede crear equipo si no pertenece a otro equipo activo.
2. Al crear el equipo, el sistema registra al creador como primer integrante.
3. Al crear el equipo, el sistema registra al creador como líder.
4. El sistema genera un código único de acceso al equipo.
5. Si el participante ya pertenece a un equipo activo, la creación falla con conflicto de negocio (`409`).
6. El equipo creado queda en estado activo.
7. La respuesta de creación retorna identificador de equipo, nombre, código de acceso, líder e integrantes actuales.
8. El endpoint exige autenticación; sin token válido responde `401`.
9. Un actor autenticado sin condición válida de participante para este flujo recibe `403` cuando aplique por política.

## Open questions

- Ninguna bloqueante para completar SDD de HU-03.

## Assumptions

- Para HU-03, la validación de “no pertenencia a otro equipo activo” se resuelve dentro de Team Service con su propio modelo/persistencia.
- La autorización base se evalúa por token/claims ya disponibles en backend; la condición de liderazgo no aplica en HU-03.
