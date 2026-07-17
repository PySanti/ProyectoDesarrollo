# Tipado del id local y renombrado del subject — Identity

**Fecha:** 2026-07-16
**Servicio:** Identity
**Cliente:** ninguno (cero cambios de contrato HTTP)
**Tipo:** refactor estructural preventivo — no introduce HU ni cambia reglas de negocio

## Por qué

Identity maneja dos identificadores distintos para la misma persona:

| | Qué es | Dónde vive |
|---|---|---|
| `Usuario.UsuarioId` | Guid local, generado con `Guid.NewGuid()` en `Usuario.Crear` | tabla `usuarios` |
| El `sub` de Keycloak | el id con el que el actor llega en el token | `Usuario.KeycloakId` (string), y **el mundo de equipos entero** |

Son valores sin relación entre sí. Comprobado contra la base real:

```
participante2 → usuarioid 6672abd0-…  |  keycloakid 8e9de588-…
participante3 → usuarioid 0f9bfac2-…  |  keycloakid ba500049-…
```

El mundo de equipos (`equipos_participantes`, `historial_nombre_equipo`, `invitaciones_equipo`) se
indexa por el **sub**, pese a que sus columnas y propiedades se llaman `usuarioid` / `UsuarioId`.
**El nombre miente**, y como ambos son `Guid`, el compilador no puede impedir confundirlos.

Ya se cobró un bug real (arreglado en `dfc56c7`): `GetParticipantesElegiblesQueryHandler` devolvía
`usuario.UsuarioId` porque el campo del mundo de equipos se llamaba igual. La invitación se creaba
bajo el id local, el invitado la buscaba con su sub, y no la veía nunca. El mismo id equivocado
anulaba además dos guardas de negocio. Tres síntomas, una línea.

Hay evidencia de que ya había mordido antes: `ListarEquiposQueryHandler:25` y
`ResolverNombresQueryHandler` llevan comentarios puestos a mano avisando del peligro. Alguien pagó
el peaje y dejó una nota en vez de arreglar la causa.

## Decisiones tomadas

### El bug siempre va en una dirección

El id local se filtra **hacia** el mundo de equipos, nunca al revés: nadie toma un sub y lo mete en
`usuarios`. Esa asimetría es la que abarata el diseño.

En producción hay **11 usos** de `Usuario.UsuarioId` (el id local), contra ~470 sitios que nombran
ids en el mundo de equipos. El id local es el raro **y** es el que se fuga: tiparlo a él detiene la
fuga sin tocar los 470.

Los 11: `CambiarRolUsuarioCommandHandler` (4), `UsuarioRepository` (2), y uno en cada uno de
`CreateUserWithInitialRoleCommandHandler`, `DeactivateUserCommandHandler`,
`UpdateUserGeneralDataCommandHandler`, `GetUserByIdQueryHandler`, `GetUsersQueryHandler`. Todos son
flujos de administración de usuarios, donde el id local es el **correcto** — por eso el tipado no
los rompe, solo los obliga a desenvolver en el borde.

### Tipar el local, renombrar el resto

**Se tipa** `Usuario.UsuarioId` → `UsuarioLocalId`. Filtrarlo al mundo de equipos deja de compilar.

**Se renombra** el mundo de equipos, que sigue siendo `Guid`. Una vez tipado el id local, el
compilador ya no hace falta ahí: el renombrado es para el lector, no para el compilador.

Alternativas descartadas:

- **Tipar los dos mundos** (`UsuarioLocalId` + un tipo para el sub): ~500 sitios entre src y tests,
  casi todo churn mecánico, para una garantía que el tipado del local ya da.
- **Solo renombrar**: ~40 sitios, pero ambos siguen siendo `Guid` y el compilador sigue sin impedir
  nada. Quita la mentira y deja la trampa.

### El nombre: `SubjectId`

`sub` es el término del estándar **OIDC**, no de Keycloak. Cualquier proveedor OIDC emite un
subject, así que el nombre sigue siendo cierto si algún día se migra de proveedor — y no mete el
nombre del vendor dentro del dominio.

Descartados:
- `SubKeycloak` / `KeycloakSub`: explícito sobre hoy, pero mete el vendor en el dominio y miente el
  día que se cambie de proveedor.
- `CompetidorId`: choca con Puntuaciones, donde `competidorid` es polimórfico (persona **o** equipo,
  según `tipocompetidor`). En el mundo de equipos de Identity siempre es una persona.

### El límite del renombrado

Se renombra **estado persistido**: entidades de dominio, mapeo EF y columnas. Ahí es donde un lector
se forma una idea equivocada del sistema, y es donde el bug se demostró con SQL.

**No** se renombran parámetros locales ni variables de handlers (`request.ActorUserId`, `liderUserId`,
etc.): son ~400 sitios de churn mecánico, son locales y no engañan a nadie que lea el modelo. El
costo del slice baja de ~470 a ~70 sitios.

Consecuencia aceptada: durante un tiempo convivirán `request.ActorUserId` (parámetro) y
`ParticipanteEquipo.SubjectId` (estado). El parámetro no miente — solo es genérico.

## Alcance

### Se toca

**Dominio (Identity):**
- Nuevo: `Domain/ValueObjects/UsuarioLocalId.cs` — carpeta nueva en este servicio, copiando el
  patrón que Partidas ya usa con `PartidaId`:
  ```csharp
  public readonly record struct UsuarioLocalId(Guid Valor)
  {
      public static UsuarioLocalId New() => new(Guid.NewGuid());
      public static UsuarioLocalId From(Guid valor) => new(valor);
  }
  ```
- `Usuario.UsuarioId`: `Guid` → `UsuarioLocalId`. `Usuario.Crear` usa `UsuarioLocalId.New()`.
- Renombrados (siguen `Guid`):

  | Hoy | Queda |
  |---|---|
  | `ParticipanteEquipo.UsuarioId` | `SubjectId` |
  | `InvitacionEquipo.InvitadoUserId` | `InvitadoSubjectId` |
  | `InvitacionEquipo.InvitadoPorUserId` | `InvitadoPorSubjectId` |
  | `HistorialNombreEquipo.UsuarioId` | `SubjectId` |

**Infraestructura:**
- `IdentityDbContext`: `ValueConverter<UsuarioLocalId, Guid>` para `Usuario.UsuarioId` (patrón
  `PartidasDbContext:19-20`), y `HasColumnName` de las propiedades renombradas.
- Migración de renombrado de columnas:

  | Tabla | Hoy | Queda |
  |---|---|---|
  | `equipos_participantes` | `usuarioid` | `subjectid` |
  | `historial_nombre_equipo` | `usuarioid` | `subjectid` |
  | `invitaciones_equipo` | `invitadouserid` | `invitadosubjectid` |
  | `invitaciones_equipo` | `invitadoporuserid` | `invitadoporsubjectid` |

  `RenameColumn` preserva los datos; no hace falta backfill. Datos vivos al escribir este spec:
  2 usuarios, 3 membresías, 3 de historial, 0 invitaciones.

**Aplicación:**
- `IUsuarioRepository.GetByIdAsync(Guid)` → `GetByIdAsync(UsuarioLocalId)`; `ExistsByEmailAsync`
  toma `UsuarioLocalId?` en `excludingUserId`.
- Los 11 sitios que consumen `Usuario.UsuarioId` desenvuelven con `.Valor` al construir DTOs y
  eventos.
- Los handlers del mundo de equipos pasan a nombrar `SubjectId` donde leían `UsuarioId`.

### No se toca

- **Contratos HTTP.** Los DTOs siguen exponiendo `Guid` crudo — `UserDetailResponse(Guid UserId, string KeycloakId, …)`,
  `ParticipanteElegibleResponse(Guid UserId, …)`, etc. El tipo fuerte vive **solo dentro del
  dominio** y se desenvuelve en el borde, igual que `PartidaId` en Partidas (`PartidaSummaryDto`
  expone `Guid PartidaId`). Web y móvil no se enteran.
- **Contratos de eventos.** Los integration events siguen llevando `Guid`.
- **Operaciones de Sesión, Puntuaciones, Partidas, gateway, frontend, mobile.** La confusión solo es
  posible donde coexisten los dos ids, que es únicamente Identity: los otros servicios guardan el sub
  pero nunca ven el `UsuarioId` local, así que no hay nada que confundir. Tiparlo allí no compraría
  nada.
- **`Usuario.KeycloakId`** (string). Renombrarlo a `IdpSubject` sería coherente con `SubjectId`, pero
  arrastra a los 9 handlers que hacen el join y a los DTOs que lo exponen (`UserDetailResponse.KeycloakId`
  es contrato). Queda fuera: es cosmético y este slice ya justifica su costo sin él.
- **La convención de fondo**: que el sub sea la identidad del competidor en todo el sistema no
  cambia. Este slice hace que la convención sea *legible y difícil de violar*, no la revierte.
  Revertirla es otra discusión (ver "Lo que este slice no resuelve").

## Verificación

- **Test de regresión del bug real**: un test que fije que `GetParticipantesElegibles` devuelve el
  subject y no el id local ya existe (`dfc56c7`) y debe seguir verde sin tocarlo.
- **Test del tipo**: `UsuarioLocalId` no es `Guid` y no hay conversión implícita. Se fija con un test
  de dominio sobre `New()`/`From()`/igualdad; la garantía real es de compilación, y el compilador
  la ejerce en cada build.
- **Round-trip EF**: `Usuario.UsuarioId` sobrevive el `ValueConverter` (patrón de los tests de
  persistencia que ya existen).
- **Migración**: las 4 columnas renombradas se verifican a mano contra Postgres con `\d`. **Ningún
  test cubre las migraciones** en este repo — es un hueco sistémico preexistente, no de este slice.
- **Línea base**: la suite de Identity está en 345 verdes (249 unit + 49 integration + 47 contract)
  al escribir este spec. Debe seguir en 345 o más, sin tests modificados salvo los que nombran las
  propiedades renombradas.

## Riesgos

- **Churn amplio y mecánico.** El renombrado toca muchos archivos a la vez. El rojo del ciclo TDD
  será un **error de compilación**, no una aserción fallida — C# no permite otra cosa al cambiar un
  tipo o un nombre público. Está previsto.
- **Un `.Valor` distraído.** Ningún sistema de tipos impide que alguien escriba `.Valor` sin pensar
  y vuelva a filtrar el id local. El tipado sube la fricción y hace visible el cruce al lector; no
  lo hace imposible. Es una mejora de probabilidad, no una garantía — y conviene no venderla como
  garantía.
- **Renombrar columnas invalida SQL escrito a mano** que ande fuera del repo (scripts sueltos,
  consultas guardadas). Dentro del repo, EF es el único consumidor.

## Lo que este slice no resuelve

El **lock-in**: el valor del sub sigue regado como identidad del competidor en las tres bases
(`inscripciones`, `respuestas_trivia`, `tesoros_qr`, `marcadores.competidorid`,
`participaciones_proyectadas`, `eventos_historial`…). Migrar de proveedor de identidad seguiría
exigiendo reescribir ese id en tres servicios y en eventos ya publicados — y `eventos_historial` es
auditoría, que no se recalcula.

La salida sería que el token cargue un claim `usuario_id` propio y que cada servicio lea ese claim
en su borde, dejando el sub como detalle de infraestructura de Identity. Es viable (el realm es
declarativo e Identity ya provisiona los usuarios en Keycloak con un puerto admin), pero es un slice
multi-servicio con ADR propio.

**Este slice no lo hace y no lo bloquea**: al dejar el subject nombrado como lo que es, hace ese
cambio futuro más fácil de razonar, no más difícil.
