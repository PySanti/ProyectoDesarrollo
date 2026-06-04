# SPECS-LIST — Active First Sprint Specs

OpenCode must only create, plan or implement features listed here.

## Active specs

| HU | Feature | Owning service | Client target | Actor | SDD folder | Status |
| --- | --- | --- | --- | --- | --- | --- |
| HU-01 | Crear usuario con rol inicial | Identity Service | React web | Administrador | docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/ | Completed / tested / backend-runtime-verified / frontend-runtime-verified |
| HU-02 | Consultar y editar datos generales de usuario | Identity Service | React web | Administrador | docs/04-sdd/specs/HU-02-consultar-y-editar-datos-generales-de-usuario/ | Completed / tested / backend-runtime-verified / acceptance updated |
| HU-03 | Crear equipo | Team Service | React Native mobile | Participante | docs/04-sdd/specs/HU-03-crear-equipo/ | Completed / tested / backend-runtime-verified / frontend-runtime-verified / hardening-10-10 / acceptance updated |
| HU-04 | Unirse a equipo usando código | Team Service | React Native mobile | Participante | docs/04-sdd/specs/HU-04-unirse-a-equipo-usando-codigo/ | Completed / tested / backend-verified / mobile-flow-tested / hardening-10-10 / postgresql-concurrency-verified / acceptance updated |
| HU-06 | Transferir liderazgo antes de salir del equipo | Team Service | React Native mobile | Participante líder | docs/04-sdd/specs/HU-06-transferir-liderazgo-antes-de-salir-del-equipo/ | 10/10 / completed / tested / backend-verified / mobile-flow-tested / PostgreSQL-verified / acceptance updated |
| HU-07 | Salir del equipo | Team Service | React Native mobile | Participante | docs/04-sdd/specs/HU-07-salir-del-equipo/ | Completed / tested / backend-HU07-verified / mobile-render-tested / hardening-10-10 / acceptance updated |
| HU-10 | Ver partidas de BDT publicadas | BDT Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-10-ver-partidas-de-bdt-publicadas/ | 10/10 / implemented / tested / backend-mobile-contract verified / PostgreSQL-verified / acceptance updated |
| HU-12 | Filtrar partidas de BDT por modalidad | BDT Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-12-filtrar-partidas-de-bdt-por-modalidad/ | 10/10 / implemented / tested / backend-mobile-contract verified / PostgreSQL-verified / acceptance updated |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder | BDT Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-14-advertencia-al-entrar-a-bdt-por-equipo-sin-ser-lider/ | Not started |
| HU-34 | Crear partida de Búsqueda del Tesoro | BDT Game Service | React web | Operador | docs/04-sdd/specs/HU-34-crear-partida-de-busqueda-del-tesoro/ | 10/10 / hardening completed / tested / PostgreSQL-verified / frontend multi-stage tested / acceptance updated |
| HU-37 | Ver lista de partidas de BDT publicadas | BDT Game Service | React web | Operador | docs/04-sdd/specs/HU-37-ver-lista-de-partidas-de-bdt-publicadas/ | 10/10 / hardening completed / tested / PostgreSQL-verified / frontend-tested / acceptance updated |
| HU-39 | Unirse a BDT individual | BDT Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-39-unirse-a-bdt-individual/ | 10/10 / implemented / tested / backend-verified / PostgreSQL-concurrency-verified / mobile-tested / acceptance updated |
| HU-40 | Unir equipo a BDT por equipos | BDT Game Service | React Native mobile | Participante líder | docs/04-sdd/specs/HU-40-unir-equipo-a-bdt-por-equipos/ | Not started |
| HU-42 | Ver participantes unidos a BDT publicada | BDT Game Service | React web | Operador | docs/04-sdd/specs/HU-42-ver-participantes-unidos-a-bdt-publicada/ | Not started |
| HU-43 | Iniciar partida BDT | BDT Game Service | React web | Operador | docs/04-sdd/specs/HU-43-iniciar-partida-bdt/ | 10/10 / completed / tested / PostgreSQL-concurrency-verified / SignalR-auth-and-scoped-delivery-verified / subscription-authorization-verified / acceptance updated |
| HU-44 | Ver etapa activa y opción de subir tesoro | BDT Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-44-ver-etapa-activa-y-opcion-de-subir-tesoro/ | 10/10 / hardening completed / tested / mobile geolocation adapter verified / live countdown verified / HU-45 navigation handoff verified / acceptance updated |
| HU-45 | Subir foto del tesoro QR | BDT Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-45-subir-foto-del-tesoro-qr/ | 10/10 / hardening completed / tested / native mobile adapters verified / real QR decoder verified / retrievable image storage verified / acceptance updated |
| HU-46 | Validar automáticamente QR enviado | BDT Game Service | Backend | Sistema | docs/04-sdd/specs/HU-46-validar-automaticamente-qr-enviado/ | Not started |
| HU-47 | Cerrar etapa BDT | BDT Game Service | Backend / React Native mobile | Sistema / Participante | docs/04-sdd/specs/HU-47-cerrar-etapa-bdt/ | Not started |
| HU-49 | Enviar pistas a participantes durante BDT | BDT Game Service | React web | Operador | docs/04-sdd/specs/HU-49-enviar-pistas-a-participantes-durante-bdt/ | Not started |
| HU-05 | Eliminar equipo creado | Team Service | React Native mobile | Participante líder | docs/04-sdd/specs/HU-05-eliminar-equipo-creado/ | Not started |
| HU-09 | Ver partidas de Trivia publicadas | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-09-ver-partidas-de-trivia-publicadas/ | Not started |
| HU-11 | Filtrar partidas de Trivia por modalidad | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-11-filtrar-partidas-de-trivia-por-modalidad/ | Not started |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-13-advertencia-al-entrar-a-trivia-por-equipo-sin-ser-lider/ | Not started |
| HU-15 | Crear formularios de Trivia | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-15-crear-formularios-de-trivia/ | Not started |
| HU-17 | Crear y publicar partida de Trivia | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-17-crear-y-publicar-partida-de-trivia/ | Not started |
| HU-18 | Unirse a Trivia individual | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-18-unirse-a-trivia-individual/ | Not started |
| HU-19 | Unir equipo a Trivia por equipos | Trivia Game Service | React Native mobile | Participante líder | docs/04-sdd/specs/HU-19-unir-equipo-a-trivia-por-equipos/ | Not started |
| HU-21 | Ver pantalla de espera de Trivia | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-21-ver-pantalla-de-espera-de-trivia/ | Not started |
| HU-22 | Ver participantes unidos a Trivia publicada | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-22-ver-participantes-unidos-a-trivia-publicada/ | Not started |
| HU-23 | Ver equipos unidos a Trivia publicada | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-23-ver-equipos-unidos-a-trivia-publicada/ | Not started |
| HU-24 | Iniciar manualmente Trivia | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-24-iniciar-manualmente-trivia/ | Not started |
| HU-26 | Responder Trivia individual | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-26-responder-trivia-individual/ | Not started |
| HU-27 | Responder Trivia por equipo | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-27-responder-trivia-por-equipo/ | Not started |
| HU-28 | Ver resultado al cerrar pregunta de Trivia | Trivia Game Service | React Native mobile | Participante | docs/04-sdd/specs/HU-28-ver-resultado-al-cerrar-pregunta-de-trivia/ | Not started |
| HU-29 | Calcular puntaje de respuesta en Trivia | Trivia Game Service | Backend / React Native mobile | Sistema / Participante | docs/04-sdd/specs/HU-29-calcular-puntaje-de-respuesta-en-trivia/ | Not started |
| HU-30 | Ver ranking durante Trivia | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/ | Not started |
| HU-35 | Ver lista de partidas de Trivia publicadas | Trivia Game Service | React web | Operador | docs/04-sdd/specs/HU-35-ver-lista-de-partidas-de-trivia-publicadas/ | Not started |

## Rules

- If a HU is not listed here, stop and report it as outside the active sprint scope.
- Every active HU must eventually have:
  - `spec.md`
  - `design.md`
  - `tasks.md`
  - `acceptance.md`
- Do not implement from `_deprecated`.
- Do not implement code before the SDD folder is complete.
