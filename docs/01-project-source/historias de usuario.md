## **Historias de Santiago**

| ID | Nombre de historia | Responsable |
| ----- | ----- | ----- |
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

## **Historias de Mariangel**

| ID | Nombre de historia | Responsable |
| ----- | ----- | ----- |
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

## **Módulo 1: Registro y Gestión de Equipos**

Este bloque permite a los usuarios organizarse antes de jugar. Al no haber competencia masiva, sirve para configurar el equipo que jugará la partida contra el reloj.

* **HU-03 (Crear equipo):** Un participante crea su equipo si no pertenece a otro, asume el rol de líder y el sistema le genera un código único.  
   PDF  
* **HU-04 (Unirse a equipo):** Un participante se une a un grupo existente validando el código, respetando el límite de máximo 5 jugadores.  
* **HU-05 (Eliminar equipo):** El líder puede disolver el grupo completo, eliminándolo para el resto e informándoles de la acción.  
* **HU-06 (Transferir liderazgo):** Si el líder decide salir y hay más jugadores, el sistema le exige designar un nuevo líder antes de marcharse.  
* **HU-07 (Salir del equipo):** Un participante común puede abandonar el grupo directamente en cualquier momento.

## **🧭 Módulo 2: Navegación, Filtros y Control de Accesos**

Regula la interfaz de la aplicación móvil para que el usuario encuentre los eventos y restringe el acceso según su rol.

* **HU-09 (Ver Trivias):** Panel del participante para visualizar todas las partidas de trivia que han sido publicadas.  
* **HU-10 (Ver BDT):** Panel del participante para visualizar las partidas de Búsqueda del Tesoro publicadas.  
* **HU-11 (Filtrar Trivia):** Permite al participante filtrar las trivias del listado entre individuales y de equipo.  
* **HU-12 (Filtrar BDT):** Permite al participante filtrar las BDT del listado entre individuales y de equipo.  
* **HU-13 (Advertencia Trivia):** Si un jugador común intenta acceder a una trivia grupal sin ser el líder de un equipo, el sistema bloquea el paso con una advertencia.  
* **HU-14 (Advertencia BDT):** Si un jugador común intenta ingresar a una BDT grupal sin ser líder, el sistema bloquea el paso indicando que debe ser líder para inscribir al grupo.

## **🧠 Módulo 3: Motor de Trivia (Individual o 1 Equipo Activo)**

Maneja el flujo de preguntas en tiempo real. Si juega un equipo, la primera respuesta congela la pantalla de los demás.

* **HU-15 (Crear formulario):** El operador diseña las preguntas, opciones, respuestas correctas, puntajes y tiempos límite.  
* **HU-17 (Crear partida):** El operador publica una partida definiendo el nombre, formulario, límites y si es individual o por equipos.  
* **HU-35 (Lista de Trivias):** Consola del operador para auditar y consultar los nombres y estados de las partidas publicadas.  
* **HU-18 (Unión Individual):** Permite a cualquier jugador unirse por su cuenta a una trivia individual disponible.  
* **HU-19 (Unión por Equipo):** El líder es el único facultado para inscribir a su equipo completo en una trivia grupal.  
* **HU-21 (Lobby de espera):** Pantalla de espera para los participantes o equipos tras unirse exitosamente a la partida.  
* **HU-22 (Observar lobby):** Panel donde el operador visualiza en tiempo real a los jugadores o equipos que están en la sala de espera.  
* **HU-23 (Aceptar/Rechazar):** Permite al operador admitir o denegar manualmente el acceso de los competidores desde el lobby.  
* **HU-24 (Inicio de Trivia):** La partida arranca al cumplirse el tiempo programado o cuando el operador la inicia manualmente.  
* **HU-26 (Respuesta Individual):** En la modalidad individual, el sistema procesa y acepta una única respuesta por jugador para cada pregunta.  
* **HU-27 (Respuesta por Equipo):** En la modalidad grupal, la primera respuesta enviada por cualquier integrante fija la opción válida para todo el equipo.  
* **HU-28 (Cierre de pregunta):** La ronda se cierra cuando expira el tiempo, evaluando la respuesta y mostrando en la app móvil el resultado correcto.  
* **HU-29 (Puntaje ponderado):** El sistema otorga y acumula los puntos preconfigurados únicamente si la opción seleccionada fue la correcta.  
* **HU-30 (Panel de control):** Durante la partida, el operador visualiza el rendimiento o ranking en vivo del competidor actual y tiene la opción de cancelar el juego.

## **🗺️ Módulo 4: Búsqueda del Tesoro (Individual o 1 Equipo Activo)**

Controla el juego de hitos físicos validando códigos de barras bidimensionales mediante la cámara del celular.

* **HU-34 (Crear BDT):** El operador configura la partida definiendo el área (texto), las etapas, el QR esperado y sus tiempos límites.  
* **HU-37 (Lista de BDT):** Panel del operador para verificar el estado de las publicaciones de búsqueda de tesoro en el sistema.  
* **HU-39 (Unión BDT Individual):** Permite a un jugador unirse por su cuenta a una partida de BDT individual y esperar en el lobby.  
* **HU-40 (Unión BDT por Equipo):** Habilita al líder a inscribir a su grupo entero a un evento de búsqueda de tesoro.  
* **HU-42 (Monitoreo de lobby):** Muestra al operador en tiempo real los individuos o equipos que se van sumando a la sala de espera de la BDT.  
* **HU-43 (Inicio de BDT):** La partida da comienzo formal una vez que el operador verifica que se cumplen las condiciones mínimas.  
* **HU-44 (Consola del jugador):** Interfaz móvil que le muestra al usuario o equipo la pista de la etapa activa, el temporizador y el botón de acción.  
* **HU-45 (Subir tesoro):** Permite al participante capturar o subir la foto de un código QR desde el dispositivo móvil para que el sistema intente decodificarlo.  
* **HU-46 (Validación automática):** El backend procesa el string decodificado del QR y determina automáticamente si corresponde al hito esperado de la etapa activa.  
* **HU-47 (Cierre de etapa):** La fase concluye de inmediato si el competidor valida con éxito el código QR o si el temporizador de la etapa llega a cero.  
* **HU-49 (Pistas del operador):** El operador puede redactar y enviar cadenas de texto en tiempo real dirigidas exclusivamente a los participantes para orientar su búsqueda.

