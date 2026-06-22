# HU-12 — Filtrar partidas de BDT por modalidad

## User story

Como **Participante**, quiero **filtrar partidas de BDT por modalidad individual o equipo**, para **encontrar rapidamente las busquedas del tesoro que puedo intentar jugar segun la modalidad que me interesa**.

## Source references

- HU: `HU-12` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-05`, `RF-35` en `docs/01-project-source/srs.md`.
- RB: `RB-02`, `RB-03`, `RB-04`, `RB-05`, `RB-B12`, `RB-B13` en `docs/01-project-source/srs.md`.
- RNF: `RNF-01`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-20` en `docs/01-project-source/srs.md`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Mobile context: `mobile/mobile-context.md`, `docs/02-project-context/mobile-participant-context.md`.
- Contract base: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Participante`.

## User goal

Permitir que el participante cambie el listado BDT entre todas las partidas publicadas, partidas individuales y partidas por equipo, sin modificar estado ni inscribirse automaticamente.

## Scope

Included:
- Filtro de modalidad en el panel movil de BDT.
- Modalidades soportadas: `Individual` y `Equipo`.
- Reutilizar la consulta de HU-10 con parametro opcional `modalidad`.
- Mostrar estado vacio especifico cuando no hay partidas publicadas para la modalidad seleccionada.
- Mantener backend como fuente de verdad del filtrado.

Out of scope:
- Ver partidas BDT publicadas sin filtro; eso pertenece a HU-10.
- Validar liderazgo al intentar entrar a BDT por equipo; eso pertenece a HU-14.
- Inscripcion individual BDT; eso pertenece a HU-39.
- Unir equipo a BDT; eso pertenece a HU-40.
- Crear o publicar partidas BDT; eso pertenece a HU-34.
- Tiempo real para cambios de listado; no es requerido para cerrar HU-12.

## Preconditions

- Usuario autenticado con rol base `Participante`.
- Existe la consulta de partidas BDT publicadas definida para HU-10.
- La app movil puede enviar `modalidad=Individual` o `modalidad=Equipo`.

## Postconditions

- La consulta no modifica estado del sistema.
- La app movil muestra solo partidas publicadas que coinciden con la modalidad seleccionada.
- La seleccion del filtro no crea inscripciones ni valida liderazgo.

## Business rules

- `RF-05`: cada panel movil permite listar partidas publicadas y filtrar por modalidad.
- `RB-04`: cada panel de la app movil debe permitir filtrar partidas por modalidad individual o equipo.
- `RB-05`: todas las partidas publicadas se muestran a todos los jugadores; el filtro solo reduce la vista.
- `RB-B12`: una BDT publicada aparece en el panel BDT.
- `RB-B13`: cualquier jugador puede intentar entrar a una BDT publicada, pero HU-12 solo filtra visibilidad.
- El backend filtra por modalidad; mobile no debe duplicar reglas autoritativas.

## Related requirements

- `RF-05`
- `RF-35`
- `RNF-01`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-20`

## Acceptance criteria

1. Un participante autenticado puede seleccionar filtro `Todas`, `Individual` o `Equipo` en el panel BDT.
2. Con filtro `Todas`, se muestran partidas publicadas individuales y por equipo.
3. Con filtro `Individual`, se muestran solo partidas BDT publicadas de modalidad individual.
4. Con filtro `Equipo`, se muestran solo partidas BDT publicadas de modalidad equipo.
5. Si no hay resultados para la modalidad seleccionada, se muestra estado vacio claro.
6. Un valor de modalidad invalido en el contrato HTTP responde `400`.
7. El endpoint exige autenticacion; sin token valido responde `401`.
8. Un actor autenticado sin rol participante recibe `403` cuando aplique por politica.
9. Cambiar el filtro no crea inscripciones ni modifica partidas.
10. La app no decide si el participante puede inscribir un equipo; esa validacion pertenece a HU-14/HU-40.

## Open questions

- Ninguna bloqueante para completar el SDD de HU-12.

## Assumptions

- HU-12 reutiliza `GET /api/bdt/games/published` agregando `modalidad` opcional.
- El filtro `Todas` se implementa omitiendo el parametro `modalidad`.
- El filtrado se resuelve en backend para mantener el contrato como fuente de verdad.
