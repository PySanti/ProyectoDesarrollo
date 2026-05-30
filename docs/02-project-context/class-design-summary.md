# Class Design Summary — UMBRAL

Este resumen traduce el diagrama de clases del project-source en una guía operativa para implementación.

## Contexto de Identidad

| Clase | Tipo | Responsabilidad |
|---|---|---|
| Usuario | Entidad / Agregado raíz | Representa un usuario local vinculado a Keycloak. |
| RolUsuario | Enum | Administrador, Operador, Participante. |
| EstadoUsuario | Enum | Activo, Desactivado. |
| KeycloakId | Value Object | Referencia externa al usuario autenticado. |

### Reglas relevantes

- UMBRAL no almacena contraseñas.
- El rol se asigna inicialmente.
- El usuario puede editar datos generales o ser desactivado.

## Contexto de Equipos

| Clase | Tipo | Responsabilidad |
|---|---|---|
| Equipo | Agregado raíz | Controla integrantes, código, liderazgo y estado. |
| Participante | Entidad hija | Miembro de equipo, con `UsuarioId`, fecha y `EsLider`. |
| EquipoId | Value Object | Identificador del equipo. |
| NombreEquipo | Value Object | Nombre validable del equipo. |
| CodigoAcceso | Value Object | Código generado y comparado para unirse. |
| EstadoEquipo | Enum | Activo, Desactivado, Eliminado. |

### Métodos principales

- `Crear()`
- `AgregarParticipante()`
- `RemoverParticipante()`
- `TransferirLiderazgo()`
- `Eliminar()`
- `Desactivar()`

## Contexto de Trivia

### FormularioTrivia

| Clase | Tipo | Responsabilidad |
|---|---|---|
| FormularioTrivia | Agregado raíz | Plantilla de preguntas creada por operador. |
| Pregunta | Entidad hija | Pregunta con opciones, puntaje y tiempo. |
| Opcion | Value Object | Texto y bandera de respuesta correcta. |
| PuntajeAsignado | Value Object | Puntaje específico de pregunta. |
| TiempoLimite | Value Object | Segundos para responder. |

### PartidaTrivia

| Clase | Tipo | Responsabilidad |
|---|---|---|
| PartidaTrivia | Agregado raíz | Controla ciclo de vida, participantes, pregunta actual y respuestas. |
| Participante | Entidad hija | Competidor activo; representa usuario o equipo. |
| RespuestaTrivia | Entidad hija | Respuesta enviada por participante/equipo. |
| PartidaId | Value Object | Identificador de partida. |
| Modalidad | Enum | Individual o Equipos. |
| EstadoPartida | Enum | Lobby, Iniciada, Cancelada, Terminada. |

### Métodos principales

- `PublicarLobby()`
- `IniciarPartida()`
- `RegistrarRespuestaDefinitiva()`
- `AcumularPuntaje()`
- `CancelarPartida()`
- `FinalizarPartida()`

## Contexto de Búsqueda del Tesoro

| Clase | Tipo | Responsabilidad |
|---|---|---|
| PartidaBDT | Agregado raíz | Controla etapas, participantes, QR, pistas y avance. |
| EtapaBDT | Entidad hija | Etapa con orden, QR esperado, tiempo y puntaje. |
| Participante | Entidad hija | Explorador activo, usuario/equipo, puntaje y ubicación. |
| TesoroQR | Entidad hija | Envío de imagen QR y resultado de validación. |
| Pista | Entidad hija | Pista enviada por operador. |
| AreaBusqueda | Value Object | Descripción textual del área. |
| UbicacionGeografica | Value Object | Latitud, longitud y fecha. |
| CodigoQREsperado | Value Object | Código QR esperado y comparación. |
| PuntajeEtapa | Value Object | Puntaje al resolver etapa. |
| EstadoEtapa | Enum | Pendiente, Activa, Resuelta, Cerrada. |
| ResultadoValidacionQR | Enum | Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva. |

### Métodos principales

- `PublicarLobby()`
- `IniciarPartida()`
- `ValidarHito()`
- `AvanzarEtapa()`
- `DespacharPista()`
- `CancelarPartida()`
- `FinalizarPartida()`

## Clases transversales de inscripción

| Clase | Tipo | Responsabilidad |
|---|---|---|
| InscripcionPartida | Entidad | Representa solicitud o registro de participación individual/equipo. |
| Convocatoria | Entidad | Invitación a integrantes de equipo. |
| EstadoInscripcion | Enum | Pendiente, Aceptada, Rechazada, Cancelada. |
| EstadoConvocatoria | Enum | Pendiente, Aceptada, Rechazada. |

## Contexto de Auditoría

| Clase | Tipo | Responsabilidad |
|---|---|---|
| RegistroAuditoria | Agregado raíz | Agrupa eventos históricos de una partida. |
| EventoHistorial | Entidad hija | Representa un evento ocurrido. |
| TipoEventoHistorial | Enum | Clasifica cambios, inscripciones, respuestas, tesoros, validaciones, pistas, ubicación, puntaje, cancelación y resultado. |

## Relaciones principales

| Relación | Cardinalidad / regla |
|---|---|
| Usuario — Equipo.Participante | Un usuario puede pertenecer como máximo a un equipo. |
| Equipo — Participante | Un equipo contiene de 1 a 5 integrantes según el diagrama; revisar ambigüedad del modelo inicial. |
| FormularioTrivia — Pregunta | Un formulario tiene una o más preguntas. |
| Pregunta — Opcion | Una pregunta tiene dos o más opciones y al menos una respuesta correcta. |
| PartidaTrivia — FormularioTrivia | Una partida usa un formulario válido. |
| PartidaTrivia — Participante | Una partida tiene participantes activos. |
| PartidaTrivia — RespuestaTrivia | Una partida registra respuestas. |
| PartidaBDT — EtapaBDT | Una BDT tiene una o más etapas. |
| EtapaBDT — TesoroQR | Una etapa puede recibir varios envíos. |
| PartidaBDT — Pista | Una partida puede tener pistas enviadas. |
| Partida — InscripcionPartida | Una partida puede tener múltiples inscripciones. |
| InscripcionPartida — Convocatoria | Una inscripción por equipo puede generar convocatorias. |
| RegistroAuditoria — EventoHistorial | Un registro agrupa eventos históricos. |
