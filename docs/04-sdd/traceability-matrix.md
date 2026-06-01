# Traceability Matrix — First Sprint

This matrix links active first-sprint user stories to requirements, services, clients, contracts and implementation status.

| HU | Feature | Delivery | Owning service | Supporting services | Client target | Contract files | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| HU-01 | Crear usuario con rol inicial | First sprint | Identity Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-02 | Consultar y editar datos generales de usuario | First sprint | Identity Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-03 | Crear equipo | First sprint | Team Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-04 | Unirse a equipo usando código | First sprint | Team Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-06 | Transferir liderazgo antes de salir del equipo | First sprint | Team Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-07 | Salir del equipo | First sprint | Team Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-10 | Ver partidas de BDT publicadas | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-12 | Filtrar partidas de BDT por modalidad | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-34 | Crear partida de Búsqueda del Tesoro | First sprint | BDT Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-37 | Ver lista de partidas de BDT publicadas | First sprint | BDT Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-39 | Unirse a BDT individual | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-40 | Unir equipo a BDT por equipos | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-42 | Ver participantes unidos a BDT publicada | First sprint | BDT Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-43 | Iniciar partida BDT | First sprint | BDT Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-44 | Ver etapa activa y opción de subir tesoro | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-45 | Subir foto del tesoro QR | First sprint | BDT Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-46 | Validar automáticamente QR enviado | First sprint | BDT Game Service | TBD by SDD | Backend | TBD after SDD | Not started |
| HU-47 | Cerrar etapa BDT | First sprint | BDT Game Service | TBD by SDD | Backend / React Native mobile | TBD after SDD | Not started |
| HU-49 | Enviar pistas a participantes durante BDT | First sprint | BDT Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-05 | Eliminar equipo creado | First sprint | Team Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-09 | Ver partidas de Trivia publicadas | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | React Native mobile | `contracts/http/trivia-game-api.md` (GET `/api/trivia-games`) | Backend done — 5 tests added (216 total) |
| HU-11 | Filtrar partidas de Trivia por modalidad | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | React Native mobile | `contracts/http/trivia-game-api.md` (GET `/api/trivia-games?modalidad=`) | Backend done — 8 tests added (224 total) |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | First sprint | Trivia Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-15 | Crear formularios de Trivia | First sprint | Trivia Game Service | Identity Service (JWT / Operador role) | React web | `contracts/http/trivia-game-api.md` (POST/PUT/GET `/api/trivia-forms`) | Backend done — 139 tests |
| HU-17 | Crear y publicar partida de Trivia | First sprint | Trivia Game Service | Identity Service (JWT / Operador role) | React web | `contracts/http/trivia-game-api.md` (POST/PUT/GET `/api/trivia-forms`, POST/GET `/api/trivia-games`, POST `/api/trivia-games/{id}/start`) | Backend done — 211 tests |
| HU-18 | Unirse a Trivia individual | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | React Native mobile | `contracts/http/trivia-game-api.md` (POST `/api/trivia-games/{id}/join`) | Backend done — 12 tests added (236 total) |
| HU-19 | Unir equipo a Trivia por equipos | First sprint | Trivia Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-21 | Ver pantalla de espera de Trivia | First sprint | Trivia Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-22 | Ver participantes unidos a Trivia publicada | First sprint | Trivia Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-23 | Ver equipos unidos a Trivia publicada | First sprint | Trivia Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-24 | Iniciar manualmente Trivia | First sprint | Trivia Game Service | TBD by SDD | React web | TBD after SDD | Not started |
| HU-26 | Responder Trivia individual | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | React Native mobile | `contracts/http/trivia-game-api.md` (POST `/api/trivia-games/{id}/questions/{preguntaId}/answer`) | Backend done — 8 API tests + 10 app tests + 13 domain tests (55 API pass total, 109 app, 154 domain) |
| HU-27 | Responder Trivia por equipo | First sprint | Trivia Game Service | TBD by SDD | React Native mobile | TBD after SDD | Not started |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | React Native mobile | `contracts/http/trivia-game-api.md` (GET `/api/trivia-games/{id}/questions/{preguntaId}/result`) | Backend done — 6 tests added (4 API + 5 app + 5 domain tests; 55 API pass, 109 app, 154 domain) |
| HU-29 | Calcular puntaje de respuesta en Trivia | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | Backend / React Native mobile | `contracts/http/trivia-game-api.md` (GET `/api/trivia-games/{id}/score`) | Backend done — 5 API score tests added (55 API pass total, 109 app, 154 domain) |
| HU-30 | Ver ranking durante Trivia | First sprint | Trivia Game Service | Identity Service (JWT / autenticación) | React web | `contracts/http/trivia-game-api.md` (GET `/api/trivia-games/{id}/ranking`, SignalR `/hubs/trivia-ranking`) | Backend done — 4 app tests + 3 API tests added (154 domain, 113 app, 58 API pass) |
| HU-35 | Ver lista de partidas de Trivia publicadas | First sprint | Trivia Game Service | TBD by SDD | React web | TBD after SDD | Not started |

## Notes

- `HU-01` and `HU-02` are owned by `Identity Service`.
- `HU-01` and `HU-02` are React web features because the actor is `Administrador`.
- Contract files must be filled after each SDD defines endpoints/events.
- No feature can be marked Done without tests and acceptance evidence.
