# Bloque 7 — Cierre al 100% (design maestro)

- **Rama:** `feature/bloque-7` (desde develop `4b49036`).
- **Fuente:** `docs/04-sdd/auditorias/2026-07-11-informe-completitud.md` (99/120 pleno; 21 requisitos restantes
  en paquetes R1-R8 + 7 menores documentales). Toda la evidencia archivo:línea vive allí; este design no la repite.
- **Meta:** 120/120 requisitos plenos según el criterio endurecido de la auditoría (end-to-end, cliente
  documentado, vía gateway) + higiene documental cerrada.
- **Estructura:** 8 sub-slices secuenciales 7a→7h; cada uno con su ciclo spec→plan→implementación→review
  (subagent-driven, patrón de bloques anteriores). Cobertura (7h) al final, sobre código ya estable.

## Decisiones de alcance (fijadas con el usuario, 2026-07-11)

1. **RNF-09 se persigue literal: 90%** de cobertura de línea backend (hoy 48.7%). Gate de CI se activa al final de 7h.
2. **HU-43 con recorte documentado:** el historial de partida NO archiva invitaciones de equipo (ciclo de vida
   de equipo ≠ evento de partida); el contrato se corrige para no prometerlas. Sí se mapean los 5 eventos de
   inscripciones (`InscripcionSolicitada/Aceptada/Rechazada`, `InscripcionEquipoCreada/Cancelada`) que
   `HistorialEventMapper` hoy descarta.
3. **Menores documentales incluidos** (slice 7g).
4. **HU-49 dual:** se añade la pantalla mobile (actor documentado = Participante); la página web existente
   (`RendimientoEquipoPage`) se conserva como herramienta de consulta del operador.
5. **Anti-leak vs HU-24/HU-35:** revelar respuesta correcta / ganador de etapa **después del cierre** no viola
   la doctrina anti-leak (el dato ya no da ventaja). Se extienden los payloads de cierre o se expone GET
   post-cierre; los payloads pre-cierre no se tocan.

## Sub-slices

### 7a — Regresión gateway equipos-admin (R2) · cierra RNF-21, RNF-22, HU-09 · pequeño, primero por urgente

- `frontend/src/api/adminTeamsApi.ts`: reescribir sobre `VITE_GATEWAY_BASE_URL` + prefijo `/identity/admin/teams`
  (mismo patrón que `identityApi.ts`); eliminar todo uso de `VITE_IDENTITY_API_BASE_URL`.
- `gateway/src/Umbral.Gateway/appsettings.json`: ruta explícita `identity-admin-teams`
  (`/identity/admin/teams/{**catch-all}`) con policy `Administrador`, Order antes del catch-all `identity`;
  fila nueva en `contracts/http/gateway-api.md`.
- Tests: unit del módulo API frontend (URL base correcta), test de matriz de rutas gateway.
- Aceptación: TeamsAdminPage funciona con `.env` regenerado desde `.env.example`; llamada pasa por :5080.

### 7b — UI de aprobación de inscripciones HU-19 (R1) · cierra HU-19 · pequeño-mediano

- Web (consola operador, `SesionOperadorPage` vista Lobby): tipo TS `LobbyDto` gana
  `solicitudesPendientesIndividual[]`/`solicitudesPendientesEquipo[]`; panel de solicitudes con botones
  aceptar/rechazar → `POST .../inscripciones/{id}/aceptacion|rechazo`; refresco por señal SignalR existente
  (patrón GET-en-señal ya establecido).
- Mobile: `partidaLobbyFlow` deja de colapsar `Inscripcion.Estado` en booleano; lobby participante muestra
  "Solicitud pendiente de aprobación" vs "Inscrito"; push/GET existente refresca al aceptar/rechazar.
- Gating: botones solo con `puedeOperar` (admin observador queda read-only, patrón remediación Bloques 2+3).
- Aceptación: flujo completo inscribir→pendiente→aceptar/rechazar→estado reflejado en ambos clientes.

### 7c — Cancelación manual de partida HU-40 (R3) · cierra HU-40, HU-37, HU-41, HU-26 · mediano

- Dominio (`SesionPartida`): `CancelarPartida(motivo)` válido en `Lobby` e `Iniciada` (guards: terminal → excepción);
  reusa el evento `PartidaCancelada` existente (motivo `"CanceladaPorOperador"`).
- Aplicación/API: `CancelarPartidaCommand` + handler + `POST /operaciones-sesion/partidas/{id}/cancelacion`
  (policy GestionarPartidas); fila en `contracts/http/operaciones-sesion-api.md`.
- Web: botón "Cancelar partida" (con confirmación) en la consola del operador — visible en Lobby y en runtime
  (cubre el criterio de HU-26 y HU-37), gateado `puedeOperar`.
- HU-41 queda plena por transitividad: la notificación `PartidaCancelada` ya está cableada extremo a extremo
  en ambos clientes; ahora es alcanzable por decisión del operador.
- Puntuaciones ya proyecta `PartidaCancelada` (verificar solo que el motivo nuevo no rompa el mapper).
- Aceptación: operador cancela desde web en Lobby y en Iniciada; participantes ven la cancelación en vivo.

### 7d — Cierre informativo del runtime (R4) · cierra HU-24+BR-T06, HU-35, HU-38, HU-18, HU-12 · mediano

- **HU-24/BR-T06:** `PreguntaCerradaPayload` gana `respuestaCorrecta` (texto u opciónId); mobile la muestra
  al cierre ("La respuesta correcta era…"). Sin cambio pre-cierre.
- **HU-35:** `EtapaCerradaPayload`/`EtapaGanadaPayload` ganan identidad del ganador (participante/equipo, o
  null = nadie); panel BDT operador muestra resultado por etapa ("Ganada por X" / "Nadie consiguió el tesoro").
- **HU-38:** `GET /operaciones-sesion/partidas/{id}/juego-actual/envios-tesoro` (operador): lista de intentos
  `TesoroQR` por participante/equipo con resultado; panel de monitoreo en la consola BDT web.
- **HU-18:** vista Lobby del operador lista los inscritos individuales (el `LobbyDto.Participantes` ya trae IDs;
  evaluar en el spec si basta ID o se resuelve nombre vía Identity — decisión del spec 7d).
- **HU-12:** al entrar al lobby de una partida por equipos sin ser líder, mobile muestra aviso explícito
  ("Solo el líder del equipo puede preinscribir al equipo").
- Aceptación por HU; contratos actualizados (payloads en `operaciones-sesion-api.md`).

### 7e — Clientes de Puntuaciones + historial (R5) · cierra HU-27, HU-49, HU-43 · mediano

- **HU-27:** pantalla mobile "Mi historial de partidas" consumiendo
  `GET /puntuaciones/participantes/{id}/historial-partidas` (id = sub del JWT); entrada desde Home.
- **HU-49:** pantalla mobile "Rendimiento de mi equipo" (equipo activo vía `GET /identity/teams/mine` →
  `GET /puntuaciones/equipos/{id}/rendimiento`); la página web queda igual.
- **HU-43:** `HistorialEventMapper` mapea los 5 eventos de inscripciones; contrato
  `operaciones-sesion-events.md` §archivado corregido: invitaciones de equipo EXCLUIDAS explícitamente
  (recorte documentado, decisión 2 de arriba).
- Limitación conocida de HU-27 (pertenencia por autoría de eventos) se documenta como matiz, no se re-arquitecta.
- Aceptación: participante ve su historial y el rendimiento de su equipo en mobile; historial web muestra
  filas de inscripciones.

### 7f — Correo asíncrono (R6) · cierra RNF-23, BR-R05 · pequeño-mediano

- Identity publica `UsuarioCreado` + `CredencialTemporalEmitida` al exchange `umbral.identity` (routing keys
  nuevas en `IdentityEventRouting` + contrato `identity-events.md`); el command handler deja de awaitear SMTP.
- Consumer propio en Identity (`BackgroundService`, mismo patrón `OperacionesInscripcionesConsumer`) consume
  `CredencialTemporalEmitida` y envía el SMTP (reusa `SmtpUserWelcomeEmailSender`). Best-effort ADR-0012:
  fallo de correo se loguea, no tumba nada; la compensación síncrona actual (rollback usuario si el correo
  falla) desaparece — decisión: el usuario queda creado aunque el correo falle (se anota en el spec 7f).
- Cambio de email con credencial temporal re-emite → mismo camino por evento.
- Aceptación: crear usuario responde sin esperar SMTP; correo llega vía consumer (round-trip opt-in con broker).

### 7g — Pase Expo + higiene documental (R8 + menores) · cierra RNF-12 + 7 menores · pequeño

- Pase visual del redesign mobile en Expo/dispositivo; fixes visuales que salgan; marcar Fase 2 completa en
  `frontend-redesign-plan.md`.
- Menores: (1) `SPECS-LIST.md` re-sincronizado o marcado como histórico apuntando a la matriz; (2) nota
  obsoleta `gateway-api.md:33` corregida; (3) `Application/Services/` de Identity: mover
  `ParticipacionProjectionUpdater` a ubicación conforme a la regla graded (o `Handlers/`); (4) appsettings
  Identity fallback → placeholders vacíos como los otros 3; (5) naming AdminTeams/TeamsAdmin: comentario
  aclaratorio cross-ref (sin rename de rutas); (6) BR-B06 matiz documentado en contrato; (7)
  `frontend/run-local.sh` regenera `.env` desde `.env.example` como hace mobile.

### 7h — Cobertura 90% (R7) · cierra RNF-09 · grande, el último

- Estrategia dirigida por dato: reporte por clase (reportgenerator HTML/JSON) por servicio; atacar en orden
  operaciones-sesion (37%) → gateway (30%) → partidas (50.4%) → puntuaciones (52.5%) → identity (71.6%)
  hasta que el TOTAL cruce 90%.
- Tests unit/application/contract sobre huecos reales (handlers sin test, ramas de excepción, workers,
  middleware), no relleno ciego; exclusiones legítimas (Migrations, Program bootstrap) vía `[ExcludeFromCodeCoverage]`
  o filtro de coverlet, documentadas.
- Al cruzar 90%: activar gate en `ci.yml` (umbral que falla el build) y actualizar la leyenda "report-only".
- Aceptación: corrida local + CI ≥90% total backend con gate activo.

## Cierre del bloque

- Re-medición de cobertura + verificación puntual de los 21 requisitos (mini-auditoría de confirmación).
- Actualizar `traceability-matrix.md` (fila por sub-slice) e informe de completitud con un anexo "estado
  post-Bloque 7".
- Integración a develop per `GUIA-USO-AGENTE.md` (squash a 1 commit + ff-only) — a orden del usuario.

## Riesgos

- **7h ≈ la mitad del esfuerzo del bloque.** Mitigación: va último; si se corta, 7a-7g ya dejan 20/21.
- 7f cambia semántica de creación de usuario (sin rollback por correo fallido) — se documenta y aprueba en su spec.
- 7d toca payloads SignalR compartidos: cambios solo aditivos (campos nuevos opcionales) para no romper clientes.
