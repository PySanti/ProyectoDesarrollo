# SP-3f-4 — Pistas BDT (operador → participante, evento vía seam)

- **Fecha:** 2026-07-01
- **Slice:** SP-3f-4 (cuelga del seam SignalR de SP-3f-2; hub alcanzable vía gateway tras SP-3g; patrón de grupos por-rol de SP-3f-3)
- **Servicio dueño:** Operaciones de Sesión
- **Cliente objetivo:** backend
- **Estado:** Diseño aprobado — pendiente de plan de implementación
- **Rama:** `feature/code-migration-SP-3`

## Contexto y regla de dominio

`CLAUDE.md` / dominio: `JuegoBDT` posee la entidad hija `Pista`; método de dominio
`JuegoBDT.EnviarPista`; evento `PistaEnviada`; `TipoEventoHistorial.PistaEnviada`
(audit). **BR-B06:** el operador puede enviar pistas (`Pista`) a
participantes/equipos **específicos** durante un `JuegoBDT` **activo**; las pistas
**se registran**.

SP-3f-2 dejó el push runtime sobre `SesionHub` colgado del puerto
`ISesionEventsPublisher` (composite No-Op + SignalR, difusión al grupo
`partida:{id}`). SP-3f-3 añadió el patrón de **grupo por-rol** (`operador:partida:{id}`,
auto-join en `SuscribirAPartida`) para la geolocalización. SP-3f-4 reutiliza ambos:
una **acción operador** que emite `PistaEnviada` por el seam y se entrega **solo al
participante destino** por un grupo por-participante.

Estado actual relevante:
- **Acciones operador** (`AvanzarPregunta`/`AvanzarEtapa`/`FinalizarJuegoActual`):
  `POST` → `MediatR` command → handler carga `SesionPartida`
  (`GetByPartidaIdAsync`), muta, `SaveChangesAsync`, publica evento(s) vía
  `ISesionEventsPublisher` (save-then-publish). Los endpoints operador **no** toman
  `participanteId` del claim.
- `ISesionEventsPublisher` (`Application/Interfaces/ISesionEventsPublisher.cs`): 13
  métodos. Impls: `SignalRSesionEventsPublisher` (Api/Realtime; helper
  `Difundir(partidaId, mensaje, payload, ct)` que difunde a `GrupoPartida`),
  `NoOpSesionEventsPublisher` y `CompositeSesionEventsPublisher`
  (Infrastructure/Services). Eventos en `Application/Interfaces/BdtRuntimeEvents.cs`.
- `SesionHub.SuscribirAPartida`: rama operador (join `partida:{id}` +
  `operador:partida:{id}`, return) y rama participante (valida inscripción, setea
  `Context.Items`, join `partida:{id}`).
- Excepciones de dominio ya existentes reutilizables: `ParticipanteNoInscritoException`,
  `NoHayEtapaActivaException`, `JuegoActivoNoEsBDTException`.
- `TimeProvider` registrado en DI (`Application/DependencyInjection.cs`).

## Enfoque elegido (3 decisiones cerradas en brainstorming)

1. **Disparador:** HTTP command (`POST /operaciones-sesion/partidas/{id}/pistas`
   → `EnviarPistaCommand` → handler), igual patrón que las demás acciones operador.
   (Descartado: método hub servidor — más acoplado, sin MediatR/validación.)
2. **Registro (BR-B06 "se registran"):** **solo evento** vía seam. El handler emite
   `PistaEnviadaEvent`; el push SignalR entrega en vivo y el **audit** materializa el
   registro **de forma diferida** (broker RabbitMQ, otra slice). Sin persistir la
   entidad `Pista`, sin migración — consistente con cómo fluye el resto del runtime
   (tesoro/etapa son evented) y con "Operaciones almacena solo estado transitorio".
   (Descartado: persistir `Pista` + migración + GET historial — diferido.)
3. **Targeting/entrega:** grupo **por-participante** `participante:{id}` (auto-join en
   `SuscribirAPartida`, como el grupo operador de SP-3f-3). La pista se entrega a
   `participante:{destinoId}`. **Individual**; Equipo → slice-E. (Descartado:
   `Clients.User()` + `IUserIdProvider` — requiere provider nuevo, se aparta del
   patrón de grupos.)

## Flujo

`SesionesController.EnviarPista(partidaId, EnviarPistaRequest{participanteDestinoId, texto})`
(operador; no toma participanteId del claim)
→ `EnviarPistaCommand(partidaId, participanteDestinoId, texto)`
→ `EnviarPistaCommandHandler`:
- carga `SesionPartida` (`GetByPartidaIdAsync`; `SesionNoEncontradaException` si falta),
- `var juegoId = sesion.PrepararPista(participanteDestinoId, now)` — método
  **read-only** de dominio que enforced BR-B06 y retorna el `JuegoId` del BDT activo,
- **sin `SaveChangesAsync`** (no muta estado — es un relay de acción operador),
- `await _events.PublicarPistaEnviadaAsync(new PistaEnviadaEvent(partidaId,
  sesion.Id.Valor, juegoId, participanteDestinoId, texto, now), ct)`.

El composite fan-out a No-Op (registro futuro/broker) + SignalR (push).
`SignalRSesionEventsPublisher.PublicarPistaEnviadaAsync` difunde el mensaje
`PistaEnviada` con `PistaEnviadaPayload` **al grupo `participante:{destinoId}`**
(NO a `partida:{id}`).

## Regla de dominio: `SesionPartida.PrepararPista`

Método read-only (no muta, no persiste), firma tentativa
`Guid PrepararPista(Guid participanteDestinoId, DateTime now)`:
- destino debe estar **inscrito activo** → si no, `ParticipanteNoInscritoException`;
- el **juego activo** debe ser BDT → si no, `JuegoActivoNoEsBDTException`;
- ese BDT debe tener **etapa activa** → si no, `NoHayEtapaActivaException`;
- retorna el `JuegoId` del BDT activo (para el evento/payload).

Mantiene BR-B06 en el agregado (testeable en dominio), deja el handler delgado y no
introduce estado persistido.

## Contrato del hub / entrega

- `SuscribirAPartida` (rama participante): además de `partida:{id}` y de setear
  `Context.Items`, auto-une a `GrupoParticipante(participanteId)` = `participante:{id}`.
  `DesuscribirDePartida`: quita también de `participante:{id}`. La rama operador **no**
  se une al grupo participante.
- **Servidor→cliente** `PistaEnviada`
  `{ partidaId, juegoId, participanteDestinoId, texto, timestampUtc }` — entregado
  **solo** a `participante:{destinoId}`. Solo el destinatario recibe (BR-B06
  "específico"); el resto de participantes y el operador no.
- **Texto en el payload:** al ser event-only no hay store que consultar por pull; el
  `texto` de la pista viaja en el payload. No aplica el anti-leak de SP-3f-2 (el texto
  es justo el contenido destinado al participante, entregado solo a él).
- **Sin replay:** si el destino está offline, la pista se pierde (transitorio, igual
  que estado/geoloc; sin persistencia en este slice).

## Piezas nuevas (por capa)

- **Domain:** `SesionPartida.PrepararPista(...)` (reusa las 3 excepciones existentes).
- **Application:** `EnviarPistaCommand`, `EnviarPistaRequest` (DTO),
  `EnviarPistaCommandValidator`, `EnviarPistaCommandHandler`; `PistaEnviadaEvent`
  (en `BdtRuntimeEvents.cs`); `ISesionEventsPublisher.PublicarPistaEnviadaAsync`
  (método 14).
- **Infrastructure:** impl del método nuevo en `NoOpSesionEventsPublisher` (no-op) y
  `CompositeSesionEventsPublisher` (fan-out).
- **Api:** impl en `SignalRSesionEventsPublisher` (push a `participante:{destinoId}`;
  **no** reutiliza `Difundir`, que apunta a `GrupoPartida`); `SesionRealtimeMessages`
  (const `PistaEnviada` + helper `GrupoParticipante`); `SesionRealtimePayloads`
  (`PistaEnviadaPayload`); `SesionHub` (join/leave del grupo participante);
  `SesionesController` (endpoint `POST .../pistas`).

## Autorización (nota / deuda pre-existente)

Endpoint operador-triggered, mismo patrón que `AvanzarPregunta`/`AvanzarEtapa` (sin
gate de rol in-controller; el gateway hace auth coarse por rol de ruta). **Riesgo
conocido:** los endpoints operador comparten el prefijo `/operaciones-sesion` con los
de participante, así que un participante autenticado podría vía gateway invocar
acciones operador — **deuda pre-existente que afecta a todos los endpoints operador
por igual**, no la introduce este slice. Endurecer con un check operador in-service es
un follow-up documentado, fuera de alcance.

## Estrategia de pruebas

- **Domain (`SesionPartidaTests` o análogo):** `PrepararPista` retorna el `JuegoId`
  con BDT activo + destino inscrito; lanza `ParticipanteNoInscritoException` (destino
  no inscrito), `JuegoActivoNoEsBDTException` (juego activo Trivia),
  `NoHayEtapaActivaException` (BDT sin etapa activa).
- **Handler (`EnviarPistaCommandHandlerTests`):** publica `PistaEnviadaEvent` con
  campos correctos (fakes repo/eventos/time); propaga la excepción de dominio **sin
  publicar** (evento no emitido si `PrepararPista` lanza).
- **Hub (`SesionHubTests`):** participante `SuscribirAPartida` → une
  `participante:{id}`; `DesuscribirDePartida` lo quita; operador **no** lo une.
- **SignalR publisher (`SignalRSesionEventsPublisherTests`):**
  `PublicarPistaEnviadaAsync` → `SendAsync` a `participante:{destinoId}` con
  `PistaEnviadaPayload`.
- **Composite (`CompositeSesionEventsPublisherTests`):** el método nuevo hace fan-out
  a ambos inners (reusa el patrón de los tests existentes).
- **Validator:** `texto` vacío/whitespace → inválido; cota de largo; destino no vacío.
- **Controller:** el endpoint despacha `EnviarPistaCommand` (test unitario de
  controller, requerido por las reglas del repo).
- **Contract (`RealtimeContractTests`):** `PistaEnviada` documentado (InlineData
  doc↔constantes). El endpoint HTTP se documenta en el contrato.
- **Gap documentado:** entrega WS end-to-end no testeable en el harness (misma clase
  que los gaps de SP-3f-2/3/g); cubierto por unit tests.

## Actualización del fake de publisher

`FakeSesionEventsPublisher` (test double de `ISesionEventsPublisher`) debe implementar
el método nuevo `PublicarPistaEnviadaAsync` (capturar el evento para asertar en los
tests de handler). Sin él, el proyecto de test no compila.

## Contrato y documentación

- `contracts/http/operaciones-sesion-api.md`:
  - Sección HTTP: nuevo `POST /operaciones-sesion/partidas/{id}/pistas`
    (operador; body `{ participanteDestinoId, texto }`; efecto: push `PistaEnviada`).
  - Sección Realtime: fila servidor→cliente `PistaEnviada` **(participante-destino
    only)** con el shape del payload; nota de entrega por-participante + no-persistencia.
- Fila de traceability SP-3f-4 (carve-out: se **escribe, no se commitea**).
- Al cerrar el slice, actualizar/crear memoria del canal de pistas.

## Fuera de alcance / forward-looking

- **Persistir entidad `Pista`** + migración + **GET historial de pistas** — diferido
  (Q2 event-only); el registro lo materializa audit vía broker.
- **Materialización de audit** (`TipoEventoHistorial.PistaEnviada`) → broker RabbitMQ,
  diferido.
- **Targeting Equipo** (pista a un equipo completo) → slice-E.
- **Cableado de clientes** (UI operador para enviar / recepción móvil) — clients, no
  este slice backend.
- **Endurecer rol-operador in-service** en los endpoints operador — follow-up
  cross-cutting, fuera de alcance (deuda pre-existente).

## Riesgos

- **Método nuevo en `ISesionEventsPublisher` (14º):** obliga a implementar en las 3
  impls **y** en `FakeSesionEventsPublisher`; un olvido rompe compilación (ruidoso, no
  silencioso). Mecánico.
- **Texto arbitrario en payload:** el `texto` es input del operador; el validator
  acota largo y no-vacío. Entregado solo al destino; sin persistencia. Sin
  interpretación server-side (relay de texto).
- **Grupo participante nuevo por conexión:** el participante debe estar suscrito para
  recibir; sin replay. Aceptado (transitorio), documentado.

## Descomposición tentativa (se detalla en writing-plans)

1. Realtime decls (`PistaEnviada` const + `GrupoParticipante` helper +
   `PistaEnviadaPayload`) + `SesionHub` join/leave del grupo participante + hub tests.
2. `PistaEnviadaEvent` + `ISesionEventsPublisher.PublicarPistaEnviadaAsync` + impls
   No-Op/Composite/SignalR (a `participante:{destinoId}`) + `FakeSesionEventsPublisher`
   + tests SignalR/Composite.
3. Dominio `SesionPartida.PrepararPista` + domain tests.
4. `EnviarPistaCommand` + `EnviarPistaRequest` + `EnviarPistaCommandValidator` +
   `EnviarPistaCommandHandler` + tests handler/validator.
5. `SesionesController` endpoint `POST .../pistas` + controller test.
6. Contrato (HTTP + Realtime) + `RealtimeContractTests` InlineData + fila traceability
   (carve-out no-commit).
