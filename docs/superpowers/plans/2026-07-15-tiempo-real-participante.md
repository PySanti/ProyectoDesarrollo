# Tiempo real en las pantallas del participante — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el participante vea la aceptación/rechazo de su solicitud y el arranque de la partida sin pulsar "Recargar".

**Architecture:** Una sola causa raíz: la pertenencia a grupos de SignalR está condicionada a tener ya participación en la partida, pero todo lo que hay que notificar ocurre antes. Se separan las dos clases de mensaje: lo **personal** (aceptación, rechazo, convocatoria) va a `participante:{sub}`, al que ahora se entra **al conectar**; lo de **la partida** (inicio) sigue en `partida:{id}`, y lo que se añade es **reintentar** la suscripción cuando la participación aparece.

**Tech Stack:** .NET 8 (Operaciones de Sesión: xUnit), React Native/Expo (móvil: `node --test`), SignalR.

**Spec:** `docs/superpowers/specs/2026-07-15-tiempo-real-participante-design.md`

## Global Constraints

- **BR-G09 no se toca.** El miembro sigue libre hasta que acepta su convocatoria. Decidido en el spec §4.
- **La forma de los eventos en RabbitMQ no cambia.** Los destinatarios viajan como **parámetro del publisher**, nunca dentro del evento: son un asunto de entrega, no un hecho del dominio. Meterlos en el evento los mandaría al broker (`RabbitMqSesionEventsPublisher.cs:23-36`) y acabarían como ruido en el `detalle` del historial (`HistorialEventMapper.cs:65-72`).
- **Puntuaciones, Partidas, Identity y el gateway no se tocan.**
- **`InscripcionSolicitada` sigue siendo no-op** en SignalR: nadie la espera en vivo. Solo cambian `Aceptada` y `Rechazada`.
- **Un único mensaje realtime `InscripcionResuelta`** con `aceptada: bool`, no dos mensajes. La pantalla hace lo mismo en ambos casos (refrescar y decidir el aviso).
- **Resolver en vivo nunca rompe la pantalla.** Si el hub falla, se mantiene el aviso actual "Sin conexión en vivo; usa recargar" y Recargar sigue siendo la red de seguridad.
- **El arnés móvil es `node --test` y NO puede importar `.tsx`.** La lógica que merezca prueba va en `.js` (patrón de `liveLabels.js`). El render de las pantallas queda sin cobertura: limitación conocida y declarada.
- **Nada cambia para el operador.**

---

### Task 1: Canal personal al conectar (hub)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs` (`OnConnectedAsync` ~40-62; `DesuscribirDePartida` ~100-112)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs`

**Interfaces:**
- Produces: nada nuevo en código; establece la invariante de la que dependen las Tasks 3 y 5 — toda conexión de participante está en `SesionRealtimeMessages.GrupoParticipante(sub)`.

**Contexto:** `OnConnectedAsync` ya calcula `sub` y descarta al operador para el re-push de convocatorias (líneas 43-44). Se reusa ese mismo cálculo. `SuscribirAPartida` sigue apuntando al mismo grupo (línea 92): en SignalR `AddToGroupAsync` es idempotente, así que no hay conflicto.

**Ojo con `DesuscribirDePartida`:** hoy saca del grupo personal (líneas 104-107). Con el canal personal ligado a la identidad, eso debe **desaparecer**: salir de una partida no puede dejarte sordo a tus convocatorias.

- [ ] **Step 1: Write the failing tests**

Añadir a `SesionHubTests.cs`. Leer primero el arnés real del archivo: existe `Construir(repo, user, groups, ...)`, `Usuario(sub, rol)` y `FakeGroupManager` con `Added`/`Removed` (listas de tuplas `(connId, groupName)`).

```csharp
    [Fact]
    public async Task Al_conectar_el_participante_entra_a_su_canal_personal()
    {
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.OnConnectedAsync();

        // Sin partida de por medio: el canal personal es tuyo por ser quien eres.
        Assert.Contains(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Added);
    }

    [Fact]
    public async Task Al_conectar_el_operador_no_entra_a_canal_personal()
    {
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Operador"), groups);

        await hub.OnConnectedAsync();

        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Desuscribir_de_partida_no_saca_del_canal_personal()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);
        await hub.SuscribirAPartida(partidaId);

        await hub.DesuscribirDePartida(partidaId);

        // Salir de una partida no puede dejarte sordo a tus convocatorias.
        Assert.DoesNotContain(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Removed);
        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Removed);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SesionHubTests"`
Expected: FAIL — los dos primeros porque `OnConnectedAsync` no apunta a ningún grupo; el tercero porque hoy sí saca del personal.

- [ ] **Step 3: Write the implementation**

En `SesionHub.cs`, dentro de `OnConnectedAsync`, en el bloque que ya comprueba no-operador + `sub` parseable (línea 44), **antes** del `try` del re-push:

```csharp
            // Canal personal por identidad, no por partida: lo que hay que notificarle al
            // participante (aceptacion, rechazo, convocatoria) ocurre ANTES de que tenga
            // participacion, que es lo que SuscribirAPartida exige.
            await Groups.AddToGroupAsync(
                Context.ConnectionId, SesionRealtimeMessages.GrupoParticipante(usuarioId), Context.ConnectionAborted);
```

En `DesuscribirDePartida`, **borrar** el bloque que quita del grupo personal:

```csharp
        if (Context.Items.TryGetValue(ClaveParticipanteId, out var u) && u is Guid participanteId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoParticipante(participanteId), Context.ConnectionAborted);
        }
```

Tras borrarlo, `ClaveParticipanteId` puede quedar sin uso en ese método: si el compilador avisa de la variable `u` sin usar, quitar también la línea. **No** tocar el bloque de `ClaveEquipoId`, que sí debe seguir saliendo.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SesionHubTests"`
Expected: PASS — los nuevos y los que ya existían.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "fix(operaciones-sesion): canal personal al conectar, no al suscribirse a partida"
```

---

### Task 2: Destinatarios como parámetro del publisher

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs:25-26`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs:116-120`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs:58-59`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/AceptarInscripcionCommandHandler.cs:54-58`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/RechazarInscripcionCommandHandler.cs:43-47`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs:108-111`
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs:141-142`

**Interfaces:**
- Produces (lo consume la Task 3):
  ```csharp
  Task PublicarInscripcionAceptadaAsync(
      InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken);
  Task PublicarInscripcionRechazadaAsync(
      InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken);
  ```

**Esta task es plomería y no cambia comportamiento:** SignalR sigue siendo no-op hasta la Task 3. Su verificación es que **todo compila y la suite sigue verde**. El compilador señala cada sitio que falta.

Los eventos ya llevan `ParticipanteId`/`EquipoId` (`ParticipacionEvents.cs:21-27`) pero **no** la lista de miembros, y en Equipo `ParticipanteId` es `Guid.Empty` (`InscripcionPartida.cs:43`) — por eso hace falta el parámetro.

- [ ] **Step 1: Cambiar la interfaz**

En `ISesionEventsPublisher.cs`, sustituir las líneas 25-26:

```csharp
    // destinatarios = a quien se le entrega en vivo. Es un asunto de ENTREGA, no un hecho del
    // dominio: por eso viaja aparte y no dentro del evento (que se serializa tal cual a RabbitMQ).
    Task PublicarInscripcionAceptadaAsync(
        InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken);
    Task PublicarInscripcionRechazadaAsync(
        InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken);
```

- [ ] **Step 2: Actualizar las cuatro implementaciones**

`SignalRSesionEventsPublisher.cs` (sigue no-op, la Task 3 lo cambia):

```csharp
    public Task PublicarInscripcionAceptadaAsync(
        InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task PublicarInscripcionRechazadaAsync(
        InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) =>
        Task.CompletedTask;
```

`RabbitMqSesionEventsPublisher.cs` — ignora el parámetro, el payload al broker no cambia:

```csharp
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) => Publicar("InscripcionAceptada", evento);
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) => Publicar("InscripcionRechazada", evento);
```

`CompositeSesionEventsPublisher.cs` — reenvía:

```csharp
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionAceptadaAsync(evento, destinatarios, cancellationToken));
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionRechazadaAsync(evento, destinatarios, cancellationToken));
```

`NoOpSesionEventsPublisher.cs` — localizar los dos métodos por nombre y añadirles el parámetro; el cuerpo (`Task.CompletedTask`) no cambia.

- [ ] **Step 3: Actualizar los dos fakes de tests**

`FakeSesionEventsPublisher.cs` — grabar también los destinatarios, que la Task 3 no usa pero deja el fake listo:

```csharp
    public List<(InscripcionAceptadaEvent Evento, IReadOnlyList<Guid> Destinatarios)> InscripcionesAceptadas { get; } = new();
    public List<(InscripcionRechazadaEvent Evento, IReadOnlyList<Guid> Destinatarios)> InscripcionesRechazadas { get; } = new();

    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken)
    { InscripcionesAceptadas.Add((evento, destinatarios)); return Task.CompletedTask; }
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken)
    { InscripcionesRechazadas.Add((evento, destinatarios)); return Task.CompletedTask; }
```

Cambiar el tipo de esas dos listas **rompe a sus lectores actuales**. Localizarlos y arreglarlos:

Run: `grep -rn "InscripcionesAceptadas\|InscripcionesRechazadas" services/operaciones-sesion/tests`

Donde un test hacía `Assert.Single(pub.InscripcionesAceptadas).PartidaId`, pasa a ser `Assert.Single(pub.InscripcionesAceptadas).Evento.PartidaId`. Si el número de lectores resulta grande, la alternativa válida es dejar las listas de eventos como estaban y **añadir** dos listas separadas `DestinatariosAceptada`/`DestinatariosRechazada` — decidir según lo que devuelva el grep, y dejar el motivo en el mensaje de commit.

`CompositeSesionEventsPublisherTests.cs` — en `NoOpBase`, líneas 141-142:

```csharp
        public virtual Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent e, IReadOnlyList<Guid> destinatarios, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent e, IReadOnlyList<Guid> destinatarios, CancellationToken ct) => Task.CompletedTask;
```

Si `RecordingPublisher` sobrescribe alguno de los dos, actualizar también su firma.

- [ ] **Step 4: Pasar los destinatarios desde los handlers**

`AceptarInscripcionCommandHandler.cs`, sustituir la llamada de las líneas 54-58:

```csharp
        // Individual: el solicitante. Equipo: el snapshot de miembros — el lider no se guarda
        // (InscripcionPartida.ParticipanteId = Guid.Empty en Equipo), asi que se notifica al conjunto.
        var destinatarios = esEquipo
            ? aceptada.MiembrosSnapshot
            : (IReadOnlyList<Guid>)new[] { aceptada.ParticipanteId };

        await _events.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(
                sesion.PartidaId, sesion.Id.Valor, aceptada.Id.Valor, aceptada.Modalidad.ToString(),
                esEquipo ? null : aceptada.ParticipanteId, esEquipo ? aceptada.EquipoId : null, now),
            destinatarios,
            cancellationToken);
```

`RechazarInscripcionCommandHandler.cs`, sustituir la llamada de las líneas 43-47:

```csharp
        // `inscripcion` no es null aqui: RechazarInscripcion (arriba) ya habria lanzado si no existiera.
        var destinatarios = esEquipo
            ? inscripcion!.MiembrosSnapshot
            : (IReadOnlyList<Guid>)new[] { participanteId!.Value };

        await _events.PublicarInscripcionRechazadaAsync(
            new InscripcionRechazadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscId, esEquipo ? "Equipo" : "Individual",
                esEquipo ? null : participanteId, equipoId, now),
            destinatarios,
            cancellationToken);
```

- [ ] **Step 5: Verificar que compila y la suite sigue verde**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS — sin cambio de comportamiento; la verificación es que compila y nada se rompió.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/
git commit -m "refactor(operaciones-sesion): destinatarios como parametro del publisher de inscripcion"
```

---

### Task 3: Difundir `InscripcionResuelta`

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs:19`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs`

**Interfaces:**
- Consumes: la firma con `destinatarios` (Task 2); el canal personal (Task 1).
- Produces (lo consume la Task 4): mensaje `"InscripcionResuelta"` con payload `{ partidaId, inscripcionId, modalidad, aceptada }` (camelCase en el cable).

**El fake de tests no sirve tal cual.** `FakeHubClients` (líneas 285-299) solo graba el **último** grupo y lanza `NotImplementedException` en `Groups(...)`. Como en Equipo hay varios destinatarios, hay que implementar `Groups(IReadOnlyList<string>)`. Se usa `Clients.Groups(nombres)`: un solo envío a varios grupos, que es la forma idiomática en SignalR.

- [ ] **Step 1: Write the failing tests**

Añadir a `SignalRSesionEventsPublisherTests.cs`:

```csharp
    [Fact]
    public async Task InscripcionAceptada_difunde_al_canal_personal_del_solicitante()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var inscripcionId = Guid.NewGuid();

        await pub.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(partidaId, Guid.NewGuid(), inscripcionId, "Individual",
                participanteId, null, T0),
            new[] { participanteId }, CancellationToken.None);

        Assert.Equal(new[] { SesionRealtimeMessages.GrupoParticipante(participanteId) }, clients.LastGroups);
        Assert.Equal(SesionRealtimeMessages.InscripcionResuelta, clients.Proxy.Method);
        var payload = Assert.IsType<InscripcionResueltaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(inscripcionId, payload.InscripcionId);
        Assert.Equal("Individual", payload.Modalidad);
        Assert.True(payload.Aceptada);
    }

    [Fact]
    public async Task InscripcionRechazada_difunde_con_aceptada_false()
    {
        var (pub, clients) = Build();
        var participanteId = Guid.NewGuid();

        await pub.PublicarInscripcionRechazadaAsync(
            new InscripcionRechazadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Individual",
                participanteId, null, T0),
            new[] { participanteId }, CancellationToken.None);

        var payload = Assert.IsType<InscripcionResueltaPayload>(clients.Proxy.Args![0]);
        Assert.False(payload.Aceptada);
    }

    [Fact]
    public async Task InscripcionAceptada_de_equipo_difunde_a_todos_los_miembros()
    {
        var (pub, clients) = Build();
        var equipoId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        await pub.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Equipo",
                null, equipoId, T0),
            new[] { m1, m2 }, CancellationToken.None);

        // El lider no es identificable (ParticipanteId = Guid.Empty en Equipo): se notifica al conjunto.
        Assert.Equal(
            new[] { SesionRealtimeMessages.GrupoParticipante(m1), SesionRealtimeMessages.GrupoParticipante(m2) },
            clients.LastGroups);
    }

    [Fact]
    public async Task InscripcionResuelta_sin_destinatarios_no_difunde()
    {
        var (pub, clients) = Build();

        await pub.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Equipo",
                null, Guid.NewGuid(), T0),
            Array.Empty<Guid>(), CancellationToken.None);

        // Un equipo con snapshot vacio no debe provocar un envio a cero grupos.
        Assert.Null(clients.Proxy.Method);
    }
```

Ampliar `FakeHubClients` para grabar varios grupos, **manteniendo** `LastGroup` para los tests que ya existen:

```csharp
    private sealed class FakeHubClients : IHubClients
    {
        public string? LastGroup { get; private set; }
        public IReadOnlyList<string>? LastGroups { get; private set; }
        public FakeClientProxy Proxy { get; } = new();
        public IClientProxy Group(string groupName) { LastGroup = groupName; return Proxy; }
        public IClientProxy Groups(IReadOnlyList<string> groupNames) { LastGroups = groupNames; return Proxy; }

        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }
```

(Se quita la línea `Groups(...) => throw new NotImplementedException();` del bloque de abajo: ahora está implementada arriba.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SignalRSesionEventsPublisherTests"`
Expected: FAIL de compilación — no existen `SesionRealtimeMessages.InscripcionResuelta` ni `InscripcionResueltaPayload`.

- [ ] **Step 3: Write the implementation**

En `SesionRealtimeMessages.cs`, tras la línea 19 (`ConvocatoriaCreada`):

```csharp
    public const string InscripcionResuelta = nameof(InscripcionResuelta);
```

En `SesionRealtimePayloads.cs`, junto al resto de records:

```csharp
// Un solo mensaje con booleano en vez de dos (Aceptada/Rechazada): la pantalla hace lo mismo en
// ambos casos — refrescar y decidir el aviso.
public sealed record InscripcionResueltaPayload(
    Guid PartidaId, Guid InscripcionId, string Modalidad, bool Aceptada);
```

En `SignalRSesionEventsPublisher.cs`, junto al helper `Difundir` existente (línea 15):

```csharp
    private Task DifundirAPersonales(IReadOnlyList<Guid> destinatarios, string mensaje, object payload, CancellationToken ct)
    {
        if (destinatarios.Count == 0) return Task.CompletedTask;
        var grupos = destinatarios.Select(SesionRealtimeMessages.GrupoParticipante).ToList();
        return _hub.Clients.Groups(grupos).SendAsync(mensaje, payload, ct);
    }
```

Requiere `using System.Linq;` y `using System.Collections.Generic;` al principio del archivo si no están.

Sustituir los dos no-op (que la Task 2 dejó con la firma nueva):

```csharp
    public Task PublicarInscripcionAceptadaAsync(
        InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) =>
        DifundirAPersonales(destinatarios, SesionRealtimeMessages.InscripcionResuelta,
            new InscripcionResueltaPayload(evento.PartidaId, evento.InscripcionId, evento.Modalidad, true),
            cancellationToken);

    public Task PublicarInscripcionRechazadaAsync(
        InscripcionRechazadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken cancellationToken) =>
        DifundirAPersonales(destinatarios, SesionRealtimeMessages.InscripcionResuelta,
            new InscripcionResueltaPayload(evento.PartidaId, evento.InscripcionId, evento.Modalidad, false),
            cancellationToken);
```

`PublicarInscripcionSolicitadaAsync` **se queda no-op**: nadie la espera en vivo. Actualizar el comentario de las líneas 112-120 para que refleje la realidad nueva:

```csharp
    // InscripcionSolicitada no difunde: el lobby del operador se refresca por polling (SP-3f-2).
    // Aceptada/Rechazada SI difunden desde este slice: el participante las espera en vivo.
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SignalRSesionEventsPublisherTests"`
Expected: PASS — los 4 nuevos y los que ya existían.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/
git commit -m "feat(operaciones-sesion): difundir InscripcionResuelta al canal personal"
```

---

### Task 4: Lobby móvil — reaccionar y re-suscribirse

**Files:**
- Create: `mobile/src/features/partidas/resolucionInscripcion.js`
- Create: `mobile/tests/resolucionInscripcion.test.js`
- Modify: `mobile/src/features/partidas/PartidaLobbyScreen.tsx` (efecto del hub ~80-94; `onAccion` ~96-113)

**Interfaces:**
- Consumes: mensaje `"InscripcionResuelta"` con `{ partidaId, inscripcionId, modalidad, aceptada }` (Task 3).
- Produces: `avisoResolucion(aceptada) → { variant: "success"|"error", texto: string }`.

**Por qué un `.js` aparte:** el arnés móvil (`node --test`) **no puede importar `.tsx`**. El copy del aviso es lo único con lógica y merece prueba; el resto es cableado de pantalla.

**Ojo:** esto es lo que habilita el salto automático al iniciar. `PartidaIniciada` va al grupo `partida:{id}` (`SignalRSesionEventsPublisher.cs:21-22`), **no** al canal personal, así que tras la aceptación hay que reinvocar `SuscribirAPartida` — que ahora sí pasa el guard, porque ya hay inscripción.

- [ ] **Step 1: Write the failing test**

Crear `mobile/tests/resolucionInscripcion.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { avisoResolucion } from "../src/features/partidas/resolucionInscripcion.js";

test("aceptada da aviso de exito", () => {
  const r = avisoResolucion(true);
  assert.equal(r.variant, "success");
  assert.match(r.texto, /confirmada|dentro/i);
});

test("rechazada dice que se puede volver a solicitar", () => {
  // El backend permite re-solicitar (OcupaParticipacion solo cuenta Pendiente|Activa),
  // asi que el copy no debe sugerir que sea terminal.
  const r = avisoResolucion(false);
  assert.equal(r.variant, "error");
  assert.match(r.texto, /volver a solicitar/i);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile && node --test tests/resolucionInscripcion.test.js`
Expected: FAIL — `Cannot find module '../src/features/partidas/resolucionInscripcion.js'`.

- [ ] **Step 3: Write the implementation**

Crear `mobile/src/features/partidas/resolucionInscripcion.js`:

```js
// Copy de la resolucion de una solicitud (HU-19). El rechazo NO es terminal: el backend deja
// volver a solicitar (OcupaParticipacion = Pendiente|Activa), asi que el texto lo dice.
export function avisoResolucion(aceptada) {
  return aceptada
    ? { variant: "success", texto: "Tu solicitud fue aceptada. Estás dentro." }
    : { variant: "error", texto: "El operador rechazó tu solicitud. Puedes volver a solicitar." };
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile && node --test tests/resolucionInscripcion.test.js`
Expected: PASS (2/2).

- [ ] **Step 5: Cablear la pantalla**

En `PartidaLobbyScreen.tsx`, añadir los imports:

```tsx
import { avisoResolucion } from "./resolucionInscripcion.js";
```

En el efecto del hub (líneas 80-94), registrar el handler nuevo. El `hub` ya está en el closure del
efecto, así que no hace falta ningún ref para reinvocar. El efecto queda así:

```tsx
  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, () => tokenRef.current);
    hub.on("PartidaEnLobby", () => void loadRef.current());
    hub.on("PartidaIniciada", () => onIniciadaRef.current());
    hub.on("PartidaCancelada", (p: { motivo?: string }) =>
      setAviso({ variant: "error", texto: p?.motivo ? `Partida cancelada: ${p.motivo}` : "Partida cancelada." })
    );
    hub.on("InscripcionResuelta", (p: { aceptada?: boolean }) => {
      const aceptada = p?.aceptada === true;
      setAviso(avisoResolucion(aceptada) as Aviso);
      void loadRef.current();
      // PartidaIniciada va al grupo de la partida, no al canal personal: sin esta
      // resuscripcion el arranque no llegaria y habria que pulsar Recargar.
      if (aceptada) void hub.invoke("SuscribirAPartida", partidaId).catch(() => {});
    });
    hub
      .start()
      .then(() => hub.invoke("SuscribirAPartida", partidaId))
      .catch(() => setAviso({ variant: "info", texto: "Sin conexión en vivo; usa recargar." }));
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl, partidaId]);
```

**No** cambiar las dependencias del efecto: la resuscripción ya no depende de que el efecto se re-ejecute. **No** tocar `onAccion`: el aviso "Solicitud enviada. Pendiente de aprobación del operador" sigue igual.

- [ ] **Step 6: Verificar tipos y suite**

Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS y exit 0. Si `avisoResolucion` (viene de un `.js` sin checkJs) da un tipo demasiado ancho para `Aviso`, el cast `as Aviso` de arriba lo resuelve — mismo patrón que `ConvocatoriasResult` en este archivo (líneas 27-32).

- [ ] **Step 7: Commit**

```bash
git add mobile/src/features/partidas/resolucionInscripcion.js mobile/tests/resolucionInscripcion.test.js mobile/src/features/partidas/PartidaLobbyScreen.tsx
git commit -m "feat(mobile): el lobby reacciona a la resolucion y se resuscribe"
```

---

### Task 5: Convocatorias móvil — escuchar el hub

**Files:**
- Modify: `mobile/src/features/partidas/ConvocatoriasScreen.tsx`

**Interfaces:**
- Consumes: el canal personal al conectar (Task 1); mensaje `"ConvocatoriaCreada"`, que **ya se emite hoy** (`SignalRSesionEventsPublisher.cs:91-96`).

**Contexto:** esta pantalla nunca abrió hub (`grep crearSesionHub mobile/src/features/partidas/*.tsx` solo devuelve lobby y live). El backend ya empuja `ConvocatoriaCreada` al canal personal y hace re-push de las pendientes al conectar (`SesionHub.cs:40-62`): ese re-push **nunca ha funcionado** porque nadie conectaba desde aquí.

**No invoca `SuscribirAPartida`:** no hay partida que mirar, y con la Task 1 el canal personal ya está activo al conectar. Es la prueba de que el canal personal era la pieza que faltaba.

**Sin test nuevo:** el arnés no puede importar `.tsx` y aquí no hay lógica que extraer — es cableado. `mobile/tests/convocatoriasFlow.test.js` ya cubre el flujo de datos que alimenta la pantalla.

- [ ] **Step 1: Cablear el hub**

En `ConvocatoriasScreen.tsx`, la línea 1 ya es
`import React, { useCallback, useEffect, useState } from "react";` — **añadir `useRef` a esa lista**,
no un import nuevo:

```tsx
import React, { useCallback, useEffect, useRef, useState } from "react";
```

Y añadir el import del hub junto a los demás de la feature:

```tsx
import { crearSesionHub } from "./sesionHub.js";
```

Añadir el ref del token junto al estado, y el efecto del hub tras el `useEffect` existente (líneas 47-53):

```tsx
  // El token va por ref: un refresh de sesion (RNF-24) no debe derribar la conexion viva.
  const tokenRef = useRef(token);
  tokenRef.current = token;
  const loadRef = useRef(load);
  loadRef.current = load;

  useEffect(() => {
    const hub = crearSesionHub(apiBaseUrl, () => tokenRef.current);
    // Sin SuscribirAPartida: no hay partida que mirar. El canal personal se activa al conectar,
    // y con el llega tanto el push en vivo como el re-push de pendientes de OnConnectedAsync.
    hub.on("ConvocatoriaCreada", () => void loadRef.current());
    hub.start().catch(() => {
      // Degradacion deliberada: la pantalla sigue siendo operativa con su carga inicial.
    });
    return () => {
      void hub.stop().catch(() => {});
    };
  }, [apiBaseUrl]);
```

- [ ] **Step 2: Verificar tipos y suite**

Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS y exit 0.

- [ ] **Step 3: Commit**

```bash
git add mobile/src/features/partidas/ConvocatoriasScreen.tsx
git commit -m "feat(mobile): convocatorias escucha el hub"
```

---

### Task 6: Re-suscripción al reconectar

**Files:**
- Modify: `mobile/src/features/partidas/sesionHub.js`
- Create: `mobile/tests/sesionHubReenganche.test.js`
- Modify: `mobile/src/features/partidas/PartidaLobbyScreen.tsx` (efecto del hub)
- Modify: `mobile/src/features/partidas/PartidaLiveScreen.tsx` (efecto del hub ~137-145)

**Interfaces:**
- Produces: `reengancharAlReconectar(hub, partidaId) → void` en `sesionHub.js`.

**El defecto:** `crearSesionHub` usa `withAutomaticReconnect()` (`sesionHub.js:13`), que restablece el socket — pero **los grupos de SignalR son por conexión y se pierden**. Ninguna pantalla re-invoca `SuscribirAPartida` al reconectar. Tras cualquier microcorte, la sesión en vivo deja de recibir preguntas, etapas y pistas **en silencio**, sin error visible, mientras la partida avanza.

- [ ] **Step 1: Write the failing test**

Crear `mobile/tests/sesionHubReenganche.test.js`:

```js
import test from "node:test";
import assert from "node:assert/strict";
import { reengancharAlReconectar } from "../src/features/partidas/sesionHub.js";

function fakeHub() {
  return {
    invocaciones: [],
    handlers: [],
    onreconnected(cb) { this.handlers.push(cb); },
    invoke(metodo, arg) { this.invocaciones.push([metodo, arg]); return Promise.resolve(); },
  };
}

test("al reconectar re-invoca SuscribirAPartida", async () => {
  const hub = fakeHub();
  reengancharAlReconectar(hub, "p1");

  await hub.handlers[0]();

  assert.deepEqual(hub.invocaciones, [["SuscribirAPartida", "p1"]]);
});

test("un fallo al re-suscribirse no propaga", async () => {
  const hub = fakeHub();
  hub.invoke = () => Promise.reject(new Error("caido"));
  reengancharAlReconectar(hub, "p1");

  // Los grupos se pierden al reconectar; si el reenganche falla, la pantalla sigue
  // operable con Recargar en vez de romperse.
  await assert.doesNotReject(() => hub.handlers[0]());
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd mobile && node --test tests/sesionHubReenganche.test.js`
Expected: FAIL — `reengancharAlReconectar is not a function`.

- [ ] **Step 3: Write the implementation**

En `mobile/src/features/partidas/sesionHub.js`, tras `crearSesionHub`:

```js
// Los grupos de SignalR son por conexion: withAutomaticReconnect restablece el socket pero
// NO devuelve al grupo. Sin esto, tras un microcorte la pantalla deja de recibir preguntas,
// etapas y pistas en silencio, sin error visible.
export function reengancharAlReconectar(hub, partidaId) {
  hub.onreconnected(() => {
    void Promise.resolve(hub.invoke("SuscribirAPartida", partidaId)).catch(() => {});
  });
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd mobile && node --test tests/sesionHubReenganche.test.js`
Expected: PASS (2/2).

- [ ] **Step 5: Cablear las dos pantallas**

En `PartidaLobbyScreen.tsx`, cambiar el import de la línea 6:

```tsx
import { crearSesionHub, reengancharAlReconectar } from "./sesionHub.js";
```

y, dentro del efecto del hub, justo antes de `hub.start()`:

```tsx
    reengancharAlReconectar(hub, partidaId);
```

En `PartidaLiveScreen.tsx`, cambiar el import de la línea 8:

```tsx
import { crearSesionHub, reengancharAlReconectar } from "./sesionHub.js";
```

y, en el efecto del hub de sesión (líneas 104-145), justo antes de `hub.start()` (línea 137):

```tsx
    reengancharAlReconectar(hub, partidaId);
```

**Solo el hub de sesión.** El hub de rankings (líneas 148-169) queda fuera: su push es aditivo y el `GET` sigue siendo la fuente recuperable, tal como declara su propio comentario (línea 147).

- [ ] **Step 6: Verificar tipos y suite**

Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS y exit 0.

- [ ] **Step 7: Commit**

```bash
git add mobile/src/features/partidas/sesionHub.js mobile/tests/sesionHubReenganche.test.js mobile/src/features/partidas/PartidaLobbyScreen.tsx mobile/src/features/partidas/PartidaLiveScreen.tsx
git commit -m "fix(mobile): re-suscribirse a la partida al reconectar"
```

---

### Task 7: Contratos y trazabilidad

**Files:**
- Modify: `contracts/events/operaciones-sesion-events.md:354`
- Modify: `docs/04-sdd/SPECS-LIST.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Esto no es papeleo:** `contracts/events/operaciones-sesion-events.md:354` declara hoy que los tres eventos de inscripción **no difunden por SignalR**. Este slice revierte esa decisión para dos de ellos. Si el contrato no se actualiza, queda diciendo lo contrario de lo que hace el código.

- [ ] **Step 1: Actualizar el contrato de eventos**

Localizar el párrafo de la línea 354 (empieza por "Los tres **no difunden por SignalR**") y sustituirlo por:

```markdown
`InscripcionSolicitada` **no difunde por SignalR** (No-Op — el operador consulta el lobby por polling,
coherente con la lista de no-broadcast de SP-3f-2). `InscripcionAceptada` e `InscripcionRechazada`
**sí difunden** desde el slice de tiempo real del participante (2026-07-15): el solicitante espera la
resolución en vivo, y la decisión original de no difundir se tomó mirando solo al operador. Los tres
se archivan en el historial de Puntuaciones vía la cola `puntuaciones.operaciones-sesion.historial`
ligada a `operaciones-sesion.#` (sin consumidor nuevo).

**Realtime — `InscripcionResuelta`.** Entrega a los grupos `participante:{id}` de los destinatarios:
en Individual el solicitante; en Equipo el `MiembrosSnapshot` de la inscripción (el líder no se guarda
—`InscripcionPartida.ParticipanteId` es `Guid.Empty` en Equipo— así que se notifica al conjunto).
Payload `{ partidaId, inscripcionId, modalidad, aceptada }`: un solo mensaje con booleano en vez de
dos, porque el cliente hace lo mismo en ambos casos. Los destinatarios viajan como **parámetro del
publisher**, no dentro del evento: la forma del evento en el broker no cambia y el `detalle` del
historial no se ensucia.
```

- [ ] **Step 2: `docs/04-sdd/SPECS-LIST.md`**

Añadir al final de la tabla:

```markdown
| Tiempo real en las pantallas del participante (corrección) | Operaciones de Sesión | mobile | Participante | docs/superpowers/specs/2026-07-15-tiempo-real-participante-design.md | Implemented (7 tasks). Aceptación/rechazo y arranque de partida sin pulsar Recargar. Causa raíz única: la pertenencia a grupos de SignalR exigía participación previa, pero todo lo notificable ocurre antes. Sin endpoints nuevos; BR-G09 sin cambios. |
```

- [ ] **Step 3: `docs/04-sdd/traceability-matrix.md`**

Tabla de 7 columnas (`Feature | Requirement | Owning service | Supporting services | SDD folder | Contracts | Status`). Añadir al final, sustituyendo los conteos `NNN` por los reales del Step 4:

```markdown
| Tiempo real en las pantallas del participante (corrección) | El participante ve la resolución de su solicitud y el arranque de la partida sin pulsar "Recargar". **Causa raíz única:** la pertenencia a grupos de SignalR estaba condicionada a `SuscribirAPartida`, que exige participación previa — pero la aceptación, el rechazo y la convocatoria ocurren **antes** de que esa condición se cumpla (circular por construcción). **Arreglo:** el canal personal `participante:{sub}` se asigna en `OnConnectedAsync` por identidad del JWT, no por partida; `InscripcionAceptada`/`Rechazada` dejan de ser no-op y difunden `InscripcionResuelta` a los destinatarios; el lobby se re-suscribe a `partida:{id}` al ser aceptado (que es lo que habilita el salto automático al iniciar, porque `PartidaIniciada` va al grupo de la partida, no al personal); Convocatorias abre hub por primera vez; lobby y live se re-enganchan al reconectar | Operaciones de Sesión | Mobile (consumidor); Puntuaciones, Partidas, Identity y Gateway **sin cambios** | docs/superpowers/specs/2026-07-15-tiempo-real-participante-design.md · docs/superpowers/plans/2026-07-15-tiempo-real-participante.md | contracts/events/operaciones-sesion-events.md | Implemented — 7 tasks, commit por task. Suites verdes en HEAD: Operaciones **NNN**, Mobile **NNN** + typecheck. **Fuente:** reporte del usuario sobre el flujo Individual, verificado en código (12 hechos con archivo y línea en el spec §2). **Decisión:** los destinatarios viajan como parámetro del publisher, no dentro del evento — son un asunto de entrega, no un hecho del dominio; así la forma del evento en RabbitMQ no cambia y el `detalle` del historial no se ensucia. **Decisión:** BR-G09 se queda como está — bloquear al miembro por una preinscripción del líder que no hizo y no puede ver lo dejaría retenido a ciegas; cambiarla exige spec propio con visibilidad para el miembro. **Revierte:** el no-broadcast de `InscripcionAceptada`/`Rechazada` declarado en el Bloque 4B, que se decidió mirando solo al operador. **Hallazgo (no arreglado aquí):** aceptar una convocatoria teniendo participación en otra partida da 409 con mensaje pobre — preexistente. **Diferido:** `PartidasPanelScreen` (el pull-to-refresh es patrón móvil legítimo para un listado de descubrimiento, no un defecto); visibilidad del miembro sobre la preinscripción del líder (capacidad nueva, HU propia). |
```

- [ ] **Step 4: Verificar las dos suites y anotar los conteos**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS. Anotar el total (línea base antes de este slice: 522 = 409 unit + 33 integration + 80 contract).

Run: `cd mobile && npm test && npm run typecheck`
Expected: PASS y exit 0. Anotar el total (línea base antes de este slice: 138).

Sustituir los `NNN` del Step 3 por los totales reales.

- [ ] **Step 5: Commit**

```bash
git add contracts/events/operaciones-sesion-events.md docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: contrato de InscripcionResuelta + trazabilidad del slice de tiempo real"
```

---

## Notas para quien ejecute

- **La Task 2 no cambia comportamiento** y es intencionadamente aburrida: su verificación es que compila y la suite sigue verde. Es la que hace posible la Task 3 sin ensuciar el evento.
- **El orden importa.** La Task 1 (canal personal) es la que hace que las Tasks 3 y 5 sirvan de algo: sin ella se difunde a grupos vacíos. No adelantar la 3 ni la 5.
- **No relajar el guard de `SuscribirAPartida`.** El arreglo es reintentar cuando el estado cambió, no bajar la autorización. Está decidido en el spec §5 (M1).
- **No añadir `InscripcionSolicitada` a SignalR** por simetría: nadie la espera en vivo.
- **Verificar las firmas reales antes de escribir**, aunque este plan las cite: los números de línea se desplazan con cada task.
