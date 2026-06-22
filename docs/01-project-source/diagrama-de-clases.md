## **1\. Contexto de Identidad**

`Umbral.Identity.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `Usuario` | Entidad / Agregado raíz | UsuarioId, KeycloakId, Nombre, Correo, Rol, Estado, EstadoCredencial  | `Crear()`, `EditarDatosGenerales()`, `Desactivar()`, `ModificarRol(), EmitirCredencialTemporal() , MarcarCredencialDefinitiva()`  | Referenciado por equipos, invitaciones, inscripciones, convocatorias, partidas, auditoría e historial. Tiene un `Rol` asignado |
| `Rol` | Agregado raíz  | `RolId, NombreRol, Privilegios, PermisosFuncionales` | `AsignarPrivilegio(), RetirarPrivilegio(), AsignarPermiso(), RetirarPermiso(), EstaProtegido()` | Define los privilegios de gobernanza y permisos funcionales de un rol. Solo existen Administrador, Operador y Participante; el rol Administrador protege sus privilegios de gobernanza. |
| `RolId` | Value Object | `Valor` | `EsValido()` | Identificador del rol. |
| `NombreRol` | Value Object | `Valor` | `EsValido()` | Nombre del rol (Administrador/Operador/Participante). |
| `Privilegio` | Enum | `GestionarUsuarios, ModificarRolDeUsuario, GestionarPermisosDeRol, GestionarEquiposAdministrativamente, ConsultarOperativoModoLectura` |  | Privilegios de gobernanza administrables por rol. |
| `PermisoFuncional` | Enum | `GestionarPartidas, GestionarEquipos, ParticiparEnPartidas` |  | Permisos funcionales agrupados; tener uno implica todas sus acciones. |
| `EstadoCredencial`  | Enum | `TemporalPendiente, Definitiva`  |  | Estado de la credencial del usuario; nace temporal pendiente y pasa a definitiva cuando el usuario cambia su contraseña.  |
| `UsuarioId` | Value Object | `Valor` | `EsValido()` | Identifica localmente al usuario. |
| `KeycloakId` | Value Object | `Valor` | `EsValido()` | Referencia externa del usuario en Keycloak. |
| `Correo` | Value Object | `Valor` | `EsValido()` | Correo del usuario. |
| `RolUsuario` | Enum | `Administrador`, `Operador`, `Participante` | — | Usado por `Usuario`. |
| `EstadoUsuario` | Enum | `Activo`, `Desactivado` | — | Usado por `Usuario`. |

## **2\. Contexto de Equipos**

`Umbral.Equipos.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `Equipo` | Agregado raíz | `EquipoId`, `NombreEquipo`, `EstadoEquipo`, `Participantes` | `CrearPorParticipante()`, `CrearPorAdministrador()`, `AgregarParticipante()`, `RemoverParticipante()`, `TransferirLiderazgo()`, `PuedeEliminarse()`, `Eliminar()`, `Desactivar()` | Contiene `1..5` participantes. Tiene un líder. Recibe integrantes mediante `InvitacionEquipo`. Puede ser usado en Trivia y BDT. |
| `ParticipanteEquipo` | Entidad hija | `ParticipanteEquipoId`, `UsuarioId`, `FechaUnion`, `EsLider` | `MarcarComoLider()`, `QuitarLiderazgo()` | Vive dentro del agregado `Equipo`. No es el mismo participante de Trivia ni BDT. |
| `InvitacionEquipo` | Agregado raíz  | `InvitacionEquipoId`, `EquipoId`, `UsuarioId`, `EstadoInvitacion`, `FechaEnvio`, `FechaRespuesta` | `Crear()`, `Aceptar()`, `Rechazar()`, `EstaPendiente()` | Asociada a un `Equipo` y a un `Usuario` invitado. La crea el líder mediante una lista dinámica. Independiente de la `Convocatoria`. |
| `HistorialEquipoUsuario` | Agregado raíz  | `UsuarioId`, `NombresEquipos` | `AgregarEquipo()`, `ObtenerNombres()` | Pertenece a un `Usuario`. Conserva solo los nombres de los equipos a los que ha pertenecido. |
| `EquipoId` | Value Object | `Valor` | `EsValido()` | Identificador del equipo. |
| `ParticipanteEquipoId` | Value Object | `Valor` | `EsValido()` | Identificador del participante dentro del equipo. |
| `NombreEquipo` | Value Object | `Valor` | `EsValido()` | Nombre del equipo. También usado por `HistorialEquipoUsuario`. |
| `InvitacionEquipoId` | Value Object  | `Valor` | `EsValido()` | Identificador de la invitación de equipo. |
| `EstadoEquipo` | Enum | `Activo`, `Desactivado`, `Eliminado` | — | Determina si el equipo puede operar o inscribirse. |
| `EstadoInvitacion` | Enum  | `Pendiente`, `Aceptada`, `Rechazada` | — | Controla el estado de una invitación de equipo. |

## **3\. Contexto de Participación**

`Umbral.Participacion.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `InscripcionPartida` | Agregado raíz | `InscripcionId`, `PartidaId`, `Modalidad`, `UsuarioId`, `EquipoId`, `EstadoInscripcion`, `FechaSolicitud`, `FechaConfirmacion` | `CrearIndividual()`, `PreinscribirEquipo()`, `ConfirmarSiCumpleMinimos()`, `Cancelar()`, `ExcluirPorMinimos()` | Se asocia a una partida; según la modalidad, hay una inscripción por participante (individual) o una por equipo (por equipo). Solo puede existir una inscripción activa por participante o por equipo a la vez; para el participante, la participación activa incluye además una convocatoria de equipo aceptada.  |
| `Convocatoria` | Entidad hija | `ConvocatoriaId`, `InscripcionId`, `PartidaId`, `EquipoId`, `UsuarioId`, `EstadoConvocatoria`, `FechaEnvio`, `FechaRespuesta` | `Aceptar()`, `Rechazar()`, `EstaAceptada()` | Convocatoria de partida: se genera cuando un líder preinscribe un equipo en una partida por equipos. Afecta solo a la participación en esa partida; no cambia la pertenencia al equipo. Un participante no puede aceptar una convocatoria si ya tiene una participación activa en otra partida (inscripción individual o convocatoria aceptada).  |
| `InscripcionId` | Value Object | `Valor` | `EsValido()` | Identificador de inscripción. |
| `ConvocatoriaId` | Value Object | `Valor` | `EsValido()` | Identificador de convocatoria. |
| `EstadoInscripcion` | Enum | `Preinscrita`, `Confirmada`, `Cancelada`, `ExcluidaPorMinimos` | — | Controla el estado de inscripción. |
| `EstadoConvocatoria` | Enum | `Pendiente`, `Aceptada`, `Rechazada` | — | Controla la respuesta del integrante convocado. |
| `Modalidad` | Enum | `Individual`, `Equipo` | — | Indica si la partida es individual o por equipo. Se define en el contexto Partidas; aquí se referencia para la inscripción. |

## **4\. Contexto de Partidas** 

`Umbral.Partidas.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `Partida` | Agregado raíz | `PartidaId`, `NombrePartida`, `EstadoPartida`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio`, `Juegos`, `RankingConsolidado` | `Crear()`, `AgregarJuego()`, `PublicarPartida()`, `ValidarMinimosParticipacion()`, `IniciarPartida()`, `CancelarAutomaticamentePorMinimos()`, `ActivarSiguienteJuego()`, `CalcularRankingConsolidado()`, `CancelarPartida()`, `FinalizarPartida()` | Contiene `1..*` `Juego` en orden secuencial. Tiene un único lobby como fase y, según la modalidad, una o varias inscripciones. |
| `Juego` | Entidad base | `JuegoId`, `Orden`, `TipoJuego`, `EstadoJuego`, `PartidaId` | `Activar()`, `Finalizar()` | Pertenece a `Partida`. Especializada en `JuegoTrivia` (contexto Trivia) y `JuegoBDT` (contexto BDT) según `TipoJuego`. |
| `PartidaId` | Value Object | `Valor` | `EsValido()` | Identificador de partida. Referenciado por `JuegoTrivia`, `JuegoBDT`, inscripciones y auditoría. |
| `JuegoId` | Value Object | `Valor` | `EsValido()` | Identificador de juego. Usado por `Juego`, `JuegoTrivia` y `JuegoBDT`. |
| `RankingConsolidado` | Value Object | `Posiciones` | `ConsolidarPorJuegosGanadosPuntosYTiempo()`  | Resultado calculado para la partida al finalizar; ordena por número de juegos ganados, luego por puntaje total y luego por menor tiempo total.  |
| `TipoJuego` | Enum | `Trivia`, `BusquedaDelTesoro` | — | Define el tipo de cada juego. |
| `EstadoJuego` | Enum | `Pendiente`, `Activo`, `Finalizado` | — | Sub-estado interno de un juego dentro de la partida. |
| `EstadoPartida` | Enum | `Lobby`, `Iniciada`, `Cancelada`, `Terminada` | — | Estado de la partida. |
| `Modalidad` | Enum | `Individual`, `Equipo` | — | Modalidad de la partida; aplica a todos sus juegos. |
| `ModoInicioPartida` | Enum | `Manual`, `Automatico`, `ManualYAutomatico` | — | Modo de inicio. `Manual`: la inicia el operador. `Automatico`: se inicia al llegar `TiempoInicio`. `ManualYAutomatico`: el operador puede iniciarla antes del `TiempoInicio` o, si no lo hace, se inicia automáticamente al llegarlo. Todo inicio requiere cumplir los mínimos de participación.  |

### 

## **5\. Contexto de Trivia**

`Umbral.Trivias.Domain`

Nota: los value objects `PartidaId` y `JuegoId` y los enums `EstadoJuego`, `EstadoPartida` y `Modalidad` se definen en el contexto Partidas; aquí se referencian. Se elimina el agregado `FormularioTrivia`: sus preguntas y value objects de contenido pasan a `JuegoTrivia`.

**Agregado `JuegoTrivia`**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| JuegoTrivia | Agregado raíz (especialización de Juego) | JuegoId, PartidaId, Orden, EstadoJuego, Preguntas, Participantes, Respuestas, PreguntaActualId | Crear(), AgregarPregunta(), Activar(), RegistrarRespuestaDefinitiva(), CerrarPregunta(), AvanzarPregunta(), AcumularPuntaje(), ActualizarRanking(), Finalizar() | Especialización de Juego. Contiene directamente sus preguntas (1..\*), creadas al crear el juego. Contiene Participantes activos. Pertenece a una Partida (PartidaId). Genera eventos e historial. |
| Pregunta | Entidad hija | PreguntaId, Texto, Opciones, PuntajeAsignado, TiempoLimite | AgregarOpcion(), DefinirRespuestaCorrecta(), EsValida(), ObtenerRespuestaCorrecta() | Pertenece a JuegoTrivia. Se crea al crear el juego. |
| Opcion | Value Object | Texto, EsCorrecta | — | Pertenece a una Pregunta. |
| ParticipanteTrivia | Entidad hija | ParticipanteId, TipoParticipante, PuntajeAcumulado, TiempoRespuestaAcumulado | AcumularPuntaje(), AcumularTiempoRespuesta() | Representa participante individual o equipo. Su ID puede mapear a UsuarioId o EquipoId. |
| RespuestaTrivia | Entidad hija | RespuestaId, ParticipanteId, PreguntaId, OpcionSeleccionada, EsCorrecta, TiempoEmpleado, FechaRespuesta | ValidarContraPregunta() | Pertenece a JuegoTrivia. Solo una por Participante/pregunta. |
| RankingTrivia | Value Object | Posiciones | OrdenarPorPuntajeYTiempo() | Resultado calculado para el juego de Trivia. |
| ParticipanteId | Value Object | Valor, TipoParticipante | EsUsuario(), EsEquipo() | Identifica al Participante lógico. |
| RespuestaId | Value Object | Valor | EsValido() | Identificador de respuesta. |
| PreguntaId | Value Object | Valor | EsValido() | Identificador de pregunta. |
| PuntajeAsignado | Value Object | Valor | EsValido() | Puntaje directo otorgado si la respuesta es correcta. |
| TiempoLimite | Value Object | Segundos | EsValido() | Tiempo para controlar disponibilidad de respuesta. No modifica puntaje. |
| TipoParticipante | Enum | Usuario, Equipo | — | Define si compite un usuario o equipo. |
| PartidaId | — | (reubicado) | — | Se define en el contexto Partidas; en JuegoTrivia es una referencia. |
| EstadoPartida | — | (reubicado) | — | Se traslada al contexto Partidas (estado a nivel partida). |
| Modalidad | — | (reubicado) | — | Se traslada al contexto Partidas (modalidad a nivel partida). |

## **6\. Contexto de Búsqueda del Tesoro**

`Umbral.Bdt.Domain`

Nota: los value objects `PartidaId` y `JuegoId` y los enums `EstadoJuego`, `EstadoPartida` y `Modalidad` se definen en el contexto Partidas; aquí se referencian.

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `JuegoBDT` | Agregado raíz (especialización de `Juego`) | `JuegoId`, `PartidaId`, `Orden`, `EstadoJuego`, `AreaBusqueda`, `Etapas`, `Participantes`, `IndiceEtapaActual` | `Activar()`, `ActivarPrimeraEtapa()`, `RegistrarTesoro()`, `ValidarTesoro()`, `CerrarEtapa()`, `AvanzarEtapa()`, `ActualizarRanking()`, `EnviarPista()`, `Finalizar()` | Especialización de `Juego`. Contiene etapas, participantes activos, tesoros subidos y pistas. Pertenece a una `Partida` (`PartidaId`). |
| `EtapaBDT` | Entidad hija | `EtapaId`, `Orden`, `CodigoQREsperado`, `PuntajeAsignado`, `TiempoLimite`, `EstadoEtapa`, `GanadorId`, `TiempoResolucion` | `Activar()`, `ValidarQR()`, `MarcarGanadaPor()`, `CerrarPorGanador()`, `CerrarPorTiempo()` | Pertenece a `JuegoBDT`. Su puntaje alimenta el ranking consolidado de la partida. |
| `ParticipanteBDT` | Entidad hija | `ParticipanteId`, `TipoParticipante`, `PuntajeAcumulado`, `EtapasGanadas`, `TiempoAcumuladoEtapasGanadas`, `UbicacionActual`, `GeolocalizacionAutorizada`  | `AutorizarGeolocalizacion()`, `ActualizarUbicacion()`, `RegistrarEtapaGanada()`, `AcumularPuntaje()`  | Representa participante individual o equipo. Su ID puede mapear a `UsuarioId` o `EquipoId`. Su puntaje en el juego es la suma de los puntos de las etapas ganadas. |
| `TesoroQR` | Entidad hija | `TesoroId`, `EtapaId`, `ParticipanteId`, `ImagenUrl`, `QrDecodificado`, `ResultadoValidacion`, `FechaEnvio` | `MarcarValido()`, `MarcarInvalido()`, `MarcarNoLegible()`, `MarcarNoCorrespondeEtapaActiva()` | Se registra por cada intento de subida. Puede haber varios por etapa. |
| `Pista` | Entidad hija | `PistaId`, `Texto`, `DestinatarioId`, `FechaEnvio`, `OperadorId` | `Despachar()` | Enviada por operador a participante/equipo. |
| `RankingBDT` | Value Object | `Posiciones` | OrdenarPorPuntajeYTiempo()  | Ranking del juego de BDT por puntaje acumulado (suma de los puntos de las etapas ganadas) y desempate por menor tiempo de esas etapas. |
| `AreaBusqueda` | Value Object | `Descripcion` | `EsValida()` | Texto descriptivo simple del área de búsqueda. |
| `CodigoQREsperado` | Value Object | `Valor` | `CoincideCon(qrDecodificado)` | Contenido textual esperado del QR. |
| `PuntajeAsignado` | Value Object  | `Valor` | `EsValido()` | Puntaje otorgado por ganar la etapa; alimenta el ranking consolidado. |
| `UbicacionGeografica` | Value Object | `Latitud`, `Longitud`, `FechaRegistro` | `EsValida()` | Ubicación enviada por participante autorizado. |
| `TiempoResolucionEtapa` | Value Object | `Segundos` | `EsValido()` | Tiempo usado para desempate BDT en etapas ganadas. |
| `EtapaId` | Value Object | `Valor` | `EsValido()` | Identificador de etapa. |
| `TesoroId` | Value Object | `Valor` | `EsValido()` | Identificador de tesoro subido. |
| `PistaId` | Value Object | `Valor` | `EsValido()` | Identificador de pista. |
| `EstadoEtapa` | Enum | `Pendiente`, `Activa`, `Ganada`, `CerradaPorTiempo`, `Cerrada` | — | Estado interno de una etapa BDT. |
| `ResultadoValidacionQR` | Enum | `Valido`, `Invalido`, `NoLegible`, `NoCorrespondeEtapaActiva` | — | Resultado del envío del tesoro. |
| `ModoInicioPartida` | — | **(reubicado)** | — | Se traslada al contexto Partidas (modo de inicio a nivel partida). |
| `TipoParticipante` | Enum | `Usuario`, `Equipo` | — | Define si participa usuario o equipo. |

## **7\. Contexto de Auditoría**

`Umbral.Auditoria.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `RegistroAuditoria` | Agregado raíz | `RegistroAuditoriaId`, `PartidaId`, `Eventos` | `CrearParaPartida()`, `AgregarEvento()` | Agrupa eventos históricos de una partida. Capacidad transversal materializada en Puntuaciones y Operaciones de sesión. |
| `EventoHistorial` | Entidad hija | `EventoHistorialId`, `TipoEvento`, `ActorId`, `Descripcion`, `FechaOcurrencia`, `Datos` | `Crear()` | Pertenece a `RegistroAuditoria`. |
| `RegistroAuditoriaId` | Value Object | `Valor` | `EsValido()` | Identificador del registro. |
| `EventoHistorialId` | Value Object | `Valor` | `EsValido()` | Identificador del evento. |
| `TipoEventoHistorial` | Enum | `CambioEstado`, `Inscripcion`, `Convocatoria`, `InvitacionEquipo`, `JuegoActivado`, `JuegoFinalizado`, `RespuestaTrivia`, `TesoroSubido`, `ValidacionQR`, `PistaEnviada`, `Ubicacion`, `Ranking`, `RankingConsolidado`, `Puntaje`, `Cancelacion`, `Resultado`, `EquipoEliminado`, `CambioRol`, `PermisosRol` | — | Clasifica eventos históricos. Se añaden `CambioRol` y `PermisosRol` (gobernanza), además de los ya incorporados `InvitacionEquipo`, `JuegoActivado`, `JuegoFinalizado` y `RankingConsolidado`. |

# **Relaciones principales del diagrama**

| Relación | Cardinalidad | Descripción |
| ----- | ----- | ----- |
| `Usuario` — `ParticipanteEquipo` | `1` — `0..1` | Un usuario participante puede pertenecer como máximo a un equipo activo. |
| `Equipo` — `ParticipanteEquipo` | `1` — `1..5` | Un equipo contiene de 1 a 5 integrantes. |
| `Equipo` — `InvitacionEquipo` | `1` — `0..*` | Un equipo puede tener múltiples invitaciones de equipo. |
| `Usuario` — `InvitacionEquipo` | `1` — `0..*` | Un usuario puede recibir múltiples invitaciones (a lo sumo una pendiente por cada equipo que lo invite). |
| `Usuario` — `HistorialEquipoUsuario` | `1` — `0..1` | Cada usuario tiene un historial con los nombres de los equipos a los que ha pertenecido. |
| `Equipo` — `InscripcionPartida` | `1` — `0..*` | Un equipo puede acumular varias inscripciones a lo largo del tiempo, pero solo una activa a la vez (partida en lobby o iniciada, inscripción preinscrita o confirmada).  |
| `Usuario — InscripcionPartida`  | `1 — 0..*`  | Un participante puede acumular varias inscripciones individuales a lo largo del tiempo, pero solo una activa a la vez. |
| `InscripcionPartida` — `Convocatoria` | `1` — `0..*` | Una preinscripción por equipo genera convocatorias para integrantes. |
| `Convocatoria` — `Usuario` | `*` — `1` | Cada convocatoria corresponde a un usuario convocado. |
| `Partida` — `Juego` | `1` — `1..*` | Una partida contiene uno o más juegos en orden secuencial. |
| `Juego` — `JuegoTrivia` / `JuegoBDT` | herencia | Un `Juego` se especializa en `JuegoTrivia` o `JuegoBDT` según `TipoJuego`. |
| `Partida` — `RankingConsolidado` | `1` — `0..1` | Una partida finalizada tiene un ranking consolidado (por juegos ganados, puntaje total y tiempo). |
| `Pregunta` — `Opcion` | `1` — `2..*` | Una pregunta tiene opciones de respuesta. |
| `JuegoTrivia` — `ParticipanteTrivia` | `1` — `1..*` | Un juego de Trivia tiene Participantes activos. |
| `JuegoTrivia` — `RespuestaTrivia` | `1` — `0..*` | Un juego de Trivia registra respuestas. |
| `ParticipanteTrivia` — `RespuestaTrivia` | `1` — `0..*` | Un Participante puede responder distintas preguntas, una vez por pregunta. |
| `JuegoBDT` — `EtapaBDT` | `1` — `1..*` | Un juego de BDT contiene una o más etapas. |
| `JuegoBDT` — `ParticipanteBDT` | `1` — `1..*` | Un juego de BDT tiene Participantes activos. |
| `EtapaBDT` — `TesoroQR` | `1` — `0..*` | Una etapa puede recibir múltiples intentos de QR. |
| `ParticipanteBDT` — `TesoroQR` | `1` — `0..*` | Un Participante puede subir múltiples tesoros QR. |
| `JuegoBDT` — `Pista` | `1` — `0..*` | El operador puede enviar pistas durante el juego. |
| `ParticipanteBDT` — `UbicacionGeografica` | `1` — `0..1 actual` | Un Participante mantiene su ubicación actual durante un juego BDT activo. |
| `Partida` — `InscripcionPartida` | `1` — `0..*` | Una partida puede tener múltiples inscripciones según la modalidad (una por participante en individual; una por equipo en partidas por equipo); la fase de lobby es única por partida. |
| `RegistroAuditoria` — `EventoHistorial` | `1` — `0..*` | Un registro agrupa eventos históricos. |
| `Partida` — `RegistroAuditoria` | `1` — `1` | Cada partida tiene trazabilidad histórica asociada. |
| `JuegoTrivia — Pregunta` | `1 — 1..*` | Un juego de Trivia contiene una o más preguntas, creadas al crearlo. |
| `Usuario — Rol` | `* — 1` | Cada usuario tiene un rol asignado que define sus permisos y privilegios. |
| `Rol — Privilegio` | `1 — 0..*` | Un rol agrupa privilegios de gobernanza. |
| `Rol — PermisoFuncional` | `1 — 0..*` | Un rol agrupa permisos funcionales. |

