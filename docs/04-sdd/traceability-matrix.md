# Traceability Matrix — First Sprint

This matrix links active first-sprint user stories to requirements, services, clients, contracts and implementation status.

| HU | Feature | Requirement | Owning service | Supporting services | SDD folder | Contract files | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| HU-01 | Crear usuario con rol inicial | RF-01, RNF-13, RNF-14 | Identity Service | Keycloak (real adapter + backend runtime verification completed + React web login flow), PostgreSQL/EF Core (runtime verified; InMemory only for tests) | docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/ | contracts/http/identity-api.md | Completed / tested / backend-runtime-verified / frontend-runtime-verified |
| HU-02 | Consultar y editar datos generales de usuario | TBD by SDD | Identity Service | TBD by SDD | docs/04-sdd/specs/HU-02-consultar-y-editar-datos-generales-de-usuario/ | TBD after SDD | Not started |
| HU-03 | Crear equipo | TBD by SDD | Team Service | TBD by SDD | docs/04-sdd/specs/HU-03-crear-equipo/ | TBD after SDD | Not started |
| HU-04 | Unirse a equipo usando código | TBD by SDD | Team Service | TBD by SDD | docs/04-sdd/specs/HU-04-unirse-a-equipo-usando-codigo/ | TBD after SDD | Not started |
| HU-06 | Transferir liderazgo antes de salir del equipo | TBD by SDD | Team Service | TBD by SDD | docs/04-sdd/specs/HU-06-transferir-liderazgo-antes-de-salir-del-equipo/ | TBD after SDD | Not started |
| HU-07 | Salir del equipo | TBD by SDD | Team Service | TBD by SDD | docs/04-sdd/specs/HU-07-salir-del-equipo/ | TBD after SDD | Not started |
| HU-10 | Ver partidas de BDT publicadas | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-10-ver-partidas-de-bdt-publicadas/ | TBD after SDD | Not started |
| HU-12 | Filtrar partidas de BDT por modalidad | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-12-filtrar-partidas-de-bdt-por-modalidad/ | TBD after SDD | Not started |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-14-advertencia-al-entrar-a-bdt-por-equipo-sin-ser-lider/ | TBD after SDD | Not started |
| HU-34 | Crear partida de Búsqueda del Tesoro | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-34-crear-partida-de-busqueda-del-tesoro/ | TBD after SDD | Not started |
| HU-37 | Ver lista de partidas de BDT publicadas | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-37-ver-lista-de-partidas-de-bdt-publicadas/ | TBD after SDD | Not started |
| HU-39 | Unirse a BDT individual | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-39-unirse-a-bdt-individual/ | TBD after SDD | Not started |
| HU-40 | Unir equipo a BDT por equipos | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-40-unir-equipo-a-bdt-por-equipos/ | TBD after SDD | Not started |
| HU-42 | Ver participantes unidos a BDT publicada | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-42-ver-participantes-unidos-a-bdt-publicada/ | TBD after SDD | Not started |
| HU-43 | Iniciar partida BDT | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-43-iniciar-partida-bdt/ | TBD after SDD | Not started |
| HU-44 | Ver etapa activa y opción de subir tesoro | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-44-ver-etapa-activa-y-opcion-de-subir-tesoro/ | TBD after SDD | Not started |
| HU-45 | Subir foto del tesoro QR | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-45-subir-foto-del-tesoro-qr/ | TBD after SDD | Not started |
| HU-46 | Validar automáticamente QR enviado | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-46-validar-automaticamente-qr-enviado/ | TBD after SDD | Not started |
| HU-47 | Cerrar etapa BDT | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-47-cerrar-etapa-bdt/ | TBD after SDD | Not started |
| HU-49 | Enviar pistas a participantes durante BDT | TBD by SDD | BDT Game Service | TBD by SDD | docs/04-sdd/specs/HU-49-enviar-pistas-a-participantes-durante-bdt/ | TBD after SDD | Not started |
| HU-05 | Eliminar equipo creado | TBD by SDD | Team Service | TBD by SDD | docs/04-sdd/specs/HU-05-eliminar-equipo-creado/ | TBD after SDD | Not started |
| HU-09 | Ver partidas de Trivia publicadas | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-09-ver-partidas-de-trivia-publicadas/ | TBD after SDD | Not started |
| HU-11 | Filtrar partidas de Trivia por modalidad | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-11-filtrar-partidas-de-trivia-por-modalidad/ | TBD after SDD | Not started |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-13-advertencia-al-entrar-a-trivia-por-equipo-sin-ser-lider/ | TBD after SDD | Not started |
| HU-15 | Crear formularios de Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-15-crear-formularios-de-trivia/ | TBD after SDD | Not started |
| HU-17 | Crear y publicar partida de Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-17-crear-y-publicar-partida-de-trivia/ | TBD after SDD | Not started |
| HU-18 | Unirse a Trivia individual | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-18-unirse-a-trivia-individual/ | TBD after SDD | Not started |
| HU-19 | Unir equipo a Trivia por equipos | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-19-unir-equipo-a-trivia-por-equipos/ | TBD after SDD | Not started |
| HU-21 | Ver pantalla de espera de Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-21-ver-pantalla-de-espera-de-trivia/ | TBD after SDD | Not started |
| HU-22 | Ver participantes unidos a Trivia publicada | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-22-ver-participantes-unidos-a-trivia-publicada/ | TBD after SDD | Not started |
| HU-23 | Ver equipos unidos a Trivia publicada | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-23-ver-equipos-unidos-a-trivia-publicada/ | TBD after SDD | Not started |
| HU-24 | Iniciar manualmente Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-24-iniciar-manualmente-trivia/ | TBD after SDD | Not started |
| HU-26 | Responder Trivia individual | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-26-responder-trivia-individual/ | TBD after SDD | Not started |
| HU-27 | Responder Trivia por equipo | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-27-responder-trivia-por-equipo/ | TBD after SDD | Not started |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-28-ver-resultado-al-cerrar-pregunta-de-trivia/ | TBD after SDD | Not started |
| HU-29 | Calcular puntaje de respuesta en Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-29-calcular-puntaje-de-respuesta-en-trivia/ | TBD after SDD | Not started |
| HU-30 | Ver ranking durante Trivia | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/ | TBD after SDD | Not started |
| HU-35 | Ver lista de partidas de Trivia publicadas | TBD by SDD | Trivia Game Service | TBD by SDD | docs/04-sdd/specs/HU-35-ver-lista-de-partidas-de-trivia-publicadas/ | TBD after SDD | Not started |

## Notes

- `HU-01` and `HU-02` are owned by `Identity Service`.
- `HU-01` and `HU-02` are React web features because the actor is `Administrador`.
- Contract files must be filled after each SDD defines endpoints/events.
- No feature can be marked Done without tests and acceptance evidence.
