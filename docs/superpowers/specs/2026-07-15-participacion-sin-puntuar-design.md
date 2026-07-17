# Participación sin puntuar: historial y rankings incluyen a todo el que jugó

**Fecha:** 2026-07-15
**Servicio:** Puntuaciones (único)
**Tipo:** corrección sobre HU ya implementadas (HU-27/HU-43/HU-49, RF-24/RF-44/RF-45). No introduce HU nueva.

## 1. Problema

Quien no anota no existe. Un participante que jugó la partida entera, respondió todas las preguntas
pero nunca llegó primero, termina invisible: no sale en el ranking final, no sale en el ranking en
vivo que mira el operador, y la partida **no aparece en su historial**. Desde su móvil es como si
nunca la hubiera jugado.

Reportado por el usuario. "Partidas jugadas" hoy significa en realidad "partidas donde puntuaste", y
no es lo mismo.

## 2. Hechos verificados

Todo lo de abajo se comprobó leyendo el código, no la documentación.

| # | Hecho | Evidencia |
|---|---|---|
| H1 | El marcador se crea **perezosamente al acreditar puntos**: si no puntúas, no existe | `ProyectarPuntajeTriviaCommandHandler.cs:31-36` |
| H2 | `PuntajeTriviaIncrementado` solo se publica cuando la respuesta **cerró la pregunta** (primer acierto). En Trivia la pregunta cierra al primer acierto, así que **puntúa un solo competidor por pregunta** | `ResponderPreguntaCommandHandler.cs:42-50` |
| H3 | El consolidado toma como universo **solo los marcadores** | `CalculadorRankingConsolidado.cs:12`, `:37` (`GroupBy(m => m.CompetidorId)`) |
| H4 | El ranking nativo por juego, **igual** | `RankingCalculator.cs:8-13` |
| H5 | El ranking en vivo que se empuja por SignalR reusa esa misma query, así que web y móvil beben de la misma fuente | `RankingBroadcastDispatcher.cs:32-37` |
| H6 | El historial del participante exige ≥1 marcador propio | `ProyeccionesRepository.cs:60-64`; comentario del handler: *"el integrante que jamás autoró una acción de juego no ve la partida"* (`ObtenerHistorialPartidasQueryHandler.cs:10-14`) |
| H7 | El rendimiento de equipo exige que el equipo **anotara** | `ObtenerRendimientoEquipoQueryHandler.cs:8-9`, `ProyeccionesRepository.cs:51-53` |
| H8 | `InscripcionAceptada` **ya se publica al broker** desde el Bloque 4B y **ya se persiste** en `EventosHistorial` con sus ids extraídos | `SesionEventRouting.cs:28`, `HistorialEventMapper.cs:34` |
| H9 | La cola de proyección liga **rutas explícitas** (7): no recibe inscripciones ni convocatorias | `RabbitMqConsumerOptions.cs:17-26` |
| H10 | `ConvocatoriaRespondidaEvent` **no lleva `EquipoId`**: sabe quién aceptó, no de qué equipo | `ParticipacionEvents.cs:6-7` |
| H11 | `ConvocatoriaCreadaEvent` **sí** lleva el par `EquipoId` + `UsuarioId`, y también `ConvocatoriaId` | `ParticipacionEvents.cs:3-4` |
| H12 | El estado publicado es el `ToString()` del enum `{Pendiente, Aceptada, Rechazada}` | `ResponderConvocatoriaCommandHandler.cs:43-46`, `EstadoConvocatoria.cs:3` |
| H13 | Ya existe precedente de resolver participación consultando `EventosHistorial`, excluyendo `ConvocatoriaCreada` a propósito (que te convoquen no es jugar: puedes rechazar) | `HistorialRepository.cs:52-58` |

## 3. Causa raíz

El contrato la declara — y **da una razón que hoy es falsa** (`contracts/http/puntuaciones-api.md:104`):

> *"**Participación = tener ≥1 marcador**: competidores que nunca anotaron no aparecen (**no hay
> evento de inscripción en el broker**; best-effort ADR-0012)."*

Eso era cierto cuando se escribió. El **Bloque 4B añadió `InscripcionAceptada` al broker** (H8). La
premisa que obligaba a atar la participación al marcador desapareció y nadie revisó la consecuencia.

Este slice no fuerza nada: cobra trabajo ya hecho. Puntuaciones tiene un universo de competidores
disponible desde hace tiempo y sigue usando el marcador como sustituto.

## 4. Decisiones

| Decisión | Valor | Motivo |
|---|---|---|
| **Qué es participar** | **Inscripción aceptada** | Fuiste aceptado y la partida arrancó contigo dentro: eres competidor y sales con 0 si no anotaste. Coherente con que los mínimos de participación te cuentan para que la partida pueda empezar. No se exige "haber actuado". |
| **Miembro de equipo pasivo** | **Incluido en este slice** | El criterio de arriba, aplicado al miembro, es "convocatoria aceptada". Dejarlo fuera sería incoherente. |
| **Backfill** | **No** | Proyecto académico con datos de prueba: si molesta, se re-siembra. Evita código de un solo uso. Las partidas anteriores conservan el comportamiento actual. |
| **Fuente de la participación** | **Proyecciones propias**, no consultar `EventosHistorial` | Ver §5. |

## 5. Alternativas consideradas

**Consultar `EventosHistorial`** (hay precedente, H13; y funcionaría para el ranking porque
`InscripcionAceptada` ya está ahí). **Descartada**: para el caso del miembro se rompe — el
`convocatoriaId` necesario para unir creación y respuesta vive dentro del `DetalleJson`, así que
sería un join sobre JSON. Y convierte un relato de auditoría en fuente de un cálculo: el historial
está para narrar, no para rankear.

**Preguntar a Operaciones de Sesión.** **Descartada**: viola la frontera dura (BR-G08), y Operaciones
solo guarda estado transitorio — de una partida terminada puede no quedar nada.

## 6. Arquitectura

Hoy los marcadores son el **único universo** de competidores. Pasan a ser
`participaciones ∪ marcadores`: quien no tiene marcador entra con 0 puntos, 0 ms y 0 juegos ganados.

Sin endpoints nuevos y **sin cambiar la forma de ningún DTO**: cambia **quién** sale en las listas, no
**cómo** sale. Solo se toca Puntuaciones — ni Operaciones de Sesión, ni Identity, ni el gateway, ni
los clientes.

### P1 — Proyección de participación

Tabla `participaciones_proyectadas`: `(PartidaId, CompetidorId, TipoCompetidor)`, clave compuesta
`(PartidaId, CompetidorId)`. Alimentada por `InscripcionAceptada`: en `Individual` la fila es el
participante (`TipoCompetidor.Participante`); en `Equipo`, el equipo (`TipoCompetidor.Equipo`). El
evento ya trae `participanteId` **xor** `equipoId` según modalidad.

Es el universo de los dos rankings.

### P2 — Proyección de convocatorias

Tabla `convocatorias_proyectadas`: `(ConvocatoriaId PK, PartidaId, EquipoId, UsuarioId, Aceptada)`.

- `ConvocatoriaCreada` inserta la fila con `Aceptada = false` (trae `ConvocatoriaId`, `EquipoId` y
  `UsuarioId`, H11).
- `ConvocatoriaRespondida` la localiza por `ConvocatoriaId` y fija
  `Aceptada = (estadoConvocatoria == "Aceptada")` (H12).

Hacen falta las dos: la respuesta no sabe el equipo (H10) y la creación no sabe si aceptó. Es el
vínculo miembro↔equipo↔partida, que hoy Puntuaciones no tiene de ninguna forma.

### P3 — Cableado

Tres rutas nuevas en `RabbitMqConsumerOptions.Bindings` (H9), que hoy liga siete:

```
operaciones-sesion.inscripcion-aceptada.v1
operaciones-sesion.convocatoria-creada.v1
operaciones-sesion.convocatoria-respondida.v1
```

Tres casos en `ProyeccionEventMapper.Map`, tres commands y tres handlers, con el patrón de
idempotencia ya existente (`EventoYaProcesadoAsync` + `RegistrarEventoProcesado`).

### C1 — Los dos calculadores

`RankingCalculator.Calcular(marcadores)` y `CalculadorRankingConsolidado.Calcular(marcadores)` pasan a
recibir también las participaciones de la partida. El competidor sin marcador entra con ceros.

`RankingCalculator` es **por juego** y las participaciones son **por partida**: quien participa en la
partida participa en todos sus juegos, así que el universo de cada juego es
`participaciones(partida) ∪ marcadores(juego)`.

Dos competidores sin anotar quedan a `0/0/0` → empate exacto → **comparten posición**. Los dos
calculadores ya resuelven así los empates exactos y no hay que tocar esa lógica.

Efecto visible más llamativo: hoy, al arrancar un juego, **nadie ha puntuado y el operador ve una
tabla vacía** (`contracts/http/puntuaciones-api.md:53`). Con el cambio ve a todos empatados a 0 y el
ranking se ordena según puntúan — que es lo que necesita para operar.

### C2 — Historial del participante

`ObtenerHistorialPartidasQueryHandler`:

- **Individual:** partidas terminadas con **participación propia** (antes: con marcador propio).
- **Equipo:** partidas donde el usuario tiene **convocatoria aceptada** (P2) **y** el equipo tiene
  participación (antes: pertenencia resuelta por acciones autoradas + ≥1 marcador del equipo).

**Las dos condiciones del caso Equipo son necesarias; no simplificar a una.** En teoría la segunda
sobra, porque las convocatorias solo se crean al aceptar la preinscripción, así que un miembro
convocado implica un equipo aceptado. Pero la entrega es best-effort (ADR-0012): si se perdiera
`InscripcionAceptada` y sí llegara `ConvocatoriaCreada`, el equipo no estaría ni en participaciones
ni en marcadores, y el `entradas.First(e => e.CompetidorId == competidorId)` del handler
(`ObtenerHistorialPartidasQueryHandler.cs:67`) **lanzaría**. Exigir participación del equipo es lo
que mantiene esa línea segura.

Retira la limitación documentada en H6. El comentario del handler que dice *"la participación exige
≥1 marcador del competidor, así que la entrada siempre existe"* debe reescribirse: la entrada sigue
existiendo siempre, pero ahora porque el universo del calculador incluye las participaciones — y
porque los dos filtros de arriba garantizan que el competidor está en ese universo.

Las canceladas siguen excluidas (RB-30): eso es deliberado y no se toca.

### C3 — Rendimiento de equipo

`ObtenerRendimientoEquipoQueryHandler`: partidas terminadas de modalidad `Equipo` con
**participación del equipo** (antes: donde el equipo anotó).

## 7. Errores y degradación

Todo degrada al comportamiento de hoy; nada rompe. Coherente con best-effort (ADR-0012):

- Si se pierde `InscripcionAceptada`, el competidor no aparece → exactamente lo de hoy.
- Si se pierde `ConvocatoriaCreada`, la respuesta no encuentra fila que actualizar: se loguea y se
  ackea (no se puede crear la fila, falta el `EquipoId`). El miembro cae al comportamiento de hoy.
- Partidas anteriores al cambio: sin filas de participación → comportamiento de hoy (sin backfill,
  decidido en §4).

## 8. Testing

**Unidad de los calculadores** — el competidor con participación y sin marcador sale último con 0
puntos y 0 tiempo; dos así empatan y comparten posición; con marcadores el orden actual no cambia
(regresión). Aplica a los dos calculadores.

**Unidad de los proyectores** — `InscripcionAceptada` en Individual proyecta al participante y en
Equipo al equipo; `ConvocatoriaCreada` inserta con `Aceptada = false`; `ConvocatoriaRespondida` marca
según el estado y es no-op si no existe la fila; los tres son idempotentes ante evento repetido.

**Integración** — el historial del participante incluye una partida terminada donde no puntuó; el
rendimiento de equipo incluye una partida donde el equipo no anotó; el miembro que aceptó su
convocatoria y nunca actuó ve la partida.

**Contract** — los contract tests existentes deben seguir verdes **sin tocarlos**: la forma de los
DTOs no cambia. Eso es parte de la verificación, no un trámite.

## 9. Contratos

`contracts/http/puntuaciones-api.md` tiene cuatro sitios que quedarían mintiendo:

- **:53** — "Juego conocido sin marcadores → `200` con `entradas: []`".
- **:104** — "Participación = tener ≥1 marcador… no hay evento de inscripción en el broker": la
  premisa es falsa desde el Bloque 4B.
- **:109** — rendimiento de equipo "donde el equipo tiene ≥1 marcador".
- **:201-203** — participación del historial por marcador propio / membresía resuelta del historial.

## 10. Criterios de aceptación

1. Un participante aceptado que no anotó **aparece** en el ranking final, último, con 0 puntos.
2. Al arrancar un juego, el ranking por juego lista a **todos** los competidores a 0, en vez de vacío.
3. Una partida terminada donde el participante no puntuó **aparece** en su historial.
4. Una partida donde el equipo no anotó **aparece** en el rendimiento del equipo.
5. El miembro que aceptó su convocatoria y nunca actuó ve la partida en su historial.
6. Dos competidores sin puntuar comparten posición.
7. La suite de Puntuaciones sigue verde, **incluidos los contract tests sin modificar**.
8. Las partidas anteriores al cambio conservan su comportamiento (sin backfill).
