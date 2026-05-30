# First Delivery Scope — UMBRAL

Este archivo resume las historias seleccionadas para la primera entrega según el documento de historias de usuario.

## Principio de alcance

La primera entrega se concentra en:

1. Gestión de equipos por participantes.
2. Navegación de partidas publicadas y filtros.
3. Motor funcional de Trivia.
4. Motor funcional de Búsqueda del Tesoro.
5. Lobbies, inicio, respuesta/envío, validación, cierre, ranking/pistas según modo.

Las historias no listadas aquí no deben implementarse en la primera entrega salvo que sean dependencias técnicas mínimas.

## Historias asignadas a Santiago

| HU | Nombre | Módulo |
|---|---|---|
| HU-03 | Crear equipo | Equipos |
| HU-04 | Unirse a equipo usando código | Equipos |
| HU-06 | Transferir liderazgo antes de salir del equipo | Equipos |
| HU-07 | Salir del equipo | Equipos |
| HU-10 | Ver partidas de BDT publicadas | Navegación BDT |
| HU-12 | Filtrar partidas de BDT por modalidad | Navegación BDT |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | Acceso BDT |
| HU-34 | Crear partida de Búsqueda del Tesoro | BDT |
| HU-37 | Ver lista de partidas de BDT publicadas | Operador BDT |
| HU-39 | Unirse a BDT individual | BDT |
| HU-40 | Unir equipo a BDT por equipos | BDT |
| HU-42 | Ver participantes unidos a BDT publicada | Lobby BDT |
| HU-43 | Iniciar partida BDT | BDT |
| HU-44 | Ver etapa activa y opción de subir tesoro | BDT |
| HU-45 | Subir foto del tesoro QR | BDT |
| HU-46 | Validar automáticamente QR enviado | BDT |
| HU-47 | Cerrar etapa BDT | BDT |
| HU-49 | Enviar pistas a participantes durante BDT | BDT |

## Historias asignadas a Mariangel

| HU | Nombre | Módulo |
|---|---|---|
| HU-05 | Eliminar equipo creado | Equipos |
| HU-09 | Ver partidas de Trivia publicadas | Navegación Trivia |
| HU-11 | Filtrar partidas de Trivia por modalidad | Navegación Trivia |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | Acceso Trivia |
| HU-15 | Crear formularios de Trivia | Trivia |
| HU-17 | Crear y publicar partida de Trivia | Trivia |
| HU-18 | Unirse a Trivia individual | Trivia |
| HU-19 | Unir equipo a Trivia por equipos | Trivia |
| HU-21 | Ver pantalla de espera de Trivia | Lobby Trivia |
| HU-22 | Ver participantes unidos a Trivia publicada | Lobby Trivia |
| HU-23 | Ver equipos unidos a Trivia publicada | Lobby Trivia |
| HU-24 | Iniciar manualmente Trivia | Trivia |
| HU-26 | Responder Trivia individual | Trivia |
| HU-27 | Responder Trivia por equipo | Trivia |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | Trivia |
| HU-29 | Calcular puntaje de respuesta en Trivia | Trivia |
| HU-30 | Ver ranking durante Trivia | Trivia |
| HU-35 | Ver lista de partidas de Trivia publicadas | Operador Trivia |

## Agrupación por módulo

### Módulo 1 — Equipos

| HU | Alcance |
|---|---|
| HU-03 | Crear equipo, validar que el usuario no pertenece a otro, generar código, asignar líder. |
| HU-04 | Unirse por código válido, validar no pertenencia y límite máximo. |
| HU-05 | Eliminar equipo, remover para integrantes y notificar. |
| HU-06 | Transferir liderazgo antes de salida del líder. |
| HU-07 | Salir de equipo según sea líder o miembro. |

### Módulo 2 — Navegación y acceso

| HU | Alcance |
|---|---|
| HU-09 | Listado de partidas Trivia publicadas para participante. |
| HU-10 | Listado de partidas BDT publicadas para participante. |
| HU-11 | Filtro de Trivia por modalidad. |
| HU-12 | Filtro de BDT por modalidad. |
| HU-13 | Bloqueo/advertencia para Trivia por equipo si no es líder. |
| HU-14 | Bloqueo/advertencia para BDT por equipo si no es líder. |

### Módulo 3 — Trivia

| HU | Alcance |
|---|---|
| HU-15 | Crear formularios de Trivia. |
| HU-17 | Crear y publicar partida Trivia. |
| HU-18 | Unirse a Trivia individual. |
| HU-19 | Unir equipo a Trivia. |
| HU-21 | Pantalla de espera. |
| HU-22 | Operador ve participantes unidos. |
| HU-23 | Operador ve equipos unidos. |
| HU-24 | Iniciar manualmente Trivia. |
| HU-26 | Respuesta individual. |
| HU-27 | Respuesta por equipo: primera respuesta válida. |
| HU-28 | Mostrar resultado al cerrar pregunta. |
| HU-29 | Calcular puntaje. |
| HU-30 | Ranking durante Trivia. |
| HU-35 | Listado de partidas Trivia publicadas para operador. |

### Módulo 4 — Búsqueda del Tesoro

| HU | Alcance |
|---|---|
| HU-34 | Crear partida BDT con área, etapas, QR y temporizador. |
| HU-37 | Operador ve partidas BDT publicadas. |
| HU-39 | Unión individual a BDT. |
| HU-40 | Unión por equipo a BDT. |
| HU-42 | Operador ve participantes/equipos unidos. |
| HU-43 | Iniciar BDT. |
| HU-44 | Participante ve etapa activa y opción subir tesoro. |
| HU-45 | Subir foto del QR. |
| HU-46 | Validación automática del QR. |
| HU-47 | Cierre de etapa. |
| HU-49 | Enviar pistas a participantes durante BDT. |

## Dependencias técnicas mínimas

Aunque no estén asignadas en la lista separada de primera entrega, estas capacidades pueden ser necesarias como infraestructura mínima:

- Usuario autenticado simulado o integrado con Keycloak.
- Roles mínimos para Administrador, Operador y Participante.
- Persistencia por microservicio o por contexto.
- Contratos HTTP para frontend.
- Canal de tiempo real para lobbies/ranking/etapas cuando aplique.
- Eventos mínimos para auditoría/ranking si la arquitectura los exige.

## Fuera de alcance por defecto

Salvo decisión explícita, no implementar en primera entrega:

- analítica histórica avanzada;
- pagos;
- app móvil nativa;
- inteligencia artificial;
- integración con dispositivos físicos;
- modos distintos a Trivia o BDT;
- funciones administrativas no necesarias para las HU seleccionadas.
