# Operaciones de Sesion HTTP Contract

## Status

Endpoints SP-3a..SP-3e-4 registered (21). Trivia and BDT runtime operational in `Individual` and `Equipo` modality; clue delivery, geolocation relay and realtime push via SignalR. RabbitMQ broker delivery and clue persistence remain deferred (see SDD specs). Functional-permission authorization enforced per endpoint since SP-5a (see "Autorización (SP-5a)" below).

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

| Capability | Method | Gateway path | Auth (SP-5a) | Success | Errors |
|---|---|---|---|---|---|
| Publish a partida to lobby | POST | `/operaciones-sesion/partidas/{partidaId}/publicacion` | Policy `GestionarPartidas` | 201 + LobbyDto (Location → lobby) | 401 sin token · 403 sin el permiso · 404 config no existe · 502 Partidas inaccesible · 409 ya publicada / no publicable |
| Inscribe (Individual) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones` | Policy `ParticiparEnPartidas` | 201 + InscripcionResponse | 401 sin identidad · 403 sin el permiso · 404 sesión no existe · 409 ya inscrito / participación activa / cupo lleno / modalidad no soportada |
| Cancel own inscription | DELETE | `/operaciones-sesion/partidas/{partidaId}/inscripciones/mia` | Policy `ParticiparEnPartidas` | 204 | 401 · 403 sin el permiso · 404 sesión / inscripción no existe |
| Preinscribir equipo (líder) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones-equipo` | Policy `ParticiparEnPartidas` (líder por regla de dominio, no por policy) | 201 + PreinscripcionEquipoResponse | 401 sin identidad · 403 sin el permiso / no es líder · 404 sesión no existe · 409 equipo ya inscrito / participación activa en otra / cupo lleno / sin equipo activo · 502 Identity inaccesible |
| Cancelar preinscripción de equipo (líder) | DELETE | `/operaciones-sesion/partidas/{partidaId}/inscripciones-equipo/mia` | Policy `ParticiparEnPartidas` (líder por regla de dominio, no por policy) | 204 | 401 sin identidad · 403 sin el permiso / no es líder · 404 sesión/inscripción no existe · 409 no en lobby / sin equipo activo · 502 Identity inaccesible |
| Aceptar convocatoria | POST | `/operaciones-sesion/convocatorias/{convocatoriaId}/aceptacion` | Policy `ParticiparEnPartidas` (convocado por regla de dominio, no por policy) | 200 + ConvocatoriaResponse | 401 sin identidad · 403 sin el permiso · 404 convocatoria no encontrada · 409 no en lobby / participación activa en otra |
| Rechazar convocatoria | POST | `/operaciones-sesion/convocatorias/{convocatoriaId}/rechazo` | Policy `ParticiparEnPartidas` (convocado por regla de dominio, no por policy) | 200 + ConvocatoriaResponse | 401 sin identidad · 403 sin el permiso · 404 convocatoria no encontrada · 409 no en lobby |
| Aceptar inscripción (operador, HU-19) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones/{inscripcionId}/aceptacion` | Policy `GestionarPartidas` | 200 + LobbyDto | 401 sin token · 403 sin el permiso · 404 sesión / inscripción no existe · 409 cupo lleno / inscripción no pendiente / sesión no en lobby |
| Rechazar inscripción (operador, HU-19) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones/{inscripcionId}/rechazo` | Policy `GestionarPartidas` | 200 + LobbyDto | 401 sin token · 403 sin el permiso · 404 sesión / inscripción no existe · 409 inscripción no pendiente / sesión no en lobby |
| Lobby state | GET | `/operaciones-sesion/partidas/{partidaId}/lobby` | Autenticado (cualquier rol; sin policy de permiso) | 200 + LobbyDto | 401 sin token · 404 sesión no existe |
| Start a partida (manual) | POST | `/operaciones-sesion/partidas/{partidaId}/inicio` | Policy `GestionarPartidas` | 200 + InicioPartidaResponse | 401 sin token · 403 sin el permiso · 404 sesión no existe · 409 no en Lobby / modo incompatible |
| Start a partida (automatic, idempotent) | POST | `/operaciones-sesion/partidas/{partidaId}/inicio-automatico` | Policy `GestionarPartidas` (llamado también por el worker interno vía `ISender` in-process, sin HTTP) | 200 + InicioPartidaResponse | 401 sin token · 403 sin el permiso · 404 sesión no existe · 409 modo incompatible |
| Finalize current game (advance) | POST | `/operaciones-sesion/partidas/{partidaId}/juego-actual/finalizacion` | Policy `GestionarPartidas` | 200 + AvanceJuegoResponse | 401 sin token · 403 sin el permiso · 404 sesión no existe · 409 no iniciada |
| Cancelar partida (operador, HU-40) | POST | `/operaciones-sesion/partidas/{partidaId}/cancelacion` | Policy `GestionarPartidas` | 200 + CancelacionPartidaResponse | 401 sin token · 403 sin el permiso · 404 sesión no existe · 409 estado terminal (`Cancelada`/`Terminada`) |
| Session state | GET | `/operaciones-sesion/partidas/{partidaId}/estado` | Autenticado (cualquier rol; sin policy de permiso) | 200 + EstadoSesionDto | 401 sin token · 404 sesión no existe |
| Partidas publicadas (descubrimiento) | GET | `/operaciones-sesion/partidas-publicadas` | Autenticado (cualquier rol; sin policy de permiso) | 200 + PartidaPublicadaDto[] (solo sesiones en `Lobby`; vacía si no hay) | 401 sin token |
| Answer active question | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/respuesta` | Policy `ParticiparEnPartidas` | 200 + RespuestaTriviaResponse | 401 sin identidad · 403 sin el permiso / no inscrito / sin convocatoria aceptada (Equipo) · 404 sesión no existe · 409 no iniciada / juego no Trivia / sin pregunta activa / duplicada (individual o, en Equipo, por equipo) / fuera de tiempo |
| Advance current question | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/avance` | Policy `GestionarPartidas` | 200 + AvancePreguntaResponse | 401 sin token · 403 sin el permiso · 404 · 409 no iniciada / juego no Trivia / sin pregunta activa |
| Current question | GET | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual` | Autenticado (cualquier rol; sin policy de permiso) | 200 + PreguntaActualDto | 401 sin token · 404 sesión no existe · 409 sin pregunta activa |
| Validar tesoro | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/tesoro` | Policy `ParticiparEnPartidas` | 200 + ValidacionTesoroResponse | 401 sin identidad · 403 sin el permiso / no inscrito / sin convocatoria aceptada (Equipo) · 404 sesión no existe · 409 no iniciada / juego no BDT / sin etapa activa |
| Avanzar/cerrar etapa | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/avance` | Policy `GestionarPartidas` | 200 + AvanceEtapaResponse | 401 sin token · 403 sin el permiso · 404 · 409 no iniciada / juego no BDT / sin etapa activa |
| Etapa actual | GET | `/operaciones-sesion/partidas/{partidaId}/etapa-actual` | Autenticado (cualquier rol; sin policy de permiso) | 200 + EtapaActualDto | 401 sin token · 404 sesión no existe · 409 sin etapa activa |
| Envíos de tesoro del juego actual (HU-38, monitoreo operador) | GET | `/operaciones-sesion/partidas/{partidaId}/juego-actual/envios-tesoro` | Policy `OperadorOAdministrador` (rol base, no `GestionarPartidas`: el Administrador observador también lo ve) | 200 + EnviosTesoroDto | 401 sin token · 403 sin rol Operador/Administrador · 404 sesión no existe · 409 juego activo no es BDT / sin juego activo |
| Enviar pista (BDT) | POST | `/operaciones-sesion/partidas/{partidaId}/pistas` | Policy `GestionarPartidas` | 200 + PistaEnviadaResponse | 401 sin token · 403 sin el permiso / destino participante no inscrito · 400 no se indicó exactamente un destino · 404 sesión no existe / equipo destino sin inscripción activa · 409 no iniciada / juego no BDT / sin etapa activa / destino equipo en partida Individual |
| Mi sesión (reconexión) | GET | `/operaciones-sesion/mi-sesion` | Policy `ParticiparEnPartidas` | 200 + MiSesionDto · 204 sin participación activa | 401 sin identidad · 403 sin el permiso |
| Mis convocatorias pendientes | GET | `/operaciones-sesion/mis-convocatorias` | Policy `ParticiparEnPartidas` | 200 + ConvocatoriaPendienteDto[] (vacía si no hay) | 401 sin identidad · 403 sin el permiso |

### DTOs

- `LobbyDto { partidaId, sesionPartidaId, estado, modalidad, minimosParticipacion, maximosParticipacion, inscritosActivos, participantes[], equipos[], solicitudesPendientesIndividual[], solicitudesPendientesEquipo[] }` (HU-19: `inscritosActivos`/`participantes`/`equipos` solo cuentan inscripciones **Activas**; las pendientes de aprobación viajan en las dos listas nuevas)
- `LobbyDto.solicitudesPendientesIndividual: [{ inscripcionId, participanteId, fechaInscripcion }]` (solo modalidad Individual)
- `LobbyDto.solicitudesPendientesEquipo: [{ inscripcionId, equipoId, miembros, fechaInscripcion }]` (solo modalidad Equipo)
- `PartidaPublicadaDto { partidaId, nombre, modalidad, modoInicioPartida, tiempoInicio (nullable), minimosParticipacion, maximosParticipacion, inscritosActivos }` — listado participant-safe para el panel mobile (Bloque 2d): solo sesiones cuyo estado es `Lobby`; sin juegos, preguntas ni códigos QR. `inscritosActivos` cuenta inscripciones activas (participantes en Individual, equipos en Equipo).
- `InscripcionResponse { inscripcionId, partidaId, participanteId }` (HU-19: inscribir/preinscribir devuelve la inscripción en estado `Pendiente`; requiere aprobación del operador para contar en mínimos/cupo/inicio)
- `InicioPartidaResponse { partidaId, estado, juegoActivadoId?, juegoActivadoOrden? }` (estado ∈ {Iniciada, Cancelada, Lobby}; Lobby = automatic no-op)
- `AvanceJuegoResponse { partidaId, estado, juegoFinalizadoOrden?, juegoActivadoOrden?, terminada }`
- `CancelacionPartidaResponse { partidaId, estado }` (HU-40: `estado` siempre `Cancelada`; válido desde `Lobby` o `Iniciada`, 409 desde estado terminal). Cancelar desde `Iniciada` cierra la pregunta/etapa activa del juego activo y lo pasa a `Finalizado` (sin activar el siguiente ni publicar eventos de juego/pregunta/etapa — solo `PartidaCancelada`): tras cancelar, `GET /pregunta-actual` y `GET /etapa-actual` dejan de exponer contenido "vivo" y devuelven `409` (`NoHayPreguntaActivaException`/`NoHayEtapaActivaException`).
- `EstadoSesionDto { partidaId, sesionPartidaId, estado, modalidad, juegos[]{ juegoId, orden, tipoJuego, estado }, juegoActualOrden? }`
- `RespuestaTriviaResponse { partidaId, preguntaId, esCorrecta, cerroPregunta, puntaje? }`
- `AvancePreguntaResponse { partidaId, preguntaCerradaOrden, preguntaActivadaOrden?, sinMasPreguntas }`
- `PreguntaActualDto { partidaId, juegoId, preguntaId, orden, texto, tiempoLimiteSegundos, fechaActivacion, opciones[]{ opcionId, texto } }` (participant-safe; nunca `esCorrecta`)
- `ValidacionTesoroResponse { partidaId, etapaId, resultado, gano, cerroEtapa, puntaje? }` (resultado ∈ {Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva})
- `AvanceEtapaResponse { partidaId, etapaCerradaOrden, etapaActivadaOrden?, sinMasEtapas }`
- `EtapaActualDto { partidaId, juegoId, etapaId, orden, areaBusqueda, tiempoLimiteSegundos, fechaActivacion }` (participant-safe; nunca `codigoQREsperado`)
- `EnviosTesoroDto { partidaId, juegoId, etapas: [{ etapaId, orden, intentos: [{ participanteId, equipoId?, resultado, instante }] }] }` (HU-38, 7d: monitoreo operador de todos los envíos de `TesoroQR` del juego BDT activo, agrupados por etapa — incluye **todas** las etapas del juego, no solo la activa; las que aún no recibieron envíos traen `intentos: []`; `resultado` ∈ {Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva}; nunca expone `codigoQREsperado`. 409 `JuegoActivoNoEsBDTException` si el juego activo es Trivia; 409 `NoHayEtapaActivaException` si no hay juego activo — mismo patrón que `GET /etapa-actual`)
- `PistaEnviadaResponse { partidaId, juegoId, participanteDestinoId?, timestampUtc, equipoDestinoId? }` (request body `{ participanteDestinoId?, texto, equipoDestinoId? }` — exactamente uno de los dos destinos, si no 400; efecto: push `PistaEnviada` al participante destino o, si el destino es un equipo (modalidad Equipo, SP-3e-4), a todos sus miembros conectados vía el grupo `equipo:{equipoDestinoId}`)
- `PreinscripcionEquipoResponse { inscripcionId, equipoId, convocados }` (líder preinscribe su equipo; el equipo y miembros se toman por snapshot de `GET /identity/teams/mine` en Identity — re-homed en SP-5a, antes `/api/teams/mine`; genera una convocatoria por integrante)
- `ConvocatoriaResponse { convocatoriaId, estado }` (`estado` ∈ `Pendiente|Aceptada|Rechazada`)
- `LobbyDto.equipos: [{ equipoId, convocados, aceptados }]` (solo modalidad Equipo)
- `MiSesionDto.convocatoria: { convocatoriaId, equipoId, estado } | null` (estado de la convocatoria del caller en modalidad Equipo)
- `MiSesionDto { partidaId, sesionPartidaId, estadoPartida, modalidad, inscripcion{ inscripcionId, estado }, juegoActivo?{ juegoId, orden, tipoJuego, estadoJuego }, preguntaActual?, etapaActual?, yaRespondioPreguntaActual?, convocatoria? }` (participant-safe; reusa PreguntaActualDto/EtapaActualDto; nunca `codigoQREsperado` ni la opción correcta). `inscripcion.estado` ∈ `Pendiente|Activa` también en modalidad Equipo (7b-bis): se resuelve por la convocatoria del caller sobre la inscripción de su equipo (`OcupaParticipacion` + `Convocatorias.Any(UsuarioId == caller)`), no por `ParticipanteId` (que en Equipo es `Guid.Empty`); `"Equipo"` queda solo como fallback cuando no hay inscripción resoluble.
- `ConvocatoriaPendienteDto { convocatoriaId, partidaId, equipoId, fechaEnvio, nombrePartida }` (solo convocatorias Pendientes accionables: partida en Lobby, inscripción del equipo activa; orden por fechaEnvio). `nombrePartida` es el snapshot `SesionPartida.Nombre`, proyectado en la misma consulta del repositorio: evita que el móvil (Participante) tenga que llegar a Partidas, que el gateway le cierra (`/partidas/{**catch-all}` → `OperadorOAdministrador`).

Notes: enums serialized as strings. `participanteId` is taken from the JWT `sub` claim (never the body). Config handoff is an internal `GET /partidas/{id}` (not via the gateway), forwarding the caller's bearer. Start/advance return 200 (state transition, not resource creation). Minimums not met on start is a valid `200 + estado=Cancelada` outcome (not a 4xx). `/inicio-automatico` is idempotent: not in Lobby or before `TiempoInicio` → no-op `200` with the current estado. Request body for `/pregunta-actual/respuesta` is `{ opcionId }`; `participanteId` taken from the JWT `sub` claim. Request body for `/etapa-actual/tesoro` is `{ imagenBase64 }`; `participanteId` taken from the JWT `sub` claim. The backend decodes the image server-side (RF-29). `GET /mi-sesion` direcciona por participante (JWT `sub`, sin `partidaId`): devuelve la única participación activa vigente (partida en Lobby/Iniciada) o `204` si no hay. `estadoPartida` en el cuerpo solo toma Lobby/Iniciada. `yaRespondioPreguntaActual` es true/false solo con pregunta Trivia activa, null en BDT/lobby. Read-only; no emite eventos. Concurrencia (SP-3f-1): `SesionPartida` usa token optimista (`xmin`). Los endpoints de runtime/inicio (responder pregunta, validar tesoro, avanzar pregunta/etapa, iniciar) pueden devolver `409 Conflict` cuando un barrido de fondo modifica la misma sesión en el instante de la petición; el cliente refetchea (`GET /mi-sesion`) y reintenta. Dos barridos de fondo (sin endpoint, dentro de Operaciones de Sesión) avanzan el estado por tiempo: inicio automático al cumplirse `TiempoInicio` (Lobby + Automatico/ManualYAutomatico) y cierre por timeout de la pregunta/etapa vencida del juego activo. Read/write internos; emiten los mismos eventos de dominio que el path request (No-Op por ahora). Modalidad Equipo (SP-3e-2): en `POST .../pregunta-actual/respuesta` responde cualquier miembro con convocatoria aceptada; la PRIMERA respuesta del equipo (correcta o no) lo sella — los demás miembros reciben 409 duplicada. `MiSesionDto.yaRespondioPreguntaActual` en Equipo significa "mi equipo ya respondió". Aceptar una convocatoria teniendo otra aceptada en la misma partida devuelve 409. Los eventos internos `RespuestaTriviaValidada`/`PuntajeTriviaIncrementado`/`PreguntaTriviaCerrada` portan `equipoId`/`ganadorEquipoId` (null en Individual); los payloads SignalR difundidos no cambian. Modalidad Equipo (SP-3e-3): en `POST .../etapa-actual/tesoro` valida cualquier miembro con convocatoria aceptada (403 `ParticipanteNoInscritoException` si no la hay) — a diferencia de Trivia, **reintentos ilimitados**: un QR incorrecto solo registra el intento (`TesoroQR` con autor + equipo), no sella nada, sin 409 de duplicado. La primera validación correcta dentro de la ventana gana la etapa para todo el equipo (`GanadorEquipoId`). Los eventos internos `TesoroQRValidado`/`EtapaBDTGanada`/`EtapaBDTCerrada` portan `equipoId`/`ganadorEquipoId` (null en Individual); los payloads SignalR difundidos no cambian.

## Autorización (SP-5a)

JWT Keycloak validado con normalizador `KeycloakRoleClaims` (`OnTokenValidated` → roles desde
`realm_access`, mismo patrón que gateway/Identity) — antes de SP-5a el claim `roles` estaba
seteado pero nada lo poblaba. `FallbackPolicy` = autenticado (cualquier rol); el hub SignalR
(`/operaciones-sesion/hubs/sesion`) queda `[Authorize]` sin policy de permiso (lo usan operador
y participante); `/health` es anónimo. `401` = sin token / token inválido; `403` = token válido
sin el permiso requerido.

| Grupo | Policy | Endpoints |
|---|---|---|
| Operación de la partida (10) | `GestionarPartidas` | `publicacion` (POST) · `inicio` (POST) · `inicio-automatico` (POST) · `cancelacion` (POST, HU-40) · `juego-actual/finalizacion` (POST) · `pregunta-actual/avance` (POST) · `etapa-actual/avance` (POST) · `pistas` (POST) · `inscripciones/{id}/aceptacion` (POST, HU-19) · `inscripciones/{id}/rechazo` (POST, HU-19) |
| Monitoreo operador/admin (1) | `OperadorOAdministrador` | `juego-actual/envios-tesoro` (GET, HU-38) |
| Participación (10) | `ParticiparEnPartidas` | `inscripciones` (POST, Individual) · `inscripciones/mia` (DELETE) · `inscripciones-equipo` (POST, líder) · `inscripciones-equipo/mia` (DELETE, líder) · `convocatorias/{id}/aceptacion` (POST, convocado) · `convocatorias/{id}/rechazo` (POST, convocado) · `pregunta-actual/respuesta` (POST) · `etapa-actual/tesoro` (POST) · `mi-sesion` (GET) · `mis-convocatorias` (GET) |
| Lectura compartida (5) | Autenticado, sin policy de permiso | `lobby` (GET) · `estado` (GET) · `pregunta-actual` (GET) · `etapa-actual` (GET) · `partidas-publicadas` (GET) |
| Infraestructura | Anónimo | `health` (GET) |

Notas: los calificadores "líder"/"convocado" en el grupo `ParticiparEnPartidas` son reglas de
**dominio** (403 `NoEsLiderEquipoException`/`ParticipanteNoInscritoException` y afines), no
policies adicionales de ASP.NET — la policy solo exige el permiso funcional, no el rol de negocio
dentro del equipo. `OperadorOAdministrador` es una policy por **rol base de Keycloak** (`Operador`
u `Administrador`), no por permiso funcional — refleja que este endpoint es monitoreo read-only:
el Administrador observador debe verlo aunque no tenga `GestionarPartidas` (mismo patrón que
`OperadorOAdministrador` en Identity `Program.cs`). El worker interno
(`MantenimientoSesionesWorker`) invoca los mismos handlers vía `ISender` in-process (sin HTTP),
por lo que proteger todos los endpoints no rompe los barridos automáticos (inicio automático,
timeouts). Fuente: spec `2026-07-03-sp5a-autorizacion-enforcement-design.md` §5.2.

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
| `PartidaCancelada` | `{ partidaId, motivo }` (motivo ∈ `MinimosNoAlcanzados` \| `CanceladaPorOperador`, HU-40) |
| `PartidaFinalizada` | `{ partidaId }` |
| `PreguntaActivada` | `{ partidaId, juegoId, preguntaId, orden, fechaLimiteUtc }` |
| `PreguntaCerrada` | `{ partidaId, juegoId, preguntaId, opcionCorrectaId?, textoOpcionCorrecta?, ganadorParticipanteId?, ganadorEquipoId? }` |
| `EtapaActivada` | `{ partidaId, juegoId, etapaId, orden, fechaLimiteUtc }` |
| `EtapaCerrada` | `{ partidaId, juegoId, etapaId, ganadorParticipanteId?, ganadorEquipoId? }` |
| `EtapaGanada` | `{ partidaId, juegoId, etapaId, ganadorParticipanteId?, ganadorEquipoId? }` |
| `UbicacionActualizada` *(operador-only)* | `{ partidaId, participanteId, latitud, longitud, timestampUtc }` |
| `PistaEnviada` *(destino only: participante o equipo)* | `{ partidaId, juegoId, participanteDestinoId?, texto, timestampUtc, equipoDestinoId? }` |
| `ConvocatoriaCreada` *(convocado-destino only)* | `{ partidaId, equipoId, convocatoriaId, usuarioId }` |

Notas: `fechaLimiteUtc` = activación + tiempo límite (cuenta regresiva local en el cliente). Cambio aditivo (7d, HU-24/35/38/18/12): `PreguntaCerrada` gana `opcionCorrectaId?`/`textoOpcionCorrecta?` (la opción correcta de la pregunta que cierra — poblados en **todo** cierre, no solo por respuesta correcta: avance de operador y timeout también los incluyen) y `ganadorParticipanteId?`/`ganadorEquipoId?` (solo poblados cuando el motivo de cierre es `RespuestaCorrecta`; `null` en cierre por avance/timeout — ya existían en el evento interno, ahora se difunden). `EtapaCerrada`/`EtapaGanada` ganan `ganadorParticipanteId?`/`ganadorEquipoId?` desde los campos ya presentes en `EtapaBDTCerradaEvent`/`EtapaBDTGanadaEvent`. Todos son campos trailing con default `null`: clientes viejos que ignoran propiedades desconocidas no rompen. Los payloads siguen sin llevar puntos acumulados ni ranking (eso es Puntuaciones/SP-4) ni texto de preguntas/opciones/QR salvo la excepción explícita de `textoOpcionCorrecta` (revelado únicamente al cerrar, nunca antes — igual que el patrón anti-leak del resto del contrato). Eventos per-participante/scoring-adjacentes (`RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `TesoroQRValidado`) NO se difunden en este slice; su efecto sale por las transiciones de estado. El push se dispara in-process desde Operaciones (no requiere el broker RabbitMQ). La ruta gateway `/operaciones-sesion/hubs/sesion` es alcanzable end-to-end (SP-3g): el servicio mapea el hub y todos sus endpoints bajo el prefijo `operaciones-sesion`, y YARP reenvía el path completo sin `PathRemovePrefix` — consistente con el resto de los servicios. `UbicacionActualizada` (SP-3f-3) se difunde SOLO al grupo `operador:partida:{id}` (BR-B07: únicamente el operador ve el mapa; el participante emisor no lo recibe); `timestampUtc` es server-stamped; el relay no persiste ni emite evento de dominio (audit de ubicación → broker, diferido). `PistaEnviada` (SP-3f-4; destino equipo — SP-3e-4) se difunde al grupo `participante:{destinoId}` **o**, cuando el request trae `equipoDestinoId` (destino equipo, solo modalidad Equipo), al grupo `equipo:{equipoDestinoId}` — nunca a ambos a la vez (BR-B06: la pista es para un destino específico, un participante puntual o un equipo entero; el resto de participantes/equipos y el operador no la reciben). El payload gana `equipoDestinoId?` y `participanteDestinoId` pasa a nullable — exactamente uno de los dos viene poblado, según el destino del request original. Es event-only: el `texto` viaja en el payload (no hay pull) — única excepción al anti-leak, justamente porque es el contenido dirigido a ese destino. `timestampUtc` es server-stamped; no se persiste la entidad `Pista` ni se emite audit en este slice (el registro BR-B06 lo materializa audit vía broker, diferido). Sin replay: si el destino está offline (o, para destino equipo, ningún miembro está conectado), la pista se pierde (transitorio). `ConvocatoriaCreada` (SP-3e-1) se difunde SOLO al grupo `participante:{usuarioId}` del convocado (para que el móvil muestre la convocatoria entrante); no llega al grupo de partida ni al operador. `ConvocatoriaRespondida` no tiene push en tiempo real en este slice (alimenta audit/scoring vía broker RabbitMQ, diferido). Snapshot de membresía: los miembros a convocar se congelan al preinscribir (altas/bajas del equipo posteriores no afectan esa partida).
