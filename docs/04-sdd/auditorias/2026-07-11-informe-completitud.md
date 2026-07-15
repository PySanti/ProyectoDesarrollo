# Informe de completitud — estado vs. documentación (2026-07-11)

Rama: `develop` (HEAD `6d36247`). Plan: `2026-07-11-auditoria-completitud.md`.
Método ejecutado según plan: 5 clústeres paralelos read-only (A1 Identity, A2 config+runtime, A3 Puntuaciones,
A4 RNF, A5 cross-check) + corrida de cobertura coverlet; los 3 hallazgos mayores fueron re-verificados
directamente por el auditor líder antes de publicar.

## Resultado global

| Eje | Pleno/Cumplido | Parcial | Backend listo, cliente sin cablear | Falta/Incumplido | Total |
|---|---|---|---|---|---|
| HU | 36 | 9 | 3 | 2 | 50 |
| BR | 44 | 2 | — | 0 | 46 |
| RNF | 19 | 4 | — | 1 | 24 |
| **Total** | **99 (82.5%)** | **15** | **3** | **3** | **120** |

- **82.5% cumplido pleno** con criterio **endurecido** (pleno exige tráfico vía gateway) — vs 47% del 2026-07-06
  (que ni siquiera exigía gateway). Contando backend listo: **85%**.
- Los bloques 1-6 + 4A/4B cerraron 43 requisitos netos desde julio-06. **Quedan 21 requisitos no plenos.**
- Cobertura de línea backend (RNF-09): **48.7% total** (identity 71.6 · partidas 50.4 · puntuaciones 52.5 ·
  operaciones-sesion 37 · gateway 30) — meta 90%; casi plana vs 48.1% de la línea base (los bloques nuevos
  trajeron tests y código en proporción similar).

## Cumplido pleno (99)

- **HU (36):** HU-01..08, HU-46..48 (Identity/equipos, incl. 4A) · HU-10, HU-11, HU-13..17, HU-20..23,
  HU-28..34, HU-36, HU-39, HU-42, HU-45 (config + runtime ambos clientes vía gateway) · HU-25, HU-44, HU-50
  (acumulación y rankings Trivia/BDT/consolidado end-to-end).
- **BR (44):** BR-G01..G09 · BR-R01..R04, R06 · BR-E01..E11 (4A cerró E06/E10/E11) · BR-T01..T05, T07, T08 ·
  BR-B01..B09 · BR-C01..C03. Doctrina de ranking verificada en código: Trivia y BDT por puntos con desempate
  por tiempo (`RankingCalculator.cs`), consolidado juegos-ganados→puntos→tiempo con empate-exacto-sin-ganador
  (`CalculadorRankingConsolidado.cs:20-49`); `UnidadesGanadas` solo informativo.
- **RNF (19):** RNF-01..08, RNF-10 (compose completo, Bloque 6), RNF-11 (CI 3 jobs), RNF-13..20, RNF-24
  (refresh 270s + modal en web y mobile).

## Lo que falta para el 100% — 21 requisitos en 8 paquetes

### R1 — Catch-up UI de HU-19 (aprobación de inscripciones) · 1 requisito · diferido explícito de 4B

**HU-19 = Backend listo, cliente sin cablear.** Backend completo y correcto (`SesionesController.cs:74-81`
aceptar/rechazar, `InscripcionPartida` nace `Pendiente`, `LobbyDto.SolicitudesPendientesIndividual/Equipo`,
cupo cuenta solo Activas). Pero: el tipo TS `LobbyDto` de `frontend/src/api/operacionesApi.ts:11-21` **ni
declara** los campos de solicitudes y la consola del operador no renderiza nada ni llama aceptar/rechazar;
mobile (`partidaLobbyFlow.js:26-27`) colapsa `Inscripcion.Estado` en un booleano y nunca muestra
Pendiente vs Activa. Tamaño: pequeño-mediano (panel web + estado mobile).

### R2 — Regresión gateway de equipos-admin (Bloque 4A) · 3 requisitos · **el más urgente**

`frontend/src/api/adminTeamsApi.ts:41` lee `VITE_IDENTITY_API_BASE_URL` y llama **directo** a
identity-service, bypaseando el gateway — patrón que Bloque 2 había retirado del resto del frontend.
Consecuencias: (a) `frontend/.env.example` ya no define esa variable → `.env` regenerado rompe
TeamsAdminPage ("Missing VITE_IDENTITY_API_BASE_URL"); (b) identity-service no tiene `UseCors` (CORS vive
solo en el gateway) → la llamada directa falla en navegador real; hoy solo funciona por remanente legacy del
`.env` local. Además la ruta `/identity/admin/teams` no tiene fila propia en la matriz del gateway
(`gateway/src/Umbral.Gateway/appsettings.json`): cae en el catch-all `identity` con policy `Default`
(cualquier autenticado) en vez de `Administrador` — la protección real queda solo en el servicio
(`AdminTeamsController` AdminOnly, defensa en profundidad correcta pero rompe la RBAC gruesa por ruta).
Cierra: **RNF-21** (Parcial→Cumplido), **RNF-22** (Parcial→Cumplido), **HU-09** (Parcial→Pleno).
Tamaño: pequeño (reescribir 1 módulo API frontend + 1 ruta gateway + tests).

### R3 — Cancelación manual de partida (HU-40) · 4 requisitos · hallazgo nuevo

**No existe en ningún nivel** — ni dominio, ni comando, ni endpoint, ni contrato, ni UI (verificado:
único "Cancelar" en `SesionesController` es cancelar-inscripción; cero `CancelarPartidaCommand`). Solo existe
la auto-cancelación por mínimos (`SesionPartida.cs:406-408`). El baseline de julio-06 la contaba mal como
"backend listo". Arrastra: **HU-37** (sin botón cancelar en panel BDT), **HU-41** (notificación
`PartidaCancelada` cableada pero solo alcanzable por la causa automática), **HU-26** (su criterio de
aceptación combina ranking vivo ✓ + cancelar ✗). Cierra: HU-40, HU-37, HU-41, HU-26.
Tamaño: mediano (dominio→UI, evento ya existe).

### R4 — Vistas de cierre/monitoreo del runtime · 6 requisitos

- **HU-24 + BR-T06**: la respuesta correcta al cerrar una pregunta **no llega al participante por ninguna
  vía** (payload `PreguntaCerrada` delgado; `ObtenerPreguntaActual` lanza tras el cierre). El operador la ve
  solo cruzando con su propia config. Más grave que el caveat del baseline.
- **HU-35**: sigue sin identidad del ganador de etapa (ni push ni GET); el operador no puede saber quién ganó
  una etapa ni que nadie la ganó.
- **HU-38 (Falta)**: cero query/panel de monitoreo de envíos `TesoroQR` (el dato sí se persiste en
  `EtapaSnapshot`).
- **HU-18**: el lobby del operador en Individual muestra solo un contador, nunca la lista de inscritos
  (el DTO ya trae los IDs, hoy solo alimentan el selector de pistas).
- **HU-12**: mobile no advierte al entrar a una partida por equipos sin ser líder; solo oculta el botón con
  copy genérico.
Tamaño: mediano (payloads/queries pequeños + paneles).

### R5 — Clientes de Puntuaciones + historial · 3 requisitos

- **HU-27 (Backend listo)**: `GET /puntuaciones/participantes/{id}/historial-partidas` completo y testeado;
  **cero pantalla mobile**. Limitación documentada en el handler: la pertenencia a equipo se infiere por
  autoría de eventos de juego, no por convocatoria.
- **HU-49 (Backend listo — cliente equivocado)**: el actor documentado es Participante→mobile; la única UI
  está en web gateada Operador/Admin (`RendimientoEquipoPage`). Mobile: cero consumidor.
- **HU-43 (Parcial)**: `HistorialEventMapper.cs:14-33` **descarta silenciosamente** los 5 eventos de
  inscripciones que `operaciones-sesion-events.md:347-368` afirma que se archivan (deuda de 4B); y las
  invitaciones de equipo viven en el exchange de Identity, que Puntuaciones no consume (falta decisión:
  tercer consumidor o recorte documentado del alcance del historial).
Tamaño: mediano.

### R6 — Correo asíncrono (RNF-23 + BR-R05) · 2 requisitos · Bloque 5 pendiente

`CreateUserWithInitialRoleCommandHandler.cs:70-83` awaitea el SMTP **inline en el request**
(`SmtpUserWelcomeEmailSender`, SMTP directo); cero eventos `UsuarioCreado`/`CredencialTemporalEmitida` en el
código. El backbone RabbitMQ de Identity ya existe (publisher de eventos de equipo + consumer de
inscripciones) — falta solo publicar el evento y mover el envío a un consumer. Tamaño: pequeño-mediano.

### R7 — Cobertura ≥90% (RNF-09) · 1 requisito · decisión académica

48.7% real vs 90% exigido. Instrumento completo (CI con coverlet + reportgenerator, report-only sin gate,
decisión explícita en `ci.yml:99`). Subir 41 puntos es el paquete más caro del proyecto; candidatos de mayor
retorno: operaciones-sesion (37%, el servicio más grande) y gateway (30%). Requiere decisión de si la meta
académica se persigue literal o se documenta el gap.

### R8 — Pase visual Expo (RNF-12) · 1 requisito

Redesign: Fase 0 y Fase 1 (web) completas; Fase 2 mobile con código completo pero **pase visual en
Expo/dispositivo pendiente** (lo marca el propio `frontend-redesign-plan.md:79`). Tamaño: pequeño.

## Hallazgos documentales menores (sin costo de requisito, higiene)

1. `docs/04-sdd/SPECS-LIST.md` desincronizado: cabecera contradice su tabla; falta todo lo posterior a 4A.
   La matriz de trazabilidad (fuente real) sí está completa y alineada — verificada fila por fila.
2. `contracts/http/gateway-api.md:33`: nota obsoleta "SP-4 aún no expone endpoints HTTP" — Puntuaciones tiene
   6 endpoints + RankingHub.
3. Regla graded de estructura: Identity tiene carpeta extra `Application/Services/`
   (`ParticipacionProjectionUpdater`) fuera del set permitido — mover a `Handlers/` o documentar excepción.
4. `appsettings.json` de Identity con fallback `Database=umbral` y credenciales dev hardcodeadas (los otros 3
   servicios usan placeholder vacío).
5. Naming confuso: `AdminTeamsController` (CRUD admin) vs `TeamsAdminController` (listado read-only 3b).
6. BR-B06 "quedan registradas": pistas solo persisten como evento hacia Puntuaciones, sin entidad local en
   Operaciones (matiz aceptable, documentar).
7. `.env` locales (gitignored) desincronizados con `.env.example` post-Bloque 2; `frontend/run-local.sh` no
   regenera `.env` (mobile sí).

## Contratos y doctrina (A5)

- Contratos HTTP/eventos **alineados campo a campo** con el código en los 4 servicios + gateway (22 routing
  keys de Operaciones y 10 de Identity: match exacto). Única discrepancia: la nota obsoleta del punto 2.
- Doctrina CLAUDE.md: 4 servicios exactos sin residuo legacy, controllers ControllerBase+MediatR todos con
  test, Program.cs sin endpoints inline, límites de DB respetados, excepciones centralizadas en los 4,
  ruteo de clientes limpio (cero gameplay en web, cero admin en mobile). Única desviación: punto 3 de arriba.

## Comparación con la línea base (2026-07-06)

| Métrica | 2026-07-06 | Hoy | Δ |
|---|---|---|---|
| Pleno estricto | 56 (47%) | 99 (82.5%) | +43 (y el criterio de hoy es más duro: exige gateway) |
| Pleno + backend listo | 93 (78%) | 102 (85%) | +9 |
| Falta backend | 27 | 3 | −24 |
| Cobertura backend | 48.1% (sin CI) | 48.7% (CI + compose) | +0.6 pts |

## Caveats de método

- Suites no re-ejecutadas: árbol `6d36247` byte-idéntico a `c351398`, verificado verde el mismo día
  (Identity 229/47/41 · Operaciones 389/31/70 · Partidas 94/5/13 · Puntuaciones 140/28/20 · Gateway 24 ·
  frontend 211 + tsc + build · mobile 106 + typecheck).
- BR-G02..G07 heredan el veredicto de las auditorías de conformidad CONFORME previas (no re-leídas
  handler por handler): margen ±1 en ese sub-eje.
- HU-09/HU-06 comparten el módulo `adminTeamsApi.ts` afectado por R2; se adjudicó el costo a HU-09 (el camino
  mobile de HU-06 sí es pleno vía gateway).
- La remediación de estos hallazgos es slice aparte (este informe no modifica código).
