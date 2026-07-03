# SP-3e-3 Runtime BDT Equipo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Habilitar subir/validar QR de tesoro en modalidad Equipo: una validación correcta de cualquier miembro activo gana la etapa para todo el equipo, con identidad dual autor+equipo (patrón SP-3e-2).

**Architecture:** Un solo servicio (Operaciones de Sesión). Identidad dual: autor real en `ParticipanteId`, equipo en `Guid? EquipoId` nuevo (null ⇔ Individual) a través de dominio → resultado → eventos → persistencia. **Sin dedup** — reintentos ilimitados, espejo de Individual (a diferencia de Trivia). Sin cambios en payloads SignalR (verificado: `EtapaActivadaPayload`/`EtapaCerradaPayload`/`EtapaGanadaPayload` no portan identidad), sin cambios en proyecciones (BDT no tiene `yaRespondio`), sin endpoints nuevos.

**Tech Stack:** .NET 8, EF Core 8 (Npgsql runtime / InMemory tests), MediatR, xUnit con fakes a mano (NO Moq).

**Spec:** `docs/superpowers/specs/2026-07-02-sp3e3-runtime-bdt-equipo-design.md` (commit `986861c`)

## Global Constraints

- Servicio: `services/operaciones-sesion/` únicamente. Clean Architecture: Domain → Application → Infrastructure → Api.
- Regla de negocio: **reintentos ilimitados** — cualquier miembro con convocatoria Aceptada intenta cuantas veces quiera hasta cierre de etapa; QR incorrecto solo registra `TesoroQR` (autor + equipo), no sella a nadie. Miembro Pendiente/Rechazado → `ParticipanteNoInscritoException` (403). Primera validación correcta en ventana → etapa `Ganada` con `GanadorParticipanteId`=autor y `GanadorEquipoId`=equipo; cierre global y auto-avance sin cambios. Timeout/AvanzarEtapa sin ganador (sin cambios).
- Individual NO cambia de comportamiento: todo campo nuevo es `Guid?` con default `null` en records/ctors extendidos — EXCEPTO `EtapaSnapshot.RegistrarTesoro`, donde `equipoId` va posicional SIN default (espejo de la decisión C1 con `RegistrarRespuesta`); sus 5 llamadas en `EtapaSnapshotTests.cs` se actualizan con `null`.
- Suites completas antes de cada commit:
  `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
  `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
  (y ContractTests en D4). Baseline actual: Unit 300, Integration 27, Contract 48. Verdes, output limpio.
- **Git (HARD):** PROHIBIDO `git checkout/restore/clean/stash/reset/rebase` en cualquier forma. NUNCA `git add -A`, `git add .`, `git add docs/` ni adds sin path exacto — stagear SOLO los archivos nombrados. Estos quedan SIEMPRE unstaged/uncommitted: `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. Mensaje de commit: línea resumen `SP-3e-3 DN: ...`, línea en blanco, y EXACTAMENTE este trailer final (nada después):
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- Shell zsh: comillas en globs (`--include="*.cs"`); rutas absolutas desde `/home/santiago/Escritorio/ProyectoDesarrollo`.

## File Structure (mapa completo del slice)

```
services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/
  Entities/TesoroQR.cs                       (M: += EquipoId? property + ctor default)
  Entities/EtapaSnapshot.cs                  (M: GanadorEquipoId? + RegistrarTesoro con equipoId posicional)
  Entities/SesionPartida.cs                  (M: ValidarTesoro resolución dual + ResultadoRegistroTesoro extendido)
  Results/ResultadoRegistroTesoro.cs         (M: += EquipoId?, GanadorEquipoId? trailing defaults)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/
  Interfaces/BdtRuntimeEvents.cs             (M: 3 records += Guid? trailing default)
  Handlers/Commands/ValidarTesoroCommandHandler.cs (M: propaga EquipoId/GanadorEquipoId en 3 sitios)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/
  OperacionesSesionDbContext.cs              (M: 2 HasColumnName)
  Migrations/<ts>_SP3e3RuntimeBdtEquipo.cs   (C: dotnet-ef, 2 AddColumn nullable)
services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/
  Domain/ValidarTesoroEquipoTests.cs         (C: 6 tests dominio)
  Domain/EtapaSnapshotTests.cs               (M: 5 llamadas RegistrarTesoro += null)
  Application/ValidarTesoroEquipoHandlerTests.cs (C: 2 tests handler)
services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/
  TesoroEquipoPersistenciaTests.cs           (C: round-trip equipoid/ganadorequipoid)
contracts/http/operaciones-sesion-api.md     (M en D4: nota semántica Equipo en fila tesoro)
docs/04-sdd/traceability-matrix.md           (M en D4: fila SP-3e-3 — NUNCA stagear)
```

Sitios de construcción que NO se tocan (compilan por trailing defaults — verificado por grep en HEAD):
- `AvanzarEtapaCommandHandler.cs:38` y `BarrerTimeoutsCommandHandler.cs:77` (`EtapaBDTCerradaEvent` por tiempo/avance — sin ganador de equipo, default null semánticamente correcto).
- `FakePublisherBdtTests.cs:14/16/18`, `SignalRSesionEventsPublisherTests.cs:82/120` (construcciones posicionales existentes, el trailing default las deja compilando).
- `BdtLeafTypesTests.cs:13` (`new TesoroQR(...)` con 4 args — ctor gana 5º parámetro con default).

---

### Task D1: Dominio — ValidarTesoro con identidad dual (autor + equipo, sin dedup)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/TesoroQR.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/EtapaSnapshot.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs:231-258`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoRegistroTesoro.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/EtapaSnapshotTests.cs` (5 llamadas `RegistrarTesoro` en líneas 29/45/58/59/69: insertar `null` como 2º argumento)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ValidarTesoroEquipoTests.cs` (nuevo)

**Interfaces:**
- Consumes (existentes en HEAD, verificadas): `SesionPartida.PreinscribirEquipo(Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros, bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)`; `SesionPartida.ResponderConvocatoria(Guid convocatoriaId, Guid usuarioId, bool aceptar, bool participanteTieneParticipacionActivaEnOtra, DateTime now)`; `SesionPartida.Inscribir(Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)`; `EtapaSnapshot(Guid etapaId, int orden, string codigoQREsperado, int puntaje, int tiempoLimiteSegundos)`; `JuegoResumen(Guid juegoId, int orden, TipoJuego tipo, string areaBusqueda, IReadOnlyList<EtapaSnapshot> etapas)` (overload BDT de 5 args); `InscripcionPartida.EquipoId` (Guid), `Convocatoria.EstaAceptada`.
- Produces (para D2/D3):
  - `ResultadoRegistroTesoro(..., string? QrDecodificado, DateTime Instante, Guid? EquipoId = null, Guid? GanadorEquipoId = null)` — 2 posicionales nuevos al final.
  - `TesoroQR.EquipoId` (Guid?, property pública); ctor `TesoroQR(Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime fechaEnvio, Guid? equipoId = null)`.
  - `EtapaSnapshot.GanadorEquipoId` (Guid?, property pública); `internal RegistrarTesoro(Guid participanteId, Guid? equipoId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime now)` (equipoId posicional sin default).

- [ ] **Step 1: Escribir tests que fallan** — crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ValidarTesoroEquipoTests.cs`:

```csharp
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ValidarTesoroEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
    }

    private static byte[] Img(string texto) => System.Text.Encoding.UTF8.GetBytes(texto);

    // Sesión BDT Equipo iniciada: equipo A (líder + miembro aceptados, convocadoPendiente sin responder),
    // equipo B (solo líder aceptado). 2 etapas para observar auto-avance.
    // Nota: out params no pueden capturarse en lambdas (CS1628); copias locales, asignación al final.
    private static SesionPartida SesionBdtEquipoIniciada(
        out Guid liderA, out Guid miembroA, out Guid convocadoPendienteA, out Guid equipoA,
        out Guid liderB, out Guid equipoB)
    {
        var liderALocal = Guid.NewGuid(); var miembroALocal = Guid.NewGuid();
        var pendienteALocal = Guid.NewGuid(); var equipoALocal = Guid.NewGuid();
        var liderBLocal = Guid.NewGuid(); var equipoBLocal = Guid.NewGuid();

        var etapas = new[]
        {
            new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60),
            new EtapaSnapshot(Guid.NewGuid(), 2, "QR-2", 30, 60)
        };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio central", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var insA = sesion.PreinscribirEquipo(equipoALocal, true, new[] { liderALocal, miembroALocal, pendienteALocal }, false, 0, T0);
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == liderALocal).Id.Valor, liderALocal, true, false, T0);
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == miembroALocal).Id.Valor, miembroALocal, true, false, T0);
        // pendienteALocal NO responde su convocatoria.
        var insB = sesion.PreinscribirEquipo(equipoBLocal, true, new[] { liderBLocal }, false, 1, T0);
        sesion.ResponderConvocatoria(insB.Convocatorias.Single(c => c.UsuarioId == liderBLocal).Id.Valor, liderBLocal, true, false, T0);

        sesion.Iniciar(T0);
        liderA = liderALocal; miembroA = miembroALocal; convocadoPendienteA = pendienteALocal; equipoA = equipoALocal;
        liderB = liderBLocal; equipoB = equipoBLocal;
        return sesion;
    }

    [Fact]
    public void Miembro_aceptado_qr_valido_gana_etapa_para_el_equipo()
    {
        var sesion = SesionBdtEquipoIniciada(out _, out var miembroA, out _, out var equipoA, out _, out _);

        var r = sesion.ValidarTesoro(miembroA, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());

        Assert.Equal(ResultadoValidacionQR.Valido, r.Resultado);
        Assert.True(r.Gano);
        Assert.Equal(50, r.Puntaje);
        Assert.Equal(equipoA, r.EquipoId);
        Assert.Equal(equipoA, r.GanadorEquipoId);
        Assert.Equal(miembroA, r.ParticipanteId);
        var juego = sesion.Juegos.Single();
        var etapa1 = juego.Etapas.Single(e => e.Orden == 1);
        Assert.Equal(EstadoEtapa.Ganada, etapa1.Estado);
        Assert.Equal(miembroA, etapa1.GanadorParticipanteId);
        Assert.Equal(equipoA, etapa1.GanadorEquipoId);
        Assert.Equal(2, juego.EtapaActiva!.Orden); // cierre global + auto-avance
    }

    [Fact]
    public void Qr_invalido_no_sella_ambos_miembros_reintentan()
    {
        var sesion = SesionBdtEquipoIniciada(out var liderA, out var miembroA, out _, out var equipoA, out _, out _);

        var r1 = sesion.ValidarTesoro(liderA, Img("QR-MALO"), T0.AddSeconds(2), new TextoQrDecoder());
        Assert.False(r1.Gano);
        Assert.Equal(equipoA, r1.EquipoId);
        Assert.Null(r1.GanadorEquipoId);

        // Mismo miembro reintenta, luego otro miembro del mismo equipo — sin excepción (sin dedup).
        var r2 = sesion.ValidarTesoro(liderA, Img("QR-MALO"), T0.AddSeconds(3), new TextoQrDecoder());
        Assert.False(r2.Gano);
        var r3 = sesion.ValidarTesoro(miembroA, Img("QR-1"), T0.AddSeconds(4), new TextoQrDecoder());
        Assert.True(r3.Gano);

        var etapa1 = sesion.Juegos.Single().Etapas.Single(e => e.Orden == 1);
        Assert.Equal(3, etapa1.Tesoros.Count);
        Assert.All(etapa1.Tesoros, t => Assert.Equal(equipoA, t.EquipoId));
    }

    [Fact]
    public void Convocado_pendiente_no_puede_validar()
    {
        var sesion = SesionBdtEquipoIniciada(out _, out _, out var pendienteA, out _, out _, out _);

        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ValidarTesoro(pendienteA, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder()));
    }

    [Fact]
    public void Convocado_rechazado_no_puede_validar()
    {
        var rechazado = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var lider = Guid.NewGuid();
        var ins = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { lider, rechazado }, false, 0, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == lider).Id.Valor, lider, true, false, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == rechazado).Id.Valor, rechazado, false, false, T0);
        sesion.Iniciar(T0);

        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ValidarTesoro(rechazado, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder()));
    }

    [Fact]
    public void Qr_valido_de_equipo_A_cierra_etapa_para_equipo_B()
    {
        var sesion = SesionBdtEquipoIniciada(out var liderA, out _, out _, out var equipoA, out var liderB, out var equipoB);

        var rA = sesion.ValidarTesoro(liderA, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());
        Assert.True(rA.Gano);

        // La etapa 1 quedó Ganada para todos; B ahora valida contra la etapa 2 activa.
        var rB = sesion.ValidarTesoro(liderB, Img("QR-2"), T0.AddSeconds(10), new TextoQrDecoder());
        Assert.True(rB.Gano);
        Assert.Equal(equipoB, rB.GanadorEquipoId);
        var etapa1 = sesion.Juegos.Single().Etapas.Single(e => e.Orden == 1);
        Assert.Equal(equipoA, etapa1.GanadorEquipoId);
    }

    [Fact]
    public void Individual_regression_equipoid_null_en_todo_el_flujo()
    {
        var jugador = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        sesion.Inscribir(jugador, false, 0, T0);
        sesion.Iniciar(T0);

        var r = sesion.ValidarTesoro(jugador, Img("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());

        Assert.True(r.Gano);
        Assert.Null(r.EquipoId);
        Assert.Null(r.GanadorEquipoId);
        var etapa1 = sesion.Juegos.Single().Etapas.Single(e => e.Orden == 1);
        Assert.Null(etapa1.GanadorEquipoId);
        Assert.Null(etapa1.Tesoros.Single().EquipoId);
    }
}
```

- [ ] **Step 2: Correr para verificar RED**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~ValidarTesoroEquipoTests" 2>&1 | tail -15`
Expected: FAIL de compilación — CS1061 (`ResultadoRegistroTesoro` no contiene `EquipoId`/`GanadorEquipoId`; `EtapaSnapshot` no contiene `GanadorEquipoId`; `TesoroQR` no contiene `EquipoId`). Capturar el error.

- [ ] **Step 3: Implementar dominio**

3a. `TesoroQR.cs` — property + ctor con trailing default:

```csharp
public sealed class TesoroQR
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public string? QrDecodificado { get; private set; }
    public ResultadoValidacionQR Resultado { get; private set; }
    public DateTime FechaEnvio { get; private set; }

    private TesoroQR() { } // EF

    public TesoroQR(Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime fechaEnvio, Guid? equipoId = null)
    {
        Id = Guid.NewGuid();
        ParticipanteId = participanteId;
        EquipoId = equipoId;
        QrDecodificado = qrDecodificado;
        Resultado = resultado;
        FechaEnvio = fechaEnvio;
    }
}
```

3b. `EtapaSnapshot.cs` — property `GanadorEquipoId` (después de `GanadorParticipanteId`, línea 18):

```csharp
    public Guid? GanadorParticipanteId { get; private set; }
    public Guid? GanadorEquipoId { get; private set; }
```

y `RegistrarTesoro` con `equipoId` posicional (sin default):

```csharp
    internal (bool CerroEtapa, bool Gano, int? Puntaje, long? TiempoResolucionMs) RegistrarTesoro(
        Guid participanteId, Guid? equipoId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime now)
    {
        if (Estado != EstadoEtapa.Activa)
            throw new InvalidOperationException($"La etapa {EtapaId} no está activa.");

        _tesoros.Add(new TesoroQR(participanteId, qrDecodificado, resultado, now, equipoId));

        var dentroDeVentana = now < FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos);
        if (resultado == ResultadoValidacionQR.Valido && dentroDeVentana)
        {
            var tiempoMs = (long)(now - FechaActivacion!.Value).TotalMilliseconds;
            Estado = EstadoEtapa.Ganada;
            FechaCierre = now;
            MotivoCierre = MotivoCierreEtapa.Ganador;
            GanadorParticipanteId = participanteId;
            GanadorEquipoId = equipoId;
            TiempoResolucionMs = tiempoMs;
            return (true, true, Puntaje, tiempoMs);
        }
        return (false, false, null, null);
    }
```

3c. `ResultadoRegistroTesoro.cs` — 2 posicionales trailing con default:

```csharp
public sealed record ResultadoRegistroTesoro(
    ResultadoValidacionQR Resultado,
    bool CerroEtapa,
    bool Gano,
    int? Puntaje,
    Guid JuegoId,
    Guid EtapaId,
    Guid ParticipanteId,
    Guid? GanadorParticipanteId,
    long? TiempoResolucionMs,
    string? QrDecodificado,
    DateTime Instante,
    Guid? EquipoId = null,
    Guid? GanadorEquipoId = null);
```

3d. `SesionPartida.ValidarTesoro` (líneas 231-258) — resolución dual espejo VERBATIM de `ResponderPregunta` (línea 191) + propagación:

```csharp
    public ResultadoRegistroTesoro ValidarTesoro(Guid participanteId, byte[] imagen, DateTime now, Umbral.OperacionesSesion.Domain.Abstractions.IQrDecoder decoder)
    {
        Guid? equipoId = null;
        if (Modalidad == Modalidad.Equipo)
        {
            var inscripcion = _inscripciones.FirstOrDefault(i => i.EsActiva
                    && i.Convocatorias.Any(c => c.UsuarioId == participanteId && c.EstaAceptada))
                ?? throw new ParticipanteNoInscritoException(participanteId);
            equipoId = inscripcion.EquipoId;
        }
        else if (!_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
        {
            throw new ParticipanteNoInscritoException(participanteId);
        }
        var juego = JuegoBDTActivo();
        var activa = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);

        var texto = decoder.Decodificar(imagen);
        var resultado = ClasificarQr(texto, activa, juego);

        var reg = activa.RegistrarTesoro(participanteId, equipoId, texto, resultado, now);

        if (reg.Gano)
        {
            juego.ActivarSiguienteEtapa(now);
        }
        else if (now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos))
        {
            activa.CerrarPorTiempo(now);
            juego.ActivarSiguienteEtapa(now);
        }

        return new ResultadoRegistroTesoro(
            resultado, reg.CerroEtapa || (!reg.Gano && activa.Estado == EstadoEtapa.CerradaPorTiempo),
            reg.Gano, reg.Puntaje, juego.JuegoId, activa.EtapaId, participanteId,
            reg.Gano ? participanteId : null, reg.TiempoResolucionMs, texto, now,
            equipoId, reg.Gano ? equipoId : null);
    }
```

3e. `EtapaSnapshotTests.cs` — insertar `null` como 2º argumento en las 5 llamadas existentes (líneas 29, 45, 58, 59, 69), p. ej. `e.RegistrarTesoro(participante, null, "QR-1", ResultadoValidacionQR.Valido, t0.AddSeconds(5))`. Sin tocar asserts.

- [ ] **Step 4: Correr filtro nuevo y verificar GREEN**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~ValidarTesoroEquipoTests" 2>&1 | tail -5`
Expected: PASS 6/6.

- [ ] **Step 5: Búsqueda repo-wide de construction sites rotos (lección B13/C1)**

Run: `grep -rn "RegistrarTesoro(\|new TesoroQR(\|new ResultadoRegistroTesoro(" services/operaciones-sesion/src services/operaciones-sesion/tests --include="*.cs" | grep -v "internal (\|public sealed record"`
Expected: los únicos call sites son `SesionPartida.cs` (actualizado), `EtapaSnapshot.cs:48` (actualizado), `EtapaSnapshotTests.cs` ×5 (actualizados con null), `BdtLeafTypesTests.cs:13` (compila por default — NO tocar). Si aparece otro sitio, actualizarlo mínimamente y disclosear.

- [ ] **Step 6: Suites completas**

Run: ambas suites de Global Constraints.
Expected: Unit 306/306 (300 + 6 nuevos), Integration 27/27.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/TesoroQR.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/EtapaSnapshot.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoRegistroTesoro.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ValidarTesoroEquipoTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/EtapaSnapshotTests.cs
git commit -m "SP-3e-3 D1: dominio tesoro BDT Equipo (identidad dual, reintentos libres, ganador equipo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task D2: Aplicación — eventos BDT con EquipoId

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ValidarTesoroCommandHandler.cs:44-62`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ValidarTesoroEquipoHandlerTests.cs` (nuevo)

**Interfaces:**
- Consumes (de D1): `ResultadoRegistroTesoro.EquipoId` / `.GanadorEquipoId` (Guid?).
- Consumes (existentes): `FakeSesionPartidaRepository.Add(SesionPartida)`, `FakeOperacionesSesionUnitOfWork`, `FakeSesionEventsPublisher` (colecciones `TesorosValidados`, `EtapasGanadas`, `EtapasCerradas`, `EtapasActivadas`), `FakeTimeProvider(DateTime)`, `ValidarTesoroCommand(Guid PartidaId, Guid ParticipanteId, string ImagenBase64)`.
- Produces (para D4/SP-4): `TesoroQRValidadoEvent(..., DateTime Instante, Guid? EquipoId = null)`; `EtapaBDTGanadaEvent(..., long TiempoResolucionMs, Guid? EquipoId = null)`; `EtapaBDTCerradaEvent(..., Guid? GanadorParticipanteId, Guid? GanadorEquipoId = null)`.

- [ ] **Step 1: Escribir tests que fallan** — crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ValidarTesoroEquipoHandlerTests.cs`:

```csharp
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ValidarTesoroEquipoHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : Encoding.UTF8.GetString(imagen);
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));

    private static SesionPartida SesionBdtEquipoIniciada(out Guid liderA, out Guid equipoA)
    {
        var liderALocal = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoALocal, true, new[] { liderALocal }, false, 0, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == liderALocal).Id.Valor, liderALocal, true, false, T0);
        sesion.Iniciar(T0);
        liderA = liderALocal; equipoA = equipoALocal;
        return sesion;
    }

    [Fact]
    public async Task En_equipo_los_eventos_bdt_portan_el_equipo()
    {
        var sesion = SesionBdtEquipoIniciada(out var liderA, out var equipoA);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ValidarTesoroCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)), new TextoQrDecoder());

        await handler.Handle(new ValidarTesoroCommand(sesion.PartidaId, liderA, B64("QR-1")), CancellationToken.None);

        var validado = events.TesorosValidados.Single();
        Assert.Equal(equipoA, validado.EquipoId);
        Assert.Equal(liderA, validado.ParticipanteId);
        var ganada = events.EtapasGanadas.Single();
        Assert.Equal(equipoA, ganada.EquipoId);
        var cerrada = events.EtapasCerradas.Single();
        Assert.Equal(equipoA, cerrada.GanadorEquipoId);
        Assert.Equal(liderA, cerrada.GanadorParticipanteId);
    }

    [Fact]
    public async Task En_individual_los_eventos_bdt_llevan_equipo_null()
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
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ValidarTesoroCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)), new TextoQrDecoder());

        await handler.Handle(new ValidarTesoroCommand(sesion.PartidaId, jugador, B64("QR-1")), CancellationToken.None);

        Assert.Null(events.TesorosValidados.Single().EquipoId);
        Assert.Null(events.EtapasGanadas.Single().EquipoId);
        Assert.Null(events.EtapasCerradas.Single().GanadorEquipoId);
    }
}
```

- [ ] **Step 2: Correr para verificar RED**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~ValidarTesoroEquipoHandlerTests" 2>&1 | tail -10`
Expected: FAIL de compilación — CS1061 (`TesoroQRValidadoEvent` no contiene `EquipoId`, etc.).

- [ ] **Step 3: Extender los 3 event records** — `BdtRuntimeEvents.cs` (records `PistaEnviadaEvent` y `EtapaBDTActivadaEvent` NO se tocan):

```csharp
public sealed record TesoroQRValidadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    Guid ParticipanteId, string Resultado, DateTime Instante, Guid? EquipoId = null);

public sealed record EtapaBDTGanadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    Guid ParticipanteId, int Puntaje, long TiempoResolucionMs, Guid? EquipoId = null);

public sealed record EtapaBDTCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid EtapaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId, Guid? GanadorEquipoId = null);
```

- [ ] **Step 4: Propagar en `ValidarTesoroCommandHandler`** — 3 construcciones (la 4ª, rama "Tiempo" en línea ~69, NO se toca — default null correcto):

```csharp
        await _events.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(
                sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                r.ParticipanteId, r.Resultado.ToString(), r.Instante, r.EquipoId),
            cancellationToken);

        if (r.Gano)
        {
            await _events.PublicarEtapaBDTGanadaAsync(
                new EtapaBDTGanadaEvent(
                    sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    r.ParticipanteId, r.Puntaje!.Value, r.TiempoResolucionMs!.Value, r.EquipoId),
                cancellationToken);
            await _events.PublicarEtapaBDTCerradaAsync(
                new EtapaBDTCerradaEvent(
                    sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaId,
                    "Ganador", now, r.GanadorParticipanteId, r.GanadorEquipoId),
                cancellationToken);
```

- [ ] **Step 5: Correr filtro nuevo y verificar GREEN**

Run: mismo filtro del Step 2.
Expected: PASS 2/2.

- [ ] **Step 6: Verificar construction sites no tocados**

Run: `grep -rn "TesoroQRValidadoEvent(\|EtapaBDTGanadaEvent(\|EtapaBDTCerradaEvent(" services/operaciones-sesion/src services/operaciones-sesion/tests --include="*.cs" | grep -v "record "`
Expected: `ValidarTesoroCommandHandler` (3 sitios con propagación + 1 rama Tiempo sin tocar), `AvanzarEtapaCommandHandler.cs:38`, `BarrerTimeoutsCommandHandler.cs:77`, `FakePublisherBdtTests.cs`, `SignalRSesionEventsPublisherTests.cs` — estos últimos 4 SIN cambios (compilan por defaults).

- [ ] **Step 7: Suites completas**

Expected: Unit 308/308 (306 + 2), Integration 27/27.

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ValidarTesoroCommandHandler.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ValidarTesoroEquipoHandlerTests.cs
git commit -m "SP-3e-3 D2: eventos BDT con EquipoId (Validado/Ganada/Cerrada, default null en Individual)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task D3: EF — columnas nuevas + migración SP3e3RuntimeBdtEquipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs` (bloques `EtapaSnapshot` y `TesoroQR`)
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3e3RuntimeBdtEquipo.cs` (+ `.Designer.cs`, y snapshot actualizado) — vía `dotnet ef`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/TesoroEquipoPersistenciaTests.cs` (nuevo)

**Interfaces:**
- Consumes (de D1): `EtapaSnapshot.GanadorEquipoId`, `TesoroQR.EquipoId`, `SesionPartida.ValidarTesoro` dual.
- Produces: columnas `etapas_snapshot.ganadorequipoid` y `tesoros_qr.equipoid` (uuid, nullable) en Npgsql.

- [ ] **Step 1: Escribir test round-trip** — crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/TesoroEquipoPersistenciaTests.cs` (patrón write/read contexts separados, espejo de `RespuestaEquipoPersistenciaTests.cs`):

```csharp
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Abstractions;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class TesoroEquipoPersistenciaTests
{
    private static readonly DateTime T0 = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TextoQrDecoder : IQrDecoder
    {
        public string? Decodificar(byte[] imagen) =>
            imagen.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(imagen);
    }

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    [Fact]
    public async Task Tesoro_de_equipo_persiste_equipoid_y_ganadorequipoid()
    {
        var db = "tesoro-eq-" + Guid.NewGuid();
        var lider = Guid.NewGuid(); var equipo = Guid.NewGuid();
        Guid partidaId;

        await using (var ctx = NewCtx(db))
        {
            var etapas = new[] { new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 60) };
            var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Patio", etapas);
            var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
            var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
            partidaId = sesion.PartidaId;
            var ins = sesion.PreinscribirEquipo(equipo, true, new[] { lider }, false, 0, T0);
            sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
            sesion.Iniciar(T0);
            sesion.ValidarTesoro(lider, System.Text.Encoding.UTF8.GetBytes("QR-1"), T0.AddSeconds(5), new TextoQrDecoder());
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetByPartidaIdAsync(partidaId, default);
            var etapa = r!.Juegos.Single().Etapas.Single(e => e.Orden == 1);
            Assert.Equal(equipo, etapa.GanadorEquipoId);
            Assert.Equal(equipo, etapa.Tesoros.Single().EquipoId);
        }
    }
}
```

- [ ] **Step 2: Correr para verificar estado** — puede pasar by-convention bajo InMemory (como pasó en C4; NO es fallo — el mapping explícito es para los nombres de columna reales en Npgsql). Capturar resultado.

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj --filter "FullyQualifiedName~TesoroEquipoPersistenciaTests" 2>&1 | tail -5`

- [ ] **Step 3: Mapping explícito** — `OperacionesSesionDbContext.cs`; en el bloque `EtapaSnapshot`, tras la línea `entity.Property(x => x.GanadorParticipanteId).HasColumnName("ganadorparticipanteid");`:

```csharp
            entity.Property(x => x.GanadorEquipoId).HasColumnName("ganadorequipoid");
```

y en el bloque `TesoroQR`, tras `entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();`:

```csharp
            entity.Property(x => x.EquipoId).HasColumnName("equipoid");
```

- [ ] **Step 4: Generar migración**

Run (desde repo root):
```bash
dotnet ef migrations add SP3e3RuntimeBdtEquipo \
  --project services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project services/operaciones-sesion/src/Umbral.OperacionesSesion.Api
```
Expected `Up()` (verificar que sea EXACTAMENTE esto — 2 AddColumn nullable, nada más):

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "equipoid",
                table: "tesoros_qr",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ganadorequipoid",
                table: "etapas_snapshot",
                type: "uuid",
                nullable: true);
        }
```
y `Down()` con los 2 `DropColumn` espejo. Si `dotnet ef` no está disponible o el output difiere, escribir la migración a mano con ese contenido + Designer/snapshot coherentes (patrón C4: Designer ≡ snapshot en las columnas nuevas).

- [ ] **Step 5: Suites completas**

Expected: Unit 308/308, Integration 28/28 (27 + 1).

- [ ] **Step 6: Commit** (stagear los archivos de migración generados por su path exacto — listar con `git status --short` antes):

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs "services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3e3RuntimeBdtEquipo.cs" "services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3e3RuntimeBdtEquipo.Designer.cs" services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/OperacionesSesionDbContextModelSnapshot.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/TesoroEquipoPersistenciaTests.cs
git commit -m "SP-3e-3 D3: EF equipoid/ganadorequipoid BDT + migración SP3e3RuntimeBdtEquipo (additiva)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task D4: Contrato HTTP + traceability (carve-out)

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md` (STAGE + COMMIT)
- Modify: `docs/04-sdd/traceability-matrix.md` (EDITAR, NUNCA stagear ni commitear)

**Interfaces:**
- Consumes: todo D1-D3 (verificar cada claim contra código antes de escribirlo).
- Produces: contrato actualizado; ContractTests verdes.

- [ ] **Step 1: Actualizar contrato** — en `contracts/http/operaciones-sesion-api.md`, sobre la fila/sección existente de `POST /operaciones-sesion/partidas/{partidaId}/etapa-actual/tesoro`, añadir nota de semántica Equipo (redacción consistente con la nota Equipo que C8 añadió a pregunta-actual/respuesta):
  - En modalidad `Equipo`, el emisor debe tener convocatoria **Aceptada** en la inscripción activa de su equipo; sin ella → `403` (mismo código actual). Reintentos ilimitados (sin 409 de duplicado — a diferencia de Trivia). Una validación correcta de cualquier miembro gana la etapa para todo el equipo.
  - Verificar contra código: guard dual en `SesionPartida.ValidarTesoro` (D1), sin dedup en `EtapaSnapshot.RegistrarTesoro`, mapping 403 de `ParticipanteNoInscritoException` en `ExceptionHandlingMiddleware.MapStatus`.
  - Si el doc lista los eventos del seam, añadir `equipoId?`/`ganadorEquipoId?` donde el doc ya describa `TesoroQRValidado`/`EtapaBDTGanada`/`EtapaBDTCerrada`; si no los lista, no inventar sección nueva.

- [ ] **Step 2: Editar traceability (SIN stagear)** — añadir fila SP-3e-3 en `docs/04-sdd/traceability-matrix.md` siguiendo el formato de la fila SP-3e-2 (spec, plan, commits D1-D3, archivos clave, conteos de tests reales).

- [ ] **Step 3: Las 3 suites completas**

Run: Unit + Integration + Contract (`dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`).
Expected: Unit 308/308, Integration 28/28, Contract 48/48 (si algún ContractTest de paridad doc↔constantes falla por la edición, ajustar la edición — no el test).

- [ ] **Step 4: Commit (SOLO el contrato)**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "SP-3e-3 D4: contrato — semántica Equipo en etapa-actual/tesoro (403 sin convocatoria aceptada, reintentos libres)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git status --short   # verificar traceability-matrix.md sigue " M" unstaged
```

---

## Self-Review (ejecutado al escribir el plan)

1. **Spec coverage:** §2 reglas → D1 (dual, sin dedup, ganador dual, Pendiente/Rechazado 403, cierre global, Individual regression); §3.2 eventos → D2; §3.3 HTTP → D4; §3.4 EF → D3; §3.5 proyecciones → sin cambios (ningún task — correcto); §4 testing → tests en D1/D2/D3; minor C1 heredado (Rechazado) → test explícito en D1. Sin gaps.
2. **Placeholders:** ninguno — todo step con código o comando completo; único `<timestamp>` es el prefijo que genera dotnet-ef (inevitable, instrucción de listar con git status).
3. **Consistencia de tipos:** `RegistrarTesoro(participanteId, equipoId, qrDecodificado, resultado, now)` usada igual en D1 código y D1 tests; `ResultadoRegistroTesoro` 13 posicionales consistente entre 3c y 3d; eventos D2 con nombres/orden verificados contra `BdtRuntimeEvents.cs` en HEAD; helpers de test usan firmas verificadas (`PreinscribirEquipo` 6 args, `ResponderConvocatoria` 5 args, `Inscribir` 4 args, `JuegoResumen` BDT 5 args, `EtapaSnapshot` 5 args).
4. **Firmas verificadas contra HEAD `986861c`:** `ValidarTesoro(Guid, byte[], DateTime, IQrDecoder)` (SesionPartida.cs:231); resolución dual copiada VERBATIM de `ResponderPregunta` (SesionPartida.cs:191); fakes (`TesorosValidados`/`EtapasGanadas`/`EtapasCerradas`/`EtapasActivadas`) de FakeSesionEventsPublisher.cs:62-65; DbContext bloques etapas_snapshot/tesoros_qr líneas 68-97.
