# Bloque 7f — Correo asíncrono vía RabbitMQ — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El correo de credenciales temporales deja de bloquear el request de creación/cambio de email y pasa a un consumidor RabbitMQ. Cierra RNF-23 y completa BR-R05 (el único requisito "Incumplido" que sobrevive del viejo Bloque 5).

**Architecture:** Identity YA tiene todo el backbone: `IIdentityEventsPublisher` (Composite NoOp+RabbitMQ), `IdentityEventRouting`, `RabbitMqPublishChannel`, y el patrón de consumidor `OperacionesInscripcionesConsumer` (BackgroundService, ack-siempre best-effort ADR-0012). Se añade un evento `CredencialTemporalEmitida` (lleva la contraseña temporal) que los dos command handlers publican **best-effort tras el save**, en vez de awaitear el SMTP inline; un consumidor nuevo en Identity lo consume y envía el correo reusando `SmtpUserWelcomeEmailSender`. La compensación all-or-nothing por fallo de correo **desaparece**: el usuario queda creado aunque el correo falle (decisión del design maestro §7f — el correo es best-effort, no parte de la transacción).

**Decisión de seguridad (documentada):** la contraseña temporal viaja en el payload del evento por el exchange **interno** `umbral.identity`. Ese exchange no cruza el gateway ni sale del backend; la credencial es de un solo uso con cambio obligatorio en el primer login (Keycloak). Es la única forma compatible con "el consumidor reusa `SmtpUserWelcomeEmailSender`" (el handler ya generó la password para crear la credencial en Keycloak). NO se publica un `UsuarioCreado` genérico adicional (YAGNI: sin consumidor; se añadirá en un slice de auditoría si algún consumidor lo requiere).

**Tech Stack:** .NET 8 (Identity: Application/Infrastructure/Api + xUnit).

## Global Constraints

- Rama: `feature/bloque-7`. Commits con trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- Gate: `export DOTNET_ROOT=/snap/dotnet-sdk/current; dotnet test services/identity-service/Umbral.IdentityService.sln` (baseline verde; ~229u/47i/41c según referencias previas — confirmar el número real antes de tocar).
- Sin dependencias nuevas. Los eventos/rutas nuevos son **aditivos** (no rompen consumidores/publishers existentes).
- El evento sigue el estilo de los `*IntegrationEvent` existentes en `IIdentityEventsPublisher.cs` (record con `OccurredOnUtc`).
- Best-effort ADR-0012: publicar tras `SaveChanges`/`AddAsync` exitoso; fallo de publicación se loguea, no tumba el request. El consumidor ack-siempre, loguea el fallo de SMTP (sin poison-loop; el correo es reconstruible re-emitiendo).

---

### Task 1: Evento CredencialTemporalEmitida + publicación (deja de awaitear SMTP inline)

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IIdentityEventsPublisher.cs` (método `PublishCredencialTemporalEmitidaAsync` + record `CredencialTemporalEmitidaIntegrationEvent(string Nombre, string Correo, string Rol, string PasswordTemporal, DateTime OccurredOnUtc)`)
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/IdentityEventRouting.cs` (`["CredencialTemporalEmitida"] = "identity.credencial-temporal-emitida.v1"`)
- Modify: los 3 implementadores del publisher: `RabbitMqIdentityEventsPublisher.cs`, `NoOpIdentityEventsPublisher.cs`, `CompositeIdentityEventsPublisher.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/CreateUserWithInitialRoleCommandHandler.cs` (líneas ~70-83: quitar el `await _welcomeEmailSender...` inline + su `catch`/compensación de correo; en su lugar `await _identityEventsPublisher.PublishCredencialTemporalEmitidaAsync(...)` tras el `AddAsync` exitoso). El ctor deja de depender de `IUserWelcomeEmailSender`; gana `IIdentityEventsPublisher` (si no lo tiene ya).
- Modify: tests del handler + del publisher/routing (localizar `CreateUserWithInitialRoleCommandHandlerTests`, `RabbitMqIdentityEventsPublisherTests`, `IdentityEventRoutingTests`)
- Modify: `contracts/events/identity-events.md` (fila + sample del evento nuevo; nota de seguridad exchange interno)

**Interfaces (Task 2 consume):**
- Evento serializado camelCase: `{ nombre, correo, rol, passwordTemporal, occurredOnUtc }`, routing key `identity.credencial-temporal-emitida.v1` en el exchange `umbral.identity`.

- [ ] Step 1: tests RED — (a) el handler, tras crear usuario, publica `CredencialTemporalEmitida` con nombre/correo/rol/password correctos y **ya no** llama a `IUserWelcomeEmailSender`; (b) si la publicación del evento falla, el usuario NO se compensa (queda creado) — el request no lanza por fallo de correo; (c) routing key nueva mapeada; (d) publisher RabbitMQ serializa el evento a la routing key correcta.
- [ ] Step 2: implementación (record + método + 3 impls + routing + handler reescrito). Mantener la compensación de Keycloak↔local que SÍ es transaccional (si `AddAsync` local falla tras crear en Keycloak, sigue deshaciendo Keycloak — eso NO cambia); lo que se elimina es la compensación por fallo de **correo**.
- [ ] Step 3: GREEN + suite completa Identity verde.
- [ ] Step 4: contrato `identity-events.md`.
- [ ] Step 5: commit `feat(identity): evento CredencialTemporalEmitida — correo async, sin bloquear creación (7f, RNF-23)` + trailer.

### Task 2: Consumidor de credenciales que envía el SMTP

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Workers/CredencialesTemporalesConsumer.cs` (BackgroundService calcado de `OperacionesInscripcionesConsumer`: conexión con reintento 30s, exchange/cola/bindings durables, ack-siempre best-effort; cola propia p.ej. `identity.correo-credenciales` ligada a `identity.credencial-temporal-emitida.v1`)
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (registrar el hosted service, patrón del consumidor existente)
- Create/Modify: un mapper/deserializador del envelope al `UserWelcomeEmailMessage` (reusar el `EnvelopeReader`/estilo del consumidor existente); el consumidor resuelve `IUserWelcomeEmailSender` del scope y llama `SendWelcomeEmailAsync`. Fallo de SMTP → log + ack (no poison-loop).
- Modify: tests — mapper/handler del consumidor (deserializa el payload → `UserWelcomeEmailMessage` correcto; fallo de SMTP no relanza). Seguir el estilo de los tests del consumidor existente.
- Modify: `contracts/http/` o `GUIA-LEVANTAMIENTO.md` si documentan la lista de workers/colas (añadir la cola nueva).

**Interfaces:**
- Consume: routing key de Task 1. Produce: correo enviado vía `SmtpUserWelcomeEmailSender` (impl existente de `IUserWelcomeEmailSender`).

- [ ] Step 1: tests RED del mapper (envelope camelCase → `UserWelcomeEmailMessage(nombre, correo, rol, passwordTemporal)`) + del comportamiento best-effort (SMTP lanza → el consumidor no relanza, ack igual).
- [ ] Step 2: implementación del consumidor + registro en Program.cs + mapper.
- [ ] Step 3: GREEN + suite completa verde. (El round-trip real con broker vivo es opt-in como en SP-3i/SP-4a; no se corre en CI por defecto — documentar el comando opt-in si aplica.)
- [ ] Step 4: commit `feat(identity): consumidor RabbitMQ envía el correo de credenciales (7f, RNF-23)` + trailer.

### Task 3: Re-emisión por cambio de email vía evento

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/UpdateUserGeneralDataCommandHandler.cs` (líneas ~60-74: el reenvío del correo tras `ResetTemporaryPasswordAsync` deja de awaitear `_welcomeEmailSender` inline y publica `CredencialTemporalEmitida` con la nueva password; el ctor cambia `IUserWelcomeEmailSender` → `IIdentityEventsPublisher` si aplica)
- Modify: tests del handler (localizar `UpdateUserGeneralDataCommandHandlerTests`): cuando la credencial es temporal y cambia el email → resetea password en Keycloak y **publica** el evento (ya no llama al email sender inline).
- Modify: `contracts/events/identity-events.md` si el sample menciona el disparador (añadir "también se emite al cambiar el email con credencial temporal pendiente").

- [ ] Step 1: test RED — cambio de email con credencial temporal → `ResetTemporaryPasswordAsync` + `PublishCredencialTemporalEmitidaAsync` con la password nueva; sin credencial temporal → no publica (comportamiento actual preservado).
- [ ] Step 2: implementación → GREEN.
- [ ] Step 3: suite completa Identity verde. Commit `feat(identity): re-emisión de credencial temporal por cambio de email vía evento (7f, BR-R05)` + trailer.

### Task 4: Gates completos + ledger

- [ ] Suite completa Identity verde (unit+integration+contract) · árbol limpio · verificar que `IUserWelcomeEmailSender` ya no se inyecta en ningún command handler (solo en el consumidor) con grep · append ledger "7f DONE" con hashes y números.
