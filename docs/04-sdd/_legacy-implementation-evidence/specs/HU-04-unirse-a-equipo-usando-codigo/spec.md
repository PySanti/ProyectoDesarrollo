# HU-04 — Unirse a equipo usando codigo

## User story

Como **Participante**, quiero **unirme a un equipo usando un codigo**, para **formar parte de un equipo existente**.

## Source references

- HU: `HU-04` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`
- RF: `RF-07` en `docs/01-project-source/srs.md`
- RB: `RB-E01`, `RB-E03`, `RB-E06`, `RB-E07` en `docs/01-project-source/srs.md`
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-14` en `docs/01-project-source/srs.md`
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`
- Traceability base: `docs/04-sdd/traceability-matrix.md`
- Service context: `services/team-service/service-context.md`
- Contract base: `contracts/http/team-api.md`, `contracts/events/team-events.md`
- Resolved decisions: `docs/02-project-context/known-ambiguities-and-decisions.md`

## Actor

- `Participante`

## User goal

Permitir que un participante autenticado se una a un equipo activo existente mediante un codigo de acceso valido, siempre que no pertenezca ya a otro equipo activo y que el equipo destino no supere la cardinalidad maxima de 5 integrantes.

## Scope

Included:
- Unirse a un equipo desde la app movil de participante (`React Native mobile`).
- Validar que el codigo de acceso exista y corresponda a un equipo activo.
- Validar que el participante no pertenezca a otro equipo activo.
- Validar que el equipo no tenga 5 integrantes ya registrados.
- Agregar al participante como nuevo integrante no lider del equipo.
- Retornar datos minimos del equipo actualizado para la UI movil.

Out of scope:
- Crear equipo (HU-03).
- Transferir liderazgo (HU-06).
- Salir de equipo (HU-07).
- Eliminar equipo (HU-05).
- Gestion administrativa de equipos por Administrador (HU-08).
- Inscripcion de equipos en partidas.

## Preconditions

- Usuario autenticado con rol base `Participante`.
- Usuario activo en Identity Service.
- Usuario no pertenece actualmente a otro equipo activo.
- El codigo de acceso ingresado corresponde a un equipo activo existente.
- El equipo destino tiene menos de 5 integrantes.

## Postconditions

- El participante queda agregado como integrante del equipo destino.
- El participante no queda marcado como lider.
- El equipo mantiene su estado y su cardinalidad valida `1..5`.
- El codigo de acceso del equipo no cambia.

## Business rules

- `RF-07`: un participante puede unirse a un equipo con codigo valido, no puede pertenecer a mas de un equipo y el equipo no puede superar 5 jugadores.
- `BR-E01`: un participante puede pertenecer a maximo un equipo activo.
- `BR-E02`: un equipo puede existir con `1..5` integrantes.
- `BR-E04`: Team Service debe rechazar intentos de agregar un sexto integrante.
- `BR-E05`: un participante solo puede unirse usando un codigo de acceso valido.
- Decision resuelta: `1 <= Equipo.Participantes.Count <= 5` (no minimo 2).

## Related requirements

- `RF-07`
- `RNF-01`
- `RNF-02`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-14`

## Acceptance criteria

1. Un participante autenticado puede unirse a un equipo activo si provee un codigo de acceso valido y no pertenece a otro equipo activo.
2. Si el codigo de acceso no existe o no corresponde a un equipo activo, la operacion falla sin modificar estado.
3. Si el participante ya pertenece a otro equipo activo, la operacion falla con conflicto de negocio (`409`).
4. Si el equipo ya tiene 5 integrantes, la operacion falla con conflicto de negocio (`409`).
5. Al unirse exitosamente, el participante queda agregado como integrante no lider del equipo.
6. El endpoint exige autenticacion; sin token valido responde `401`.
7. Un actor autenticado sin condicion valida de participante para este flujo recibe `403` cuando aplique por politica.
8. La respuesta exitosa retorna identificador de equipo, nombre, codigo de acceso, lider e integrantes actuales.

## Open questions

- Ninguna bloqueante para completar SDD de HU-04.

## Assumptions

- Para HU-04, Team Service es la fuente de verdad para validar codigo de acceso, estado del equipo y membresia activa del participante.
- La autorizacion base se evalua por token/claims ya disponibles en backend; no se requiere consulta runtime obligatoria a Identity Service para cerrar HU-04.
