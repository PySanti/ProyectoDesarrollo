# Auto-aceptar la convocatoria del líder

**Fecha:** 2026-07-16
**Servicio:** Operaciones de Sesión (backend). Sin cambios de cliente ni de gateway.
**Tipo:** cambio de regla sobre HU ya implementadas (HU-15 / HU-16 / HU-20). No introduce HU nueva.
**Reglas:** BR-G09. Modifica la decisión "default simple aprobado" de `sp3e1` §5.
**Supersede:** `docs/superpowers/specs/2026-07-01-sp3e1-participacion-equipo-design.md` §2 (línea 19) y §5.

## 1. Problema

Reportado por el usuario: inscribe un equipo a una partida de equipo, el operador acepta, le da a
iniciar y **la partida se cancela**. Probado con un equipo de solo el líder y con dos equipos de
solo el líder: mismo resultado.

## 2. Hechos verificados

Comprobado contra el código y contra la base real (`umbral_operaciones_sesion`).

| # | Hecho | Evidencia |
|---|---|---|
| H1 | El mínimo en Equipo cuenta inscripciones activas **con ≥1 convocatoria aceptada**, no equipos inscritos | `SesionPartida.cs:433-435` |
| H2 | Bajo mínimos, `AplicarInicio` cancela **toda** la sesión | `SesionPartida.cs:436-441` |
| H3 | Las 3 sesiones de Equipo de las pruebas quedaron `Cancelada` (estado 2) | `sesiones_partida` |
| H4 | Las inscripciones de equipo **sí estaban `Activa`** — el enum tiene valores históricos: `Activa = 0` | `EstadoInscripcion.cs`, `inscripciones` |
| H5 | Las **4 convocatorias** quedaron `Pendiente` (estado 0) con `fecharespuesta` NULL: nadie aceptó nunca | `convocatorias` |
| H6 | El operador inició entre **5 y 38 segundos** después de aceptar; nadie podía responder en esa ventana | `convocatorias.fechaenvio` vs `sesiones_partida.fechafin` |
| H7 | Las convocatorias se crean al **aceptar el operador**, una por miembro del snapshot | `InscripcionPartida.cs:57-69` |
| H8 | El snapshot son todos los miembros del equipo, y **el líder es miembro** | `PreinscribirEquipoCommandHandler.cs:42` |
| H9 | `MiembrosSnapshot` es `List<Guid>`: **pierde el flag `EsLider`** | `InscripcionPartida.cs:11` |
| H10 | El handler **sí conoce** al líder (`request.LiderId`), pero no lo persiste | `PreinscribirEquipoCommandHandler.cs:41` |
| H11 | `PreinscribirEquipo` valida la participación **del equipo**, no la del líder como individuo | `SesionPartida.cs:91-92` |
| H12 | Aceptar una convocatoria a mano **sí** valida BR-G09 vía `ParticipanteTieneParticipacionActivaAsync` | `ResponderConvocatoriaCommandHandler.cs:34-39` |
| H13 | `ConvocatoriaCreadaEvent` **no lleva `Estado`**: un consumidor no puede deducir que nació aceptada | `AceptarInscripcionCommandHandler.cs:49-51` |
| H14 | En Equipo, `ConvocatoriaCreada` es la señal de que el operador aceptó (la usa el móvil para avanzar) | spec `2026-07-15-tiempo-real-participante` H7 |

## 3. Causa raíz

**No es un defecto de implementación: el código cumple `sp3e1` §5 al pie de la letra.** El problema
es la regla aprobada.

Un equipo cuenta para el mínimo solo si **alguien aceptó su convocatoria** (H1). Como el líder es
miembro (H8), el sistema le emite una convocatoria **a sí mismo**, y hasta que no la acepte su
equipo vale 0. En un equipo de solo el líder eso significa que la partida **no puede arrancar**
hasta que el líder vaya a la pantalla "Convocatorias" y se acepte a sí mismo — un paso que nadie
descubre, porque inscribir el equipo ya *era* declarar que quiere jugar.

El resultado observado (H3–H6): equipos inscritos y aceptados, convocatorias intactas, cuenta 0,
`0 < 1` → cancelación automática de toda la sesión.

`minimos = 1` con un equipo inscrito **debería** bastar. La regla de negocio no está mal; está mal
que la intención del líder no se registre en ninguna parte.

## 4. Decisión

**La convocatoria del líder nace `Aceptada`.** Preinscribir el equipo es la declaración de
intención del líder (HU-15: "Solo el líder puede unir el equipo"); pedirle además que se convoque a
sí mismo es redundante y es lo que bloquea el flujo.

Se mantiene la emisión de la convocatoria del líder (no se le excluye del snapshot) por H14: el
móvil usa `ConvocatoriaCreada` como señal de que el operador aceptó. Quitársela le dejaría la
pantalla muerta.

### D1 — La inscripción recuerda a su líder

`InscripcionPartida` gana `LiderId` (columna `liderid`). Hace falta porque las convocatorias se
crean **después** (al aceptar el operador, H7) y para entonces el flag `EsLider` ya se perdió (H9).
El handler ya tiene el dato (H10); solo hay que persistirlo. `Guid.Empty` en Individual.

### D2 — `Aceptar` nace aceptando la del líder, con el guard de BR-G09

`Aceptar(now, liderTieneParticipacionActivaEnOtra)`: crea las convocatorias y acepta la del líder
**salvo** que el líder ya tenga una participación activa en otra partida.

El guard no es opcional: auto-aceptar sin él saltaría la validación que H12 aplica en el camino
manual, y BR-G09 quedaría violada por la puerta de atrás. Si el guard salta, la convocatoria del
líder se queda `Pendiente` — exactamente el estado en que quedaría si intentara aceptarla a mano.
El equipo puede jugar igual si otro miembro acepta.

El bool lo calcula el handler y entra al dominio como parámetro, siguiendo el patrón ya establecido
por `ResponderConvocatoria` (H12): el dominio no consulta el repositorio.

### D3 — La aceptación automática también emite `ConvocatoriaRespondida`

Por H13, un consumidor que solo vea `ConvocatoriaCreada` asumiría `Pendiente` y proyectaría un
estado falso. El auto-aceptado emite además `ConvocatoriaRespondida`, igual que el camino manual.

## 5. Alternativas descartadas

| Alternativa | Por qué no |
|---|---|
| No convocar al líder y contarlo como activo por haber inscrito | Le quita la señal `ConvocatoriaCreada` que el móvil usa para avanzar (H14), y obliga a tocar el conteo de participante-activo en todo el runtime de Equipo: mucha más superficie por el mismo resultado. |
| Que el mínimo cuente equipos inscritos en vez de equipos con ≥1 aceptación | Dejaría arrancar partidas con equipos sin un solo jugador confirmado. La regla de `sp3e1` §5 es sana; lo que faltaba era registrar la intención del líder. |
| Auto-aceptar sin el guard de BR-G09 | Permitiría al líder tener dos participaciones activas a la vez, violando BR-G09 justo donde el camino manual sí valida (H12). |
| Dejarlo como está y documentar el paso | Es el estado actual, y ya bloqueó al usuario tres veces seguidas. Un paso que nadie descubre no es un paso. |

## 6. Riesgo y alcance

- **Migración:** columna `liderid` nueva. Las inscripciones de Equipo existentes quedan con
  `Guid.Empty` → ningún líder auto-aceptado retroactivamente. Aceptable: las 3 sesiones de prueba
  están canceladas y son descartables.
- **Fuera de alcance, pero real:** el operador inicia **a ciegas**. Aunque el líder quede
  auto-aceptado, un equipo de varios miembros seguirá cancelándose si el operador inicia antes de
  que alguien acepte, y el operador seguirá sin saber por qué (H6 muestra ventanas de 5–38 s). El
  lobby no le dice cuántos equipos cuentan realmente para el mínimo. Merece su propio slice.
