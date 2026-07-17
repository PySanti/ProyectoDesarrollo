# Tiempo real en las pantallas del participante

**Fecha:** 2026-07-15
**Servicios:** Operaciones de Sesión (backend) + Mobile (cliente)
**Tipo:** corrección de defectos sobre HU ya implementadas (HU-12/HU-19/HU-21). No introduce HU nueva.

## 1. Problema

El participante no ve en tiempo real lo que le ocurre a su participación. Tiene que pulsar
"Recargar" para enterarse de que el operador aceptó o rechazó su solicitud, y para saber que la
partida empezó — en vez de que la pantalla lo lleve sola a la sesión en vivo.

Reportado por el usuario sobre el flujo Individual, que es el caso gobernante de este spec.

## 2. Hechos verificados

Todo lo de abajo se comprobó leyendo el código, no la documentación.

| # | Hecho | Evidencia |
|---|---|---|
| H1 | `InscripcionSolicitada`/`Aceptada`/`Rechazada` **no difunden por SignalR**: los tres son `Task.CompletedTask` | `SignalRSesionEventsPublisher.cs:113-120` |
| H2 | El participante solo entra a su canal personal dentro de `SuscribirAPartida`, que exige participación previa en esa partida | `SesionHub.cs:81-85`, `:92` |
| H3 | El efecto que abre el hub en el lobby depende solo de `[apiBaseUrl, partidaId]`: **no reintenta** al inscribirse | `PartidaLobbyScreen.tsx:94` |
| H4 | El participante llega al lobby **antes** de inscribirse, así que la suscripción falla siempre en el primer intento | `PartidaLobbyScreenContainer.tsx:25` |
| H5 | `withAutomaticReconnect()` restablece el socket pero **los grupos se pierden**; ninguna pantalla re-invoca `SuscribirAPartida` al reconectar | `sesionHub.js:13`, `PartidaLobbyScreen.tsx:87-94`, `PartidaLiveScreen.tsx:137-145` |
| H6 | `ConvocatoriasScreen` **no abre hub**, aunque el backend ya empuja `ConvocatoriaCreada` al canal personal y hace re-push al conectar | `ConvocatoriasScreen.tsx` (sin `crearSesionHub`), `SignalRSesionEventsPublisher.cs:91-96`, `SesionHub.cs:40-62` |
| H7 | Al aceptar un equipo se publica `ConvocatoriaCreada` por miembro: en Equipo **esa es** la señal de aceptación | `AceptarInscripcionCommandHandler.cs:47-52` |
| H8 | En Equipo el líder **no se guarda** (`ParticipanteId = Guid.Empty`); solo existe `MiembrosSnapshot` | `InscripcionPartida.cs:21`, `:43` |
| H9 | Una inscripción `Rechazada` **no bloquea** volver a solicitar (`OcupaParticipacion` = Pendiente\|Activa) | `InscripcionPartida.cs:80-81`, `SesionPartida.cs:58`, `:89` |
| H10 | Tras el rechazo, `mi-sesion` deja de devolver la inscripción → la pantalla vuelve al botón "Inscribirme" **sin explicar nada** | `SesionPartidaRepository.cs:53-58`, `partidaLobbyFlow.js:26-30` |
| H11 | El mapper del historial es agnóstico a la forma: vuelca en `detalle` todo campo que no reconozca | `HistorialEventMapper.cs:65-72` |
| H12 | El publisher de RabbitMQ serializa el evento tal cual | `RabbitMqSesionEventsPublisher.cs:23-36` |

## 3. Causa raíz

**Es un solo defecto, no cuatro.** La pertenencia a grupos de SignalR está condicionada a tener ya
participación en una partida concreta (H2). Pero todo lo que el participante necesita saber ocurre
**antes** de que esa condición se cumpla: que le aceptaron, que le rechazaron, que le convocaron.
Es circular: para que te avisen de que entraste, tienes que haber entrado.

Faltan dos cosas, porque hay **dos clases de mensaje**:

- **Para ti** (aceptación, rechazo, convocatoria, pista) → canal personal, ligado a la identidad
  del JWT. Debe existir por ser quien eres, no por estar en una partida.
- **Para todos los de la partida** (inicio, activación de juego) → grupo `partida:{id}`, que sí debe
  seguir exigiendo participación. Lo que falta ahí es **reintentar** la suscripción cuando esa
  participación aparece.

## 4. Alcance

### Dentro

1. Alta al canal personal al conectar (backend).
2. Difusión de aceptación/rechazo al canal personal (backend).
3. Lobby: reaccionar a la resolución y re-suscribirse a la partida (móvil).
4. Convocatorias: escuchar el hub (móvil).
5. Re-suscripción al reconectar, en lobby y en sesión en vivo (móvil).
6. Contratos y trazabilidad: `contracts/events/operaciones-sesion-events.md` (revertir el no-op
   declarado para aceptación/rechazo y documentar `InscripcionResuelta`), `SPECS-LIST.md`,
   `traceability-matrix.md`.

### Fuera, con motivo

- **Panel de Partidas** (`PartidasPanelScreen`): el pull-to-refresh es un patrón móvil legítimo para
  un listado de descubrimiento. No es un defecto.
- **Cambiar BR-G09** (que la preinscripción pendiente del líder ocupe a los miembros): el código
  cumple la regla escrita (`business-rules.md:19`); cambiarla es decisión de dominio con spec propio,
  y exigiría darle al miembro visibilidad de una solicitud que hoy no ve, o quedaría retenido a
  ciegas. **Decidido en este spec: BR-G09 se queda como está.**
- **Visibilidad del miembro sobre la preinscripción del líder**: capacidad nueva, necesita HU propia.
- **El 409 al aceptar una convocatoria teniendo participación en otra partida**: comportamiento
  preexistente (`ResponderConvocatoria` valida participación); el mensaje al usuario es pobre pero no
  lo introduce este slice. **Hallazgo anotado, no se arregla aquí.**
- **Web (operador)**: no cambia nada. Su lobby sigue por polling (SP-3f-2).

## 5. Arquitectura

Sin endpoints nuevos, sin cambios de autorización, sin tocar Partidas, Puntuaciones, Identity ni el
gateway.

**Sí cambia el contrato de eventos, y hay que decirlo con precisión.** La *forma* de los eventos en
RabbitMQ queda intacta (ver B2), pero `contracts/events/operaciones-sesion-events.md:354` declara hoy
que los tres eventos de inscripción **no difunden por SignalR** ("No-Op — el operador consulta el
lobby por polling"). Este slice revierte esa decisión para dos de los tres, así que esa sección debe
actualizarse y el mensaje realtime `InscripcionResuelta` documentarse. La decisión original se tomó
para el operador; el participante heredó la limitación sin que se evaluara desde su lado — eso es lo
que se corrige.

### B1 — Canal personal al conectar

`SesionHub.OnConnectedAsync` apunta a todo participante autenticado no-operador a
`participante:{sub}`. El `sub` ya se calcula ahí para el re-push de convocatorias
(`SesionHub.cs:43-44`): se reusa.

Es **aditivo**: `SuscribirAPartida` sigue apuntando al mismo grupo (idempotente en SignalR) y no se
pierde comportamiento. Como el alta al canal personal ya no depende de la partida,
`DesuscribirDePartida` **debe dejar de sacar del grupo personal** (`SesionHub.cs:104-107`): salir de
una partida no puede dejarte sordo a tus convocatorias.

Política de error igual que el re-push existente: un fallo aquí se loguea y la conexión continúa.

### B2 — Difundir aceptación y rechazo

`PublicarInscripcionAceptadaAsync` / `PublicarInscripcionRechazadaAsync` dejan de ser no-op en la
implementación de SignalR y envían al canal personal de cada destinatario.

Los destinatarios viajan como **parámetro del publisher**, no dentro del evento:

```csharp
Task PublicarInscripcionAceptadaAsync(
    InscripcionAceptadaEvent evento, IReadOnlyList<Guid> destinatarios, CancellationToken ct);
```

Razón: a quién se le entrega es un asunto de **entrega**, no un hecho del dominio. Meterlo en el
evento lo mandaría a RabbitMQ (H12) y acabaría como ruido en el `detalle` del historial (H11). La
implementación de SignalR los usa; las de RabbitMQ y NoOp los ignoran; la Composite los reenvía.
Forma del evento en el cable: **intacta**. Puntuaciones: **sin cambios**. El coste es cambiar la
firma de dos métodos de `ISesionEventsPublisher`, que arrastra a sus cuatro implementaciones y a los
fakes de los tests — mecánico, y el compilador señala cada sitio.

Quiénes son, por modalidad:

- **Individual:** `[inscripcion.ParticipanteId]`.
- **Equipo:** `inscripcion.MiembrosSnapshot` — el líder no es identificable (H8), así que se notifica
  al conjunto. A los demás miembros no les estorba: en la aceptación ya reciben `ConvocatoriaCreada`,
  que es la accionable (H7).

Mensaje realtime: **uno solo**, `InscripcionResuelta`, con payload
`{ partidaId, inscripcionId, modalidad, aceptada: bool }`. Un mensaje con un booleano en vez de dos
mensajes: la pantalla hace lo mismo en ambos casos (refrescar y decidir el aviso), y así no hay que
registrar dos handlers casi idénticos.

`InscripcionSolicitada` **sigue siendo no-op**: nadie la espera en vivo (el operador usa polling).

### M1 — Lobby: reaccionar y re-suscribirse

`PartidaLobbyScreen` escucha `InscripcionResuelta`:

- **Aceptada** → recargar (pasa a "Inscripción confirmada. Estás dentro") **y reinvocar
  `SuscribirAPartida`**. Esto es lo que habilita el salto automático al iniciar: `PartidaIniciada` va
  al grupo de la partida, no al personal.
- **Rechazada** → aviso explícito *"El operador rechazó tu solicitud. Puedes volver a solicitar."* y
  recargar; el botón "Inscribirme" reaparece y funciona (H9). El aviso vive en estado propio, así que
  sobrevive al refresco. Es transitorio por diseño: al salir y volver desaparece, porque el backend
  ya no devuelve esa inscripción (H10).

Esto cierra H3/H4 sin tocar el guard de `SuscribirAPartida`: no se relaja ninguna autorización, solo
se reintenta cuando el estado cambió.

### M2 — Convocatorias: escuchar el hub

`ConvocatoriasScreen` abre `crearSesionHub`, escucha `ConvocatoriaCreada` y recarga. No invoca
`SuscribirAPartida`: no hay partida que mirar, y con B1 el canal personal ya está activo al conectar.
El re-push de cortesía (H6) empieza a funcionar por primera vez.

### M3 — Re-suscripción al reconectar

`onreconnected` → reinvocar `SuscribirAPartida` en lobby y en sesión en vivo (H5). Es el arreglo más
grave aunque el usuario no lo haya visto: hoy, tras cualquier microcorte, la pantalla en vivo deja de
recibir preguntas, etapas y pistas **en silencio**, sin error visible, mientras la partida avanza.

## 6. Errores y degradación

**Resolver en vivo nunca rompe la pantalla.** Si el hub no conecta, se mantiene el aviso actual
*"Sin conexión en vivo; usa recargar"* y Recargar sigue siendo la red de seguridad — pasa de
obligatorio a respaldo, que es lo que el propio aviso ya prometía.

Los fallos del alta al canal personal se loguean y la conexión sigue (misma política que el re-push
existente, `SesionHub.cs:56-59`).

## 7. Testing

**Backend (xUnit).** Unidad del hub: todo participante entra a su canal al conectar; el operador no;
`DesuscribirDePartida` no saca del canal personal. Unidad del publisher: `InscripcionResuelta` sale
con `aceptada` correcto y a los destinatarios correctos, en Individual y en Equipo. Los fakes de
`ISesionEventsPublisher` en tests absorben la firma nueva.

**Móvil (`node --test`).** El arnés **no puede importar `.tsx`**: la lógica nueva que merezca prueba
va en `.js` (mismo patrón que `liveLabels.js`). El render de las pantallas queda sin cobertura —
limitación conocida y ya declarada en slices anteriores.

## 8. Criterios de aceptación

1. Un participante Individual con solicitud pendiente ve la resolución **sin pulsar Recargar**:
   confirmada o rechazada, según decida el operador.
2. Tras la aceptación, cuando el operador inicia, la pantalla **navega sola** a la sesión en vivo.
3. Tras el rechazo, el participante ve por qué y puede volver a solicitar.
4. Un miembro de equipo ve aparecer su convocatoria **sin salir y entrar** de la pantalla.
5. Tras una reconexión, la sesión en vivo vuelve a recibir preguntas, etapas y pistas.
6. Sin conexión en vivo, todas las pantallas siguen siendo operables con Recargar.
7. Las suites de Operaciones de Sesión y de móvil siguen verdes. La forma de los eventos en RabbitMQ
   no cambia y Puntuaciones no se toca; el contrato de eventos sí se actualiza en su sección de
   entrega realtime, que hoy declara lo contrario de lo que este slice implementa.
