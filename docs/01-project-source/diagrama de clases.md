# **Diagrama de clases en forma de tabla**

## **1\. Contexto de Identidad**

`Umbral.Identity.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **Usuario** | Entidad / Agregado raíz | `UsuarioId`, `KeycloakId`, `Nombre`, `Correo`, `Rol`, `Estado` | `EditarDatosGenerales()`, `Desactivar()` | Referenciado por equipos, partidas, auditoría, convocatorias e historial. |
| **RolUsuario** | Enum | `Administrador`, `Operador`, `Participante` | — | Usado por `Usuario`. |
| **EstadoUsuario** | Enum | `Activo`, `Desactivado` | — | Usado por `Usuario`. |
| **KeycloakId** | Value Object | `Valor` | `EsValido()` | Referencia externa al usuario autenticado. |

## 

## **2\. Contexto de Equipos**

`Umbral.Equipos.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **Equipo** | Agregado raíz | `EquipoId`, `NombreEquipo`, `CodigoAcceso`, `EstadoEquipo`, `Participantes` | `Crear()`, `AgregarParticipante()`, `RemoverParticipante()`, `TransferirLiderazgo()`, `Eliminar()`, `Desactivar()` | Contiene `1..5` participantes. Tiene un código de acceso. Puede ser usado en Trivia y BDT. |
| **Participante** | Entidad hija | `ParticipanteId`, `UsuarioId`, `FechaUnion`, `EsLider` | `MarcarComoLider()`, `QuitarLiderazgo()` | Vive dentro del agregado `Equipo`. No es el mismo `Participante` de Trivia ni BDT. |
| **EquipoId** | Value Object | `Valor` | — | Identificador del equipo. |
| **NombreEquipo** | Value Object | `Valor` | `EsValido()` | Nombre del equipo. |
| **CodigoAcceso** | Value Object | `Valor` | `Generar()`, `CoincideCon()` | Código usado para unirse al equipo. |
| **EstadoEquipo** | Enum | `Activo`, `Desactivado`, `Eliminado` | — | Determina si el equipo puede operar o inscribirse. |

## **3\. Contexto de Trivia**

`Umbral.Trivias.Domain`

### **3.1 Agregado `FormularioTrivia`**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **FormularioTrivia** | Agregado raíz | `FormularioId`, `Titulo`, `Preguntas`, `OperadorId` | `AgregarPregunta()`, `EditarPregunta()`, `ValidarFormulario()` | Tiene `1..*` preguntas. Es usado por `PartidaTrivia`. |
| **Pregunta** | Entidad hija | `PreguntaId`, `Texto`, `Opciones`, `PuntajeAsignado`, `TiempoLimite` | `AgregarOpcion()`, `DefinirRespuestaCorrecta()`, `EsValida()` | Pertenece a `FormularioTrivia`. |
| **Opcion** | Value Object | `Texto`, `EsCorrecta` | — | Pertenece a una `Pregunta`. |
| **PuntajeAsignado** | Value Object | `Valor` | — | Puntaje específico de una pregunta. |
| **TiempoLimite** | Value Object | `Segundos` | — | Tiempo definido para responder una pregunta. |

El SRS exige que los formularios de Trivia tengan preguntas, opciones, respuesta correcta, puntaje asignado y tiempo límite por pregunta.

### **3.2 Agregado `PartidaTrivia`**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **PartidaTrivia** | Agregado raíz | `PartidaId`, `Nombre`, `EstadoPartida`, `Modalidad`, `FormularioTriviaId`, `Participantes`, `PreguntaActualId`, `TiempoInicio` | `PublicarLobby()`, `IniciarPartida()`, `RegistrarRespuestaDefinitiva()`, `AcumularPuntaje()`, `CancelarPartida()`, `FinalizarPartida()` | Usa un `FormularioTrivia`. Contiene participantes activos. Genera eventos e historial. |
| **Participante** | Entidad hija | `ParticipanteId`, `PuntajeAcumulado`, `TiempoRespuestaAcumulado` | `AcumularPuntaje()`, `AcumularTiempoRespuesta()` | Representa un competidor activo. Su `Id` puede ser `UsuarioId` o `EquipoId` según modalidad. |
| **RespuestaTrivia** | Entidad hija | `RespuestaId`, `ParticipanteId`, `PreguntaId`, `OpcionSeleccionada`, `EsCorrecta`, `TiempoEmpleado` | `ValidarContraPregunta()` | Pertenece a `PartidaTrivia`. |
| **PartidaId** | Value Object | `Valor` | — | Identificador de partida. |
| **Modalidad** | Enum | `Individual`, `Equipos` | — | Define si compite usuario o equipo. |
| **EstadoPartida** | Enum | `Lobby`, `Iniciada`, `Cancelada`, `Terminada` | — | Estado común de la partida. |

Ajuste aplicado: para Trivia usé la regla de acumulación directa del modelo de dominio, confirmada por ti: `AcumularPuntaje(participanteId, preguntaId)` toma el `PuntajeAsignado` de la pregunta y lo suma al `PuntajeAcumulado` del participante.

---

## **4\. Contexto de Búsqueda del Tesoro**

`Umbral.Bdt.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **PartidaBDT** | Agregado raíz | `PartidaId`, `Nombre`, `EstadoPartida`, `Modalidad`, `AreaBusqueda`, `Etapas`, `Participantes`, `IndiceEtapaActual` | `PublicarLobby()`, `IniciarPartida()`, `ValidarHito()`, `AvanzarEtapa()`, `DespacharPista()`, `CancelarPartida()`, `FinalizarPartida()` | Contiene etapas y participantes activos. |
| **EtapaBDT** | Entidad hija | `EtapaId`, `Orden`, `CodigoQREsperado`, `TiempoLimite`, `PuntajeEtapa`, `EstadoEtapa` | `Activar()`, `Resolver()`, `Cerrar()` | Pertenece a `PartidaBDT`. |
| **Participante** | Entidad hija | `ParticipanteId`, `PuntajeAcumulado`, `UbicacionActual` | `ActualizarUbicacion()`, `AcumularPuntaje()` | Representa al explorador. Su `Id` puede ser `UsuarioId` o `EquipoId`. |
| **TesoroQR** | Entidad hija | `TesoroId`, `EtapaId`, `ParticipanteId`, `ImagenUrl`, `QrDecodificado`, `ResultadoValidacion`, `FechaEnvio` | `MarcarValido()`, `MarcarInvalido()` | Se registra cuando un participante sube un QR. |
| **Pista** | Entidad hija | `PistaId`, `Texto`, `DestinatarioId`, `FechaEnvio` | `Despachar()` | Enviada a un participante o equipo. |
| **AreaBusqueda** | Value Object | `Descripcion` | — | Representa el área textual de búsqueda definida por el operador. |
| **UbicacionGeografica** | Value Object | `Latitud`, `Longitud`, `FechaRegistro` | — | Representa la ubicación actual del participante. |
| **CodigoQREsperado** | Value Object | `Valor` | `CoincideCon(qrDecodificado)` | QR esperado por etapa. |
| **PuntajeEtapa** | Value Object | `Valor` | — | Puntaje otorgado al resolver una etapa. |
| **EstadoEtapa** | Enum | `Pendiente`, `Activa`, `Resuelta`, `Cerrada` | — | Estado interno de una etapa BDT. |
| **ResultadoValidacionQR** | Enum | `Valido`, `Invalido`, `NoLegible`, `NoCorrespondeEtapaActiva` | — | Resultado del envío del tesoro. |

Elegí `AreaBusqueda` y `UbicacionGeografica` en vez de `UbicacionSecreta` porque el SRS habla explícitamente de **área de búsqueda** y de **geolocalización del participante**, mientras que el modelo de dominio mencionaba `UbicacionSecreta` como value object. Esto no cambia una regla de negocio; solo ajusta el nombre al vocabulario más claro del SRS.

---

## **5\. Clases transversales de inscripción y convocatoria**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **InscripcionPartida** | Entidad | `InscripcionId`, `PartidaId`, `ParticipanteId`, `EquipoId`, `Modalidad`, `EstadoInscripcion`, `FechaSolicitud` | `Aceptar()`, `Rechazar()`, `Cancelar()` | Se asocia a una partida Trivia o BDT. Puede ser individual o por equipo. |
| **Convocatoria** | Entidad | `ConvocatoriaId`, `PartidaId`, `EquipoId`, `UsuarioId`, `EstadoConvocatoria`, `FechaEnvio`, `FechaRespuesta` | `Aceptar()`, `Rechazar()` | Se genera cuando un líder inscribe un equipo en una partida por equipos. |
| **EstadoInscripcion** | Enum | `Pendiente`, `Aceptada`, `Rechazada`, `Cancelada` | — | Usado por `InscripcionPartida`. |
| **EstadoConvocatoria** | Enum | `Pendiente`, `Aceptada`, `Rechazada` | — | Usado por `Convocatoria`. |

El SRS exige inscripción en lobby, convocatoria a integrantes cuando el líder inscribe un equipo y registro de aceptación o rechazo.

---

## **6\. Contexto de Auditoría**

`Umbral.Auditoria.Domain`

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| **RegistroAuditoria** | Agregado raíz | `RegistroAuditoriaId`, `PartidaId`, `Eventos` | `AgregarEvento()` | Agrupa los eventos históricos de una partida. |
| **EventoHistorial** | Entidad hija | `EventoHistorialId`, `TipoEvento`, `ActorId`, `Descripcion`, `FechaOcurrencia`, `Datos` | `Crear()` | Pertenece a `RegistroAuditoria`. |
| **TipoEventoHistorial** | Enum | `CambioEstado`, `Inscripcion`, `Convocatoria`, `RespuestaTrivia`, `TesoroSubido`, `ValidacionQR`, `PistaEnviada`, `Ubicacion`, `Puntaje`, `Cancelacion`, `Resultado` | — | Clasifica eventos del historial. |

Aquí separé responsabilidades como pediste: `RegistroAuditoria` es el contenedor/agregado del historial, mientras que `EventoHistorial` representa cada hecho registrado. El SRS exige registrar cambios de estado, inscripciones, convocatorias, respuestas, tesoros, validaciones, pistas, puntajes, ubicaciones, cancelaciones y resultados.

---

## **7\. Eventos de dominio**

| Clase / Evento | Tipo | Datos principales | Cuándo ocurre |
| ----- | ----- | ----- | ----- |
| **RespuestaTriviaValidada** | Evento de dominio | `PartidaId`, `ParticipanteId`, `PreguntaId`, `EsCorrecta`, `TiempoEmpleado` | Al validar una respuesta de Trivia. |
| **PuntajeTriviaIncrementado** | Evento de dominio | `PartidaId`, `ParticipanteId`, `PuntajeAcumulado` | Al sumar puntaje en Trivia. |
| **HitoBDTEncontrado** | Evento de dominio | `PartidaId`, `ParticipanteId`, `EtapaId`, `QrDecodificado` | Al validar correctamente un QR. |
| **PuntajeBDTIncrementado** | Evento de dominio | `PartidaId`, `ParticipanteId`, `PuntajeAcumulado` | Al sumar el puntaje de una etapa BDT. |
| **PartidaTriviaFinalizada** | Evento de dominio | `PartidaId`, `Participantes`, `PuntajesFinales` | Al finalizar una Trivia. |
| **PartidaBDTFinalizada** | Evento de dominio | `PartidaId`, `Participantes`, `PuntajesFinales` | Al finalizar una BDT. |
| **PartidaCancelada** | Evento de dominio | `PartidaId`, `OperadorId`, `FechaCancelacion` | Al cancelar una partida. |
| **ConvocatoriaRespondida** | Evento de dominio | `ConvocatoriaId`, `UsuarioId`, `EstadoConvocatoria` | Al aceptar o rechazar convocatoria. |

Estos eventos vienen directamente del modelo de dominio, que enumera eventos como `RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `HitoBDTEncontrado`, `PuntajeBDTIncrementado` y eventos de finalización de partida.

---

## **8\. Servicios de dominio**

| Servicio | Tipo | Responsabilidad | Usa |
| ----- | ----- | ----- | ----- |
| **ClasificadorRankingService** | Servicio de dominio | Ordenar participantes y generar podio final según `PuntajeAcumulado` y, en Trivia, `TiempoRespuestaAcumulado`. | `Trivias.Participante`, `Bdt.Participante` |
| **ValidadorFormularioTriviaService** | Servicio de dominio | Validar que el formulario tenga preguntas completas, opciones, respuesta correcta, puntaje y tiempo. | `FormularioTrivia` |
| **ValidadorInscripcionService** | Servicio de dominio | Validar modalidad, estado de partida, cupo, liderazgo y equipo activo antes de inscribir. | `InscripcionPartida`, `Equipo`, `PartidaTrivia`, `PartidaBDT` |
| **ValidadorQRService** | Servicio de dominio | Comparar QR decodificado contra `CodigoQREsperado` de la etapa activa. | `PartidaBDT`, `EtapaBDT`, `TesoroQR` |

El modelo de dominio define explícitamente `ClasificadorRankingService` como servicio independiente en Trivia y BDT, encargado de recibir participantes, leer puntajes acumulados y entregar el podio final.

---

# **Relaciones principales del diagrama**

| Relación | Cardinalidad | Descripción |
| ----- | ----- | ----- |
| `Usuario` — `Equipos.Participante` | `1` — `0..1` | Un usuario participante puede pertenecer a un equipo como máximo. |
| `Equipo` — `Equipos.Participante` | `1` — `1..5` | Un equipo contiene de 1 a 5 integrantes. |
| `Equipo` — `CodigoAcceso` | `1` — `1` | Cada equipo tiene un código de acceso. |
| `FormularioTrivia` — `Pregunta` | `1` — `1..*` | Un formulario contiene preguntas. |
| `Pregunta` — `Opcion` | `1` — `2..*` | Una pregunta tiene opciones de respuesta. |
| `PartidaTrivia` — `FormularioTrivia` | `1` — `1` | Una partida de Trivia se basa en un formulario válido. |
| `PartidaTrivia` — `Trivias.Participante` | `1` — `1..*` | Una partida de Trivia tiene competidores activos. |
| `PartidaTrivia` — `RespuestaTrivia` | `1` — `0..*` | Una partida registra respuestas. |
| `PartidaBDT` — `EtapaBDT` | `1` — `1..*` | Una BDT tiene una o más etapas. |
| `PartidaBDT` — `Bdt.Participante` | `1` — `1..*` | Una BDT tiene exploradores activos. |
| `EtapaBDT` — `TesoroQR` | `1` — `0..*` | Una etapa puede recibir varios envíos de QR. |
| `PartidaBDT` — `Pista` | `1` — `0..*` | El operador puede enviar pistas durante la partida. |
| `PartidaTrivia / PartidaBDT` — `InscripcionPartida` | `1` — `0..*` | Una partida puede tener múltiples inscripciones. |
| `InscripcionPartida` — `Convocatoria` | `1` — `0..*` | Una inscripción por equipo puede generar convocatorias. |
| `RegistroAuditoria` — `EventoHistorial` | `1` — `0..*` | Un registro agrupa eventos históricos. |

