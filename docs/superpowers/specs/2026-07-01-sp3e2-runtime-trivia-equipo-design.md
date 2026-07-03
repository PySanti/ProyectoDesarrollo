# SP-3e-2 — Runtime Trivia en modalidad Equipo (+ guard double-accept + inbox convocatorias)

- **Slice:** SP-3e-2 (segundo de slice-E). Base: SP-3e-1 (participación Equipo, commits `8fe04e5..9915a5a`, APPROVED).
- **Servicio:** Operaciones de Sesión (único servicio tocado).
- **Habilita:** SP-3e-3 (BDT Equipo) hereda el patrón de identidad dual; SP-4 (Puntuaciones) recibe eventos con `EquipoId`.
- **Fuera de alcance:** BDT Equipo (3e-3), pistas Equipo (3e-4), scoring/ranking (SP-4), broker RabbitMQ real, cableado clientes móvil/web.

## 1. Objetivo

Hoy el runtime Trivia es Individual-only: `SesionPartida.ResponderPregunta` exige inscripción individual (`_inscripciones.Any(i => i.ParticipanteId == emisor && i.EsActiva)`), así que un miembro de equipo recibe 403 `ParticipanteNoInscrito`. Este slice habilita responder Trivia en modalidad Equipo con la regla del SRS — **la respuesta válida del equipo es la primera opción enviada por cualquier miembro activo** — y cierra dos follow-ups del review final de SP-3e-1 que se vuelven reales con el runtime:

1. **Guard double-accept intra-partida (BR-G09):** sin él, un usuario convocado por dos equipos de la misma partida puede aceptar ambas y quedar activo en dos equipos.
2. **Inbox de convocatorias pendientes:** sin él, un convocado offline no tiene forma HTTP de descubrir su `convocatoriaId` — el flujo aceptar/rechazar es inusable end-to-end.

## 2. Reglas de dominio

- **Una respuesta por equipo por pregunta.** La primera respuesta de CUALQUIER miembro activo (convocatoria Aceptada) sella al equipo entero, sea correcta o no. Los demás miembros reciben `RespuestaDuplicada` (409). Decisión de negocio confirmada: lectura literal del SRS, simétrica con Individual (un intento por participante).
- **Miembro activo = convocatoria Aceptada** en la inscripción activa del equipo (fundación SP-3e-1). Pendiente o Rechazada → 403 `ParticipanteNoInscrito`.
- **Cierre global sin cambios:** la primera respuesta correcta (de cualquier equipo o participante) cierra la pregunta para todos; RF-22 auto-activa la siguiente. Timeout sin cambios.
- **Ganador en Equipo = el equipo.** Se registra `GanadorEquipoId`; `GanadorParticipanteId` conserva el autor material (quién envió la opción ganadora).
- **BR-G09 intra-partida:** al aceptar una convocatoria, si el usuario ya tiene OTRA convocatoria Aceptada en la misma sesión → rechazo 409. (En partida Equipo no existen inscripciones individuales — `Inscribir` lanza `ModalidadNoSoportada` — así que el único caso intra-partida es 2 convocatorias.) El check cross-partida del repo (SP-3e-1) queda intacto.
- **Convocatoria accionable** = `Pendiente` y su sesión en `Lobby` (aceptar/rechazar exige Lobby). El inbox lista solo accionables — sin historial (YAGNI, decisión confirmada).

## 3. Diseño — enfoque "identidad dual" (aprobado, alternativa B flujo-paralelo descartada)

Autor real y equipo viajan juntos: `ParticipanteId` = quién envió (auditoría, SP-4), `EquipoId?` = a quién cuenta. `EquipoId == null` ⇔ modalidad Individual. Un solo flujo de runtime; Individual no cambia de comportamiento (campo nuevo siempre null).

### 3.1 Dominio

**`SesionPartida.ResponderPregunta(participanteId, opcionId, now)`** — resolución de participación:
- Sesión Individual: guard actual sin cambios.
- Sesión Equipo: busca la inscripción activa con convocatoria Aceptada de `participanteId` → `equipoId = inscripcion.EquipoId`. Sin match → `ParticipanteNoInscritoException` (403, actual).
- Pasa `equipoId` (Guid?) a `RegistrarRespuesta`.

**`PreguntaSnapshot.RegistrarRespuesta(participanteId, equipoId?, opcionId, now)`:**
- Dedup: `equipoId != null` → `RespuestaDuplicadaException` si ya existe respuesta con ese `EquipoId`; si no, dedup por `ParticipanteId` (actual).
- `RespuestaTrivia` += `EquipoId?` (Guid?, null en Individual).
- Acierto: `Cerrar(RespuestaCorrecta, now, ganador: autor)` + nuevo `GanadorEquipoId?` en la pregunta.
- `ResultadoRespuesta` += `EquipoId?` para que el handler lo propague a eventos.

**`SesionPartida.ResponderConvocatoria` (guard double-accept), rama aceptar:**
- Si `_inscripciones.Any(i => i.EsActiva && i.Id != inscripcionActual.Id && i.Convocatorias.Any(c => c.UsuarioId == usuarioId && c.EstaAceptada))` → `ParticipacionActivaException` (409, excepción existente; ya mapeada en middleware).

### 3.2 Aplicación

**`ResponderPreguntaCommandHandler`:** sin cambios de flujo; propaga `r.EquipoId` a los eventos.

**Eventos (seam `ISesionEventsPublisher`, 16 métodos — firmas extendidas, sin métodos nuevos):**
- `RespuestaTriviaValidadaEvent`, `PuntajeTriviaIncrementadoEvent`, `PreguntaCerradaEvent` += `EquipoId?` (Guid?; solo la rama RespuestaCorrecta lo llena — cierre por tiempo/avance no tiene ganador).
- Payloads SignalR correspondientes += `equipoId?`. Sin cambios de grupos ni eventos nuevos. Las 5 impls (SignalR/NoOp/Composite/Fake/NoOpBase) se actualizan mecánicamente; el test doc↔constantes sigue pasando.

**Inbox:** `ObtenerMisConvocatoriasPendientesQuery(UsuarioId)` + handler (Application/Queries + Handlers/Queries) → `IReadOnlyList<ConvocatoriaPendienteDto>`; DTO en Application/DTOs.

### 3.3 HTTP

- `GET /operaciones-sesion/mis-convocatorias` (Participante; `usuarioId` del JWT `sub`, nunca del body) → `200 + [{ convocatoriaId, partidaId, equipoId, fechaEnvio }]`; lista vacía si no hay accionables (siempre 200, nunca 404). Controller: dispatch MediatR puro + unit tests, patrón `SesionesController` actual.
- Endpoint de respuesta Trivia existente (`POST .../pregunta-actual/respuesta`) no cambia de forma: mismo request `{ opcionId }`, mismos códigos; en Equipo el 403 significa "sin convocatoria aceptada" y el 409 duplicada aplica por equipo.

### 3.4 Persistencia (EF, migración additiva `SP3e2RuntimeTriviaEquipo`)

- `respuestas.equipoid` (uuid, nullable) y `preguntas.ganadorequipoid` (uuid, nullable). Filas existentes intactas (null).
- Repo: `GetConvocatoriasPendientesByUsuarioAsync(usuarioId)` — sesiones en `Lobby` con `Include(Inscripciones).ThenInclude(Convocatorias)` filtrando convocatoria `Pendiente` del usuario. Interface en Domain, impl en Infrastructure, fake en UnitTests con paridad de predicado.

### 3.5 Proyecciones

- `ObtenerMiSesionQueryHandler`: en Equipo, `yaRespondioPreguntaActual` = alguna respuesta de la pregunta activa con `EquipoId == inscripcion.EquipoId` (mi equipo ya respondió). Individual sin cambios.
- Contrato: nota de semántica por equipo en `MiSesionDto.yaRespondioPreguntaActual` + fila y DTO del inbox en `contracts/http/operaciones-sesion-api.md`.

## 4. Testing

- **Dominio:** convocado aceptado responde correcta (cierra, ganador equipo + autor) / incorrecta (no cierra, equipo sellado); 2º miembro del mismo equipo → duplicada; miembro Pendiente/Rechazado → `ParticipanteNoInscrito`; multi-equipo: correcta de equipo A cierra para equipo B; guard double-accept (2ª aceptación misma sesión → `ParticipacionActiva`); Individual regression (equipoId null en todo el flujo).
- **Aplicación/handler:** eventos portan `EquipoId` en Equipo y null en Individual; inbox query (pendiente+Lobby sí / Iniciada no / Aceptada-Rechazada no / de otro usuario no).
- **Integration (InMemory):** repo scan del inbox con contextos write/read separados (patrón B11-fix); persistencia round-trip de `equipoid`/`ganadorequipoid`.
- **Controller unit:** dispatch + 200 lista/vacía del inbox.
- **Regresión:** suites completas de Operaciones (Unit/Integration/Contract) verdes; cambios en tests existentes solo por arity de records extendidos, nunca de comportamiento.

## 5. Riesgos y mitigaciones

- **Ventana EF:** los campos nuevos (`EquipoId` en `RespuestaTrivia`, `GanadorEquipoId` en `PreguntaSnapshot`) son tipos primitivos nullable ya soportados por el mapping actual — no se repite la ventana B3→B10 de SP-3e-1 (que la causó un VO sin converter). Aun así, la tarea de dominio y la de migración deben quedar adyacentes en el plan.
- **Arity ripple:** extender `ResultadoRespuesta` y los event records rompe construction sites en tests — el plan debe listar la búsqueda repo-wide de sitios como paso explícito (lección B13).
- **Concurrencia:** respuesta de equipo escribe en el aggregate root vía `xmin` existente (SP-3f-1) — dos miembros simultáneos: uno gana, el otro recibe 409 de concurrencia o duplicada; ambos aceptables.

## 6. Follow-ups diferidos (no bloqueantes)

- BDT Equipo (SP-3e-3, mismo patrón identidad dual sobre `ValidarTesoro`/`TesoroQR`), pistas Equipo (SP-3e-4).
- Minors heredados de SP-3e-1: `lobby.participantes` con `Guid.Empty` en Equipo; xmin en child-only writes de convocatorias; índice `inscripciones.equipoid`; cancel con líder que salió del equipo.
- Replay SignalR de `ConvocatoriaCreada` (el inbox HTTP cubre el caso offline; replay queda opcional).
