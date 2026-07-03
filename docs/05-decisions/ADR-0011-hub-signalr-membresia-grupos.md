# ADR-0011 — Los hubs SignalR resuelven membresía de grupos vía repositorio de lectura

- **Estado:** Accepted
- **Fecha:** 2026-07-03
- **Contexto de origen:** hallazgo I-4 (escalado) del informe de auditoría `docs/04-sdd/auditorias/2026-07-02-informe-conformidad-sp3c-3e4.md`.

## Contexto

CLAUDE.md ("Structure & coding rules") exige que la capa `Api/` despache por MediatR y no contenga lógica de negocio. `SesionHub.SuscribirAPartida` (Operaciones de Sesión) valida la pertenencia del caller a la partida y resuelve su grupo de equipo inyectando `ISesionPartidaRepository` directamente, sin MediatR. El design aprobado SP-3f-2 lo mandó explícitamente ("reutiliza la consulta de participación existente") y SP-3e-4 extendió el patrón al grupo `equipo:{id}`. La auditoría SP-3c..3e-4 escaló la tensión entre ambas autoridades.

## Decisión

Los hubs SignalR de los servicios UMBRAL **pueden resolver la validación de pertenencia y la membresía de grupos en el handshake de suscripción vía repositorio de lectura inyectado**, sin despachar por MediatR.

Racional: es validación de identidad/pertenencia del canal realtime — equivalente funcional a un middleware de autorización, no a un command/query de negocio. La identidad sale siempre del JWT `sub` server-side; el cliente solo aporta `partidaId`.

## Límites (siguen vigentes)

1. **Solo lectura.** Un hub nunca muta estado ni invoca `SaveChanges`/unit-of-work.
2. **Solo handshake.** El patrón aplica a métodos de suscripción/desuscripción (`SuscribirAPartida`, `DesuscribirDePartida`). Cualquier otra operación de hub con reglas de negocio debe despachar por MediatR.
3. **Relay puro permitido.** Métodos como `EnviarUbicacion` (relay a grupo operador, sin persistencia, BR-B07) permanecen sin repositorio ni MediatR.
4. Las excepciones de hub (`HubException`) son el mecanismo de rechazo del canal realtime; el middleware HTTP de excepciones no aplica a hubs.

## Consecuencias

- Cierra el hallazgo I-4 sin cambio de código; el patrón existente en `SesionHub` queda sancionado.
- Futuros hubs (p. ej. Puntuaciones/SignalR en SP-4) heredan esta regla y sus límites.

## Referencias

- Informe de auditoría 2026-07-02, hallazgo I-4.
- Spec SP-3f-2 (`docs/superpowers/specs/2026-06-30-sp3f2-push-tiempo-real-signalr-design.md`), sección del hub.
- CLAUDE.md, "Structure & coding rules (graded)".
