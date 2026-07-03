# Operaciones de Sesion HTTP Contract

## Status

SP-3a endpoints registered. Remaining capabilities require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- Publishing a partida to lobby.
- Manual and automatic partida start.
- Runtime session queries for lobby, active question, active stage and session state.
- Partida-level inscriptions and convocatorias.
- Trivia answer submission and validation.
- BDT QR treasure upload and validation.
- Sequential game and stage advance.
- BDT clues, geolocation and reconnection support.
- User-visible session real-time communication through the gateway.

## Endpoint Registry

| Capability | Method | Gateway path | Auth (coarse) | Success | Errors |
|---|---|---|---|---|---|
| Publish a partida to lobby | POST | `/operaciones-sesion/partidas/{partidaId}/publicacion` | Operador | 201 + LobbyDto (Location → lobby) | 404 config no existe · 502 Partidas inaccesible · 409 ya publicada / no publicable |
| Inscribe (Individual) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones` | Participante | 201 + InscripcionResponse | 401 sin identidad · 404 sesión no existe · 409 ya inscrito / participación activa / cupo lleno / modalidad no soportada |
| Cancel own inscription | DELETE | `/operaciones-sesion/partidas/{partidaId}/inscripciones/mia` | Participante | 204 | 401 · 404 sesión / inscripción no existe |
| Preinscribir equipo (líder) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones-equipo` | Participante (líder) | 201 + PreinscripcionEquipoResponse | 404 sesión no existe · 403 no es líder · 409 equipo ya inscrito / participación activa en otra / cupo lleno / sin equipo activo · 502 Identity inaccesible |
| Cancelar preinscripción de equipo (líder) | DELETE | `/operaciones-sesion/partidas/{partidaId}/inscripciones-equipo/mia` | Participante (líder) | 204 | 404 sesión/inscripción no existe · 403 no es líder · 409 no en lobby / sin equipo activo · 502 Identity inaccesible |
| Aceptar convocatoria | POST | `/operaciones-sesion/convocatorias/{convocatoriaId}/aceptacion` | Participante (convocado) | 200 + ConvocatoriaResponse | 404 convocatoria no encontrada · 409 no en lobby / participación activa en otra |
| Rechazar convocatoria | POST | `/operaciones-sesion/convocatorias/{convocatoriaId}/rechazo` | Participante (convocado) | 200 + ConvocatoriaResponse | 404 convocatoria no encontrada · 409 no en lobby |
| Lobby state | GET | `/operaciones-sesion/partidas/{partidaId}/lobby` | Operador/Participante | 200 + LobbyDto | 404 sesión no existe |
| Start a partida (manual) | POST | `/operaciones-sesion/partidas/{partidaId}/inicio` | Operador | 200 + InicioPartidaResponse | 404 sesión no existe · 409 no en Lobby / modo incompatible |
| Start a partida (automatic, idempotent) | POST | `/operaciones-sesion/partidas/{partidaId}/inicio-automatico` | Operador/Sistema | 200 + InicioPartidaResponse | 404 sesión no existe · 409 modo incompatible |
| Finalize current game (advance) | POST | `/operaciones-sesion/partidas/{partidaId}/juego-actual/finalizacion` | Operador | 200 + AvanceJuegoResponse | 404 sesión no existe · 409 no iniciada |
| Session state | GET | `/operaciones-sesion/partidas/{partidaId}/estado` | Operador/Participante | 200 + EstadoSesionDto | 404 sesión no existe |
| Answer active question | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/respuesta` | Participante | 200 + RespuestaTriviaResponse | 401 sin identidad · 403 no inscrito / sin convocatoria aceptada (Equipo) · 404 sesión no existe · 409 no iniciada / juego no Trivia / sin pregunta activa / duplicada (individual o, en Equipo, por equipo) / fuera de tiempo |
| Advance current question | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/avance` | Operador | 200 + AvancePreguntaResponse | 404 · 409 no iniciada / juego no Trivia / sin pregunta activa |
| Current question | GET | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual` | Operador/Participante | 200 + PreguntaActualDto | 404 sesión no existe · 409 sin pregunta activa |
| Validar tesoro | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/tesoro` | Participante | 200 + ValidacionTesoroResponse | 401 sin identidad · 403 no inscrito / sin convocatoria aceptada (Equipo) · 404 sesión no existe · 409 no iniciada / juego no BDT / sin etapa activa |
| Avanzar/cerrar etapa | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/avance` | Operador | 200 + AvanceEtapaResponse | 404 · 409 no iniciada / juego no BDT / sin etapa activa |
| Etapa actual | GET | `/operaciones-sesion/partidas/{partidaId}/etapa-actual` | Operador/Participante | 200 + EtapaActualDto | 404 sesión no existe · 409 sin etapa activa |
| Enviar pista (BDT) | POST | `/operaciones-sesion/partidas/{partidaId}/pistas` | Operador | 200 + PistaEnviadaResponse | 400 no se indicó exactamente un destino · 404 sesión no existe / equipo destino sin inscripción activa · 403 destino participante no inscrito · 409 no iniciada / juego no BDT / sin etapa activa / destino equipo en partida Individual |
| Mi sesión (reconexión) | GET | `/operaciones-sesion/mi-sesion` | Participante | 200 + MiSesionDto · 204 sin participación activa | 401 sin identidad |
| Mis convocatorias pendientes | GET | `/operaciones-sesion/mis-convocatorias` | Participante | 200 + ConvocatoriaPendienteDto[] (vacía si no hay) | 401 sin identidad |

### DTOs

- `LobbyDto { partidaId, sesionPartidaId, estado, modalidad, minimosParticipacion, maximosParticipacion, inscritosActivos, participantes[], equipos[] }`
- `InscripcionResponse { inscripcionId, partidaId, participanteId }`
- `InicioPartidaResponse { partidaId, estado, juegoActivadoId?, juegoActivadoOrden? }` (estado ∈ {Iniciada, Cancelada, Lobby}; Lobby = automatic no-op)
- `AvanceJuegoResponse { partidaId, estado, juegoFinalizadoOrden?, juegoActivadoOrden?, terminada }`
- `EstadoSesionDto { partidaId, sesionPartidaId, estado, modalidad, juegos[]{ juegoId, orden, tipoJuego, estado }, juegoActualOrden? }`
- `RespuestaTriviaResponse { partidaId, preguntaId, esCorrecta, cerroPregunta, puntaje? }`
- `AvancePreguntaResponse { partidaId, preguntaCerradaOrden, preguntaActivadaOrden?, sinMasPreguntas }`
- `PreguntaActualDto { partidaId, juegoId, preguntaId, orden, texto, tiempoLimiteSegundos, fechaActivacion, opciones[]{ opcionId, texto } }` (participant-safe; nunca `esCorrecta`)
- `ValidacionTesoroResponse { partidaId, etapaId, resultado, gano, cerroEtapa, puntaje? }` (resultado ∈ {Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva})
- `AvanceEtapaResponse { partidaId, etapaCerradaOrden, etapaActivadaOrden?, sinMasEtapas }`
- `EtapaActualDto { partidaId, juegoId, etapaId, orden, areaBusqueda, tiempoLimiteSegundos, fechaActivacion }` (participant-safe; nunca `codigoQREsperado`)
- `PistaEnviadaResponse { partidaId, juegoId, participanteDestinoId?, timestampUtc, equipoDestinoId? }` (request body `{ participanteDestinoId?, texto, equipoDestinoId? }` — exactamente uno de los dos destinos, si no 400; efecto: push `PistaEnviada` al participante destino o, si el destino es un equipo (modalidad Equipo, SP-3e-4), a todos sus miembros conectados vía el grupo `equipo:{equipoDestinoId}`)
- `PreinscripcionEquipoResponse { inscripcionId, equipoId, convocados }` (líder preinscribe su equipo; el equipo y miembros se toman por snapshot de `GET /api/teams/mine` en Identity; genera una convocatoria por integrante)
- `ConvocatoriaResponse { convocatoriaId, estado }` (`estado` ∈ `Pendiente|Aceptada|Rechazada`)
- `LobbyDto.equipos: [{ equipoId, convocados, aceptados }]` (solo modalidad Equipo)
- `MiSesionDto.convocatoria: { convocatoriaId, equipoId, estado } | null` (estado de la convocatoria del caller en modalidad Equipo)
- `MiSesionDto { partidaId, sesionPartidaId, estadoPartida, modalidad, inscripcion{ inscripcionId, estado }, juegoActivo?{ juegoId, orden, tipoJuego, estadoJuego }, preguntaActual?, etapaActual?, yaRespondioPreguntaActual?, convocatoria? }` (participant-safe; reusa PreguntaActualDto/EtapaActualDto; nunca `codigoQREsperado` ni la opción correcta)
- `ConvocatoriaPendienteDto { convocatoriaId, partidaId, equipoId, fechaEnvio }` (solo convocatorias Pendientes accionables: partida en Lobby, inscripción del equipo activa; orden por fechaEnvio)

Notes: enums serialized as strings. `participanteId` is taken from the JWT `sub` claim (never the body). Config handoff is an internal `GET /partidas/{id}` (not via the gateway), forwarding the caller's bearer. Start/advance return 200 (state transition, not resource creation). Minimums not met on start is a valid `200 + estado=Cancelada` outcome (not a 4xx). `/inicio-automatico` is idempotent: not in Lobby or before `TiempoInicio` → no-op `200` with the current estado. Request body for `/pregunta-actual/respuesta` is `{ opcionId }`; `participanteId` taken from the JWT `sub` claim. Request body for `/etapa-actual/tesoro` is `{ imagenBase64 }`; `participanteId` taken from the JWT `sub` claim. The backend decodes the image server-side (RF-29). `GET /mi-sesion` direcciona por participante (JWT `sub`, sin `partidaId`): devuelve la única participación activa vigente (partida en Lobby/Iniciada) o `204` si no hay. `estadoPartida` en el cuerpo solo toma Lobby/Iniciada. `yaRespondioPreguntaActual` es true/false solo con pregunta Trivia activa, null en BDT/lobby. Read-only; no emite eventos. Concurrencia (SP-3f-1): `SesionPartida` usa token optimista (`xmin`). Los endpoints de runtime/inicio (responder pregunta, validar tesoro, avanzar pregunta/etapa, iniciar) pueden devolver `409 Conflict` cuando un barrido de fondo modifica la misma sesión en el instante de la petición; el cliente refetchea (`GET /mi-sesion`) y reintenta. Dos barridos de fondo (sin endpoint, dentro de Operaciones de Sesión) avanzan el estado por tiempo: inicio automático al cumplirse `TiempoInicio` (Lobby + Automatico/ManualYAutomatico) y cierre por timeout de la pregunta/etapa vencida del juego activo. Read/write internos; emiten los mismos eventos de dominio que el path request (No-Op por ahora). Modalidad Equipo (SP-3e-2): en `POST .../pregunta-actual/respuesta` responde cualquier miembro con convocatoria aceptada; la PRIMERA respuesta del equipo (correcta o no) lo sella — los demás miembros reciben 409 duplicada. `MiSesionDto.yaRespondioPreguntaActual` en Equipo significa "mi equipo ya respondió". Aceptar una convocatoria teniendo otra aceptada en la misma partida devuelve 409. Los eventos internos `RespuestaTriviaValidada`/`PuntajeTriviaIncrementado`/`PreguntaTriviaCerrada` portan `equipoId`/`ganadorEquipoId` (null en Individual); los payloads SignalR difundidos no cambian. Modalidad Equipo (SP-3e-3): en `POST .../etapa-actual/tesoro` valida cualquier miembro con convocatoria aceptada (403 `ParticipanteNoInscritoException` si no la hay) — a diferencia de Trivia, **reintentos ilimitados**: un QR incorrecto solo registra el intento (`TesoroQR` con autor + equipo), no sella nada, sin 409 de duplicado. La primera validación correcta dentro de la ventana gana la etapa para todo el equipo (`GanadorEquipoId`). Los eventos internos `TesoroQRValidado`/`EtapaBDTGanada`/`EtapaBDTCerrada` portan `equipoId`/`ganadorEquipoId` (null en Individual); los payloads SignalR difundidos no cambian.

## Realtime / SignalR (SP-3f-2)

Hub: `GET /operaciones-sesion/hubs/sesion` (WebSocket vía gateway YARP; passthrough automático). Auth: JWT obligatorio; en el handshake WebSocket el token viaja por query `access_token` (lo leen gateway y servicio). Grupo por partida.

Cliente → servidor:
- `SuscribirAPartida(partidaId)` — el llamante debe ser `Operador` o estar inscrito en la partida; une la conexión al grupo `partida:{id}`. Rechaza con error de hub en caso contrario.
- `DesuscribirDePartida(partidaId)` — saca la conexión del grupo.
- `EnviarUbicacion(latitud, longitud)` — **solo participante**, requiere `SuscribirAPartida` previo (la partida se toma de la conexión, no como parámetro). Valida rango (`latitud ∈ [-90,90]`, `longitud ∈ [-180,180]`); fuera de rango o sin suscripción → error de hub. Relay puro: no persiste. (SP-3f-3)
- `SuscribirAPartida` (rama participante) además une la conexión al grupo por-participante `participante:{id}` (para recibir `PistaEnviada`); `DesuscribirDePartida` la retira. La rama operador no se une a ningún grupo participante. (SP-3f-4)
- `SuscribirAPartida` (rama participante), en sesiones de modalidad Equipo, además une la conexión al grupo por-equipo `equipo:{equipoId}` cuando el caller es un convocado con convocatoria **Aceptada** en su inscripción activa (resuelto server-side por el `sub` del JWT contra la sesión ya cargada, sin parámetro adicional); `DesuscribirDePartida` la retira. En modalidad Individual no hay convocatorias, por lo que esta unión no aplica. (SP-3e-4)

Servidor → cliente (payloads delgados; el contenido se trae por pull `GET /pregunta-actual` / `GET /etapa-actual` / `GET /mi-sesion`):

| Mensaje | Payload |
|---|---|
| `PartidaEnLobby` | `{ partidaId }` |
| `PartidaIniciada` | `{ partidaId }` |
| `JuegoActivado` | `{ partidaId, juegoId, orden, tipoJuego }` |
| `PartidaCancelada` | `{ partidaId, motivo }` |
| `PartidaFinalizada` | `{ partidaId }` |
| `PreguntaActivada` | `{ partidaId, juegoId, preguntaId, orden, fechaLimiteUtc }` |
| `PreguntaCerrada` | `{ partidaId, juegoId, preguntaId }` |
| `EtapaActivada` | `{ partidaId, juegoId, etapaId, orden, fechaLimiteUtc }` |
| `EtapaCerrada` | `{ partidaId, juegoId, etapaId }` |
| `EtapaGanada` | `{ partidaId, juegoId, etapaId }` |
| `UbicacionActualizada` *(operador-only)* | `{ partidaId, participanteId, latitud, longitud, timestampUtc }` |
| `PistaEnviada` *(destino only: participante o equipo)* | `{ partidaId, juegoId, participanteDestinoId?, texto, timestampUtc, equipoDestinoId? }` |
| `ConvocatoriaCreada` *(convocado-destino only)* | `{ partidaId, equipoId, convocatoriaId, usuarioId }` |

Notas: `fechaLimiteUtc` = activación + tiempo límite (cuenta regresiva local en el cliente). Los payloads nunca llevan puntos acumulados ni ranking (eso es Puntuaciones/SP-4) ni texto de preguntas/opciones/QR (anti-leak). Eventos per-participante/scoring-adjacentes (`RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `TesoroQRValidado`) NO se difunden en este slice; su efecto sale por las transiciones de estado. El push se dispara in-process desde Operaciones (no requiere el broker RabbitMQ). La ruta gateway `/operaciones-sesion/hubs/sesion` es alcanzable end-to-end (SP-3g): el servicio mapea el hub y todos sus endpoints bajo el prefijo `operaciones-sesion`, y YARP reenvía el path completo sin `PathRemovePrefix` — consistente con el resto de los servicios. `UbicacionActualizada` (SP-3f-3) se difunde SOLO al grupo `operador:partida:{id}` (BR-B07: únicamente el operador ve el mapa; el participante emisor no lo recibe); `timestampUtc` es server-stamped; el relay no persiste ni emite evento de dominio (audit de ubicación → broker, diferido). `PistaEnviada` (SP-3f-4; destino equipo — SP-3e-4) se difunde al grupo `participante:{destinoId}` **o**, cuando el request trae `equipoDestinoId` (destino equipo, solo modalidad Equipo), al grupo `equipo:{equipoDestinoId}` — nunca a ambos a la vez (BR-B06: la pista es para un destino específico, un participante puntual o un equipo entero; el resto de participantes/equipos y el operador no la reciben). El payload gana `equipoDestinoId?` y `participanteDestinoId` pasa a nullable — exactamente uno de los dos viene poblado, según el destino del request original. Es event-only: el `texto` viaja en el payload (no hay pull) — única excepción al anti-leak, justamente porque es el contenido dirigido a ese destino. `timestampUtc` es server-stamped; no se persiste la entidad `Pista` ni se emite audit en este slice (el registro BR-B06 lo materializa audit vía broker, diferido). Sin replay: si el destino está offline (o, para destino equipo, ningún miembro está conectado), la pista se pierde (transitorio). `ConvocatoriaCreada` (SP-3e-1) se difunde SOLO al grupo `participante:{usuarioId}` del convocado (para que el móvil muestre la convocatoria entrante); no llega al grupo de partida ni al operador. `ConvocatoriaRespondida` no tiene push en tiempo real en este slice (alimenta audit/scoring vía broker RabbitMQ, diferido). Snapshot de membresía: los miembros a convocar se congelan al preinscribir (altas/bajas del equipo posteriores no afectan esa partida).
