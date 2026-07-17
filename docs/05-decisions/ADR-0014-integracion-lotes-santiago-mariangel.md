# ADR-0014 — Integración de los lotes paralelos de fixes (Santiago y Mariangel) en develop

- **Estado:** Aceptado (2026-07-17)
- **Contexto:** Dos lotes de correcciones se desarrollaron en paralelo sobre puntos de partida
  distintos: `feature/fixes-santiago` (74 commits sobre el tip de develop `c5f3c2f`) y
  `feature/fixes-mariangel-qr-y-nombres` (82 commits sobre `aacdcd8` = develop~9, anterior a los
  squashes de los bloques 2/3/4/7). Ambos corrigieron los mismos problemas en varios puntos
  (elegibilidad de invitados por sub de Keycloak, refresh de sesión, pantallas de equipos), y
  Mariangel además reconstruyó su rama sobre el linaje *original* de los bloques que develop
  recibió como squashes — por eso el merge produjo **152 archivos en conflicto** repartidos en
  todas las áreas (identity 44, frontend 40, operaciones-sesión 39, mobile 16, resto 13).
  Siguiendo `GUIA-USO-AGENTE.md`, cada rama se comprimió a **un solo commit** antes de fusionar
  (`4bfd4cf` Santiago, `cbf55fc` Mariangel; merge `9586190`). Backups:
  `backup/fixes-santiago-before-squash`, `backup/fixes-mariangel-before-squash`.

## Decisión: lado ganador por área

La resolución no fue archivo a archivo con un criterio único, sino **sesgo por área con
excepciones justificadas**, decidido por el equipo:

| Área | Gana | Qué se conserva |
|---|---|---|
| `services/identity-service` | **Santiago** | Gobernanza de dos privilegios, sync incondicional usuario↔Keycloak (login por correo nuevo), correos de ciclo de vida por RabbitMQ, `YaInvitado`, `Participantes.Clear()` al eliminar equipo. |
| `services/operaciones-sesion` | **Mariangel** | Su versión es superset del flujo de inscripciones: convocatoria del líder nace Aceptada (sin saltarse BR-G09), `destinatarios` en la entrega SignalR, `ConvocatoriaRespondida` para proyecciones, reenganche al grupo tras reconexión. |
| `frontend` y `mobile` — partidas y puntuaciones | **Mariangel** | QR del tesoro generado por el operador (no tecleado), columna/orden por `fechaCreacion`, nombres reales en rankings y convocatorias (`useNombres`), historial con nombre de partida, rendimiento por participación. |
| `frontend` y `mobile` — auth, identity, shell | **Santiago** | `refresh(): Promise<AuthUser>` (re-parsea privilegios de gobernanza en vivo), navegación y rutas gateadas por privilegio (`Require have={permisos}`), validación de inputs por campo (`extractFieldErrors`, `Field`, `validation`), páginas de equipos con viñetas, panel de gestión de equipo mobile. |
| `gateway` | **Santiago** | Políticas por privilegio (`GestionarPartidas`/`GestionarEquipos`, ADR-0013). El modelo por rol de la otra rama (`OperadorOAdministrador`) queda **superseded**. |
| `infra/docker-compose.yml` | **Santiago** | `KEYCLOAK_CLIENT_SECRET` con `:?` (falla ruidosa sin `--env-file .env`) e `IMPORT_CACHE_ENABLED: "true"` (con `false`, cada `up` borra los composites gobernados por el panel). |
| `contracts/`, `docs/`, guías | **Unión** | Fusión aditiva de ambos lados (merge-file --union). |

## Excepciones (donde el sesgo por área se rompió a propósito)

1. **Feature Directory restaurada dentro de identity.** "identity = Santiago" borraba
   `/identity/directory` (resolver nombres por sub), pero la web y el móvil de Mariangel la
   consumen (`useNombres`, `directoryApi`). Se restauró el slice completo desde su rama
   (controller, query, handler, validator, tests) adaptando solo las firmas del fake de test.
2. **Rename `UsuarioId→SubjectId` revertido.** El refactor de dominio de Mariangel (rename +
   value object `UsuarioLocalId`) se coló por auto-merge en archivos sin conflicto y dejaba
   identity a medias (métodos duplicados en `Equipo.cs`, interfaces incompatibles). Decisión:
   **todo identity vuelve a `UsuarioId`** (reset del subárbol a la versión de Santiago);
   `UsuarioLocalId` y sus tests quedan fuera. Si el rename se quiere de verdad, se rehace como
   refactor propio sobre develop, no como efecto colateral de un merge.
3. **`createPartidaDraft` es mezcla real:** validación "al menos una letra" de Santiago +
   mensaje "Genera el código QR de la etapa N" de Mariangel (coherente con su flujo de QR
   generado).
4. **`HomeScreen` mobile es mezcla real:** tarjeta "Gestión de equipo" de Santiago (las acciones
   de equipo viven dentro de su panel) + tarjeta "Rendimiento de mi equipo" de Mariangel
   (pantalla nueva suya).

## Alternativas descartadas

- **Blend fino de los 152 archivos:** correcto pero de costo desproporcionado; el muestreo mostró
  que en la mayoría de archivos un lado era superset estricto del otro.
- **Rebase de la rama de Mariangel sobre develop antes de fusionar:** habría repetido los mismos
  152 conflictos commit a commit (82 veces peor).

## Consecuencias

- develop queda con las **dos** familias de features; donde ambos resolvieron lo mismo, quedó
  **una** implementación (la de la tabla), no las dos.
- El rename `SubjectId` de Mariangel se pierde en identity (queda solo en operaciones-sesión,
  donde era suyo el dominio). Deuda consciente: nomenclatura `UsuarioId` en identity guarda subs
  de Keycloak en el mundo de equipos — el comentario que lo aclara vive en
  `GetParticipantesElegiblesQueryHandler`.
- Verificación del merge: identity 276+54, operaciones 429+86, partidas 110+16, puntuaciones
  170+26, gateway 30 tests; frontend 332 tests + typecheck; mobile 187 tests + typecheck. Los
  tests de integración con Postgres no corrieron (stack caído al momento del merge) — correrlos
  al siguiente levantamiento.
- Las ramas `backup/*` y `fixes-mariangel` locales se conservan hasta validar develop en vivo.
