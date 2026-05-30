# First Delivery / Primer Sprint Scope

Este archivo define las historias activas del primer sprint. OpenCode debe considerar fuera de alcance cualquier HU que no aparezca en esta lista activa.

## Historias activas

| HU | Nombre | Responsable | Owning service | Cliente objetivo | Actor |
| --- | --- | --- | --- | --- | --- |
| HU-01 | Crear usuario con rol inicial | Santiago | Identity Service | React web | Administrador |
| HU-02 | Consultar y editar datos generales de usuario | Santiago | Identity Service | React web | Administrador |
| HU-03 | Crear equipo | Santiago | Team Service | React Native mobile | Participante |
| HU-04 | Unirse a equipo usando código | Santiago | Team Service | React Native mobile | Participante |
| HU-06 | Transferir liderazgo antes de salir del equipo | Santiago | Team Service | React Native mobile | Participante líder |
| HU-07 | Salir del equipo | Santiago | Team Service | React Native mobile | Participante |
| HU-10 | Ver partidas de BDT publicadas | Santiago | BDT Game Service | React Native mobile | Participante |
| HU-12 | Filtrar partidas de BDT por modalidad | Santiago | BDT Game Service | React Native mobile | Participante |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | Santiago | BDT Game Service | React Native mobile | Participante |
| HU-34 | Crear partida de Búsqueda del Tesoro | Santiago | BDT Game Service | React web | Operador |
| HU-37 | Ver lista de partidas de BDT publicadas | Santiago | BDT Game Service | React web | Operador |
| HU-39 | Unirse a BDT individual | Santiago | BDT Game Service | React Native mobile | Participante |
| HU-40 | Unir equipo a BDT por equipos | Santiago | BDT Game Service | React Native mobile | Participante líder |
| HU-42 | Ver participantes unidos a BDT publicada | Santiago | BDT Game Service | React web | Operador |
| HU-43 | Iniciar partida BDT | Santiago | BDT Game Service | React web | Operador |
| HU-44 | Ver etapa activa y opción de subir tesoro | Santiago | BDT Game Service | React Native mobile | Participante |
| HU-45 | Subir foto del tesoro QR | Santiago | BDT Game Service | React Native mobile | Participante |
| HU-46 | Validar automáticamente QR enviado | Santiago | BDT Game Service | Backend | Sistema |
| HU-47 | Cerrar etapa BDT | Santiago | BDT Game Service | Backend / React Native mobile | Sistema / Participante |
| HU-49 | Enviar pistas a participantes durante BDT | Santiago | BDT Game Service | React web | Operador |
| HU-05 | Eliminar equipo creado | Mariangel | Team Service | React Native mobile | Participante líder |
| HU-09 | Ver partidas de Trivia publicadas | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-11 | Filtrar partidas de Trivia por modalidad | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-15 | Crear formularios de Trivia | Mariangel | Trivia Game Service | React web | Operador |
| HU-17 | Crear y publicar partida de Trivia | Mariangel | Trivia Game Service | React web | Operador |
| HU-18 | Unirse a Trivia individual | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-19 | Unir equipo a Trivia por equipos | Mariangel | Trivia Game Service | React Native mobile | Participante líder |
| HU-21 | Ver pantalla de espera de Trivia | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-22 | Ver participantes unidos a Trivia publicada | Mariangel | Trivia Game Service | React web | Operador |
| HU-23 | Ver equipos unidos a Trivia publicada | Mariangel | Trivia Game Service | React web | Operador |
| HU-24 | Iniciar manualmente Trivia | Mariangel | Trivia Game Service | React web | Operador |
| HU-26 | Responder Trivia individual | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-27 | Responder Trivia por equipo | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | Mariangel | Trivia Game Service | React Native mobile | Participante |
| HU-29 | Calcular puntaje de respuesta en Trivia | Mariangel | Trivia Game Service | Backend / React Native mobile | Sistema / Participante |
| HU-30 | Ver ranking durante Trivia | Mariangel | Trivia Game Service | React web | Operador |
| HU-35 | Ver lista de partidas de Trivia publicadas | Mariangel | Trivia Game Service | React web | Operador |

## Reglas de alcance

- Las historias con actor `Administrador` u `Operador` pertenecen a React web.
- Las historias con actor `Participante` o `Participante líder` pertenecen a React Native mobile.
- Las historias con actor `Sistema` pertenecen al backend o procesamiento interno.
- Cada historia debe tener una carpeta SDD antes de implementarse.
- No se debe implementar ninguna HU que no esté en `docs/04-sdd/SPECS-LIST.md`.

## Cambios introducidos por este patch

- Se agregan `HU-01 Crear usuario con rol inicial` y `HU-02 Consultar y editar datos generales de usuario` al primer sprint.
- Ambas historias pertenecen a `Identity Service`.
- Ambas historias usan React web porque el actor es `Administrador`.
- Se corrige el nombre operativo de HU-29 para evitar el término `puntaje ponderado`; el nombre activo debe ser `Calcular puntaje de respuesta en Trivia`.
