# Auditoría de cobertura de requisitos — estado vs. documentación (2026-07-06)

Rama: `feature/code-migration-SP-5` (HEAD `48799d2`)
Tipo: auditoría de cobertura requisito-por-requisito (no de conformidad de un slice).
Pregunta que responde: **¿cuánto falta exactamente para que el proyecto cumpla al 100% con la documentación?**

## Fuentes e inventario

| Eje | Fuente | Cantidad |
|---|---|---|
| Historias de usuario (HU) | `docs/01-project-source/srs.md` §Historias (HU-01..HU-50) | 50 |
| Reglas de negocio (BR) | `docs/02-project-context/business-rules.md` (BR-G/R/E/T/B/C) | 46 |
| Requisitos no funcionales (RNF) | `docs/01-project-source/srs.md` §Requerimientos no funcionales (RNF-01..RNF-24) | 24 |
| **Total** | | **120** |

Método: cruce de cada requisito contra (a) la matriz `docs/04-sdd/traceability-matrix.md` (slices SP-2..SP-5c),
(b) verificaciones directas de código/config donde había duda (controllers de identity y operaciones-sesion,
`.env` de frontend/mobile, `frontend/src/auth/`, `mobile/src/config/env.ts`, existencia de `.github/workflows/`,
listado de `services/`), y (c) las auditorías de conformidad previas (2026-06-27, 2026-07-02, ambas CONFORME).

Criterio de "cumplido pleno": el actor documentado puede ejecutar la HU **end-to-end en el cliente documentado
contra el backend nuevo**. "Backend listo" = la lógica vive en los 4 servicios doctrinales, verde y auditada,
pero el cliente aún no la consume.

## Resultado global

| Eje | Cumplido pleno | Backend listo, cliente sin cablear | Falta backend | Total |
|---|---|---|---|---|
| HU | 9 | 30 (2 parciales`*`) | 11 | 50 |
| BR | 35 | — | 11 (7 SP-4 · 3 equipos · 1 transporte) | 46 |
| RNF | 12 | 7 parciales | 5 | 24 |
| **Total** | **56 (~47%)** | **37** | **27** | **120** |

- **~47%** cumplido estricto end-to-end.
- **~78%** contando lo que ya tiene backend nuevo terminado y solo espera cableado de clientes.
- El backend nuevo cubre **40/50 HUs (80%)**: el hueco dominante no es lógica de negocio, es conexión de clientes.

`*` HU-35 (el payload SignalR `EtapaGanada` no porta identidad del ganador — decisión anti-leak SP-3e-3; el dato
existe en dominio/eventos RabbitMQ) y HU-38 (panel de monitoreo de envíos: los `TesoroQR` se registran pero no hay
query de operador dedicada).

## Detalle por estado

### HU cumplidas end-to-end (9)

HU-01, HU-02, HU-03, HU-04 (usuarios + gobernanza, web vía identity — SP-1/1R + SP-5b/5c),
HU-05, HU-07, HU-08, HU-46, HU-47 (equipos core + invitaciones, mobile vía identity — SP-1).
Nota: los clientes llegan **directo** al servicio, no por gateway — eso se contabiliza como RNF-21, no contra estas HU.

### HU con backend nuevo listo, pendientes solo de cliente (30)

HU-10, HU-11, HU-12, HU-13, HU-14, HU-15, HU-16, HU-17, HU-18, HU-20, HU-21, HU-22, HU-23, HU-24,
HU-28, HU-29, HU-30, HU-31, HU-32, HU-33, HU-34, HU-35`*`, HU-36, HU-37, HU-38`*`, HU-39, HU-40, HU-41, HU-42, HU-45.
Respaldo: SP-2 (configuración Partidas), SP-3a..3i (lobby, inscripciones Individual/Equipo, convocatorias, inicio
manual/automático, runtime Trivia/BDT Individual/Equipo, pistas, geolocalización, reconexión, SignalR, RabbitMQ).

### HU que requieren backend nuevo (11)

| HU | Qué falta | Bloque |
|---|---|---|
| HU-25 (parcial) | acumulación de `PuntajeAcumulado` (el evento `PuntajeTriviaIncrementado` ya se emite) | 1 — SP-4 |
| HU-26 | ranking Trivia en vivo para operador | 1 — SP-4 |
| HU-27 | historial único de partidas del participante | 1 — SP-4 |
| HU-43 | historial/auditoría de partida para operador | 1 — SP-4 |
| HU-44 | ranking nativo BDT (puntos de etapas ganadas) | 1 — SP-4 |
| HU-49 | rendimiento del equipo por partida | 1 — SP-4 |
| HU-50 | ranking consolidado de la partida | 1 — SP-4 |
| HU-06 | eliminar equipo (endpoint inexistente) + guard partida Lobby/Iniciada + notificación | 4 — equipos-admin |
| HU-09 | gestión administrativa de equipos (CRUD admin, cero endpoints) | 4 — equipos-admin |
| HU-19 | operador acepta/rechaza inscripciones (hoy inscripción directa, sin aprobación) | 4 — hallazgo |
| HU-48 | historial de nombres de equipos por participante | 4 — equipos-admin |

### BR

- **Cumplidas (35):** BR-G01..G09 · BR-R01..R04, R06 · BR-E01..E05, E07..E09 · BR-T01..T06 · BR-B01..B07.
  Respaldo: auditorías de conformidad CONFORME + suites verdes por servicio.
- **Faltan por SP-4 (7):** BR-T07 (acumulación — el evento ya porta el puntaje), BR-T08 (ranking Trivia),
  BR-B08 (acumulación — `EtapaBDTGanada` ya porta `Puntaje`), BR-B09 (ranking BDT), BR-C01, BR-C02, BR-C03.
- **Faltan por equipos-admin (3):** BR-E10 (equipo desactivado + guard de borrado con inscripción activa),
  BR-E11 (historial de nombres), BR-E06 parcial (borrado explícito de equipo con limpieza de invitaciones —
  hoy el único camino de borrado es líder-solo-que-sale).
- **Parcial de transporte (1):** BR-R05 — contraseña temporal + re-emisión ✓ (SP-1), pero el correo sale por
  SMTP directo, no "asíncrono mediante RabbitMQ" (diferido explícito de SP-5b).

### RNF

- **Cumplidos (12):** RNF-01 (stack), RNF-02 (PostgreSQL+EF), RNF-04 (MediatR/CQRS), RNF-05 (RabbitMQ backbone
  SP-3i + identity SP-5b), RNF-06 (clean architecture), RNF-07 (dominio sin infra), RNF-08 (logging/excepciones/
  validaciones), RNF-13 (Keycloak, incl. audience mappers `48799d2`), RNF-14 (sin contraseñas locales),
  RNF-16 (decodificación QR backend), RNF-20 (mobile solo contratos), RNF-22 (autorización por rol en gateway, SP-5a).
- **Parciales (7):** RNF-03/RNF-17 (SignalR backend completo; falta ranking en vivo → SP-4 y conexión de clientes),
  RNF-10 (compose levanta solo infra, servicios por `dotnet run`), RNF-12 (redesign Fase 1 en curso),
  RNF-15 (relay 2s server-side listo; emisor móvil sin cablear), RNF-18/RNF-19 (permisos cámara/geoloc en app
  vieja; flujo nuevo sin cablear).
- **Incumplidos (5):**
  - RNF-09 — cobertura ≥90% **jamás medida** (~900 tests verdes, cero instrumento de cobertura).
  - RNF-11 — **no existe pipeline CI** (`.github/workflows/` ausente).
  - RNF-21 — **ningún cliente pasa por el gateway** (evidencia: `frontend/.env` → identity :5000 + trivia :5015 +
    bdt :5016 directos; `mobile/.env` ídem; cero URL de gateway en `frontend/src/` y `mobile/src/`).
  - RNF-23 — correo asíncrono vía RabbitMQ (hoy SMTP directo desde Identity).
  - RNF-24 — refresh de token 270s + registro de actividad: web no lo tiene (`frontend/src/auth/keycloak.ts`
    sin scheduler); mobile solo refresh on-demand.

## Bloques de trabajo faltantes (63 requisitos)

### Bloque 1 — SP-4 Puntuaciones (14 requisitos; ya en el roadmap)

Cierra: HU-25 (completa), HU-26, HU-27, HU-43, HU-44, HU-49, HU-50 · BR-T07, BR-T08, BR-B08, BR-B09,
BR-C01..C03 · completa RNF-17. `services/puntuaciones` hoy es esqueleto (HealthController + consumidor de humo
RabbitMQ de SP-3i). Los eventos de dominio ya fluyen por el broker: SP-4 arranca consumiendo, no instrumentando.

### Bloque 2 — Re-cableado de clientes (37 requisitos desbloqueados; ya en el roadmap, el mayor por volumen)

Web + mobile → **gateway** (base URL única) + Partidas/Operaciones nuevos + hub SignalR + UI de Puntuaciones
(post-Bloque 1). Cierra RNF-21 y completa RNF-03/15/17/18/19. Incluye UI web de partida multi-juego
(HU-13/28/45 — la UI vieja no puede expresar una partida con varios juegos) y el flujo participante completo
en mobile (panel único, convocatorias, gameplay nuevo, pistas, geolocalización).
Evidencia del estado actual: `frontend/src/api/{triviaApi,bdtApi}.ts` y `mobile/src/config/env.ts` consumen
los servicios viejos.

### Bloque 3 — Retiro de trivia-game-service / bdt-game-service (limpieza doctrinal; ya en el roadmap)

Depende de Bloques 1+2 (36 archivos con lógica de scoring/ranking solo viven ahí y los clientes aún los usan).
`team-service` ya no existe en `services/`; queda la var muerta `EXPO_PUBLIC_TEAM_API_BASE_URL` en `mobile/.env`
(sin consumidor en `mobile/src/` — borrar en Bloque 2).

### Bloque 4 — Slice equipos-admin / ciclo de vida (8 requisitos; **hallazgo — sin dueño previo**)

HU-06, HU-09, HU-19, HU-48 · BR-E06 (parcial), BR-E10, BR-E11. Todo en Identity salvo HU-19 (Operaciones) y el
guard de BR-E10/HU-06, que necesita consulta cross-service Identity→Operaciones ("¿equipo inscrito en partida
Lobby/Iniciada?"). `TeamsController` hoy solo expone create / mine / membership / leadership. Paralelizable con
Bloques 1-2.

### Bloque 5 — RNF sueltos de auth/notificaciones (2 requisitos; ya conocidos)

- RNF-23 + BR-R05: publicar `UsuarioCreado`/`CredencialTemporalEmitida` y mover el envío de correo a un consumidor
  RabbitMQ (diferido explícito de SP-5b; el backbone de Identity ya existe).
- RNF-24: scheduler de refresh 270s + registro de actividad + modal de continuidad, en web y mobile.

### Bloque 6 — Infraestructura académica (2-3 requisitos; **hallazgo — sin dueño previo**)

- RNF-11: pipeline CI de compilación + tests (no existe).
- RNF-09: instrumentar y medir cobertura ≥90% (coverlet + umbral en CI).
- RNF-10 (menor/interpretable): compose para los 4 servicios + gateway, no solo infra.

## Ruta crítica y paralelismo

```
SP-4 (Bloque 1) ──► Re-cableado clientes (Bloque 2) ──► Retiro servicios viejos (Bloque 3)
Equipos-admin (Bloque 4) ─── paralelo
RNF auth/correo (Bloque 5) ── paralelo
CI + cobertura (Bloque 6) ─── paralelo (conviene temprano: protege todo lo demás)
```

## Caveats de método

- Las BR se auditaron contra las auditorías de conformidad CONFORME de cada slice + spot-checks de código,
  no releyendo las 46 una por una contra cada handler: margen de error ±2 en ese eje.
- HU-24 ("mostrar la respuesta correcta al cierre"): verificar en el cableado que el payload/estado de cierre
  expone la respuesta correcta al cliente (los payloads SignalR son deliberadamente delgados).
- "Cumplido pleno" no exige gateway (RNF-21 se cuenta una sola vez como RNF, no contra cada HU); con el criterio
  más estricto, las 9 HU end-to-end también quedarían pendientes de Bloque 2.
