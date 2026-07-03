# SP-3e-2 Runtime Trivia Equipo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Habilitar responder Trivia en modalidad Equipo (1 respuesta por equipo, identidad dual autor+equipo), cerrar el hueco BR-G09 intra-partida (double-accept) y exponer el inbox de convocatorias pendientes.

**Architecture:** Un solo servicio (Operaciones de Sesión). Enfoque "identidad dual": el autor real viaja en `ParticipanteId`, el equipo en `Guid? EquipoId` nuevo (null ⇔ Individual) a través de dominio → resultado → eventos → persistencia → proyección. Sin flujos paralelos, sin eventos nuevos, sin cambios de payloads SignalR (los payloads actuales no portan identidad; `RespuestaTriviaValidada`/`PuntajeTriviaIncrementado` no difunden per SP-3f-2). El inbox es un read puro nuevo (repo scan + query + GET).

**Tech Stack:** .NET 8, EF Core 8 (Npgsql runtime / InMemory tests), MediatR, xUnit con fakes a mano (NO Moq), SignalR (sin cambios).

**Spec:** `docs/superpowers/specs/2026-07-01-sp3e2-runtime-trivia-equipo-design.md`

## Global Constraints

- Servicio: `services/operaciones-sesion/` únicamente. Clean Architecture: Domain → Application → Infrastructure → Api; interfaces de repo en Domain, impl en Infrastructure.
- Regla de negocio: **una respuesta por equipo por pregunta** — la primera respuesta de CUALQUIER miembro activo (convocatoria Aceptada) sella al equipo (correcta o no); los demás → `RespuestaDuplicadaException` (409). Miembro Pendiente/Rechazado → `ParticipanteNoInscritoException` (403). Cierre global sin cambios (primera correcta cierra para todos; RF-22 auto-activa siguiente).
- Individual NO cambia de comportamiento: todo campo nuevo es `Guid?` con default `null`; los records extendidos usan parámetros con default para no romper construction sites.
- Suites completas antes de cada commit:
  `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
  `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
  (y ContractTests en C8). Verdes, output limpio.
- **Git (HARD):** PROHIBIDO `git checkout/restore/clean/stash/reset/rebase` en cualquier forma. NUNCA `git add -A`, `git add .`, `git add docs/` ni adds sin path exacto — stagear SOLO los archivos nombrados. Estos quedan SIEMPRE unstaged/uncommitted: `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. Mensaje de commit: línea resumen `SP-3e-2 CN: ...`, línea en blanco, y EXACTAMENTE este trailer final (nada después):
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- Shell zsh: comillas en globs (`--include="*.cs"`); rutas absolutas desde `/home/santiago/Escritorio/ProyectoDesarrollo`.

## File Structure (mapa completo del slice)

```
services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/
  Entities/RespuestaTrivia.cs                (M: += EquipoId?)
  Entities/PreguntaSnapshot.cs               (M: GanadorEquipoId? + RegistrarRespuesta dual + Cerrar)
  Entities/SesionPartida.cs                  (M: ResponderPregunta resolución dual; ResponderConvocatoria guard)
  Results/ResultadoRespuesta.cs              (M: += EquipoId? default null)
  Abstractions/Persistence/ISesionPartidaRepository.cs (M: += GetConvocatoriasPendientesByUsuarioAsync)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/
  Interfaces/TriviaRuntimeEvents.cs          (M: 3 records += Guid? con default)
  Handlers/Commands/ResponderPreguntaCommandHandler.cs (M: propaga EquipoId)
  Handlers/Queries/ObtenerMiSesionQueryHandler.cs      (M: yaRespondio por equipo)
  Handlers/Queries/ObtenerMisConvocatoriasPendientesQueryHandler.cs (C)
  Queries/ObtenerMisConvocatoriasPendientesQuery.cs    (C)
  DTOs/ConvocatoriaPendienteDto.cs           (C)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/
  Persistence/OperacionesSesionDbContext.cs  (M: 2 columnas nuevas)
  Persistence/SesionPartidaRepository.cs     (M: += inbox scan)
  Persistence/Migrations/*SP3e2RuntimeTriviaEquipo* (C: generada)
services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/
  Controllers/SesionesController.cs          (M: GET mis-convocatorias)
services/operaciones-sesion/tests/
  UnitTests/Domain/ResponderPreguntaEquipoTests.cs     (C)
  UnitTests/Domain/ResponderConvocatoriaGuardTests.cs  (C)
  UnitTests/Application/ResponderPreguntaEquipoHandlerTests.cs (C)
  UnitTests/Application/ObtenerMisConvocatoriasPendientesQueryHandlerTests.cs (C)
  UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs (M: += método inbox)
  UnitTests/Api/SesionesControllerMisConvocatoriasTests.cs (C)
  IntegrationTests/RespuestaEquipoPersistenciaTests.cs (C)
  IntegrationTests/ConvocatoriasPendientesScanTests.cs (C)
contracts/http/operaciones-sesion-api.md     (M, en C8)
docs/04-sdd/traceability-matrix.md           (M en C8, NUNCA staged — carve-out)
```

Firmas actuales relevantes (verificadas contra HEAD `9a4e546`):

```csharp
// Domain/Results/ResultadoRespuesta.cs (actual)
public sealed record ResultadoRespuesta(
    bool EsCorrecta, bool CerroPregunta, int? Puntaje, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, Guid OpcionId, DateTime Instante, long TiempoRespuestaMs);

// Application/Interfaces/TriviaRuntimeEvents.cs (actual)
public sealed record RespuestaTriviaValidadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, Guid OpcionId, bool EsCorrecta, DateTime Instante);
public sealed record PuntajeTriviaIncrementadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs);
public sealed record PreguntaTriviaCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId);

// SesionPartida (actual): Modalidad property existe; ResponderPregunta en :187;
// ResponderConvocatoria en :101 (guard Lobby, localiza Pendiente del usuario, rama aceptar).
// InscripcionPartida: EsActiva, EquipoId (Guid?), Convocatorias (IReadOnlyList<Convocatoria>).
// Convocatoria: Id (ConvocatoriaId VO, .Valor), PartidaId, EquipoId, UsuarioId, Estado,
//   FechaEnvio, EstaAceptada, EstaPendiente.
```

Helper de tests de dominio existente (patrón en `SesionPartidaRepositoryScansTests.cs` y `ProyeccionesEquipoTests.cs`): construir `SesionPartida.Publicar(partidaId, ConfiguracionSnapshot(...))` con `JuegoResumen(id, 1, TipoJuego.Trivia, preguntas)` y `PreguntaSnapshot(id, orden, texto, puntaje, límite, opciones)`.

---

### Task C1: Dominio — respuesta Trivia con identidad dual (autor + equipo)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/RespuestaTrivia.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoRespuesta.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/PreguntaSnapshot.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs` (solo `ResponderPregunta`, línea ~187)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ResponderPreguntaEquipoTests.cs` (create)

**Interfaces:**
- Consumes: `InscripcionPartida.EsActiva/EquipoId/Convocatorias`, `Convocatoria.UsuarioId/EstaAceptada` (SP-3e-1).
- Produces: `ResultadoRespuesta.EquipoId` (`Guid?`, default `null`); `RespuestaTrivia.EquipoId` (`Guid?`); `PreguntaSnapshot.GanadorEquipoId` (`Guid?`); `RegistrarRespuesta(Guid participanteId, Guid? equipoId, Guid opcionId, DateTime now)`; `Cerrar(MotivoCierrePregunta, DateTime, Guid? ganador, Guid? ganadorEquipo = null)`. C3 lee `r.EquipoId`; C4 mapea los 2 campos; C5 lee `RespuestaTrivia.EquipoId`.

- [ ] **Step 1: Escribir tests de dominio que fallan**

Crear `ResponderPreguntaEquipoTests.cs`:

```csharp
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ResponderPreguntaEquipoTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid OpcionOk = Guid.NewGuid();
    private static readonly Guid OpcionMal = Guid.NewGuid();

    private static SesionPartida SesionEquipoIniciada(
        out Guid liderA, out Guid miembroA, out Guid equipoA,
        out Guid liderB, out Guid equipoB)
    {
        liderA = Guid.NewGuid(); miembroA = Guid.NewGuid(); equipoA = Guid.NewGuid();
        liderB = Guid.NewGuid(); equipoB = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(OpcionOk, "ok", true), new OpcionSnapshot(OpcionMal, "no", false) });
        var pregunta2 = new PreguntaSnapshot(Guid.NewGuid(), 2, "Q2", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta, pregunta2 });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var insA = sesion.PreinscribirEquipo(equipoA, true, new[] { liderA, miembroA }, false, 0, T0);
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == liderA).Id.Valor, liderA, true, false, T0);
        sesion.ResponderConvocatoria(insA.Convocatorias.Single(c => c.UsuarioId == miembroA).Id.Valor, miembroA, true, false, T0);
        var insB = sesion.PreinscribirEquipo(equipoB, true, new[] { liderB }, false, 1, T0);
        sesion.ResponderConvocatoria(insB.Convocatorias.Single(c => c.UsuarioId == liderB).Id.Valor, liderB, true, false, T0);

        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public void Miembro_aceptado_responde_correcta_cierra_y_gana_el_equipo()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out _, out var equipoA, out _, out _);

        var r = sesion.ResponderPregunta(liderA, OpcionOk, T0.AddSeconds(5));

        Assert.True(r.EsCorrecta);
        Assert.True(r.CerroPregunta);
        Assert.Equal(equipoA, r.EquipoId);
        Assert.Equal(liderA, r.ParticipanteId);
        var pregunta = sesion.Juegos.Single().Preguntas.Single(p => p.Orden == 1);
        Assert.Equal(equipoA, pregunta.GanadorEquipoId);
        Assert.Equal(liderA, pregunta.GanadorParticipanteId);
    }

    [Fact]
    public void Respuesta_incorrecta_sella_al_equipo_entero()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out var miembroA, out var equipoA, out _, out _);

        var r1 = sesion.ResponderPregunta(liderA, OpcionMal, T0.AddSeconds(5));
        Assert.False(r1.EsCorrecta);
        Assert.Equal(equipoA, r1.EquipoId);

        Assert.Throws<RespuestaDuplicadaException>(
            () => sesion.ResponderPregunta(miembroA, OpcionOk, T0.AddSeconds(6)));
    }

    [Fact]
    public void Otro_equipo_puede_responder_tras_respuesta_incorrecta_del_primero()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out _, out _, out var liderB, out var equipoB);

        sesion.ResponderPregunta(liderA, OpcionMal, T0.AddSeconds(5));
        var r = sesion.ResponderPregunta(liderB, OpcionOk, T0.AddSeconds(6));

        Assert.True(r.CerroPregunta);
        Assert.Equal(equipoB, r.EquipoId);
    }

    [Fact]
    public void Convocado_pendiente_no_puede_responder()
    {
        // equipo B con 2 miembros: líder acepta, el otro queda Pendiente
        var liderB = Guid.NewGuid(); var pendiente = Guid.NewGuid(); var equipoB = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(OpcionOk, "ok", true), new OpcionSnapshot(OpcionMal, "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var ins = sesion.PreinscribirEquipo(equipoB, true, new[] { liderB, pendiente }, false, 0, T0);
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == liderB).Id.Valor, liderB, true, false, T0);
        sesion.Iniciar(T0);

        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ResponderPregunta(pendiente, OpcionOk, T0.AddSeconds(5)));
    }

    [Fact]
    public void Individual_sigue_registrando_sin_equipo()
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(OpcionOk, "ok", true), new OpcionSnapshot(OpcionMal, "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var p = Guid.NewGuid();
        sesion.Inscribir(p, false, 0, T0);
        sesion.Iniciar(T0);

        var r = sesion.ResponderPregunta(p, OpcionOk, T0.AddSeconds(5));

        Assert.Null(r.EquipoId);
        Assert.Null(sesion.Juegos.Single().Preguntas.Single().GanadorEquipoId);
    }
}
```

Nota: firmas verificadas contra HEAD `9a4e546`: `PreinscribirEquipo(Guid equipoId, bool callerEsLider, IReadOnlyList<Guid> miembros, bool equipoTieneParticipacionActivaEnOtra, int equiposActivos, DateTime fecha)` e `Inscribir(Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)`. Si al compilar difieren, adaptar el helper del test, NUNCA la firma de producción.

- [ ] **Step 2: Correr los tests — deben FALLAR compilando**

```
dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter ResponderPreguntaEquipoTests
```
Esperado: error de compilación (`ResultadoRespuesta` no tiene `EquipoId`, `RegistrarRespuesta` no acepta equipo). Capturar output (RED).

- [ ] **Step 3: Implementar dominio**

`ResultadoRespuesta.cs` — parámetro nuevo con default (no rompe sitios existentes):

```csharp
public sealed record ResultadoRespuesta(
    bool EsCorrecta,
    bool CerroPregunta,
    int? Puntaje,
    Guid JuegoId,
    Guid PreguntaId,
    Guid ParticipanteId,
    Guid OpcionId,
    DateTime Instante,
    long TiempoRespuestaMs,
    Guid? EquipoId = null);
```

`RespuestaTrivia.cs` — propiedad + parámetro opcional al final del ctor:

```csharp
public Guid? EquipoId { get; private set; }

public RespuestaTrivia(Guid participanteId, Guid opcionId, bool esCorrecta, DateTime instante, Guid? equipoId = null)
{
    Id = Guid.NewGuid();
    ParticipanteId = participanteId;
    OpcionId = opcionId;
    EsCorrecta = esCorrecta;
    Instante = instante;
    EquipoId = equipoId;
}
```

`PreguntaSnapshot.cs` — propiedad `GanadorEquipoId`, `Cerrar` con parámetro default (los llamadores existentes `Cerrar(motivo, now, ganador: null)` en `SesionPartida.AvanzarPregunta`/`CerrarActividadVencida` compilan sin cambios), `RegistrarRespuesta` dual:

```csharp
public Guid? GanadorEquipoId { get; private set; }

internal void Cerrar(MotivoCierrePregunta motivo, DateTime now, Guid? ganador, Guid? ganadorEquipo = null)
{
    if (Estado != EstadoPregunta.Activa)
        throw new InvalidOperationException($"La pregunta {PreguntaId} no está activa.");
    Estado = EstadoPregunta.Cerrada;
    FechaCierre = now;
    MotivoCierre = motivo;
    GanadorParticipanteId = ganador;
    GanadorEquipoId = ganadorEquipo;
}

internal ResultadoRespuesta RegistrarRespuesta(Guid participanteId, Guid? equipoId, Guid opcionId, DateTime now)
{
    if (Estado != EstadoPregunta.Activa)
        throw new InvalidOperationException($"La pregunta {PreguntaId} no está activa.");
    var duplicada = equipoId is null
        ? _respuestas.Any(r => r.ParticipanteId == participanteId)
        : _respuestas.Any(r => r.EquipoId == equipoId);
    if (duplicada)
        throw new RespuestaDuplicadaException(participanteId);
    if (now > FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos))
        throw new PreguntaFueraDeTiempoException(PreguntaId);

    var esCorrecta = _opciones.Any(o => o.OpcionId == opcionId && o.EsCorrecta);
    _respuestas.Add(new RespuestaTrivia(participanteId, opcionId, esCorrecta, now, equipoId));

    var cerro = false;
    if (esCorrecta)
    {
        Cerrar(MotivoCierrePregunta.RespuestaCorrecta, now, participanteId, equipoId);
        cerro = true;
    }

    var tiempoMs = (long)(now - FechaActivacion!.Value).TotalMilliseconds;
    return new ResultadoRespuesta(esCorrecta, cerro, esCorrecta ? PuntajeAsignado : null,
        Guid.Empty, PreguntaId, participanteId, opcionId, now, tiempoMs, equipoId);
}
```

`SesionPartida.ResponderPregunta` — resolución dual (reemplaza el guard actual):

```csharp
public ResultadoRespuesta ResponderPregunta(Guid participanteId, Guid opcionId, DateTime now)
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

    var juego = JuegoTriviaActivo();
    var activa = juego.PreguntaActiva ?? throw new NoHayPreguntaActivaException(PartidaId);

    var resultado = activa.RegistrarRespuesta(participanteId, equipoId, opcionId, now) with { JuegoId = juego.JuegoId };
    if (resultado.CerroPregunta)
        juego.ActivarSiguientePregunta(now); // RF-22: al cerrar por acierto, auto-activar la siguiente
    return resultado;
}
```

Buscar TODOS los construction sites de `ResultadoRespuesta` y llamadores de `RegistrarRespuesta`/`Cerrar` repo-wide antes de compilar (lección B13):
```
grep -rn "RegistrarRespuesta\|new ResultadoRespuesta\|\.Cerrar(" services/operaciones-sesion/ --include="*.cs"
```
Con los defaults de arriba no debería requerirse tocar ninguno fuera de los archivos listados; si un test existente construye `ResultadoRespuesta` posicionalmente con 9 args, compila igual (el 10º tiene default).

- [ ] **Step 4: Correr los tests nuevos — PASS (GREEN)**

```
dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter ResponderPreguntaEquipoTests
```
Esperado: 5/5 PASS. Capturar output.

- [ ] **Step 5: Suites completas + commit**

Correr ambas suites completas (Global Constraints). Esperado: todo verde (los tests Individual existentes de trivia no cambian de comportamiento).

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/RespuestaTrivia.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoRespuesta.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/PreguntaSnapshot.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ResponderPreguntaEquipoTests.cs
git commit -m "SP-3e-2 C1: dominio respuesta Trivia Equipo (identidad dual, 1 respuesta por equipo, ganador equipo)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task C2: Dominio — guard double-accept intra-partida (BR-G09)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs` (solo `ResponderConvocatoria`, línea ~101)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ResponderConvocatoriaGuardTests.cs` (create)

**Interfaces:**
- Consumes: `ResponderConvocatoria(convocatoriaId, usuarioId, aceptar, participanteTieneParticipacionActivaEnOtra, now)` (SP-3e-1); `ParticipacionActivaExistenteException` (existente, ya mapeada a 409 en el middleware).
- Produces: mismo método, ahora rechaza aceptar si el usuario ya tiene otra convocatoria Aceptada en ESTA sesión.

- [ ] **Step 1: Test que falla**

```csharp
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ResponderConvocatoriaGuardTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionEquipoLobbyConUsuarioEnDosEquipos(
        Guid usuario, out Guid convocatoriaA, out Guid convocatoriaB)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var liderA = Guid.NewGuid(); var liderB = Guid.NewGuid();
        var insA = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { liderA, usuario }, false, 0, T0);
        var insB = sesion.PreinscribirEquipo(Guid.NewGuid(), true, new[] { liderB, usuario }, false, 1, T0);
        convocatoriaA = insA.Convocatorias.Single(c => c.UsuarioId == usuario).Id.Valor;
        convocatoriaB = insB.Convocatorias.Single(c => c.UsuarioId == usuario).Id.Valor;
        return sesion;
    }

    [Fact]
    public void Aceptar_segunda_convocatoria_en_la_misma_sesion_lanza_participacion_activa()
    {
        var usuario = Guid.NewGuid();
        var sesion = SesionEquipoLobbyConUsuarioEnDosEquipos(usuario, out var convA, out var convB);

        sesion.ResponderConvocatoria(convA, usuario, aceptar: true, false, T0);

        Assert.Throws<ParticipacionActivaExistenteException>(
            () => sesion.ResponderConvocatoria(convB, usuario, aceptar: true, false, T0.AddSeconds(1)));
    }

    [Fact]
    public void Aceptar_tras_rechazar_la_otra_es_valido()
    {
        var usuario = Guid.NewGuid();
        var sesion = SesionEquipoLobbyConUsuarioEnDosEquipos(usuario, out var convA, out var convB);

        sesion.ResponderConvocatoria(convA, usuario, aceptar: false, false, T0);
        var c = sesion.ResponderConvocatoria(convB, usuario, aceptar: true, false, T0.AddSeconds(1));

        Assert.True(c.EstaAceptada);
    }

    [Fact]
    public void Rechazar_no_esta_bloqueado_por_una_aceptada_previa()
    {
        var usuario = Guid.NewGuid();
        var sesion = SesionEquipoLobbyConUsuarioEnDosEquipos(usuario, out var convA, out var convB);

        sesion.ResponderConvocatoria(convA, usuario, aceptar: true, false, T0);
        var c = sesion.ResponderConvocatoria(convB, usuario, aceptar: false, false, T0.AddSeconds(1));

        Assert.False(c.EstaAceptada);
    }
}
```

- [ ] **Step 2: Correr — FAIL**

```
dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter ResponderConvocatoriaGuardTests
```
Esperado: `Aceptar_segunda_convocatoria...` FALLA (hoy acepta ambas). Los otros dos pueden pasar ya — está bien; el RED es el primero.

- [ ] **Step 3: Implementar guard**

En `SesionPartida.ResponderConvocatoria`, rama `if (aceptar)` — añadir tras el check cross-partida existente:

```csharp
if (aceptar)
{
    if (participanteTieneParticipacionActivaEnOtra)
        throw new ParticipacionActivaExistenteException(usuarioId);
    var yaAceptoOtraEnEstaSesion = _inscripciones.Any(i => i.EsActiva
        && i.Convocatorias.Any(c => c.UsuarioId == usuarioId && c.EstaAceptada));
    if (yaAceptoOtraEnEstaSesion)
        throw new ParticipacionActivaExistenteException(usuarioId);
    convocatoria.Aceptar(now);
}
```

(No hace falta excluir la convocatoria actual: está `Pendiente`, así que cualquier `Aceptada` del usuario es necesariamente otra.)

- [ ] **Step 4: Correr — 3/3 PASS**. Capturar GREEN.

- [ ] **Step 5: Suites completas + commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ResponderConvocatoriaGuardTests.cs
git commit -m "SP-3e-2 C2: guard BR-G09 intra-partida (segunda convocatoria aceptada misma sesión → 409)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task C3: Aplicación — eventos con EquipoId

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/TriviaRuntimeEvents.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ResponderPreguntaCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ResponderPreguntaEquipoHandlerTests.cs` (create)

**Interfaces:**
- Consumes: `ResultadoRespuesta.EquipoId` (C1); `FakeSesionEventsPublisher` existente en UnitTests (captura eventos publicados).
- Produces: `RespuestaTriviaValidadaEvent`/`PuntajeTriviaIncrementadoEvent` += `Guid? EquipoId = null` (último parámetro); `PreguntaTriviaCerradaEvent` += `Guid? GanadorEquipoId = null`. Los payloads SignalR NO cambian (no portan identidad; Validada/Puntaje no difunden per SP-3f-2). Las 5 impls del seam no cambian de firma de método.

- [ ] **Step 1: Test que falla (compilación)**

Crear `ResponderPreguntaEquipoHandlerTests.cs` siguiendo el patrón de los handler-tests existentes de trivia (localizar con `grep -rln "ResponderPreguntaCommandHandler" services/operaciones-sesion/tests/ --include="*.cs"` y copiar su arreglo de fakes: `FakeSesionPartidaRepository`, `FakeOperacionesSesionUnitOfWork`, `FakeSesionEventsPublisher`, `TimeProvider` fijo). Dos tests:

```csharp
[Fact]
public async Task En_equipo_los_eventos_portan_el_equipo()
{
    // Arrange: sesión Equipo iniciada (helper como en C1) guardada en el fake repo;
    // command con el líder aceptado respondiendo la opción correcta.
    // Act: await handler.Handle(command, CancellationToken.None);
    // Assert:
    var validada = events.RespuestasValidadas.Single();
    Assert.Equal(equipoA, validada.EquipoId);
    var puntaje = events.PuntajesIncrementados.Single();
    Assert.Equal(equipoA, puntaje.EquipoId);
    var cerrada = events.PreguntasCerradas.Single();
    Assert.Equal(equipoA, cerrada.GanadorEquipoId);
    Assert.Equal(liderA, cerrada.GanadorParticipanteId);
}

[Fact]
public async Task En_individual_los_eventos_llevan_equipo_null()
{
    // Arrange: sesión Individual iniciada, participante inscrito, opción correcta.
    // Assert:
    Assert.Null(events.RespuestasValidadas.Single().EquipoId);
    Assert.Null(events.PreguntasCerradas.Single().GanadorEquipoId);
}
```

(Los nombres de las colecciones del fake publisher — `RespuestasValidadas`, etc. — deben tomarse del `FakeSesionEventsPublisher` real; si difieren, usar los reales.)

- [ ] **Step 2: Correr — FAIL** (los records no tienen los campos). Capturar RED.

- [ ] **Step 3: Implementar**

`TriviaRuntimeEvents.cs`:

```csharp
public sealed record RespuestaTriviaValidadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, Guid OpcionId, bool EsCorrecta, DateTime Instante,
    Guid? EquipoId = null);

public sealed record PuntajeTriviaIncrementadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs,
    Guid? EquipoId = null);

public sealed record PreguntaTriviaCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId,
    Guid? GanadorEquipoId = null);
```

`ResponderPreguntaCommandHandler.Handle` — añadir el campo a las 3 construcciones:

```csharp
await _events.PublicarRespuestaTriviaValidadaAsync(
    new RespuestaTriviaValidadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
        r.ParticipanteId, r.OpcionId, r.EsCorrecta, r.Instante, r.EquipoId), cancellationToken);

if (r.CerroPregunta)
{
    await _events.PublicarPuntajeTriviaIncrementadoAsync(
        new PuntajeTriviaIncrementadoEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
            r.ParticipanteId, r.Puntaje!.Value, r.TiempoRespuestaMs, r.EquipoId), cancellationToken);
    await _events.PublicarPreguntaTriviaCerradaAsync(
        new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
            MotivoCierrePregunta.RespuestaCorrecta.ToString(), r.Instante, r.ParticipanteId, r.EquipoId), cancellationToken);
    ...
}
```

Otros construction sites de `PreguntaTriviaCerradaEvent` (avance de operador, barrido de timeouts) compilan sin cambios por el default `= null` — verificar con:
```
grep -rn "PreguntaTriviaCerradaEvent(\|RespuestaTriviaValidadaEvent(\|PuntajeTriviaIncrementadoEvent(" services/operaciones-sesion/ --include="*.cs"
```
No editarlos: un cierre por tiempo/avance no tiene ganador de equipo.

- [ ] **Step 4: Correr — PASS.** Capturar GREEN.

- [ ] **Step 5: Suites completas + commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/TriviaRuntimeEvents.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ResponderPreguntaCommandHandler.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ResponderPreguntaEquipoHandlerTests.cs
git commit -m "SP-3e-2 C3: eventos Trivia con EquipoId (Validada/Puntaje/Cerrada, default null en Individual)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task C4: EF — columnas nuevas + migración SP3e2RuntimeTriviaEquipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create (generada): `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3e2RuntimeTriviaEquipo.cs` + `.Designer.cs` + `OperacionesSesionDbContextModelSnapshot.cs` (M)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/RespuestaEquipoPersistenciaTests.cs` (create)

**Interfaces:**
- Consumes: `RespuestaTrivia.EquipoId`, `PreguntaSnapshot.GanadorEquipoId` (C1).
- Produces: columnas `respuestas_trivia.equipoid` (uuid null) y `preguntas_snapshot.ganadorequipoid` (uuid null).

- [ ] **Step 1: Test de round-trip que falla (o pasa por convención — ver nota)**

```csharp
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class RespuestaEquipoPersistenciaTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static OperacionesSesionDbContext NewCtx(string name) =>
        new(new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options);

    [Fact]
    public async Task Respuesta_de_equipo_persiste_equipoid_y_ganadorequipoid()
    {
        var db = "resp-eq-" + Guid.NewGuid();
        var lider = Guid.NewGuid(); var equipo = Guid.NewGuid();
        var opcionOk = Guid.NewGuid();
        Guid partidaId;

        await using (var ctx = NewCtx(db))
        {
            var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60,
                new[] { new OpcionSnapshot(opcionOk, "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
            var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
            var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
            var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);
            partidaId = sesion.PartidaId;
            var ins = sesion.PreinscribirEquipo(equipo, true, new[] { lider }, false, 0, T0);
            sesion.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, lider, true, false, T0);
            sesion.Iniciar(T0);
            sesion.ResponderPregunta(lider, opcionOk, T0.AddSeconds(5));
            new SesionPartidaRepository(ctx).Add(sesion);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewCtx(db))
        {
            var r = await new SesionPartidaRepository(ctx).GetByPartidaIdAsync(partidaId, default);
            var preg = r!.Juegos.Single().Preguntas.Single(p => p.Orden == 1);
            Assert.Equal(equipo, preg.GanadorEquipoId);
            Assert.Equal(equipo, preg.Respuestas.Single().EquipoId);
        }
    }
}
```

Nota: con InMemory, EF puede mapear los `Guid?` nuevos por convención y este test puede pasar ANTES de la config explícita — no es una ventana como B3→B10. El valor del test es el round-trip; el RED estricto aplica a la migración (Step 3). Si pasa en verde directo, documentarlo en el report (no es fallo del test).

- [ ] **Step 2: Config EF explícita**

En `OperacionesSesionDbContext.cs`, dentro del bloque `Entity<PreguntaSnapshot>` (tras `GanadorParticipanteId`, línea ~111):

```csharp
entity.Property(x => x.GanadorEquipoId).HasColumnName("ganadorequipoid");
```

Dentro del bloque `Entity<RespuestaTrivia>` (tras `Instante`, línea ~135):

```csharp
entity.Property(x => x.EquipoId).HasColumnName("equipoid");
```

- [ ] **Step 3: Generar migración y verificar Up()**

```bash
cd /home/santiago/Escritorio/ProyectoDesarrollo/services/operaciones-sesion
dotnet ef migrations add SP3e2RuntimeTriviaEquipo --project src/Umbral.OperacionesSesion.Infrastructure --startup-project src/Umbral.OperacionesSesion.Api
```

`Up()` esperado — exactamente dos `AddColumn<Guid>` nullable:

```csharp
migrationBuilder.AddColumn<Guid>(name: "ganadorequipoid", table: "preguntas_snapshot", type: "uuid", nullable: true);
migrationBuilder.AddColumn<Guid>(name: "equipoid", table: "respuestas_trivia", type: "uuid", nullable: true);
```

`Down()`: los dos `DropColumn`. Si el generador añade algo más, detenerse y reportar (drift de snapshot).

- [ ] **Step 4: Suites completas** (Integration incluye el test nuevo). Verde.

- [ ] **Step 5: Commit (stagear SOLO los 5 archivos)**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs "services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/"*SP3e2RuntimeTriviaEquipo* services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/OperacionesSesionDbContextModelSnapshot.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/RespuestaEquipoPersistenciaTests.cs
git commit -m "SP-3e-2 C4: EF equipoid/ganadorequipoid + migración SP3e2RuntimeTriviaEquipo (additiva)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task C5: Proyección mi-sesión — yaRespondioPreguntaActual por equipo

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs` (línea ~55)
- Test: añadir 1 test a `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ProyeccionesEquipoTests.cs` (existente de SP-3e-1; si el arreglo del archivo no sirve, crear `MiSesionEquipoYaRespondioTests.cs` junto a él con el mismo patrón)

**Interfaces:**
- Consumes: `RespuestaTrivia.EquipoId` (C1); variable local `convocatoria` ya resuelta en el handler (la del caller, `Convocatoria.EquipoId`).
- Produces: en Equipo, `MiSesionDto.YaRespondioPreguntaActual` = "mi equipo ya respondió la pregunta activa".

- [ ] **Step 1: Test que falla**

Arreglo: sesión Equipo `Iniciada` con equipo A (líder + miembro, ambos aceptados) y equipo B; el líder de A responde INCORRECTO (equipo sellado, pregunta sigue activa); query mi-sesión del **miembro** de A y del líder de B:

```csharp
[Fact]
public async Task YaRespondio_es_true_para_todo_el_equipo_que_ya_respondio_y_false_para_los_demas()
{
    // ... arrange: sesión como arriba, guardada en FakeSesionPartidaRepository
    var deMiembroA = await handler.Handle(new ObtenerMiSesionQuery(miembroA), default);
    var deLiderB = await handler.Handle(new ObtenerMiSesionQuery(liderB), default);

    Assert.True(deMiembroA!.YaRespondioPreguntaActual);
    Assert.False(deLiderB!.YaRespondioPreguntaActual);
}
```

- [ ] **Step 2: Correr — FAIL** (`deMiembroA.YaRespondioPreguntaActual` es false hoy: busca por ParticipanteId). Capturar RED.

- [ ] **Step 3: Implementar**

En `ObtenerMiSesionQueryHandler`, reemplazar la línea `yaRespondio = preg.Respuestas.Any(r => r.ParticipanteId == request.ParticipanteId);` por:

```csharp
yaRespondio = sesion.Modalidad == Modalidad.Equipo && convocatoria is not null
    ? preg.Respuestas.Any(r => r.EquipoId == convocatoria.EquipoId)
    : preg.Respuestas.Any(r => r.ParticipanteId == request.ParticipanteId);
```

(`convocatoria` es la variable ya existente del handler — la del caller; en sesión Iniciada el caller llegó por convocatoria Aceptada, así que no es null en Equipo.)

- [ ] **Step 4: Correr — PASS.** Capturar GREEN.

- [ ] **Step 5: Suites completas + commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ProyeccionesEquipoTests.cs
git commit -m "SP-3e-2 C5: mi-sesion yaRespondioPreguntaActual por equipo en modalidad Equipo

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Si se creó archivo nuevo en Step 1, stagear ese en lugar de ProyeccionesEquipoTests.cs.)

---

### Task C6: Repo — scan de convocatorias pendientes accionables

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/ConvocatoriasPendientesScanTests.cs` (create)

**Interfaces:**
- Produces (C7 la consume):
  `Task<IReadOnlyList<Convocatoria>> GetConvocatoriasPendientesByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);`
  Devuelve entidades `Convocatoria` (portan `Id.Valor`, `PartidaId`, `EquipoId`, `FechaEnvio`) de sesiones en `Lobby`, inscripción `Activa`, estado `Pendiente` del usuario, orden por `FechaEnvio`.

- [ ] **Step 1: Tests integration que fallan (compilación)**

`ConvocatoriasPendientesScanTests.cs` con el patrón `NewCtx` (contextos write/read separados, lección B11-fix). Casos:

```csharp
[Fact] public async Task Devuelve_pendiente_de_sesion_en_lobby() { /* pendiente en Lobby → 1 resultado con ids correctos */ }
[Fact] public async Task Excluye_sesion_iniciada() { /* convocatoria pendiente pero sesión Iniciada (otro miembro aceptó y se inició) → vacío */ }
[Fact] public async Task Excluye_respondidas() { /* aceptada y rechazada → vacío */ }
[Fact] public async Task Excluye_de_otros_usuarios() { /* pendiente de otro usuario → vacío para el caller */ }
```

Arreglos: como en C4 (Publicar Equipo + PreinscribirEquipo; para "Iniciada": aceptar con OTRO miembro y `Iniciar(T0)` dejando al caller Pendiente). Asserts del primero:

```csharp
var r = await new SesionPartidaRepository(readCtx).GetConvocatoriasPendientesByUsuarioAsync(usuario, default);
var c = Assert.Single(r);
Assert.Equal(partidaId, c.PartidaId);
Assert.Equal(equipo, c.EquipoId);
Assert.Equal(EstadoConvocatoria.Pendiente, c.Estado);
```

- [ ] **Step 2: Correr — FAIL** (método no existe). Capturar RED.

- [ ] **Step 3: Implementar**

Interface (`ISesionPartidaRepository.cs`):

```csharp
Task<IReadOnlyList<Convocatoria>> GetConvocatoriasPendientesByUsuarioAsync(
    Guid usuarioId, CancellationToken cancellationToken);
```

Impl (`SesionPartidaRepository.cs`):

```csharp
public async Task<IReadOnlyList<Convocatoria>> GetConvocatoriasPendientesByUsuarioAsync(
    Guid usuarioId, CancellationToken cancellationToken)
    => await _dbContext.Sesiones
        .Where(s => s.Estado == EstadoSesion.Lobby)
        .SelectMany(s => s.Inscripciones)
        .Where(i => i.Estado == EstadoInscripcion.Activa)
        .SelectMany(i => i.Convocatorias)
        .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoConvocatoria.Pendiente)
        .OrderBy(c => c.FechaEnvio)
        .ToListAsync(cancellationToken);
```

Fake (`FakeSesionPartidaRepository.cs`) — mismo predicado sobre `_store`:

```csharp
public Task<IReadOnlyList<Convocatoria>> GetConvocatoriasPendientesByUsuarioAsync(
    Guid usuarioId, CancellationToken cancellationToken)
    => Task.FromResult<IReadOnlyList<Convocatoria>>(_store.Values
        .Where(s => s.Estado == EstadoSesion.Lobby)
        .SelectMany(s => s.Inscripciones)
        .Where(i => i.EsActiva)
        .SelectMany(i => i.Convocatorias)
        .Where(c => c.UsuarioId == usuarioId && c.EstaPendiente)
        .OrderBy(c => c.FechaEnvio)
        .ToList());
```

(Adaptar `_store.Values` al nombre real de la colección interna del fake.)

- [ ] **Step 4: Correr — 4/4 PASS.** Capturar GREEN.

- [ ] **Step 5: Suites completas + commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/ConvocatoriasPendientesScanTests.cs
git commit -m "SP-3e-2 C6: repo scan convocatorias pendientes accionables (Lobby + Activa + Pendiente del usuario)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task C7: Inbox — query + endpoint GET /mis-convocatorias

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ObtenerMisConvocatoriasPendientesQuery.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ConvocatoriaPendienteDto.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMisConvocatoriasPendientesQueryHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMisConvocatoriasPendientesQueryHandlerTests.cs` (create)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerMisConvocatoriasTests.cs` (create)

**Interfaces:**
- Consumes: `GetConvocatoriasPendientesByUsuarioAsync` (C6).
- Produces: `GET /operaciones-sesion/mis-convocatorias` → `200 Ok(IReadOnlyList<ConvocatoriaPendienteDto>)`, lista vacía si nada (nunca 404/204). `ConvocatoriaPendienteDto(Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio)`.

- [ ] **Step 1: Tests que fallan**

Handler test (patrón fakes existente):

```csharp
[Fact]
public async Task Mapea_convocatorias_pendientes_a_dto()
{
    // arrange: sesión Equipo en Lobby con convocatoria Pendiente del usuario en el fake repo
    var r = await handler.Handle(new ObtenerMisConvocatoriasPendientesQuery(usuario), default);
    var dto = Assert.Single(r);
    Assert.Equal(convocatoriaId, dto.ConvocatoriaId);
    Assert.Equal(partidaId, dto.PartidaId);
    Assert.Equal(equipoId, dto.EquipoId);
}

[Fact]
public async Task Sin_pendientes_devuelve_lista_vacia()
{
    var r = await handler.Handle(new ObtenerMisConvocatoriasPendientesQuery(Guid.NewGuid()), default);
    Assert.Empty(r);
}
```

Controller test (patrón `SesionesControllerEquipoTests` con `FakeSender` + `WithUser`):

```csharp
[Fact]
public async Task ObtenerMisConvocatorias_despacha_query_con_usuario_del_token_y_devuelve_200()
{
    var usuario = Guid.NewGuid();
    var sender = new FakeSender(new List<ConvocatoriaPendienteDto>());
    var controller = WithUser(new SesionesController(sender), usuario);

    var result = await controller.ObtenerMisConvocatorias(CancellationToken.None);

    var query = Assert.IsType<ObtenerMisConvocatoriasPendientesQuery>(sender.LastRequest);
    Assert.Equal(usuario, query.UsuarioId);
    Assert.IsType<OkObjectResult>(result);
}
```

(Firmas de `FakeSender`/`WithUser`: copiar del test file `SesionesControllerEquipoTests.cs` — patrón privado por archivo.)

- [ ] **Step 2: Correr — FAIL** (tipos no existen). Capturar RED.

- [ ] **Step 3: Implementar**

`ObtenerMisConvocatoriasPendientesQuery.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ObtenerMisConvocatoriasPendientesQuery(Guid UsuarioId)
    : IRequest<IReadOnlyList<ConvocatoriaPendienteDto>>;
```

`ConvocatoriaPendienteDto.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ConvocatoriaPendienteDto(
    Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio);
```

`ObtenerMisConvocatoriasPendientesQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerMisConvocatoriasPendientesQueryHandler
    : IRequestHandler<ObtenerMisConvocatoriasPendientesQuery, IReadOnlyList<ConvocatoriaPendienteDto>>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerMisConvocatoriasPendientesQueryHandler(ISesionPartidaRepository sesiones)
        => _sesiones = sesiones;

    public async Task<IReadOnlyList<ConvocatoriaPendienteDto>> Handle(
        ObtenerMisConvocatoriasPendientesQuery request, CancellationToken cancellationToken)
    {
        var pendientes = await _sesiones.GetConvocatoriasPendientesByUsuarioAsync(request.UsuarioId, cancellationToken);
        return pendientes
            .Select(c => new ConvocatoriaPendienteDto(c.Id.Valor, c.PartidaId, c.EquipoId, c.FechaEnvio))
            .ToList();
    }
}
```

`SesionesController.cs` — junto a `ObtenerMiSesion` (línea ~150):

```csharp
[HttpGet("mis-convocatorias")]
public async Task<IActionResult> ObtenerMisConvocatorias(CancellationToken cancellationToken)
{
    var usuarioId = ObtenerParticipanteId();
    var dto = await _mediator.Send(new ObtenerMisConvocatoriasPendientesQuery(usuarioId), cancellationToken);
    return Ok(dto);
}
```

- [ ] **Step 4: Correr — PASS.** Capturar GREEN.

- [ ] **Step 5: Suites completas + commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ObtenerMisConvocatoriasPendientesQuery.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ConvocatoriaPendienteDto.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMisConvocatoriasPendientesQueryHandler.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMisConvocatoriasPendientesQueryHandlerTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerMisConvocatoriasTests.cs
git commit -m "SP-3e-2 C7: inbox GET /mis-convocatorias (query + handler + DTO + endpoint)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task C8: Contrato HTTP + traceability (carve-out)

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify (NUNCA stagear): `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: shapes commiteados de C1-C7 — el CÓDIGO es autoritativo; verificar cada celda contra los archivos reales antes de escribir.

- [ ] **Step 1: Actualizar contrato**

En `contracts/http/operaciones-sesion-api.md`:

1. Fila nueva en Endpoint Registry (tras `Mi sesión (reconexión)`):

```markdown
| Mis convocatorias pendientes | GET | `/operaciones-sesion/mis-convocatorias` | Participante | 200 + ConvocatoriaPendienteDto[] (vacía si no hay) | 401 sin identidad |
```

2. Bullet DTO nuevo (sección DTOs):

```markdown
- `ConvocatoriaPendienteDto { convocatoriaId, partidaId, equipoId, fechaEnvio }` (solo convocatorias Pendientes accionables: partida en Lobby, inscripción del equipo activa; orden por fechaEnvio)
```

3. En la fila `Answer active question`, ampliar la celda Errors: `403 no inscrito` → `403 no inscrito / sin convocatoria aceptada (Equipo)`; y en `409` añadir `duplicada (por equipo en modalidad Equipo)` si la celda distingue — mantener el estilo de la celda actual.

4. Nota nueva al final de Notes:

```markdown
Modalidad Equipo (SP-3e-2): en `POST .../pregunta-actual/respuesta` responde cualquier miembro con convocatoria aceptada; la PRIMERA respuesta del equipo (correcta o no) lo sella — los demás miembros reciben 409 duplicada. `MiSesionDto.yaRespondioPreguntaActual` en Equipo significa "mi equipo ya respondió". Aceptar una convocatoria teniendo otra aceptada en la misma partida devuelve 409. Los eventos internos `RespuestaTriviaValidada`/`PuntajeTriviaIncrementado`/`PreguntaTriviaCerrada` portan `equipoId`/`ganadorEquipoId` (null en Individual); los payloads SignalR difundidos no cambian.
```

Verificar cada shape contra el código commiteado (controller, DTOs, handler) antes de escribir.

- [ ] **Step 2: Correr ContractTests + suites**

```
dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj
```
Esperado: verde (el doc mantiene las líneas que los tests asserten).

- [ ] **Step 3: Traceability (carve-out)**

Añadir la fila SP-3e-2 a `docs/04-sdd/traceability-matrix.md` siguiendo el formato de las filas SP-3e-1/SP-3f. **NO stagearla jamás.**

- [ ] **Step 4: Commit SOLO del contrato**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "SP-3e-2 C8: contrato — mis-convocatorias + semántica respuesta Equipo en pregunta-actual

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

Después: `git status --short` debe mostrar `docs/04-sdd/traceability-matrix.md` como `M` unstaged y el commit debe contener 1 solo archivo.

---

## Self-Review (ejecutado al escribir el plan)

1. **Cobertura del spec:** §3.1 dominio → C1; guard BR-G09 → C2; §3.2 eventos → C3 (con la corrección verificada: payloads SignalR no cambian porque no portan identidad — Validada/Puntaje no difunden per SP-3f-2, `PreguntaCerradaPayload` no lleva ganador); §3.4 EF/migración → C4; §3.5 proyección → C5; §3.3+§3.4 inbox → C6+C7; contrato+traceability → C8. Sin huecos.
2. **Placeholders:** los tests de C3/C5/C6 handler-side referencian fakes existentes cuyos nombres exactos el implementer debe copiar del archivo real (instrucción explícita de localización incluida) — no son TBD, son anti-drift.
3. **Consistencia de tipos:** `ResultadoRespuesta.EquipoId` (C1) consumido en C3; `GetConvocatoriasPendientesByUsuarioAsync` (C6) consumido en C7 con firma idéntica; `ConvocatoriaPendienteDto` idéntico en C7 y C8.
