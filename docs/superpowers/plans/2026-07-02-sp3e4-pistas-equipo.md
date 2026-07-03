# SP-3e-4 Pistas BDT Equipo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Habilitar pistas con destino equipo en modalidad Equipo: el operador envía la pista a un equipo y la reciben todos sus miembros conectados vía el grupo SignalR nuevo `equipo:{equipoId}`.

**Architecture:** Un solo servicio (Operaciones de Sesión). Cadena dual-destino: request/command/response/evento/payload ganan `Guid? EquipoDestinoId` trailing y `ParticipanteDestinoId` pasa a `Guid?` (conversión implícita cubre call sites). Dominio nuevo `PrepararPistaEquipo` (409 Individual / 404 equipo no inscrito / guards BDT actuales). El hub une al grupo de equipo server-side (identidad del JWT `sub`, resuelta contra la sesión que ya carga hoy). Event-only: sin persistencia, sin migración, sin proyecciones.

**Tech Stack:** .NET 8, MediatR, FluentValidation, SignalR, xUnit con fakes a mano (NO Moq).

**Spec:** `docs/superpowers/specs/2026-07-02-sp3e4-pistas-equipo-design.md` (commit `3b1f084`)

## Global Constraints

- Servicio: `services/operaciones-sesion/` únicamente.
- Reglas: destino en Equipo = equipo entero (sin pista a miembro individual en sesión Equipo); destino válido = equipo con inscripción activa (`InscripcionNoEncontrada` → 404); destino-equipo en Individual → `ModalidadNoSoportada` (409); destino-participante en Equipo → 403 actual sin cambios; exactamente un destino por request (400 FluentValidation); guards BDT actuales (`JuegoActivoNoEsBDT`/`NoHayEtapaActiva` → 409). Sin persistencia (event-only, igual SP-3f-4).
- Individual NO cambia de comportamiento: `EquipoDestinoId` default null en todos los records extendidos; routing publisher: sin `EquipoDestinoId` → grupo `participante:{id}` actual.
- Suites completas antes de cada commit:
  `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
  `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
  (y ContractTests en E4). Baseline: Unit 308, Integration 28, Contract 48. Verdes, output limpio.
- **Git (HARD):** PROHIBIDO `git checkout/restore/clean/stash/reset/rebase` en cualquier forma. NUNCA `git add -A`, `git add .`, `git add docs/` ni adds sin path exacto — stagear SOLO los archivos nombrados. Estos quedan SIEMPRE unstaged/uncommitted: `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. Mensaje de commit: línea resumen `SP-3e-4 EN: ...`, línea en blanco, y EXACTAMENTE este trailer final (nada después):
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- Shell zsh: comillas en globs (`--include="*.cs"`); rutas absolutas desde `/home/santiago/Escritorio/ProyectoDesarrollo`.

## File Structure (mapa completo del slice)

```
services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/
  Entities/SesionPartida.cs                  (M en E1: += PrepararPistaEquipo)
  Exceptions/InscripcionNoEncontradaException.cs (M en E1: += factory ParaEquipo)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/
  DTOs/BdtRuntimeDtos.cs                     (M en E2: EnviarPistaRequest + PistaEnviadaResponse duales)
  Commands/EnviarPistaCommand.cs             (M en E2: dual destino)
  Validators/EnviarPistaCommandValidator.cs  (M en E2: exactamente-uno)
  Handlers/Commands/EnviarPistaCommandHandler.cs (M en E2: rama por destino)
  Interfaces/BdtRuntimeEvents.cs             (M en E2: PistaEnviadaEvent dual)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/
  Controllers/SesionesController.cs          (M en E2: pasa EquipoDestinoId al command)
  Realtime/SesionRealtimeMessages.cs         (M en E2: += GrupoEquipo)
  Realtime/SesionRealtimePayloads.cs         (M en E2: PistaEnviadaPayload dual)
  Realtime/SignalRSesionEventsPublisher.cs   (M en E2: routing por destino)
  Realtime/SesionHub.cs                      (M en E3: join/leave grupo equipo)
services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/
  Domain/PrepararPistaEquipoTests.cs         (C en E1: 4 tests)
  Application/EnviarPistaCommandValidatorTests.cs (M en E2: +3 tests)
  Application/EnviarPistaEquipoHandlerTests.cs (C en E2: 2 tests)
  Api/Realtime/SesionRealtimeMessagesTests.cs (M en E2: +2 tests)
  Api/Realtime/SignalRSesionEventsPublisherTests.cs (M en E2: +1 test)
  Api/Realtime/SesionHubTests.cs             (M en E3: +3 tests)
contracts/http/operaciones-sesion-api.md     (M en E4: fila pistas + notas realtime)
docs/04-sdd/traceability-matrix.md           (M en E4: fila SP-3e-4 — NUNCA stagear)
```

Call sites que compilan sin tocar (verificado por grep en HEAD `3b1f084` — `Guid` → `Guid?` implícito + trailing defaults):
- `EnviarPistaCommandHandlerTests.cs:22/43/56` (`EnviarPistaCommand` 3 args posicionales).
- `SesionesControllerBdtTests.cs:62` (`PistaEnviadaResponse` 4 args).
- `SignalRSesionEventsPublisherTests.cs:136` y `CompositeSesionEventsPublisherTests.cs:61` (`PistaEnviadaEvent` 6 args).
- `EnviarPistaCommandValidatorTests.cs` (5 tests existentes siguen válidos con las reglas nuevas — el destino participante presente cumple exactamente-uno).

---

### Task E1: Dominio — PrepararPistaEquipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs` (después de `PrepararPista`, ~línea 283)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/InscripcionNoEncontradaException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PrepararPistaEquipoTests.cs` (nuevo)

**Interfaces:**
- Consumes (existentes, verificadas en HEAD): `Modalidad` (property de `SesionPartida`), `_inscripciones` (`InscripcionPartida.EquipoId` es `Guid?`, `EsActiva`), `JuegoBDTActivo()`, `JuegoResumen.EtapaActiva`, `NoHayEtapaActivaException(Guid)`, `ModalidadNoSoportadaException(Guid)`; helpers de test: `PreinscribirEquipo(Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros, bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)`, `ResponderConvocatoria(Guid convocatoriaId, Guid usuarioId, bool aceptar, bool participanteTieneParticipacionActivaEnOtra, DateTime now)`, `Inscribir(Guid participanteId, bool, int, DateTime)`, `EtapaSnapshot(Guid, int, string, int, int)`, `JuegoResumen(Guid, int, TipoJuego, string, IReadOnlyList<EtapaSnapshot>)`.
- Produces (para E2): `public Guid PrepararPistaEquipo(Guid equipoDestinoId)` en `SesionPartida`; `InscripcionNoEncontradaException.ParaEquipo(Guid equipoId)` (factory estática).

- [ ] **Step 1: Escribir tests que fallan** — crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PrepararPistaEquipoTests.cs`:

```csharp
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class PrepararPistaEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionBdtEquipoIniciada(out Guid equipoA)
    {
        var lider = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoALocal, true, new[] { lider }, false, 0, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
        sesion.Iniciar(T0);
        equipoA = equipoALocal;
        return sesion;
    }

    [Fact]
    public void Equipo_inscrito_devuelve_juegoid()
    {
        var sesion = SesionBdtEquipoIniciada(out var equipoA);

        var juegoId = sesion.PrepararPistaEquipo(equipoA);

        Assert.Equal(sesion.Juegos.Single().JuegoId, juegoId);
    }

    [Fact]
    public void Sesion_individual_lanza_modalidad_no_soportada()
    {
        var jugador = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        sesion.Inscribir(jugador, false, 0, T0);
        sesion.Iniciar(T0);

        Assert.Throws<ModalidadNoSoportadaException>(() => sesion.PrepararPistaEquipo(Guid.NewGuid()));
    }

    [Fact]
    public void Equipo_no_inscrito_lanza_inscripcion_no_encontrada()
    {
        var sesion = SesionBdtEquipoIniciada(out _);

        Assert.Throws<InscripcionNoEncontradaException>(() => sesion.PrepararPistaEquipo(Guid.NewGuid()));
    }

    [Fact]
    public void Sin_etapa_activa_lanza_no_hay_etapa_activa()
    {
        var sesion = SesionBdtEquipoIniciada(out var equipoA);
        sesion.AvanzarEtapa(T0.AddSeconds(5)); // cierra la única etapa; no queda activa

        Assert.Throws<NoHayEtapaActivaException>(() => sesion.PrepararPistaEquipo(equipoA));
    }
}
```

- [ ] **Step 2: Correr para verificar RED**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~PrepararPistaEquipoTests" 2>&1 | tail -10`
Expected: FAIL de compilación — CS1061 (`SesionPartida` no contiene `PrepararPistaEquipo`). Capturar.

- [ ] **Step 3: Implementar dominio**

3a. `InscripcionNoEncontradaException.cs` — añadir ctor privado por mensaje + factory (el ctor existente no cambia):

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class InscripcionNoEncontradaException : Exception
{
    public InscripcionNoEncontradaException(Guid participanteId)
        : base($"El participante {participanteId} no tiene una inscripción activa en esta partida.") { }

    private InscripcionNoEncontradaException(string message) : base(message) { }

    public static InscripcionNoEncontradaException ParaEquipo(Guid equipoId) =>
        new($"El equipo {equipoId} no tiene una inscripción activa en esta partida.");
}
```

3b. `SesionPartida.cs` — nuevo método inmediatamente después de `PrepararPista` (~línea 283):

```csharp
    public Guid PrepararPistaEquipo(Guid equipoDestinoId)
    {
        if (Modalidad != Modalidad.Equipo)
            throw new ModalidadNoSoportadaException(PartidaId);
        if (!_inscripciones.Any(i => i.EquipoId == equipoDestinoId && i.EsActiva))
            throw InscripcionNoEncontradaException.ParaEquipo(equipoDestinoId);
        var juego = JuegoBDTActivo();
        _ = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);
        return juego.JuegoId;
    }
```

- [ ] **Step 4: Correr filtro nuevo y verificar GREEN**

Run: mismo filtro del Step 2.
Expected: PASS 4/4.

- [ ] **Step 5: Suites completas**

Expected: Unit 312/312 (308 + 4), Integration 28/28.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/InscripcionNoEncontradaException.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PrepararPistaEquipoTests.cs
git commit -m "SP-3e-4 E1: dominio PrepararPistaEquipo (409 Individual, 404 equipo no inscrito, guards BDT)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task E2: Cadena dual-destino — request → command → evento → payload → routing

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/BdtRuntimeDtos.cs:15-16`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/EnviarPistaCommand.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/EnviarPistaCommandValidator.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/EnviarPistaCommandHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs:19-21` (solo `PistaEnviadaEvent`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs:137`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs` (+= `GrupoEquipo`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs:16`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs:78-83`
- Test (M): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaCommandValidatorTests.cs` (+3)
- Test (C): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaEquipoHandlerTests.cs` (2)
- Test (M): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs` (+2)
- Test (M): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs` (+1)

**Interfaces:**
- Consumes (de E1): `SesionPartida.PrepararPistaEquipo(Guid) → Guid`.
- Consumes (existentes): `FakeSesionPartidaRepository.Add`, `FakeSesionEventsPublisher.PistasEnviadas` (verificar nombre real de la colección en el fake antes de usar — si no existe lista para pista, mirar cómo la testea `EnviarPistaCommandHandlerTests.cs` y seguir ese patrón), `FakeTimeProvider`, patrón `Build()`/`FakeClients` de `SignalRSesionEventsPublisherTests`.
- Produces (para E3/E4): `SesionRealtimeMessages.GrupoEquipo(Guid equipoId) → $"equipo:{equipoId}"`; `PistaEnviadaEvent(Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, string Texto, DateTime Instante, Guid? EquipoDestinoId = null)`; `PistaEnviadaPayload(Guid PartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, string Texto, DateTime TimestampUtc, Guid? EquipoDestinoId = null)`; `EnviarPistaRequest(Guid? ParticipanteDestinoId, string Texto, Guid? EquipoDestinoId = null)`; `PistaEnviadaResponse(Guid PartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, DateTime TimestampUtc, Guid? EquipoDestinoId = null)`.

- [ ] **Step 1: Escribir tests que fallan**

1a. Añadir a `EnviarPistaCommandValidatorTests.cs` (3 tests; usar el campo `_v` existente):

```csharp
    [Fact]
    public void Sin_ningun_destino_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), null, "hola")).IsValid);

    [Fact]
    public void Con_ambos_destinos_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "hola", Guid.NewGuid())).IsValid);

    [Fact]
    public void Solo_equipo_destino_es_valido() =>
        Assert.True(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), null, "hola", Guid.NewGuid())).IsValid);
```

1b. Crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaEquipoHandlerTests.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EnviarPistaEquipoHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionBdtEquipoIniciada(out Guid equipoA)
    {
        var lider = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoALocal, true, new[] { lider }, false, 0, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
        sesion.Iniciar(T0);
        equipoA = equipoALocal;
        return sesion;
    }

    [Fact]
    public async Task Destino_equipo_publica_evento_con_equipo_y_participante_null()
    {
        var sesion = SesionBdtEquipoIniciada(out var equipoA);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new EnviarPistaCommandHandler(repo, events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(
            new EnviarPistaCommand(sesion.PartidaId, null, "Mira el mural norte", equipoA), CancellationToken.None);

        var evento = events.PistasEnviadas.Single();
        Assert.Equal(equipoA, evento.EquipoDestinoId);
        Assert.Null(evento.ParticipanteDestinoId);
        Assert.Equal("Mira el mural norte", evento.Texto);
        Assert.Equal(equipoA, resp.EquipoDestinoId);
        Assert.Null(resp.ParticipanteDestinoId);
        Assert.Equal(sesion.Juegos.Single().JuegoId, resp.JuegoId);
    }

    [Fact]
    public async Task Destino_participante_mantiene_flujo_individual_con_equipo_null()
    {
        var jugador = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        sesion.Inscribir(jugador, false, 0, T0);
        sesion.Iniciar(T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new EnviarPistaCommandHandler(repo, events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(
            new EnviarPistaCommand(sesion.PartidaId, jugador, "Mira el faro"), CancellationToken.None);

        var evento = events.PistasEnviadas.Single();
        Assert.Null(evento.EquipoDestinoId);
        Assert.Equal(jugador, evento.ParticipanteDestinoId);
        Assert.Null(resp.EquipoDestinoId);
        Assert.Equal(jugador, resp.ParticipanteDestinoId);
    }
}
```

> Nota: si la colección del fake para pistas no se llama `PistasEnviadas`, usar el nombre real (ver `FakeSesionEventsPublisher.cs` y cómo asserta `EnviarPistaCommandHandlerTests.cs`) y disclosear.

1c. Añadir a `SesionRealtimeMessagesTests.cs` (2 tests):

```csharp
    [Fact]
    public void GrupoEquipo_tiene_formato_estable()
    {
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Assert.Equal("equipo:33333333-3333-3333-3333-333333333333",
            SesionRealtimeMessages.GrupoEquipo(id));
    }

    [Fact]
    public void GrupoEquipo_difiere_de_los_otros_grupos()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(SesionRealtimeMessages.GrupoPartida(id), SesionRealtimeMessages.GrupoEquipo(id));
        Assert.NotEqual(SesionRealtimeMessages.GrupoParticipante(id), SesionRealtimeMessages.GrupoEquipo(id));
        Assert.NotEqual(SesionRealtimeMessages.GrupoOperadorPartida(id), SesionRealtimeMessages.GrupoEquipo(id));
    }
```

1d. Añadir a `SignalRSesionEventsPublisherTests.cs` (1 test; usar el helper `Build()` existente):

```csharp
    [Fact]
    public async Task PistaEnviada_con_equipo_destino_difunde_solo_al_grupo_del_equipo()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var equipo = Guid.NewGuid();

        await pub.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(partidaId, Guid.NewGuid(), juegoId, null, "Al norte del patio", T0, equipo),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoEquipo(equipo), clients.LastGroup);
        Assert.Equal(SesionRealtimeMessages.PistaEnviada, clients.Proxy.Method);
        var payload = Assert.IsType<PistaEnviadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(equipo, payload.EquipoDestinoId);
        Assert.Null(payload.ParticipanteDestinoId);
        Assert.Equal("Al norte del patio", payload.Texto);
    }
```

- [ ] **Step 2: Correr para verificar RED**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~EnviarPistaEquipoHandlerTests|FullyQualifiedName~GrupoEquipo|FullyQualifiedName~equipo_destino" 2>&1 | tail -10`
Expected: FAIL de compilación (CS1061/CS1729 — miembros y arities nuevos inexistentes). Capturar.

- [ ] **Step 3: Implementar la cadena**

3a. `BdtRuntimeDtos.cs` líneas 15-16:

```csharp
public sealed record EnviarPistaRequest(Guid? ParticipanteDestinoId, string Texto, Guid? EquipoDestinoId = null);
public sealed record PistaEnviadaResponse(Guid PartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, DateTime TimestampUtc, Guid? EquipoDestinoId = null);
```

3b. `EnviarPistaCommand.cs`:

```csharp
public sealed record EnviarPistaCommand(Guid PartidaId, Guid? ParticipanteDestinoId, string Texto, Guid? EquipoDestinoId = null) : IRequest<PistaEnviadaResponse>;
```

3c. `EnviarPistaCommandValidator.cs`:

```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class EnviarPistaCommandValidator : AbstractValidator<EnviarPistaCommand>
{
    public EnviarPistaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.ParticipanteDestinoId.HasValue ^ x.EquipoDestinoId.HasValue)
            .WithMessage("Debe indicarse exactamente un destino: participanteDestinoId o equipoDestinoId.");
        RuleFor(x => x.ParticipanteDestinoId).NotEqual(Guid.Empty).When(x => x.ParticipanteDestinoId.HasValue);
        RuleFor(x => x.EquipoDestinoId).NotEqual(Guid.Empty).When(x => x.EquipoDestinoId.HasValue);
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(500);
    }
}
```

3d. `BdtRuntimeEvents.cs` — solo `PistaEnviadaEvent` (los otros 4 records NO se tocan):

```csharp
public sealed record PistaEnviadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid? ParticipanteDestinoId,
    string Texto, DateTime Instante, Guid? EquipoDestinoId = null);
```

3e. `EnviarPistaCommandHandler.cs` — método `Handle` completo:

```csharp
    public async Task<PistaEnviadaResponse> Handle(EnviarPistaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var juegoId = request.EquipoDestinoId is { } equipoDestino
            ? sesion.PrepararPistaEquipo(equipoDestino)
            : sesion.PrepararPista(request.ParticipanteDestinoId!.Value); // el validator garantiza exactamente un destino

        await _events.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(
                sesion.PartidaId, sesion.Id.Valor, juegoId, request.ParticipanteDestinoId, request.Texto, now,
                request.EquipoDestinoId),
            cancellationToken);

        return new PistaEnviadaResponse(sesion.PartidaId, juegoId, request.ParticipanteDestinoId, now, request.EquipoDestinoId);
    }
```

3f. `SesionesController.cs` línea 137:

```csharp
        => Ok(await _mediator.Send(new EnviarPistaCommand(partidaId, request.ParticipanteDestinoId, request.Texto, request.EquipoDestinoId), cancellationToken));
```

3g. `SesionRealtimeMessages.cs` — tras `GrupoParticipante` (línea 23):

```csharp
    public static string GrupoEquipo(Guid equipoId) => $"equipo:{equipoId}";
```

3h. `SesionRealtimePayloads.cs` línea 16:

```csharp
public sealed record PistaEnviadaPayload(Guid PartidaId, Guid JuegoId, Guid? ParticipanteDestinoId, string Texto, DateTime TimestampUtc, Guid? EquipoDestinoId = null);
```

3i. `SignalRSesionEventsPublisher.cs` — `PublicarPistaEnviadaAsync` completo:

```csharp
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) =>
        _hub.Clients.Group(evento.EquipoDestinoId is { } equipo
                ? SesionRealtimeMessages.GrupoEquipo(equipo)
                : SesionRealtimeMessages.GrupoParticipante(evento.ParticipanteDestinoId!.Value))
            .SendAsync(
                SesionRealtimeMessages.PistaEnviada,
                new PistaEnviadaPayload(evento.PartidaId, evento.JuegoId, evento.ParticipanteDestinoId, evento.Texto, evento.Instante, evento.EquipoDestinoId),
                cancellationToken);
```

- [ ] **Step 4: Correr filtros nuevos y verificar GREEN**

Run: mismo filtro del Step 2.
Expected: PASS (2 handler + 2 messages + 1 publisher + los 3 del validator vía suite completa).

- [ ] **Step 5: Búsqueda repo-wide de construction sites (lección B13)**

Run: `grep -rn "PistaEnviadaEvent(\|EnviarPistaCommand(\|PistaEnviadaResponse(\|PistaEnviadaPayload(\|EnviarPistaRequest(" services/operaciones-sesion/src services/operaciones-sesion/tests --include="*.cs" | grep -v "record "`
Expected: solo los sitios del mapa del plan; los listados como "compilan sin tocar" siguen sin tocar. Si algo más rompe, fix mínimo + disclose.

- [ ] **Step 6: Suites completas**

Expected: Unit 320/320 (312 + 3 validator + 2 handler + 2 messages + 1 publisher), Integration 28/28.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/BdtRuntimeDtos.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/EnviarPistaCommand.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/EnviarPistaCommandValidator.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/EnviarPistaCommandHandler.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaCommandValidatorTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaEquipoHandlerTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs
git commit -m "SP-3e-4 E2: cadena dual-destino pista (request/command/evento/payload) + routing a grupo equipo

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task E3: Hub — unirse/salir del grupo de equipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs`
- Test (M): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs` (+3)

**Interfaces:**
- Consumes (de E2): `SesionRealtimeMessages.GrupoEquipo(Guid)`.
- Consumes (existentes): `SesionPartida.Inscripciones` (`IReadOnlyList<InscripcionPartida>`; `EsActiva`, `EquipoId` es `Guid?`, `Convocatorias` con `UsuarioId`/`EstaAceptada`); helpers de test `Usuario(sub, rol)`, `Construir(repo, user, groups)`, `ISesionPartidaRepositorioFake`, `FakeGroupManager` (colecciones `Added`/`Removed`), patrón `SesionDe` (Individual) — para Equipo construir la sesión con `PreinscribirEquipo`+`ResponderConvocatoria` como en `PrepararPistaEquipoTests`.
- Produces: conexión de convocado aceptado unida a `equipo:{equipoId}` mientras está suscrito.

- [ ] **Step 1: Escribir tests que fallan** — añadir a `SesionHubTests.cs` (usar los helpers existentes del archivo; añadir helper de sesión Equipo):

```csharp
    private static SesionPartida SesionEquipoDe(Guid partidaId, Guid participanteId, out Guid equipoId)
    {
        var equipoLocal = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, Array.Empty<PreguntaSnapshot>());
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        var t0 = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var ins = s.PreinscribirEquipo(equipoLocal, true, new[] { participanteId }, false, 0, t0);
        s.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, participanteId, true, false, t0);
        equipoId = equipoLocal;
        return s;
    }

    [Fact]
    public async Task Convocado_aceptado_se_une_al_grupo_de_su_equipo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionEquipoDe(partidaId, participanteId, out var equipoId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoEquipo(equipoId)), groups.Added);
    }

    [Fact]
    public async Task Desuscribir_remueve_el_grupo_de_equipo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionEquipoDe(partidaId, participanteId, out var equipoId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);
        await hub.SuscribirAPartida(partidaId);

        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoEquipo(equipoId)), groups.Removed);
    }

    [Fact]
    public async Task Inscrito_individual_no_se_une_a_grupo_de_equipo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.DoesNotContain(groups.Added, g => g.Item2.StartsWith("equipo:"));
    }
```

> Nota: si `FakeGroupManager.Added`/`Removed` no son tuplas `(connId, group)`, adaptar los asserts al shape real del fake (leer `FakeGroupManager` antes) y disclosear.

- [ ] **Step 2: Correr para verificar RED**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionHubTests" 2>&1 | tail -10`
Expected: FAIL — los 2 primeros tests nuevos fallan por assert (el hub no une al grupo de equipo); el 3º pasa. Capturar.

- [ ] **Step 3: Implementar hub** — en `SesionHub.cs`:

3a. Constante nueva junto a las existentes (líneas 13-14):

```csharp
    private const string ClaveEquipoId = "equipoId";
```

3b. En `SuscribirAPartida`, tras `Context.Items[ClaveParticipanteId] = participanteId;` (línea 49) y antes de los `AddToGroupAsync` existentes, añadir resolución de equipo (requiere `using System.Linq;` al tope del archivo):

```csharp
        var inscripcionEquipo = sesion.Inscripciones.FirstOrDefault(i => i.EsActiva
            && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.EstaAceptada));
```

y tras el `AddToGroupAsync(GrupoParticipante...)` existente (línea 51):

```csharp
        if (inscripcionEquipo?.EquipoId is { } equipoId)
        {
            Context.Items[ClaveEquipoId] = equipoId;
            await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoEquipo(equipoId), Context.ConnectionAborted);
        }
```

3c. En `DesuscribirDePartida`, tras el bloque existente de `GrupoParticipante` (líneas 58-61):

```csharp
        if (Context.Items.TryGetValue(ClaveEquipoId, out var e) && e is Guid equipoId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoEquipo(equipoId), Context.ConnectionAborted);
        }
```

- [ ] **Step 4: Correr filtro y verificar GREEN**

Run: mismo filtro del Step 2.
Expected: PASS (todos los del archivo, incluidos los 3 nuevos).

- [ ] **Step 5: Suites completas**

Expected: Unit 323/323 (320 + 3), Integration 28/28.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "SP-3e-4 E3: hub une convocado aceptado al grupo equipo:{id} (server-side desde JWT sub)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task E4: Contrato HTTP + traceability (carve-out)

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md` (STAGE + COMMIT)
- Modify: `docs/04-sdd/traceability-matrix.md` (EDITAR, NUNCA stagear ni commitear)

**Interfaces:**
- Consumes: todo E1-E3 — verificar cada claim contra código antes de escribirlo.
- Produces: contrato actualizado; 3 suites verdes.

- [ ] **Step 1: Actualizar contrato** — en `contracts/http/operaciones-sesion-api.md`:
  - Fila/sección de `POST /operaciones-sesion/partidas/{partidaId}/pistas`: request ahora `{ participanteDestinoId?, texto, equipoDestinoId? }` — **exactamente un destino** (si no, 400); response gana `equipoDestinoId?` y `participanteDestinoId` pasa a nullable; códigos nuevos del destino equipo: 404 (equipo sin inscripción activa) y 409 (destino-equipo en partida Individual); destino-participante en partida Equipo → 403 actual.
  - Nota realtime de suscripción (línea ~80): `SuscribirAPartida` además une al grupo `equipo:{equipoId}` cuando el caller es convocado con convocatoria Aceptada en una sesión Equipo; `DesuscribirDePartida` la retira.
  - Nota realtime de `PistaEnviada` (línea ~100): se difunde al grupo `participante:{destinoId}` **o** al grupo `equipo:{equipoDestinoId}` según el destino del request (SP-3e-4); el payload gana `equipoDestinoId?` y `participanteDestinoId` pasa a nullable.
  - Verificar contra código: validator (400), `PrepararPistaEquipo` (404/409), routing del publisher, hub join/leave, shape real de `PistaEnviadaPayload`.
- [ ] **Step 2: Editar traceability (SIN stagear)** — fila SP-3e-4 en `docs/04-sdd/traceability-matrix.md` (formato de la fila SP-3e-3: spec, plan, commits E1-E3, archivos clave, conteos reales).
- [ ] **Step 3: Las 3 suites completas**

Run: Unit + Integration + Contract.
Expected: Unit 323/323, Integration 28/28, Contract 48/48 (si un ContractTest de paridad doc↔constantes falla por la edición, ajustar la edición — no el test).

- [ ] **Step 4: Commit (SOLO el contrato)**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "SP-3e-4 E4: contrato — pista con destino equipo (dual destino, 400/404/409, grupo equipo:{id})

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git status --short   # verificar traceability-matrix.md sigue " M" unstaged
```

---

## Self-Review (ejecutado al escribir el plan)

1. **Spec coverage:** §2 reglas → E1 (404/409/guards) + E2 (400 exactamente-uno, destino-participante-en-Equipo sin cambios) + E3 (membresía server-side); §3.1-3.3 → E2; §3.4 → E2 (routing/payload/grupo) + E3 (hub); §3.5 sin persistencia → ningún task EF (correcto); §4 testing → E1 (4 dominio), E2 (3 validator + 2 handler + 2 messages + 1 publisher, regresión Individual en handler y publisher existentes), E3 (3 hub incl. Individual-no-join), E4 (contrato + 3 suites). Sin gaps.
2. **Placeholders:** ninguno — código completo en cada step; las 2 notas "si el fake difiere, adaptar y disclosear" son contingencias explícitas con instrucción concreta, no TODOs.
3. **Consistencia de tipos:** `PistaEnviadaEvent` 7 posicionales igual en 3d (E2), test 1d y publisher 3i; `EnviarPistaCommand` 4 posicionales igual en 3b, tests 1a/1b y controller 3f; `PistaEnviadaResponse` con `TimestampUtc` (nombre real verificado, no `Instante`); `GrupoEquipo` definido en E2-3g y consumido en E2-1d/3i y E3; helper Equipo usa firmas verificadas (`PreinscribirEquipo` 6 args, `ResponderConvocatoria` 5 args).
4. **Firmas verificadas contra HEAD `3b1f084`:** shapes actuales de request/response/command/evento/payload/validator leídos del código; call sites enumerados por grep; `InscripcionPartida.EquipoId` es `Guid?`; `SesionPartida.Inscripciones` público; `GetByParticipanteActivoAsync` ya matchea convocados aceptados (hub Equipo funciona hoy); patrón de tests hub/publisher/validator leído de los archivos reales.
