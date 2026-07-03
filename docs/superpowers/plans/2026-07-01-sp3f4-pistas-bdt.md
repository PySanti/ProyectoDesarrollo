# SP-3f-4 — Pistas BDT (operador → participante, evento vía seam) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** El operador envía una pista de texto a un participante específico durante un `JuegoBDT` activo; se entrega en tiempo real **solo** a ese participante vía SignalR, como evento a través del seam existente (sin persistir).

**Architecture:** Acción operador HTTP → `EnviarPistaCommand` (MediatR) → handler carga `SesionPartida`, valida BR-B06 con el método de dominio read-only `PrepararPista` (retorna el `JuegoId` del BDT activo), **no** persiste, y publica `PistaEnviadaEvent` por `ISesionEventsPublisher`. El composite hace fan-out a No-Op (registro futuro/broker, diferido) + SignalR. `SignalRSesionEventsPublisher` difunde el mensaje `PistaEnviada` al grupo por-participante `participante:{destinoId}` (NO al grupo de partida). El participante se auto-une a `participante:{id}` en `SuscribirAPartida`, igual que el patrón de grupo por-rol de SP-3f-3.

**Tech Stack:** .NET 8, Clean Architecture + CQRS/MediatR, FluentValidation, SignalR, EF Core 8 (sin migración en este slice), xUnit con fakes a mano (sin Moq).

## Global Constraints

- **Servicio:** todo el trabajo vive en `services/operaciones-sesion/`. Backend-only.
- **Event-only, sin persistencia:** NO se persiste la entidad `Pista`, NO hay migración EF, NO hay GET de historial. El "registro" (BR-B06) lo materializa audit vía broker RabbitMQ — diferido a otra slice.
- **Acción operador:** el endpoint es operador-triggered (mismo patrón que `AvanzarEtapa`/`AvanzarPregunta`); **no** toma `participanteId` del claim. El `participanteDestinoId` viaja en el body.
- **Entrega aislada:** la pista se entrega **solo** a `participante:{destinoId}` — nunca a `partida:{id}` ni al operador. Solo el destinatario la recibe (BR-B06 "específico").
- **Fakes a mano, sin Moq.** TDD: escribir test que falla → correr y ver que falla → implementar mínimo → correr y ver verde → commit.
- **`TimeProvider` inyectado** (registrado en DI); server-stampea el timestamp. IDs value-object vía `.Valor`; enums vía `.ToString()`.
- **El método 14 de `ISesionEventsPublisher` rompe compilación en 5 implementadores** si falta: `SignalRSesionEventsPublisher`, `NoOpSesionEventsPublisher`, `CompositeSesionEventsPublisher`, `FakeSesionEventsPublisher` (test), y `NoOpBase` (clase abstracta dentro de `CompositeSesionEventsPublisherTests`). Los 5 deben actualizarse en la Tarea 2.
- **Carve-out git (NO commitear):** `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. La Tarea 6 **escribe** la fila de traceability pero **no la commitea**. `git add` SOLO archivos nombrados exactos — nunca `git add -A`/`.`/`docs/`. Prohibido `git checkout`/`restore`/`clean`/`stash`/`reset` de rango amplio.
- **Trailer de commit:** cada commit termina con exactamente `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (sin línea de sesión).
- **Comando de test:** `dotnet test <ruta-de-un-solo-.csproj>`. NO pasar dos rutas de proyecto en un solo comando (falla `MSB1008`). Correr UnitTests y ContractTests por separado.

**Rutas de proyecto (para los comandos):**
- Unit: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
- Contract: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`

**Firmas clave (contrato entre tareas):**
- Tarea 1 produce: `SesionRealtimeMessages.PistaEnviada` (const, valor wire = `"PistaEnviada"`); `SesionRealtimeMessages.GrupoParticipante(Guid) => "participante:{id}"`; `PistaEnviadaPayload(Guid PartidaId, Guid JuegoId, Guid ParticipanteDestinoId, string Texto, DateTime TimestampUtc)`.
- Tarea 2 produce: `PistaEnviadaEvent(Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid ParticipanteDestinoId, string Texto, DateTime Instante)` (en `BdtRuntimeEvents.cs`); `ISesionEventsPublisher.PublicarPistaEnviadaAsync(PistaEnviadaEvent, CancellationToken)`; `FakeSesionEventsPublisher.PistasEnviadas` (lista capturada).
- Tarea 3 produce: `SesionPartida.PrepararPista(Guid participanteDestinoId) → Guid` (JuegoId del BDT activo; read-only, no persiste).
- Tarea 4 produce: `EnviarPistaCommand(Guid PartidaId, Guid ParticipanteDestinoId, string Texto) : IRequest<PistaEnviadaResponse>`; `EnviarPistaRequest(Guid ParticipanteDestinoId, string Texto)`; `PistaEnviadaResponse(Guid PartidaId, Guid JuegoId, Guid ParticipanteDestinoId, DateTime TimestampUtc)`; `EnviarPistaCommandHandler`; `EnviarPistaCommandValidator`.
- Tarea 5 produce: endpoint `POST /operaciones-sesion/partidas/{partidaId}/pistas` (`SesionesController.EnviarPista`).

---

### Task 1: Declaraciones Realtime + grupo por-participante en el hub

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs`

**Interfaces:**
- Consumes: existentes `SesionRealtimeMessages.GrupoPartida`, `GrupoOperadorPartida`; `SesionHub` con `Context.Items[ClaveParticipanteId]` poblado por la rama participante (SP-3f-3).
- Produces: `SesionRealtimeMessages.PistaEnviada`, `SesionRealtimeMessages.GrupoParticipante(Guid)`, `PistaEnviadaPayload`; el participante suscrito queda en el grupo `participante:{id}`; `DesuscribirDePartida` lo retira.

- [ ] **Step 1: Añadir tests al hub y al helper de mensajes (fallan a compilar/rojo)**

En `SesionRealtimeMessagesTests.cs`, añadir dentro de la clase:

```csharp
    [Fact]
    public void GrupoParticipante_tiene_formato_estable()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Assert.Equal("participante:22222222-2222-2222-2222-222222222222",
            SesionRealtimeMessages.GrupoParticipante(id));
    }

    [Fact]
    public void GrupoParticipante_difiere_de_los_otros_grupos()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(SesionRealtimeMessages.GrupoPartida(id), SesionRealtimeMessages.GrupoParticipante(id));
        Assert.NotEqual(SesionRealtimeMessages.GrupoOperadorPartida(id), SesionRealtimeMessages.GrupoParticipante(id));
    }
```

En `SesionHubTests.cs`, añadir dentro de la clase `SesionHubTests` (los fakes `FakeGroupManager`/`FakeClients`/`FakeHubCallerContext` y el helper `Construir` ya existen y se reutilizan):

```csharp
    [Fact]
    public async Task Inscrito_se_une_al_grupo_participante()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Added);
    }

    [Fact]
    public async Task Operador_no_se_une_a_ningun_grupo_participante()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.DoesNotContain(groups.Added, x => x.Group.StartsWith("participante:"));
    }

    [Fact]
    public async Task Desuscribir_quita_al_participante_de_su_grupo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId); // puebla Context.Items[participanteId]
        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Removed);
    }
```

- [ ] **Step 2: Correr los tests y verificar que fallan (a compilar)**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`PistaEnviada`, `GrupoParticipante` no existen).

- [ ] **Step 3: Añadir const y helper en `SesionRealtimeMessages.cs`**

Añadir la const junto a las demás (tras `UbicacionActualizada`) y el helper junto a los otros helpers de grupo:

```csharp
    public const string PistaEnviada = nameof(PistaEnviada);
```

```csharp
    public static string GrupoParticipante(Guid participanteId) => $"participante:{participanteId}";
```

- [ ] **Step 4: Añadir el payload en `SesionRealtimePayloads.cs`**

Añadir al final del archivo:

```csharp
public sealed record PistaEnviadaPayload(Guid PartidaId, Guid JuegoId, Guid ParticipanteDestinoId, string Texto, DateTime TimestampUtc);
```

- [ ] **Step 5: Unir/retirar al participante del grupo en `SesionHub.cs`**

En la rama participante de `SuscribirAPartida`, tras la línea que une a `GrupoPartida`, añadir la unión al grupo participante. La rama queda:

```csharp
        Context.Items[ClavePartidaId] = partidaId;
        Context.Items[ClaveParticipanteId] = participanteId;
        await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoParticipante(participanteId), Context.ConnectionAborted);
```

En `DesuscribirDePartida`, tras retirar de los dos grupos existentes, retirar del grupo participante usando el `participanteId` guardado en `Context.Items` (el operador no lo tiene, así que la remoción es condicional):

```csharp
    public async Task DesuscribirDePartida(Guid partidaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoOperadorPartida(partidaId), Context.ConnectionAborted);
        if (Context.Items.TryGetValue(ClaveParticipanteId, out var u) && u is Guid participanteId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoParticipante(participanteId), Context.ConnectionAborted);
        }
    }
```

- [ ] **Step 6: Correr los tests y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (todos, incluidos los 3 nuevos del hub y los 2 del helper). Los tests de ubicación/suscripción existentes siguen verdes (cambios aditivos).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-4 T1: const PistaEnviada + grupo participante-scoped + payload; hub une/retira participante:{id}

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Evento `PistaEnviada` + método 14 del seam en las 5 impls + tests SignalR/Composite

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs` (incluye la clase `NoOpBase` que debe ganar el método 14)

**Interfaces:**
- Consumes: de Tarea 1 → `SesionRealtimeMessages.PistaEnviada`, `GrupoParticipante`, `PistaEnviadaPayload`.
- Produces: `PistaEnviadaEvent`; `ISesionEventsPublisher.PublicarPistaEnviadaAsync`; `FakeSesionEventsPublisher.PistasEnviadas`.

- [ ] **Step 1: Escribir los tests SignalR y Composite (fallan a compilar/rojo)**

En `SignalRSesionEventsPublisherTests.cs`, añadir dentro de la clase (reutiliza el helper `Build()` y `T0` existentes):

```csharp
    [Fact]
    public async Task PistaEnviada_difunde_solo_al_grupo_del_participante_destino()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var destino = Guid.NewGuid();

        await pub.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(partidaId, Guid.NewGuid(), juegoId, destino, "Mira bajo el faro", T0),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoParticipante(destino), clients.LastGroup); // NO GrupoPartida
        Assert.Equal(SesionRealtimeMessages.PistaEnviada, clients.Proxy.Method);
        var payload = Assert.IsType<PistaEnviadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(juegoId, payload.JuegoId);
        Assert.Equal(destino, payload.ParticipanteDestinoId);
        Assert.Equal("Mira bajo el faro", payload.Texto);
        Assert.Equal(T0, payload.TimestampUtc);
    }
```

En `CompositeSesionEventsPublisherTests.cs`:
1. Añadir el campo y override en la clase `RecordingPublisher`:

```csharp
        public int Pistas;
        public override Task PublicarPistaEnviadaAsync(PistaEnviadaEvent e, CancellationToken ct)
        { Pistas++; return Task.CompletedTask; }
```

2. Añadir el método virtual no-op en la clase abstracta `NoOpBase` (junto a los otros 13):

```csharp
        public virtual Task PublicarPistaEnviadaAsync(PistaEnviadaEvent e, CancellationToken ct) => Task.CompletedTask;
```

3. Añadir el test de fan-out dentro de la clase:

```csharp
    [Fact]
    public async Task Pista_fan_out_invoca_a_todos()
    {
        var a = new RecordingPublisher();
        var b = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { a, b }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "pista", T0),
            CancellationToken.None);

        Assert.Equal(1, a.Pistas);
        Assert.Equal(1, b.Pistas);
    }
```

- [ ] **Step 2: Correr y verificar que falla (a compilar)**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`PistaEnviadaEvent`, `PublicarPistaEnviadaAsync` no existen).

- [ ] **Step 3: Añadir el record de evento en `BdtRuntimeEvents.cs`**

Añadir al final del archivo:

```csharp
public sealed record PistaEnviadaEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, Guid ParticipanteDestinoId,
    string Texto, DateTime Instante);
```

- [ ] **Step 4: Añadir el método 14 a `ISesionEventsPublisher.cs`**

Añadir dentro de la interfaz (tras el último método):

```csharp
    Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken);
```

- [ ] **Step 5: Implementar en No-Op y Composite**

En `NoOpSesionEventsPublisher.cs`, añadir:

```csharp
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;
```

En `CompositeSesionEventsPublisher.cs`, añadir junto a los demás fan-out:

```csharp
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPistaEnviadaAsync(evento, cancellationToken));
```

- [ ] **Step 6: Implementar en `SignalRSesionEventsPublisher.cs` (push al grupo participante, NO vía `Difundir`)**

`Difundir` apunta a `GrupoPartida`; la pista NO usa `Difundir`. Añadir el método explícito:

```csharp
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) =>
        _hub.Clients.Group(SesionRealtimeMessages.GrupoParticipante(evento.ParticipanteDestinoId))
            .SendAsync(
                SesionRealtimeMessages.PistaEnviada,
                new PistaEnviadaPayload(evento.PartidaId, evento.JuegoId, evento.ParticipanteDestinoId, evento.Texto, evento.Instante),
                cancellationToken);
```

- [ ] **Step 7: Implementar en `FakeSesionEventsPublisher.cs` (capturar para asertar)**

Añadir el campo y el método (junto a los grupos BDT existentes):

```csharp
    public List<PistaEnviadaEvent> PistasEnviadas { get; } = new();

    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken)
    { PistasEnviadas.Add(evento); return Task.CompletedTask; }
```

- [ ] **Step 8: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS. El proyecto de test vuelve a compilar (las 5 impls tienen el método 14) y los 2 tests nuevos pasan.

- [ ] **Step 9: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-4 T2: PistaEnviadaEvent + seam método 14 (5 impls) + SignalR push a participante:{destino}

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Dominio — `SesionPartida.PrepararPista` (read-only, enforced BR-B06)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaBdtTests.cs`

**Interfaces:**
- Consumes: helpers privados existentes `JuegoBDTActivo()` (lanza `SesionNoIniciadaException` si no iniciada / `JuegoActivoNoEsBDTException` si el juego activo no es BDT); `_inscripciones`, `EsActiva`; `JuegoResumen.EtapaActiva`, `.JuegoId`. Excepciones existentes: `ParticipanteNoInscritoException(Guid)`, `NoHayEtapaActivaException(Guid)`, `JuegoActivoNoEsBDTException(Guid)`.
- Produces: `Guid PrepararPista(Guid participanteDestinoId)` — retorna el `JuegoId` del BDT activo. **No** muta estado, **no** requiere `SaveChanges`.

Nota de diseño: la firma tentativa del spec incluía `DateTime now`, pero la validación BR-B06 (destino inscrito + BDT activo + etapa activa) no usa el reloj, así que se **omite** `now` (YAGNI; el `now` del evento lo server-stampea el handler). El orden de chequeos espeja `ValidarTesoro`: inscrito → BDT activo → etapa activa.

- [ ] **Step 1: Escribir los tests de dominio (rojo)**

En `SesionPartidaBdtTests.cs`, añadir un helper para construir una sesión Trivia-activa (para el caso "juego activo no es BDT") y los 4 tests. Añadir dentro de la clase `SesionPartidaBdtTests`:

```csharp
    private static SesionPartida SesionTriviaIniciada(Guid participante)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30, new[] { ok });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snapshot = new ConfiguracionSnapshot(
            "Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snapshot);
        var now = new DateTime(2026, 6, 28, 10, 0, 0);
        sesion.Inscribir(participante, false, 0, now);
        sesion.Iniciar(now);
        return sesion;
    }

    [Fact]
    public void PrepararPista_con_bdt_activo_y_destino_inscrito_retorna_juegoId()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        var juegoId = sesion.Juegos.Single().JuegoId;

        var r = sesion.PrepararPista(jugador);

        Assert.Equal(juegoId, r);
    }

    [Fact]
    public void PrepararPista_destino_no_inscrito_lanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));

        Assert.Throws<ParticipanteNoInscritoException>(() => sesion.PrepararPista(Guid.NewGuid()));
    }

    [Fact]
    public void PrepararPista_juego_activo_no_es_bdt_lanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionTriviaIniciada(jugador);

        Assert.Throws<JuegoActivoNoEsBDTException>(() => sesion.PrepararPista(jugador));
    }

    [Fact]
    public void PrepararPista_bdt_sin_etapa_activa_lanza()
    {
        var jugador = Guid.NewGuid();
        var sesion = SesionBdtIniciada(jugador, ("QR-1", 60));
        // ganar la única etapa deja el BDT activo pero sin etapa activa (no finaliza el juego)
        sesion.ValidarTesoro(jugador, Img("QR-1"), new DateTime(2026, 6, 28, 10, 0, 5), new TextoQrDecoder());
        Assert.Null(sesion.Juegos.Single().EtapaActiva);

        Assert.Throws<NoHayEtapaActivaException>(() => sesion.PrepararPista(jugador));
    }
```

(Los `using` `System.Linq`, `Domain.Entities`, `Domain.Enums`, `Domain.Exceptions`, `Domain.ValueObjects` ya están en el archivo. `SesionBdtIniciada`, `Img`, `TextoQrDecoder` ya existen.)

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`PrepararPista` no existe).

- [ ] **Step 3: Implementar `PrepararPista` en `SesionPartida.cs`**

Añadir el método público (p. ej. tras `AvanzarEtapa`, junto a las demás operaciones BDT):

```csharp
    public Guid PrepararPista(Guid participanteDestinoId)
    {
        if (!_inscripciones.Any(i => i.ParticipanteId == participanteDestinoId && i.EsActiva))
            throw new ParticipanteNoInscritoException(participanteDestinoId);
        var juego = JuegoBDTActivo();
        _ = juego.EtapaActiva ?? throw new NoHayEtapaActivaException(PartidaId);
        return juego.JuegoId;
    }
```

- [ ] **Step 4: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (los 4 nuevos + toda la suite).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaBdtTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-4 T3: SesionPartida.PrepararPista read-only (BR-B06: destino inscrito + BDT activo + etapa activa)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Command + DTO + Validator + Handler

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/EnviarPistaCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/EnviarPistaCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/EnviarPistaCommandHandler.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/BdtRuntimeDtos.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaCommandHandlerTests.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaCommandValidatorTests.cs`

**Interfaces:**
- Consumes: de Tarea 2 → `PistaEnviadaEvent`, `ISesionEventsPublisher.PublicarPistaEnviadaAsync`, `FakeSesionEventsPublisher.PistasEnviadas`; de Tarea 3 → `SesionPartida.PrepararPista`. Existentes: `ISesionPartidaRepository.GetByPartidaIdAsync`, `SesionNoEncontradaException`, `TimeProvider`, `BdtBuilder.SesionIniciada`, `FakeSesionPartidaRepository`, `FakeTimeProvider`.
- Produces: `EnviarPistaCommand`, `EnviarPistaRequest`, `PistaEnviadaResponse`, `EnviarPistaCommandHandler`, `EnviarPistaCommandValidator`.

- [ ] **Step 1: Escribir tests de handler y validator (rojo)**

Crear `EnviarPistaCommandHandlerTests.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EnviarPistaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 28, 10, 0, 5, DateTimeKind.Utc);

    [Fact]
    public async Task Publica_pista_enviada_con_campos_correctos()
    {
        var (repo, _, fake, partidaId, jugador) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new EnviarPistaCommandHandler(repo, fake, new FakeTimeProvider(T0));

        var resp = await handler.Handle(new EnviarPistaCommand(partidaId, jugador, "Mira el faro"), default);

        var evt = Assert.Single(fake.PistasEnviadas);
        Assert.Equal(partidaId, evt.PartidaId);
        Assert.Equal(jugador, evt.ParticipanteDestinoId);
        Assert.Equal("Mira el faro", evt.Texto);
        Assert.Equal(T0, evt.Instante);
        Assert.NotEqual(Guid.Empty, evt.JuegoId);
        Assert.Equal(partidaId, resp.PartidaId);
        Assert.Equal(jugador, resp.ParticipanteDestinoId);
        Assert.Equal(evt.JuegoId, resp.JuegoId);
        Assert.Equal(T0, resp.TimestampUtc);
    }

    [Fact]
    public async Task Destino_no_inscrito_propaga_sin_publicar()
    {
        var (repo, _, fake, partidaId, _) = BdtBuilder.SesionIniciada(("QR-1", 60));
        var handler = new EnviarPistaCommandHandler(repo, fake, new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<ParticipanteNoInscritoException>(
            () => handler.Handle(new EnviarPistaCommand(partidaId, Guid.NewGuid(), "x"), default));

        Assert.Empty(fake.PistasEnviadas);
    }

    [Fact]
    public async Task Sesion_inexistente_lanza()
    {
        var repo = new FakeSesionPartidaRepository();
        var fake = new FakeSesionEventsPublisher();
        var handler = new EnviarPistaCommandHandler(repo, fake, new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "x"), default));
    }
}
```

Crear `EnviarPistaCommandValidatorTests.cs`:

```csharp
using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class EnviarPistaCommandValidatorTests
{
    private readonly EnviarPistaCommandValidator _v = new();

    [Fact]
    public void Valido() =>
        Assert.True(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "Mira el faro")).IsValid);

    [Fact]
    public void Texto_vacio_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "")).IsValid);

    [Fact]
    public void Texto_whitespace_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), "   ")).IsValid);

    [Fact]
    public void Texto_muy_largo_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 501))).IsValid);

    [Fact]
    public void Destino_vacio_es_invalido() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.NewGuid(), Guid.Empty, "hola")).IsValid);

    [Fact]
    public void Partida_vacia_es_invalida() =>
        Assert.False(_v.Validate(new EnviarPistaCommand(Guid.Empty, Guid.NewGuid(), "hola")).IsValid);
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`EnviarPistaCommand`, `EnviarPistaCommandHandler`, `EnviarPistaCommandValidator` no existen).

- [ ] **Step 3: Crear el command y los DTOs**

Crear `EnviarPistaCommand.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
namespace Umbral.OperacionesSesion.Application.Commands;
public sealed record EnviarPistaCommand(Guid PartidaId, Guid ParticipanteDestinoId, string Texto) : IRequest<PistaEnviadaResponse>;
```

En `BdtRuntimeDtos.cs`, añadir al final:

```csharp
public sealed record EnviarPistaRequest(Guid ParticipanteDestinoId, string Texto);
public sealed record PistaEnviadaResponse(Guid PartidaId, Guid JuegoId, Guid ParticipanteDestinoId, DateTime TimestampUtc);
```

- [ ] **Step 4: Crear el validator**

Crear `EnviarPistaCommandValidator.cs`:

```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;
namespace Umbral.OperacionesSesion.Application.Validators;
public sealed class EnviarPistaCommandValidator : AbstractValidator<EnviarPistaCommand>
{
    public EnviarPistaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.ParticipanteDestinoId).NotEmpty();
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(500);
    }
}
```

(`NotEmpty()` de FluentValidation rechaza null/empty/whitespace para strings; el test de whitespace lo verifica.)

- [ ] **Step 5: Crear el handler (read-only: sin `SaveChanges`)**

Crear `EnviarPistaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class EnviarPistaCommandHandler : IRequestHandler<EnviarPistaCommand, PistaEnviadaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public EnviarPistaCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<PistaEnviadaResponse> Handle(EnviarPistaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var juegoId = sesion.PrepararPista(request.ParticipanteDestinoId); // enforced BR-B06; lanza antes de publicar

        await _events.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(
                sesion.PartidaId, sesion.Id.Valor, juegoId, request.ParticipanteDestinoId, request.Texto, now),
            cancellationToken);

        return new PistaEnviadaResponse(sesion.PartidaId, juegoId, request.ParticipanteDestinoId, now);
    }
}
```

- [ ] **Step 6: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS (handler + validator + suite completa). El `ValidationBehavior` de MediatR auto-descubre el validator; no requiere registro manual.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/EnviarPistaCommand.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/EnviarPistaCommandValidator.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/EnviarPistaCommandHandler.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/BdtRuntimeDtos.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaCommandHandlerTests.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/EnviarPistaCommandValidatorTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-4 T4: EnviarPistaCommand + request/response DTO + validator + handler (read-only, publica PistaEnviadaEvent)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Endpoint del controller `POST .../pistas`

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerBdtTests.cs`

**Interfaces:**
- Consumes: de Tarea 4 → `EnviarPistaCommand`, `EnviarPistaRequest`, `PistaEnviadaResponse`. Existentes: `ISender _mediator`, patrón `FakeSender`, `WithUser`.
- Produces: acción `EnviarPista(Guid partidaId, EnviarPistaRequest request, CancellationToken)`.

- [ ] **Step 1: Escribir el test del controller (rojo)**

En `SesionesControllerBdtTests.cs`, añadir dentro de la clase:

```csharp
    [Fact]
    public async Task Enviar_pista_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var destino = Guid.NewGuid();
        var sender = new FakeSender(new PistaEnviadaResponse(partidaId, Guid.NewGuid(), destino,
            new DateTime(2026, 6, 28, 10, 0, 0, DateTimeKind.Utc)));
        var controller = WithUser(sender, Guid.NewGuid());

        var result = await controller.EnviarPista(partidaId, new EnviarPistaRequest(destino, "Mira el faro"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var cmd = Assert.IsType<EnviarPistaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(destino, cmd.ParticipanteDestinoId);
        Assert.Equal("Mira el faro", cmd.Texto);
    }
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FAIL — no compila (`controller.EnviarPista` no existe).

- [ ] **Step 3: Añadir el endpoint (acción operador; NO toma participanteId del claim)**

En `SesionesController.cs`, añadir junto a las demás acciones BDT (p. ej. tras `AvanzarEtapa`):

```csharp
    [HttpPost("partidas/{partidaId:guid}/pistas")]
    public async Task<IActionResult> EnviarPista(Guid partidaId, [FromBody] EnviarPistaRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new EnviarPistaCommand(partidaId, request.ParticipanteDestinoId, request.Texto), cancellationToken));
```

(Los `using` de `Application.Commands` y `Application.DTOs` ya están en el archivo.)

- [ ] **Step 4: Correr y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerBdtTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-4 T5: endpoint POST /partidas/{id}/pistas (acción operador → EnviarPistaCommand)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Contrato (HTTP + Realtime) + test doc↔constantes + traceability (carve-out)

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs`
- Modify (WRITE, **NO commitear** — carve-out): `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: de Tarea 1 → `SesionRealtimeMessages.PistaEnviada` (para el `nameof` del test); las firmas de payload/endpoint de Tareas 1/4/5.
- Produces: contrato documentado; `RealtimeContractTests` verifica `PistaEnviada`.

- [ ] **Step 1: Añadir el InlineData al test de contrato (rojo)**

En `RealtimeContractTests.cs`, añadir dentro del `[Theory] Cada_mensaje_del_codigo_esta_documentado`, tras la línea de `UbicacionActualizada`:

```csharp
    [InlineData(nameof(SesionRealtimeMessages.PistaEnviada))]
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: FAIL — `Assert.Contains("PistaEnviada", contrato)` falla (aún no documentado).

- [ ] **Step 3: Documentar el endpoint HTTP en `operaciones-sesion-api.md`**

En la tabla **Endpoint Registry**, añadir esta fila tras la de `Etapa actual` (`.../etapa-actual` GET):

```markdown
| Enviar pista (BDT) | POST | `/operaciones-sesion/partidas/{partidaId}/pistas` | Operador | 200 + PistaEnviadaResponse | 404 sesión no existe · 403 destino no inscrito · 409 no iniciada / juego no BDT / sin etapa activa |
```

En la sección **### DTOs**, añadir tras la línea de `EtapaActualDto`:

```markdown
- `PistaEnviadaResponse { partidaId, juegoId, participanteDestinoId, timestampUtc }` (request body `{ participanteDestinoId, texto }`; efecto: push `PistaEnviada` solo al participante destino)
```

- [ ] **Step 4: Documentar el mensaje Realtime**

En la sección **## Realtime / SignalR**, en el bloque *Cliente → servidor*, añadir una nota (tras `EnviarUbicacion`) sobre el auto-join del grupo participante:

```markdown
- `SuscribirAPartida` (rama participante) además une la conexión al grupo por-participante `participante:{id}` (para recibir `PistaEnviada`); `DesuscribirDePartida` la retira. La rama operador no se une a ningún grupo participante. (SP-3f-4)
```

En la tabla *Servidor → cliente*, añadir esta fila tras `UbicacionActualizada`:

```markdown
| `PistaEnviada` *(participante-destino only)* | `{ partidaId, juegoId, participanteDestinoId, texto, timestampUtc }` |
```

En el párrafo **Notas** (final de la sección Realtime), añadir esta frase (el `texto` de la pista es la excepción documentada al anti-leak porque es contenido dirigido exclusivamente a su receptor):

```markdown
`PistaEnviada` (SP-3f-4) se difunde SOLO al grupo `participante:{destinoId}` (BR-B06: la pista es para un participante específico; ni el resto de participantes ni el operador la reciben). Es event-only: el `texto` viaja en el payload (no hay pull) — única excepción al anti-leak, justamente porque es el contenido dirigido a ese participante. `timestampUtc` es server-stamped; no se persiste la entidad `Pista` ni se emite audit en este slice (el registro BR-B06 lo materializa audit vía broker, diferido). Sin replay: si el destino está offline, la pista se pierde (transitorio).
```

- [ ] **Step 5: Correr el test de contrato y verificar verde**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: PASS.

- [ ] **Step 6: Escribir la fila de traceability (NO commitear — carve-out)**

Editar `docs/04-sdd/traceability-matrix.md` y añadir la fila de SP-3f-4 (pistas BDT: HU/BR-B06 → endpoint `POST .../pistas`, evento `PistaEnviada`, dominio `PrepararPista`, entrega `participante:{destinoId}`), siguiendo el formato de las filas SP-3f-3/SP-3g existentes. **Este archivo NO se incluye en el commit.**

- [ ] **Step 7: Commit (SOLO contrato + test; el traceability queda unstaged)**

```bash
git add contracts/http/operaciones-sesion-api.md \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-4 T6: contrato HTTP+Realtime de pistas (PistaEnviada participante-destino) + test doc↔constantes

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 8: Verificar el carve-out intacto**

Run: `git status --short`
Expected: `docs/04-sdd/traceability-matrix.md` aparece como `M` (modificado, **sin** commitear); `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` y `docs/04-sdd/auditorias/` siguen unstaged. Ningún archivo de docs commiteado en T6.

---

## Self-Review

**1. Spec coverage:**
- Disparador HTTP command → T4 (command/handler) + T5 (endpoint). ✅
- Registro event-only vía seam (sin persistir/migración) → T2 (evento + seam), handler sin `SaveChanges` (T4). ✅
- Targeting grupo por-participante `participante:{id}` → T1 (grupo + hub join/leave) + T2 (SignalR push al grupo destino). ✅
- Dominio `PrepararPista` read-only (BR-B06, reusa 3 excepciones) → T3. ✅
- Texto en el payload, entrega solo al destino, sin replay → T1 (payload) + T2 (push) + T6 (doc). ✅
- `FakeSesionEventsPublisher` + `NoOpBase` con el método 14 → T2. ✅
- Estrategia de pruebas (dominio, handler, hub, SignalR, composite, validator, controller, contract) → T1–T6 cubren cada una. ✅
- Contrato HTTP + Realtime + traceability carve-out → T6. ✅
- Fuera de alcance (persistencia/migración/GET, audit broker, Equipo, clientes, hardening rol operador) → no hay tareas (correcto). ✅

**2. Placeholder scan:** sin TBD/TODO; todo step con código o comando concreto. ✅

**3. Type consistency:**
- `PistaEnviadaEvent` usa `SesionPartidaId` (consistente con los demás eventos BDT); `PistaEnviadaPayload` NO lleva `SesionPartidaId` (payload delgado, cliente). ✅
- `PrepararPista(Guid) → Guid` idéntico en T3 (def), T4 (uso). ✅
- `PistaEnviadaResponse(PartidaId, JuegoId, ParticipanteDestinoId, TimestampUtc)` idéntico en T4 (def), T5 (test). ✅
- `EnviarPistaCommand(PartidaId, ParticipanteDestinoId, Texto)` idéntico en T4, T5. ✅
- `GrupoParticipante` / `PistaEnviada` / `PistaEnviadaPayload` producidos en T1, consumidos en T2. ✅

## Execution Handoff

Plan guardado en `docs/superpowers/plans/2026-07-01-sp3f4-pistas-bdt.md`.
