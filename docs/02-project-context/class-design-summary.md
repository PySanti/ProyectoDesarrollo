# Class Design Summary — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

Derived from `docs/01-project-source/diagrama-de-clases.md`. Logical contexts materialize onto the four target services: **Identity**, **Partidas**, **Operaciones de Sesion**, **Puntuaciones** (behind the mandatory YARP gateway). The central structure is a **`Partida`** containing **sequential `Juego`s**, each a **`JuegoTrivia`** or **`JuegoBDT`**.

## Identidad context (service: Identity)

| Class | Type | Responsibility |
|---|---|---|
| Usuario | Aggregate root | Local user linked to Keycloak; `Crear/EditarDatosGenerales/Desactivar/ModificarRol/EmitirCredencialTemporal/MarcarCredencialDefinitiva`. |
| Rol | Aggregate root | Holds `Privilegios` and `PermisosFuncionales`; `AsignarPrivilegio/Retirar/.../EstaProtegido`. Only Administrador/Operador/Participante. |
| RolUsuario | Enum | Administrador, Operador, Participante. |
| EstadoUsuario | Enum | Activo, Desactivado. |
| EstadoCredencial | Enum | TemporalPendiente, Definitiva. |
| Privilegio | Enum | GestionarUsuarios, ModificarRolDeUsuario, GestionarPermisosDeRol, GestionarEquiposAdministrativamente, ConsultarOperativoModoLectura. |
| PermisoFuncional | Enum | GestionarPartidas, GestionarEquipos, ParticiparEnPartidas. |
| KeycloakId, Correo, RolId, NombreRol | Value Objects | External reference and role identity. |

## Equipos context (service: Identity)

| Class | Type | Responsibility |
|---|---|---|
| Equipo | Aggregate root | Controls 1–5 members, leadership, and state; receives members via `InvitacionEquipo`. |
| ParticipanteEquipo | Child entity | Team member with `UsuarioId`, join date, `EsLider`. |
| InvitacionEquipo | Aggregate root | Created by the leader from a dynamic list; `Aceptar/Rechazar`; does not expire. Independent of `Convocatoria`. |
| HistorialEquipoUsuario | Aggregate root | Stores only the names of teams a participant belonged to. |
| EquipoId, NombreEquipo, InvitacionEquipoId, ParticipanteEquipoId | Value Objects | Identifiers and name. |
| EstadoEquipo | Enum | Activo, Desactivado, Eliminado. |
| EstadoInvitacion | Enum | Pendiente, Aceptada, Rechazada. |

> Obsolete: there is no `CodigoAcceso`/team access code; members join only via `InvitacionEquipo`.

## Participación context (service: Operaciones de Sesion)

| Class | Type | Responsibility |
|---|---|---|
| InscripcionPartida | Aggregate root | Partida-level inscription; `CrearIndividual/PreinscribirEquipo/ConfirmarSiCumpleMinimos/Cancelar`. One active per participant/team. |
| Convocatoria | Child entity | Generated when a leader preinscribes a team; `Aceptar/Rechazar`; affects only that partida. |
| InscripcionId, ConvocatoriaId | Value Objects | Identifiers. |
| EstadoInscripcion | Enum | Preinscrita, Confirmada, Cancelada, ExcluidaPorMinimos. |
| EstadoConvocatoria | Enum | Pendiente, Aceptada, Rechazada. |
| Modalidad | Enum | Individual, Equipo (defined in Partidas; referenced here). |

## Partidas context (service: Partidas)

| Class | Type | Responsibility |
|---|---|---|
| Partida | Aggregate root | Contains 1..* `Juego` in sequential order; `Crear/AgregarJuego/PublicarPartida/ValidarMinimosParticipacion/IniciarPartida/CancelarAutomaticamentePorMinimos/ActivarSiguienteJuego/CalcularRankingConsolidado/CancelarPartida/FinalizarPartida`. |
| Juego | Base entity | `Orden`, `TipoJuego`, `EstadoJuego`, `PartidaId`; `Activar/Finalizar`. Specialized as `JuegoTrivia` / `JuegoBDT`. |
| RankingConsolidado | Value Object | `ConsolidarPorJuegosGanadosPuntosYTiempo()`; computed on finish (games won → total points → lowest time). |
| PartidaId, JuegoId, NombrePartida, TiempoInicio, MinimosParticipacion, MaximosParticipacion | Value Objects | Identity and configuration. |
| TipoJuego | Enum | Trivia, BusquedaDelTesoro. |
| EstadoJuego | Enum | Pendiente, Activo, Finalizado. |
| EstadoPartida | Enum | Lobby, Iniciada, Cancelada, Terminada. |
| Modalidad | Enum | Individual, Equipo (one per partida, all games). |
| ModoInicioPartida | Enum | Manual, Automatico, ManualYAutomatico. |

## Trivia context (`JuegoTrivia`)

> Config lives in **Partidas**; runtime in **Operaciones de Sesion**; scoring/ranking in **Puntuaciones**. There is no `FormularioTrivia`: `JuegoTrivia` owns its questions directly.

| Class | Type | Responsibility |
|---|---|---|
| JuegoTrivia | Aggregate root (specialization of `Juego`) | Owns `Pregunta`s (created with the game), `ParticipanteTrivia`, `RespuestaTrivia`; `AgregarPregunta/RegistrarRespuestaDefinitiva/CerrarPregunta/AvanzarPregunta/AcumularPuntaje/ActualizarRanking/Finalizar`. |
| Pregunta | Child entity | Text, `Opciones`, `PuntajeAsignado`, `TiempoLimite`; created with the game. |
| Opcion | Value Object | Text + `EsCorrecta`. |
| ParticipanteTrivia | Child entity | `PuntajeAcumulado`, `TiempoRespuestaAcumulado`; maps to `UsuarioId` or `EquipoId`. |
| RespuestaTrivia | Child entity | One per participant/question; `ValidarContraPregunta`. |
| PuntajeAsignado, TiempoLimite, RankingTrivia, ParticipanteId | Value Objects | Direct score, availability timer, native ranking. |
| TipoParticipante | Enum | Usuario, Equipo. |

## BDT context (`JuegoBDT`)

> Config lives in **Partidas**; runtime in **Operaciones de Sesion**; scoring/ranking in **Puntuaciones**.

| Class | Type | Responsibility |
|---|---|---|
| JuegoBDT | Aggregate root (specialization of `Juego`) | Owns `EtapaBDT`s, `ParticipanteBDT`, `TesoroQR`, `Pista`; `ActivarPrimeraEtapa/RegistrarTesoro/ValidarTesoro/CerrarEtapa/AvanzarEtapa/ActualizarRanking/EnviarPista/Finalizar`. |
| EtapaBDT | Child entity | `Orden`, `CodigoQREsperado`, `PuntajeAsignado`, `TiempoLimite`, `EstadoEtapa`, `GanadorId`, `TiempoResolucion`. Won stage grants its `Puntaje`. |
| ParticipanteBDT | Child entity | `PuntajeAcumulado`, `EtapasGanadas` (informative), `TiempoAcumuladoEtapasGanadas`, `UbicacionActual`, `GeolocalizacionAutorizada`. |
| TesoroQR | Child entity | One per upload attempt; `MarcarValido/MarcarInvalido/MarcarNoLegible/MarcarNoCorrespondeEtapaActiva`. |
| Pista | Child entity | Operator clue to a participant/team. |
| AreaBusqueda, CodigoQREsperado, PuntajeAsignado, UbicacionGeografica, TiempoResolucionEtapa, RankingBDT | Value Objects | Text area, expected QR text, per-stage points, location, won-stage time, native ranking. |
| EstadoEtapa | Enum | Pendiente, Activa, Ganada, CerradaPorTiempo, Cerrada. |
| ResultadoValidacionQR | Enum | Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva. |

## Auditoría context (cross-cutting; materialized in Puntuaciones and Operaciones de Sesion)

| Class | Type | Responsibility |
|---|---|---|
| RegistroAuditoria | Aggregate root | Groups historical events of a partida. |
| EventoHistorial | Child entity | A recorded fact. |
| TipoEventoHistorial | Enum | CambioEstado, Inscripcion, Convocatoria, InvitacionEquipo, JuegoActivado, JuegoFinalizado, RespuestaTrivia, TesoroSubido, ValidacionQR, PistaEnviada, Ubicacion, Ranking, RankingConsolidado, Puntaje, Cancelacion, Resultado, EquipoEliminado, CambioRol, PermisosRol. |

## Main relationships

| Relationship | Cardinality / rule |
|---|---|
| Usuario — ParticipanteEquipo | 1 — 0..1 (at most one active team). |
| Equipo — ParticipanteEquipo | 1 — 1..5. |
| Equipo — InvitacionEquipo | 1 — 0..* . |
| Usuario — HistorialEquipoUsuario | 1 — 0..1. |
| Partida — Juego | 1 — 1..* in sequential order. |
| Juego — JuegoTrivia / JuegoBDT | inheritance by `TipoJuego`. |
| Partida — RankingConsolidado | 1 — 0..1 (computed on finish). |
| Partida — InscripcionPartida | 1 — 0..* (single lobby phase; one inscription per participant/team). |
| InscripcionPartida — Convocatoria | 1 — 0..* . |
| JuegoTrivia — Pregunta | 1 — 1..* (created with the game). |
| Pregunta — Opcion | 1 — 2..* . |
| JuegoTrivia — ParticipanteTrivia / RespuestaTrivia | 1 — 1..* / 0..* . |
| JuegoBDT — EtapaBDT / ParticipanteBDT / Pista | 1 — 1..* / 1..* / 0..* . |
| EtapaBDT — TesoroQR | 1 — 0..* (multiple attempts). |
| Usuario — Rol | * — 1; Rol — Privilegio / PermisoFuncional 1 — 0..*. |
| RegistroAuditoria — EventoHistorial | 1 — 0..* ; Partida — RegistroAuditoria 1 — 1. |
