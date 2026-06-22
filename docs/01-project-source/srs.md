# 

# 

# 

# 

# 

# 

# 

# 

# Especificación de requisitos de software

*Grupo 4 \- Proyecto Umbral*

# 

# 

# 

# 

# 

# 

# 

# 

Caracas, Mayo 2026					Santiago De Andrade  
Mariangel Fernandez  
**Tabla de contenido**

[**Objetivo general	3**](#objetivo-general)

[**Objetivos específicos	4**](#objetivos-específicos)

[**Requerimientos	7**](#requerimientos)

[Requerimientos funcionales	7](#requerimientos-funcionales)

[Requerimientos no funcionales	19](#requerimientos-no-funcionales)

[**Clientes objetivo de las historias de usuario	23**](#heading)

[**Historias de usuario	24**](#historias-de-usuario)

[**Actores	48**](#actores)

[Consideraciones de acceso y dominio	52](#consideraciones-de-acceso-y-dominio)

[**Reglas de negocio	58**](#reglas-de-negocio)

[Reglas de negocio generales	58](#reglas-de-negocio-generales)

[Reglas de negocio de inicio de partidas	63](#reglas-de-negocio-de-inicio-de-partidas)

[Reglas de negocio de convocatorias	64](#heading-1)

[Reglas de negocio de equipos	64](#reglas-de-negocio-de-equipos)

[Reglas de negocio de usuarios y roles	67](#reglas-de-negocio-de-usuarios-y-roles)

[Reglas de negocio de trivias	69](#reglas-de-negocio-de-trivias)

[Reglas de búsqueda de tesoro	72](#reglas-de-búsqueda-de-tesoro)

[**Alcance	77**](#alcance)

[Alcance específico del modo Búsqueda del Tesoro	81](#alcance-específico-del-modo-búsqueda-del-tesoro)

[Alcance específico del modo Trivia	85](#alcance-específico-del-modo-trivia)

[Límites del alcance	89](#límites-del-alcance)

# 

# **Objetivo general** {#objetivo-general}

Centralizar y controlar la operación de partidas interactivas en tiempo real bajo los modos de juego Trivia y Búsqueda del Tesoro, permitiendo la creación de partidas individuales o por equipos, gestión de lobbies, participación de participantes, seguimiento operativo, validación de respuestas o tesoros, cálculo de puntajes y trazabilidad de eventos mediante una solución compuesta por una aplicación web para administradores y operadores, una aplicación móvil para participantes, backend basado en arquitectura hexagonal, persistencia relacional, comunicación en tiempo real y mensajería asíncrona.

# 

# **Objetivos específicos** {#objetivos-específicos}

* Definir la arquitectura funcional y técnica del sistema, estableciendo una separación clara entre dominio, aplicación, infraestructura e interfaces externas, conforme a principios de arquitectura hexagonal.  
* Modelar el dominio del sistema UMBRAL, identificando entidades, agregados, objetos de valor, servicios de dominio y reglas de negocio necesarias para representar partidas, juegos, participantes, equipos, líderes, invitaciones de equipo, convocatorias, roles, permisos y privilegios, preguntas y etapas de Búsqueda del Tesoro, tesoros QR, respuestas, puntajes, ubicaciones, eventos y comportamientos propios de los modos Trivia y Búsqueda del Tesoro.  
* Delimitar los modos de juego soportados por la plataforma, estableciendo que cada juego de una partida debe ser exclusivamente de tipo Trivia o Búsqueda del Tesoro, sin permitir la creación, configuración o ejecución de tipos de juego adicionales.  
* Estructurar cada partida como una secuencia ordenada de uno o más juegos de tipo Trivia o Búsqueda del Tesoro, definida por el operador al crear la partida, donde la partida concentra el ciclo de vida, el estado de lobby, la inscripción, las convocatorias, la modalidad y la cancelación, y cada juego se activa de forma secuencial con su propio sub-estado interno.  
* Diseñar los flujos principales de administración, operación y participación, diferenciando las funcionalidades comunes del sistema y las acciones específicas de cada modo de juego.  
* Implementar un panel de gobernanza para el administrador que permita consultar y modificar, a nivel de rol, los permisos y privilegios de cada rol, distinguiendo dos niveles —privilegios de gobernanza y permisos funcionales—, así como modificar el rol de operadores y participantes (incluida la promoción a administrador) sin poder modificar el rol de un administrador, propagando los cambios a Keycloak; los privilegios de gobernanza del rol Administrador están protegidos.  
* Implementar la integración con Keycloak para la autenticación, autorización base y asignación inicial de roles, manteniendo en UMBRAL las referencias locales necesarias y la matriz de permisos y privilegios por rol para asociar usuarios con equipos, invitaciones de equipo, partidas, convocatorias, historial y reglas del dominio.  
* Implementar la gestión de equipos globales para la plataforma, permitiendo su uso tanto en Trivia como en Búsqueda del Tesoro, con reglas de creación, invitación de integrantes mediante una lista dinámica de participantes, liderazgo, transferencia de liderazgo, salida de integrantes y límite máximo de cinco participantes por equipo.  
* Implementar la invitación de participantes a un equipo mediante una lista dinámica de todos los participantes de la plataforma que excluye a quienes ya pertenecen a un equipo e impide invitar cuando el equipo está lleno, de modo que, al aceptar la invitación, el participante pase a ser miembro del equipo.  
* Implementar la creación y operación de partidas, permitiendo publicar la partida —con lo que pasa a estado lobby y habilita una única inscripción de participantes o equipos según su modalidad—, convocar integrantes de equipo, iniciar la partida manualmente o por tiempo y cancelarla, activando sus juegos de forma secuencial.  
* Implementar la creación y operación de juegos de Trivia dentro de una partida, permitiendo crear sus preguntas (opciones de respuesta, respuesta correcta, puntaje asignado y tiempo límite por pregunta) al momento de añadir el juego, sincronizar preguntas, validar respuestas, calcular puntajes y actualizar el ranking del juego en tiempo real.  
* Implementar la creación y operación de juegos de Búsqueda del Tesoro dentro de una partida, permitiendo definir área de búsqueda, etapas, QR esperado por etapa, puntaje por etapa, tiempo límite por etapa, pistas, avance sincronizado y cierre de etapas por hallazgo o por agotamiento del tiempo.  
* Implementar la validación automática de tesoros en Búsqueda del Tesoro mediante la decodificación del QR contenido en la imagen subida por el participante y la comparación de su contenido con el QR esperado de la etapa activa.  
* Implementar un ranking consolidado de la partida que, al finalizar, determine la clasificación general por número de juegos ganados (cada juego lo gana el participante o equipo con más puntaje en ese juego); en caso de empate, por mayor puntaje total acumulado en todos los juegos y, de persistir, por menor tiempo total. Cada juego conserva además su ranking nativo, ordenado por puntaje.  
* Incorporar geolocalización operativa para Búsqueda del Tesoro, permitiendo al operador visualizar en un mapa la ubicación de los participantes durante partidas iniciadas, con actualización cada dos segundos y previa autorización del usuario.  
* Incorporar trazabilidad sobre las acciones relevantes del sistema, registrando cambios de estado, inscripciones, convocatorias, invitaciones de equipo, activación y finalización de juegos, respuestas de Trivia, tesoros subidos, validaciones de QR, pistas enviadas, ubicaciones relevantes, variaciones de puntaje, cancelaciones, ranking consolidado, cambios de rol, cambios de permisos por rol y resultados de partida.  
* Integrar comunicación en tiempo real, permitiendo que la publicación de partidas, el estado de lobby, los temporizadores, juegos, preguntas, ranking, etapas, pistas, resultados, geolocalización y cambios de estado se actualicen de forma inmediata para operadores y participantes.  
* Implementar una separación entre operaciones de lectura y escritura, organizando los casos de uso mediante CQRS y MediatR para estructurar comandos, consultas y manejadores.  
* Persistir la información del sistema en una base de datos relacional, utilizando PostgreSQL y Entity Framework Core para almacenar referencias locales de usuarios autenticados por Keycloak, roles, permisos y privilegios por rol, equipos, invitaciones de equipo, historial de equipos, partidas, juegos, preguntas, respuestas, etapas, tesoros subidos, puntajes, ubicaciones, convocatorias e historial.  
* Organizar el backend en cuatro microservicios de negocio —Partidas, Operaciones de sesión, Puntuaciones e Identity—, manteniendo los contextos acotados como límites lógicos del dominio que se materializan sobre dichos microservicios.  
* Implementar un API Gateway con YARP como punto único de entrada al backend, encargado de validar el token JWT emitido por Keycloak y de controlar la autorización de acceso a los microservicios mediante autorización por rol (Administrador, Operador, Participante) a nivel de ruta —usando los claims de rol del token, sin consultar a Identity en cada petición—, y de enrutar todo el tráfico, incluido el de tiempo real (WebSockets/SignalR), hacia el microservicio correspondiente; la autorización fina por permisos funcionales permanece en los microservicios.  
* Desacoplar procesos secundarios del flujo principal de la partida mediante mensajería asíncrona con RabbitMQ para la publicación y procesamiento de eventos relacionados con auditoría, historial, notificaciones internas, ranking y trazabilidad.  
* Garantizar la calidad técnica de la solución, incorporando validaciones de negocio, manejo de excepciones, logging y pruebas unitarias, de integración y end-to-end con criterios de cobertura definidos.

# 

# **Lenguaje Ubicuo**

## *Estructura de partida y juegos*

| Término | Definición |
| ----- | ----- |
| Partida | Unidad organizativa de nivel superior. Contiene uno o más juegos en orden secuencial. Tiene una única modalidad, un modo de inicio, mínimos/máximos de participación y estados lobby, iniciada, cancelada y terminada. |
| Juego | Componente de una partida; es de tipo Trivia o Búsqueda del Tesoro. Tiene un orden dentro de la partida y sub-estados pendiente, activo y finalizado. |
| Juego de Trivia (JuegoTrivia) | Juego compuesto por preguntas, creadas al añadir el juego. |
| Juego de Búsqueda del Tesoro / BDT (JuegoBDT) | Juego compuesto por etapas. Cada etapa tiene un tesoro a conseguir. |
| Pregunta | Elemento de un juego de Trivia: opciones, respuesta correcta, puntaje y tiempo límite. |
| Etapa | Elemento de un juego de BDT: contenido textual esperado del QR (el tesoro), puntaje y tiempo límite. |
| Tesoro (TesoroQR) | Código QR esperado de una etapa; su contenido textual valida el hallazgo. |
| Modalidad | Define si la partida se juega individual o por equipos. Única para toda la partida; aplica a todos sus juegos. No confundir con el tipo de juego. |
| Tipo de juego | Trivia o Búsqueda del Tesoro. No confundir con la modalidad. |

## *Actores y unidad que compite*

| Término | Definición |
| ----- | ----- |
| Participante | Actor humano; usuario con rol Participante. Participa en partidas individuales, crea equipos o se une a ellos por invitación, y puede ser líder. |
| Operador | Actor humano que crea, configura, pública, ejecuta y supervisa partidas. |
| Administrador | Actor humano que gestiona usuarios, roles y la gobernanza de permisos. |
| Rol | Clasificación de acceso de un usuario: Administrador, Operador o Participante. |
| Unidad competidora (ParticipanteTrivia / ParticipanteBDT) | La entidad que compite, acumula puntaje y es clasificada dentro de un juego. Según la modalidad representa a un participante individual o a un equipo; su identidad mapea a un UsuarioId o a un EquipoId. En enunciados de puntaje y ranking se usa “participante o equipo”; no debe usarse “participante” a secas para la unidad que compite, porque en modalidad por equipos esa unidad es un equipo. |
| Equipo | Agrupación de 1 a 5 participantes con un líder. Compite como unidad en partidas por equipos. |
| Líder de equipo | Participante creador del equipo o a quien se transfirió el liderazgo. Único habilitado para inscribir al equipo e invitar integrantes. |
| Integrante / miembro | Participante que pertenece a un equipo. |

## *Participación*

| Término | Definición |
| ----- | ----- |
| Inscripción (InscripcionPartida) | Registro de la confirmación de participación de un participante individual o un equipo en una partida por equipos. Una sola por partida, no por juego. |
| Convocatoria | Llamado a los integrantes de un equipo para participar en una partida por equipos; cada integrante la acepta o la rechaza. Solo quienes aceptan cuentan como participantes activos de esa partida. |
| Invitación de equipo (InvitacionEquipo) | Solicitud para que un participante se una a un equipo; se acepta o rechaza y no caduca. Distinta de la convocatoria. |
| Lobby | Estado de una partida publicada que admite inscripciones. Es un estado de la partida, no un componente. |
| Modo de inicio (ModoInicioPartida) | Determina cómo arranca la partida: Manual, Automatico o ManualYAutomatico. |
| Participación activa | Situación de un participante o equipo con una inscripción activa o, para el participante, una convocatoria aceptada activa. Solo se permite una a la vez. |

## *Puntaje y ranking*

| Término | Definición |
| ----- | ----- |
| Puntaje | Puntos. El operador asigna un puntaje a cada pregunta de Trivia y a cada etapa de BDT. |
| Pregunta/etapa ganada | La que un participante o equipo gana —en Trivia, ser el primero en responder correctamente; en BDT, ser el primero en encontrar el tesoro—, obteniendo su puntaje. |
| Ranking nativo de un juego | Clasificación dentro de un juego, ordenada por el puntaje acumulado en ese juego. El desempate se realiza por menor tiempo en el juego. |
| Ganador de un juego | El participante o equipo con mayor puntaje en ese juego. El desempate se realiza por menor tiempo; si persiste, el juego no otorga victoria. |
| Ranking consolidado de la partida | Clasificación final de la partida: por número de juegos ganados; en empate, por mayor puntaje total acumulado en todos los juegos; y, de persistir, por menor tiempo total. |
| Ganador de la partida | El participante o equipo en la primera posición del ranking consolidado. |
| Etapas ganadas | Cantidad de etapas que un participante o equipo ganó en un juego de BDT. Dato informativo, no criterio de ordenación. |
| Tiempo total | Suma de los tiempos de respuesta de las preguntas de Trivia ganadas más los tiempos de obtención de los tesoros de las etapas de BDT ganadas. Último desempate del ranking consolidado. |

## 

## *Identidad, accesos e infraestructura*

| Término | Definición |
| ----- | ----- |
| Credencial temporal (EstadoCredencial) | Estado de la credencial de un usuario: TemporalPendiente, cuando la contraseña temporal no fue cambiada, o Definitiva, cuando el usuario ya la cambió. |
| Gobernanza | Administración, a nivel de rol, de los privilegios de gobernanza y los permisos funcionales. |
| Privilegio de gobernanza | Capacidad de administración asociada a un rol, por ejemplo: GestionarUsuarios o ModificarRolDeUsuario. |
| Permiso funcional | Capacidad de uso de una función asociada a un rol, por ejemplo: GestionarPartidas, GestionarEquipos o ParticiparEnPartidas. |
| API Gateway (YARP) | Punto único de entrada al backend; valida el JWT de Keycloak y aplica autorización por rol a nivel de ruta. |
| Keycloak | Proveedor de identidad externo; gestiona autenticación, tokens JWT y roles base. |

# **Requerimientos** {#requerimientos}

## *Requerimientos funcionales* {#requerimientos-funcionales}

| ID | Modo | Descripción |
| ----- | ----- | ----- |
| RF-01 | General | El sistema debe integrarse con Keycloak para autenticar administradores, operadores y participantes, permitir la creación de usuarios desde UMBRAL, asignar un rol inicial durante la creación, permitir que el administrador modifique posteriormente el rol de operadores y participantes —incluida la promoción a administrador— sin poder modificar el rol de un administrador y propagando el cambio a Keycloak, consultar/editar datos generales (usuario y correo), desactivar usuarios y almacenar únicamente una referencia local al identificador proveniente de Keycloak, sin guardar contraseñas. |
| RF-02 | General | El sistema debe diferenciar las funcionalidades disponibles según el rol autenticado del usuario —administrador, operador o participante— y según reglas propias del dominio, como liderazgo de equipo, pertenencia a equipo, invitación de equipo, inscripción, convocatoria y participación en partidas. |
| RF-03 | General | El sistema debe permitir crear partidas cuyos juegos sean únicamente de tipo Trivia o Búsqueda del Tesoro, e impedir la creación, configuración o ejecución de cualquier otro tipo de juego. |
| RF-04 | General | Toda partida debe manejar únicamente los estados lobby, iniciada, cancelada y terminada; el sistema debe validar toda transición de estado, permitir al operador cancelar partidas únicamente desde lobby o iniciada, y bloquear nuevas acciones de juego cuando una partida esté cancelada o terminada. |
| RF-05 | General | El sistema debe mostrar a todos los participantes, desde la aplicación móvil, las partidas publicadas, independientemente de si son individuales o por equipo, mediante un único panel principal Partidas que lista todas las partidas publicadas y permite filtrar por modalidad individual o equipo. |
| RF-06 | General | El sistema debe permitir que un participante juegue partidas individuales aunque pertenezca a un equipo —siempre que no tenga una participación activa en otra partida —, pero debe impedir que un participante no líder inscriba un equipo en una partida por equipo, mostrando el mensaje: “Debes ser líder de un equipo para entrar en esta partida”.  |
| RF-07 | Equipos | El sistema debe permitir que un participante cree un equipo solo si no pertenece a otro, registrar como líder al creador, permitir que el líder invite a otros participantes mediante una lista dinámica de todos los participantes de la plataforma que excluye a quienes ya pertenecen a un equipo e impide invitar cuando el equipo está lleno, hacer que un participante pase a ser miembro del equipo al aceptar una invitación, impedir que un participante pertenezca a más de un equipo, limitar cada equipo a un máximo global de cinco participantes y permitir que los mismos equipos participen tanto en Trivia como en Búsqueda del Tesoro. |
| RF-08 | Equipos | El sistema debe permitir que un participante salga de su equipo; si no es líder, sale directamente, pero si es líder y existen otros integrantes, debe transferir el liderazgo antes de salir, mientras que si no existen otros integrantes el equipo debe eliminarse. |
| RF-09 | Equipos | El sistema debe permitir al administrador crear, consultar, editar, desactivar y eliminar equipos, respetando las reglas del dominio. Un equipo desactivado no puede inscribirse en nuevas partidas. Un equipo no puede eliminarse si está inscrito en una partida en estado lobby o participando en una partida en estado iniciada. |
| RF-10 | General | El sistema debe permitir que el líder preinscriba su equipo en partidas por equipo mientras estén en estado lobby y exista cupo disponible. Al hacerlo, el sistema debe enviar convocatoria a los integrantes del equipo y registrar la aceptación o rechazo de cada convocado. La inscripción del equipo se confirmará al iniciar la partida solo si cumple el mínimo de participantes aceptados definido por el operador. |
| RF-11 | General | El sistema debe impedir el inicio de una partida por equipos si no cumple la cantidad mínima de equipos o participantes aceptados por equipo definida por el operador. En partidas individuales, el máximo corresponde a participantes; en partidas por equipo, el máximo corresponde a equipos, debiendo definirse además mínimo y máximo de participantes aceptados por equipo. |
| RF-12 | General | El sistema debe registrar un historial de eventos relevantes de la partida, incluyendo cambios de estado, inscripciones, convocatorias, respuestas, tesoros subidos, validaciones, pistas, puntajes, cancelaciones y resultados. |
| RF-13 | General | El sistema debe actualizar en tiempo real los cambios relevantes de las partidas para los clientes correspondientes: aplicación web de operador/administrador y aplicación móvil de participantes, incluyendo publicación, lobby, estados, juegos, preguntas, ranking, etapas, temporizadores, pistas, geolocalización, resultados y sincronización entre dispositivos autorizados de participantes de un mismo equipo. |
| RF-14 | General | El sistema debe permitir que un participante se reconecte desde la aplicación móvil a una partida en curso mientras esta siga en estado iniciada, recuperando el estado vigente que le corresponda según su rol, equipo, convocatoria, inscripción, modalidad de la partida y juego activo. |
| RF-17 | Trivia | El sistema debe permitir al operador crear juegos de Trivia dentro de una partida, creando sus preguntas en el momento (cada una con opciones de respuesta, respuesta correcta, puntaje asignado y tiempo límite), sin reutilizar preguntas ni usar banco de preguntas; debe rechazar el juego sin al menos una pregunta completa. Los datos de nivel partida —nombre, modalidad, mínimos y máximos de participación, modo y tiempo de inicio— se definen en la partida que contiene al juego. |
| RF-18 | Trivia | Cuando una partida iniciada active un juego de Trivia según su orden secuencial, el sistema debe iniciar el juego presentando su primera pregunta a los participantes inscritos según la modalidad de la partida. |
| RF-19 | Trivia | Durante un juego de Trivia activo, el sistema debe mostrar la misma pregunta y las mismas opciones a todos los participantes al mismo tiempo, sincronizando el temporizador de cada pregunta para todos los participantes. |
| RF-20 | Trivia | En un juego de Trivia en modalidad individual, el sistema debe aceptar una única respuesta por participante por pregunta activa; en modalidad por equipos, debe aceptar una única respuesta por equipo, registrando como válida la primera opción seleccionada por cualquier integrante del equipo. |
| RF-21 | Trivia | En un juego de Trivia, el sistema debe aceptar una única respuesta por participante en modalidad individual y una única respuesta por equipo en modalidad por equipos. Debe rechazar respuestas repetidas, tardías o enviadas fuera del estado válido de la pregunta activa, validar automáticamente cada respuesta contra la opción correcta configurada y cerrar la pregunta para todos cuando algún participante/equipo responda correctamente o cuando se agote el tiempo límite. |
| RF-22 | Trivia | Al cerrar una pregunta de un juego de Trivia, el sistema debe mostrar la respuesta correcta a todos los participantes, avanzar automáticamente a la siguiente pregunta si existe, actualizar el ranking del juego en tiempo real y asignar a toda respuesta correcta el puntaje configurado para la pregunta, sin ponderarlo por tiempo restante o tiempo empleado. En caso de empate de puntaje, el ranking se ordenará por menor tiempo acumulado de respuesta. |
| RF-23 | Trivia | Durante un juego de Trivia activo, el panel del operador debe mostrar únicamente el ranking actualizado del juego y la opción de cancelar la partida, sin permitirle intervenir en las respuestas de los participantes. |
| RF-24 | General | El sistema debe permitir al participante consultar un historial único de partidas jugadas, mostrando los juegos de cada partida, la modalidad, la fecha y la posición obtenida, e incluyendo el equipo asociado si se trata de una partida por equipos. |
| RF-25 | Búsqueda del Tesoro | El sistema debe permitir al operador crear juegos de Búsqueda del Tesoro dentro de una partida, definiendo el área de búsqueda como texto descriptivo; los datos de nivel partida —nombre, modalidad, mínimos y máximos de participación y modo de inicio— se definen en la partida que contiene al juego. |
| RF-26 | Búsqueda del Tesoro | El sistema debe permitir al operador configurar una o más etapas para un juego de Búsqueda del Tesoro, donde cada etapa debe tener el contenido textual esperado del código QR, un puntaje y un tiempo límite; el sistema debe impedir publicar la partida sin etapas válidas o con etapas sin QR esperado, sin puntaje o sin tiempo definido. |
| RF-27 | Búsqueda del Tesoro | Cuando una partida iniciada active un juego de Búsqueda del Tesoro según su orden secuencial, el sistema debe activar su primera etapa y habilitar la subida de tesoros para los participantes inscritos según la modalidad de la partida. |
| RF-28 | Búsqueda del Tesoro | Durante un juego de Búsqueda del Tesoro activo, la aplicación móvil del participante debe mostrar la etapa activa, el temporizador y la opción “subir tesoro”, permitiendo tomar o subir una foto del QR encontrado como tesoro de la etapa activa. |
| RF-29 | Búsqueda del Tesoro | El sistema debe procesar la imagen subida por el participante, decodificar el contenido del QR detectado y compararlo con el contenido esperado del QR configurado para la etapa activa, marcando el tesoro como válido si coincide, o inválido si no coincide, no puede leerse o no corresponde a la etapa activa. |
| RF-30 | Búsqueda del Tesoro | El sistema debe registrar cada tesoro subido con participante o equipo asociado, partida, juego, etapa, fecha, contenido decodificado y resultado de validación; además, el operador debe poder visualizar cada tesoro subido y si fue válido o inválido. |
| RF-31 | Búsqueda del Tesoro | El sistema debe cerrar la etapa activa para todos cuando un participante/equipo valide correctamente el QR esperado o cuando se agote el tiempo límite configurado. Si hubo ganador, debe mostrar quién consiguió el tesoro y en cuánto tiempo; si no hubo ganador, debe mostrar el mensaje “nadie consiguió el tesoro”. |
| RF-32 | Búsqueda del Tesoro | Al cerrarse una etapa de Búsqueda del Tesoro, el sistema debe avanzar a la siguiente etapa si existe; si se cierra la última etapa del juego, el juego se da por finalizado y la partida activa el siguiente juego si existe, o pasa a estado terminada si era el último juego. |
| RF-33 | Búsqueda del Tesoro | El sistema debe permitir al operador visualizar la lista de participantes o equipos inscritos, enviar pistas a participantes o equipos específicos durante una partida iniciada. |
| RF-34 | Búsqueda del Tesoro | El sistema debe solicitar autorización de ubicación al participante desde la aplicación móvil antes de compartir su geolocalización. La geolocalización será obligatoria para participar en juegos BDT activos. El operador podrá visualizar en la aplicación web la ubicación de los participantes durante un juego de Búsqueda del Tesoro activo, con actualización cada 2 segundos. |
| RF-35 | Transversal | El sistema debe permitir consultar partidas, juegos, equipos, invitaciones de equipo, historial de equipos, participantes, preguntas de Trivia, etapas de Búsqueda del Tesoro, respuestas, tesoros subidos, rankings, permisos por rol e historial sin modificar el estado del sistema. |
| RF-36 | Transversal | El sistema debe aplicar reglas de negocio antes de aceptar cambios de estado, inscripciones, convocatorias, invitaciones de equipo, respuestas, tesoros subidos, validaciones, pistas, cancelaciones o cualquier acción que afecte la partida. |
| RF-37 | Transversal | El sistema debe publicar eventos relevantes del dominio para auditoría, historial, notificaciones internas dentro de la aplicación, actualización de ranking, trazabilidad de puntajes y comunicación en tiempo real. Las notificaciones push del sistema operativo quedan fuera del alcance de esta versión. |
| RF-38 | Búsqueda del Tesoro | El sistema debe calcular y mostrar un ranking de cada juego de BDT, visible para operadores y participantes, ordenado de forma descendente por el puntaje acumulado en el juego (suma de los puntos de las etapas ganadas por cada participante o equipo) y, en caso de empate, por el menor tiempo total empleado en obtener los tesoros de esas etapas ganadas. La cantidad de etapas ganadas se conserva como dato informativo, no como criterio de ordenación. |
| RF-39 | General |   El sistema debe permitir al operador crear una partida compuesta por uno o más juegos en un orden secuencial definido por el operador (Juego 1, Juego 2, …), donde cada juego es de tipo Trivia o Búsqueda del Tesoro, fijando para toda la partida una única modalidad (individual o por equipo). |
| RF-40 | General |  El sistema debe permitir al operador publicar la partida —con lo que la partida pasa a estado lobby y queda visible en el panel único Partidas—, habilitando una única inscripción de participantes individuales o equipos según la modalidad de la partida, e iniciar la partida manualmente o automáticamente al cumplirse el tiempo configurado, siempre que se cumplan los mínimos de participación; si llega el tiempo configurado y no se cumplen los mínimos, la partida debe cancelarse automáticamente. |
| RF-41 | General |   Al iniciar la partida, el sistema debe activar sus juegos de forma secuencial, uno tras otro en el orden definido; cada juego maneja un sub-estado interno propio —Pendiente, Activo o Finalizado—, y al finalizar el último juego la partida pasa a estado terminada. La cancelación aplica a toda la partida. |
| RF-42 | Equipos |   El sistema debe permitir que el líder de un equipo invite a otros participantes mediante una lista dinámica de todos los participantes de la plataforma, excluyendo a los que ya pertenecen a un equipo e impidiendo invitar cuando el equipo está lleno (máximo cinco integrantes). Al aceptar una invitación, el participante pasa a ser miembro del equipo. Las invitaciones recibidas son visibles para todos los participantes; si un participante intenta aceptar una invitación cuando ya pertenece a un equipo, el sistema debe mostrar “Ya perteneces a un equipo”, y si el equipo ya está lleno, debe mostrar “El equipo ya está lleno”. Las invitaciones no caducan; al eliminarse un equipo se eliminan sus invitaciones pendientes. |
| RF-43 | Equipos |   El sistema debe conservar y permitir consultar el historial de los equipos a los que ha pertenecido cada participante, mostrando los nombres de dichos equipos. |
| RF-44 | Equipos | El sistema debe permitir consultar el rendimiento de un equipo en todas las partidas por equipo en las que ha participado, es decir, para cada partida, la posición obtenida y si fue ganada o no, sin duplicar el cálculo de puntajes. Este rendimiento consiste únicamente en el historial de esas partidas y, para cada una, la posición obtenida en el ranking consolidado de la partida y si la ganó (entendiéndose que la ganó si obtuvo la primera posición). |
| RF-45 | General | Al finalizar una partida, el sistema debe calcular un ranking consolidado que ordene a los participantes o equipos, primero, por el número de juegos ganados; en caso de empate, por el mayor puntaje total acumulado en todos los juegos (puntaje de Trivia más puntaje de las etapas ganadas en Búsqueda del Tesoro); y, de persistir, por el menor tiempo total (suma de los tiempos de respuesta de las preguntas de Trivia ganadas más los tiempos de obtención de los tesoros de las etapas de BDT ganadas). Cada juego lo gana el participante o equipo con mayor puntaje en ese juego; en caso de empate en un juego, lo gana quien empleó menor tiempo en él y, si persiste, ese juego no otorga victoria. Cada juego conserva además su ranking nativo, ordenado por puntaje. |
| RF-46 | Búsqueda del Tesoro | Cada etapa de Búsqueda del Tesoro ganada otorga, al participante o equipo que la ganó, el puntaje configurado para esa etapa. Ese puntaje se acumula como el puntaje del participante o equipo dentro del juego de BDT y determina tanto el ranking nativo del juego como su aportación al puntaje total de la partida. Las etapas que nadie gana no otorgan puntaje. |
| RF-47 | General | El sistema debe proporcionar al administrador un panel de gobernanza para consultar y modificar, a nivel de rol, los privilegios de gobernanza y los permisos funcionales asociados a cada rol (Administrador, Operador, Participante). Los permisos se administran por rol, no por usuario individual; no se pueden crear roles nuevos; y los privilegios de gobernanza del rol Administrador están protegidos y no pueden retirarse. |
| RF-48 | General | El sistema debe permitir al administrador modificar el rol de un usuario operador o participante, incluida su promoción a administrador, e impedir la modificación del rol de un usuario administrador; el cambio de rol debe propagarse a Keycloak. |
| RF-49 | General  | El sistema debe impedir que un participante o un equipo participe en más de una partida a la vez. Un equipo no puede inscribirse ni preinscribirse en una partida si ya tiene una inscripción activa en otra. Un participante no puede inscribirse individualmente ni aceptar una convocatoria de equipo si ya tiene una participación activa en otra partida, contando como participación activa tanto su inscripción individual como una convocatoria de equipo aceptada. Al terminarse o cancelarse la partida correspondiente (o al cancelarse la inscripción o rechazarse la convocatoria), el participante o equipo queda libre para participar en otra. |
| RF-50 | General | El sistema debe generar una contraseña temporal y enviarla por correo al usuario cuando el administrador lo crea; dicha contraseña debe exigir su cambio en el primer inicio de sesión. Si posteriormente el administrador modifica el correo del usuario y su contraseña temporal sigue vigente (aún no ha sido cambiada), el sistema debe generar una nueva contraseña temporal, invalidar la anterior y enviarla al nuevo correo. La contraseña temporal se gestiona en Keycloak y no se almacena en UMBRAL, y el envío se realiza de forma asíncrona. |
| RF-51 | General | Una vez autenticado el usuario, en cada ciclo de refresco (cada 270 segundos) el sistema debe evaluar su actividad: si hubo interacción en los últimos 120 segundos, refresca el token de forma silenciosa; si no la hubo, en lugar de refrescar muestra un modal con el texto “La sesión está por expirarse, ¿desea continuar?” y un único botón “Continuar”, con una cuenta atrás de 30 segundos. Si el usuario pulsa “Continuar” dentro de esos 30 segundos, el sistema refresca el token y reanuda el seguimiento de actividad; si no responde dentro de ese tiempo (al cabo del cual el token expira), el sistema cierra la sesión y redirige al inicio de sesión. Si el refresco del token contra Keycloak falla (por ejemplo, el refresh token ha expirado o hay un error de red), el sistema cierra la sesión y muestra un mensaje descriptivo indicando que la sesión ha finalizado y que debe iniciar sesión nuevamente. |

## *Requerimientos no funcionales* {#requerimientos-no-funcionales}

| ID | Descripción |
| ----- | ----- |
| RNF-01 | La solución debe implementarse con una aplicación web en React para administradores y operadores, una aplicación móvil en React Native para participantes y backend en .NET Core. |
| RNF-02 | La persistencia principal debe resolverse con PostgreSQL y Entity Framework Core. |
| RNF-03 | La comunicación en tiempo real debe implementarse sobre WebSockets. |
| RNF-04 | La lógica de aplicación debe estructurarse con MediatR y enfoque CQRS. |
| RNF-05 | Los procesos asíncronos deben desacoplarse mediante RabbitMQ. |
| RNF-06 | La solución debe seguir arquitectura hexagonal o una variante compatible con arquitectura limpia. |
| RNF-07 | El dominio no debe depender de infraestructura ni de detalles del framework web. |
| RNF-08 | La aplicación debe incorporar logging, manejo de excepciones y validaciones. |
| RNF-09 | El backend debe alcanzar como meta académica una cobertura de pruebas de al menos 90%. |
| RNF-10 | La solución debe poder ejecutarse localmente mediante Docker Compose. |
| RNF-11 | El repositorio debe incluir pipeline de integración continua para compilación y ejecución de pruebas. |
| RNF-12 | Las interfaces web y móvil deben ser claras, utilizables y coherentes con los flujos principales del sistema, diferenciando los flujos de administración/operación de los flujos de participación. |
| RNF-13 | La autenticación y autorización base del sistema debe integrarse con Keycloak mediante tokens seguros. |
| RNF-14 | El sistema no debe almacenar contraseñas ni credenciales sensibles de usuarios en la base de datos propia de UMBRAL. |
| RNF-15 | El sistema debe soportar actualización de geolocalización cada 2 segundos durante partidas BDT iniciadas, sin bloquear la operación principal. |
| RNF-16 | El sistema debe permitir decodificar códigos QR desde imágenes capturadas o subidas por los participantes desde una app móvil. |
| RNF-17 | El canal de tiempo real debe soportar actualizaciones de lobby, preguntas, ranking, etapas, pistas, geolocalización y cambios de estado. |
| RNF-18  | La aplicación móvil de participantes debe permitir el uso de cámara o selección de imágenes para la subida de tesoros QR, solicitando los permisos correspondientes del dispositivo. |
| RNF-19  | La aplicación móvil de participantes debe solicitar permiso de geolocalización antes de compartir la ubicación durante partidas BDT iniciadas. |
| RNF-20  | La aplicación móvil de participantes debe consumir únicamente los contratos HTTP y de tiempo real definidos por el backend, sin acceder directamente a bases de datos ni duplicar reglas autoritativas del dominio. |
| RNF-21 | El acceso al backend debe realizarse a través de un API Gateway implementado con YARP, que actúa como punto único de entrada; ningún cliente accede directamente a los microservicios. El gateway valida el token JWT emitido por Keycloak (RNF-13) en cada petición, rechaza las peticiones no autenticadas y enruta todo el tráfico —incluido el de tiempo real (WebSockets/SignalR)— hacia el microservicio correspondiente.  |
| RNF-22 | El API Gateway debe aplicar la autorización de acceso por rol (Administrador, Operador, Participante) a nivel de ruta, usando los claims de rol contenidos en el token JWT, sin consultar a Identity en cada petición. La autorización fina por permisos funcionales (por ejemplo, “Gestionar partidas”) permanece en los microservicios.  |
| RNF-23 | El sistema debe disponer de una capacidad de envío de correo (notificaciones por correo) para entregar las contraseñas temporales a los usuarios. El envío debe realizarse de forma asíncrona mediante RabbitMQ (conforme a RNF-05), sin fijar un proveedor de correo específico.  |
| RNF-24 | Una vez autenticado el usuario, el front-end debe refrescar el token de Keycloak cada 270 segundos (4,5 minutos), directamente contra Keycloak y sin pasar por el backend ni por el API Gateway. El token de acceso de Keycloak tiene un tiempo de vida de 300 segundos (5 minutos), por lo que el refresco se realiza con 30 segundos de margen antes de su expiración. El front-end debe registrar de forma continua la actividad del usuario, entendida como cualquier interacción (toques, clicks, desplazamiento, teclado o navegación). Este comportamiento aplica a todos los usuarios autenticados, tanto en la aplicación web (administrador/operador) como en la aplicación móvil (participante). |

# **Clientes objetivo de las historias de usuario** {#heading}

Las historias cuyo actor principal sea Administrador u Operador corresponden a la aplicación web en React.

Las historias cuyo actor principal sea Participante corresponden a la aplicación móvil en React Native, salvo que se indique explícitamente lo contrario.

Las historias cuyo actor principal sea Sistema corresponden a lógica backend o procesamiento interno.

#  {#heading}

# 

# **Historias de usuario** {#historias-de-usuario}

| ID | Módulo | Historia de usuario | Actor principal | Criterios de aceptación | Prioridad |
| ----- | ----- | ----- | ----- | ----- | ----- |
| HU-01 | Usuarios y roles | Como Administrador, quiero crear usuarios en la plataforma y asignarles un rol inicial, para establecer y controlar los accesos seguros al sistema. | Administrador | El administrador puede crear usuarios. Todo usuario debe tener un rol inicial asignado durante la creación. El administrador le asigna a los usuarios un nombre de usuario y un correo durante la creación. Al crear el usuario, el sistema genera una contraseña temporal (con cambio obligatorio en el primer inicio de sesión) y la envía por correo  | Alta |
| HU-02 | Usuarios y roles | Como Administrador, quiero consultar y editar los datos generales (usuario y correo) de los usuarios y desactivarlos, para mantener actualizada y controlada la base de usuarios. | Administrador | El administrador puede consultar usuarios, editar sus datos generales (usuario y correo) y desactivarlos. Si el administrador modifica el correo del usuario y su contraseña temporal sigue vigente, el sistema genera una nueva contraseña temporal y la envía al nuevo correo; si la contraseña ya fue cambiada, no se envía nada  | Alta |
| HU-03 | Usuarios y roles | Como Administrador, quiero modificar el rol de un operador o participante, incluida su promoción a administrador, para reorganizar los accesos del sistema. | Administrador | El administrador puede cambiar el rol de usuarios operador o participante e incluso promoverlos a administrador. No puede modificar el rol de un usuario administrador. El cambio de rol se propaga a Keycloak. | Alta |
| HU-04 | Gobernanza | Como Administrador, quiero gestionar los permisos y privilegios de cada rol desde un panel de gobernanza, para adaptar lo que cada rol puede hacer. | Administrador | El administrador consulta y modifica, a nivel de rol, los privilegios de gobernanza y los permisos funcionales de Administrador, Operador y Participante. Los privilegios de gobernanza del rol Administrador están protegidos y no pueden retirarse. No se pueden crear roles nuevos. | Alta |
| HU-05 | Equipos | Como Participante, quiero crear un equipo, para participar en partidas (Trivia o BDT) de equipo. | Participante | El participante puede crear un equipo solo si no pertenece a otro. El creador queda registrado como líder. | Alta |
| HU-06 | Equipos | Como Líder de equipo, quiero eliminar el equipo que lidero, para cerrar el equipo cuando ya no deba seguir existiendo. | Participante | El líder puede eliminar su equipo aunque tenga integrantes. El sistema debe impedir la eliminación si el equipo está inscrito en una partida en estado lobby o participando en una partida en estado iniciada. Al eliminarse el equipo, todos los integrantes deben ser notificados y el historial previo debe conservarse.  | Alta |
| HU-07 | Equipos | Como Líder de equipo, quiero transferir el liderazgo antes de salir del equipo, para que el equipo pueda seguir existiendo. | Participante | Si el líder desea salir y hay otros participantes, debe elegir un nuevo líder. Si no hay más participantes, el equipo se elimina. | Alta |
| HU-08 | Equipos | Como Participante, quiero salir de mi equipo, para dejar de participar en él. | Participante | El participante puede salir del equipo. Si no es líder, sale directamente. Si es líder, debe transferir liderazgo o eliminarse el equipo si está solo. | Alta |
| HU-09 | Equipos | Como Administrador, quiero gestionar equipos, para mantener control administrativo sobre los equipos de la plataforma. | Administrador | El administrador puede crear, consultar, editar, desactivar y eliminar equipos. Si crea un equipo, debe asignar un líder válido y respetar las reglas de mínimo 1 integrante, máximo 5 integrantes y no pertenencia múltiple. Si modifica el liderazgo, se debe notificar al líder anterior y al nuevo líder. | Alta |
| HU-10 | Listado de partidas | Como Participante, quiero ver todas las partidas publicadas en un único panel, para encontrarlas en un solo lugar. | Participante | En la aplicación móvil, el participante cuenta con un único panel “Partidas” donde aparecen todas las partidas publicadas, sin importar el tipo de los juegos que contengan. | Alta |
| HU-11 | Filtros de partidas | Como Participante, quiero filtrar las partidas por modalidad individual o equipo, para encontrar las que me interesan. | Participante | El panel “Partidas” permite filtrar por “partidas individuales” y “partidas de equipo”. | Media |
| HU-12 | Acceso a partidas de equipo | Como Participante, quiero recibir una advertencia si intento entrar a una partida de equipo sin ser líder. | Participante | Si el participante no es líder de ningún equipo e intenta entrar a una partida de equipo, el sistema muestra: “Debes ser líder de un equipo para entrar en esta partida”. | Alta |
| HU-13 | Creación de juego de Trivia | Como Operador, quiero añadir un juego de Trivia a una partida creando sus preguntas en el momento, para preparar el contenido del juego sin depender de formularios. | Operador | El operador crea el juego de Trivia y define sus preguntas en ese momento; cada pregunta tiene opciones, respuesta correcta, puntaje y tiempo límite. No existe reutilización de preguntas ni banco de preguntas. El nombre de la partida, la modalidad, los mínimos/máximos de participación y el tiempo de inicio se definen a nivel de partida. | Alta |
| HU-14 | Unión a partida individual | Como Participante, quiero unirme a una partida individual publicada, para participar individualmente. | Participante | Cualquier participante puede inscribirse a una partida individual publicada mientras esté en lobby y no se haya alcanzado el limite de participantes para esa partida. El participante no puede unirse si ya tiene una participación activa en otra partida (inscripción individual o convocatoria de equipo aceptada); en ese caso el sistema muestra ‘Ya estás participando en otra partida ‘. | Alta |
| HU-15 | Unión a partida por equipo | Como Líder de equipo, quiero unir mi equipo a una partida por equipos, para participar con mi equipo. | Participante líder | Solo el líder puede unir el equipo. No debe superar el máximo de equipos. No puede unir el equipo si este ya tiene una inscripción activa en otra partida; en ese caso el sistema muestra ‘El equipo ya está inscrito en otra partida’  | Alta |
| HU-16 | Convocatoria a partida por equipo | Como Participante de equipo, quiero recibir una convocatoria cuando mi líder preinscriba al equipo a una partida, para aceptar o rechazar mi participación. | Participante | El sistema envía convocatoria a los integrantes del equipo. Cada integrante puede aceptar o rechazar; la respuesta afecta solo a esa partida y no cambia la pertenencia al equipo. Un integrante no puede aceptar la convocatoria si ya tiene una participación activa en otra partida; en ese caso el sistema muestra ‘Ya estás participando en otra partida’. | Alta |
| HU-17 | Pantalla de espera | Como Participante, quiero ver una pantalla de espera después de unirme a una partida. | Participante | En la aplicación móvil, el participante ve un panel de “espera” mientras la partida está en lobby. | Alta |
| HU-18 | Pantalla de espera | Como Operador quiero observar los participantes o equipos que solicitaron inscribirse a la partida publicada. | Operador | El panel muestra los equipos o participantes que entraron a la partida. El panel se muestra mientras la partida está en estado lobby. | Alta |
| HU-19 | Pantalla de espera | Como Operador quiero observar y aceptar o rechazar los participantes/equipos que solicitan inscribirse a la partida publicada. | Operador | El panel muestra los participantes/equipos que desean entrar a la partida y permite aceptarlos o rechazarlos mientras la partida está en lobby. | Media |
| HU-20 | Inicio de partida | Como Operador, quiero iniciar la partida, para comenzarla cuando se cumplan las condiciones de participación. | Operador | La partida puede iniciar manualmente o automáticamente al llegar el tiempo configurado, siempre que cumpla los mínimos de participación. Si no cumple los mínimos, no puede iniciar; si el inicio era automático, se cancela automáticamente. Al iniciar, sus juegos se activan de forma secuencial. | Alta |
| HU-21 | Ejecución sincronizada de Trivia | Como Participante, quiero que todos recibamos la misma pregunta al mismo tiempo durante un juego de Trivia, para competir bajo condiciones iguales. | Participante | Durante un juego de Trivia activo, todos los participantes ven la misma pregunta y opciones simultáneamente. El temporizador se sincroniza para todos. | Alta |
| HU-22 | Respuesta en Trivia individual | Como Participante, quiero seleccionar una única respuesta por pregunta. | Participante | En un juego de Trivia en modalidad individual, solo se acepta una respuesta por participante por pregunta. | Alta |
| HU-23 | Respuesta en Trivia por equipo | Como Participante de equipo, quiero poder responder una pregunta, para contribuir a la respuesta del equipo. | Participante | En un juego de Trivia en modalidad equipo, solo se acepta una respuesta por equipo. La respuesta válida será la primera opción seleccionada por cualquier participante del equipo. | Alta |
| HU-24 | Cierre de pregunta Trivia | Como Participante, quiero ver el resultado de la pregunta cuando se cierre, para saber cuál era la respuesta correcta. | Participante | Durante un juego de Trivia, la pregunta se cierra para todos cuando un participante/equipo responde correctamente o cuando expira el tiempo. Al cerrarse, el sistema muestra la respuesta correcta a todos, incluyendo a quienes no respondieron. | Alta |
| HU-25 | Puntaje Trivia | Como Participante, quiero que mi respuesta correcta sume el puntaje configurado para la pregunta, para conocer mi avance en el juego. | Participante | Solo se otorgan puntos si la respuesta es correcta. El puntaje obtenido corresponde al valor configurado para la pregunta y no depende del tiempo de respuesta. En caso de empate, el ranking del juego se ordena por menor tiempo acumulado de respuesta. | Alta |
| HU-26 | Panel operador Trivia | Como Operador, quiero ver el ranking de los participantes o equipos durante un juego de Trivia. | Operador | Durante un juego de Trivia activo, el operador solo ve el ranking actualizado del juego y un botón para cancelar la partida. | Alta |
| HU-27 | Historial de partidas | Como Participante, quiero consultar mi historial único de partidas jugadas, para revisar mis participaciones individuales y de equipo. | Participante | El historial muestra las partidas jugadas con sus juegos, la modalidad, la fecha y el resultado o posición obtenida; incluye el equipo asociado cuando se trate de una partida por equipos. | Media |
| HU-28 | Creación de juego BDT | Como Operador, quiero añadir un juego de Búsqueda del Tesoro a una partida, con sus etapas, el tesoro, el puntaje y el temporizador de cada etapa, para preparar la dinámica de búsqueda. | Operador | El operador define el área de búsqueda como texto y las etapas; cada etapa tiene el contenido textual esperado del QR, un puntaje y un tiempo límite. El nombre, la modalidad, los mínimos/máximos y el modo de inicio se definen a nivel de partida. | Alta |
| HU-29 | Panel de Operador | Como Operador, quiero ver la lista de partidas que fueron publicadas. | Operador | El operador debe poder consultar la lista de partidas publicadas, ver su nombre y estado. | Media |
| HU-30 | Panel de operador | Como Operador quiero poder ver el detalle de las partidas publicadas. | Operador | El operador debe poder acceder al detalle de una partida y ver toda su información, incluidos sus juegos. | Media |
| HU-31 | Panel participante BDT | Como Participante, quiero ver la etapa activa y la opción de subir tesoro durante un juego de Búsqueda del Tesoro. | Participante | El panel muestra la etapa actual, el temporizador y el botón “subir tesoro”. | Alta |
| HU-32 | Subida de tesoro BDT | Como Participante, quiero tomar o subir una foto del tesoro QR, para intentar validar la etapa activa. | Participante | El participante puede tomar o subir una foto desde la aplicación móvil. Puede realizar múltiples intentos durante la etapa hasta validar correctamente el QR esperado o hasta que la etapa se cierre. El sistema procesa la imagen enviada e intenta decodificar el contenido del QR detectado. | Alta |
| HU-33 | Validación de QR BDT | Como Sistema, quiero validar automáticamente el QR enviado, para garantizar la transparencia del juego sin intervención manual. | Sistema | Si el contenido decodificado coincide con el contenido esperado, el envío se marca como válido. Si no coincide, no puede leerse o no corresponde a la etapa activa, se marca como inválido. Todo envío queda registrado. | Alta |
| HU-34 | Cierre de etapa BDT | Como Participante, quiero que la etapa termine cuando alguien encuentre el tesoro o culmine el temporizador, para avanzar a la siguiente etapa. | Participante | La etapa termina para todos si un participante/equipo valida correctamente el QR esperado o si expira el tiempo configurado. Al cerrarse la última etapa del juego, el juego se finaliza y la partida activa el siguiente juego, o termina si era el último. | Alta |
| HU-35 | Resultado de etapa BDT | Como Participante, quiero saber quién encontró el tesoro de cada etapa y cuánto tiempo tardó en conseguirlo, para conocer el resultado de la etapa. | Participante | Si hubo ganador, se muestra quién consiguió el tesoro y en cuánto tiempo. Si nadie lo consigue, se muestra “nadie consiguió el tesoro”. | Alta |
| HU-36 | Pistas BDT | Como Operador, quiero enviar pistas a participantes o equipos durante un juego de BDT, para orientar su búsqueda. | Operador | El operador puede enviar pistas a participantes/equipos específicos. Las pistas quedan registradas. Las pistas son cadenas de texto. | Alta |
| HU-37 | Panel de operador en BDT | Como Operador, quiero ver un panel durante el juego de Búsqueda del Tesoro que permita cancelar la partida y seleccionar a un participante o equipo para enviarle una pista. | Operador | Durante un juego de BDT, el operador debe tener en su panel la opción de cancelar la partida y de enviar una pista a un participante o equipo. | Alta |
| HU-38 | Monitoreo BDT | Como Operador, quiero ver la lista de participantes/equipos y sus tesoros subidos, para supervisar el juego. | Operador | El panel muestra participantes/equipos, etapa actual, envíos realizados y si cada tesoro fue válido o inválido. | Alta |
| HU-39 | Geolocalización BDT | Como Operador, quiero ver en un mapa la geolocalización de los participantes durante un juego de BDT activo, para supervisar la búsqueda. | Operador | Una vez activo el juego, el operador ve un mapa con la ubicación de los participantes. El sistema debe solicitar autorización de ubicación al participante. La ubicación se actualiza cada 2 segundos mientras el juego de BDT esté activo. | Alta |
| HU-40 | Cancelación de partida | Como Operador, quiero cancelar una partida, para detener su ejecución cuando sea necesario. | Operador | El operador puede cancelar partidas en estado lobby o iniciada. Una partida cancelada no acepta nuevas acciones de juego. Sus partidas, puntajes y resultados parciales se conservan en historial, pero no cuentan como resultado final. | Alta |
| HU-41 | Cancelación de partida | Como Participante, quiero recibir una notificación si la partida se cancela, para saber que ya no puedo continuar jugando. | Participante | Si el operador cancela la partida, los participantes reciben una notificación dentro de la aplicación. La partida deja de aceptar acciones de juego y el historial se conserva visible. | Media |
| HU-42 | Tiempo real | Como Usuario autenticado, quiero recibir actualizaciones en tiempo real, para ver cambios sin recargar la página. | Operador / Participante | El sistema actualiza partidas publicadas, lobby, juegos, preguntas, ranking, etapas, temporizadores, pistas, geolocalización, resultados y estados en tiempo real. | Alta |
| HU-43 | Historial y trazabilidad | Como Operador, quiero consultar el historial de una partida, para auditar lo ocurrido. | Operador | El historial registra cambios de estado, inscripciones, convocatorias, invitaciones de equipo, activación y finalización de juegos, respuestas, puntajes, etapas, QR enviados, validaciones, pistas, ubicaciones relevantes, cancelaciones y ranking consolidado. | Alta |
| HU-44 | Ranking BDT | Como Participante u Operador, quiero ver el ranking de un juego de Búsqueda del Tesoro, para conocer la posición de participantes o equipos durante el juego. | Operador / Participante | El ranking del juego muestra participantes/equipos ordenados por el puntaje acumulado en el juego (suma de los puntos de las etapas ganadas). En caso de empate, por el menor tiempo total empleado en obtener los tesoros de esas etapas ganadas. La cantidad de etapas ganadas se muestra como dato informativo. El ranking es visible para operadores y participantes. | Alta |
| HU-45 | Creación de partida | Como Operador, quiero crear una partida compuesta por varios juegos en un orden secuencial, para diseñar una experiencia con uno o más juegos de Trivia o Búsqueda del Tesoro. | Operador | El operador define la secuencia de juegos (Juego 1, Juego 2, …), cada uno de tipo Trivia o Búsqueda del Tesoro, y fija una única modalidad (individual o equipo) para toda la partida. | Alta |
| HU-46 | Equipos | Como Líder de equipo, quiero invitar a otros participantes a mi equipo mediante una lista dinámica de participantes, para sumar integrantes. | Participante líder | La lista muestra a todos los participantes de la plataforma, excluye a quienes ya pertenecen a un equipo e impide invitar si el equipo está lleno (máximo 5). Solo el líder puede invitar. | Alta |
| HU-47 | Equipos | Como Participante, quiero ver y responder las invitaciones de equipo que recibo, para unirme a un equipo. | Participante | Todos los participantes pueden ver su lista de invitaciones recibidas. Al aceptar, el participante pasa a ser miembro del equipo. Si ya pertenece a un equipo, el sistema muestra “Ya perteneces a un equipo”; si el equipo está lleno, muestra “El equipo ya está lleno”. Las invitaciones no caducan. | Alta |
| HU-48 | Equipos | Como Participante, quiero ver el historial de equipos a los que he pertenecido, para recordar mi trayectoria. | Participante | El historial muestra los nombres de los equipos a los que ha pertenecido el participante. | Media |
| HU-49 | Equipos | Como Participante de un equipo, quiero ver el rendimiento de mi equipo en las partidas en las que ha participado, para conocer su desempeño. | Participante | Se muestra, para cada partida en la que participó el equipo, la posición obtenida en el ranking consolidado y si la ganó (es decir, si obtuvo la primera posición). | Media |
| HU-50 | Ranking | Como Participante u Operador, quiero ver el ranking consolidado de la partida al finalizar, para conocer la clasificación general. | Operador / Participante | Al finalizar la partida, se muestra un ranking consolidado que ordena a los participantes o equipos por número de juegos ganados; en caso de empate, por mayor puntaje total acumulado en todos los juegos; y, de persistir, por menor tiempo total. Cada juego lo gana quien obtuvo más puntaje en él (desempate por menor tiempo en el juego). | Alta |

# 

# **Actores** {#actores}

| ID | Actor | Descripción | Responsabilidades principales | Permisos mínimos esperados |
| ----- | ----- | ----- | ----- | ----- |
| AC-01 | Administrador | Usuario responsable de la configuración administrativa general del sistema, de la gobernanza de permisos por rol y de la gestión de accesos mediante la integración con Keycloak. | Crear usuarios desde UMBRAL mediante Keycloak; asignar rol inicial; consultar, editar datos generales (usuario y correo) y desactivar usuarios; modificar el rol de operadores y participantes (incluida la promoción a administrador) sin modificar el rol de un administrador; gestionar, a nivel de rol, los privilegios de gobernanza y los permisos funcionales desde el panel de gobernanza; consultar y gestionar equipos administrativamente; consultar información operativa cuando asi lo desee. | Acceder al módulo de administración y al panel de gobernanza; crear usuarios mediante Keycloak; asignar y modificar roles según las reglas; gestionar permisos y privilegios por rol; crear, consultar, editar, desactivar y eliminar equipos; consultar información general sin intervenir directamente en la operación de partidas. |
| AC-02 | Operador | Usuario encargado de preparar, configurar, publicar, ejecutar y supervisar partidas en vivo compuestas por uno o más juegos de tipo Trivia o Búsqueda del Tesoro. | Crear partidas compuestas por uno o más juegos en orden secuencial; añadir juegos de Trivia creando sus preguntas (opciones, respuesta correcta, puntaje y tiempo por pregunta) en el momento; añadir juegos de BDT; configurar etapas, QR esperado, puntaje y tiempo por etapa; publicar partidas; iniciar partidas; cancelar partidas; supervisar el ranking de cada juego; enviar pistas en BDT; visualizar tesoros subidos; visualizar geolocalización de participantes en BDT; consultar historial, ranking consolidado y eventos relevantes. | Acceder al panel de operador; crear partidas y sus juegos con sus preguntas/etapas; publicar partida; iniciar partida; cancelar partida; observar el ranking de juego y el ranking consolidado; enviar pistas; consultar tesoros subidos; consultar geolocalización BDT; consultar historial de partida. |
| AC-03 | Participante | Usuario autenticado que puede participar en partidas individuales, crear equipos o unirse a ellos por invitación, actuar como líder de equipo cuando sea el creador del equipo o cuando el liderazgo haya sido transferido hacia el y participar en partidas compuestas por juegos de Trivia o Búsqueda del Tesoro desde una aplicación móvil. | Visualizar el panel único de Partidas en la app móvil; consultar partidas publicadas; filtrar por modalidad; crear equipo; ver y responder invitaciones de equipo recibidas (al aceptar, pasar a ser miembro); salir de equipo; transferir liderazgo si es líder; invitar a otros participantes mediante la lista dinámica si es líder; consultar el rendimiento del equipo y el historial de equipos; inscribirse en partidas individuales; inscribir equipo si es líder; aceptar o rechazar convocatorias; responder preguntas de Trivia; subir tesoros QR en BDT; consultar el historial único de partidas y el ranking consolidado; permitir geolocalización durante los juegos de BDT.. | Acceder a la aplicación móvil de participante; ver partidas publicadas; participar en partidas individuales; gestionar su pertenencia a equipo y sus invitaciones; responder Trivia; subir tesoros en BDT; aceptar/rechazar convocatorias; consultar historial; compartir ubicación en juegos BDT activos previa autorización. |

## 

## *Consideraciones de acceso y dominio* {#consideraciones-de-acceso-y-dominio}

| Elemento | Aclaración |
| ----- | ----- |
| Autenticación | La autenticación será gestionada por Keycloak. UMBRAL no almacenará contraseñas ni credenciales sensibles. |
| Roles base | Los roles base del sistema son administrador, operador y participante. Provienen de Keycloak para autenticación y rol base; en UMBRAL, el administrador puede modificar los permisos y privilegios de cada rol y el rol de operadores/participantes según las reglas. |
| Usuario local | UMBRAL almacenará una referencia local al usuario autenticado mediante el identificador proveniente de Keycloak, con el fin de asociarlo a equipos, invitaciones de equipo, partidas, juegos, convocatorias, respuestas, tesoros, ubicaciones e historial. |
| Administrador | El administrador gestiona usuarios desde UMBRAL mediante integración con Keycloak, administra equipos y ejerce la gobernanza de permisos: modifica, a nivel de rol, los privilegios de gobernanza y los permisos funcionales, y modifica el rol de operadores y participantes (incluida la promoción a administrador), sin poder modificar el rol de un administrador. También puede consultar partidas, rankings, historial y detalles operativos en modo lectura, sin intervenir directamente en la ejecución de partidas. |
| Operador | El operador es el actor responsable de crear y operar los juegos. Puede crear partidas compuestas por juegos de Trivia y/o Búsqueda del Tesoro en orden secuencial, con sus preguntas de Trivia (creadas al crear el juego), etapas, QR esperados, puntajes por etapa, tiempos y pistas, así como publicar e iniciar partidas. |
| Participante | El participante puede visualizar partidas publicadas, jugar partidas individuales, crear equipos o unirse a ellos por invitación, aceptar convocatorias, responder preguntas de Trivia y subir tesoros QR en BDT. |
| Líder de equipo | El liderazgo de equipo no es un rol de Keycloak, sino una relación o atributo de negocio dentro de UMBRAL. El líder es quien creó el equipo o recibió transferencia de liderazgo. |
| Equipo | El equipo no es un actor independiente, sino una entidad del dominio. Agrupa participantes, tiene un líder y puede participar tanto en Trivia como en BDT. Los integrantes se suman mediante invitaciones de equipo. |
| Partidas publicadas | Todas las partidas publicadas se muestran a todos los participantes. La visibilidad de una partida no implica autorización automática para inscribirse. |
| Panel del participante  | El participante cuenta en la aplicación móvil con un único panel principal “Partidas” donde aparecen todas las partidas publicadas, sin importar el tipo de los juegos que contengan, con filtro por modalidad individual o equipo. |
| Partidas individuales | Un participante puede jugar partidas individuales aunque pertenezca a un equipo. |
| Partidas por equipo | Solo el líder puede inscribir un equipo en una partida por equipo. Si un participante no líder intenta entrar, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en esta partida”. |
| Partida y juegos  | Una partida está compuesta por uno o más juegos de tipo Trivia o Búsqueda del Tesoro, ejecutados en orden secuencial. El ciclo de vida, el lobby, la inscripción, la modalidad y la cancelación son de nivel partida; cada juego tiene un sub-estado interno (Pendiente/Activo/Finalizado). |
| Convocatorias | Cuando un líder inscribe su equipo en una partida por equipo, el sistema envía una convocatoria a los demás integrantes, quienes pueden aceptar o rechazar. La convocatoria afecta solo a la participación en esa partida y nunca cambia la pertenencia al equipo. |
| Invitaciones de equipo  | Los integrantes se suman a un equipo mediante invitaciones. El líder invita a participantes que aún no pertenecen a un equipo mediante una lista dinámica; al aceptar, el participante pasa a ser miembro. La invitación de equipo (pertenencia) es distinta e independiente de la convocatoria de partida (participación). |
| Historial y rendimiento de equipo | El sistema conserva, por participante, el historial de los equipos a los que ha pertenecido (sus nombres) y permite consultar el rendimiento del equipo en las partidas en las que participó, es decir, posición obtenida en cada partida y si la partida fue ganada o no. |
| Trivia | En un juego de Trivia, todos los participantes reciben la misma pregunta al mismo tiempo. El sistema valida automáticamente las respuestas y calcula el puntaje según la regla de negocio definida: una respuesta correcta suma directamente el puntaje asignado a la pregunta, sin ponderación por tiempo. El operador solo visualiza ranking y opción de cancelación durante el juego. |
| Búsqueda del Tesoro | En un juego de BDT, el participante sube una foto del QR encontrado. El sistema decodifica el QR y compara su contenido con el QR esperado de la etapa activa. El operador puede enviar pistas y supervisar tesoros subidos. |
| Ranking consolidado  | Al finalizar una partida se calcula un ranking consolidado que clasifica a los participantes o equipos por número de juegos ganados; en caso de empate, por el puntaje total acumulado en todos los juegos y, de persistir, por el menor tiempo total. Cada juego lo gana quien obtuvo más puntaje en él (desempate por menor tiempo en el juego) y conserva su ranking nativo, ordenado por puntaje.  |
| Geolocalización | En un juego de BDT activo, el sistema puede solicitar autorización de ubicación al participante y enviar su ubicación al operador cada 2 segundos para visualización en mapa. |
| Interacción móvil | La participación de los usuarios con rol Participante se contempla mediante una aplicación móvil desarrollada en React Native. La aplicación móvil será el cliente principal para visualizar partidas, gestionar equipos e invitaciones, inscribirse a partidas, responder Trivia, subir tesoros QR, recibir pistas y compartir geolocalización. |
| Aplicación web | La aplicación web estará orientada únicamente a los roles Administrador y Operador, permitiendo la gestión de usuarios, la gobernanza de permisos por rol, equipos, partidas (con sus preguntas de Trivia), su publicación, ranking, pistas, geolocalización operativa e historial. |
| Organización en microservicios | Los contextos acotados son límites lógicos que se materializan sobre cuatro microservicios de negocio: Partidas, Operaciones de sesión, Puntuaciones e Identity, ubicados detrás de un API Gateway (YARP) que actúa como punto único de entrada al backend. Los usuarios, roles base, permisos y privilegios por rol, equipos, invitaciones de equipo e historial de equipos se gestionan en Identity.  |
| API Gateway (YARP)  | El acceso al backend se realiza a través de un API Gateway implementado con YARP, que actúa como punto único de entrada. Valida el token JWT emitido por Keycloak y aplica autorización por rol (Administrador, Operador, Participante) a nivel de ruta usando los claims del token, sin consultar a Identity en cada petición; la autorización fina por permisos funcionales permanece en los microservicios. Enruta todo el tráfico, incluido el de tiempo real (WebSockets/SignalR), y es extensible a otras funciones de borde (limitación de tasa, balanceo de carga, terminación TLS) sin afectar la lógica de dominio.  |
| Gobernanza de permisos  | Existen dos niveles de autorización: privilegios de gobernanza (administración del sistema) y permisos funcionales (operación y participación). El administrador dispone de un panel de gobernanza para consultarlos y modificarlos a nivel de rol. Los privilegios de gobernanza del rol Administrador están protegidos y no se pueden crear roles nuevos.  |
| Permisos funcionales | Los permisos funcionales se agrupan en “Gestionar partidas” (operación completa de partidas y su contenido), “Gestionar equipos” y “Participar en partidas”; tener un permiso implica todas las acciones que agrupa. Por defecto: Administrador con los privilegios de gobernanza; Operador con “Gestionar partidas”; Participante con “Gestionar equipos” y “Participar en partidas”. |

# **Reglas de negocio** {#reglas-de-negocio}

## *Reglas de negocio generales* {#reglas-de-negocio-generales}

| ID | Regla de negocio |
| ----- | ----- |
| RB-01 | El sistema solo permite dos tipos de juego dentro de una partida: Trivia y Búsqueda del Tesoro. |
| RB-02 | En la aplicación móvil del participante debe existir un único panel principal: Partidas. |
| RB-03 | El panel Partidas de la aplicación móvil del participante debe mostrar la lista de todas las partidas publicadas, sin importar el tipo de sus juegos. |
| RB-04 | El panel Partidas de la aplicación móvil del participante debe permitir filtrar partidas por modalidad: individual o equipo. |
| RB-05 | Todas las partidas publicadas deben mostrarse a todos los participantes, sin importar si son individuales o por equipo. |
| RB-06 | Si una partida es de equipo y el participante no es líder de ningún equipo, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en esta partida”. |
| RB-07 | Las partidas solo pueden tener los estados lobby, iniciada, cancelada o terminada. |
| RB-08 | Una partida en estado lobby permite inscripción de participantes o equipos. |
| RB-09 | Una partida en estado iniciada permite acciones propias del juego, como responder preguntas o subir tesoros. |
| RB-10 | Una partida en estado cancelada no acepta nuevas inscripciones, respuestas, tesoros, pistas ni cambios de participación. |
| RB-11 | Una partida en estado terminada no acepta nuevas acciones de juego. |
| RB-12 | Toda transición de estado debe ser validada por el sistema antes de aplicarse. |
| RB-13 | El operador es el único actor autorizado para crear juegos, partidas, preguntas, etapas, pistas y configuración operativa de Trivia o BDT. |
| RB-14 | El operador puede cancelar una partida si se encuentra en un estado válido para cancelación. |
| RB-15 | Las acciones relevantes deben registrarse en el historial de la partida. |
| RB-16 | Los cambios importantes deben publicarse en tiempo real para los usuarios afectados. |
| RB-17 | El sistema debe diferenciar las funcionalidades según el rol autenticado: administrador, operador o participante. |
| RB-18 | Los participantes pueden jugar partidas individuales aunque pertenezcan a un equipo. |
| RB-19 | Un participante que pertenece a un equipo solo puede jugar partidas de equipo si su líder une al equipo y el participante acepta la convocatoria. |
| RB-20 | En partidas individuales, el operador define el máximo de participantes. |
| RB-21 | En partidas por equipo, el operador define el máximo de equipos. |
| RB-22 | En partidas por equipo, el operador puede definir cantidad mínima y máxima de participantes por equipo para esa partida. |
| RB-23 | Una partida no puede iniciar si no cumple los mínimos configurados por el operador. |
| RB-24 | El sistema debe conservar trazabilidad de puntajes, respuestas, tesoros, validaciones, pistas, estados y resultados. |
| RB-25 | Las acciones de participación directa —gestión de equipo e invitaciones como participante, inscripción, respuesta de Trivia, subida de tesoro QR, recepción de pistas y envío de geolocalización— se realizarán desde la aplicación móvil de participantes. |
| RB-26 | El modo de inicio de la partida (Manual, Automatico, ManualYAutomatico) determina su arranque, y en todos los modos el inicio exige cumplir los mínimos de participación. En Manual, la partida solo inicia cuando el operador la inicia; no utiliza tiempo de inicio ni se cancela automáticamente por tiempo. En Automatico, inicia automáticamente al llegar el tiempo de inicio si cumple los mínimos; si no los cumple en ese momento, se cancela automáticamente. En ManualYAutomatico, el operador puede iniciarla manualmente antes del tiempo de inicio y, si no lo ha hecho al llegar dicho tiempo, el sistema la inicia automáticamente si cumple los mínimos o la cancela automáticamente si no. El tiempo de inicio solo aplica a los modos Automatico y ManualYAutomatico. |
| RB-28 | Las partidas pueden configurarse para iniciar manualmente, automáticamente por tiempo o bajo ambas modalidades, según lo defina el operador durante la creación de la partida. |
| RB-29 | El operador puede cancelar una partida únicamente si se encuentra en estado lobby o iniciada. |
| RB-30 | Una partida cancelada conserva sus juegos, puntajes y resultados parciales en el historial, pero estos no cuentan como resultado final de partida. |
| RB-31 | La cancelación de una partida no elimina el historial visible de los usuarios afectados. |
| RB-32 | Las notificaciones del sistema se resolverán dentro de la aplicación mediante comunicación en tiempo real. Las notificaciones push del sistema operativo quedan fuera del alcance de esta versión. |
| RB-33 | Un participante puede reconectarse a una partida iniciada mientras la partida continúe en estado iniciada, recuperando el estado vigente que le corresponda según su rol, equipo, inscripción, convocatoria, modalidad y juego activo. |
| RB-34 |   Una partida está compuesta por uno o más juegos ordenados secuencialmente (Juego 1, Juego 2, …), definidos por el operador al crearla; cada juego es de tipo Trivia o Búsqueda del Tesoro. |
| RB-35 | La modalidad (individual o por equipo) se fija una sola vez para toda la partida y aplica a todos sus juegos. La partida tiene un único lobby como fase (una única fase de inscripción a nivel de partida, no por juego); puede registrar múltiples inscripciones según la modalidad (una por participante en individual; una por equipo en partidas por equipo). |
| RB-36 |   Al iniciar la partida, sus juegos se activan de forma secuencial, uno tras otro, en el orden definido. Cada juego maneja un sub-estado interno propio: Pendiente, Activo o Finalizado. |
| RB-37 |   La cancelación aplica a toda la partida, no a un juego individual. |
| RB-38 |   Al finalizar el último juego de la partida, la partida pasa a estado terminada. |
| RB-39 | Cada juego mantiene su ranking nativo ordenado por puntaje acumulado en el juego: Trivia por la suma de los puntos de las preguntas ganadas (desempate por menor tiempo acumulado de respuesta) y Búsqueda del Tesoro por la suma de los puntos de las etapas ganadas (desempate por menor tiempo acumulado únicamente de las etapas ganadas). El participante o equipo con mayor puntaje en un juego es su ganador; en caso de empate, lo gana quien empleó menor tiempo en él y, si persiste, el juego no otorga victoria. La cantidad de etapas ganadas en BDT se conserva como dato informativo. |
| RB-40 | Al finalizar la partida, el sistema calcula un ranking consolidado que ordena a los participantes o equipos por número de juegos ganados; en caso de empate, por el mayor puntaje total acumulado en todos los juegos (Trivia más etapas ganadas en BDT); y, de persistir, por el menor tiempo total (tiempos de las preguntas de Trivia ganadas más tiempos de las etapas de BDT ganadas).  |
| RB-41 |   El ranking consolidado no reemplaza el ranking nativo de cada juego; ambos coexisten. |
| RB-42 | Existen dos niveles de autorización: privilegios de gobernanza (administración del sistema) y permisos funcionales (operación y participación). |
| RB-43 | Los permisos y privilegios se administran a nivel de rol, no por usuario individual. |
| RB-44 | El administrador dispone de un panel de gobernanza para consultar y modificar los permisos y privilegios de cada rol (Administrador, Operador, Participante). |
| RB-45 | Los privilegios de gobernanza del rol Administrador están protegidos y no pueden retirarse. |
| RB-46 | No se pueden crear roles nuevos; solo existen Administrador, Operador y Participante. |
| RB-47 | Los permisos funcionales se agrupan en “Gestionar partidas”, “Gestionar equipos” y “Participar en partidas”; contar con un permiso implica poder realizar todas las acciones que agrupa. |
| RB-48 | Una inscripción se considera activa mientras su partida esté en estado lobby o iniciada y su estado sea preinscrita o confirmada (no cancelada ni excluida). Una convocatoria de equipo se considera activa mientras esté aceptada y su partida esté en estado lobby o iniciada.  |
| RB-49 | Un equipo puede tener como máximo una inscripción activa a la vez. Un participante puede tener como máximo una participación activa a la vez, entendida como su inscripción individual activa o una convocatoria de equipo aceptada y activa; mientras la mantenga, no puede inscribirse individualmente en otra partida ni aceptar otra convocatoria.   |
| RB-50 | Si un participante intenta inscribirse individualmente o aceptar una convocatoria teniendo ya una participación activa en otra partida, el sistema debe rechazarlo y mostrar “Ya estás participando en otra partida”. Si un líder intenta preinscribir un equipo que ya tiene una inscripción activa, el sistema debe rechazarlo y mostrar “El equipo ya está inscrito en otra partida”.  |

## *Reglas de negocio de inicio de partidas* {#reglas-de-negocio-de-inicio-de-partidas}

| ID | Regla de negocio |
| ----- | ----- |
| RB-C01 | Cuando un líder inscribe su equipo en una partida por equipos, el equipo queda preinscrito. La inscripción se confirma al momento de iniciar la partida si cumple los mínimos configurados por el operador. |
| RB-C02 | En partidas por equipo, solo los integrantes que aceptan la convocatoria cuentan como participantes activos de esa partida. |
| RB-C03 | El mínimo de participantes por equipo se calcula sobre los integrantes que aceptaron la convocatoria, no sobre la cantidad total de integrantes del equipo. |
| RB-C04 | Si un integrante rechaza una convocatoria, no participa en esa partida, pero conserva su pertenencia al equipo. |
| RB-C05 | Si un equipo preinscrito no alcanza el mínimo de participantes aceptados requerido por el operador antes del inicio, no podrá participar en la partida. |
| RB-C06  | Un participante no puede aceptar una convocatoria si ya tiene una participación activa en otra partida (su inscripción individual activa u otra convocatoria aceptada y activa); en ese caso el sistema muestra “Ya estás participando en otra partida”. |

##  {#heading-1}

## *Reglas de negocio de equipos* {#reglas-de-negocio-de-equipos}

| ID | Regla de negocio |
| ----- | ----- |
| RB-E01 | Los equipos son globales para toda la aplicación y se usan tanto en Trivia como en BDT. |
| RB-E02 | Todo participante puede crear un equipo si no pertenece a otro. |
| RB-E05 | El participante que crea el equipo queda registrado automáticamente como líder. |
| RB-E06 | Un participante solo puede pertenecer a un equipo a la vez. |
| RB-E07 | Un equipo puede existir con mínimo 1 integrante y máximo 5 integrantes. El participante que crea el equipo cuenta como primer integrante y queda registrado automáticamente como líder. |
| RB-E08 | Los participantes pueden salir de su equipo. |
| RB-E09 | Si un participante no líder sale del equipo, simplemente deja de pertenecer al equipo. |
| RB-E10 | Si el líder desea salir y existen otros integrantes, debe transferir el liderazgo a otro participante antes de salir. |
| RB-E11 | Si el líder desea salir y no existen otros integrantes, el equipo se elimina. |
| RB-E12 | El administrador puede crear, consultar, editar, desactivar y eliminar equipos. Cuando el administrador cree un equipo, debe asignar un líder válido y respetar las invariantes del dominio: mínimo 1 integrante, máximo 5 integrantes y participantes que no pertenezcan a otro equipo activo. |
| RB-E13 | Un equipo desactivado no puede inscribirse en nuevas partidas. |
| RB-E14 | El líder es el único autorizado para inscribir al equipo en partidas de equipo. |
| RB-E15 | El líder puede eliminar su equipo aunque tenga integrantes. Al eliminarse el equipo, todos los integrantes deben ser notificados y dejan de pertenecer al equipo. |
| RB-E16 | Un equipo no puede eliminarse si se encuentra inscrito en una partida en estado lobby o si está participando en una partida en estado iniciada. |
| RB-E17 | La eliminación de un equipo no elimina ni modifica el historial de partidas, participaciones, puntajes o partidas ya registrados. |
| RB-E18 |    Los integrantes se suman a un equipo mediante invitaciones de equipo. Solo el líder puede invitar. |
| RB-E19 |    El líder invita mediante una lista dinámica que muestra a todos los participantes de la plataforma, excluyendo a quienes ya pertenecen a un equipo. |
| RB-E20 |    El sistema impide invitar si el equipo ya está lleno (cinco integrantes). |
| RB-E21 |    Al aceptar una invitación, el participante pasa a ser miembro del equipo. |
| RB-E22 |    Las invitaciones de equipo recibidas son visibles para todos los participantes, tengan o no equipo, sean o no líderes. |
| RB-E23 |    Si un participante intenta aceptar una invitación cuando ya pertenece a un equipo, el sistema muestra “Ya perteneces a un equipo” y la invitación permanece pendiente. |
| RB-E24 |    Si un participante intenta aceptar una invitación cuando el equipo ya está lleno, el sistema muestra “El equipo ya está lleno” y la invitación permanece pendiente. |
| RB-E25 |    Las invitaciones de equipo no caducan por tiempo. |
| RB-E26 |    Al eliminarse un equipo, se eliminan todas sus invitaciones pendientes. |
| RB-E27 |    El sistema conserva, por participante, el historial de los equipos a los que ha pertenecido, registrando los nombres de dichos equipos. |
| RB-E28 |    La invitación de equipo (que determina la pertenencia) es independiente de la convocatoria de partida (que solo afecta la participación en una partida). |

## *Reglas de negocio de usuarios y roles* {#reglas-de-negocio-de-usuarios-y-roles}

| ID | Regla de negocio |
| ----- | ----- |
| RB-U01 | La autenticación de usuarios será gestionada por Keycloak. |
| RB-U02 | Los roles base del sistema serán administrados mediante Keycloak: administrador, operador y participante. |
| RB-U03 | UMBRAL no almacenará contraseñas ni credenciales sensibles de usuarios en su base de datos. |
| RB-U04 | UMBRAL almacenará una referencia local al usuario autenticado mediante el identificador proveniente de Keycloak. |
| RB-U05 | El administrador podrá crear usuarios desde UMBRAL mediante integración con Keycloak. |
| RB-U06 | El administrador deberá asignar un rol inicial al usuario durante su creación. |
| RB-U07 | El administrador podrá modificar el rol de un usuario operador o participante después de su creación, incluida su promoción a administrador; no podrá modificar el rol de un usuario administrador. El cambio de rol se propaga a Keycloak. |
| RB-U08 | El administrador podrá consultar, editar datos generales (usuario y correo) y desactivar usuarios vinculados a Keycloak. |
| RB-U09 | Un usuario desactivado no podrá acceder a partidas ni ejecutar acciones dentro del sistema. |
| RB-U10 | El liderazgo de equipo no constituye un rol de Keycloak, sino una condición de negocio administrada dentro de UMBRAL. |
| RB-U11 | La gestión de equipos, invitaciones de equipo e historial de equipos forma parte del microservicio Identity, junto con los usuarios y los roles base. |
| RB-U12 | La administración de roles, permisos y privilegios por rol forma parte del microservicio Identity. Keycloak conserva la autenticación y el rol base; UMBRAL mantiene la matriz de permisos y privilegios por rol y la sincroniza con Keycloak cuando cambia el rol de un usuario. |
| RB-U13 | Al crear un usuario, el sistema genera una contraseña temporal, la fija en Keycloak con cambio obligatorio en el primer inicio de sesión y la envía por correo. UMBRAL no almacena la contraseña; solo registra que la credencial está en estado temporal pendiente.  |
| RB-U14 | Si el administrador modifica el correo de un usuario cuya credencial sigue en estado temporal pendiente, el sistema genera una nueva contraseña temporal, invalida la anterior y la envía al nuevo correo. Cuando el usuario cambia su contraseña, la credencial pasa a definitiva y deja de reenviarse. La desactivación de un usuario no genera envío de correo.  |

## 

## *Reglas de negocio de trivias* {#reglas-de-negocio-de-trivias}

| ID | Regla de negocio |
| ----- | ----- |
| RB-T01 | Solo el operador puede crear juegos de Trivia. |
| RB-T02 | Al añadir un juego de Trivia, el operador crea sus preguntas en el momento (opciones, respuesta correcta, puntaje y tiempo por pregunta); el nombre, la modalidad, los mínimos y máximos de participación y el tiempo de inicio se definen a nivel de partida. |
| RB-T03 | Si la partida es individual, el máximo configurado corresponde a cantidad máxima de participantes. |
| RB-T04 | Si la partida es por equipo, el máximo configurado corresponde a cantidad máxima de equipos. |
| RB-T05 | Si la partida es por equipo, el operador define mínimo y máximo de participantes por equipo para esa partida. |
| RB-T06 | Al publicar la partida, esta pasa a estado lobby y queda visible para todos los participantes en el panel único Partidas. |
| RB-T07 | Cualquier participante puede intentar entrar a una partida publicada. |
| RB-T08 | Si la partida es individual, cualquier participante puede inscribirse mientras la partida esté en lobby y haya cupo. |
| RB-T09 | Si la partida es por equipo, solo el líder puede inscribir al equipo. |
| RB-T10 | Si un participante que no es líder intenta entrar a una partida por equipo, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en esta partida”. |
| RB-T11 | Cuando un líder inscribe a su equipo en una partida por equipo, el sistema envía convocatoria a los integrantes del equipo. |
| RB-T12 | Los integrantes convocados pueden aceptar o rechazar la convocatoria. |
| RB-T13 | La partida inicia cuando se cumple el tiempo definido por el operador o cuando el operador la inicia manualmente. |
| RB-T14 | Al iniciar la partida, esta cambia a estado iniciada y activa su primer juego según el orden definido. |
| RB-T15 | Durante un juego de Trivia activo, todos los participantes reciben la misma pregunta al mismo tiempo. |
| RB-T16 | Todas las preguntas tienen un tiempo límite propio, definido al crear el juego de Trivia. |
| RB-T17 | En un juego de Trivia en modalidad individual, cada participante solo puede enviar una respuesta por pregunta activa. |
| RB-T18 | En un juego de Trivia en modalidad por equipos, solo puede registrarse una respuesta por equipo por pregunta activa. |
| RB-T19 | En un juego de Trivia en modalidad por equipos, la respuesta válida del equipo será la primera opción seleccionada por cualquier integrante activo del equipo. |
| RB-T20 | El sistema debe rechazar respuestas repetidas, tardías o enviadas fuera de la pregunta activa. Si un participante/equipo responde incorrectamente, no puede volver a intentar responder la misma pregunta. |
| RB-T21 | La pregunta activa se cierra para todos cuando algún participante/equipo responde correctamente o cuando se agota el tiempo límite. |
| RB-T22 | Al cerrarse una pregunta, el sistema debe mostrar la respuesta correcta a todos los participantes, incluyendo a quienes no alcanzaron a responder. |
| RB-T23 | Al cerrarse una pregunta, el sistema avanza automáticamente a la siguiente pregunta si existe. |
| RB-T24 | El puntaje se otorga únicamente cuando la respuesta es correcta. |
| RB-T25 | El puntaje de una respuesta correcta debe ser igual al puntaje asignado a la pregunta por el operador. El tiempo restante, el tiempo empleado o el tiempo total de la pregunta no modifican el puntaje obtenido. |
| RB-T26 | El ranking del juego de Trivia debe actualizarse en tiempo real. |
| RB-T27 | Durante un juego de Trivia activo, el operador solo visualiza el ranking del juego y la opción de cancelar la partida. |
| RB-T28 | Los participantes deben poder consultar el historial único de partidas jugadas, que incluye sus juegos de Trivia individuales y por equipo. |
| RB-T29 | El historial de partidas debe mostrar los juegos, la modalidad, la fecha, el resultado/posición obtenida y el equipo asociado cuando se trate de una partida por equipos. |
| RB-T30 | Para Trivia, el tiempo límite de cada pregunta cumple una función de control de disponibilidad, sincronización y rechazo de respuestas tardías. El tiempo no forma parte del cálculo de puntaje. El puntaje de una respuesta correcta corresponde directamente al puntaje asignado a la pregunta por el operador. |
| RB-T31 | En caso de empate en puntaje dentro del ranking de un juego de Trivia, se desempata por menor tiempo acumulado de respuesta. Este tiempo solo se usa como criterio de desempate y no modifica el puntaje obtenido por cada respuesta correcta. |
| RB-T32 | En un juego de Trivia por equipos, el puntaje se asigna al equipo, no individualmente a cada integrante. |
| RB-T33 | Solo el operador puede crear juegos de Trivia y sus preguntas. |
| RB-T34 | Las preguntas de un juego de Trivia se crean al momento de crear el juego e incluyen opciones, respuesta correcta, puntaje y tiempo límite por pregunta. No existe reutilización de preguntas ni banco de preguntas. |
| RB-T35 | No se puede publicar una partida con un juego de Trivia que no tenga al menos una pregunta completa. |

## *Reglas de búsqueda de tesoro* {#reglas-de-búsqueda-de-tesoro}

| ID | Regla de negocio |
| ----- | ----- |
| RB-B01 | Solo el operador puede crear juegos de Búsqueda del Tesoro. |
| RB-B02 | Un juego de Búsqueda del Tesoro hereda la modalidad (individual o por equipos) de la partida que lo contiene. |
| RB-B03 | Al añadir un juego de BDT, el operador debe definir el área de búsqueda; el nombre, la modalidad y los mínimos/máximos de participación se definen a nivel de partida. |
| RB-B04 | Si la partida es individual, el máximo configurado corresponde a cantidad máxima de participantes. |
| RB-B05 | Si la partida es por equipo, el máximo configurado corresponde a cantidad máxima de equipos. |
| RB-B06 | Si la partida es por equipo, el operador define la cantidad mínima de participantes por equipo para esa partida. |
| RB-B07 | El operador debe definir las etapas del juego de BDT durante su creación. |
| RB-B08 | Cada etapa debe tener un tesoro configurado como código QR (contenido textual esperado). |
| RB-B09 | Cada etapa debe tener un tiempo límite definido por el operador. |
| RB-B10 | No se puede publicar una partida con un juego de BDT sin al menos una etapa válida. |
| RB-B11 | No se puede publicar una etapa de BDT sin QR esperado, sin puntaje y sin tiempo límite. |
| RB-B12 | Al publicar la partida, esta pasa a estado lobby y queda visible para todos los participantes en el panel único Partidas. |
| RB-B13 | Cualquier participante puede intentar entrar a una partida publicada. |
| RB-B14 | Si la partida es individual, cualquier participante puede inscribirse mientras la partida esté en lobby y haya cupo. |
| RB-B15 | Si la partida es por equipo, solo el líder puede inscribir al equipo. |
| RB-B16 | Si un participante que no es líder intenta entrar a una partida por equipo, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en esta partida”. |
| RB-B17 | Cuando un líder inscribe a su equipo en una partida por equipo, el sistema envía convocatoria a los integrantes del equipo. |
| RB-B18 | Los integrantes convocados pueden aceptar o rechazar la convocatoria. |
| RB-B19 | Cuando el juego de BDT se activa según el orden de la partida, se activa su primera etapa. |
| RB-B20 | Durante un juego de BDT activo, el participante debe tener disponible en la aplicación móvil la opción “subir tesoro”. |
| RB-B21 | Subir tesoro implica tomar o cargar desde la aplicación móvil una foto que contiene el supuesto QR encontrado. |
| RB-B22 | Al subir un tesoro, el sistema debe procesar la imagen enviada por el participante y decodificar el contenido del QR detectado. |
| RB-B23 | El sistema debe comparar el contenido decodificado del QR subido con el contenido esperado del QR configurado para la etapa activa. |
| RB-B24 | Si el contenido decodificado del QR coincide con el contenido esperado de la etapa activa, el tesoro se considera válido. |
| RB-B25 | Si el contenido decodificado del QR no coincide, no puede leerse o no corresponde a la etapa activa, el tesoro se considera inválido. |
| RB-B26 | Todo tesoro subido debe quedar registrado con participante/equipo, partida, juego, etapa, fecha/hora y resultado de validación. |
| RB-B27 | Si un participante/equipo encuentra el tesoro correcto, gana la etapa. |
| RB-B28 | Cuando un participante/equipo gana la etapa, la etapa se cierra para todos. |
| RB-B29 | Al cerrar una etapa con ganador, el sistema muestra quién consiguió el tesoro y en cuánto tiempo. |
| RB-B30 | Si se agota el tiempo de la etapa sin ganador, la etapa se cierra automáticamente. |
| RB-B31 | Si nadie consiguió el tesoro antes de agotarse el tiempo, el sistema muestra: “nadie consiguió el tesoro”. |
| RB-B32 | Al cerrarse una etapa, el juego avanza a la siguiente etapa si existe. |
| RB-B33 | Si se cierra la última etapa del juego, el juego se finaliza; la partida activa el siguiente juego si existe o pasa a estado terminada si era el último. |
| RB-B34 | El operador puede enviar pistas a participantes o equipos durante un juego de BDT activo. |
| RB-B35 | El operador puede elegir a qué participante/equipo enviar una pista. |
| RB-B36 | Las pistas enviadas deben quedar registradas en el historial. |
| RB-B37 | El operador debe ver la lista de participantes/equipos inscritos en la partida. |
| RB-B38 | El operador debe ver cada tesoro subido y si fue válido o inválido. |
| RB-B39 | Mientras un juego de BDT está activo, el operador debe ver un mapa con la geolocalización de los participantes. |
| RB-B40 | El sistema debe solicitar permiso de ubicación al participante desde la aplicación móvil antes de compartir su geolocalización durante un juego de BDT. |
| RB-B41 | Durante un juego de BDT activo, la ubicación de los participantes debe actualizarse cada 2 segundos y mostrarse en el mapa del operador. |
| RB-B42 | En cada juego de BDT debe existir un ranking visible para operadores y participantes. |
| RB-B43 | El ranking del juego de BDT se calcula por el puntaje acumulado en el juego (suma de los puntos de las etapas ganadas). En caso de empate, se desempata por el menor tiempo acumulado únicamente de las etapas ganadas. La cantidad de etapas ganadas se conserva como dato informativo. |
| RB-B44 | Un participante/equipo puede realizar múltiples intentos de subida de tesoro durante una misma etapa, hasta que valide correctamente el QR esperado o hasta que la etapa se cierre. |
| RB-B45 | En BDT por equipos, si cualquier integrante activo del equipo sube correctamente el QR esperado, la etapa se considera ganada por todo el equipo. |
| RB-B46 | Cuando un participante/equipo valida correctamente el QR esperado, la etapa se cierra inmediatamente para todos los participantes. |
| RB-B47 | El sistema almacena como QR esperado el contenido textual esperado del código QR, no necesariamente la imagen del QR. |
| RB-B48 | El área de búsqueda será representada como texto descriptivo simple. En esta versión no se validará mediante coordenadas, polígonos ni restricciones geográficas automáticas. |
| RB-B49 | La geolocalización es obligatoria para participar en un juego de BDT activo. Si el participante no concede permiso de ubicación, no podrá participar en la dinámica de BDT. |
| RB-B50 |  Cada etapa de Búsqueda del Tesoro tiene un puntaje configurado por el operador al crearla. |
| RB-B51 |  Cada etapa ganada otorga su puntaje configurado, que se utiliza para el ranking consolidado de la partida. |

# **Alcance** {#alcance}

El alcance del sistema UMBRAL comprende el desarrollo de una solución compuesta por una aplicación web para administración y operación, una aplicación móvil para participantes y servicios backend para la gestión y operación en tiempo real de partidas interactivas compuestas por uno o más juegos de tipo Trivia o Búsqueda del Tesoro.

La interacción de los participantes será resuelta mediante una aplicación móvil desarrollada en React Native, mientras que las funcionalidades de administrador y operador serán resueltas mediante una aplicación web.

Cada juego de una partida deberá ser exactamente de uno de los dos tipos soportados: Trivia o Búsqueda del Tesoro. A partir de esta definición, la plataforma permitirá centralizar los procesos de autenticación y acceso, gestión de equipos e invitaciones, creación de partidas compuestas por juegos de Trivia y/o Búsqueda del Tesoro en orden secuencial, publicación de lobbies, inscripción de participantes o equipos, convocatorias, ejecución de dinámicas, validación de respuestas o tesoros, cálculo de puntajes, actualización del ranking de cada juego y del ranking consolidado, geolocalización operativa en BDT y trazabilidad de eventos relevantes.

El sistema cubrirá los flujos principales de administración, operación y participación, diferenciando las funcionalidades comunes de la plataforma y los comportamientos específicos de cada modo de juego.

| Área incluida | Descripción del alcance |
| ----- | ----- |
| Gestión de usuarios y roles | El sistema se integrará con Keycloak para autenticar usuarios y administrar roles base. UMBRAL permitirá crear usuarios mediante dicha integración, asignar rol inicial, consultar/editar datos generales (usuario y correo), desactivar usuarios y almacenar únicamente una referencia local al identificador proveniente de Keycloak. |
| Gestión de equipos | El sistema permitirá crear equipos, invitar integrantes mediante una lista dinámica de participantes (excluyendo a quienes ya tienen equipo e impidiendo invitar si el equipo está lleno), limitar cada equipo a cinco participantes, registrar líder, transferir liderazgo, salir de equipos, conservar el historial de equipos del participante y gestionar equipos administrativamente. Los equipos serán comunes para Trivia y Búsqueda del Tesoro. |
| Gestión de invitaciones de equipo  | El sistema permitirá al líder invitar participantes mediante una lista dinámica, hacer que un participante pase a ser miembro al aceptar la invitación, mostrar las invitaciones recibidas a todos los participantes y aplicar las reglas de invitación (sin caducidad; eliminación de invitaciones pendientes al borrar el equipo; mensajes de advertencia al aceptar cuando ya se tiene equipo o el equipo está lleno). |
| Gestión de partidas | El sistema permitirá crear partidas compuestas por uno o más juegos de tipo Trivia o Búsqueda del Tesoro en orden secuencial, con una única modalidad (individual o por equipos) por partida, y manejar únicamente los estados lobby, iniciada, cancelada y terminada a nivel de partida, además de un sub-estado interno (Pendiente/Activo/Finalizado) por juego. |
| Panel del participante | El participante contará en la aplicación móvil con un único panel principal: Partidas. En él podrá ver todas las partidas publicadas, filtrar por modalidad individual o equipo, inscribirse cuando asi lo desee, aceptar o rechazar convocatorias y acceder a la dinámica activa. |
| Panel del operador | El operador contará con una aplicación web desde la cual podrá crear partidas con sus juegos (incluidas las preguntas de Trivia), publicar partidas, iniciar partidas, cancelar partidas, visualizar el ranking de cada juego y el ranking consolidado, enviar pistas en BDT, consultar tesoros subidos y visualizar geolocalización de participantes durante juegos BDT activos. |
| Partidas individuales | El sistema permitirá que los participantes participen individualmente aunque pertenezcan a un equipo. En estas partidas, el máximo configurado por el operador corresponde a cantidad máxima de participantes. |
| Partidas por equipo | El sistema permitirá que solo el líder inscriba un equipo en partidas por equipo. Al inscribirlo, se enviarán convocatorias a los integrantes del equipo. En estas partidas, el máximo configurado por el operador corresponde a cantidad máxima de equipos. |
| Trivia | El sistema permitirá crear juegos de Trivia con sus preguntas (opciones, respuesta correcta, puntaje y tiempo por pregunta), creadas al añadir el juego; sincronizar preguntas; validar respuestas; calcular puntaje y actualizar el ranking del juego en tiempo real. |
| Búsqueda del Tesoro | El sistema permitirá crear juegos de BDT con área de búsqueda, etapas, QR esperado por etapa, puntaje por etapa y tiempo por etapa. Los participantes podrán tomar o subir fotos de QR encontrados desde la aplicación móvil, y el backend validará el tesoro mediante comparación del contenido decodificado del QR. |
| Geolocalización BDT | El sistema permitirá al operador visualizar en la aplicación web la ubicación de participantes durante juegos BDT activos, con actualización cada dos segundos enviada desde la aplicación móvil y previa autorización del participante. |
| Actualización en tiempo real | El sistema reflejará en tiempo real los cambios relevantes de publicación, lobby, estados, juegos, preguntas, temporizadores, ranking, etapas, pistas, geolocalización, resultados y eventos relevantes. |
| Puntuación y ranking | El sistema otorgará puntos solo a respuestas correctas en Trivia (igual al puntaje asignado a la pregunta, sin ponderación por tiempo) y a etapas ganadas en BDT según el puntaje configurado por etapa. Cada juego mantendrá su ranking nativo y, al finalizar la partida, el sistema calculará un ranking consolidado que ordena a los participantes o equipos por número de juegos ganados, luego por puntaje total y luego por menor tiempo total. |
| Ranking consolidado de la partida  | Al finalizar la partida, el sistema unificará los rankings de todos sus juegos en un ranking consolidado que ordena a los participantes o equipos por número de juegos ganados; en caso de empate, por el mayor puntaje total acumulado en todos los juegos y, de persistir, por el menor tiempo total.  |
| Trazabilidad operativa | El sistema registrará eventos relevantes como cambios de estado, inscripciones, convocatorias, invitaciones de equipo, activación y finalización de juegos, respuestas, tesoros subidos, validaciones, pistas, ubicaciones relevantes, variaciones de puntaje, cancelaciones, ranking consolidado y resultados. |
| Procesamiento asíncrono | El sistema utilizará mensajería asíncrona para procesos secundarios como auditoría, consolidación de historial, notificaciones internas, actualización de ranking o procesamiento de eventos que no deban bloquear la operación principal. |
| Gobernanza de permisos | El administrador contará con un panel de gobernanza para consultar y modificar, a nivel de rol, los privilegios de gobernanza y los permisos funcionales de cada rol, y para modificar el rol de operadores y participantes (incluida la promoción a administrador) sin poder modificar el rol de un administrador. |

## *Alcance específico del modo Búsqueda del Tesoro* {#alcance-específico-del-modo-búsqueda-del-tesoro}

En el modo Búsqueda del Tesoro, el sistema permitirá al operador crear juegos de Búsqueda del Tesoro dentro de una partida, definiendo el área de búsqueda, etapas, QR esperado por etapa, puntaje por etapa y tiempo límite por etapa. El nombre, la modalidad y las cantidades mínimas y máximas de participación se definen a nivel de partida. La partida se publicará y quedará en estado lobby y, una vez iniciada y activo el juego de BDT, permitirá a los participantes subir fotos del QR encontrado como tesoro de la etapa activa.

En cada juego de Búsqueda del Tesoro, el ranking se calculará por el puntaje acumulado en el juego (suma de los puntos de las etapas ganadas) y, en caso de empate, por el menor tiempo acumulado de las etapas ganadas. Cada etapa ganada otorga un puntaje configurado por el operador, que determina el ranking del juego y alimenta el puntaje total de la partida para el ranking consolidado.

| Área incluida | Descripción del alcance |
| ----- | ----- |
| Creación de juego BDT | El operador podrá crear juegos de Búsqueda del Tesoro dentro de una partida, definiendo el área de búsqueda como texto descriptivo. El nombre, la modalidad y los mínimos/máximos de participación se definen a nivel de partida. |
| Configuración de etapas | El operador podrá configurar una o más etapas para el juego. Cada etapa deberá tener un QR esperado, un puntaje y un tiempo límite. |
| Puntaje por etapa  | Cada etapa ganada otorgará el puntaje configurado para esa etapa al participante o equipo que la ganó; ese puntaje determina el ranking nativo del juego de BDT (por puntaje) y se suma al puntaje total de la partida para el ranking consolidado.  |
| Publicación en lobby | El operador podrá publicar la partida, con lo que pasa a estado lobby y habilita inscripciones de participantes individuales o equipos, según su modalidad. La partida aparecerá en el panel único Partidas de la aplicación móvil de los participantes. |
| Inscripción individual | En partidas individuales, los participantes podrán inscribirse mientras la partida esté en estado lobby, exista cupo disponible y se cumplan las reglas definidas. |
| Inscripción por equipos | En partidas por equipo, solo el líder podrá inscribir el equipo. Al hacerlo, el sistema enviará convocatoria a los integrantes del equipo para aceptar o rechazar su participación. |
| Inicio de partida | El operador podrá iniciar la partida desde el lobby cuando se cumplan las condiciones mínimas de participación. Al iniciar, la partida activará sus juegos secuencialmente; al activarse el juego de BDT, se activará su primera etapa. |
| Panel del participante | Durante un juego de BDT activo, el participante visualizará en la aplicación móvil la etapa activa, el temporizador y la opción “subir tesoro”. |
| Subida de tesoro | El participante podrá tomar o subir desde la aplicación móvil una foto del QR encontrado como tesoro de la etapa activa. |
| Validación automática de QR | El sistema procesará la imagen subida, decodificará el contenido del QR detectado y lo comparará con el contenido esperado del QR configurado para la etapa activa. |
| Resultado de validación | Si el contenido decodificado coincide con el esperado, el tesoro se marcará como válido. Si no coincide, no puede leerse o no corresponde a la etapa activa, se marcará como inválido. |
| Cierre de etapa | La etapa se cerrará cuando un participante/equipo valide correctamente el QR esperado o cuando se agote el tiempo límite definido para la etapa. |
| Resultado de etapa | Si hubo ganador, el sistema mostrará quién consiguió el tesoro y en cuánto tiempo. Si nadie lo consigue, mostrará el mensaje “nadie consiguió el tesoro”. |
| Avance de etapa | Al cerrarse una etapa, el juego avanzará a la siguiente etapa si existe. Si se cierra la última etapa del juego, el juego se finalizará y la partida activará el siguiente juego, o pasará a estado terminada si era el último. |
| Pistas | El operador podrá enviar pistas a participantes o equipos específicos durante un juego de BDT activo. Toda pista enviada deberá registrarse en el historial. |
| Monitoreo del operador | El operador podrá visualizar participantes o equipos inscritos, etapa activa, tesoros subidos, resultado de validación y eventos relevantes de la partida. |
| Geolocalización | Durante un juego de BDT activo, la aplicación móvil solicitará autorización de ubicación al participante y enviará su ubicación al backend para que el operador pueda visualizarla en un mapa con actualización cada dos segundos. |

## *Alcance específico del modo Trivia* {#alcance-específico-del-modo-trivia}

En el modo Trivia, el sistema permitirá al operador crear juegos de Trivia dentro de una partida, definiendo sus preguntas (opciones de respuesta, respuesta correcta, puntaje y tiempo límite por pregunta) en el momento de crear el juego. La partida se publicará y quedará en estado lobby y se iniciará manualmente o por tiempo; durante el juego de Trivia activo se sincronizarán las preguntas para todos los participantes, se validarán las respuestas automáticamente, se calcularán puntajes y se actualizará el ranking del juego en tiempo real

| Área incluida | Descripción del alcance |
| ----- | ----- |
| Creación de juego de Trivia | El operador podrá añadir juegos de Trivia a una partida creando sus preguntas en el momento. El nombre, la modalidad, los mínimos y máximos de participación y el tiempo de inicio se definen a nivel de partida. |
| Gestión de preguntas de Trivia | El operador podrá crear las preguntas de cada juego de Trivia al momento de crearlo. Cada pregunta deberá contener opciones de respuesta, respuesta correcta, puntaje asignado y tiempo límite. No existe banco de preguntas ni reutilización entre juegos.  |
| Publicación en lobby | El operador podrá publicar la partida, con lo que pasa a estado lobby y habilita inscripciones. La partida aparecerá en el panel único Partidas de la aplicación móvil de los participantes. |
| Inscripción individual | En partidas individuales, cualquier participante podrá inscribirse mientras la partida esté en estado lobby, exista cupo disponible y se cumplan las reglas de inscripción. |
| Inscripción por equipos | En partidas por equipo, solo el líder podrá inscribir el equipo. Al hacerlo, el sistema enviará convocatoria a los integrantes del equipo. |
| Inicio de partida | La partida podrá iniciar manualmente por acción del operador o automáticamente al cumplirse el tiempo configurado. Al iniciar, activará sus juegos secuencialmente; el juego de Trivia presentará sus preguntas cuando esté activo. |
| Ejecución sincronizada | Durante un juego de Trivia activo, todos los participantes recibirán en la aplicación móvil la misma pregunta y las mismas opciones al mismo tiempo, con temporizador sincronizado. |
| Respuesta individual | En un juego de Trivia en modalidad individual, la aplicación móvil permitirá al participante enviar una única respuesta por pregunta activa. |
| Respuesta por equipo | En un juego de Trivia en modalidad por equipos, la aplicación móvil permitirá enviar la respuesta del equipo, registrando como válida la primera opción seleccionada por cualquier integrante del equipo. |
| Validación automática | El sistema validará automáticamente cada respuesta contra la opción correcta configurada en la pregunta. |
| Cierre de pregunta | La pregunta activa se cerrará cuando algún participante/equipo responda correctamente o cuando se agote el tiempo límite. |
| Cambio de pregunta | Al cerrarse una pregunta, el sistema avanzará automáticamente a la siguiente pregunta si existe. |
| Cálculo de puntaje | El sistema otorgará puntos solo a respuestas correctas. El puntaje obtenido por una respuesta correcta será igual al puntaje asignado a la pregunta por el operador. El tiempo restante, el tiempo empleado y el tiempo total de la pregunta no modifican el puntaje obtenido. |
| Ranking | El ranking del juego de Trivia se actualizará en tiempo real según los puntajes obtenidos. |
| Panel del operador | Durante un juego de Trivia activo, el operador visualizará únicamente el ranking actualizado del juego y la opción de cancelar la partida, sin intervenir en las respuestas. |
| Historial | El participante podrá consultar desde la aplicación móvil su historial único de partidas jugadas, incluyendo los juegos, la modalidad, la fecha, el resultado/posición obtenida y el equipo asociado cuando se trate de una partida por equipos. |

## *Límites del alcance* {#límites-del-alcance}

Queda expresamente fuera del alcance del sistema la creación de tipos de juego adicionales distintos a Trivia y Búsqueda del Tesoro. El sistema no permitirá configurar workflows genéricos, dinámicas personalizadas no contempladas por estos modos, ni experiencias inmersivas arbitrarias fuera del dominio definido.

También quedan fuera del alcance funcionalidades avanzadas como cobros en línea, integración con dispositivos físicos, inteligencia artificial aplicada al contenido, analítica histórica compleja, navegación asistida, rutas históricas complejas de ubicación y cualquier integración externa que no sea necesaria para demostrar el flujo principal del sistema.

La aplicación móvil de participantes sí forma parte del alcance del sistema. Su alcance se limita a los flujos de participación definidos: consulta de partidas, gestión de equipos e invitaciones, inscripción, convocatorias, respuesta de Trivia, subida de tesoro QR, recepción de pistas, visualización de estados/resultados y geolocalización BDT previa autorización.

La solución se concentrará en una aplicación web para administración y operación, una aplicación móvil para participantes y servicios backend trazables y técnicamente defendibles, capaces de demostrar los flujos principales para los dos modos de juego definidos.

