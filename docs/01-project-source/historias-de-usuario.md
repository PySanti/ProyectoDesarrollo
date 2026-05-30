# Historias de usuario — Primer sprint

## Historias de Santiago

| ID | Nombre de historia | Responsable |
| --- | --- | --- |
| HU-01 | Crear usuario con rol inicial | Santiago |
| HU-02 | Consultar y editar datos generales de usuario | Santiago |
| HU-03 | Crear equipo | Santiago |
| HU-04 | Unirse a equipo usando código | Santiago |
| HU-06 | Transferir liderazgo antes de salir del equipo | Santiago |
| HU-07 | Salir del equipo | Santiago |
| HU-10 | Ver partidas de BDT publicadas | Santiago |
| HU-12 | Filtrar partidas de BDT por modalidad | Santiago |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | Santiago |
| HU-34 | Crear partida de Búsqueda del Tesoro | Santiago |
| HU-37 | Ver lista de partidas de BDT publicadas | Santiago |
| HU-39 | Unirse a BDT individual | Santiago |
| HU-40 | Unir equipo a BDT por equipos | Santiago |
| HU-42 | Ver participantes unidos a BDT publicada | Santiago |
| HU-43 | Iniciar partida BDT | Santiago |
| HU-44 | Ver etapa activa y opción de subir tesoro | Santiago |
| HU-45 | Subir foto del tesoro QR | Santiago |
| HU-46 | Validar automáticamente QR enviado | Santiago |
| HU-47 | Cerrar etapa BDT | Santiago |
| HU-49 | Enviar pistas a participantes durante BDT | Santiago |

## Historias de Mariangel

| ID | Nombre de historia | Responsable |
| --- | --- | --- |
| HU-05 | Eliminar equipo creado | Mariangel |
| HU-09 | Ver partidas de Trivia publicadas | Mariangel |
| HU-11 | Filtrar partidas de Trivia por modalidad | Mariangel |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | Mariangel |
| HU-15 | Crear formularios de Trivia | Mariangel |
| HU-17 | Crear y publicar partida de Trivia | Mariangel |
| HU-18 | Unirse a Trivia individual | Mariangel |
| HU-19 | Unir equipo a Trivia por equipos | Mariangel |
| HU-21 | Ver pantalla de espera de Trivia | Mariangel |
| HU-22 | Ver participantes unidos a Trivia publicada | Mariangel |
| HU-23 | Ver equipos unidos a Trivia publicada | Mariangel |
| HU-24 | Iniciar manualmente Trivia | Mariangel |
| HU-26 | Responder Trivia individual | Mariangel |
| HU-27 | Responder Trivia por equipo | Mariangel |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | Mariangel |
| HU-29 | Calcular puntaje de respuesta en Trivia | Mariangel |
| HU-30 | Ver ranking durante Trivia | Mariangel |
| HU-35 | Ver lista de partidas de Trivia publicadas | Mariangel |

## Módulo 0: Gestión de Usuarios y Roles

Este bloque permite al administrador gestionar los usuarios base del sistema mediante integración con Keycloak, asignando un rol inicial durante la creación y manteniendo actualizados los datos generales de usuario.

* **HU-01 (Crear usuario con rol inicial):** El administrador crea usuarios en la plataforma y asigna un rol inicial, que no podrá modificarse posteriormente desde UMBRAL.
* **HU-02 (Consultar y editar datos generales de usuario):** El administrador consulta usuarios existentes y edita sus datos generales, sin modificar el rol asignado durante la creación.

## Módulo 1: Registro y Gestión de Equipos

Este bloque permite a los usuarios organizarse antes de jugar. Sirve para configurar el equipo que podrá participar en partidas de Trivia o Búsqueda del Tesoro.

* **HU-03 (Crear equipo):** Un participante crea su equipo si no pertenece a otro, asume el rol de líder y el sistema le genera un código único.
* **HU-04 (Unirse a equipo):** Un participante se une a un equipo existente validando el código, respetando el límite máximo de 5 jugadores.
* **HU-05 (Eliminar equipo):** El líder puede eliminar su equipo, informando a los integrantes y conservando el historial previo según reglas del dominio.
* **HU-06 (Transferir liderazgo):** Si el líder decide salir y hay más jugadores, debe designar un nuevo líder antes de salir.
* **HU-07 (Salir del equipo):** Un participante común puede abandonar el equipo directamente.

## Módulo 2: Navegación, Filtros y Control de Accesos

Regula la interfaz de la aplicación móvil para que el participante encuentre partidas y el sistema restrinja el acceso según modalidad, liderazgo e inscripción.

* **HU-09 (Ver Trivias):** Panel del participante para visualizar las partidas de Trivia publicadas.
* **HU-10 (Ver BDT):** Panel del participante para visualizar las partidas de Búsqueda del Tesoro publicadas.
* **HU-11 (Filtrar Trivia):** Permite al participante filtrar las trivias del listado entre individuales y de equipo.
* **HU-12 (Filtrar BDT):** Permite al participante filtrar las BDT del listado entre individuales y de equipo.
* **HU-13 (Advertencia Trivia):** Si un participante no líder intenta acceder a una Trivia por equipo, el sistema bloquea el paso con una advertencia.
* **HU-14 (Advertencia BDT):** Si un participante no líder intenta acceder a una BDT por equipo, el sistema bloquea el paso con una advertencia.

## Módulo 3: Motor de Trivia

Maneja el flujo de preguntas en tiempo real, respuestas individuales o por equipo, cierre de preguntas y ranking.

* **HU-15 (Crear formulario):** El operador diseña preguntas, opciones, respuestas correctas, puntajes y tiempos límite.
* **HU-17 (Crear y publicar partida de Trivia):** El operador crea y publica una partida definiendo nombre, formulario, modalidad y límites de participación.
* **HU-35 (Lista de Trivias):** El operador consulta los nombres y estados de las partidas de Trivia publicadas.
* **HU-18 (Unión individual):** Permite a cualquier participante unirse por su cuenta a una Trivia individual.
* **HU-19 (Unión por equipo):** El líder inscribe su equipo en una Trivia por equipos.
* **HU-21 (Lobby de espera):** Pantalla de espera para participantes o equipos tras unirse exitosamente.
* **HU-22 (Observar participantes):** Panel donde el operador visualiza en tiempo real a los participantes o equipos en lobby.
* **HU-23 (Ver equipos unidos):** Permite al operador observar los equipos unidos a la partida de Trivia publicada.
* **HU-24 (Inicio de Trivia):** El operador inicia manualmente la Trivia si se cumplen las condiciones mínimas.
* **HU-26 (Respuesta individual):** En modalidad individual, el sistema acepta una única respuesta por jugador para cada pregunta.
* **HU-27 (Respuesta por equipo):** En modalidad por equipo, la primera respuesta enviada por cualquier integrante activo fija la opción del equipo.
* **HU-28 (Cierre de pregunta):** La pregunta se cierra al acertar o al agotar el temporizador y muestra la respuesta correcta.
* **HU-29 (Calcular puntaje de respuesta en Trivia):** El sistema otorga el puntaje configurado solo si la respuesta es correcta, sin ponderación por tiempo.
* **HU-30 (Ranking durante Trivia):** Durante la partida, el operador visualiza el ranking en vivo y puede cancelar si corresponde.

## Módulo 4: Búsqueda del Tesoro

Controla el juego de etapas, QR esperado, subida de tesoros, validación automática, pistas y ranking BDT.

* **HU-34 (Crear BDT):** El operador configura la partida definiendo área de búsqueda textual, etapas, QR esperado y tiempos límite.
* **HU-37 (Lista de BDT):** El operador consulta el estado de las partidas BDT publicadas.
* **HU-39 (Unión BDT individual):** Permite a un participante unirse a una BDT individual y esperar en el lobby.
* **HU-40 (Unión BDT por equipo):** El líder inscribe su equipo en una BDT por equipos.
* **HU-42 (Monitoreo de lobby):** El operador visualiza en tiempo real los individuos o equipos que se suman al lobby.
* **HU-43 (Inicio de BDT):** La partida inicia cuando se cumplen las condiciones mínimas.
* **HU-44 (Consola del jugador):** La app móvil muestra etapa activa, temporizador y acción para subir tesoro.
* **HU-45 (Subir tesoro):** El participante captura o sube una imagen de QR para que el sistema intente decodificarla.
* **HU-46 (Validación automática):** El backend procesa el contenido decodificado del QR y lo compara con el QR esperado.
* **HU-47 (Cierre de etapa):** La etapa concluye si se valida correctamente el QR o si vence el temporizador.
* **HU-49 (Pistas del operador):** El operador envía pistas de texto en tiempo real a participantes o equipos.
