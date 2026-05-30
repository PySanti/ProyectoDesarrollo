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

[Requerimientos no funcionales	15](#requerimientos-no-funcionales)

[**Modelo de dominio inicial	18**](#modelo-de-dominio-inicial)

[**Historias de usuario	19**](#historias-de-usuario)

[**Actores	38**](#actores)

[Consideraciones de acceso y dominio	42](#consideraciones-de-acceso-y-dominio)

[**Reglas de negocio	45**](#reglas-de-negocio)

[Reglas de negocio generales	45](#reglas-de-negocio-generales)

[Reglas de negocio de equipos	47](#reglas-de-negocio-de-equipos)

[Reglas de negocio de usuarios y roles	48](#reglas-de-negocio-de-usuarios-y-roles)

[Reglas de negocio de trivias	50](#reglas-de-negocio-de-trivias)

[Reglas de búsqueda de tesoro	52](#reglas-de-búsqueda-de-tesoro)

[**Alcance	57**](#alcance)

[Alcance específico del modo Búsqueda del Tesoro	60](#alcance-específico-del-modo-búsqueda-del-tesoro)

[Alcance específico del modo Trivia	63](#alcance-específico-del-modo-trivia)

[Límites del alcance	67](#límites-del-alcance)

# 

# **Objetivo general** {#objetivo-general}

Centralizar y controlar la operación de partidas interactivas en tiempo real bajo los modos de juego Trivia y Búsqueda del Tesoro, permitiendo la creación de partidas individuales o por equipos, gestión de lobbies, participación de jugadores, seguimiento operativo, validación de respuestas o tesoros, cálculo de puntajes y trazabilidad de eventos mediante una solución web basada en arquitectura hexagonal, persistencia relacional, comunicación en tiempo real y mensajería asíncrona.

# 

# **Objetivos específicos** {#objetivos-específicos}

* Definir la arquitectura funcional y técnica del sistema, estableciendo una separación clara entre dominio, aplicación, infraestructura e interfaces externas, conforme a principios de arquitectura hexagonal.  
    
* Modelar el dominio del sistema UMBRAL, identificando entidades, agregados, objetos de valor, servicios de dominio y reglas de negocio necesarias para representar partidas, participantes, equipos, líderes, convocatorias, formularios de Trivia, etapas de Búsqueda del Tesoro, tesoros QR, respuestas, puntajes, ubicaciones, eventos y comportamientos propios de los modos Trivia y Búsqueda del Tesoro.  
    
* Delimitar los modos de juego soportados por la plataforma, estableciendo que toda partida debe estar asociada exclusivamente a Trivia o Búsqueda del Tesoro, sin permitir la creación, configuración o ejecución de modos adicionales.  
    
* Diseñar los flujos principales de administración, operación y participación, diferenciando las funcionalidades comunes del sistema y las acciones específicas de cada modo de juego.  
    
* Implementar la integración con Keycloak para la autenticación, autorización base y asignación inicial de roles, manteniendo en UMBRAL únicamente las referencias locales necesarias para asociar usuarios con equipos, partidas, convocatorias, historial y reglas del dominio.  
    
* Implementar la gestión de equipos globales para la plataforma, permitiendo su uso tanto en Trivia como en Búsqueda del Tesoro, con reglas de creación, código de ingreso, liderazgo, transferencia de liderazgo, salida de integrantes y límite máximo de cinco jugadores por equipo.  
    
* Implementar la creación de formularios de Trivia por parte del operador, incluyendo preguntas, opciones de respuesta, respuesta correcta, puntaje asignado y tiempo límite por pregunta.  
    
* Implementar la creación y operación de partidas de Trivia individuales o por equipos, permitiendo publicarlas en lobby, gestionar inscripciones, convocar integrantes de equipo, iniciar la partida manualmente o por tiempo, sincronizar preguntas, validar respuestas, calcular puntajes y actualizar el ranking en tiempo real.  
    
* Implementar la creación y operación de partidas de Búsqueda del Tesoro individuales o por equipos, permitiendo definir área de búsqueda, etapas, QR esperado por etapa, tiempo límite por etapa, pistas, lobby, inscripciones, avance sincronizado y cierre de etapas por hallazgo o por agotamiento del tiempo.  
    
* Implementar la validación automática de tesoros en Búsqueda del Tesoro mediante la decodificación del QR contenido en la imagen subida por el participante y la comparación de su contenido con el QR esperado de la etapa activa.  
    
* Incorporar geolocalización operativa para Búsqueda del Tesoro, permitiendo al operador visualizar en un mapa la ubicación de los participantes durante partidas iniciadas, con actualización cada dos segundos y previa autorización del usuario.  
    
* Incorporar trazabilidad sobre las acciones relevantes del sistema, registrando cambios de estado, inscripciones, convocatorias, respuestas de Trivia, tesoros subidos, validaciones de QR, pistas enviadas, ubicaciones relevantes, variaciones de puntaje, cancelaciones y resultados de partida.  
    
* Integrar comunicación en tiempo real, permitiendo que la publicación de partidas, el estado del lobby, los temporizadores, preguntas, ranking, etapas, pistas, resultados, geolocalización y cambios de estado se actualicen de forma inmediata para operadores y participantes.  
    
* Implementar una separación entre operaciones de lectura y escritura, organizando los casos de uso mediante CQRS y MediatR para estructurar comandos, consultas y manejadores.  
    
* Persistir la información del sistema en una base de datos relacional, utilizando PostgreSQL y Entity Framework Core para almacenar referencias locales de usuarios autenticados por Keycloak, equipos, partidas, formularios de Trivia, preguntas, respuestas, etapas, tesoros subidos, puntajes, ubicaciones, convocatorias e historial.  
    
* Desacoplar procesos secundarios del flujo principal de la partida mediante mensajería asíncrona con RabbitMQ para la publicación y procesamiento de eventos relacionados con auditoría, historial, notificaciones internas, ranking y trazabilidad.  
    
* Garantizar la calidad técnica de la solución, incorporando validaciones de negocio, manejo de excepciones, logging y pruebas unitarias, de integración y end-to-end con criterios de cobertura definidos.  
    
* Preparar la solución para ejecución y validación en ambientes controlados, mediante contenedores con Docker Compose y un pipeline de integración continua para compilación y ejecución automatizada de pruebas.  
    
* Asegurar una interfaz web clara, responsive y coherente con los flujos del sistema, facilitando la administración, operación y participación desde distintos dispositivos sin requerir aplicaciones móviles nativas.

# 

# **Requerimientos** {#requerimientos}

## *Requerimientos funcionales* {#requerimientos-funcionales}

| ID | Modo | Descripción |
| ----- | ----- | ----- |
| RF-01 | General | El sistema debe integrarse con Keycloak para autenticar administradores, operadores y participantes, permitir la creación de usuarios desde UMBRAL, asignar un rol inicial durante la creación, impedir la modificación posterior del rol desde UMBRAL, consultar/editar datos generales, desactivar usuarios y almacenar únicamente una referencia local al identificador proveniente de Keycloak, sin guardar contraseñas. |
| RF-02 | General | El sistema debe diferenciar las funcionalidades disponibles según el rol autenticado del usuario —administrador, operador o participante— y según reglas propias del dominio, como liderazgo de equipo, pertenencia a equipo, inscripción, convocatoria y participación en partidas. |
| RF-03 | General | El sistema debe permitir crear partidas únicamente bajo los modos de juego **Trivia** o **Búsqueda del Tesoro**, e impedir la creación, configuración o ejecución de cualquier otro modo de juego. |
| RF-04 | General | Toda partida debe manejar únicamente los estados `lobby`, `iniciada`, `cancelada` y `terminada`; el sistema debe validar toda transición de estado, permitir al operador cancelar partidas en estados válidos y bloquear nuevas acciones de juego cuando una partida esté `cancelada` o `terminada`. |
| RF-05 | General | El sistema debe mostrar a todos los jugadores las partidas publicadas, independientemente de si son individuales o por equipo, mediante dos paneles principales: **Trivia** y **Búsqueda del Tesoro**, cada uno con listado de partidas publicadas y filtros por modalidad **individual** o **equipo**. |
| RF-06 | General | El sistema debe permitir que un participante juegue partidas individuales aunque pertenezca a un equipo, pero debe impedir que un participante no líder inscriba un equipo en una partida de equipo, mostrando el mensaje: “Debes ser líder de un equipo para entrar en este evento”. |
| RF-07 | Equipos | El sistema debe permitir que un participante cree un equipo solo si no pertenece a otro, generar un código único de ingreso, registrar como líder al creador, permitir unirse mediante código válido, impedir que un jugador pertenezca a más de un equipo, limitar cada equipo a un máximo global de cinco jugadores y permitir que los mismos equipos participen tanto en Trivia como en Búsqueda del Tesoro. |
| RF-08 | Equipos | El sistema debe permitir que un participante salga de su equipo; si no es líder, sale directamente, pero si es líder y existen otros integrantes, debe transferir el liderazgo antes de salir, mientras que si no existen otros integrantes el equipo debe eliminarse. |
| RF-09 | Equipos | El sistema debe permitir al administrador crear, consultar, editar y desactivar equipos, e impedir que equipos desactivados se inscriban en nuevas partidas. |
| RF-10 | General | El sistema debe permitir que el líder inscriba su equipo en partidas de equipo mientras estén en estado `lobby` y exista cupo disponible; al hacerlo, debe enviar convocatoria a los integrantes del equipo y registrar la aceptación o rechazo de cada convocado. |
| RF-11 | General | El sistema debe impedir el inicio de una partida si no cumple la cantidad mínima de participantes, equipos o jugadores por equipo definida por el operador; en juegos individuales el máximo corresponde a jugadores, y en juegos por equipo el máximo corresponde a equipos, pudiendo definirse además mínimo y máximo de jugadores por equipo. |
| RF-12 | General | El sistema debe registrar un historial de eventos relevantes de la partida, incluyendo cambios de estado, inscripciones, convocatorias, respuestas, tesoros subidos, validaciones, pistas, puntajes, ubicaciones relevantes, cancelaciones y resultados. |
| RF-13 | General | El sistema debe actualizar en tiempo real los cambios relevantes de las partidas, incluyendo publicación, lobby, estados, preguntas, ranking, etapas, temporizadores, pistas, geolocalización, resultados y sincronización entre dispositivos autorizados de participantes de un mismo equipo. |
| RF-14 | General | El sistema debe permitir que un participante se reconecte a una partida en curso y recupere el estado vigente que le corresponda según su rol, equipo, convocatoria, inscripción y modalidad de la partida. |
| RF-15 | Trivia | El sistema debe permitir al operador crear, editar y consultar formularios de Trivia, los cuales deben contener preguntas, opciones de respuesta, respuesta correcta, puntaje asignado y tiempo límite por pregunta. |
| RF-16 | Trivia | El sistema debe validar que un formulario de Trivia esté completo antes de permitir su uso en una partida, rechazando formularios sin preguntas, sin opciones, sin respuesta correcta, sin puntaje o sin tiempo por pregunta. |
| RF-17 | Trivia | El sistema debe permitir al operador crear partidas de Trivia asociadas a un formulario válido, definiendo nombre, modalidad individual o equipo, cantidad mínima de participantes, máximo de jugadores si es individual, máximo de equipos si es por equipo, mínimo y máximo de jugadores por equipo cuando aplique, y tiempo de inicio. |
| RF-18 | Trivia | El sistema debe permitir al operador iniciar el lobby de una partida de Trivia para publicarla, habilitar inscripciones de jugadores individuales o equipos según su modalidad, e iniciar la partida manualmente o automáticamente al cumplirse el tiempo configurado, cambiando su estado a `iniciada`. |
| RF-19 | Trivia | Durante una partida de Trivia iniciada, el sistema debe mostrar la misma pregunta y las mismas opciones a todos los participantes al mismo tiempo, sincronizando el temporizador de cada pregunta para todos los jugadores. |
| RF-20 | Trivia | En Trivia individual, el sistema debe aceptar una única respuesta por jugador por pregunta activa; en Trivia por equipos, debe aceptar una única respuesta por equipo, registrando como válida la primera opción seleccionada por cualquier integrante del equipo. |
| RF-21 | Trivia | El sistema debe rechazar respuestas repetidas, tardías o enviadas fuera del estado válido de la pregunta activa, validar automáticamente cada respuesta contra la opción correcta configurada y cerrar la pregunta cuando algún jugador/equipo responda correctamente o cuando se agote el tiempo límite. |
| RF-22 | Trivia | Al cerrar una pregunta de Trivia, el sistema debe avanzar automáticamente a la siguiente pregunta si existe, actualizar el ranking en tiempo real y calcular el puntaje de toda respuesta correcta mediante la fórmula `puntaje_obtenido = puntaje_pregunta * (tiempo_restante / tiempo_total)`. |
| RF-23 | Trivia | Durante una partida de Trivia iniciada, el panel del operador debe mostrar únicamente el ranking actualizado y la opción de cancelar la partida, sin permitirle intervenir en las respuestas de los participantes. |
| RF-24 | Trivia | El sistema debe permitir al participante consultar su historial de partidas de Trivia individuales y por equipo, mostrando modalidad, fecha, puntaje obtenido, posición en ranking y equipo asociado cuando aplique. |
| RF-25 | Búsqueda del Tesoro | El sistema debe permitir al operador crear partidas de Búsqueda del Tesoro, definiendo nombre, área de búsqueda, modalidad individual o equipo, cantidad mínima de participantes, máximo de jugadores si es individual, máximo de equipos si es por equipo y mínimo de jugadores por equipo cuando aplique. |
| RF-26 | Búsqueda del Tesoro | El sistema debe permitir al operador configurar una o más etapas para una partida de Búsqueda del Tesoro, donde cada etapa debe tener un código QR esperado y un tiempo límite; el sistema debe impedir publicar partidas sin etapas válidas o etapas sin QR esperado o tiempo definido. |
| RF-27 | Búsqueda del Tesoro | El sistema debe permitir al operador crear el lobby de una partida de Búsqueda del Tesoro para publicarla, habilitar inscripciones de jugadores individuales o equipos según su modalidad e iniciar la partida desde el lobby cuando se cumplan las condiciones mínimas, cambiando su estado a `iniciada` y activando la primera etapa. |
| RF-28 | Búsqueda del Tesoro | Durante una partida de Búsqueda del Tesoro iniciada, el panel del participante debe mostrar la etapa activa, el temporizador y la opción “subir tesoro”, permitiendo tomar o subir una foto del QR encontrado como tesoro de la etapa activa. |
| RF-29 | Búsqueda del Tesoro | El sistema debe procesar la imagen subida por el participante, decodificar el contenido del QR detectado y compararlo con el contenido esperado del QR configurado para la etapa activa, marcando el tesoro como válido si coincide, o inválido si no coincide, no puede leerse o no corresponde a la etapa activa. |
| RF-30 | Búsqueda del Tesoro | El sistema debe registrar cada tesoro subido con participante o equipo asociado, partida, etapa, fecha, contenido decodificado cuando aplique y resultado de validación; además, el operador debe poder visualizar cada tesoro subido y si fue válido o inválido. |
| RF-31 | Búsqueda del Tesoro | El sistema debe cerrar la etapa activa cuando un jugador/equipo valide correctamente el QR esperado o cuando se agote el tiempo límite configurado; si hubo ganador, debe mostrar quién consiguió el tesoro y en cuánto tiempo, y si no hubo ganador debe mostrar el mensaje “nadie consiguió el tesoro”. |
| RF-32 | Búsqueda del Tesoro | Al cerrarse una etapa de Búsqueda del Tesoro, el sistema debe avanzar a la siguiente etapa si existe; si se cierra la última etapa, debe cambiar la partida a estado `terminada`. |
| RF-33 | Búsqueda del Tesoro | El sistema debe permitir al operador visualizar la lista de jugadores o equipos inscritos, enviar pistas a jugadores o equipos específicos durante una partida iniciada y registrar cada pista enviada en el historial. |
| RF-34 | Búsqueda del Tesoro | El sistema debe solicitar autorización de ubicación al participante antes de compartir su geolocalización, permitir al operador visualizar en un mapa la ubicación de los participantes durante una partida de Búsqueda del Tesoro iniciada y actualizar dicha geolocalización cada 2 segundos. |
| RF-35 | Transversal | El sistema debe permitir consultar partidas, equipos, participantes, formularios de Trivia, etapas de Búsqueda del Tesoro, respuestas, tesoros subidos, rankings e historial sin modificar el estado del sistema. |
| RF-36 | Transversal | El sistema debe aplicar reglas de negocio antes de aceptar cambios de estado, inscripciones, convocatorias, respuestas, tesoros subidos, validaciones, pistas, cancelaciones o cualquier acción que afecte la partida. |
| RF-37 | Transversal | El sistema debe publicar eventos relevantes del dominio para auditoría, historial, notificaciones internas, actualización de ranking, trazabilidad de puntajes y comunicación en tiempo real. |

## 

## *Requerimientos no funcionales* {#requerimientos-no-funcionales}

| ID | Descripción |
| ----- | ----- |
| RNF-01 | La solución debe implementarse con frontend en React y backend en .NET Core. |
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
| RNF-12 | La interfaz debe ser clara, utilizable y coherente con los flujos principales del sistema. |
| RNF-13 | La autenticación y autorización base del sistema debe integrarse con Keycloak mediante tokens seguros. |
| RNF-14 | El sistema no debe almacenar contraseñas ni credenciales sensibles de usuarios en la base de datos propia de UMBRAL. |
| RNF-15 | El sistema debe soportar actualización de geolocalización cada 2 segundos durante partidas BDT iniciadas, sin bloquear la operación principal. |
| RNF-16 | El sistema debe permitir decodificar códigos QR desde imágenes capturadas o subidas por los participantes desde una interfaz web responsive. |
| RNF-17 | El canal de tiempo real debe soportar actualizaciones de lobby, preguntas, ranking, etapas, pistas, geolocalización y cambios de estado. |

# 

# **Modelo de dominio inicial** {#modelo-de-dominio-inicial}

## 

Por definir

# 

# 

# **Historias de usuario** {#historias-de-usuario}

| ID | Módulo | Historia de usuario | Actor principal | Criterios de aceptación | Prioridad |
| ----- | ----- | ----- | ----- | ----- | ----- |
| HU-01 | Usuarios y roles | Como Administrador, quiero crear usuarios en la plataforma y asignarles un rol inicial, para establecer y controlar los accesos seguros al sistema.  | Administrador | El administrador puede crear usuarios. Todo usuario debe tener un rol inicial. El rol solo puede asignarse durante la creación. | Alta |
| HU-02 | Usuarios y roles | Como Administrador, quiero consultar, editar datos generales, para mantener actualizada y controlada la base de usuarios. | Administrador | El administrador puede consultar usuarios, editar datos generales y desactivar usuarios. No puede modificar roles después de la creación. | Alta |
| HU-03 | Equipos | Como **Participante**, quiero crear un equipo, para participar en partidas (Trivia o BDT) de equipo. | Participante | El participante puede crear un equipo solo si no pertenece a otro. El creador queda registrado como líder. El sistema genera un código único de equipo. | Alta |
| HU-04 | Equipos | Como **Participante**, quiero unirme a un equipo usando un código, para formar parte de un equipo existente. | Participante | El código debe ser válido. El participante no puede pertenecer a otro equipo. El equipo no puede superar 5 jugadores. | Alta |
| HU-05 | Equipos | Como **Líder de equipo** quiero eliminar el equipo que cree. | Participante | El lider de equipo debe poder eliminar el equipo, si esto ocurre, tambien se debe eliminar el equipo para los integrantes y se les debe informar. | Alta |
| HU-06 | Equipos | Como **Líder de equipo**, quiero transferir el liderazgo antes de salir del equipo, para que el equipo pueda seguir existiendo. | Participante  | Si el líder desea salir y hay otros jugadores, debe elegir un nuevo líder. Si no hay más jugadores, el equipo se elimina. | Alta |
| HU-07 | Equipos | Como **Participante**, quiero salir de mi equipo, para dejar de participar en él. | Participante | El participante puede salir del equipo. Si no es líder, sale directamente. Si es líder, debe transferir liderazgo o eliminarse el equipo si está solo. | Alta |
| HU-08 | Equipos | Como **Administrador**, quiero gestionar equipos, para mantener control administrativo sobre los equipos de la plataforma. | Administrador | El administrador puede crear, consultar, editar. Los equipos son comunes para Trivia y BDT. El administrador puede modificar tambien el liderazgo del equipo, en caso de hacerlo, se le debe notificar tanto al ex-lider como al nuevo lider. | Alta |
| HU-09 | Listado de partidas | Como **Participante**, quiero ver las partidas de trivias publicadas. | Participante | Cada participante debe tener un panel “Trivia” en donde salgan las partidas de trivia publicadas.  | Alta |
| HU-10 | Listado de partidas | Como **Participante**, quiero ver las partidas de BDT publicadas. | Participante | Cada participante debe tener un panel “Busqueda de tesoro” en donde salgan las partidas de búsqueda de tesoro publicadas.  | Alta |
| HU-11 | Filtros de partidas | Como **Participante**, quiero filtrar partidas de trivias  por modalidad individual o equipo. | Participante | Cada panel permite filtrar por “partidas individuales” y “partidas de equipo”. | Media |
| HU-12 | Filtros de partidas | Como **Participante**, quiero filtrar partidas de BDT  por modalidad individual o equipo. | Participante | Cada panel permite filtrar por “partidas individuales” y “partidas de equipo”. | Media |
| HU-13 | Acceso a partidas de equipo | Como **Participante**, quiero recibir una advertencia si intento entrar a una partida de trivia de equipo sin ser líder. | Participante | Si el jugador no es líder de ningún equipo e intenta entrar a una partida de equipo, el sistema muestra: “Debes ser líder de un equipo para entrar en este evento”. | Alta |
| HU-14 | Acceso a partidas de equipo | Como **Participante**, quiero recibir una advertencia si intento entrar a una partida de BDT de equipo sin ser líder. | Participante | Si el jugador no es líder de ningún equipo e intenta entrar a una partida de equipo, el sistema muestra: “Debes ser líder de un equipo para entrar en este evento”. | Alta |
| HU-15 | Creación de Trivia | Como **Operador**, quiero crear formularios, para preparar el contenido que luego será usado en partidas de trivia. | Operador | El operador puede crear, editar y consultar formularios de Trivia. Cada formulario contiene preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta. | Alta |
| HU-16 | Creación de Trivia | Como **Operador** quiero visualizar los formularios que he creado  | Operador | El operador puede crear, editar y consultar formularios de Trivia. Cada formulario contiene preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta. | Alta |
| HU-17 | Creación de partida Trivia | Como **Operador**, quiero crear una partida de Trivia asociada a un formulario existente y publicarlo. | Operador | El operador define nombre, formulario asociado, modalidad individual/equipo, mínimo de participantes, máximo de jugadores si es individual, máximo de equipos si es por equipo, mínimo/máximo de jugadores por equipo y tiempo de inicio. | Alta |
| HU-18 | Unión a Trivia individual | Como **Participante**, quiero unirme a una Trivia individual publicada, para participar individualmente. | Participante | Cualquier jugador puede unirse a una partida publicada para su categoría. | Alta |
| HU-19 | Unión a Trivia por equipo | Como **Líder de equipo**, quiero unir mi equipo a una Trivia por equipos, para participar con mi equipo. | Participante líder | Solo el líder puede unir el equipo. No debe superar el máximo de equipos. | Alta |
| HU-20 | Convocatoria Trivia por equipo | Como **Participante de equipo**, quiero recibir una convocatoria cuando mi líder una el equipo a una Trivia, para aceptar o rechazar mi participación. | Participante | El sistema envía convocatoria a los integrantes del equipo. Cada integrante puede aceptar o rechazar. | Alta |
| HU-21 | Pantalla de espera Trivia | Como **Participante**, quiero ver una pantalla de espera después de unirme. | Participante | El sistema muestra un panel de espera.. | Alta |
| HU-22 | Pantalla de espera Trivia | Como **Operador** quiero observar los participantes que solicitaron unirse a la partida de trivia publicada. | Operador | El panel debe mostrar los equipos o jugadores que entraron a la partida. El panel se muestra mientras la partida está en estado “lobby”. | Alta |
| HU-23 | Pantalla de espera Trivia | Como **Operador** quiero observar los equipos que solicitaron unirse a la partida de trivia publicada. | Operador | El panel debe mostrar los jugadores/equipos que desean entrar a la partida y puede aceptarlos o rechazarlos.  | Media |
| HU-24 | Inicio de Trivia | Como **Operador**, quiero iniciar manualmente la Trivia. | Operador | La partida inicia cuando llega el tiempo definido cuando se creó la partida  o cuando el operador la inicia manualmente. | Alta |
| HU-25 | Ejecución sincronizada de Trivia | Como **Participante**, quiero que todos los participantes recibamos la misma pregunta al mismo tiempo, para competir bajo condiciones iguales. | Participante | Todos los participantes ven la misma pregunta y opciones simultáneamente. El temporizador se sincroniza para todos. | Alta |
| HU-26 | Respuesta en Trivia individual | Como **Participante**, quiero seleccionar una única respuesta por pregunta | Participante | En modalidad individual, solo se acepta una respuesta por jugador por pregunta. | Alta |
| HU-27 | Respuesta en Trivia por equipo | Como **Participante de equipo**, quiero poder responder una pregunta, para contribuir a la respuesta del equipo. | Participante | En modalidad equipo, solo se acepta una respuesta por equipo. La respuesta válida será la primera opción seleccionada por cualquier participante del equipo. | Alta |
| HU-28 | Cierre de pregunta Trivia | Como **Participante**, quiero ver si la respuesta que escogí para la pregunta es correcta o incorrecta una vez finalizado el temporizador | Participante | La pregunta se cierra cuando un jugador/equipo responde y expira el tiempo de la pregunta y muestra el resultado correcto | Alta |
| HU-29 | Puntaje Trivia | Como **Participante**, quiero que mi respuesta de cada pregunta sea ponderada. | Participante | Solo se otorgan puntos si la respuesta es correcta. El puntaje se calcula según el valor configurado. | Alta |
| HU-30 | Panel operador Trivia | Como **Operador**, quiero ver el ranking del participante/participantes o equipos durante una partida.  | Operador | Durante la Trivia, el operador solo ve el ranking actualizado y un botón para cancelar la partida. | Alta |
| HU-31 | Panel operador trivia | Como **Operador** quiero poder cancelar una partida iniciada. | Operador | En el panel que se le muestre al operador durante la ejecución de la partida, le debe aparecer un botón “cancelar partida” |  |
| HU-32 | Panel participante trivia | Como **Participante** quiero ser notificado si la partida fue cancelada | Participante | En el panel del participante mientras se ejecuta la partida, debe mostrarse una notificación si la partida fue cancelada inmediatamente después. | Media |
| HU-33 | Historial de Trivia | Como **Participante**, quiero consultar mi historial de partidas de Trivia, para revisar mis participaciones individuales y de equipo. | Participante | El historial muestra partidas jugadas, modalidad, fecha, puntaje, ranking y equipo asociado cuando aplique. | Media |
| HU-34 | Creación de partida BDT | Como **Operador**, quiero crear una partida de Búsqueda del Tesoro. Añadir etapas, tesoro por etapa  y temporizador de cada etapa. | Operador | El operador define nombre, área de búsqueda (un texto), modalidad individual/equipo, mínimo de participantes, máximo de jugadores si es individual, máximo de equipos si es por equipo y mínimo de jugadores por equipo cuando aplique. El tesoro es un QR | Alta |
| HU-35 | Panel de Operador | Como **Operador**, quiero ver la lista de partidas de trivia que fueron publicadas. | Operador | El operador debe poder consultar la lista de partidas de trivia, ver su nombre y estado | Media |
| HU-36 | Panel de operador | Como **Operador** quiero poder ver el detalle de las partidas de trivia publicadas | Operador | El operador debe poder acceder al detalle de una publicación de una partida de Trivia y ver toda su información. | Media |
| HU-37 | Panel de Operador | Como **Operador**, quiero ver la lista de partidas de búsqueda de tesoro que fueron publicadas. | Operador | El operador debe poder consultar la lista de partidas de búsqueda de tesoro, ver su nombre y estado | Media |
| HU-38 | Panel de operador | Como **Operador** quiero poder ver el detalle de las partidas de búsqueda de tesoro publicadas | Operador | El operador debe poder acceder al detalle de una publicación de una partida de búsqueda de tesoro y ver toda su información. | Media |
| HU-39 | Unión a BDT individual | Como **Participante**, quiero unirme a una BDT individual publicada, para jugar por mi cuenta. | Participante | El jugador puede unirse a la partida de BDT.  Una vez que el jugador se una, al jugador le debe salir un panel de espera mientras se une el resto de jugadores. | Alta |
| HU-40 | Unión a BDT por equipo | Como **Líder de equipo**, quiero unir mi equipo a una BDT por equipos, para participar con mi equipo. | Participante líder | Solo el líder puede unir el equipo. | Alta |
| HU-41 | Convocatoria BDT por equipo | Como **Participante de equipo**, quiero recibir una convocatoria cuando mi líder una al equipo a una BDT, para aceptar o rechazar mi participación. | Participante | Los integrantes reciben convocatoria y pueden aceptar o rechazar. | Alta |
| HU-42 | Panel de Operador | Como operador quiero observar los participantes que solicitaron unirse a la partida de BDT publicada | Operador | Una vez creada y publicada una partida de búsqueda de tesoro, el operador podrá ver en tiempo real los equipos o participantes que se unan a la partida. | Alta |
| HU-43 | Inicio de BDT | Como **Operador**, quiero iniciar una partida BDT | Operador | La partida solo inicia si cumple mínimos de participación.  | Alta |
| HU-44 | Panel jugador BDT | Como **Participante**, quiero ver la etapa activa y la opción de subir tesoro. | Participante | El panel muestra etapa actual, temporizador y botón “subir tesoro”. | Alta |
| HU-45 | Subida de tesoro BDT | Como **Participante**, quiero tomar o subir una foto del tesoro (QR) | Participante | El jugador puede tomar o subir una foto. El sistema procesa la imagen enviada e intenta decodificar el contenido del QR detectado. | Alta |
| HU-46 | Validación de QR BDT | Como **Operador**, quiero que el sistema valide automáticamente el QR enviado, para garantizar la transparencia del juego sin intervención manual  | Sistema | Si el contenido decodificado coincide con el contenido esperado, el envío se marca como válido. Si no coincide, no puede leerse o no corresponde a la etapa activa, se marca como inválido. Todo envío queda registrado. | Alta |
| HU-47 | Cierre de etapa BDT | Como **Participante**, quiero que la etapa termine cuando encuentre el tesoro o culmine el temporizador, para avanzar a la siguiente etapa. | Participante | La etapa termina si un jugador/equipo valida correctamente el QR o si expira el tiempo configurado para la etapa. | Alta |
| HU-48 | Resultado de etapa BDT | Como **Participante**, quiero saber quién encontró el tesoro de cada etapa y cuánto tiempo tardó en conseguirlo, para conocer el resultado de la etapa. | Participante | Si hubo ganador, se muestra quién consiguió el tesoro y en cuánto tiempo. Si nadie lo consigue, se muestra “nadie consiguió el tesoro”. | Alta |
| HU-49 | Pistas BDT | Como **Operador**, quiero enviar pistas a participantes o equipos durante la BDT, para orientar su búsqueda. | Operador | El operador puede enviar pistas a jugadores/equipos específicos. Las pistas quedan registradas. Las pistas son cadenas de texto. E | Alta |
| HU-50 | Panel de operador en BDT | Como operador, quiero ver un panel durante la partida de búsqueda de tesoro que permita cancelar la partida y seleccionar a un jugador o equipo para enviarle una pista. | Operador | El operador debe tener en su panel la opción de cancelar la partida y de una enviarle pista a un jugador o equipo. | Alta |
| HU-51 | Monitoreo BDT | Como **Operador**, quiero ver la lista de jugadores/equipos y sus tesoros subidos, para supervisar la partida. | Operador | El panel muestra participantes/equipos, etapa actual, envíos realizados y si cada tesoro fue válido o inválido. | Alta |
| HU-52 | Geolocalización BDT | Como **Operador**, quiero ver en un mapa la geolocalización de los participantes durante una BDT iniciada, para supervisar la búsqueda. | Operador | Una vez iniciada la partida, el operador ve un mapa con la ubicación de los participantes. El sistema debe solicitar/autorización de ubicación al jugador. La ubicación se actualiza cada 2 segundos mientras la partida BDT esté iniciada. | Alta |
| HU-53 | Cancelación de partida | Como **Operador**, quiero cancelar una partida. | Operador | El operador puede cancelar partidas en estados permitidos. Una partida cancelada no acepta nuevas acciones de juego. | Alta |
| HU-54 | Cancelación de partida | Como **participante** quiero poder recibir una notificación si la partida se cancela. | Participante | Si el operador cancela la partida los participantes deben recibir una notificación. | Media |
| HU-55 | Tiempo real | Como **Usuario autenticado**, quiero recibir actualizaciones en tiempo real, para ver cambios sin recargar la página. | Operador / Participante | El sistema actualiza partidas publicadas, lobby, preguntas, ranking, etapas, temporizadores, pistas, geolocalización, resultados y estados en tiempo real. | Alta |
| HU-56 | Historial y trazabilidad | Como **Operador**, quiere consultar el historial de una partida, para auditar lo ocurrido. | Operador | El historial registra cambios de estado, inscripciones, convocatorias, respuestas, puntajes, etapas, QR enviados, validaciones, pistas, ubicaciones relevantes y cancelaciones. | Alta |

# **Actores** {#actores}

| ID | Actor | Descripción | Responsabilidades principales | Permisos mínimos esperados |
| ----- | ----- | ----- | ----- | ----- |
| AC-01 | Administrador | Usuario responsable de la configuración administrativa general del sistema y de la gestión inicial de accesos mediante la integración con Keycloak. | Crear usuarios desde UMBRAL mediante Keycloak; asignar rol inicial durante la creación; consultar, editar datos generales y desactivar usuarios; consultar y gestionar equipos desde una perspectiva administrativa; consultar información operativa cuando corresponda. | Acceder al módulo de administración; crear usuarios mediante Keycloak; asignar rol inicial; consultar, editar y desactivar usuarios; crear, consultar, editar y desactivar equipos; consultar información general sin intervenir directamente en la operación de partidas. |
| AC-02 | Operador | Usuario encargado de preparar, configurar, publicar, ejecutar y supervisar partidas en vivo bajo los modos Trivia o Búsqueda del Tesoro. | Crear formularios de Trivia; configurar preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta; crear partidas de Trivia; crear partidas BDT; configurar etapas, QR esperado y tiempo por etapa; publicar lobbies; iniciar partidas; cancelar partidas; supervisar ranking en Trivia; enviar pistas en BDT; visualizar tesoros subidos; visualizar geolocalización de participantes en BDT; consultar historial y eventos relevantes. | Acceder al panel de operador; crear formularios y partidas; configurar Trivia y BDT; iniciar lobby; iniciar partida; cancelar partida; observar ranking; enviar pistas; consultar tesoros subidos; consultar geolocalización BDT; consultar historial de partida. |
| AC-03 | Participante | Usuario autenticado que puede participar en partidas individuales, crear o unirse a equipos, actuar como líder de equipo cuando corresponda y participar en partidas de Trivia o Búsqueda del Tesoro desde una interfaz web responsive. | Visualizar paneles de Trivia y Búsqueda del Tesoro; consultar partidas publicadas; filtrar por modalidad; crear equipo; unirse a equipo mediante código; salir de equipo; transferir liderazgo si es líder; inscribirse en partidas individuales; inscribir equipo si es líder; aceptar o rechazar convocatorias; responder preguntas de Trivia; subir tesoros QR en BDT; consultar historial de Trivia; permitir geolocalización en BDT cuando aplique. | Acceder a los paneles de jugador; ver partidas publicadas; participar en partidas individuales; gestionar su pertenencia a equipo; responder Trivia; subir tesoros en BDT; aceptar/rechazar convocatorias; consultar historial; compartir ubicación en partidas BDT iniciadas previa autorización. |

## 

## *Consideraciones de acceso y dominio* {#consideraciones-de-acceso-y-dominio}

| Elemento | Aclaración |
| ----- | ----- |
| Autenticación | La autenticación será gestionada por Keycloak. UMBRAL no almacenará contraseñas ni credenciales sensibles. |
| Roles base | Los roles base del sistema son administrador, operador y participante. Estos roles provienen de Keycloak y se usan para controlar permisos generales. |
| Usuario local | UMBRAL almacenará una referencia local al usuario autenticado mediante el identificador proveniente de Keycloak, con el fin de asociarlo a equipos, partidas, convocatorias, respuestas, tesoros, ubicaciones e historial. |
| Administrador | El administrador gestiona usuarios desde UMBRAL mediante integración con Keycloak y administra equipos. No es el actor responsable de crear formularios de Trivia ni partidas BDT. |
| Operador | El operador es el actor responsable de crear y operar los juegos. Puede crear formularios de Trivia, partidas de Trivia, partidas BDT, etapas, QR esperados, tiempos, pistas y lobbies. |
| Participante | El participante puede visualizar partidas publicadas, jugar partidas individuales, crear o unirse a equipos, aceptar convocatorias, responder preguntas de Trivia y subir tesoros QR en BDT. |
| Líder de equipo | El liderazgo de equipo no es un rol de Keycloak, sino una condición de negocio dentro de UMBRAL. El líder es quien creó el equipo o recibió transferencia de liderazgo. |
| Equipo | El equipo no es un actor independiente, sino una entidad del dominio. Agrupa participantes, tiene un líder, posee un código de ingreso y puede participar tanto en Trivia como en BDT. |
| Partidas publicadas | Todas las partidas publicadas se muestran a todos los jugadores. La visibilidad de una partida no implica autorización automática para inscribirse. |
| Partidas individuales | Un participante puede jugar partidas individuales aunque pertenezca a un equipo. |
| Partidas por equipo | Solo el líder puede inscribir un equipo en una partida por equipo. Si un jugador no líder intenta entrar, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en este evento”. |
| Convocatorias | Cuando un líder inscribe su equipo en una partida por equipo, el sistema envía una convocatoria a los demás integrantes, quienes pueden aceptar o rechazar. |
| Trivia | En Trivia, todos los jugadores reciben la misma pregunta al mismo tiempo. El sistema valida automáticamente las respuestas y calcula el puntaje según la fórmula definida. El operador solo visualiza ranking y opción de cancelación durante la partida. |
| Búsqueda del Tesoro | En BDT, el participante sube una foto del QR encontrado. El sistema decodifica el QR y compara su contenido con el QR esperado de la etapa activa. El operador puede enviar pistas y supervisar tesoros subidos. |
| Geolocalización | En BDT iniciada, el sistema puede solicitar autorización de ubicación al participante y enviar su ubicación al operador cada 2 segundos para visualización en mapa. |
| Interacción móvil | La participación desde dispositivos móviles se contempla mediante una interfaz web responsive, no mediante aplicaciones móviles nativas. |

# **Reglas de negocio** {#reglas-de-negocio}

## *Reglas de negocio generales* {#reglas-de-negocio-generales}

| ID | Regla de negocio |
| ----- | ----- |
| RB-01 | El sistema solo permite dos tipos de juego: **Trivia** y **Búsqueda del Tesoro**. |
| RB-02 | En la interfaz del jugador deben existir dos paneles principales: **Trivia** y **Búsqueda del Tesoro**. |
| RB-03 | Cada panel debe mostrar la lista de partidas publicadas correspondientes a ese tipo de juego. |
| RB-04 | Cada panel debe permitir filtrar partidas por modalidad: **individual** o **equipo**. |
| RB-05 | Todas las partidas publicadas deben mostrarse a todos los jugadores, sin importar si son individuales o por equipo. |
| RB-06 | Si una partida es de equipo y el jugador no es líder de ningún equipo, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en este evento”. |
| RB-07 | Las partidas solo pueden tener los estados `lobby`, `iniciada`, `cancelada` o `terminada`. |
| RB-08 | Una partida en estado `lobby` permite inscripción de jugadores o equipos. |
| RB-09 | Una partida en estado `iniciada` permite acciones propias del juego, como responder preguntas o subir tesoros. |
| RB-10 | Una partida en estado `cancelada` no acepta nuevas inscripciones, respuestas, tesoros, pistas ni cambios de participación. |
| RB-11 | Una partida en estado `terminada` no acepta nuevas acciones de juego. |
| RB-12 | Toda transición de estado debe ser validada por el sistema antes de aplicarse. |
| RB-13 | El operador es el único actor autorizado para crear juegos, formularios, partidas, preguntas, etapas, pistas y configuración operativa de Trivia o BDT. |
| RB-14 | El operador puede cancelar una partida si se encuentra en un estado válido para cancelación. |
| RB-15 | Las acciones relevantes deben registrarse en el historial de la partida. |
| RB-16 | Los cambios importantes deben publicarse en tiempo real para los usuarios afectados. |
| RB-17 | El sistema debe diferenciar las funcionalidades según el rol autenticado: administrador, operador o participante. |
| RB-18 | Los participantes pueden jugar partidas individuales aunque pertenezcan a un equipo. |
| RB-19 | Un participante que pertenece a un equipo solo puede jugar partidas de equipo si su líder une al equipo y el participante acepta la convocatoria. |
| RB-20 | En juegos individuales, el operador define el máximo de jugadores. |
| RB-21 | En juegos por equipo, el operador define el máximo de equipos. |
| RB-22 | En juegos por equipo, el operador puede definir cantidad mínima y máxima de jugadores por equipo para esa partida. |
| RB-23 | Una partida no puede iniciar si no cumple los mínimos configurados por el operador. |
| RB-24 | El sistema debe conservar trazabilidad de puntajes, respuestas, tesoros, validaciones, pistas, estados y resultados. |

## *Reglas de negocio de equipos* {#reglas-de-negocio-de-equipos}

| ID | Regla de negocio |
| ----- | ----- |
| RB-E01 | Los equipos son globales para toda la aplicación y se usan tanto en Trivia como en BDT. |
| RB-E02 | Todo jugador puede crear un equipo si no pertenece a otro. |
| RB-E03 | Todo jugador puede unirse a un equipo mediante código si no pertenece a otro. |
| RB-E04 | Cuando se crea un equipo, el sistema genera un código único de ingreso. |
| RB-E05 | El jugador que crea el equipo queda registrado automáticamente como líder. |
| RB-E06 | Un jugador solo puede pertenecer a un equipo a la vez. |
| RB-E07 | Un equipo puede tener máximo 5 jugadores. |
| RB-E08 | Los jugadores pueden salir de su equipo. |
| RB-E09 | Si un jugador no líder sale del equipo, simplemente deja de pertenecer al equipo. |
| RB-E10 | Si el líder desea salir y existen otros integrantes, debe transferir el liderazgo a otro jugador antes de salir. |
| RB-E11 | Si el líder desea salir y no existen otros integrantes, el equipo se elimina. |
| RB-E12 | El administrador puede crear, consultar, editar y desactivar equipos. |
| RB-E13 | Un equipo desactivado no puede inscribirse en nuevas partidas. |
| RB-E14 | El líder es el único autorizado para inscribir al equipo en partidas de equipo. |

## *Reglas de negocio de usuarios y roles* {#reglas-de-negocio-de-usuarios-y-roles}

| ID | Regla de negocio |
| ----- | ----- |
| RB-U01 | La autenticación de usuarios será gestionada por Keycloak. |
| RB-U02 | Los roles base del sistema serán administrados mediante Keycloak: administrador, operador y participante. |
| RB-U03 | UMBRAL no almacenará contraseñas ni credenciales sensibles de usuarios en su base de datos. |
| RB-U04 | UMBRAL almacenará una referencia local al usuario autenticado mediante el identificador proveniente de Keycloak. |
| RB-U05 | El administrador podrá crear usuarios desde UMBRAL mediante integración con Keycloak. |
| RB-U06 | El administrador deberá asignar un rol inicial al usuario durante su creación. |
| RB-U07 | Desde UMBRAL no se permitirá modificar el rol de un usuario después de su creación. |
| RB-U08 | El administrador podrá consultar, editar datos generales y desactivar usuarios vinculados a Keycloak. |
| RB-U09 | Un usuario desactivado no podrá acceder a partidas ni ejecutar acciones dentro del sistema. |
| RB-U10 | El liderazgo de equipo no constituye un rol de Keycloak, sino una condición de negocio administrada dentro de UMBRAL. |

## 

## *Reglas de negocio de trivias* {#reglas-de-negocio-de-trivias}

| ID | Regla de negocio |
| ----- | ----- |
| RB-T01 | Solo el operador puede crear formularios de Trivia. |
| RB-T02 | Un formulario de Trivia debe contener preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta. |
| RB-T03 | No se puede usar un formulario de Trivia incompleto para crear una partida. |
| RB-T04 | Solo el operador puede crear partidas de Trivia. |
| RB-T05 | Toda partida de Trivia debe estar asociada a un formulario de Trivia previamente creado y válido. |
| RB-T06 | Al crear una partida de Trivia, el operador debe definir nombre, modalidad, formulario asociado, mínimos de participación, máximos de participación y tiempo de inicio. |
| RB-T07 | Si la Trivia es individual, el máximo configurado corresponde a cantidad máxima de jugadores. |
| RB-T08 | Si la Trivia es por equipo, el máximo configurado corresponde a cantidad máxima de equipos. |
| RB-T09 | Si la Trivia es por equipo, el operador define mínimo y máximo de jugadores por equipo para esa partida. |
| RB-T10 | Al iniciar el lobby, la partida de Trivia queda publicada para todos los jugadores en el panel de Trivia. |
| RB-T11 | Cualquier jugador puede intentar entrar a una Trivia publicada. |
| RB-T12 | Si la Trivia es individual, cualquier jugador puede inscribirse mientras la partida esté en `lobby` y haya cupo. |
| RB-T13 | Si la Trivia es por equipo, solo el líder puede inscribir al equipo. |
| RB-T14 | Si un jugador que no es líder intenta entrar a una Trivia por equipo, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en este evento”. |
| RB-T15 | Cuando un líder inscribe a su equipo en una Trivia, el sistema envía convocatoria a los integrantes del equipo. |
| RB-T16 | Los integrantes convocados pueden aceptar o rechazar la convocatoria. |
| RB-T17 | La Trivia inicia cuando se cumple el tiempo definido por el operador o cuando el operador la inicia manualmente. |
| RB-T18 | Al iniciar la Trivia, la partida cambia a estado `iniciada`. |
| RB-T19 | Todos los jugadores reciben la misma pregunta al mismo tiempo. |
| RB-T20 | Todas las preguntas tienen un tiempo límite propio, definido en el formulario de Trivia. |
| RB-T21 | En modalidad individual, cada jugador solo puede enviar una respuesta por pregunta. |
| RB-T22 | En modalidad por equipos, solo puede registrarse una respuesta por equipo por pregunta. |
| RB-T23 | En modalidad por equipos, la respuesta válida del equipo será la primera opción seleccionada por cualquier integrante del equipo. |
| RB-T24 | El sistema debe rechazar respuestas repetidas, tardías o enviadas fuera de la pregunta activa. |
| RB-T25 | La pregunta activa se cierra cuando alguien responde correctamente o cuando se agota el tiempo. |
| RB-T26 | Si alguien responde correctamente, se cambia la pregunta para todos los jugadores. |
| RB-T27 | Si se agota el tiempo sin respuesta correcta, se cambia la pregunta para todos los jugadores. |
| RB-T28 | El puntaje se otorga únicamente cuando la respuesta es correcta. |
| RB-T29 | El puntaje de una respuesta correcta debe calcularse mediante la fórmula `puntaje_obtenido = puntaje_pregunta * (tiempo_restante / tiempo_total)`, donde `puntaje_pregunta` es el valor definido por el operador, `tiempo_restante` es el tiempo disponible al momento de responder correctamente y `tiempo_total` es el tiempo máximo configurado para la pregunta. |
| RB-T30 | El ranking de Trivia debe actualizarse en tiempo real. |
| RB-T31 | Durante la Trivia, el operador solo visualiza el ranking y la opción de cancelar la partida. |
| RB-T32 | Los jugadores deben poder consultar historial de partidas de Trivia individuales y por equipo. |
| RB-T33 | El historial de Trivia debe mostrar modalidad, fecha, puntaje, ranking obtenido y equipo asociado cuando aplique. |

## *Reglas de búsqueda de tesoro* {#reglas-de-búsqueda-de-tesoro}

| ID | Regla de negocio |
| ----- | ----- |
| RB-B01 | Solo el operador puede crear partidas de Búsqueda del Tesoro. |
| RB-B02 | Una partida BDT puede ser individual o por equipos. |
| RB-B03 | Al crear una BDT, el operador debe definir nombre de la partida, área de búsqueda, modalidad, mínimos de participación y máximos de participación. |
| RB-B04 | Si la BDT es individual, el máximo configurado corresponde a cantidad máxima de jugadores. |
| RB-B05 | Si la BDT es por equipo, el máximo configurado corresponde a cantidad máxima de equipos. |
| RB-B06 | Si la BDT es por equipo, el operador define la cantidad mínima de jugadores por equipo para esa partida. |
| RB-B07 | El operador debe definir las etapas de la BDT durante la creación de la partida. |
| RB-B08 | Cada etapa debe tener un tesoro configurado en forma de imagen/código QR. |
| RB-B09 | Cada etapa debe tener un tiempo límite definido por el operador. |
| RB-B10 | No se puede publicar una BDT sin al menos una etapa válida. |
| RB-B11 | No se puede publicar una etapa BDT sin QR esperado y tiempo límite. |
| RB-B12 | Al crear el lobby, la BDT queda publicada para todos los jugadores en el panel de Búsqueda del Tesoro. |
| RB-B13 | Cualquier jugador puede intentar entrar a una BDT publicada. |
| RB-B14 | Si la BDT es individual, cualquier jugador puede inscribirse mientras la partida esté en `lobby` y haya cupo. |
| RB-B15 | Si la BDT es por equipo, solo el líder puede inscribir al equipo. |
| RB-B16 | Si un jugador que no es líder intenta entrar a una BDT por equipo, el sistema debe mostrar: “Debes ser líder de un equipo para entrar en este evento”. |
| RB-B17 | Cuando un líder inscribe a su equipo en una BDT, el sistema envía convocatoria a los integrantes del equipo. |
| RB-B18 | Los integrantes convocados pueden aceptar o rechazar la convocatoria. |
| RB-B19 | Al iniciar la BDT, la partida cambia a estado `iniciada` y se activa la primera etapa. |
| RB-B20 | Durante una BDT iniciada, el jugador debe tener disponible la opción “subir tesoro”. |
| RB-B21 | Subir tesoro implica tomar o cargar una foto que contiene el supuesto QR encontrado. |
| RB-B22 | Al subir un tesoro, el sistema debe procesar la imagen enviada por el participante y decodificar el contenido del QR detectado. |
| RB-B23 | El sistema debe comparar el contenido decodificado del QR subido con el contenido esperado del QR configurado para la etapa activa. |
| RB-B24 | Si el contenido decodificado del QR coincide con el contenido esperado de la etapa activa, el tesoro se considera válido. |
| RB-B25 | Si el contenido decodificado del QR no coincide, no puede leerse o no corresponde a la etapa activa, el tesoro se considera inválido. |
| RB-B26 | Todo tesoro subido debe quedar registrado con jugador/equipo, partida, etapa, fecha/hora y resultado de validación. |
| RB-B27 | Si un jugador/equipo encuentra el tesoro correcto, gana la etapa. |
| RB-B28 | Cuando un jugador/equipo gana la etapa, la etapa se cierra para todos. |
| RB-B29 | Al cerrar una etapa con ganador, el sistema muestra quién consiguió el tesoro y en cuánto tiempo. |
| RB-B30 | Si se agota el tiempo de la etapa sin ganador, la etapa se cierra automáticamente. |
| RB-B31 | Si nadie consiguió el tesoro antes de agotarse el tiempo, el sistema muestra: “nadie consiguió el tesoro”. |
| RB-B32 | Al cerrarse una etapa, la partida avanza a la siguiente etapa si existe. |
| RB-B33 | Si se cierra la última etapa, la partida pasa a estado `terminada`. |
| RB-B34 | El operador puede enviar pistas a jugadores o equipos durante una BDT iniciada. |
| RB-B35 | El operador puede elegir a qué jugador/equipo enviar una pista. |
| RB-B36 | Las pistas enviadas deben quedar registradas en el historial. |
| RB-B37 | El operador debe ver la lista de jugadores/equipos inscritos en la BDT. |
| RB-B38 | El operador debe ver cada tesoro subido y si fue válido o inválido. |
| RB-B39 | Después de iniciada la BDT, el operador debe ver un mapa con la geolocalización de los participantes. |
| RB-B40 | El sistema debe solicitar permiso de ubicación al participante antes de compartir su geolocalización durante una partida BDT. |
| RB-B41 | Durante una partida BDT iniciada, la ubicación de los participantes debe actualizarse cada 2 segundos y mostrarse en el mapa del operador. |

# **Alcance** {#alcance}

El alcance del sistema UMBRAL comprende el desarrollo de una plataforma web para la gestión y operación en tiempo real de partidas interactivas bajo dos modos de juego definidos: Trivia y Búsqueda del Tesoro. El sistema no funcionará como un motor genérico de experiencias inmersivas, ni permitirá crear, configurar o ejecutar modos de juego distintos a los establecidos.

Toda partida creada en UMBRAL deberá estar asociada exactamente a uno de los dos modos soportados. A partir de esta definición, la plataforma permitirá centralizar los procesos de autenticación y acceso, gestión de equipos, creación de formularios de Trivia, creación de partidas de Trivia, creación de partidas de Búsqueda del Tesoro, publicación de lobbies, inscripción de jugadores o equipos, convocatorias, ejecución de dinámicas, validación de respuestas o tesoros, cálculo de puntajes, actualización de ranking, geolocalización operativa en BDT y trazabilidad de eventos relevantes.

El sistema cubrirá los flujos principales de administración, operación y participación, diferenciando las funcionalidades comunes de la plataforma y los comportamientos específicos de cada modo de juego. La interacción de los participantes desde dispositivos móviles será resuelta mediante una interfaz web responsive, no mediante aplicaciones móviles nativas.

| Área incluida | Descripción del alcance |
| ----- | ----- |
| Gestión de usuarios y roles | El sistema se integrará con Keycloak para autenticar usuarios y administrar roles base. UMBRAL permitirá crear usuarios mediante dicha integración, asignar rol inicial, consultar/editar datos generales, desactivar usuarios y almacenar únicamente una referencia local al identificador proveniente de Keycloak. |
| Gestión de equipos | El sistema permitirá crear equipos, generar código único de ingreso, unir participantes mediante código, limitar cada equipo a cinco jugadores, registrar líder, transferir liderazgo, salir de equipos y gestionar equipos administrativamente. Los equipos serán comunes para Trivia y Búsqueda del Tesoro. |
| Gestión de partidas | El sistema permitirá crear partidas únicamente bajo los modos Trivia o Búsqueda del Tesoro, con modalidad individual o por equipos, y manejar únicamente los estados `lobby`, `iniciada`, `cancelada` y `terminada`. |
| Panel del jugador | El participante contará con dos paneles principales: Trivia y Búsqueda del Tesoro. En cada panel podrá ver partidas publicadas, filtrar por modalidad individual o equipo, inscribirse cuando corresponda, aceptar o rechazar convocatorias y acceder a la dinámica activa. |
| Panel del operador | El operador podrá crear formularios de Trivia, crear partidas, publicar lobbies, iniciar partidas, cancelar partidas, visualizar ranking, enviar pistas en BDT, consultar tesoros subidos y visualizar geolocalización de participantes durante partidas BDT iniciadas. |
| Partidas individuales | El sistema permitirá que los jugadores participen individualmente aunque pertenezcan a un equipo. En estas partidas, el máximo configurado por el operador corresponde a cantidad máxima de jugadores. |
| Partidas por equipo | El sistema permitirá que solo el líder inscriba un equipo en partidas por equipo. Al inscribirlo, se enviarán convocatorias a los integrantes del equipo. En estas partidas, el máximo configurado por el operador corresponde a cantidad máxima de equipos. |
| Trivia | El sistema permitirá crear formularios de Trivia con preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta; crear partidas asociadas a formularios válidos; sincronizar preguntas; validar respuestas; calcular puntaje y actualizar ranking en tiempo real. |
| Búsqueda del Tesoro | El sistema permitirá crear partidas BDT con área de búsqueda, etapas, QR esperado por etapa y tiempo por etapa. Los participantes podrán subir fotos de QR encontrados y el sistema validará el tesoro mediante comparación del contenido decodificado del QR. |
| Geolocalización BDT | El sistema permitirá al operador visualizar en un mapa la ubicación de participantes durante partidas BDT iniciadas, con actualización cada dos segundos y previa autorización del participante. |
| Actualización en tiempo real | El sistema reflejará en tiempo real los cambios relevantes de publicación, lobby, estados, preguntas, temporizadores, ranking, etapas, pistas, geolocalización, resultados y eventos relevantes. |
| Puntuación y ranking | El sistema calculará y actualizará puntajes según las reglas del modo de juego. En Trivia, el puntaje de respuestas correctas se calculará mediante `puntaje_pregunta * (tiempo_restante / tiempo_total)`. |
| Trazabilidad operativa | El sistema registrará eventos relevantes como cambios de estado, inscripciones, convocatorias, respuestas, tesoros subidos, validaciones, pistas, ubicaciones relevantes, variaciones de puntaje, cancelaciones y resultados. |
| Procesamiento asíncrono | El sistema utilizará mensajería asíncrona para procesos secundarios como auditoría, consolidación de historial, notificaciones internas, actualización de ranking o procesamiento de eventos que no deban bloquear la operación principal. |

## *Alcance específico del modo Búsqueda del Tesoro* {#alcance-específico-del-modo-búsqueda-del-tesoro}

En el modo Búsqueda del Tesoro, el sistema permitirá al operador crear partidas individuales o por equipos, definiendo nombre, área de búsqueda, modalidad, cantidades mínimas y máximas de participación, etapas, QR esperado por etapa y tiempo límite por etapa. La partida se publicará mediante un lobby y, una vez iniciada, permitirá a los participantes subir fotos del QR encontrado como tesoro de la etapa activa.

En Búsqueda del Tesoro, el ranking se calculará según la cantidad de etapas ganadas y, en caso de empate, por el menor tiempo acumulado de resolución.

| Área incluida | Descripción del alcance |
| ----- | ----- |
| Creación de partida BDT | El operador podrá crear partidas de Búsqueda del Tesoro definiendo nombre, área de búsqueda, modalidad individual o equipo, cantidad mínima de participantes, máximo de jugadores si es individual, máximo de equipos si es por equipo y mínimo de jugadores por equipo cuando aplique. |
| Configuración de etapas | El operador podrá configurar una o más etapas para la partida. Cada etapa deberá tener un QR esperado y un tiempo límite. |
| Publicación en lobby | El operador podrá crear el lobby de la partida para publicarla y habilitar inscripciones de jugadores individuales o equipos, según su modalidad. |
| Inscripción individual | En partidas individuales, los jugadores podrán inscribirse mientras la partida esté en estado `lobby`, exista cupo disponible y se cumplan las reglas definidas. |
| Inscripción por equipos | En partidas por equipo, solo el líder podrá inscribir el equipo. Al hacerlo, el sistema enviará convocatoria a los integrantes del equipo para aceptar o rechazar su participación. |
| Inicio de partida | El operador podrá iniciar la partida desde el lobby cuando se cumplan las condiciones mínimas de participación. Al iniciar, la partida pasará a estado `iniciada` y se activará la primera etapa. |
| Panel del participante | Durante la partida iniciada, el participante visualizará la etapa activa, el temporizador y la opción “subir tesoro”. |
| Subida de tesoro | El participante podrá tomar o subir una foto del QR encontrado como tesoro de la etapa activa. |
| Validación automática de QR | El sistema procesará la imagen subida, decodificará el contenido del QR detectado y lo comparará con el contenido esperado del QR configurado para la etapa activa. |
| Resultado de validación | Si el contenido decodificado coincide con el esperado, el tesoro se marcará como válido. Si no coincide, no puede leerse o no corresponde a la etapa activa, se marcará como inválido. |
| Cierre de etapa | La etapa se cerrará cuando un jugador/equipo valide correctamente el QR esperado o cuando se agote el tiempo límite definido para la etapa. |
| Resultado de etapa | Si hubo ganador, el sistema mostrará quién consiguió el tesoro y en cuánto tiempo. Si nadie lo consigue, mostrará el mensaje “nadie consiguió el tesoro”. |
| Avance de etapa | Al cerrarse una etapa, el sistema avanzará a la siguiente etapa si existe. Si se cierra la última etapa, la partida pasará a estado `terminada`. |
| Pistas | El operador podrá enviar pistas a jugadores o equipos específicos durante una partida iniciada. Toda pista enviada deberá registrarse en el historial. |
| Monitoreo del operador | El operador podrá visualizar jugadores o equipos inscritos, etapa activa, tesoros subidos, resultado de validación y eventos relevantes de la partida. |
| Geolocalización | Durante una partida BDT iniciada, el sistema solicitará autorización de ubicación al participante y permitirá al operador visualizar su ubicación en un mapa con actualización cada dos segundos. |

## *Alcance específico del modo Trivia* {#alcance-específico-del-modo-trivia}

En el modo Trivia, el sistema permitirá al operador crear formularios de Trivia compuestos por preguntas, opciones de respuesta, respuesta correcta, puntaje y tiempo límite por pregunta. A partir de un formulario válido, el operador podrá crear partidas individuales o por equipos, publicarlas en lobby, iniciar la partida manualmente o por tiempo, sincronizar preguntas para todos los participantes, validar respuestas automáticamente, calcular puntajes y actualizar el ranking en tiempo real.

| Área incluida | Descripción del alcance |
| ----- | ----- |
| Gestión de formularios de Trivia | El operador podrá crear, editar y consultar formularios de Trivia. Cada formulario deberá contener preguntas, opciones de respuesta, respuesta correcta, puntaje asignado y tiempo límite por pregunta. |
| Validación de formularios | El sistema validará que el formulario esté completo antes de permitir su uso en una partida. No se podrán usar formularios sin preguntas, opciones, respuesta correcta, puntaje o tiempo por pregunta. |
| Creación de partida Trivia | El operador podrá crear partidas de Trivia asociadas a un formulario válido, definiendo nombre, modalidad individual o equipo, cantidad mínima de participantes, máximo de jugadores si es individual, máximo de equipos si es por equipo, mínimo y máximo de jugadores por equipo cuando aplique, y tiempo de inicio. |
| Publicación en lobby | El operador podrá iniciar el lobby de una partida de Trivia para publicarla y habilitar inscripciones. La partida aparecerá en el panel de Trivia de todos los jugadores. |
| Inscripción individual | En partidas individuales, cualquier jugador podrá inscribirse mientras la partida esté en estado `lobby`, exista cupo disponible y se cumplan las reglas de inscripción. |
| Inscripción por equipos | En partidas por equipo, solo el líder podrá inscribir el equipo. Al hacerlo, el sistema enviará convocatoria a los integrantes del equipo. |
| Inicio de partida | La partida de Trivia podrá iniciar manualmente por acción del operador o automáticamente al cumplirse el tiempo configurado. Al iniciar, pasará a estado `iniciada`. |
| Ejecución sincronizada | Durante la partida, todos los participantes recibirán la misma pregunta y las mismas opciones al mismo tiempo, con temporizador sincronizado. |
| Respuesta individual | En modalidad individual, el sistema aceptará una única respuesta por jugador por pregunta activa. |
| Respuesta por equipo | En modalidad por equipos, el sistema aceptará una única respuesta por equipo por pregunta activa, registrando como válida la primera opción seleccionada por cualquier integrante del equipo. |
| Validación automática | El sistema validará automáticamente cada respuesta contra la opción correcta configurada en la pregunta. |
| Cierre de pregunta | La pregunta activa se cerrará cuando algún jugador/equipo responda correctamente o cuando se agote el tiempo límite. |
| Cambio de pregunta | Al cerrarse una pregunta, el sistema avanzará automáticamente a la siguiente pregunta si existe. |
| Cálculo de puntaje | El sistema otorgará puntos solo a respuestas correctas y calculará el puntaje mediante la fórmula `puntaje_obtenido = puntaje_pregunta * (tiempo_restante / tiempo_total)`. |
| Ranking | El ranking de la partida se actualizará en tiempo real según los puntajes obtenidos. |
| Panel del operador | Durante una partida de Trivia iniciada, el operador visualizará únicamente el ranking actualizado y la opción de cancelar la partida, sin intervenir en las respuestas. |
| Historial | El participante podrá consultar su historial de partidas de Trivia individuales y por equipo, incluyendo modalidad, fecha, puntaje obtenido, posición en ranking y equipo asociado cuando aplique. |

## *Límites del alcance* {#límites-del-alcance}

Queda expresamente fuera del alcance del sistema la creación de modos de juego adicionales distintos a Trivia y Búsqueda del Tesoro. El sistema no permitirá configurar workflows genéricos, dinámicas personalizadas no contempladas por estos modos, ni experiencias inmersivas arbitrarias fuera del dominio definido.

También quedan fuera del alcance funcionalidades avanzadas como cobros en línea, integración con dispositivos físicos, inteligencia artificial aplicada al contenido, analítica histórica compleja, aplicaciones móviles nativas, navegación asistida, rutas históricas complejas de ubicación y cualquier integración externa que no sea necesaria para demostrar el flujo principal del sistema.

Aunque el enunciado base excluye funcionalidades avanzadas de geolocalización, el sistema sí incluirá una visualización operativa básica de ubicación de participantes durante partidas de Búsqueda del Tesoro iniciadas. Esta funcionalidad estará limitada a mostrar la ubicación de los participantes en un mapa para el operador, con actualización cada dos segundos y previa autorización del usuario. No incluirá análisis avanzado de rutas, navegación guiada, geocercas, optimización de recorrido ni almacenamiento histórico complejo de trayectorias.

La geolocalización podrá registrarse únicamente como dato operativo vigente o como evento puntual relevante de la partida, pero no se almacenarán trayectorias históricas completas ni rutas detalladas de desplazamiento.

La solución se concentrará en una aplicación web funcional, responsive, trazable y técnicamente defendible, capaz de demostrar los flujos principales de administración, operación y participación para los dos modos de juego definidos.

