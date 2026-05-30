# **Diagrama de clases actualizado en forma de tabla**

## **1\. Contexto de Identidad**

`Umbral.Identity.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `Usuario` | Entidad / Agregado raíz | `UsuarioId`, `KeycloakId`, `Nombre`, `Correo`, `Rol`, `Estado` | `Crear()`, `EditarDatosGenerales()`, `Desactivar()` | Referenciado por equipos, inscripciones, convocatorias, partidas, auditoría e historial. |
| `UsuarioId` | Value Object | `Valor` | `EsValido()` | Identifica localmente al usuario. |
| `KeycloakId` | Value Object | `Valor` | `EsValido()` | Referencia externa del usuario en Keycloak. |
| `Correo` | Value Object | `Valor` | `EsValido()` | Correo del usuario. |
| `RolUsuario` | Enum | `Administrador`, `Operador`, `Participante` | — | Usado por `Usuario`. |
| `EstadoUsuario` | Enum | `Activo`, `Desactivado` | — | Usado por `Usuario`. |

---

## **2\. Contexto de Equipos**

`Umbral.Equipos.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `Equipo` | Agregado raíz | `EquipoId`, `NombreEquipo`, `CodigoAcceso`, `EstadoEquipo`, `Participantes` | `CrearPorParticipante()`, `CrearPorAdministrador()`, `AgregarParticipante()`, `RemoverParticipante()`, `TransferirLiderazgo()`, `PuedeEliminarse()`, `Eliminar()`, `Desactivar()` | Contiene `1..5` participantes. Tiene un líder. Puede ser usado en Trivia y BDT. |
| `ParticipanteEquipo` | Entidad hija | `ParticipanteEquipoId`, `UsuarioId`, `FechaUnion`, `EsLider` | `MarcarComoLider()`, `QuitarLiderazgo()` | Vive dentro del agregado `Equipo`. No es el mismo participante de Trivia ni BDT. |
| `EquipoId` | Value Object | `Valor` | `EsValido()` | Identificador del equipo. |
| `ParticipanteEquipoId` | Value Object | `Valor` | `EsValido()` | Identificador del participante dentro del equipo. |
| `NombreEquipo` | Value Object | `Valor` | `EsValido()` | Nombre del equipo. |
| `CodigoAcceso` | Value Object | `Valor` | `Generar()`, `CoincideCon()` | Código usado para unirse al equipo. |
| `EstadoEquipo` | Enum | `Activo`, `Desactivado`, `Eliminado` | — | Determina si el equipo puede operar o inscribirse. |

---

## **3\. Contexto de Participación**

`Umbral.Participacion.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `InscripcionPartida` | Agregado raíz | `InscripcionId`, `PartidaId`, `TipoPartida`, `Modalidad`, `UsuarioId`, `EquipoId`, `EstadoInscripcion`, `FechaSolicitud`, `FechaConfirmacion` | `CrearIndividual()`, `PreinscribirEquipo()`, `ConfirmarSiCumpleMinimos()`, `Cancelar()`, `ExcluirPorMinimos()` | Se asocia a una partida Trivia o BDT. Puede ser individual o por equipo. |
| `Convocatoria` | Entidad hija | `ConvocatoriaId`, `InscripcionId`, `PartidaId`, `EquipoId`, `UsuarioId`, `EstadoConvocatoria`, `FechaEnvio`, `FechaRespuesta` | `Aceptar()`, `Rechazar()`, `EstaAceptada()` | Se genera cuando un líder preinscribe un equipo en una partida por equipos. |
| `InscripcionId` | Value Object | `Valor` | `EsValido()` | Identificador de inscripción. |
| `ConvocatoriaId` | Value Object | `Valor` | `EsValido()` | Identificador de convocatoria. |
| `EstadoInscripcion` | Enum | `Preinscrita`, `Confirmada`, `Cancelada`, `ExcluidaPorMinimos` | — | Controla el estado de inscripción. |
| `EstadoConvocatoria` | Enum | `Pendiente`, `Aceptada`, `Rechazada` | — | Controla la respuesta del integrante convocado. |
| `TipoPartida` | Enum | `Trivia`, `BusquedaDelTesoro` | — | Indica el modo de juego asociado. |
| `Modalidad` | Enum | `Individual`, `Equipo` | — | Indica si la partida es individual o por equipo. |

---

## **4\. Contexto de Trivia**

`Umbral.Trivias.Domain`

### **4.1 Agregado `FormularioTrivia`**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `FormularioTrivia` | Agregado raíz | `FormularioId`, `Titulo`, `Preguntas`, `OperadorId` | `Crear()`, `AgregarPregunta()`, `EditarPregunta()`, `ValidarFormulario()` | Tiene `1..*` preguntas. Es usado por `PartidaTrivia`. |
| `Pregunta` | Entidad hija | `PreguntaId`, `Texto`, `Opciones`, `PuntajeAsignado`, `TiempoLimite` | `AgregarOpcion()`, `DefinirRespuestaCorrecta()`, `EsValida()`, `ObtenerRespuestaCorrecta()` | Pertenece a `FormularioTrivia`. |
| `Opcion` | Value Object | `Texto`, `EsCorrecta` | — | Pertenece a una `Pregunta`. |
| `FormularioId` | Value Object | `Valor` | `EsValido()` | Identificador del formulario. |
| `PreguntaId` | Value Object | `Valor` | `EsValido()` | Identificador de pregunta. |
| `PuntajeAsignado` | Value Object | `Valor` | `EsValido()` | Puntaje directo otorgado si la respuesta es correcta. |
| `TiempoLimite` | Value Object | `Segundos` | `EsValido()` | Tiempo para controlar disponibilidad de respuesta. No modifica puntaje. |

### **4.2 Agregado `PartidaTrivia`**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `PartidaTrivia` | Agregado raíz | `PartidaId`, `Nombre`, `EstadoPartida`, `Modalidad`, `FormularioTriviaId`, `Competidores`, `Respuestas`, `PreguntaActualId`, `TiempoInicio`, `InicioAutomatico` | `PublicarLobby()`, `ValidarMinimosParticipacion()`, `IniciarPartida()`, `CancelarAutomaticamentePorMinimos()`, `RegistrarRespuestaDefinitiva()`, `CerrarPregunta()`, `AvanzarPregunta()`, `AcumularPuntaje()`, `ActualizarRanking()`, `CancelarPartida()`, `FinalizarPartida()` | Usa un `FormularioTrivia`. Contiene competidores activos. Genera eventos e historial. |
| `CompetidorTrivia` | Entidad hija | `CompetidorId`, `TipoCompetidor`, `PuntajeAcumulado`, `TiempoRespuestaAcumulado` | `AcumularPuntaje()`, `AcumularTiempoRespuesta()` | Representa jugador individual o equipo. Su ID puede mapear a `UsuarioId` o `EquipoId`. |
| `RespuestaTrivia` | Entidad hija | `RespuestaId`, `CompetidorId`, `PreguntaId`, `OpcionSeleccionada`, `EsCorrecta`, `TiempoEmpleado`, `FechaRespuesta` | `ValidarContraPregunta()` | Pertenece a `PartidaTrivia`. Solo una por competidor/pregunta. |
| `RankingTrivia` | Value Object | `Posiciones` | `OrdenarPorPuntajeYTiempo()` | Resultado calculado para la partida. |
| `PartidaId` | Value Object | `Valor` | `EsValido()` | Identificador de partida. |
| `CompetidorId` | Value Object | `Valor`, `TipoCompetidor` | `EsUsuario()`, `EsEquipo()` | Identifica al competidor lógico. |
| `RespuestaId` | Value Object | `Valor` | `EsValido()` | Identificador de respuesta. |
| `TipoCompetidor` | Enum | `Usuario`, `Equipo` | — | Define si compite un usuario o equipo. |
| `EstadoPartida` | Enum | `Lobby`, `Iniciada`, `Cancelada`, `Terminada` | — | Estado común de partida. |
| `Modalidad` | Enum | `Individual`, `Equipo` | — | Modalidad de la partida. |

---

## **5\. Contexto de Búsqueda del Tesoro**

`Umbral.Bdt.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `PartidaBDT` | Agregado raíz | `PartidaId`, `Nombre`, `EstadoPartida`, `Modalidad`, `AreaBusqueda`, `Etapas`, `Exploradores`, `IndiceEtapaActual`, `ModoInicio` | `PublicarLobby()`, `ValidarMinimosParticipacion()`, `IniciarPartida()`, `CancelarAutomaticamentePorMinimos()`, `ActivarPrimeraEtapa()`, `RegistrarTesoro()`, `ValidarTesoro()`, `CerrarEtapa()`, `AvanzarEtapa()`, `ActualizarRanking()`, `EnviarPista()`, `CancelarPartida()`, `FinalizarPartida()` | Contiene etapas, exploradores activos, tesoros subidos y pistas. |
| `EtapaBDT` | Entidad hija | `EtapaId`, `Orden`, `CodigoQREsperado`, `TiempoLimite`, `EstadoEtapa`, `GanadorId`, `TiempoResolucion` | `Activar()`, `ValidarQR()`, `MarcarGanadaPor()`, `CerrarPorGanador()`, `CerrarPorTiempo()` | Pertenece a `PartidaBDT`. |
| `ExploradorBDT` | Entidad hija | `ExploradorId`, `TipoCompetidor`, `EtapasGanadas`, `TiempoAcumuladoEtapasGanadas`, `UbicacionActual`, `GeolocalizacionAutorizada` | `AutorizarGeolocalizacion()`, `ActualizarUbicacion()`, `RegistrarEtapaGanada()` | Representa jugador individual o equipo. Su ID puede mapear a `UsuarioId` o `EquipoId`. |
| `TesoroQR` | Entidad hija | `TesoroId`, `EtapaId`, `ExploradorId`, `ImagenUrl`, `QrDecodificado`, `ResultadoValidacion`, `FechaEnvio` | `MarcarValido()`, `MarcarInvalido()`, `MarcarNoLegible()`, `MarcarNoCorrespondeEtapaActiva()` | Se registra por cada intento de subida. Puede haber varios por etapa. |
| `Pista` | Entidad hija | `PistaId`, `Texto`, `DestinatarioId`, `FechaEnvio`, `OperadorId` | `Despachar()` | Enviada por operador a jugador/equipo. |
| `RankingBDT` | Value Object | `Posiciones` | `OrdenarPorEtapasGanadasYTiempo()` | Ranking por etapas ganadas y desempate por tiempo acumulado de etapas ganadas. |
| `AreaBusqueda` | Value Object | `Descripcion` | `EsValida()` | Texto descriptivo simple del área de búsqueda. |
| `CodigoQREsperado` | Value Object | `Valor` | `CoincideCon(qrDecodificado)` | Contenido textual esperado del QR. |
| `UbicacionGeografica` | Value Object | `Latitud`, `Longitud`, `FechaRegistro` | `EsValida()` | Ubicación enviada por participante autorizado. |
| `TiempoResolucionEtapa` | Value Object | `Segundos` | `EsValido()` | Tiempo usado para desempate BDT en etapas ganadas. |
| `EtapaId` | Value Object | `Valor` | `EsValido()` | Identificador de etapa. |
| `TesoroId` | Value Object | `Valor` | `EsValido()` | Identificador de tesoro subido. |
| `PistaId` | Value Object | `Valor` | `EsValido()` | Identificador de pista. |
| `EstadoEtapa` | Enum | `Pendiente`, `Activa`, `Ganada`, `CerradaPorTiempo`, `Cerrada` | — | Estado interno de una etapa BDT. |
| `ResultadoValidacionQR` | Enum | `Valido`, `Invalido`, `NoLegible`, `NoCorrespondeEtapaActiva` | — | Resultado del envío del tesoro. |
| `ModoInicioPartida` | Enum | `Manual`, `Automatico`, `ManualYAutomatico` | — | Define modo de inicio BDT. |
| `TipoCompetidor` | Enum | `Usuario`, `Equipo` | — | Define si participa usuario o equipo. |

---

## **6\. Contexto de Auditoría**

`Umbral.Auditoria.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `RegistroAuditoria` | Agregado raíz | `RegistroAuditoriaId`, `PartidaId`, `Eventos` | `CrearParaPartida()`, `AgregarEvento()` | Agrupa eventos históricos de una partida. |
| `EventoHistorial` | Entidad hija | `EventoHistorialId`, `TipoEvento`, `ActorId`, `Descripcion`, `FechaOcurrencia`, `Datos` | `Crear()` | Pertenece a `RegistroAuditoria`. |
| `RegistroAuditoriaId` | Value Object | `Valor` | `EsValido()` | Identificador del registro. |
| `EventoHistorialId` | Value Object | `Valor` | `EsValido()` | Identificador del evento. |
| `TipoEventoHistorial` | Enum | `CambioEstado`, `Inscripcion`, `Convocatoria`, `RespuestaTrivia`, `TesoroSubido`, `ValidacionQR`, `PistaEnviada`, `Ubicacion`, `Ranking`, `Puntaje`, `Cancelacion`, `Resultado`, `EquipoEliminado` | — | Clasifica eventos históricos. |

---

# **Relaciones principales del diagrama**

| Relación | Cardinalidad | Descripción |
| ----- | ----- | ----- |
| `Usuario` — `ParticipanteEquipo` | `1` — `0..1` | Un usuario participante puede pertenecer como máximo a un equipo activo. |
| `Equipo` — `ParticipanteEquipo` | `1` — `1..5` | Un equipo contiene de 1 a 5 integrantes. |
| `Equipo` — `CodigoAcceso` | `1` — `1` | Cada equipo tiene un código único de acceso. |
| `Equipo` — `InscripcionPartida` | `1` — `0..*` | Un equipo puede preinscribirse/inscribirse en múltiples partidas, si está activo y cumple reglas. |
| `InscripcionPartida` — `Convocatoria` | `1` — `0..*` | Una preinscripción por equipo genera convocatorias para integrantes. |
| `Convocatoria` — `Usuario` | `*` — `1` | Cada convocatoria corresponde a un usuario convocado. |
| `FormularioTrivia` — `Pregunta` | `1` — `1..*` | Un formulario contiene una o más preguntas. |
| `Pregunta` — `Opcion` | `1` — `2..*` | Una pregunta tiene opciones de respuesta. |
| `PartidaTrivia` — `FormularioTrivia` | `1` — `1` | Una Trivia se basa en un formulario válido. |
| `PartidaTrivia` — `CompetidorTrivia` | `1` — `1..*` | Una partida de Trivia tiene competidores activos. |
| `PartidaTrivia` — `RespuestaTrivia` | `1` — `0..*` | Una partida registra respuestas. |
| `CompetidorTrivia` — `RespuestaTrivia` | `1` — `0..*` | Un competidor puede responder distintas preguntas, una vez por pregunta. |
| `PartidaBDT` — `EtapaBDT` | `1` — `1..*` | Una BDT contiene una o más etapas. |
| `PartidaBDT` — `ExploradorBDT` | `1` — `1..*` | Una BDT tiene exploradores activos. |
| `EtapaBDT` — `TesoroQR` | `1` — `0..*` | Una etapa puede recibir múltiples intentos de QR. |
| `ExploradorBDT` — `TesoroQR` | `1` — `0..*` | Un explorador puede subir múltiples tesoros QR. |
| `PartidaBDT` — `Pista` | `1` — `0..*` | El operador puede enviar pistas durante la partida. |
| `ExploradorBDT` — `UbicacionGeografica` | `1` — `0..1 actual` | Un explorador mantiene su ubicación actual durante BDT iniciada. |
| `PartidaTrivia / PartidaBDT` — `InscripcionPartida` | `1` — `0..*` | Una partida puede tener múltiples inscripciones. |
| `RegistroAuditoria` — `EventoHistorial` | `1` — `0..*` | Un registro agrupa eventos históricos. |
| `PartidaTrivia / PartidaBDT` — `RegistroAuditoria` | `1` — `1` | Cada partida tiene trazabilidad histórica asociada. |

