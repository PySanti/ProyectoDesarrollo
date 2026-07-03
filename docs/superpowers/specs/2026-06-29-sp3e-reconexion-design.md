# SP-3e — Reconexión / recuperación de estado transitorio (modalidad Individual) — Design

> **Desambiguación:** "SP-3e" en este documento = slice de **reconexión/mi-sesión** (2026-06-29). No confundir con **SP-3e-1..SP-3e-4** (modalidad Equipo, 2026-07-01/02), que son slices independientes posteriores.

**Fecha:** 2026-06-29
**Servicio:** Operaciones de Sesión
**Slice:** SP-3e (continúa la migración SP-3; sucede a SP-3d runtime BDT)
**Base prevista:** HEAD de la rama `feature/code-migration-SP-3` al iniciar ejecución (registrar en plan/ledger). Base esperada: `d4089ba` (cierre SP-3d con follow-ups).

## 1. Alcance

Materializa **RF-14 / RB-33**: un participante que cierra o pierde la app puede reconectarse y recuperar, en un solo gesto, el **estado vigente que le corresponda** según su inscripción, la modalidad, el estado de la partida y el juego activo. La recuperación es un **snapshot HTTP de solo lectura** (pull); el push en tiempo real es SP-3f.

**Fuentes de autoridad:**
- **RF-14** (SRS): "el sistema debe permitir que un participante se reconecte desde la app móvil a una partida en curso mientras esta siga en estado iniciada, recuperando el estado vigente que le corresponda según su rol, equipo, convocatoria, inscripción, modalidad de la partida y juego activo."
- **RB-33** (SRS): misma regla de negocio.

**Incluye:**
- Endpoint agregador único `GET /operaciones-sesion/mi-sesion`, direccionado **por participante** (JWT `sub`), apoyado en la invariante "una participación activa a la vez" (SP-3a). Sin recordar `partidaId` en el cliente.
- Snapshot unificado `MiSesionDto`: participación (inscripción) + estado de la partida + juego activo + sub-estado vigente (pregunta o etapa activa, según el tipo) + **progreso propio mínimo vigente**.
- Cobertura de los estados **vivos** Lobby + Iniciada con contenido. Una participación cuya partida ya está **Terminada o Cancelada** se considera **no vigente** → `204` (no es "participación activa" según la definición de CLAUDE.md, y RF-14 acota la reconexión a partida "en curso / iniciada"). La pantalla de resultados/cancelación es push (SP-3f) / proyección (SP-4), no esta reconexión.
- Reuso de los sub-DTOs participant-safe existentes (`PreguntaActualDto`, `EtapaActualDto`) → **no-leak** por construcción.

**Difiere explícitamente (NO implementar aquí):**
- Modalidad **Equipo** / convocatoria en la reconexión → **slice-E** (el modelo Equipo aún no existe en Operaciones; este snapshot resuelve participación vía `InscripcionPartida` individual).
- **Push** de reconexión / sincronización en tiempo real (SignalR/WebSockets) → **SP-3f**. Esta slice es pull HTTP.
- Geolocalización y pistas → **SP-3f**.
- Scoring / ranking / puntaje acumulado / posición → **SP-4** (Puntuaciones). El snapshot expone solo estado transitorio, nunca puntaje.
- Reconexión / vista operativa del **Operador** → fuera de objetivo (RF-14/RB-33 son del participante; el operador ya dispone de `GET .../estado`).

## 2. Decisiones (locked en brainstorming)

1. **Forma = agregador único, snapshot completo backend-side.** El backend es dueño de la orquestación ("el sistema recupera"); el cliente reconstruye la pantalla con una llamada.
2. **Direccionamiento = por participante: `GET /mi-sesion`.** El backend halla la única participación activa vigente del participante vía JWT `sub`. Sin `partidaId` en la ruta. Sin participación activa vigente → `204 No Content` (vacío limpio, no error).
3. **Estados cubiertos = Lobby + Iniciada (vivos) con contenido.** "Participación activa vigente" = inscripción `Activa` en una partida en estado **Lobby o Iniciada** (definición de CLAUDE.md). Una inscripción cuya partida ya pasó a **Terminada/Cancelada** NO es vigente → `204`. Esto es fiel al literal "mientras siga iniciada" de RF-14 y evita lógica de desempate (terminal antigua vs nueva en lobby): el query solo considera partidas vivas, por lo que la invariante "una participación activa a la vez" garantiza ≤1 resultado de forma natural. (Refinamiento del self-review respecto al borrador presentado, que contemplaba un snapshot terminal `200`; se descartó por contradecir la definición de participación activa.)
4. **Progreso propio = mínimo vigente.** Solo lo necesario para reanudar la interacción ACTUAL. Tras analizar la mecánica de auto-avance del dominio, el único dato propio **vivo** sobre el sub-estado activo es, en Trivia, **`yaRespondioPreguntaActual`** (bool): Trivia admite una sola respuesta por participante (`RespuestaDuplicadaException`), así que si el participante ya respondió y la pregunta **sigue activa**, su respuesta fue incorrecta (acertar la cierra) → "ya gastaste tu intento, esperando cierre". En BDT **no hay progreso propio vigente** sobre la etapa activa: ganar cierra y auto-avanza la etapa, de modo que `¿ya ganó la etapa activa?` sería siempre `false` (campo muerto), y los reintentos no se exponen (YAGNI). Por tanto el campo se modela como un único `yaRespondioPreguntaActual?` top-level (presente solo con pregunta Trivia activa; `null` en BDT/lobby). Sin histórico, sin puntaje. Respeta la frontera Puntuaciones = SP-4.
5. **Read-only, CQRS Query.** Sin mutación de estado, **sin emisión de eventos**, no toca el publisher. Es un `IRequest<MiSesionDto?>`.
6. **Implementación = proyección directa (enfoque A).** Un handler que carga la `SesionPartida` del participante **una vez** (con eager-load del grafo) y proyecta el DTO. No compone ni acopla los query handlers existentes (evita múltiples cargas y fragilidad).
7. **Individual-only.** El snapshot resuelve participación vía `InscripcionPartida` individual. La rama Equipo/convocatoria queda como punto de extensión para slice-E (el agregador se ampliará entonces para resolver convocatoria de equipo).

## 3. Contrato HTTP

```
GET /operaciones-sesion/mi-sesion        Auth (coarse): Participante
  participanteId = JWT `sub` (nunca body / nunca query string)
  200 + MiSesionDto   → tiene una participación activa vigente (partida en Lobby o Iniciada)
  204 No Content      → no tiene ninguna participación activa vigente
                        (sin inscripción, o su única inscripción está en una partida Terminada/Cancelada)
```

Registrar en `contracts/http/operaciones-sesion-api.md` (Endpoint Registry + DTOs + Notes).

## 4. DTO (Application/DTOs)

```
MiSesionDto {
  partidaId,                // Guid
  sesionPartidaId,          // Guid
  estadoPartida,            // string enum: Lobby | Iniciada (los terminales no llegan a 200; ver §7)
  modalidad,                // string enum: Individual (única en esta slice)
  inscripcion {             // participación del solicitante
     inscripcionId,         // Guid
     estado                 // string enum: Activa | Cancelada (será Activa por construcción del query)
  },
  juegoActivo?  {           // null si la partida no está Iniciada o no hay juego activo
     juegoId, orden, tipoJuego, estadoJuego   // tipoJuego: Trivia | BusquedaDelTesoro
  },
  preguntaActual?,          // PreguntaActualDto (participant-safe, reusado) — solo Trivia activo con pregunta activa
  etapaActual?,             // EtapaActualDto (participant-safe, reusado) — solo BDT activo con etapa activa
  yaRespondioPreguntaActual?  // bool? — true si el participante ya respondió la pregunta Trivia activa;
                              // null cuando no hay pregunta Trivia activa (BDT, lobby, entre activaciones)
}
```

Notas de modelado:
- `MiSesionDto` reusa los sub-DTOs participant-safe; **no** introduce `codigoQREsperado` ni la opción correcta. Garantía no-leak estructural.
- `yaRespondioPreguntaActual` se proyecta como `juego.PreguntaActiva.Respuestas.Any(r => r.ParticipanteId == participanteId)`; es `null` salvo que exista pregunta Trivia activa.
- No se modela progreso propio para BDT (ver decisión 4): la etapa activa nunca está ganada por el solicitante; exponerlo sería un campo muerto.

## 5. Dominio (Domain/)

### Repositorio (aditivo, sin romper firmas existentes)
`ISesionPartidaRepository.GetByParticipanteActivoAsync(Guid participanteId, CancellationToken) → SesionPartida?`
- Devuelve la `SesionPartida` donde el participante tiene una `InscripcionPartida` **activa** (`EsActiva`) **y la partida está en estado Lobby o Iniciada** (vigente), o `null` si no existe.
- **Eager-load del grafo completo** (juegos → preguntas/etapas → respuestas/tesoros) reusando exactamente el `Include` consolidado de SP-3c/SP-3d (T13). Una sola carga.
- Implementación EF en Infrastructure, mismo patrón de consulta que `ParticipanteTieneParticipacionActivaAsync` (filtra por inscripción activa del participante) pero materializando la entidad con `Include` y restringiendo a estados de partida vivos.
- El filtro por estado vivo + la invariante "una participación activa a la vez" garantizan ≤1 resultado; si por defensa hubiera más de uno, tomar el primero determinista (orden estable, p.ej. por `PartidaId`) y registrar — no lanzar.

### Proyección del progreso propio
`yaRespondioPreguntaActual` se calcula leyendo la pregunta Trivia activa: `juego.PreguntaActiva?.Respuestas.Any(r => r.ParticipanteId == participanteId)`. `PreguntaActiva` y `Respuestas` ya están expuestos por `JuegoResumen`/`PreguntaSnapshot` (SP-3c) y `RespuestaTrivia.ParticipanteId` es público — no requiere método nuevo de dominio ni mutación. El handler proyecta directamente; no se añaden predicados nuevos (evita superficie innecesaria). BDT no aporta progreso propio (decisión 4).

## 6. Application (CQRS Query)

- `ObtenerMiSesionQuery(Guid ParticipanteId) : IRequest<MiSesionDto?>` — `ParticipanteId` proviene del claim, inyectado por el controller.
- `ObtenerMiSesionQueryHandler`:
  - `repo.GetByParticipanteActivoAsync(participanteId)`; si `null` → devuelve `null` (controller → 204).
  - Si existe, proyecta `MiSesionDto` según la matriz de estados (§7), reusando `PreguntaActualDto`/`EtapaActualDto` para el sub-estado y los predicados de dominio para `progresoPropio`.
- Sin validator (no hay body; el `participanteId` es del claim, no entrada de usuario).

## 7. Matriz de estados (comportamiento exhaustivo)

| Situación del participante | HTTP | Contenido de `MiSesionDto` |
|---|---|---|
| Sin inscripción activa en ninguna partida | `204` | (cuerpo vacío) |
| Su única inscripción está en una partida **Terminada/Cancelada** (no vigente) | `204` | (cuerpo vacío) |
| Partida en **Lobby** | `200` | estadoPartida=Lobby; juegoActivo=null; sub-estado=null; yaRespondioPreguntaActual=null |
| **Iniciada**, juego **Trivia** activo con pregunta activa | `200` | juegoActivo(Trivia) + preguntaActual + yaRespondioPreguntaActual (true/false) |
| **Iniciada**, juego **BDT** activo con etapa activa | `200` | juegoActivo(BDT) + etapaActual + yaRespondioPreguntaActual=null |
| **Iniciada**, juego activo **sin** pregunta/etapa activa (entre activaciones) | `200` | juegoActivo presente; sub-estado=null; yaRespondioPreguntaActual=null |

Nota: `estadoPartida` en el cuerpo solo toma los valores vivos `Lobby` o `Iniciada` (los terminales nunca llegan a `200` porque el query no los devuelve). El campo se mantiene en el DTO por claridad y para la extensión futura.

## 8. Api (Api/Controllers)

`SesionesController` (añadir tras los endpoints existentes):
```csharp
[HttpGet("mi-sesion")]
public async Task<IActionResult> ObtenerMiSesion(CancellationToken cancellationToken)
{
    var participanteId = ObtenerParticipanteId();              // claim sub (ya existe)
    var dto = await _mediator.Send(new ObtenerMiSesionQuery(participanteId), cancellationToken);
    return dto is null ? NoContent() : Ok(dto);
}
```
Sin nuevos arms de middleware (la ausencia de participación es `204`, no excepción). Auth coarse: Participante.

## 9. No-leak

- `MiSesionDto` reusa `PreguntaActualDto` (sin la opción correcta) y `EtapaActualDto` (sin `codigoQREsperado`).
- `yaRespondioPreguntaActual` es un bool propio del solicitante (no expone respuestas de terceros ni la opción correcta).
- Test estructural/reflexión: `MiSesionDto` y sus sub-DTOs no exponen `codigoQREsperado` ni el flag de opción correcta de la pregunta.

## 10. Testing

- **Repositorio (integration):** `GetByParticipanteActivoAsync` halla la sesión correcta con el grafo cargado; `null` cuando el participante no tiene inscripción activa; ignora inscripciones canceladas; ignora partidas Terminada/Cancelada; ≤1 resultado por la invariante.
- **Handler (unit):** una prueba por fila de la matriz §7 (incluida `null → null` → 204) con la proyección esperada; rama Trivia (con/sin pregunta activa, `yaRespondioPreguntaActual` true/false) y rama BDT (`yaRespondioPreguntaActual=null`).
- **Controller (unit):** `200 + MiSesionDto` con participación; `204` sin participación; `participanteId` tomado del claim `sub` (no del body). Reusa `FakeSender` (sin Moq).
- **Contract e2e (WebApplicationFactory):** reconectar (a) en lobby, (b) con pregunta Trivia activa recuperando la pregunta, (c) con etapa BDT activa recuperando la etapa; aserción no-leak (cuerpo crudo sin `codigoQREsperado` ni opción correcta); `204` sin participación.

## 11. Fronteras y diferimientos (resumen)

| Capacidad | Slice dueña |
|---|---|
| Reconexión modalidad Equipo / convocatoria | slice-E |
| Push reconexión en tiempo real (SignalR) | SP-3f |
| Geolocalización / pistas | SP-3f |
| Scoring / ranking / puntaje en el snapshot | SP-4 |
| Reconexión / vista del Operador | fuera de objetivo (usa `GET estado`) |
| Eventos de dominio | ninguno (query read-only) |

## 12. Watch-items

- **Concurrencia (heredado):** el snapshot es read-only; no agrava el watch-item `sp3f-concurrency-token`. No introduce doble-publish (no publica).
- **Extensión slice-E:** al introducir Equipo, `GetByParticipanteActivoAsync` y `MiSesionDto` deben extenderse para resolver convocatoria de equipo; el contrato `GET /mi-sesion` se mantiene (cambia la resolución de participación, no la forma).
- **Git carve-out (decisión del usuario, vigente):** `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` y `docs/04-sdd/auditorias/` permanecen modificados/sin commitear, reservados para el squash propio del usuario. La fila SP-3e en la matriz se **escribe pero no se commitea** (igual que SP-3c T16 / SP-3d T17).
