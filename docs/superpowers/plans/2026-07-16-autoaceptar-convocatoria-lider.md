# Auto-aceptar la convocatoria del líder — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que un equipo inscrito y aceptado cuente para el mínimo sin que el líder tenga que aceptarse una convocatoria a sí mismo. Hoy un equipo de solo el líder no puede arrancar nunca: la partida se auto-cancela por mínimos.

**Architecture:** La convocatoria del líder nace `Aceptada`. La inscripción persiste `LiderId` (las convocatorias se crean después, al aceptar el operador, y para entonces el flag `EsLider` ya se perdió). El auto-aceptado va guardado por el mismo check de BR-G09 que aplica el camino manual, calculado por el handler y pasado al dominio como bool.

**Tech Stack:** .NET 8, EF Core + Npgsql (con migraciones), MediatR, xUnit.

**Spec:** `docs/superpowers/specs/2026-07-16-autoaceptar-convocatoria-lider-design.md`

## Global Constraints

- **El dominio no consulta el repositorio.** `liderTieneParticipacionActivaEnOtra` lo calcula el handler y entra como parámetro, igual que `ResponderConvocatoria`.
- **El guard de BR-G09 no es opcional.** Sin él, el auto-aceptado salta la validación que el camino manual sí hace y el líder podría tener dos participaciones activas. Si el guard salta → la convocatoria del líder se queda `Pendiente`.
- **El líder sigue recibiendo su convocatoria.** No se le excluye del snapshot: el móvil usa `ConvocatoriaCreada` como señal de que el operador aceptó (spec tiempo-real, H7). Quitársela le mata la pantalla.
- **`EstadoInscripcion` tiene valores históricos** (`Activa = 0`, `Cancelada = 1`, `Pendiente = 2`, `Rechazada = 3`). No reordenar: corrompe filas existentes.
- El mínimo **sigue exigiendo ≥1 convocatoria aceptada** por equipo. No se toca `AplicarInicio`.
- **Cero cambios** en Identity, Partidas, Puntuaciones, gateway, web y móvil. Sin cambios de contrato HTTP.
- `liderid` es `Guid.Empty` en Individual y en las filas migradas.

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `Domain/Entities/InscripcionPartida.cs` | `LiderId`; `PreinscribirEquipo` lo guarda; `Aceptar` acepta la del líder |
| `Domain/Entities/SesionPartida.cs` | Firmas de `PreinscribirEquipo` y `AceptarInscripcion` |
| `Application/Handlers/Commands/PreinscribirEquipoCommandHandler.cs` | Pasa `request.LiderId` |
| `Application/Handlers/Commands/AceptarInscripcionCommandHandler.cs` | Calcula el guard; emite `ConvocatoriaRespondida` del auto-aceptado |
| `Infrastructure/Persistence/**` | Mapeo + migración de `liderid` |

---

## Task 1 — Test que falla: un equipo de solo el líder arranca

- [ ] Test de dominio: sesión Equipo con `minimos = 1`; preinscribir equipo de un solo miembro (el líder); `AceptarInscripcion`; `Iniciar` → hoy devuelve `Cancelada`, debe devolver `Iniciada`.
- [ ] Test de dominio: tras `AceptarInscripcion`, la convocatoria del líder está `Aceptada` y las del resto `Pendiente`.
- [ ] Test de dominio (guard): si el líder tiene participación activa en otra partida, su convocatoria se queda `Pendiente`.
- [ ] Verificar que **fallan por la razón esperada** antes de tocar producción.

## Task 2 — `InscripcionPartida` recuerda a su líder

- [ ] `LiderId` en `InscripcionPartida`; `PreinscribirEquipo(equipoId, liderId, miembros, partidaId, fecha)` lo guarda. `Guid.Empty` en Individual.
- [ ] `SesionPartida.PreinscribirEquipo` acepta y reenvía `liderId`.
- [ ] `PreinscribirEquipoCommandHandler` pasa `request.LiderId`.

## Task 3 — `Aceptar` auto-acepta la del líder, con guard

- [ ] `Aceptar(now, liderTieneParticipacionActivaEnOtra)`: crea las convocatorias; la del líder nace `Aceptada` salvo que el guard salte.
- [ ] `SesionPartida.AceptarInscripcion(inscripcionId, inscritosActivos, liderTieneParticipacionActivaEnOtra, now)`.
- [ ] `AceptarInscripcionCommandHandler` calcula el bool con `ParticipanteTieneParticipacionActivaAsync(inscripcion.LiderId, partidaId)` **solo** para inscripciones de Equipo.
- [ ] Los tests de Task 1 pasan.

## Task 4 — Persistencia

- [ ] Mapear `LiderId` → columna `liderid`.
- [ ] Migración EF (`dotnet ef migrations add`). Filas existentes → `Guid.Empty`.
- [ ] Test de persistencia: `LiderId` sobrevive al round-trip.

## Task 5 — Evento del auto-aceptado

- [ ] Tras `ConvocatoriaCreada`, emitir `ConvocatoriaRespondida` para la convocatoria del líder si nació `Aceptada`. Sin esto, un consumidor la proyecta como `Pendiente` (`ConvocatoriaCreadaEvent` no lleva `Estado`).
- [ ] Test de handler: se emiten ambos eventos, en ese orden.

## Task 6 — Verificación end-to-end

- [ ] `dotnet test services/operaciones-sesion/<Solution>.sln` en verde.
- [ ] Aplicar la migración contra la base real y comprobar que arranca sin error.
- [ ] Ejercitar el flujo real: equipo de solo el líder → inscribir → operador acepta → operador inicia → **la partida inicia**, no se cancela.
- [ ] Actualizar `docs/04-sdd/SPECS-LIST.md` y `docs/04-sdd/traceability-matrix.md`; anotar en el spec `sp3e1` que su §5 queda superseded.
