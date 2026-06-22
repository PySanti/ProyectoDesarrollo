# HU-10 — Ver partidas de BDT publicadas

## User story

Como **Participante**, quiero **ver las partidas de BDT publicadas**, para **conocer las busquedas del tesoro disponibles en las que podria participar**.

## Source references

- HU: `HU-10` en `docs/01-project-source/srs.md` y `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-05`, `RF-13`, `RF-25`, `RF-27`, `RF-35` en `docs/01-project-source/srs.md`.
- RB: `RB-02`, `RB-03`, `RB-05`, `RB-B12`, `RB-B13` en `docs/01-project-source/srs.md`.
- RNF: `RNF-01`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-20` en `docs/01-project-source/srs.md`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Mobile context: `mobile/mobile-context.md`, `docs/02-project-context/mobile-participant-context.md`.
- Contract base: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Participante`.

## User goal

Mostrar en la app movil el panel de Busqueda del Tesoro con las partidas BDT publicadas en estado `Lobby`, sin exigir que el participante ya este inscrito.

## Scope

Included:
- Consulta de partidas BDT publicadas desde React Native mobile.
- Mostrar partidas BDT publicadas en estado `Lobby` para participantes autenticados.
- Incluir datos minimos para una lista movil: identificador, nombre, modalidad, estado, area de busqueda textual y cantidad de etapas.
- Manejar estados de carga, lista vacia y error de consulta en la app movil.
- Consumir contrato HTTP documentado por BDT Game Service.

Out of scope:
- Filtrar por modalidad; eso pertenece a HU-12, aunque usa el mismo endpoint con query opcional.
- Unirse a BDT individual; eso pertenece a HU-39.
- Unir equipo a BDT por equipos; eso pertenece a HU-40.
- Advertencia por no ser lider en BDT por equipo; eso pertenece a HU-14.
- Crear o publicar partidas BDT; eso pertenece a HU-34 y cliente web de operador.
- Monitoreo de lobby por operador; eso pertenece a HU-42.
- Tiempo real para nuevas publicaciones; no es requerido para cerrar HU-10.

## Preconditions

- Usuario autenticado con rol base `Participante`.
- Existe BDT Game Service como fuente de verdad de partidas BDT.
- Las partidas visibles para HU-10 estan publicadas en estado `Lobby`.
- La app movil tiene acceso al contrato HTTP documentado.

## Postconditions

- La consulta no modifica estado del sistema.
- La app movil muestra la lista de partidas BDT publicadas o un estado vacio claro.
- No se inscribe al participante ni se valida liderazgo en esta HU.

## Business rules

- `RF-05`: la app movil del participante debe mostrar partidas publicadas en paneles separados de Trivia y BDT.
- `RB-02`: el participante debe tener panel principal de Busqueda del Tesoro.
- `RB-03`: el panel BDT muestra partidas publicadas de ese tipo de juego.
- `RB-05`: todas las partidas publicadas se muestran a todos los jugadores, sin importar modalidad.
- `RB-B12`: al crear el lobby, la BDT queda publicada para todos los jugadores en el panel BDT.
- `RB-B13`: cualquier jugador puede intentar entrar a una BDT publicada, pero la visibilidad no implica autorizacion para inscribirse.
- BDT Game Service no debe calcular ranking ni puntaje para esta consulta.

## Related requirements

- `RF-05`
- `RF-13`
- `RF-25`
- `RF-27`
- `RF-35`
- `RNF-01`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-20`

## Acceptance criteria

1. Un participante autenticado puede abrir el panel movil de Busqueda del Tesoro.
2. El panel consulta BDT Game Service mediante contrato HTTP documentado.
3. La respuesta incluye solo partidas BDT publicadas en estado `Lobby`.
4. Se muestran partidas individuales y por equipo sin filtrar por defecto.
5. Cada partida muestra como minimo nombre, modalidad, estado, area de busqueda textual y cantidad de etapas.
6. Si no hay partidas publicadas, la app muestra un estado vacio claro.
7. Si la consulta falla, la app muestra un error claro sin inventar reglas de negocio.
8. La consulta no modifica estado y no crea inscripciones.
9. El endpoint exige autenticacion; sin token valido responde `401`.
10. Un actor autenticado sin rol participante recibe `403` cuando aplique por politica.

## Open questions

- Ninguna bloqueante para completar el SDD de HU-10.

## Assumptions

- `Publicada` para el listado participante se representa como partida BDT en estado `Lobby`.
- El listado se implementa como query HTTP; actualizaciones en tiempo real de nuevas publicaciones quedan fuera de HU-10.
- La misma consulta se reutiliza por HU-12 con parametro opcional de modalidad.
