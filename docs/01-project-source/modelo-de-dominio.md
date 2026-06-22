## 

## 

## 

## 

## 

## **MODELO DE DOMINIO**

[1\. Actores del dominio	3](#1.-actores-del-dominio)

[2\. Conceptos principales del dominio	4](#2.-conceptos-principales-del-dominio)

[3\. Subdominios	7](#3.-subdominios)

[4\. Contextos acotados	8](#4.-contextos-acotados)

[5\. Agregados e invariantes	10](#5.-agregados-e-invariantes)

[A. Contexto de Identidad	10](#a.-contexto-de-identidad)

[B. Contexto de Equipos	12](#b.-contexto-de-equipos)

[C. Contexto de Participación	14](#c.-contexto-de-participación)

[D. Contexto de Partidas	16](#d.-contexto-de-partidas)

[E. Contexto de Trivia	20](#e.-contexto-de-trivia)

[F. Contexto de Búsqueda del Tesoro	21](#f.-contexto-de-búsqueda-del-tesoro)

[G. Contexto de Auditoría	23](#g.-contexto-de-auditoría)

[6\. Eventos de dominio	24](#6.-eventos-de-dominio)

[7\. Servicios de dominio actualizados	29](#7.-servicios-de-dominio-actualizados)

## 

# **1\. Actores del dominio** {#1.-actores-del-dominio}

| Actor | Descripción | Responsabilidades principales |
| ----- | ----- | ----- |
| Administrador | Usuario responsable de la configuración administrativa general, la gobernanza de permisos por rol y la gestión de accesos/equipos. | Crear usuarios mediante Keycloak, asignar rol inicial, consultar/editar/desactivar usuarios, modificar el rol de operadores y participantes (incluida la promoción a administrador) sin modificar el rol de un administrador, gestionar a nivel de rol los privilegios de gobernanza y los permisos funcionales desde el panel de gobernanza, crear/consultar/editar/desactivar/eliminar equipos y consultar información operativa en modo lectura. |
| Operador | Usuario responsable de preparar, publicar, iniciar, supervisar y cancelar partidas compuestas por uno o más juegos. | Crear partidas compuestas por juegos de Trivia y/o Búsqueda del Tesoro en orden secuencial, crear las preguntas de Trivia al añadir el juego, configurar etapas con su puntaje, publicar partidas, iniciar partidas, cancelar partidas, visualizar el ranking de cada juego y el ranking consolidado, enviar pistas, consultar tesoros, consultar geolocalización e historial. |
| Participante | Usuario autenticado que participa desde la aplicación móvil. | Ver partidas publicadas en un único panel, crear equipos, ver y responder invitaciones de equipo (uniéndose al aceptar), salir de equipos, actuar como líder  (invitando integrantes) si fue el creador del equipo o si el liderazgo fue transferido hacia el, inscribirse en partidas, aceptar/rechazar convocatorias, responder Trivia, subir tesoros QR, compartir geolocalización en BDT y consultar el historial de partidas, el historial de equipos y el rendimiento del equipo. |
| Líder de equipo | Condición de negocio de un participante dentro de un equipo. No es rol Keycloak. | Invitar integrantes mediante una lista dinámica de participantes, inscribir/preinscribir equipo en partidas por equipo, transferir liderazgo, eliminar equipo si cumple reglas y convocar integrantes indirectamente al inscribir equipo. |
| Sistema | Actor lógico para procesos automáticos. | Validar respuestas, validar QR, actualizar los rankings de juego y el ranking consolidado, activar juegos de forma secuencial, cerrar preguntas/etapas, finalizar juegos, cancelar automáticamente partidas sin mínimos, registrar eventos y publicar actualizaciones en tiempo real. |

# **2\. Conceptos principales del dominio** {#2.-conceptos-principales-del-dominio}

| Concepto | Descripción |
| ----- | ----- |
| Usuario | Representación local de un usuario autenticado por Keycloak. |
| Equipo | Agrupación global de participantes, válida para Trivia y BDT. Puede existir con 1 a 5 integrantes. Los integrantes se suman por invitación. |
| InvitacionEquipo |  Invitación a un participante para formar parte de un equipo. La envía el líder mediante una lista dinámica de participantes (que excluye a quienes ya tienen equipo e impide invitar si el equipo está lleno). Si el participante acepta, pasa a ser miembro del equipo. Reemplaza la antigua unión por código. Es lo que un participante ve en “invitaciones recibidas”. |
| Historial de equipos |  Registro, por participante, de los nombres de los equipos a los que ha pertenecido. Se persiste en el contexto de Equipos (microservicio Identity). |
| Pregunta | Pregunta de un juego de Trivia, con texto, opciones, respuesta correcta, puntaje asignado y tiempo límite. Se crea al crear el juego. |
| Opción | Posible respuesta de una pregunta. |
| Partida | Agregado que el operador crea y que contiene uno o más Juegos en orden secuencial. Concentra el ciclo de vida (estados lobby/iniciada/cancelada/terminada), la inscripción a nivel de partida, las convocatorias, la modalidad (individual/equipo) y la cancelación. Tiene una sola modalidad para todos sus juegos. |
| Juego |  Unidad ordenada dentro de una Partida, de tipo Trivia o Búsqueda del Tesoro, con un sub-estado interno propio (Pendiente/Activo/Finalizado). Se activa de forma secuencial al iniciar la partida. |
| JuegoTrivia | Juego de Trivia dentro de una partida, que contiene directamente sus preguntas.  |
| JuegoBDT | Juego de Búsqueda del Tesoro dentro de una partida, basado en etapas y códigos QR. |
| Inscripción | Registro de intención o confirmación de participación en una partida. Se realiza una sola vez a nivel de partida (no por juego); según la modalidad, hay una inscripción por participante (individual) o por equipo (por equipo). |
| Preinscripción | Registro de intención o confirmación de participación en una partida. Se realiza una sola vez a nivel de partida (no por juego); según la modalidad, hay una inscripción por participante (individual) o por equipo (por equipo). |
| Convocatoria | Invitación a un integrante que ya es miembro de un equipo para jugar una partida junto a su equipo, enviada cuando el líder preinscribe el equipo en una partida por equipos. Aceptar o rechazar afecta solo a la participación en esa partida y nunca cambia la pertenencia al equipo. No debe confundirse con la InvitacionEquipo. |
| Participante activo | En partidas por equipo, solo el integrante que aceptó la convocatoria. |
| RespuestaTrivia | Respuesta enviada por un participante o equipo ante una pregunta activa. |
| TesoroQR | Envío de imagen realizado por el participante para validar un QR encontrado. |
| Código QR esperado | Contenido textual esperado del QR configurado para una etapa BDT. |
| Área de búsqueda | Descripción textual simple del área donde se desarrolla una BDT. |
| Ubicación geográfica | Latitud/longitud enviada por participantes durante un juego BDT activo. |
| Pista | Mensaje enviado por el operador a participante/equipo durante un juego BDT. |
| Ranking Trivia | Clasificación nativa del juego de Trivia por puntaje acumulado y desempate por menor tiempo acumulado de respuesta. |
| Ranking BDT | Clasificación nativa del juego de BDT por puntaje acumulado (suma de los puntos de las etapas ganadas) y desempate por menor tiempo acumulado únicamente de las etapas ganadas. |
| Ranking consolidado | Clasificación única de la partida, calculada al finalizar. Ordena a los participantes o equipos por número de juegos ganados; en empate, por el mayor puntaje total acumulado en todos los juegos y, de persistir, por el menor tiempo total. Coexiste con el ranking nativo de cada juego.  |
| Registro de auditoría | Contenedor de eventos históricos relevantes de una partida. |
| EventoHistorial | Hecho registrado: inscripción, convocatoria, invitación de equipo, activación/finalización de juego, respuesta, validación, pista, ubicación, puntaje, ranking consolidado, cancelación o resultado. |
| Rol | Conjunto de privilegios de gobernanza y permisos funcionales asociado a un nombre de rol (Administrador, Operador, Participante). El administrador puede modificar sus permisos/privilegios desde el panel de gobernanza; no se crean roles nuevos. |
| Privilegio de gobernanza | Capacidad de administración del sistema (gestionar usuarios, modificar el rol de un usuario, gestionar permisos por rol, administrar equipos, consultar en modo lectura). Los del rol Administrador están protegidos y no pueden retirarse. |
| Permiso funcional | Permiso que habilita un grupo de acciones de operación o participación: “Gestionar partidas”, “Gestionar equipos” o “Participar en partidas”. Tener el permiso implica todas las acciones que agrupa |

# **3\. Subdominios** {#3.-subdominios}

| Subdominio | Tipo | Responsabilidad |
| ----- | ----- | ----- |
| Partidas y Juegos | Core | Gestionar la creación y estructura de la partida y sus juegos en orden secuencial, el ciclo de vida y estados de la partida, la activación secuencial de juegos con su sub-estado y el ranking consolidado. |
| Trivia | Core | Gestionar las preguntas del juego (creadas al crear el juego), su sincronización, respuestas únicas, cierre de pregunta, puntaje directo y ranking dentro de los juegos de Trivia. |
| Búsqueda del Tesoro | Core | Gestionar etapas, QR esperado, puntaje por etapa, subida de tesoros, validación automática, geolocalización, pistas, cierre de etapas y ranking BDT dentro de los juegos de BDT. |
| Gestión de Equipos | Soporte | Gestionar creación, membresía, liderazgo, invitaciones de equipo, historial de equipos, eliminación/desactivación y reglas de equipo. |
| Inscripciones y Convocatorias | Soporte | Gestionar preinscripción de equipos, confirmación por mínimos, aceptación/rechazo de convocatorias y participantes activos, a nivel de partida. |
| Auditoría e Historial | Soporte | Registrar eventos relevantes y conservar trazabilidad operativa. |
| Identidad, Acceso y Gobernanza | Genérico | Integración con Keycloak, roles base y referencia local de usuarios; administración, a nivel de rol, de permisos y privilegios; y modificación del rol de los usuarios. |

# **4\. Contextos acotados** {#4.-contextos-acotados}

| Contexto | Namespace sugerido | Responsabilidad |
| ----- | ----- | ----- |
| Partidas | Umbral.Partidas.Domain | Partida, Juego, orden secuencial, estados de partida, sub-estados de juego, modalidad y ranking consolidado. |
| Identidad | Umbral.Identity.Domain | Usuarios locales, roles, permisos y privilegios por rol, modificación de rol, estado de usuario y referencia Keycloak. |
| Equipos | Umbral.Equipos.Domain | Equipo, participantes de equipo, liderazgo, invitaciones de equipo, historial de equipos y estado de equipo. |
| Participación | Umbral.Participacion.Domain | Inscripciones y convocatorias a nivel de partida, transversales a sus juegos. |
| Trivia | Umbral.Trivias.Domain | Preguntas, juegos de Trivia, respuestas, puntaje y ranking de Trivia. |
| Búsqueda del Tesoro | Umbral.Bdt.Domain | Juegos de BDT, etapas (con puntaje), tesoros QR, geolocalización, pistas y ranking BDT. |
| Auditoría | Umbral.Auditoria.Domain | Registro de eventos históricos y trazabilidad (capacidad transversal). |

# 

| Contexto lógico (o porción) | Microservicio físico |
| ----- | ----- |
| Identidad | Identity |
| Equipos (incluye Invitaciones de equipo e Historial de equipos) | Identity |
| Partidas (estructura Partida/Juego y configuración) | Partidas |
| Trivia y Búsqueda del Tesoro — configuración (preguntas, etapas, QR, puntaje) | Partidas |
| Trivia y Búsqueda del Tesoro — runtime (sesión en vivo) | Operaciones de sesión |
| Participación (inscripción \+ convocatoria) | Operaciones de sesión |
| Puntaje, rankings nativos y ranking consolidado | Puntuaciones |
| Auditoría (transversal) | Materializada en Puntuaciones y en Operaciones de sesión |

# **5\. Agregados e invariantes** {#5.-agregados-e-invariantes}

## *A. Contexto de Identidad* {#a.-contexto-de-identidad}

**Agregado raíz: Usuario**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | Usuario |
| Value Objects | UsuarioId, KeycloakId, Correo |
| Enums | RolUsuario, EstadoUsuario, EstadoCredencial   |
| Invariantes | UMBRAL no almacena contraseñas. El rol se asigna al crear el usuario y el administrador puede modificarlo después —operadores y participantes, incluida la promoción a administrador—, salvo el rol de un administrador; el cambio se propaga a Keycloak. Un usuario desactivado no puede ejecutar acciones dentro del sistema. |
| Relación con SRS | El SRS establece integración con Keycloak, almacenamiento de referencia local y roles base Administrador/Operador/Participante. |
| Credencial temporal  | Al crear el usuario, su credencial nace en estado temporal pendiente y se emite una contraseña temporal por correo (gestionada en Keycloak, no almacenada en UMBRAL). Si se modifica el correo mientras la credencial siga temporal pendiente, se emite una nueva contraseña temporal al nuevo correo. Cuando el usuario cambia su contraseña (vía Keycloak), la credencial pasa a definitiva.  |

## 

**Agregado raíz: Rol** 

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | Rol |
| Value Objects | RolId, NombreRol |
| Colecciones | PrivilegiosGobernanza, PermisosFuncionales |
| Enums | Privilegio (GestionarUsuarios, ModificarRolDeUsuario, GestionarPermisosDeRol, GestionarEquiposAdministrativamente, ConsultarOperativoModoLectura), PermisoFuncional (GestionarPartidas, GestionarEquipos, ParticiparEnPartidas) |
| Invariantes | Solo existen los roles Administrador, Operador y Participante; no se crean roles nuevos. Los permisos y privilegios se administran a nivel de rol, no por usuario. Los privilegios de gobernanza del rol Administrador están protegidos y no pueden retirarse. |
| Asignación por defecto | Administrador: privilegios de gobernanza. Operador: GestionarPartidas. Participante: GestionarEquipos y ParticiparEnPartidas. El administrador puede reasignar permisos/privilegios desde el panel de gobernanza. |
| Relación con SRS | RF-47 (panel de gobernanza), RF-48 (modificación de rol) y RB-42 a RB-47. |

## 

## 

## *B. Contexto de Equipos* {#b.-contexto-de-equipos}

**Agregado raíz: Equipo**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | Equipo |
| Entidades hijas | ParticipanteEquipo |
| Value Objects | EquipoId, NombreEquipo |
| Enums | EstadoEquipo |
| Invariantes | Un equipo puede existir con mínimo 1 integrante y máximo 5 integrantes. El creador cuenta como primer integrante y queda como líder. Un usuario solo puede pertenecer a un equipo activo a la vez. |
| Reglas de incorporación | Los integrantes se suman mediante invitaciones de equipo enviadas por el líder. |
| Reglas de eliminación | El líder puede eliminar un equipo aunque tenga integrantes, pero no si el equipo está inscrito en una partida en lobby o participando en una partida iniciada. Al eliminarse el equipo se eliminan sus invitaciones pendientes. La eliminación conserva el historial. |
| Reglas administrativas | El administrador puede crear, consultar, editar, desactivar y eliminar equipos respetando las invariantes del dominio. |
| Relación con SRS | Corrige la versión anterior del modelo, que indicaba ≥2 y ≤5; el SRS actual trabaja con equipo de 1 a 5 integrantes y equipos globales para Trivia/BDT. Se elimina el código de acceso y se incorpora la invitación de equipo. |

**Agregado raíz: InvitacionEquipo** 

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | InvitacionEquipo |
| Value Objects | InvitacionEquipoId, EquipoId, UsuarioId (participante invitado) |
| Enums | EstadoInvitacion (Pendiente, Aceptada, Rechazada) |
| Invariantes | Solo el líder puede crear invitaciones. La lista de candidatos es dinámica e incluye a todos los participantes de la plataforma, excluyendo a quienes ya pertenecen a un equipo. No puede crearse una invitación si el equipo está lleno (5 integrantes). Las invitaciones no caducan. |
| Regla de aceptación | Al aceptar, el participante pasa a ser miembro del equipo. Si al aceptar el participante ya pertenece a un equipo, se muestra “Ya perteneces a un equipo” y la invitación permanece pendiente. Si al aceptar el equipo está lleno, se muestra “El equipo ya está lleno” y la invitación permanece pendiente. |
| Regla de eliminación | Al eliminarse un equipo se eliminan todas sus invitaciones pendientes. |
| Visibilidad | Las invitaciones recibidas son visibles para todos los participantes, tengan o no equipo, sean o no líderes. |
| Relación con la Convocatoria | La InvitacionEquipo determina la pertenencia al equipo y es independiente de la Convocatoria, que solo afecta la participación en una partida. |

**Agregado raíz: HistorialEquipoUsuario** 

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | HistorialEquipoUsuario |
| Value Objects | UsuarioId, NombreEquipo |
| Invariantes | Conserva, por participante, los nombres de los equipos a los que ha pertenecido. Solo almacena los nombres. La eliminación de un equipo no borra el historial. |
| Relación con SRS | El SRS exige conservar y permitir consultar el historial de equipos del participante (RF-43). |

## *C. Contexto de Participación* {#c.-contexto-de-participación}

**Agregado raíz: InscripcionPartida**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | InscripcionPartida |
| Entidades hijas | Convocatoria |
| Value Objects | InscripcionId, ConvocatoriaId, PartidaId, UsuarioId, EquipoId |
| Enums | EstadoInscripcion, EstadoConvocatoria, Modalidad |
| Invariantes | En partidas individuales, la inscripción corresponde a un usuario. En partidas por equipo, el líder preinscribe el equipo y el sistema genera convocatorias para sus integrantes. La inscripción se realiza una sola vez a nivel de partida (no por juego); en individual hay una inscripción por participante y en partidas por equipo una por equipo. La modalidad se fija una vez para toda la partida. Además, un equipo no puede tener más de una inscripción activa a la vez, y un participante no puede tener más de una participación activa a la vez, contando como participación activa su inscripción individual activa o una convocatoria de equipo aceptada (partida en lobby o iniciada).  |
| Regla de confirmación | La preinscripción del equipo se confirma al iniciar la partida solo si cumple el mínimo de participantes aceptados configurado por el operador. |
| Participantes activos | En partidas por equipo, solo los integrantes que aceptan convocatoria cuentan como participantes activos. |
| Rechazo de convocatoria | Rechazar una convocatoria no elimina al usuario del equipo; solo lo excluye de esa partida. |
| Aclaración de la Convocatoria | La Convocatoria es la convocatoria de partida; se materializa físicamente en el microservicio Operaciones de sesión y opera a nivel de partida (la inscripción y la convocatoria son por partida, no por juego). |
| Relación con SRS | El SRS exige inscripción en lobby, convocatoria a integrantes, registro de aceptación/rechazo y cálculo de mínimos sobre participantes aceptados. |

## *D. Contexto de Partidas*  {#d.-contexto-de-partidas}

**Agregado raíz: Partida**

| Clase | Tipo | Atributos | Métodos principales | Relaciones |
| ----- | ----- | ----- | ----- | ----- |
| `Partida` | Agregado raíz | `PartidaId`, `NombrePartida`, `EstadoPartida`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio`, `MinimosParticipacion`, `MaximosParticipacion`, `Juegos`, `RankingConsolidado` | `Crear()`, `AgregarJuego()`, `PublicarPartida()`, `ValidarMinimosParticipacion()`, `IniciarPartida()`, `CancelarAutomaticamentePorMinimos()`, `ActivarSiguienteJuego()`, `CalcularRankingConsolidado()`, `CancelarPartida()`, `FinalizarPartida()` | Contiene `1..*` `Juego` en orden secuencial. Tiene un único lobby como fase y, según la modalidad, una o varias `InscripcionPartida` (contexto de Participación). Al finalizar, calcula su `RankingConsolidado`. |
| `Juego` | Entidad base | `JuegoId`, `Orden`, `TipoJuego`, `EstadoJuego`, `PartidaId` | `Activar()`, `Finalizar()` | Pertenece a `Partida`. Especializada en `JuegoTrivia` (contexto Trivia) y `JuegoBDT` (contexto BDT) según `TipoJuego`. |
| `PartidaId` | Value Object | `Valor` | `EsValido()` | Identificador de partida. Referenciado por `JuegoTrivia`, `JuegoBDT`, `InscripcionPartida` y auditoría. |
| `JuegoId` | Value Object | `Valor` | `EsValido()` | Identificador de juego. Usado por `Juego`, `JuegoTrivia` y `JuegoBDT`. |
| `NombrePartida` | Value Object | `Valor` | `EsValido()` | Nombre de la partida; no vacío. |
| `TiempoInicio` | Value Object | `FechaHora` | `EsValido()`, `YaAlcanzadoEn(actual)` | Momento configurado para el inicio automático. Aplica en los modos `Automatico` y `ManualYAutomatico`. |
| `MinimosParticipacion` | Value Object | `Valor` (y, en modalidad equipo, mínimo de participantes aceptados por equipo) | `EsValido()` | Mínimos que condicionan el inicio y la cancelación automática. En `Individual`: mínimo de participantes; en `Equipo`: mínimo de equipos (más mínimo de participantes aceptados por equipo). |
| `MaximosParticipacion` | Value Object | `Valor` (y, en modalidad equipo, máximo de participantes por equipo) | `EsValido()` | En `Individual`: máximo de participantes; en `Equipo`: máximo de equipos (más máximo de participantes por equipo). |
| `TipoJuego` | Enum | `Trivia`, `BusquedaDelTesoro` | — | Define el tipo de cada juego. |
| `EstadoJuego` | Enum | `Pendiente`, `Activo`, `Finalizado` | — | Sub-estado interno de un juego dentro de la partida. |
| `EstadoPartida` | Enum | `Lobby`, `Iniciada`, `Cancelada`, `Terminada` | — | Estado de la partida. |
| `Modalidad` | Enum | `Individual`, `Equipo` | — | Modalidad de la partida; aplica a todos sus juegos. |
| `ModoInicioPartida` | Enum | `Manual`, `Automatico`, `ManualYAutomatico` | — | Modo de inicio. `Manual`: la inicia el operador. `Automatico`: se inicia al llegar `TiempoInicio`. `ManualYAutomatico`: el operador puede iniciarla antes del `TiempoInicio` o, si no lo hace, se inicia automáticamente al llegarlo. Todo inicio requiere cumplir los mínimos de participación. |

**Entidad: Juego (base)**

| Elemento | Detalle |
| ----- | ----- |
| Tipo | Entidad base dentro del agregado Partida, especializada en JuegoTrivia y JuegoBDT según TipoJuego. |
| Value Objects | JuegoId |
| Atributos | Orden, TipoJuego, EstadoJuego, PartidaId |
| Invariantes | Cada juego pertenece a una partida y tiene un orden único dentro de ella. Su sub-estado (Pendiente/Activo/Finalizado) es independiente del estado de la partida. La especialización concreta (JuegoTrivia o JuegoBDT) aporta el contenido y el runtime del tipo correspondiente. |
| Relación | JuegoTrivia (contexto Trivia) y JuegoBDT (contexto BDT) son las especializaciones de Juego. |

## *E. Contexto de Trivia* {#e.-contexto-de-trivia}

**Agregado raíz : JuegoTrivia** 

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | JuegoTrivia (especialización de Juego) |
| Entidades hijas | Pregunta, ParticipanteTrivia, RespuestaTrivia |
| Value Objects | JuegoId, PartidaId, TiempoRespuesta, RankingTrivia, Opcion, PuntajeAsignado, TiempoLimite |
| Atributos | Orden, EstadoJuego, PartidaId, Preguntas, Participantes, Respuestas, PreguntaActualId |
| Enums | EstadoJuego |
| Invariantes de modalidad | La modalidad la define la partida. Si es individual, el Participante representa un UsuarioId; si es por equipos, representa un EquipoId. |
| Invariantes de contenido | El juego contiene directamente sus preguntas, creadas al crear el juego. Cada pregunta debe tener opciones, una respuesta correcta, puntaje asignado y tiempo límite. Debe existir al menos una pregunta completa para poder publicar la partida. No hay reutilización de preguntas ni banco de preguntas. |
| Regla de respuesta | En individual, una respuesta por participante por pregunta. En equipos, una respuesta por equipo por pregunta, siendo válida la primera opción enviada por cualquier integrante activo. |
| Regla de cierre | La pregunta se cierra para todos cuando un participante/equipo responde correctamente o cuando se agota el tiempo. |
| Regla de puntaje | Respuesta correcta suma directamente el PuntajeAsignado de la pregunta. El tiempo no modifica el puntaje. |
| Regla de desempate (ranking nativo) | Si hay empate en puntaje, se ordena por menor tiempo acumulado de respuesta. |
| Aporte al ranking consolidado | El puntaje acumulado de cada Participante en el juego alimenta el ranking consolidado de la partida. |
| Cambios respecto a la versión anterior | Ahora es el único agregado de Trivia: pierde estado de partida, modalidad e inicio (suben a Partida), elimina la dependencia del formulario y contiene directamente sus preguntas (con opciones, respuesta correcta, puntaje y tiempo). |
| Relación con SRS | El SRS actual define respuesta única, cierre por respuesta correcta o tiempo, puntaje directo sin ponderación y desempate por tiempo acumulado, dentro de un juego de Trivia. |

## *F. Contexto de Búsqueda del Tesoro* {#f.-contexto-de-búsqueda-del-tesoro}

**Agregado raíz: JuegoBDT** 

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | JuegoBDT (especialización de Juego) |
| Entidades hijas | EtapaBDT, ParticipanteBDT, TesoroQR, Pista |
| Value Objects | JuegoId, PartidaId, AreaBusqueda, CodigoQREsperado, UbicacionGeografica, TiempoLimite, TiempoResolucionEtapa, RankingBDT |
| Atributos | Orden, EstadoJuego, PartidaId, AreaBusqueda, Etapas, Participantes, IndiceEtapaActual |
| Enums | EstadoJuego, EstadoEtapa, ResultadoValidacionQR |
| Invariantes de juego | Todo juego de BDT debe tener una o más etapas válidas. Cada etapa debe tener contenido textual esperado del QR, puntaje y tiempo límite. |
| Área de búsqueda | Texto descriptivo simple. No se modela como coordenadas ni polígono geográfico en esta versión. |
| QR esperado | Se almacena como contenido textual esperado del QR. La imagen subida por el participante se procesa para decodificar su contenido. |
| Intentos de tesoro | Un participante/equipo puede hacer múltiples intentos en una etapa hasta validar correctamente o hasta que la etapa cierre. |
| Cierre de etapa | La etapa se cierra inmediatamente para todos cuando un participante/equipo valida correctamente el QR o cuando vence el tiempo. |
| BDT por equipos | Si un integrante activo sube el QR correcto, la etapa se considera ganada por todo el equipo. |
| Ranking BDT (nativo) | Se ordena por el puntaje acumulado en el juego (suma de los puntos de las etapas ganadas). En empate, por menor tiempo acumulado únicamente de las etapas ganadas. La cantidad de etapas ganadas se conserva como dato informativo.  |
| Puntaje por etapa | Puntaje por etapa | Cada EtapaBDT tiene un puntaje configurado por el operador. Cada etapa ganada otorga ese puntaje al participante o equipo que la ganó; ese puntaje se acumula como su puntaje dentro del juego de BDT y determina el ranking nativo del juego y su aportación al puntaje total de la partida. Las etapas que nadie gana no otorgan puntaje.  |
| Geolocalización | Es obligatoria para participar en un juego BDT activo. El participante debe autorizar ubicación desde la app móvil. |
| Inicio | El modo de inicio se define a nivel de partida (ModoInicioPartida). El juego de BDT activa su primera etapa cuando se activa según el orden de la partida. |
| Cambios respecto a la versión anterior | Antes era el agregado PartidaBDT. Pierde estado de partida, modalidad y modo de inicio (suben a Partida) y gana JuegoId, Orden, EstadoJuego y la referencia a su Partida. EtapaBDT gana su puntaje. |
| Relación con SRS | Relación con SRS | El SRS define área textual, QR esperado como contenido, cierre por validación o tiempo, geolocalización obligatoria, ranking del juego por puntaje (suma de los puntos de las etapas ganadas) y, para el consolidado, juegos ganados, puntaje total y tiempo total.  |

# 

## *G. Contexto de Auditoría* {#g.-contexto-de-auditoría}

**Agregado raíz: RegistroAuditoria**

| Elemento | Detalle |
| ----- | ----- |
| Entidad principal | RegistroAuditoria |
| Entidades hijas | EventoHistorial |
| Enums | TipoEventoHistorial |
| Naturaleza | Capacidad transversal: ya no es un microservicio físico aparte; se materializa principalmente en Puntuaciones y en Operaciones de sesión, alimentada por eventos de dominio. |
| Invariantes | El historial se conserva aunque una partida sea cancelada o un equipo sea eliminado. Una partida cancelada conserva eventos, puntajes/resultados parciales e historial, pero no cuenta como resultado final. |
| Eventos registrados | Cambios de estado, inscripciones, convocatorias, invitaciones de equipo, activación y finalización de juegos, respuestas, tesoros subidos, validaciones QR, pistas, ubicaciones, variaciones de ranking/puntaje, ranking consolidado, cancelaciones y resultados. |
| Relación con SRS | El SRS exige trazabilidad de eventos, historial y conservación de eventos relevantes. |

# **6\. Eventos de dominio**  {#6.-eventos-de-dominio}

| Evento | Datos principales | Cuándo ocurre |
| ----- | ----- | ----- |
| UsuarioCreado | UsuarioId, KeycloakId, Rol | Cuando se crea un usuario desde UMBRAL mediante Keycloak. |
| UsuarioDesactivado | UsuarioId | Cuando el administrador desactiva un usuario. |
| EquipoCreado | EquipoId, LiderId | Cuando un participante o administrador crea un equipo. |
| ParticipanteAgregadoAEquipoPorInvitacion | EquipoId, UsuarioId, InvitacionEquipoId |  Cuando un participante acepta una invitación y pasa a ser miembro del equipo. |
| InvitacionEquipoCreada | InvitacionEquipoId, EquipoId, UsuarioId |  Cuando el líder invita a un participante. |
| InvitacionEquipoRespondida | InvitacionEquipoId, UsuarioId, EstadoInvitacion |  Cuando el participante acepta o rechaza la invitación. |
| LiderazgoTransferido | EquipoId, LiderAnteriorId, NuevoLiderId | Cuando se transfiere liderazgo. |
| EquipoEliminado | EquipoId, ActorId | Cuando el líder o administrador elimina un equipo permitido. |
| EquipoDesactivado | EquipoId, AdministradorId | Cuando el administrador desactiva un equipo. |
| PartidaCreada | PartidaId, OperadorId, Modalidad, Juegos (orden y tipo) |  Cuando el operador crea una partida con sus juegos en orden secuencial. |
| EquipoPreinscritoEnPartida | InscripcionId, PartidaId, EquipoId | Cuando el líder preinscribe un equipo. |
| ConvocatoriaCreada | ConvocatoriaId, PartidaId, EquipoId, UsuarioId | Cuando se convoca a un integrante. |
| ConvocatoriaRespondida | ConvocatoriaId, UsuarioId, EstadoConvocatoria | Cuando un integrante acepta o rechaza. |
| RolDeUsuarioModificado | UsuarioId, RolAnterior, RolNuevo, AdministradorId | Cuando el administrador modifica el rol de un usuario (no aplica a administradores). |
| PermisosDeRolModificados | NombreRol, Privilegios, PermisosFuncionales, AdministradorId | Cuando el administrador modifica los permisos o privilegios de un rol desde el panel de gobernanza. |
| InscripcionConfirmada | InscripcionId, PartidaId | Cuando se confirma que cumple mínimos al iniciar. |
| PartidaPublicadaEnLobby | PartidaId | Cuando el operador publica la partida y esta pasa a estado lobby. |
| PartidaIniciada | PartidaId | Cuando la partida inicia cumpliendo mínimos. |
| JuegoActivado | PartidaId, JuegoId, TipoJuego, Orden |  Cuando la partida activa un juego según el orden secuencial. |
| PartidaCancelada | PartidaId, OperadorId, Motivo | Cuando el operador cancela o cuando el sistema cancela por mínimos no cumplidos. |
| RespuestaTriviaRegistrada | PartidaId, JuegoId, ParticipanteId, PreguntaId, OpcionSeleccionada | Cuando se recibe respuesta válida. |
| RespuestaTriviaValidada | PartidaId, JuegoId, ParticipanteId, PreguntaId, EsCorrecta, TiempoEmpleado | Al validar la respuesta. |
| PuntajeTriviaIncrementado | PartidaId, JuegoId, ParticipanteId, PuntajeAcumulado | Cuando una respuesta correcta suma puntaje directo. |
| PreguntaTriviaCerrada | PartidaId, JuegoId, PreguntaId, RespuestaCorrecta | Cuando alguien acierta o vence el tiempo. |
| RankingTriviaActualizado | PartidaId, JuegoId, Ranking | Cuando cambia el ranking del juego por respuesta correcta o desempate. |
| TesoroQRSubido | PartidaId, JuegoId, EtapaId, ParticipanteId, ImagenUrl | Cuando el participante sube imagen QR. |
| TesoroQRValidado | PartidaId, JuegoId, EtapaId, ParticipanteId, ResultadoValidacion | Cuando se compara QR decodificado con QR esperado. |
| EtapaBDTGanada | PartidaId, JuegoId, EtapaId, ParticipanteId, TiempoResolucion, Puntaje | Cuando un participante/equipo valida correctamente el QR; incluye el puntaje otorgado por la etapa. |
| EtapaBDTCerrada | PartidaId, JuegoId, EtapaId, MotivoCierre | Cuando la etapa se cierra por ganador o por tiempo. |
| RankingBDTActualizado | PartidaId, JuegoId, Ranking | Cuando cambia el ranking del juego de BDT (por puntaje o por desempate). |
| PistaEnviada | PartidaId, JuegoId, DestinatarioId, Texto | Cuando el operador envía pista. |
| UbicacionParticipanteActualizada | PartidaId, JuegoId, ParticipanteId, UbicacionGeografica | Cuando llega ubicación autorizada en un juego BDT. |
| JuegoFinalizado | PartidaId, JuegoId |  Cuando un juego termina (última pregunta o última etapa). |
| PartidaFinalizada | PartidaId, ResultadoFinal | Cuando termina el último juego de la partida. |
| RankingConsolidadoCalculado | PartidaId, RankingConsolidado |  Cuando la partida finaliza y se calcula el ranking consolidado. |
| CredencialTemporalEmitida  | UsuarioId, Correo, Motivo (Creacion / CambioDeCorreo)  | Cuando el sistema emite y envía una contraseña temporal al usuario: al crearlo o al cambiar su correo con la credencial temporal pendiente.  |

# **7\. Servicios de dominio actualizados** {#7.-servicios-de-dominio-actualizados}

| Servicio | Responsabilidad | Usa |
| ----- | ----- | ----- |
| ValidadorJuegoTriviaService | Validar que el juego de Trivia tenga al menos una pregunta completa: cada pregunta con opciones, respuesta correcta, puntaje y tiempo.  | JuegoTrivia, Pregunta |
| GestorPermisosRolService | Aplicar, a nivel de rol, los cambios de permisos y privilegios desde el panel de gobernanza, protegiendo los privilegios de gobernanza del rol Administrador e impidiendo crear roles nuevos. | Rol |
| ValidadorCambioRolService | Validar que el administrador pueda modificar el rol de un usuario: permite operadores y participantes (incluida la promoción a administrador), impide modificar el rol de un administrador y propaga el cambio a Keycloak. | Usuario, Rol |
| ValidadorInscripcionService | Validar estado de partida, modalidad, cupo, liderazgo, equipo activo, mínimos, preinscripción y que el participante o equipo no tenga ya una participación activa en otra partida (para el participante, inscripción individual o convocatoria de equipo aceptada), a nivel de partida   | InscripcionPartida, Equipo, Partida |
| ValidadorConvocatoriaService | Determinar participantes activos según convocatorias aceptadas y validar que un participante no acepte una convocatoria si ya tiene una participación activa en otra partida.  | Convocatoria, InscripcionPartida |
| ValidadorInvitacionEquipoService |  Validar que solo el líder invite, que el invitado no pertenezca ya a un equipo (lista dinámica/exclusión), que el equipo no esté lleno (tope de 5\) y aplicar las reglas de aceptación (mensajes “Ya perteneces a un equipo” / “El equipo ya está lleno”). | InvitacionEquipo, Equipo, Usuario |
| CalculadorRankingTriviaService | Ordenar el ranking del juego de Trivia por puntaje acumulado descendente y desempatar por menor tiempo acumulado de respuesta. | ParticipanteTrivia |
| ValidadorRespuestaTriviaService | Validar opción seleccionada contra respuesta correcta y reglas de respuesta única. | Pregunta, RespuestaTrivia, JuegoTrivia |
| ValidadorQRService | Comparar QR decodificado contra contenido textual esperado de la etapa activa. | CodigoQREsperado, TesoroQR, EtapaBDT |
| CalculadorRankingBDTService | Ordenar el ranking del juego de BDT por puntaje acumulado (suma de los puntos de las etapas ganadas) y desempatar por menor tiempo acumulado únicamente de las etapas ganadas.  | ParticipanteBDT |
| CalculadorRankingConsolidadoService | Determinar el ganador de cada juego (el participante o equipo con mayor puntaje en él; desempate por menor tiempo en el juego; si persiste, sin ganador) y calcular, al finalizar la partida, el ranking consolidado ordenando por número de juegos ganados, luego por puntaje total acumulado en todos los juegos y luego por menor tiempo total.  | Partida, JuegoTrivia, JuegoBDT |
| ValidadorGeolocalizacionBDTService | Validar que el participante haya autorizado geolocalización para participar en un juego BDT activo. | ParticipanteBDT, UbicacionGeografica |
| ValidadorEliminacionEquipoService | Validar que un equipo no esté en lobby ni en partida iniciada antes de eliminarse. | Equipo, InscripcionPartida |
| ValidadorTransicionEstadoPartidaService | Validar transiciones entre lobby, iniciada, cancelada y terminada. | EstadoPartida |
| GestorCredencialTemporalService  | Decidir la emisión de contraseña temporal: al crear el usuario y al cambiar su correo si la credencial sigue temporal pendiente; y marcar la credencial como definitiva cuando el usuario cambia su contraseña. El envío del correo en sí es responsabilidad de la infraestructura (consumidor del evento).  | Usuario |

