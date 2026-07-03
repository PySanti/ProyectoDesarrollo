# SP-3c — Runtime Trivia (Individual) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Llevar a la vida un `JuegoTrivia` activo dentro de una `SesionPartida` ya `Iniciada` (SP-3b): activar/sincronizar la pregunta actual, validar respuestas (primera-correcta cierra; ventana de tiempo aplicada), avanzar preguntas y, al agotarlas, avanzar de juego reutilizando `FinalizarJuegoActual`.

**Architecture:** Todo el gameplay pasa por el agregado `SesionPartida`. El contenido de preguntas se captura como **snapshot inmutable al publicar** (extensión del `GET /partidas/{id}` ya existente); en vivo se lee de la propia DB de Operaciones (cero red). Scoring/ranking = Puntuaciones (SP-4); aquí Operaciones **valida y emite** eventos por el puerto No-Op. Avance operador-dirigido; barrido automático de timeout y SignalR → SP-3f.

**Tech Stack:** .NET 8, Clean Architecture + CQRS (MediatR 12.2.0), FluentValidation, EF Core 8 (Npgsql + InMemory fallback), xUnit, `WebApplicationFactory<Program>` para contract tests.

## Global Constraints

- **Servicio:** `services/operaciones-sesion`, namespace `Umbral.OperacionesSesion.*` (ADR-0009). No tocar otras DB; el contenido de preguntas entra **una sola vez** por HTTP en publicación.
- **Estructura graduada (no negociable):** Application con carpetas exactas {Commands, Queries, Interfaces, Validators, DTOs, Handlers, Handlers/Commands, Handlers/Queries, Exceptions}; controllers nativos `ControllerBase` + `_mediator.Send` sin lógica; repos en `Domain/Abstractions/Persistence`; `Infrastructure/{Persistence,Services}`; middleware centralizado; `Program.cs` sólo `MapControllers`.
- **R1 / ADR-0010:** estado runtime sólo en Operaciones. Sin cambios.
- **Eventos:** por `ISesionEventsPublisher` (No-Op), **publicados después de `SaveChanges`**. Las transiciones devuelven **200** (no creación).
- **Reloj:** `TimeProvider` inyectado; `now = _timeProvider.GetUtcNow().UtcDateTime`; pasar `now` como parámetro al dominio. **Nunca** `DateTime.UtcNow` inline.
- **Modalidad: Individual** solamente. Equipo Trivia → SP-3a-E.
- **Resoluciones:** (a) participante sin inscripción activa que responde → **403**; (b) **4 eventos** Trivia registrados; (c) respuesta fuera de tiempo → **409**.
- **No leak:** `PreguntaActualDto` y la opción pública NUNCA exponen `esCorrecta`.
- **Compatibilidad con SP-3a/3b (crítico):** Un `JuegoTrivia` con **cero** preguntas tiene `TienePreguntasAbiertas == false`, así que el guard de `FinalizarJuegoActual` no dispara para los juegos question-less que usan los tests existentes. **NO** agregar preguntas a `StubConfigClient.Default` ni a los snapshots question-less de tests existentes; los tests SP-3c registran su propia config-con-preguntas vía `Stub.Respuestas[partidaId]`.
- **Higiene git (lección SP-3b):** en cada task hacer `git add` SÓLO de los archivos listados; **nunca** `git add -A`/`.`, ni `git checkout/restore/clean .` — hay cambios intencionales sin stagear en el árbol.
- **Postgres dev (design-time):** `Host=localhost;Port=55432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005`.
- **Tests:** suite completa `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`. Por proyecto: `dotnet test services/operaciones-sesion/tests/<Proj>/<Proj>.csproj`. Baseline antes de empezar: **111/111 verde**. Commit por task.
- **EF CLI:** si `dotnet-ef` global falla por arquitectura, usar `dotnet tool run dotnet-ef` desde dentro de `services/operaciones-sesion/`.

---

### Task 1: Enums, datos snapshot y records de resultado

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoPregunta.cs`
- Create: `.../Domain/Enums/MotivoCierrePregunta.cs`
- Create: `.../Domain/Entities/OpcionSnapshot.cs`
- Create: `.../Domain/Entities/RespuestaTrivia.cs`
- Create: `.../Domain/Results/ResultadoRespuesta.cs`
- Create: `.../Domain/Results/ResultadoAvancePregunta.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/TriviaLeafTypesTests.cs`

**Interfaces:**
- Produces: `EstadoPregunta { Pendiente, Activa, Cerrada }`; `MotivoCierrePregunta { RespuestaCorrecta, AvanceOperador, Tiempo }`; `OpcionSnapshot(Guid OpcionId, string Texto, bool EsCorrecta)` (con ctor EF privado); `RespuestaTrivia(Guid ParticipanteId, Guid OpcionId, bool EsCorrecta, DateTime Instante)` (ctor EF privado); `ResultadoRespuesta` y `ResultadoAvancePregunta` (records).

- [ ] **Step 1: Write the failing test**

```csharp
// TriviaLeafTypesTests.cs
using System;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class TriviaLeafTypesTests
{
    [Fact]
    public void Opcion_and_respuesta_expose_their_data()
    {
        var op = new OpcionSnapshot(Guid.NewGuid(), "Paris", true);
        Assert.True(op.EsCorrecta);
        Assert.Equal("Paris", op.Texto);

        var pid = Guid.NewGuid();
        var r = new RespuestaTrivia(pid, op.OpcionId, true, new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(pid, r.ParticipanteId);
        Assert.True(r.EsCorrecta);
    }

    [Fact]
    public void Result_records_carry_outcome()
    {
        var rr = new ResultadoRespuesta(true, true, 10, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc), 1200);
        Assert.True(rr.CerroPregunta);
        Assert.Equal(10, rr.Puntaje);

        var av = new ResultadoAvancePregunta(Guid.NewGuid(), Guid.NewGuid(), 1, MotivoCierrePregunta.AvanceOperador,
            null, null, null, null, true);
        Assert.True(av.SinMasPreguntas);
        Assert.Equal(MotivoCierrePregunta.AvanceOperador, av.MotivoCierre);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter TriviaLeafTypesTests`
Expected: FAIL (types not defined).

- [ ] **Step 3: Write minimal implementation**

```csharp
// EstadoPregunta.cs
namespace Umbral.OperacionesSesion.Domain.Enums;
public enum EstadoPregunta { Pendiente, Activa, Cerrada }
```
```csharp
// MotivoCierrePregunta.cs
namespace Umbral.OperacionesSesion.Domain.Enums;
public enum MotivoCierrePregunta { RespuestaCorrecta, AvanceOperador, Tiempo }
```
```csharp
// OpcionSnapshot.cs
namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class OpcionSnapshot
{
    public Guid OpcionId { get; private set; }
    public string Texto { get; private set; } = null!;
    public bool EsCorrecta { get; private set; }

    private OpcionSnapshot() { } // EF

    public OpcionSnapshot(Guid opcionId, string texto, bool esCorrecta)
    {
        OpcionId = opcionId;
        Texto = texto;
        EsCorrecta = esCorrecta;
    }
}
```
```csharp
// RespuestaTrivia.cs
namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class RespuestaTrivia
{
    public Guid Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid OpcionId { get; private set; }
    public bool EsCorrecta { get; private set; }
    public DateTime Instante { get; private set; }

    private RespuestaTrivia() { } // EF

    public RespuestaTrivia(Guid participanteId, Guid opcionId, bool esCorrecta, DateTime instante)
    {
        Id = Guid.NewGuid();
        ParticipanteId = participanteId;
        OpcionId = opcionId;
        EsCorrecta = esCorrecta;
        Instante = instante;
    }
}
```
```csharp
// ResultadoRespuesta.cs
namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoRespuesta(
    bool EsCorrecta,
    bool CerroPregunta,
    int? Puntaje,
    Guid JuegoId,
    Guid PreguntaId,
    Guid ParticipanteId,
    Guid OpcionId,
    DateTime Instante,
    long TiempoRespuestaMs);
```
```csharp
// ResultadoAvancePregunta.cs
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoAvancePregunta(
    Guid JuegoId,
    Guid PreguntaCerradaId,
    int PreguntaCerradaOrden,
    MotivoCierrePregunta MotivoCierre,
    Guid? PreguntaActivadaId,
    int? PreguntaActivadaOrden,
    int? TiempoLimiteActivadaSegundos,
    DateTime? FechaActivacionActivada,
    bool SinMasPreguntas);
```

- [ ] **Step 4: Run test to verify it passes**

Run: same as Step 2. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoPregunta.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/MotivoCierrePregunta.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/OpcionSnapshot.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/RespuestaTrivia.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoRespuesta.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoAvancePregunta.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/TriviaLeafTypesTests.cs
git commit -m "SP-3c T1: enums Trivia + OpcionSnapshot/RespuestaTrivia + records de resultado"
```

---

### Task 2: `PreguntaSnapshot` — máquina de estado + registro de respuesta

**Files:**
- Create: `.../Domain/Entities/PreguntaSnapshot.cs`
- Create: `.../Domain/Exceptions/RespuestaDuplicadaException.cs`
- Create: `.../Domain/Exceptions/PreguntaFueraDeTiempoException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PreguntaSnapshotTests.cs`

**Interfaces:**
- Consumes: `OpcionSnapshot`, `RespuestaTrivia`, `EstadoPregunta`, `MotivoCierrePregunta`, `ResultadoRespuesta` (T1).
- Produces:
  - `PreguntaSnapshot(Guid preguntaId, int orden, string texto, int puntajeAsignado, int tiempoLimiteSegundos, IEnumerable<OpcionSnapshot> opciones)`
  - props: `PreguntaId, Orden, Texto, PuntajeAsignado, TiempoLimiteSegundos, EstadoPregunta Estado, DateTime? FechaActivacion, DateTime? FechaCierre, MotivoCierrePregunta? MotivoCierre, Guid? GanadorParticipanteId, IReadOnlyList<OpcionSnapshot> Opciones, IReadOnlyList<RespuestaTrivia> Respuestas`
  - `internal void Activar(DateTime now)` (Pendiente→Activa, set FechaActivacion)
  - `internal void Cerrar(MotivoCierrePregunta motivo, DateTime now, Guid? ganador)` (Activa→Cerrada)
  - `internal ResultadoRespuesta RegistrarRespuesta(Guid participanteId, Guid opcionId, DateTime now)`
  - `RespuestaDuplicadaException(Guid participanteId)`, `PreguntaFueraDeTiempoException(Guid preguntaId)`

- [ ] **Step 1: Write the failing test**

```csharp
// PreguntaSnapshotTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class PreguntaSnapshotTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot Pregunta(int limite = 30)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "Paris", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "Londres", false);
        return new PreguntaSnapshot(Guid.NewGuid(), 1, "Capital?", 10, limite, new[] { ok, no });
    }

    private static Guid CorrectaId(PreguntaSnapshot p) => p.Opciones.Single(o => o.EsCorrecta).OpcionId;
    private static Guid IncorrectaId(PreguntaSnapshot p) => p.Opciones.First(o => !o.EsCorrecta).OpcionId;

    [Fact]
    public void Activar_sets_active_and_fecha()
    {
        var p = Pregunta();
        p.Activar(T0);
        Assert.Equal(EstadoPregunta.Activa, p.Estado);
        Assert.Equal(T0, p.FechaActivacion);
    }

    [Fact]
    public void Correct_answer_closes_and_sets_winner_and_score()
    {
        var p = Pregunta();
        p.Activar(T0);
        var part = Guid.NewGuid();
        var r = p.RegistrarRespuesta(part, CorrectaId(p), T0.AddSeconds(5));
        Assert.True(r.EsCorrecta);
        Assert.True(r.CerroPregunta);
        Assert.Equal(10, r.Puntaje);
        Assert.Equal(5000, r.TiempoRespuestaMs);
        Assert.Equal(EstadoPregunta.Cerrada, p.Estado);
        Assert.Equal(MotivoCierrePregunta.RespuestaCorrecta, p.MotivoCierre);
        Assert.Equal(part, p.GanadorParticipanteId);
    }

    [Fact]
    public void Wrong_answer_records_but_keeps_open()
    {
        var p = Pregunta();
        p.Activar(T0);
        var r = p.RegistrarRespuesta(Guid.NewGuid(), IncorrectaId(p), T0.AddSeconds(2));
        Assert.False(r.EsCorrecta);
        Assert.False(r.CerroPregunta);
        Assert.Null(r.Puntaje);
        Assert.Equal(EstadoPregunta.Activa, p.Estado);
        Assert.Single(p.Respuestas);
    }

    [Fact]
    public void Duplicate_answer_by_same_participant_throws()
    {
        var p = Pregunta();
        p.Activar(T0);
        var part = Guid.NewGuid();
        p.RegistrarRespuesta(part, IncorrectaId(p), T0.AddSeconds(1));
        Assert.Throws<RespuestaDuplicadaException>(() => p.RegistrarRespuesta(part, IncorrectaId(p), T0.AddSeconds(2)));
    }

    [Fact]
    public void Answer_after_time_limit_throws()
    {
        var p = Pregunta(limite: 30);
        p.Activar(T0);
        Assert.Throws<PreguntaFueraDeTiempoException>(
            () => p.RegistrarRespuesta(Guid.NewGuid(), CorrectaId(p), T0.AddSeconds(31)));
    }

    [Fact]
    public void Operator_close_sets_motivo_without_winner()
    {
        var p = Pregunta();
        p.Activar(T0);
        p.Cerrar(MotivoCierrePregunta.AvanceOperador, T0.AddSeconds(10), ganador: null);
        Assert.Equal(EstadoPregunta.Cerrada, p.Estado);
        Assert.Null(p.GanadorParticipanteId);
        Assert.Equal(MotivoCierrePregunta.AvanceOperador, p.MotivoCierre);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter PreguntaSnapshotTests`
Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

```csharp
// RespuestaDuplicadaException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class RespuestaDuplicadaException : Exception
{
    public RespuestaDuplicadaException(Guid participanteId)
        : base($"El participante {participanteId} ya respondió esta pregunta.") { }
}
```
```csharp
// PreguntaFueraDeTiempoException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class PreguntaFueraDeTiempoException : Exception
{
    public PreguntaFueraDeTiempoException(Guid preguntaId)
        : base($"La pregunta {preguntaId} está fuera de su ventana de tiempo.") { }
}
```
```csharp
// PreguntaSnapshot.cs
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class PreguntaSnapshot
{
    private readonly List<OpcionSnapshot> _opciones = new();
    private readonly List<RespuestaTrivia> _respuestas = new();

    public Guid PreguntaId { get; private set; }
    public int Orden { get; private set; }
    public string Texto { get; private set; } = null!;
    public int PuntajeAsignado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }
    public EstadoPregunta Estado { get; private set; } = EstadoPregunta.Pendiente;
    public DateTime? FechaActivacion { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public MotivoCierrePregunta? MotivoCierre { get; private set; }
    public Guid? GanadorParticipanteId { get; private set; }

    public IReadOnlyList<OpcionSnapshot> Opciones => _opciones;
    public IReadOnlyList<RespuestaTrivia> Respuestas => _respuestas;

    private PreguntaSnapshot() { } // EF

    public PreguntaSnapshot(Guid preguntaId, int orden, string texto, int puntajeAsignado,
        int tiempoLimiteSegundos, IEnumerable<OpcionSnapshot> opciones)
    {
        PreguntaId = preguntaId;
        Orden = orden;
        Texto = texto;
        PuntajeAsignado = puntajeAsignado;
        TiempoLimiteSegundos = tiempoLimiteSegundos;
        _opciones.AddRange(opciones);
    }

    internal void Activar(DateTime now)
    {
        if (Estado != EstadoPregunta.Pendiente)
            throw new InvalidOperationException($"La pregunta {PreguntaId} no está pendiente.");
        Estado = EstadoPregunta.Activa;
        FechaActivacion = now;
    }

    internal void Cerrar(MotivoCierrePregunta motivo, DateTime now, Guid? ganador)
    {
        if (Estado != EstadoPregunta.Activa)
            throw new InvalidOperationException($"La pregunta {PreguntaId} no está activa.");
        Estado = EstadoPregunta.Cerrada;
        FechaCierre = now;
        MotivoCierre = motivo;
        GanadorParticipanteId = ganador;
    }

    internal ResultadoRespuesta RegistrarRespuesta(Guid participanteId, Guid opcionId, DateTime now)
    {
        if (Estado != EstadoPregunta.Activa)
            throw new InvalidOperationException($"La pregunta {PreguntaId} no está activa.");
        if (_respuestas.Any(r => r.ParticipanteId == participanteId))
            throw new RespuestaDuplicadaException(participanteId);
        if (now > FechaActivacion!.Value.AddSeconds(TiempoLimiteSegundos))
            throw new PreguntaFueraDeTiempoException(PreguntaId);

        var esCorrecta = _opciones.Any(o => o.OpcionId == opcionId && o.EsCorrecta);
        _respuestas.Add(new RespuestaTrivia(participanteId, opcionId, esCorrecta, now));

        var cerro = false;
        if (esCorrecta)
        {
            Cerrar(MotivoCierrePregunta.RespuestaCorrecta, now, participanteId);
            cerro = true;
        }

        var tiempoMs = (long)(now - FechaActivacion!.Value).TotalMilliseconds;
        return new ResultadoRespuesta(esCorrecta, cerro, esCorrecta ? PuntajeAsignado : null,
            Guid.Empty, PreguntaId, participanteId, opcionId, now, tiempoMs);
    }
}
```

- [ ] **Step 4: Run test to verify it passes** — same as Step 2. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/PreguntaSnapshot.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/RespuestaDuplicadaException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/PreguntaFueraDeTiempoException.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/PreguntaSnapshotTests.cs
git commit -m "SP-3c T2: PreguntaSnapshot (estado + registro de respuesta, ventana de tiempo, dedup)"
```

---

### Task 3: `JuegoResumen` lleva preguntas y las activa secuencialmente

**Files:**
- Modify: `.../Domain/Entities/JuegoResumen.cs`
- Modify: `.../Domain/Entities/SesionPartida.cs` (call sites `Activar()` → `Activar(now)`)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/JuegoResumenTriviaTests.cs`

**Interfaces:**
- Consumes: `PreguntaSnapshot` (T2).
- Produces (en `JuegoResumen`):
  - nuevo ctor: `JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego, IEnumerable<PreguntaSnapshot> preguntas)` (el ctor de 3 args sigue existiendo, equivale a sin preguntas)
  - `IReadOnlyList<PreguntaSnapshot> Preguntas`
  - `internal void Activar(DateTime now)` (firma cambia: activa el juego y, si es Trivia, su primera pregunta pendiente)
  - `internal PreguntaSnapshot? ActivarSiguientePregunta(DateTime now)`
  - `PreguntaSnapshot? PreguntaActiva` (la `Activa` o null)
  - `bool TienePreguntasAbiertas` (alguna `Activa` o `Pendiente`)

- [ ] **Step 1: Write the failing test**

```csharp
// JuegoResumenTriviaTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class JuegoResumenTriviaTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    [Fact]
    public void Activar_trivia_activates_first_question_by_orden()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(2), P(1) });
        juego.Activar(T0);
        Assert.Equal(EstadoJuego.Activo, juego.Estado);
        Assert.NotNull(juego.PreguntaActiva);
        Assert.Equal(1, juego.PreguntaActiva!.Orden);
        Assert.True(juego.TienePreguntasAbiertas);
    }

    [Fact]
    public void Activar_siguiente_returns_next_then_null()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1), P(2) });
        juego.Activar(T0); // Q1 activa
        juego.PreguntaActiva!.Cerrar(MotivoCierrePregunta.AvanceOperador, T0, null);
        var sig = juego.ActivarSiguientePregunta(T0);
        Assert.NotNull(sig);
        Assert.Equal(2, sig!.Orden);
        sig.Cerrar(MotivoCierrePregunta.AvanceOperador, T0, null);
        Assert.Null(juego.ActivarSiguientePregunta(T0));
        Assert.False(juego.TienePreguntasAbiertas);
    }

    [Fact]
    public void Trivia_without_questions_has_no_open_questions()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia); // sin preguntas
        juego.Activar(T0);
        Assert.Null(juego.PreguntaActiva);
        Assert.False(juego.TienePreguntasAbiertas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter JuegoResumenTriviaTests`
Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

Replace `JuegoResumen.cs` entirely with:

```csharp
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class JuegoResumen
{
    private readonly List<PreguntaSnapshot> _preguntas = new();

    public Guid JuegoId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }
    public EstadoJuego Estado { get; private set; } = EstadoJuego.Pendiente;

    public IReadOnlyList<PreguntaSnapshot> Preguntas => _preguntas;
    public PreguntaSnapshot? PreguntaActiva => _preguntas.FirstOrDefault(p => p.Estado == EstadoPregunta.Activa);
    public bool TienePreguntasAbiertas =>
        _preguntas.Any(p => p.Estado is EstadoPregunta.Activa or EstadoPregunta.Pendiente);

    private JuegoResumen() { } // EF

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego)
        : this(juegoId, orden, tipoJuego, Enumerable.Empty<PreguntaSnapshot>()) { }

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego, IEnumerable<PreguntaSnapshot> preguntas)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
        _preguntas.AddRange(preguntas);
    }

    internal void Activar(DateTime now)
    {
        if (Estado != EstadoJuego.Pendiente)
            throw new InvalidOperationException($"El juego {JuegoId} no está pendiente.");
        Estado = EstadoJuego.Activo;
        if (TipoJuego == TipoJuego.Trivia)
            ActivarSiguientePregunta(now);
    }

    internal void Finalizar()
    {
        if (Estado != EstadoJuego.Activo)
            throw new InvalidOperationException($"El juego {JuegoId} no está activo.");
        Estado = EstadoJuego.Finalizado;
    }

    internal PreguntaSnapshot? ActivarSiguientePregunta(DateTime now)
    {
        var siguiente = _preguntas
            .Where(p => p.Estado == EstadoPregunta.Pendiente)
            .OrderBy(p => p.Orden)
            .FirstOrDefault();
        siguiente?.Activar(now);
        return siguiente;
    }
}
```

In `SesionPartida.cs`, update the two `Activar()` call sites to pass `now`:
- In `AplicarInicio(DateTime now)`: `primero.Activar();` → `primero.Activar(now);`
- In `FinalizarJuegoActual(DateTime now)`: `siguiente.Activar();` → `siguiente.Activar(now);`

- [ ] **Step 4: Run the full unit project to verify no regression**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (new JuegoResumenTriviaTests + all existing — question-less Trivia games still behave).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/JuegoResumen.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/JuegoResumenTriviaTests.cs
git commit -m "SP-3c T3: JuegoResumen lleva preguntas y activa la primera/siguiente; Activar(now)"
```

---

### Task 4: `SesionPartida` — Responder, Avanzar pregunta y guard de finalización

**Files:**
- Modify: `.../Domain/Entities/SesionPartida.cs`
- Create: `.../Domain/Exceptions/JuegoActivoNoEsTriviaException.cs`
- Create: `.../Domain/Exceptions/NoHayPreguntaActivaException.cs`
- Create: `.../Domain/Exceptions/ParticipanteNoInscritoException.cs`
- Create: `.../Domain/Exceptions/JuegoConPreguntasPendientesException.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTriviaTests.cs`

**Interfaces:**
- Consumes: `JuegoResumen` helpers (T3), `PreguntaSnapshot.RegistrarRespuesta/Cerrar` (T2), `ResultadoRespuesta`, `ResultadoAvancePregunta` (T1).
- Produces (en `SesionPartida`):
  - `ResultadoRespuesta ResponderPregunta(Guid participanteId, Guid opcionId, DateTime now)`
  - `ResultadoAvancePregunta AvanzarPregunta(DateTime now)`
  - `FinalizarJuegoActual(DateTime now)` ahora lanza `JuegoConPreguntasPendientesException` si el juego Trivia activo tiene preguntas abiertas.
  - 4 exceptions nuevas (todas `(Guid …)`).

- [ ] **Step 1: Write the failing test**

```csharp
// SesionPartidaTriviaTests.cs
using System;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaTriviaTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden, out Guid correcta)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        correcta = ok.OpcionId;
        return new PreguntaSnapshot(Guid.NewGuid(), orden, $"Q{orden}", 10, 30, new[] { ok, no });
    }

    private static SesionPartida IniciadaConTrivia(Guid partidaId, Guid participante, params PreguntaSnapshot[] preguntas)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, preguntas);
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(participante, false, 0, T0);
        sesion.Iniciar(T0); // juego Trivia activo, Q1 activa
        return sesion;
    }

    [Fact]
    public void Responder_correcta_cierra_y_devuelve_puntaje_y_juegoId()
    {
        var part = Guid.NewGuid();
        var p1 = P(1, out var correcta);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, p1);
        var juegoId = sesion.Juegos.Single().JuegoId;

        var r = sesion.ResponderPregunta(part, correcta, T0.AddSeconds(3));

        Assert.True(r.EsCorrecta);
        Assert.True(r.CerroPregunta);
        Assert.Equal(10, r.Puntaje);
        Assert.Equal(juegoId, r.JuegoId);
        Assert.Equal(p1.PreguntaId, r.PreguntaId);
    }

    [Fact]
    public void Responder_sin_inscripcion_lanza_no_inscrito()
    {
        var p1 = P(1, out var correcta);
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), p1);
        Assert.Throws<ParticipanteNoInscritoException>(
            () => sesion.ResponderPregunta(Guid.NewGuid(), correcta, T0.AddSeconds(1)));
    }

    [Fact]
    public void Responder_cuando_no_iniciada_lanza()
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, out _) });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap); // Lobby
        Assert.Throws<SesionNoIniciadaException>(
            () => sesion.ResponderPregunta(Guid.NewGuid(), Guid.NewGuid(), T0));
    }

    [Fact]
    public void Avanzar_cierra_y_activa_siguiente()
    {
        var part = Guid.NewGuid();
        var sesion = IniciadaConTrivia(Guid.NewGuid(), part, P(1, out _), P(2, out _));
        var r = sesion.AvanzarPregunta(T0.AddSeconds(5));
        Assert.Equal(1, r.PreguntaCerradaOrden);
        Assert.Equal(MotivoCierrePregunta.AvanceOperador, r.MotivoCierre);
        Assert.Equal(2, r.PreguntaActivadaOrden);
        Assert.False(r.SinMasPreguntas);
    }

    [Fact]
    public void Avanzar_pasado_el_limite_cierra_por_tiempo()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _), P(2, out _));
        var r = sesion.AvanzarPregunta(T0.AddSeconds(31)); // límite 30
        Assert.Equal(MotivoCierrePregunta.Tiempo, r.MotivoCierre);
    }

    [Fact]
    public void Avanzar_en_ultima_reporta_sin_mas_preguntas()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _));
        var r = sesion.AvanzarPregunta(T0.AddSeconds(5));
        Assert.True(r.SinMasPreguntas);
        Assert.Null(r.PreguntaActivadaOrden);
    }

    [Fact]
    public void Finalizar_con_preguntas_abiertas_lanza()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _));
        Assert.Throws<JuegoConPreguntasPendientesException>(() => sesion.FinalizarJuegoActual(T0.AddSeconds(5)));
    }

    [Fact]
    public void Agotar_preguntas_luego_finalizar_termina_la_sesion()
    {
        var sesion = IniciadaConTrivia(Guid.NewGuid(), Guid.NewGuid(), P(1, out _));
        sesion.AvanzarPregunta(T0.AddSeconds(5)); // cierra Q1, sin más
        var avance = sesion.FinalizarJuegoActual(T0.AddSeconds(6)); // único juego → Terminada
        Assert.True(avance.Terminada());
        Assert.Equal(EstadoSesion.Terminada, sesion.Estado);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter SesionPartidaTriviaTests`
Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

Create the 4 exceptions:
```csharp
// JuegoActivoNoEsTriviaException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoActivoNoEsTriviaException : Exception
{
    public JuegoActivoNoEsTriviaException(Guid partidaId)
        : base($"El juego activo de la partida {partidaId} no es de tipo Trivia.") { }
}
```
```csharp
// NoHayPreguntaActivaException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class NoHayPreguntaActivaException : Exception
{
    public NoHayPreguntaActivaException(Guid partidaId)
        : base($"No hay una pregunta activa en la partida {partidaId}.") { }
}
```
```csharp
// ParticipanteNoInscritoException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class ParticipanteNoInscritoException : Exception
{
    public ParticipanteNoInscritoException(Guid participanteId)
        : base($"El participante {participanteId} no tiene inscripción activa en esta partida.") { }
}
```
```csharp
// JuegoConPreguntasPendientesException.cs
namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoConPreguntasPendientesException : Exception
{
    public JuegoConPreguntasPendientesException(Guid partidaId)
        : base($"El juego Trivia de la partida {partidaId} aún tiene preguntas abiertas.") { }
}
```

In `SesionPartida.cs`: add `ResponderPregunta` and `AvanzarPregunta`, and extend `FinalizarJuegoActual`. Add these methods (place after `FinalizarJuegoActual`):

```csharp
public ResultadoRespuesta ResponderPregunta(Guid participanteId, Guid opcionId, DateTime now)
{
    var juego = JuegoTriviaActivo();
    var activa = juego.PreguntaActiva ?? throw new NoHayPreguntaActivaException(PartidaId);
    if (!_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
        throw new ParticipanteNoInscritoException(participanteId);

    var resultado = activa.RegistrarRespuesta(participanteId, opcionId, now);
    return resultado with { JuegoId = juego.JuegoId };
}

public ResultadoAvancePregunta AvanzarPregunta(DateTime now)
{
    var juego = JuegoTriviaActivo();
    var activa = juego.PreguntaActiva ?? throw new NoHayPreguntaActivaException(PartidaId);

    var vencida = now >= activa.FechaActivacion!.Value.AddSeconds(activa.TiempoLimiteSegundos);
    var motivo = vencida ? MotivoCierrePregunta.Tiempo : MotivoCierrePregunta.AvanceOperador;
    activa.Cerrar(motivo, now, ganador: null);

    var siguiente = juego.ActivarSiguientePregunta(now);
    return new ResultadoAvancePregunta(
        juego.JuegoId, activa.PreguntaId, activa.Orden, motivo,
        siguiente?.PreguntaId, siguiente?.Orden, siguiente?.TiempoLimiteSegundos, siguiente?.FechaActivacion,
        siguiente is null);
}

private JuegoResumen JuegoTriviaActivo()
{
    if (Estado != EstadoSesion.Iniciada)
        throw new SesionNoIniciadaException(PartidaId);
    var juego = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
    if (juego.TipoJuego != TipoJuego.Trivia)
        throw new JuegoActivoNoEsTriviaException(PartidaId);
    return juego;
}
```

In `FinalizarJuegoActual`, right after `var actual = _juegos.Single(j => j.Estado == EstadoJuego.Activo);` add the Trivia guard before `actual.Finalizar();`:
```csharp
if (actual.TipoJuego == TipoJuego.Trivia && actual.TienePreguntasAbiertas)
    throw new JuegoConPreguntasPendientesException(PartidaId);
```
Ensure `using Umbral.OperacionesSesion.Domain.Enums;` covers `MotivoCierrePregunta` (it does, same namespace).

- [ ] **Step 4: Run the full unit project** — `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`. Expected: PASS (new + existing).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/JuegoActivoNoEsTriviaException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/NoHayPreguntaActivaException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/ParticipanteNoInscritoException.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/JuegoConPreguntasPendientesException.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTriviaTests.cs
git commit -m "SP-3c T4: ResponderPregunta + AvanzarPregunta + guard de finalización Trivia"
```

---

### Task 5: Eventos Trivia (puerto No-Op + fake)

**Files:**
- Create: `.../Application/Interfaces/TriviaRuntimeEvents.cs`
- Modify: `.../Application/Interfaces/ISesionEventsPublisher.cs` (+4 métodos)
- Modify: `.../Infrastructure/Services/NoOpSesionEventsPublisher.cs` (+4)
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs` (+4 listas/métodos)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakePublisherTriviaTests.cs`

**Interfaces:**
- Produces: records `RespuestaTriviaValidadaEvent`, `PuntajeTriviaIncrementadoEvent`, `PreguntaTriviaActivadaEvent`, `PreguntaTriviaCerradaEvent`; métodos `PublicarRespuestaTriviaValidadaAsync`, `PublicarPuntajeTriviaIncrementadoAsync`, `PublicarPreguntaTriviaActivadaAsync`, `PublicarPreguntaTriviaCerradaAsync`; fake con listas `RespuestasValidadas`, `PuntajesIncrementados`, `PreguntasActivadas`, `PreguntasCerradas`.

- [ ] **Step 1: Write the failing test**

```csharp
// FakePublisherTriviaTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class FakePublisherTriviaTests
{
    [Fact]
    public async Task Fake_records_trivia_events()
    {
        var fake = new FakeSesionEventsPublisher();
        await fake.PublicarRespuestaTriviaValidadaAsync(
            new RespuestaTriviaValidadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, DateTime.UtcNow), CancellationToken.None);
        await fake.PublicarPuntajeTriviaIncrementadoAsync(
            new PuntajeTriviaIncrementadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 1500), CancellationToken.None);
        await fake.PublicarPreguntaTriviaActivadaAsync(
            new PreguntaTriviaActivadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 30, DateTime.UtcNow), CancellationToken.None);
        await fake.PublicarPreguntaTriviaCerradaAsync(
            new PreguntaTriviaCerradaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "RespuestaCorrecta", DateTime.UtcNow, Guid.NewGuid()), CancellationToken.None);

        Assert.Single(fake.RespuestasValidadas);
        Assert.Single(fake.PuntajesIncrementados);
        Assert.Single(fake.PreguntasActivadas);
        Assert.Single(fake.PreguntasCerradas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter FakePublisherTriviaTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

```csharp
// TriviaRuntimeEvents.cs
namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record RespuestaTriviaValidadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, Guid OpcionId, bool EsCorrecta, DateTime Instante);

public sealed record PuntajeTriviaIncrementadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    Guid ParticipanteId, int Puntaje, long TiempoRespuestaMs);

public sealed record PreguntaTriviaActivadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    int Orden, int TiempoLimiteSegundos, DateTime FechaActivacion);

public sealed record PreguntaTriviaCerradaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid PreguntaId,
    string Motivo, DateTime FechaCierre, Guid? GanadorParticipanteId);
```

Append to `ISesionEventsPublisher`:
```csharp
    Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken);
    Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken);
    Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken);
    Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken);
```

Append to `NoOpSesionEventsPublisher` (4 methods, each `=> Task.CompletedTask;`):
```csharp
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken) => Task.CompletedTask;
```

Append to `FakeSesionEventsPublisher`:
```csharp
    public List<RespuestaTriviaValidadaEvent> RespuestasValidadas { get; } = new();
    public List<PuntajeTriviaIncrementadoEvent> PuntajesIncrementados { get; } = new();
    public List<PreguntaTriviaActivadaEvent> PreguntasActivadas { get; } = new();
    public List<PreguntaTriviaCerradaEvent> PreguntasCerradas { get; } = new();

    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken)
    { RespuestasValidadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken)
    { PuntajesIncrementados.Add(evento); return Task.CompletedTask; }
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken)
    { PreguntasActivadas.Add(evento); return Task.CompletedTask; }
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken)
    { PreguntasCerradas.Add(evento); return Task.CompletedTask; }
```

- [ ] **Step 4: Run test to verify it passes** — `--filter FakePublisherTriviaTests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/TriviaRuntimeEvents.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakePublisherTriviaTests.cs
git commit -m "SP-3c T5: 4 eventos Trivia en puerto No-Op + fake"
```

---

### Task 6: Snapshot extendido — config DTO, cliente HTTP y mapeo en publicación

**Files:**
- Modify: `.../Application/DTOs/ConfiguracionPartidaDto.cs` (Trivia config en `JuegoResumenDto`)
- Modify: `.../Infrastructure/Services/PartidasConfigHttpClient.cs` (deserialización + mapeo)
- Modify: `.../Application/Handlers/Commands/PublicarPartidaCommandHandler.cs` (construye `JuegoResumen` con preguntas)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PublicarPartidaTriviaSnapshotTests.cs`

**Interfaces:**
- Produces: `JuegoResumenDto(Guid JuegoId, int Orden, string TipoJuego, TriviaConfigDto? Trivia)`; `TriviaConfigDto(IReadOnlyList<PreguntaConfigDto> Preguntas)`; `PreguntaConfigDto(Guid PreguntaId, string Texto, int PuntajeAsignado, int TiempoLimiteSegundos, IReadOnlyList<OpcionConfigDto> Opciones)`; `OpcionConfigDto(Guid OpcionId, string Texto, bool EsCorrecta)`. El handler mapea Trivia→`PreguntaSnapshot` (Orden por posición 1..n).

- [ ] **Step 1: Write the failing test**

```csharp
// PublicarPartidaTriviaSnapshotTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaTriviaSnapshotTests
{
    private sealed class StubClient : IConfiguracionPartidaClient
    {
        private readonly ConfiguracionPartidaDto _dto;
        public StubClient(ConfiguracionPartidaDto dto) => _dto = dto;
        public Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(Guid p, string? b, CancellationToken c)
            => Task.FromResult<ConfiguracionPartidaDto?>(_dto);
    }

    [Fact]
    public async Task Publicar_captures_trivia_questions_into_snapshot()
    {
        var partidaId = Guid.NewGuid();
        var trivia = new TriviaConfigDto(new List<PreguntaConfigDto>
        {
            new(Guid.NewGuid(), "Capital?", 10, 30, new List<OpcionConfigDto>
            {
                new(Guid.NewGuid(), "Paris", true), new(Guid.NewGuid(), "Londres", false)
            })
        });
        var config = new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia", trivia) });

        var repo = new FakeSesionPartidaRepository();
        var handler = new PublicarPartidaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(),
            new StubClient(config), new FakeSesionEventsPublisher());

        await handler.Handle(new PublicarPartidaCommand(partidaId, null), CancellationToken.None);

        var sesion = repo.Store[partidaId];
        var juego = sesion.Juegos.Single();
        Assert.Equal(TipoJuego.Trivia, juego.TipoJuego);
        Assert.Single(juego.Preguntas);
        var pregunta = juego.Preguntas.Single();
        Assert.Equal(1, pregunta.Orden);
        Assert.Equal(10, pregunta.PuntajeAsignado);
        Assert.Equal(2, pregunta.Opciones.Count);
        Assert.Single(pregunta.Opciones.Where(o => o.EsCorrecta));
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter PublicarPartidaTriviaSnapshotTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

Replace `ConfiguracionPartidaDto.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ConfiguracionPartidaDto(
    string Nombre,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    IReadOnlyList<JuegoResumenDto> Juegos);

public sealed record JuegoResumenDto(Guid JuegoId, int Orden, string TipoJuego, TriviaConfigDto? Trivia = null);

public sealed record TriviaConfigDto(IReadOnlyList<PreguntaConfigDto> Preguntas);

public sealed record PreguntaConfigDto(
    Guid PreguntaId, string Texto, int PuntajeAsignado, int TiempoLimiteSegundos,
    IReadOnlyList<OpcionConfigDto> Opciones);

public sealed record OpcionConfigDto(Guid OpcionId, string Texto, bool EsCorrecta);
```

In `PartidasConfigHttpClient.cs`: extend the local deserialization shapes and the mapping. Replace `PartidasJuegoResponse` and the `Juegos.Select(...)` mapping:
```csharp
                payload.Juegos.Select(j => new JuegoResumenDto(
                    j.JuegoId, j.Orden, j.TipoJuego,
                    j.Trivia is null
                        ? null
                        : new TriviaConfigDto(j.Trivia.Preguntas
                            .Select(p => new PreguntaConfigDto(
                                p.PreguntaId, p.Texto, p.PuntajeAsignado, p.TiempoLimiteSegundos,
                                p.Opciones.Select(o => new OpcionConfigDto(o.OpcionId, o.Texto, o.EsCorrecta)).ToList()))
                            .ToList()))).ToList());
```
and the private records:
```csharp
    private sealed record PartidasJuegoResponse(Guid JuegoId, int Orden, string TipoJuego, PartidasTriviaResponse? Trivia);
    private sealed record PartidasTriviaResponse(List<PartidasPreguntaResponse> Preguntas);
    private sealed record PartidasPreguntaResponse(Guid PreguntaId, string Texto, int PuntajeAsignado, int TiempoLimiteSegundos, List<PartidasOpcionResponse> Opciones);
    private sealed record PartidasOpcionResponse(Guid OpcionId, string Texto, bool EsCorrecta);
```
(The JSON from Partidas nests questions under `juego.trivia.preguntas[]` with `opciones[]` — see `contracts/http/partidas-config.md`. camelCase binding is case-insensitive.)

In `PublicarPartidaCommandHandler.cs`, replace the `config.Juegos.Select(...)` snapshot line with a mapping that builds questions for Trivia:
```csharp
            config.Juegos.Select(MapearJuego).ToList());
```
and add a private static mapper at the bottom of the class:
```csharp
    private static JuegoResumen MapearJuego(JuegoResumenDto j)
    {
        var tipo = Enum.Parse<TipoJuego>(j.TipoJuego, ignoreCase: true);
        if (tipo != TipoJuego.Trivia || j.Trivia is null)
            return new JuegoResumen(j.JuegoId, j.Orden, tipo);

        var preguntas = j.Trivia.Preguntas
            .Select((p, idx) => new PreguntaSnapshot(
                p.PreguntaId, idx + 1, p.Texto, p.PuntajeAsignado, p.TiempoLimiteSegundos,
                p.Opciones.Select(o => new OpcionSnapshot(o.OpcionId, o.Texto, o.EsCorrecta)).ToList()))
            .ToList();
        return new JuegoResumen(j.JuegoId, j.Orden, tipo, preguntas);
    }
```
Ensure the handler `using`s include `Umbral.OperacionesSesion.Domain.Entities;` (already present) and `Umbral.OperacionesSesion.Application.DTOs;` (already present).

- [ ] **Step 4: Run the full unit project** — `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`. Expected: PASS (existing publish tests still pass: question-less Trivia maps with `Trivia == null` → empty preguntas).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ConfiguracionPartidaDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/PartidasConfigHttpClient.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/PublicarPartidaCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PublicarPartidaTriviaSnapshotTests.cs
git commit -m "SP-3c T6: snapshot extendido con preguntas/opciones Trivia al publicar"
```

---

### Task 7: Commands, Query, DTOs y Validators

**Files:**
- Create: `.../Application/Commands/ResponderPreguntaCommand.cs`
- Create: `.../Application/Commands/AvanzarPreguntaCommand.cs`
- Create: `.../Application/Queries/ObtenerPreguntaActualQuery.cs`
- Create: `.../Application/DTOs/RespuestaTriviaResponse.cs`
- Create: `.../Application/DTOs/AvancePreguntaResponse.cs`
- Create: `.../Application/DTOs/PreguntaActualDto.cs`
- Create: `.../Application/DTOs/ResponderPreguntaRequest.cs`
- Create: `.../Application/Validators/ResponderPreguntaCommandValidator.cs`
- Create: `.../Application/Validators/AvanzarPreguntaCommandValidator.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/TriviaValidatorsTests.cs`

**Interfaces:**
- Produces:
  - `ResponderPreguntaCommand(Guid PartidaId, Guid ParticipanteId, Guid OpcionId) : IRequest<RespuestaTriviaResponse>`
  - `AvanzarPreguntaCommand(Guid PartidaId) : IRequest<AvancePreguntaResponse>`
  - `ObtenerPreguntaActualQuery(Guid PartidaId) : IRequest<PreguntaActualDto>`
  - `RespuestaTriviaResponse(Guid PartidaId, Guid PreguntaId, bool EsCorrecta, bool CerroPregunta, int? Puntaje)`
  - `AvancePreguntaResponse(Guid PartidaId, int PreguntaCerradaOrden, int? PreguntaActivadaOrden, bool SinMasPreguntas)`
  - `PreguntaActualDto(Guid PartidaId, Guid JuegoId, Guid PreguntaId, int Orden, string Texto, int TiempoLimiteSegundos, DateTime FechaActivacion, IReadOnlyList<OpcionPublicaDto> Opciones)` + `OpcionPublicaDto(Guid OpcionId, string Texto)`
  - `ResponderPreguntaRequest(Guid OpcionId)`

- [ ] **Step 1: Write the failing test**

```csharp
// TriviaValidatorsTests.cs
using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class TriviaValidatorsTests
{
    [Fact]
    public void Responder_requires_partida_and_opcion()
    {
        var v = new ResponderPreguntaCommandValidator();
        Assert.False(v.Validate(new ResponderPreguntaCommand(Guid.Empty, Guid.NewGuid(), Guid.Empty)).IsValid);
        Assert.True(v.Validate(new ResponderPreguntaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())).IsValid);
    }

    [Fact]
    public void Avanzar_requires_partida()
    {
        var v = new AvanzarPreguntaCommandValidator();
        Assert.False(v.Validate(new AvanzarPreguntaCommand(Guid.Empty)).IsValid);
        Assert.True(v.Validate(new AvanzarPreguntaCommand(Guid.NewGuid())).IsValid);
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter TriviaValidatorsTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

```csharp
// ResponderPreguntaCommand.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record ResponderPreguntaCommand(Guid PartidaId, Guid ParticipanteId, Guid OpcionId) : IRequest<RespuestaTriviaResponse>;
```
```csharp
// AvanzarPreguntaCommand.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record AvanzarPreguntaCommand(Guid PartidaId) : IRequest<AvancePreguntaResponse>;
```
```csharp
// ObtenerPreguntaActualQuery.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Queries;
public sealed record ObtenerPreguntaActualQuery(Guid PartidaId) : IRequest<PreguntaActualDto>;
```
```csharp
// RespuestaTriviaResponse.cs
namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record RespuestaTriviaResponse(Guid PartidaId, Guid PreguntaId, bool EsCorrecta, bool CerroPregunta, int? Puntaje);
```
```csharp
// AvancePreguntaResponse.cs
namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record AvancePreguntaResponse(Guid PartidaId, int PreguntaCerradaOrden, int? PreguntaActivadaOrden, bool SinMasPreguntas);
```
```csharp
// PreguntaActualDto.cs
namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record PreguntaActualDto(
    Guid PartidaId, Guid JuegoId, Guid PreguntaId, int Orden, string Texto,
    int TiempoLimiteSegundos, DateTime FechaActivacion, IReadOnlyList<OpcionPublicaDto> Opciones);
public sealed record OpcionPublicaDto(Guid OpcionId, string Texto);
```
```csharp
// ResponderPreguntaRequest.cs
namespace Umbral.OperacionesSesion.Application.DTOs;
public sealed record ResponderPreguntaRequest(Guid OpcionId);
```
```csharp
// ResponderPreguntaCommandValidator.cs
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class ResponderPreguntaCommandValidator : AbstractValidator<ResponderPreguntaCommand>
{
    public ResponderPreguntaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.OpcionId).NotEmpty();
    }
}
```
```csharp
// AvanzarPreguntaCommandValidator.cs
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class AvanzarPreguntaCommandValidator : AbstractValidator<AvanzarPreguntaCommand>
{
    public AvanzarPreguntaCommandValidator() => RuleFor(x => x.PartidaId).NotEmpty();
}
```

- [ ] **Step 4: Run test to verify it passes** — `--filter TriviaValidatorsTests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/ResponderPreguntaCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/AvanzarPreguntaCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ObtenerPreguntaActualQuery.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/RespuestaTriviaResponse.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/AvancePreguntaResponse.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/PreguntaActualDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/ResponderPreguntaRequest.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/ResponderPreguntaCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/AvanzarPreguntaCommandValidator.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/TriviaValidatorsTests.cs
git commit -m "SP-3c T7: commands/query/DTOs/validators de runtime Trivia"
```

---

### Task 8: `ResponderPreguntaCommandHandler`

**Files:**
- Create: `.../Application/Handlers/Commands/ResponderPreguntaCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ResponderPreguntaCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository`, `IOperacionesSesionUnitOfWork`, `ISesionEventsPublisher`, `TimeProvider`; `SesionPartida.ResponderPregunta` (T4); eventos T5.
- Produces: handler que guarda y emite **después** de save: siempre `RespuestaTriviaValidada`; si `CerroPregunta` con ganador → `PuntajeTriviaIncrementado` + `PreguntaTriviaCerrada(motivo=RespuestaCorrecta, ganador)`.

- [ ] **Step 1: Write the failing test**

```csharp
// ResponderPreguntaCommandHandlerTests.cs
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

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ResponderPreguntaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static (SesionPartida sesion, Guid participante, Guid correcta) Iniciada(Guid partidaId)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30, new[] { ok, no });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var part = Guid.NewGuid();
        sesion.Inscribir(part, false, 0, T0);
        sesion.Iniciar(T0);
        return (sesion, part, ok.OpcionId);
    }

    [Fact]
    public async Task Correct_answer_saves_and_publishes_three_events()
    {
        var partidaId = Guid.NewGuid();
        var (sesion, part, correcta) = Iniciada(partidaId);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(4)));

        var resp = await handler.Handle(new ResponderPreguntaCommand(partidaId, part, correcta), CancellationToken.None);

        Assert.True(resp.EsCorrecta);
        Assert.True(resp.CerroPregunta);
        Assert.Equal(10, resp.Puntaje);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.RespuestasValidadas);
        Assert.Single(events.PuntajesIncrementados);
        Assert.Single(events.PreguntasCerradas);
    }

    [Fact]
    public async Task Wrong_answer_publishes_only_validada()
    {
        var partidaId = Guid.NewGuid();
        var (sesion, part, correcta) = Iniciada(partidaId);
        var incorrecta = sesion.Juegos.Single().Preguntas.Single().Opciones.First(o => o.OpcionId != correcta).OpcionId;
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(2)));

        var resp = await handler.Handle(new ResponderPreguntaCommand(partidaId, part, incorrecta), CancellationToken.None);

        Assert.False(resp.EsCorrecta);
        Assert.False(resp.CerroPregunta);
        Assert.Single(events.RespuestasValidadas);
        Assert.Empty(events.PuntajesIncrementados);
        Assert.Empty(events.PreguntasCerradas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter ResponderPreguntaCommandHandlerTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

```csharp
// ResponderPreguntaCommandHandler.cs
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class ResponderPreguntaCommandHandler : IRequestHandler<ResponderPreguntaCommand, RespuestaTriviaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public ResponderPreguntaCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<RespuestaTriviaResponse> Handle(ResponderPreguntaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.ResponderPregunta(request.ParticipanteId, request.OpcionId, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarRespuestaTriviaValidadaAsync(
            new RespuestaTriviaValidadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
                r.ParticipanteId, r.OpcionId, r.EsCorrecta, r.Instante), cancellationToken);

        if (r.CerroPregunta)
        {
            await _events.PublicarPuntajeTriviaIncrementadoAsync(
                new PuntajeTriviaIncrementadoEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
                    r.ParticipanteId, r.Puntaje!.Value, r.TiempoRespuestaMs), cancellationToken);
            await _events.PublicarPreguntaTriviaCerradaAsync(
                new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaId,
                    MotivoCierrePregunta.RespuestaCorrecta.ToString(), r.Instante, r.ParticipanteId), cancellationToken);
        }

        return new RespuestaTriviaResponse(sesion.PartidaId, r.PreguntaId, r.EsCorrecta, r.CerroPregunta, r.Puntaje);
    }
}
```

- [ ] **Step 4: Run test to verify it passes** — `--filter ResponderPreguntaCommandHandlerTests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/ResponderPreguntaCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ResponderPreguntaCommandHandlerTests.cs
git commit -m "SP-3c T8: ResponderPreguntaCommandHandler (emite Validada/Incrementado/Cerrada post-save)"
```

---

### Task 9: `AvanzarPreguntaCommandHandler`

**Files:**
- Create: `.../Application/Handlers/Commands/AvanzarPreguntaCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/AvanzarPreguntaCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `SesionPartida.AvanzarPregunta` (T4); eventos T5.
- Produces: handler que tras save emite `PreguntaTriviaCerrada` (motivo del resultado, sin ganador) y, si hubo activación, `PreguntaTriviaActivada`. Devuelve `AvancePreguntaResponse`.

- [ ] **Step 1: Write the failing test**

```csharp
// AvanzarPreguntaCommandHandlerTests.cs
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

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class AvanzarPreguntaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida Iniciada(Guid partidaId, params PreguntaSnapshot[] preguntas)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, preguntas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public async Task Advance_to_next_publishes_cerrada_and_activada()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, P(1), P(2)));
        var events = new FakeSesionEventsPublisher();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var handler = new AvanzarPreguntaCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(new AvanzarPreguntaCommand(partidaId), CancellationToken.None);

        Assert.Equal(1, resp.PreguntaCerradaOrden);
        Assert.Equal(2, resp.PreguntaActivadaOrden);
        Assert.False(resp.SinMasPreguntas);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PreguntasCerradas);
        Assert.Single(events.PreguntasActivadas);
    }

    [Fact]
    public async Task Advance_on_last_publishes_only_cerrada()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, P(1)));
        var events = new FakeSesionEventsPublisher();
        var handler = new AvanzarPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(new AvanzarPreguntaCommand(partidaId), CancellationToken.None);

        Assert.True(resp.SinMasPreguntas);
        Assert.Null(resp.PreguntaActivadaOrden);
        Assert.Single(events.PreguntasCerradas);
        Assert.Empty(events.PreguntasActivadas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter AvanzarPreguntaCommandHandlerTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

```csharp
// AvanzarPreguntaCommandHandler.cs
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class AvanzarPreguntaCommandHandler : IRequestHandler<AvanzarPreguntaCommand, AvancePreguntaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public AvanzarPreguntaCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<AvancePreguntaResponse> Handle(AvanzarPreguntaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.AvanzarPregunta(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarPreguntaTriviaCerradaAsync(
            new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaCerradaId,
                r.MotivoCierre.ToString(), now, null), cancellationToken);

        if (r.PreguntaActivadaId is not null)
        {
            await _events.PublicarPreguntaTriviaActivadaAsync(
                new PreguntaTriviaActivadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaActivadaId.Value,
                    r.PreguntaActivadaOrden!.Value, r.TiempoLimiteActivadaSegundos!.Value, r.FechaActivacionActivada!.Value),
                cancellationToken);
        }

        return new AvancePreguntaResponse(sesion.PartidaId, r.PreguntaCerradaOrden, r.PreguntaActivadaOrden, r.SinMasPreguntas);
    }
}
```

- [ ] **Step 4: Run test to verify it passes** — `--filter AvanzarPreguntaCommandHandlerTests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/AvanzarPreguntaCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/AvanzarPreguntaCommandHandlerTests.cs
git commit -m "SP-3c T9: AvanzarPreguntaCommandHandler (emite Cerrada + Activada post-save)"
```

---

### Task 10: `ObtenerPreguntaActualQueryHandler` (participant-safe)

**Files:**
- Create: `.../Application/Handlers/Queries/ObtenerPreguntaActualQueryHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerPreguntaActualQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository`; `SesionPartida` helpers.
- Produces: handler que devuelve `PreguntaActualDto` **sin `esCorrecta`**; si no hay pregunta activa → `NoHayPreguntaActivaException`; si sesión no existe → `SesionNoEncontradaException`.

- [ ] **Step 1: Write the failing test**

```csharp
// ObtenerPreguntaActualQueryHandlerTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerPreguntaActualQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Iniciada(Guid partidaId)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Capital?", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "Paris", true), new OpcionSnapshot(Guid.NewGuid(), "Londres", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public async Task Returns_active_question_without_correct_flag()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId));
        var handler = new ObtenerPreguntaActualQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerPreguntaActualQuery(partidaId), CancellationToken.None);

        Assert.Equal(1, dto.Orden);
        Assert.Equal(2, dto.Opciones.Count);
        // DTO de opción pública NO tiene propiedad EsCorrecta:
        Assert.Null(typeof(Umbral.OperacionesSesion.Application.DTOs.OpcionPublicaDto).GetProperty("EsCorrecta"));
    }

    [Fact]
    public async Task No_active_question_throws()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "a", true), new OpcionSnapshot(Guid.NewGuid(), "b", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        repo.Add(SesionPartida.Publicar(partidaId, snap)); // Lobby, ninguna pregunta activa
        var handler = new ObtenerPreguntaActualQueryHandler(repo);

        await Assert.ThrowsAsync<NoHayPreguntaActivaException>(
            () => handler.Handle(new ObtenerPreguntaActualQuery(partidaId), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter ObtenerPreguntaActualQueryHandlerTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

```csharp
// ObtenerPreguntaActualQueryHandler.cs
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerPreguntaActualQueryHandler : IRequestHandler<ObtenerPreguntaActualQuery, PreguntaActualDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerPreguntaActualQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<PreguntaActualDto> Handle(ObtenerPreguntaActualQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
        var pregunta = juego?.PreguntaActiva ?? throw new NoHayPreguntaActivaException(request.PartidaId);

        return new PreguntaActualDto(
            sesion.PartidaId, juego!.JuegoId, pregunta.PreguntaId, pregunta.Orden, pregunta.Texto,
            pregunta.TiempoLimiteSegundos, pregunta.FechaActivacion!.Value,
            pregunta.Opciones.Select(o => new OpcionPublicaDto(o.OpcionId, o.Texto)).ToList());
    }
}
```

- [ ] **Step 4: Run test to verify it passes** — `--filter ObtenerPreguntaActualQueryHandlerTests`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerPreguntaActualQueryHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerPreguntaActualQueryHandlerTests.cs
git commit -m "SP-3c T10: ObtenerPreguntaActualQueryHandler (participant-safe, sin esCorrecta)"
```

---

### Task 11: Emitir `PreguntaTriviaActivada` al activarse el primer juego Trivia

**Files:**
- Modify: `.../Application/Handlers/Commands/IniciarPartidaCommandHandler.cs`
- Modify: `.../Application/Handlers/Commands/FinalizarJuegoActualCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PreguntaActivadaEnInicioTests.cs`

**Interfaces:**
- Consumes: `JuegoResumen.PreguntaActiva` (T3); evento `PreguntaTriviaActivadaEvent` (T5).
- Produces: helper compartido `PublicarPreguntaActivadaSiTriviaAsync(events, sesion, juego, cancellationToken)`; ambos handlers lo invocan tras publicar `JuegoActivado` cuando el juego activado es Trivia con pregunta activa.

**Note for implementer:** este task toca handlers SP-3b. NO cambiar su lógica de inicio/secuenciación; sólo AÑADIR la emisión del evento de pregunta activada. Re-correr los tests SP-3b existentes (`IniciarPartidaCommandHandlerTests`, `FinalizarJuegoActualCommandHandlerTests`) — deben seguir verdes (sus juegos Trivia no tienen preguntas → `PreguntaActiva == null` → no se emite nada nuevo).

- [ ] **Step 1: Write the failing test**

```csharp
// PreguntaActivadaEnInicioTests.cs
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

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PreguntaActivadaEnInicioTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Starting_partida_with_trivia_questions_publishes_pregunta_activada()
    {
        var partidaId = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new IniciarPartidaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0));

        await handler.Handle(new IniciarPartidaCommand(partidaId), CancellationToken.None);

        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
        Assert.Single(events.PreguntasActivadas);
        Assert.Equal(1, events.PreguntasActivadas[0].Orden);
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `--filter PreguntaActivadaEnInicioTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

In `IniciarPartidaCommandHandler.cs`, add a shared helper (place as `internal static`) and call it from `PublicarEventosInicioAsync` in the `Iniciada` branch right after publishing `JuegoActivado`:
```csharp
    internal static async Task PublicarPreguntaActivadaSiTriviaAsync(
        ISesionEventsPublisher events, SesionPartida sesion, JuegoResumen juego, CancellationToken cancellationToken)
    {
        var pregunta = juego.PreguntaActiva;
        if (pregunta is null) return;
        await events.PublicarPreguntaTriviaActivadaAsync(
            new PreguntaTriviaActivadaEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, pregunta.PreguntaId,
                pregunta.Orden, pregunta.TiempoLimiteSegundos, pregunta.FechaActivacion!.Value),
            cancellationToken);
    }
```
and in the `Iniciada` case, after the `PublicarJuegoActivadoAsync(...)` call:
```csharp
                await PublicarPreguntaActivadaSiTriviaAsync(events, sesion, juego, cancellationToken);
```
Add `using Umbral.OperacionesSesion.Domain.Entities;` to the file if not present.

In `FinalizarJuegoActualCommandHandler.cs`, after it publishes `JuegoActivado` for the newly activated game (the `Avanzado` branch), call:
```csharp
                await IniciarPartidaCommandHandler.PublicarPreguntaActivadaSiTriviaAsync(events, sesion, <juegoActivado>, cancellationToken);
```
where `<juegoActivado>` is the `ResultadoAvance.JuegoActivado` already used to build the `JuegoActivado` event. (Read the handler's existing structure and reuse the same variable; the publisher reference is the handler's `_events` field or the `events` parameter, matching the existing call.)

- [ ] **Step 4: Run the full unit project** — `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`. Expected: PASS (new test + existing SP-3b handler tests stay green: question-less Trivia → no new event).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/IniciarPartidaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/FinalizarJuegoActualCommandHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/PreguntaActivadaEnInicioTests.cs
git commit -m "SP-3c T11: emite PreguntaTriviaActivada al activarse el primer juego Trivia"
```

---

### Task 12: Mapeos EF + migración

**Files:**
- Modify: `.../Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create: `.../Infrastructure/Persistence/Migrations/<timestamp>_SP3cRuntimeTrivia.cs` (generada)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/TriviaSnapshotPersistenceTests.cs`

**Interfaces:**
- Produces: mapeo de `PreguntaSnapshot` (tabla `preguntas_snapshot`), `OpcionSnapshot` (`opciones_snapshot`), `RespuestaTrivia` (`respuestas_trivia`); navegaciones por campo; FK en cascada. Migración aditiva.

- [ ] **Step 1: Write the failing test**

```csharp
// TriviaSnapshotPersistenceTests.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class TriviaSnapshotPersistenceTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Persists_and_reads_back_trivia_questions_and_answer()
    {
        var partidaId = Guid.NewGuid();
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Capital?", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "Paris", true), new OpcionSnapshot(Guid.NewGuid(), "Londres", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var part = Guid.NewGuid();
        sesion.Inscribir(part, false, 0, T0);
        sesion.Iniciar(T0); // Q1 activa
        sesion.ResponderPregunta(part, pregunta.Opciones.Single(o => o.EsCorrecta).OpcionId, T0.AddSeconds(3));

        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("trivia-" + Guid.NewGuid()).Options;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            new SesionPartidaRepository(write).Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var loaded = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);
            Assert.NotNull(loaded);
            var p = loaded!.Juegos.Single().Preguntas.Single();
            Assert.Equal(EstadoPregunta.Cerrada, p.Estado);
            Assert.Equal(MotivoCierrePregunta.RespuestaCorrecta, p.MotivoCierre);
            Assert.Equal(part, p.GanadorParticipanteId);
            Assert.Equal(2, p.Opciones.Count);
            Assert.Single(p.Respuestas);
        }
    }
}
```

> **Note:** this test exercises the repo's eager loading (`GetByPartidaIdAsync`). It will only pass once Task 13 extends the `Include` chain. If running Task 12 in isolation, the mapping/migration parts are validated by the suite build + `dotnet-ef`; the round-trip assertion fully passes after Task 13. (Implementer: run this test at the end of Task 13.)

- [ ] **Step 2: Add the EF mappings**

In `OperacionesSesionDbContext.cs`, inside `OnModelCreating`, extend the `JuegoResumen` block with the questions navigation and add three entity blocks:
```csharp
        modelBuilder.Entity<JuegoResumen>(entity =>
        {
            entity.ToTable("sesion_juegos");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estadojuego").IsRequired();
            entity.HasMany(x => x.Preguntas).WithOne().HasForeignKey("juegoid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Preguntas).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PreguntaSnapshot>(entity =>
        {
            entity.ToTable("preguntas_snapshot");
            entity.HasKey(x => x.PreguntaId);
            entity.Property(x => x.PreguntaId).HasColumnName("preguntaid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.PuntajeAsignado).HasColumnName("puntajeasignado").IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimitesegundos").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estadopregunta").IsRequired();
            entity.Property(x => x.FechaActivacion).HasColumnName("fechaactivacion");
            entity.Property(x => x.FechaCierre).HasColumnName("fechacierre");
            entity.Property(x => x.MotivoCierre).HasColumnName("motivocierre");
            entity.Property(x => x.GanadorParticipanteId).HasColumnName("ganadorparticipanteid");
            entity.HasMany(x => x.Opciones).WithOne().HasForeignKey("preguntaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Opciones).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Respuestas).WithOne().HasForeignKey("preguntaid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Respuestas).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OpcionSnapshot>(entity =>
        {
            entity.ToTable("opciones_snapshot");
            entity.HasKey(x => x.OpcionId);
            entity.Property(x => x.OpcionId).HasColumnName("opcionid").ValueGeneratedNever();
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.EsCorrecta).HasColumnName("escorrecta").IsRequired();
        });

        modelBuilder.Entity<RespuestaTrivia>(entity =>
        {
            entity.ToTable("respuestas_trivia");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();
            entity.Property(x => x.OpcionId).HasColumnName("opcionid").IsRequired();
            entity.Property(x => x.EsCorrecta).HasColumnName("escorrecta").IsRequired();
            entity.Property(x => x.Instante).HasColumnName("instante").IsRequired();
        });
```

- [ ] **Step 3: Generate the migration**

Run (from `services/operaciones-sesion/`):
```bash
dotnet tool run dotnet-ef migrations add SP3cRuntimeTrivia \
  --project src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project src/Umbral.OperacionesSesion.Api
```
Expected: a new `Migrations/<timestamp>_SP3cRuntimeTrivia.cs` whose `Up` only `CreateTable`s `preguntas_snapshot`, `opciones_snapshot`, `respuestas_trivia` (additive; no `DropColumn`/`AlterColumn` on existing tables) and whose `Down` drops them. Verify by reading the file.

- [ ] **Step 4: Build to verify mappings compile**

Run: `dotnet build services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Umbral.OperacionesSesion.Api.csproj`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/ \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/TriviaSnapshotPersistenceTests.cs
git commit -m "SP-3c T12: mapeos EF de preguntas/opciones/respuestas + migración aditiva"
```

---

### Task 13: Carga eager del grafo Trivia en el repositorio

**Files:**
- Modify: `.../Infrastructure/Persistence/SesionPartidaRepository.cs`
- Test: (reusa `TriviaSnapshotPersistenceTests` de T12 — ahora debe pasar el round-trip completo)

**Interfaces:**
- Produces: `GetByPartidaIdAsync` con `Include`/`ThenInclude` que carga juegos→preguntas→opciones y preguntas→respuestas.

- [ ] **Step 1: Run the round-trip test to confirm it currently fails (lazy graph)**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj --filter TriviaSnapshotPersistenceTests`
Expected: FAIL (preguntas/opciones/respuestas no cargadas al releer en otro contexto).

- [ ] **Step 2: Extend the Include chain**

Replace `GetByPartidaIdAsync` in `SesionPartidaRepository.cs`:
```csharp
    public Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Inscripciones)
            .FirstOrDefaultAsync(s => s.PartidaId == partidaId, cancellationToken);
```

- [ ] **Step 3: Run the round-trip test to verify it passes**

Run: same as Step 1. Expected: PASS.

- [ ] **Step 4: Run the full integration project**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
Expected: PASS (existing persistence tests + new).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs
git commit -m "SP-3c T13: carga eager del grafo Trivia (juegos→preguntas→opciones/respuestas)"
```

---

### Task 14: Endpoints del controller + arms del middleware

**Files:**
- Modify: `.../Api/Controllers/SesionesController.cs` (+3 endpoints)
- Modify: `.../Api/Middleware/ExceptionHandlingMiddleware.cs` (+403, +nuevos 409)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerTriviaTests.cs`
- Test: extend `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`

**Interfaces:**
- Produces: `POST partidas/{partidaId:guid}/pregunta-actual/respuesta` (body `ResponderPreguntaRequest`), `POST .../pregunta-actual/avance`, `GET .../pregunta-actual`. Middleware: `ParticipanteNoInscritoException` → 403; `JuegoActivoNoEsTriviaException`/`NoHayPreguntaActivaException`/`RespuestaDuplicadaException`/`PreguntaFueraDeTiempoException`/`JuegoConPreguntasPendientesException` → 409.

- [ ] **Step 1: Write the failing tests**

```csharp
// SesionesControllerTriviaTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerTriviaTests
{
    private static SesionesController WithUser(ISender mediator, Guid sub)
    {
        var controller = new SesionesController(mediator);
        var ctx = new DefaultHttpContext();
        ctx.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim("sub", sub.ToString()) }, "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = ctx };
        return controller;
    }

    [Fact]
    public async Task Responder_dispatches_command_with_claim_sub_and_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var opcion = Guid.NewGuid();
        var mediator = new Mock<ISender>();
        mediator.Setup(m => m.Send(It.IsAny<ResponderPreguntaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RespuestaTriviaResponse(partidaId, Guid.NewGuid(), true, true, 10));
        var controller = WithUser(mediator.Object, sub);

        var result = await controller.Responder(partidaId, new ResponderPreguntaRequest(opcion), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mediator.Verify(m => m.Send(It.Is<ResponderPreguntaCommand>(
            c => c.PartidaId == partidaId && c.ParticipanteId == sub && c.OpcionId == opcion), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Avanzar_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var mediator = new Mock<ISender>();
        mediator.Setup(m => m.Send(It.IsAny<AvanzarPreguntaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvancePreguntaResponse(partidaId, 1, 2, false));
        var controller = WithUser(mediator.Object, Guid.NewGuid());

        Assert.IsType<OkObjectResult>(await controller.Avanzar(partidaId, CancellationToken.None));
    }

    [Fact]
    public async Task PreguntaActual_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var mediator = new Mock<ISender>();
        mediator.Setup(m => m.Send(It.IsAny<ObtenerPreguntaActualQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PreguntaActualDto(partidaId, Guid.NewGuid(), Guid.NewGuid(), 1, "Q", 30, DateTime.UtcNow,
                new System.Collections.Generic.List<OpcionPublicaDto>()));
        var controller = WithUser(mediator.Object, Guid.NewGuid());

        Assert.IsType<OkObjectResult>(await controller.ObtenerPreguntaActual(partidaId, CancellationToken.None));
    }
}
```
(Use the same Mock framework already used by existing `SesionesControllerTests` — match its `using`s; if it uses NSubstitute instead of Moq, mirror that style.)

Append to `ExceptionHandlingMiddlewareTests.cs`:
```csharp
    [Fact]
    public async Task Maps_participante_no_inscrito_to_403()
        => Assert.Equal((int)HttpStatusCode.Forbidden, await StatusFor(new ParticipanteNoInscritoException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_juego_activo_no_trivia_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new JuegoActivoNoEsTriviaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_no_hay_pregunta_activa_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new NoHayPreguntaActivaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_respuesta_duplicada_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new RespuestaDuplicadaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_pregunta_fuera_de_tiempo_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new PreguntaFueraDeTiempoException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_juego_con_preguntas_pendientes_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new JuegoConPreguntasPendientesException(Guid.NewGuid())));
```

- [ ] **Step 2: Run tests to verify they fail** — run the UnitTests project filtered to `SesionesControllerTriviaTests` and `ExceptionHandlingMiddlewareTests`. Expected: FAIL.

- [ ] **Step 3: Write minimal implementation**

In `SesionesController.cs`, add `using Umbral.OperacionesSesion.Application.DTOs;` and three endpoints before `ObtenerParticipanteId`:
```csharp
    [HttpPost("partidas/{partidaId:guid}/pregunta-actual/respuesta")]
    public async Task<IActionResult> Responder(Guid partidaId, [FromBody] ResponderPreguntaRequest request, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var response = await _mediator.Send(new ResponderPreguntaCommand(partidaId, participanteId, request.OpcionId), cancellationToken);
        return Ok(response);
    }

    [HttpPost("partidas/{partidaId:guid}/pregunta-actual/avance")]
    public async Task<IActionResult> Avanzar(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new AvanzarPreguntaCommand(partidaId), cancellationToken));

    [HttpGet("partidas/{partidaId:guid}/pregunta-actual")]
    public async Task<IActionResult> ObtenerPreguntaActual(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ObtenerPreguntaActualQuery(partidaId), cancellationToken));
```

In `ExceptionHandlingMiddleware.cs`, add the 403 arm and the new 409s. Update `MapStatus`:
```csharp
        ParticipanteNoIdentificadoException => HttpStatusCode.Unauthorized,
        ParticipanteNoInscritoException => HttpStatusCode.Forbidden,
        PartidaConfigNoEncontradaException
            or SesionNoEncontradaException
            or InscripcionNoEncontradaException => HttpStatusCode.NotFound,
        PartidasConfigInaccesibleException => HttpStatusCode.BadGateway,
        SesionYaPublicadaException
            or PartidaNoPublicableException
            or SesionNoEnLobbyException
            or ModalidadNoSoportadaException
            or ParticipanteYaInscritoException
            or ParticipacionActivaExistenteException
            or CupoLlenoException
            or ModoInicioNoCompatibleException
            or SesionNoIniciadaException
            or JuegoActivoNoEsTriviaException
            or NoHayPreguntaActivaException
            or RespuestaDuplicadaException
            or PreguntaFueraDeTiempoException
            or JuegoConPreguntasPendientesException => HttpStatusCode.Conflict,
        ValidationException or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
```
(`Umbral.OperacionesSesion.Domain.Exceptions` is already imported.)

- [ ] **Step 4: Run the full unit project** — `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerTriviaTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs
git commit -m "SP-3c T14: endpoints pregunta-actual (respuesta/avance/get) + arms 403/409 en middleware"
```

---

### Task 15: Contract tests end-to-end

**Files:**
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TriviaRuntimeEndpointsTests.cs`
- (Reusa `OperacionesSesionWebFactory` + `StubConfigClient` + `TestAuthHandler` existentes. **No** modificar `StubConfigClient.Default`.)

**Interfaces:**
- Consumes: `Stub.Respuestas[partidaId]` para registrar una config-con-preguntas; `CreateClientAs(participanteId)`; endpoints T14.

- [ ] **Step 1: Write the failing test**

```csharp
// TriviaRuntimeEndpointsTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class TriviaRuntimeEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    public TriviaRuntimeEndpointsTests(OperacionesSesionWebFactory factory) => _factory = factory;

    private static ConfiguracionPartidaDto TriviaConfig(Guid opcionCorrecta, Guid opcionIncorrecta)
    {
        var trivia = new TriviaConfigDto(new List<PreguntaConfigDto>
        {
            new(Guid.NewGuid(), "Capital de Francia?", 10, 30, new List<OpcionConfigDto>
            {
                new(opcionCorrecta, "Paris", true),
                new(opcionIncorrecta, "Londres", false),
            })
        });
        return new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia", trivia) });
    }

    [Fact]
    public async Task Full_trivia_flow_publish_start_answer_advance_finish()
    {
        var partidaId = Guid.NewGuid();
        var correcta = Guid.NewGuid();
        var incorrecta = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = TriviaConfig(correcta, incorrecta);

        var operador = _factory.CreateClientAs(Guid.NewGuid());
        var participante = _factory.CreateClientAs(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Created, (await operador.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await participante.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await operador.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        // pregunta actual visible y sin esCorrecta
        var pregunta = await participante.GetFromJsonAsync<PreguntaActualDto>($"/partidas/{partidaId}/pregunta-actual");
        Assert.NotNull(pregunta);
        Assert.Equal(1, pregunta!.Orden);
        Assert.Equal(2, pregunta.Opciones.Count);

        // respuesta correcta cierra la pregunta
        var resp = await participante.PostAsJsonAsync($"/partidas/{partidaId}/pregunta-actual/respuesta", new ResponderPreguntaRequest(correcta));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<RespuestaTriviaResponse>();
        Assert.True(body!.EsCorrecta);
        Assert.True(body.CerroPregunta);
        Assert.Equal(10, body.Puntaje);

        // avanzar: era la última pregunta → sinMasPreguntas
        var avance = await operador.PostAsync($"/partidas/{partidaId}/pregunta-actual/avance", null);
        Assert.Equal(HttpStatusCode.OK, avance.StatusCode);
        var av = await avance.Content.ReadFromJsonAsync<AvancePreguntaResponse>();
        Assert.True(av!.SinMasPreguntas);

        // finalizar el juego → única partida → Terminada
        var fin = await operador.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.OK, fin.StatusCode);
    }

    [Fact]
    public async Task Answer_without_inscription_returns_403()
    {
        var partidaId = Guid.NewGuid();
        var correcta = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = TriviaConfig(correcta, Guid.NewGuid());

        var operador = _factory.CreateClientAs(Guid.NewGuid());
        var intruso = _factory.CreateClientAs(Guid.NewGuid());

        await operador.PostAsync($"/partidas/{partidaId}/publicacion", null);
        // sin inscribir al intruso; iniciar requiere mínimos=1 → inscribe a alguien más:
        var jugador = _factory.CreateClientAs(Guid.NewGuid());
        await jugador.PostAsync($"/partidas/{partidaId}/inscripciones", null);
        await operador.PostAsync($"/partidas/{partidaId}/inicio", null);

        var resp = await intruso.PostAsJsonAsync($"/partidas/{partidaId}/pregunta-actual/respuesta", new ResponderPreguntaRequest(correcta));
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Finalizar_with_open_question_returns_409()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = TriviaConfig(Guid.NewGuid(), Guid.NewGuid());
        var operador = _factory.CreateClientAs(Guid.NewGuid());
        var jugador = _factory.CreateClientAs(Guid.NewGuid());
        await operador.PostAsync($"/partidas/{partidaId}/publicacion", null);
        await jugador.PostAsync($"/partidas/{partidaId}/inscripciones", null);
        await operador.PostAsync($"/partidas/{partidaId}/inicio", null);

        // pregunta sigue abierta → no se puede finalizar el juego
        var fin = await operador.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.Conflict, fin.StatusCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail (or pass once wiring is correct)** — `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj --filter TriviaRuntimeEndpointsTests`. Expected initially: FAIL until the endpoints (T14) and graph load (T13) are present — by this task they are, so iterate any wiring issues until PASS.

- [ ] **Step 3: Fix any wiring gaps** — if a test fails, read the failure and correct only the contract test or a genuine wiring bug (do NOT change `StubConfigClient.Default`). Common gaps: MediatR/validator registration auto-discovers new handlers/validators (assembly scan) so no DI change needed; confirm.

- [ ] **Step 4: Run the full contract project** — `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`. Expected: PASS (existing SP-3a/3b contract tests + new Trivia).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TriviaRuntimeEndpointsTests.cs
git commit -m "SP-3c T15: contract tests end-to-end de runtime Trivia"
```

---

### Task 16: Contratos + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md` (3 endpoints + DTOs)
- Modify: `contracts/events/operaciones-sesion-events.md` (4 eventos Trivia)
- Modify: `docs/04-sdd/traceability-matrix.md` (fila SP-3c)

**Interfaces:** documentación; sin código.

- [ ] **Step 1: Update the HTTP contract**

In `contracts/http/operaciones-sesion-api.md`, add to the Endpoint Registry table:
```
| Answer active question | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/respuesta` | Participante | 200 + RespuestaTriviaResponse | 401 sin identidad · 403 no inscrito · 404 sesión no existe · 409 no iniciada / juego no Trivia / sin pregunta activa / duplicada / fuera de tiempo |
| Advance current question | POST | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual/avance` | Operador | 200 + AvancePreguntaResponse | 404 · 409 no iniciada / juego no Trivia / sin pregunta activa |
| Current question | GET | `/operaciones-sesion/partidas/{partidaId}/pregunta-actual` | Operador/Participante | 200 + PreguntaActualDto | 404 sesión no existe · 409 sin pregunta activa |
```
and to the DTOs list:
```
- `RespuestaTriviaResponse { partidaId, preguntaId, esCorrecta, cerroPregunta, puntaje? }`
- `AvancePreguntaResponse { partidaId, preguntaCerradaOrden, preguntaActivadaOrden?, sinMasPreguntas }`
- `PreguntaActualDto { partidaId, juegoId, preguntaId, orden, texto, tiempoLimiteSegundos, fechaActivacion, opciones[]{ opcionId, texto } }` (participant-safe; nunca `esCorrecta`)
```
and a note: request body of respuesta is `{ opcionId }`; `participanteId` from the JWT `sub`.

- [ ] **Step 2: Update the events contract**

In `contracts/events/operaciones-sesion-events.md`, register:
```
| RespuestaTriviaValidada (SP-3c) | Cada respuesta registrada | Registered | { partidaId, sesionPartidaId, juegoId, preguntaId, participanteId, opcionId, esCorrecta, instante } |
| PuntajeTriviaIncrementado (SP-3c) | Primera respuesta correcta | Registered | { partidaId, sesionPartidaId, juegoId, preguntaId, participanteId, puntaje, tiempoRespuestaMs } |
| PreguntaTriviaActivada (SP-3c) | Se activa una pregunta (inicio de juego o avance) | Registered | { partidaId, sesionPartidaId, juegoId, preguntaId, orden, tiempoLimiteSegundos, fechaActivacion } |
| PreguntaTriviaCerrada (SP-3c) | Se cierra una pregunta | Registered | { partidaId, sesionPartidaId, juegoId, preguntaId, motivo, fechaCierre, ganadorParticipanteId? } |
```
Note: emitted via `NoOpSesionEventsPublisher`; `RankingTriviaActualizado` queda en Puntuaciones (SP-4).

- [ ] **Step 3: Update the traceability matrix**

Add a row to `docs/04-sdd/traceability-matrix.md`:
```
| Runtime Trivia Individual (SP-3c) | Validar respuestas en vivo de un JuegoTrivia activo (primera-correcta cierra; ventana de tiempo), avance operador-dirigido de preguntas, agotar→FinalizarJuegoActual; snapshot de preguntas al publicar; eventos por puerto No-Op | Operaciones de Sesión | Partidas (snapshot de preguntas vía HTTP, solo lectura); Puntuaciones consume eventos en SP-4 | docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md · docs/superpowers/plans/2026-06-27-sp3c-runtime-trivia.md | contracts/http/operaciones-sesion-api.md · contracts/events/operaciones-sesion-events.md | Implemented — suite verde. **Diferido:** Equipo Trivia→SP-3a-E, barrido automático de timeout + SignalR→SP-3f, runtime BDT→SP-3d, scoring/ranking real→SP-4. |
```

- [ ] **Step 4: Run the FULL suite to confirm everything green**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS — all unit + integration + contract green.

- [ ] **Step 5: Commit**

```bash
git add contracts/http/operaciones-sesion-api.md \
        contracts/events/operaciones-sesion-events.md \
        docs/04-sdd/traceability-matrix.md
git commit -m "SP-3c T16: contratos HTTP/evento + traceability de runtime Trivia"
```

---

## Self-Review

**Spec coverage:**
- §1 alcance Individual + diferimientos → respetado (sin Equipo/BDT/scheduler/SignalR; question-less Trivia compat).
- §2 snapshot-at-publish → T6; avance operador + ventana → T4/T8/T9; eventos No-Op post-save → T5/T8/T9/T11; TimeProvider → todos los handlers.
- §3 modelo (enums, PreguntaSnapshot, OpcionSnapshot, RespuestaTrivia, JuegoResumen) → T1/T2/T3; invariantes (un Activo, dedup, no-respuestas-en-cerrada, ganador sólo en correcta) → T2/T3/T4.
- §4 transiciones (ResponderPregunta, AvanzarPregunta, guard finalización) → T4; emisión → T8/T9/T11.
- §5 API 3 endpoints + DTOs participant-safe → T7/T14; 200/403/409 → T14.
- §6 4 eventos → T5, emitidos T8/T9/T11.
- §7 excepciones (403 + 5×409) → T2/T4 (dominio) + T14 (middleware).
- §8 persistencia/migración aditiva + Include → T12/T13.
- §9 pruebas dominio/handler/controller/contract → T2–T15.
- §10 doctrina (estructura graduada, límites, R1) → respetado por construcción.
- §11 watch-items (concurrencia 3f, git-cleanup) → Global Constraints.
- §12 traceability → T16.

**Placeholder scan:** sin TBD/TODO; cada paso de código lleva código real; tests con asserts reales.

**Type consistency:** `ResultadoRespuesta` (9 campos) usado consistente en T1/T2/T4/T8; `ResultadoAvancePregunta` (9 campos) en T1/T4/T9; nombres de eventos y métodos del publisher idénticos en T5/T8/T9/T11; `JuegoResumen` ctor de 4 args y `Activar(now)` consistentes T3→T4/T6; `PreguntaActualDto`/`OpcionPublicaDto` consistentes T7/T10/T15.

**Nota de orden T12→T13:** el round-trip de T12 sólo pasa tras el `Include` de T13; documentado en T12 Step 1 Note. Ambos deben quedar verdes al cerrar T13.
