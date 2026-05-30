## **1\. Actores del dominio**

| Actor | Descripción | Responsabilidades principales |
| ----- | ----- | ----- |
| Administrador | Usuario responsable de la configuración administrativa general y gestión de accesos/equipos. | Crear usuarios mediante Keycloak, asignar rol inicial, consultar/editar/desactivar usuarios, crear/consultar/editar/desactivar/eliminar equipos y consultar información operativa en modo lectura. |
| Operador | Usuario responsable de preparar, publicar, iniciar, supervisar y cancelar partidas. | Crear formularios de Trivia, crear partidas Trivia/BDT, configurar etapas, publicar lobbies, iniciar partidas, cancelar partidas, visualizar ranking, enviar pistas, consultar tesoros, consultar geolocalización e historial. |
| Participante | Usuario autenticado que participa desde la aplicación móvil. | Ver partidas publicadas, crear/unirse/salir de equipos, actuar como líder cuando aplique, inscribirse en partidas, aceptar/rechazar convocatorias, responder Trivia, subir tesoros QR, compartir geolocalización en BDT y consultar historial. |
| Líder de equipo | Condición de negocio de un participante dentro de un equipo. No es rol Keycloak. | Inscribir/preinscribir equipo en partidas por equipo, transferir liderazgo, eliminar equipo si cumple reglas y convocar integrantes indirectamente al inscribir equipo. |
| Sistema | Actor lógico para procesos automáticos. | Validar respuestas, validar QR, actualizar ranking, cerrar preguntas/etapas, cancelar automáticamente partidas sin mínimos, registrar eventos y publicar actualizaciones en tiempo real. |

---

## **2\. Conceptos principales del dominio**

| Concepto | Descripción |
| ----- | ----- |
| Usuario | Representación local de un usuario autenticado por Keycloak. |
| Equipo | Agrupación global de participantes, válida para Trivia y BDT. Puede existir con 1 a 5 integrantes. |
| Código de acceso | Código único que permite unirse a un equipo. |
| FormularioTrivia | Plantilla de preguntas usada para crear partidas de Trivia. |
| Pregunta | Elemento del formulario con texto, opciones, respuesta correcta, puntaje asignado y tiempo límite. |
| Opción | Posible respuesta de una pregunta. |
| Partida | Juego publicado o ejecutado bajo uno de los dos modos permitidos: Trivia o Búsqueda del Tesoro. |
| PartidaTrivia | Partida basada en un formulario de Trivia. |
| PartidaBDT | Partida de Búsqueda del Tesoro basada en etapas y códigos QR. |
| Inscripción | Registro de intención o confirmación de participación en una partida. |
| Preinscripción | Estado de un equipo inscrito por su líder antes de confirmar que cumple los mínimos de integrantes aceptados. |
| Convocatoria | Invitación enviada a integrantes de un equipo cuando el líder preinscribe el equipo en una partida por equipos. |
| Participante activo | En partidas por equipo, solo el integrante que aceptó la convocatoria. |
| RespuestaTrivia | Respuesta enviada por un jugador o equipo ante una pregunta activa. |
| TesoroQR | Envío de imagen realizado por el participante para validar un QR encontrado. |
| Código QR esperado | Contenido textual esperado del QR configurado para una etapa BDT. |
| Área de búsqueda | Descripción textual simple del área donde se desarrolla una BDT. |
| Ubicación geográfica | Latitud/longitud enviada por participantes durante BDT iniciada. |
| Pista | Mensaje enviado por el operador a participante/equipo durante BDT. |
| Ranking Trivia | Clasificación por puntaje acumulado y desempate por menor tiempo acumulado de respuesta. |
| Ranking BDT | Clasificación por etapas ganadas y desempate por menor tiempo acumulado únicamente de etapas ganadas. |
| Registro de auditoría | Contenedor de eventos históricos relevantes de una partida. |
| EventoHistorial | Hecho registrado: inscripción, convocatoria, respuesta, validación, pista, ubicación, puntaje, cancelación o resultado. |

---

## **3\. Subdominios**

| Subdominio | Tipo | Responsabilidad |
| ----- | ----- | ----- |
| Trivia | Core | Gestionar formularios, preguntas sincronizadas, respuestas únicas, cierre de pregunta, puntaje directo y ranking. |
| Búsqueda del Tesoro | Core | Gestionar etapas, QR esperado, subida de tesoros, validación automática, geolocalización, pistas, cierre de etapas y ranking BDT. |
| Gestión de Equipos | Soporte | Gestionar creación, membresía, liderazgo, código de acceso, eliminación/desactivación y reglas de equipo. |
| Inscripciones y Convocatorias | Soporte | Gestionar preinscripción de equipos, confirmación por mínimos, aceptación/rechazo de convocatorias y participantes activos. |
| Auditoría e Historial | Soporte | Registrar eventos relevantes y conservar trazabilidad operativa. |
| Identidad y Acceso | Genérico | Integración con Keycloak, roles base y referencia local de usuarios. |

---

## **4\. Contextos acotados**

Nota: estos contextos son **lógicos/de dominio**. No implican crear microservicios físicos adicionales. Se mantiene la decisión arquitectónica de microservicios físicos acordada previamente.

| Contexto | Namespace sugerido | Responsabilidad |
| ----- | ----- | ----- |
| Identidad | `Umbral.Identity.Domain` | Usuarios locales, roles base, estado de usuario y referencia Keycloak. |
| Equipos | `Umbral.Equipos.Domain` | Equipo, participantes de equipo, liderazgo, código de acceso y estado de equipo. |
| Participación | `Umbral.Participacion.Domain` | Inscripciones y convocatorias transversales para Trivia y BDT. |
| Trivia | `Umbral.Trivias.Domain` | Formularios, preguntas, partidas Trivia, respuestas, puntaje y ranking Trivia. |
| Búsqueda del Tesoro | `Umbral.Bdt.Domain` | Partidas BDT, etapas, tesoros QR, geolocalización, pistas y ranking BDT. |
| Auditoría | `Umbral.Auditoria.Domain` | Registro de eventos históricos y trazabilidad. |

---

# **5\. Agregados e invariantes**

## **A. Contexto de Identidad**

### **Agregado raíz: `Usuario`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `Usuario` |
| Value Objects | `UsuarioId`, `KeycloakId`, `Correo` |
| Enums | `RolUsuario`, `EstadoUsuario` |
| Invariantes | UMBRAL no almacena contraseñas. El rol base se asigna al crear usuario y no se modifica desde UMBRAL. Un usuario desactivado no puede ejecutar acciones dentro del sistema. |
| Relación con SRS | El SRS establece integración con Keycloak, almacenamiento de referencia local y roles base Administrador/Operador/Participante. |

---

## **B. Contexto de Equipos**

### **Agregado raíz: `Equipo`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `Equipo` |
| Entidades hijas | `ParticipanteEquipo` |
| Value Objects | `EquipoId`, `NombreEquipo`, `CodigoAcceso` |
| Enums | `EstadoEquipo` |
| Invariantes | Un equipo puede existir con mínimo 1 integrante y máximo 5 integrantes. El creador cuenta como primer integrante y queda como líder. Un usuario solo puede pertenecer a un equipo activo a la vez. El equipo tiene un código único de acceso. |
| Reglas de eliminación | El líder puede eliminar un equipo aunque tenga integrantes, pero no si el equipo está inscrito en una partida en `lobby` o participando en una partida `iniciada`. La eliminación conserva el historial. |
| Reglas administrativas | El administrador puede crear, consultar, editar, desactivar y eliminar equipos respetando las invariantes del dominio. |
| Relación con SRS | Corrige la versión anterior del modelo, que indicaba `≥2 y ≤5`; el SRS actual trabaja con equipo de 1 a 5 integrantes y equipos globales para Trivia/BDT. |

---

## **C. Contexto de Participación**

### **Agregado raíz: `InscripcionPartida`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `InscripcionPartida` |
| Entidades hijas | `Convocatoria` |
| Value Objects | `InscripcionId`, `ConvocatoriaId`, `PartidaId`, `UsuarioId`, `EquipoId` |
| Enums | `EstadoInscripcion`, `EstadoConvocatoria`, `TipoPartida`, `Modalidad` |
| Invariantes | En partidas individuales, la inscripción corresponde a un usuario. En partidas por equipo, el líder preinscribe el equipo y el sistema genera convocatorias para sus integrantes. |
| Regla de confirmación | La preinscripción del equipo se confirma al iniciar la partida solo si cumple el mínimo de jugadores aceptados configurado por el operador. |
| Participantes activos | En partidas por equipo, solo los integrantes que aceptan convocatoria cuentan como participantes activos. |
| Rechazo de convocatoria | Rechazar una convocatoria no elimina al usuario del equipo; solo lo excluye de esa partida. |
| Relación con SRS | El SRS exige inscripción en lobby, convocatoria a integrantes, registro de aceptación/rechazo y cálculo de mínimos sobre jugadores aceptados. |

---

## **D. Contexto de Trivia**

### **Agregado raíz 1: `FormularioTrivia`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `FormularioTrivia` |
| Entidades hijas | `Pregunta` |
| Value Objects | `FormularioId`, `TituloFormulario`, `Opcion`, `PuntajeAsignado`, `TiempoLimite` |
| Invariantes | Un formulario debe tener al menos una pregunta. Cada pregunta debe tener opciones, una respuesta correcta, puntaje asignado y tiempo límite. |
| Relación con SRS | El SRS exige que los formularios de Trivia tengan preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta. |

### **Agregado raíz 2: `PartidaTrivia`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `PartidaTrivia` |
| Entidades hijas | `CompetidorTrivia`, `RespuestaTrivia` |
| Value Objects | `PartidaId`, `NombrePartida`, `TiempoInicio`, `TiempoRespuesta` |
| Enums | `EstadoPartida`, `Modalidad` |
| Invariantes de modalidad | Si es individual, el competidor representa un `UsuarioId`. Si es por equipos, el competidor representa un `EquipoId`. |
| Regla de respuesta | En individual, una respuesta por jugador por pregunta. En equipos, una respuesta por equipo por pregunta, siendo válida la primera opción enviada por cualquier integrante activo. |
| Regla de cierre | La pregunta se cierra para todos cuando un jugador/equipo responde correctamente o cuando se agota el tiempo. |
| Regla de puntaje | Respuesta correcta suma directamente el `PuntajeAsignado` de la pregunta. El tiempo no modifica el puntaje. |
| Regla de desempate | Si hay empate en puntaje, se ordena por menor tiempo acumulado de respuesta. |
| Regla de inicio automático | Si llega la hora de inicio automático y no se cumplen mínimos, la partida se cancela automáticamente. |
| Regla de cancelación | Solo puede cancelarse en `lobby` o `iniciada`. |
| Relación con SRS | El SRS actual define respuesta única, cierre por respuesta correcta o tiempo, puntaje directo sin ponderación y desempate por tiempo acumulado. |

---

## **E. Contexto de Búsqueda del Tesoro**

### **Agregado raíz: `PartidaBDT`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `PartidaBDT` |
| Entidades hijas | `EtapaBDT`, `ExploradorBDT`, `TesoroQR`, `Pista` |
| Value Objects | `PartidaId`, `AreaBusqueda`, `CodigoQREsperado`, `UbicacionGeografica`, `TiempoLimite`, `TiempoResolucionEtapa` |
| Enums | `EstadoPartida`, `EstadoEtapa`, `Modalidad`, `ResultadoValidacionQR` |
| Invariantes de partida | Toda BDT debe tener una o más etapas válidas. Cada etapa debe tener contenido textual esperado del QR y tiempo límite. |
| Área de búsqueda | Texto descriptivo simple. No se modela como coordenadas ni polígono geográfico en esta versión. |
| QR esperado | Se almacena como contenido textual esperado del QR. La imagen subida por el participante se procesa para decodificar su contenido. |
| Intentos de tesoro | Un jugador/equipo puede hacer múltiples intentos en una etapa hasta validar correctamente o hasta que la etapa cierre. |
| Cierre de etapa | La etapa se cierra inmediatamente para todos cuando un jugador/equipo valida correctamente el QR o cuando vence el tiempo. |
| BDT por equipos | Si un integrante activo sube el QR correcto, la etapa se considera ganada por todo el equipo. |
| Ranking BDT | Se ordena por cantidad de etapas ganadas. En empate, por menor tiempo acumulado únicamente de etapas ganadas. |
| Geolocalización | Es obligatoria para participar en BDT iniciada. El participante debe autorizar ubicación desde la app móvil. |
| Inicio | Puede ser manual, automático o ambos, según configuración del operador. |
| Relación con SRS | El SRS define área textual, QR esperado como contenido, cierre por validación o tiempo, geolocalización obligatoria y ranking visible por etapas ganadas. |

---

## **F. Contexto de Auditoría**

### **Agregado raíz: `RegistroAuditoria`**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | `RegistroAuditoria` |
| Entidades hijas | `EventoHistorial` |
| Enums | `TipoEventoHistorial` |
| Invariantes | El historial se conserva aunque una partida sea cancelada o un equipo sea eliminado. Una partida cancelada conserva eventos, puntajes/resultados parciales e historial, pero no cuenta como resultado final. |
| Eventos registrados | Cambios de estado, inscripciones, convocatorias, respuestas, tesoros subidos, validaciones QR, pistas, ubicaciones, variaciones de ranking/puntaje, cancelaciones y resultados. |
| Relación con SRS | El SRS exige trazabilidad de eventos, historial y conservación de eventos relevantes. |

---

# **6\. Eventos de dominio actualizados**

| Evento | Datos principales | Cuándo ocurre |
| ----- | ----- | ----- |
| `UsuarioCreado` | `UsuarioId`, `KeycloakId`, `Rol` | Cuando se crea un usuario desde UMBRAL mediante Keycloak. |
| `UsuarioDesactivado` | `UsuarioId` | Cuando el administrador desactiva un usuario. |
| `EquipoCreado` | `EquipoId`, `LiderId`, `CodigoAcceso` | Cuando un participante o administrador crea un equipo. |
| `ParticipanteAgregadoAEquipo` | `EquipoId`, `UsuarioId` | Cuando un participante se une por código válido. |
| `LiderazgoTransferido` | `EquipoId`, `LiderAnteriorId`, `NuevoLiderId` | Cuando se transfiere liderazgo. |
| `EquipoEliminado` | `EquipoId`, `ActorId` | Cuando el líder o administrador elimina un equipo permitido. |
| `EquipoDesactivado` | `EquipoId`, `AdministradorId` | Cuando el administrador desactiva un equipo. |
| `EquipoPreinscritoEnPartida` | `InscripcionId`, `PartidaId`, `EquipoId` | Cuando el líder preinscribe un equipo. |
| `ConvocatoriaCreada` | `ConvocatoriaId`, `PartidaId`, `EquipoId`, `UsuarioId` | Cuando se convoca a un integrante. |
| `ConvocatoriaRespondida` | `ConvocatoriaId`, `UsuarioId`, `EstadoConvocatoria` | Cuando un integrante acepta o rechaza. |
| `InscripcionConfirmada` | `InscripcionId`, `PartidaId` | Cuando se confirma que cumple mínimos al iniciar. |
| `PartidaPublicadaEnLobby` | `PartidaId`, `TipoPartida` | Cuando el operador publica un lobby. |
| `PartidaIniciada` | `PartidaId`, `TipoPartida` | Cuando la partida inicia cumpliendo mínimos. |
| `PartidaCancelada` | `PartidaId`, `OperadorId`, `Motivo` | Cuando el operador cancela o cuando el sistema cancela por mínimos no cumplidos. |
| `RespuestaTriviaRegistrada` | `PartidaId`, `CompetidorId`, `PreguntaId`, `OpcionSeleccionada` | Cuando se recibe respuesta válida. |
| `RespuestaTriviaValidada` | `PartidaId`, `CompetidorId`, `PreguntaId`, `EsCorrecta`, `TiempoEmpleado` | Al validar la respuesta. |
| `PuntajeTriviaIncrementado` | `PartidaId`, `CompetidorId`, `PuntajeAcumulado` | Cuando una respuesta correcta suma puntaje directo. |
| `PreguntaTriviaCerrada` | `PartidaId`, `PreguntaId`, `RespuestaCorrecta` | Cuando alguien acierta o vence el tiempo. |
| `RankingTriviaActualizado` | `PartidaId`, `Ranking` | Cuando cambia el ranking por respuesta correcta o desempate. |
| `TesoroQRSubido` | `PartidaId`, `EtapaId`, `ExploradorId`, `ImagenUrl` | Cuando el participante sube imagen QR. |
| `TesoroQRValidado` | `PartidaId`, `EtapaId`, `ExploradorId`, `ResultadoValidacion` | Cuando se compara QR decodificado con QR esperado. |
| `EtapaBDTGanada` | `PartidaId`, `EtapaId`, `ExploradorId`, `TiempoResolucion` | Cuando un jugador/equipo valida correctamente el QR. |
| `EtapaBDTCerrada` | `PartidaId`, `EtapaId`, `MotivoCierre` | Cuando la etapa se cierra por ganador o por tiempo. |
| `RankingBDTActualizado` | `PartidaId`, `Ranking` | Cuando cambia ranking por etapas ganadas o desempate. |
| `PistaEnviada` | `PartidaId`, `DestinatarioId`, `Texto` | Cuando el operador envía pista. |
| `UbicacionParticipanteActualizada` | `PartidaId`, `ExploradorId`, `UbicacionGeografica` | Cuando llega ubicación autorizada en BDT. |
| `PartidaFinalizada` | `PartidaId`, `TipoPartida`, `ResultadoFinal` | Cuando termina la última pregunta o etapa. |

---

# **7\. Servicios de dominio actualizados**

| Servicio | Responsabilidad | Usa |
| ----- | ----- | ----- |
| `ValidadorFormularioTriviaService` | Validar que un formulario tenga preguntas completas, opciones, respuesta correcta, puntaje y tiempo. | `FormularioTrivia`, `Pregunta` |
| `ValidadorInscripcionService` | Validar estado de partida, modalidad, cupo, liderazgo, equipo activo, mínimos y preinscripción. | `InscripcionPartida`, `Equipo`, `PartidaTrivia`, `PartidaBDT` |
| `ValidadorConvocatoriaService` | Determinar participantes activos según convocatorias aceptadas. | `Convocatoria`, `InscripcionPartida` |
| `CalculadorRankingTriviaService` | Ordenar por puntaje acumulado descendente y desempatar por menor tiempo acumulado de respuesta. | `CompetidorTrivia` |
| `ValidadorRespuestaTriviaService` | Validar opción seleccionada contra respuesta correcta y reglas de respuesta única. | `Pregunta`, `RespuestaTrivia`, `PartidaTrivia` |
| `ValidadorQRService` | Comparar QR decodificado contra contenido textual esperado de la etapa activa. | `CodigoQREsperado`, `TesoroQR`, `EtapaBDT` |
| `CalculadorRankingBDTService` | Ordenar por etapas ganadas y desempatar por menor tiempo acumulado de etapas ganadas. | `ExploradorBDT` |
| `ValidadorGeolocalizacionBDTService` | Validar que el participante haya autorizado geolocalización para participar en BDT iniciada. | `ExploradorBDT`, `UbicacionGeografica` |
| `ValidadorEliminacionEquipoService` | Validar que un equipo no esté en lobby ni en partida iniciada antes de eliminarse. | `Equipo`, `InscripcionPartida` |
| `ValidadorTransicionEstadoPartidaService` | Validar transiciones entre `lobby`, `iniciada`, `cancelada` y `terminada`. | `EstadoPartida` |

