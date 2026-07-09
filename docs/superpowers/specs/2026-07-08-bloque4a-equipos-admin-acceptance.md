# Bloque 4A — equipos-admin — Acceptance

Fecha: 2026-07-08 · Rama: `feature/bloque4a-equipos-admin`
Spec: `2026-07-08-bloque4a-equipos-admin-design.md` · Plan: `../plans/2026-07-08-bloque4a-equipos-admin.md`

Ejecutado subagent-driven en 23 tasks (A1..I3), cada una con revisión de spec+calidad (review-clean).
Suites verdes en HEAD: Identity **312/312**, Operaciones **458**, web **84/84** (+build), mobile **112/112** (+typecheck).

## Criterios por historia de usuario

### HU-06 — El líder elimina el equipo que lidera
- [x] `DELETE /identity/teams/mine` elimina el equipo del líder **aunque tenga integrantes** (soft-delete `Estado=Eliminado`; `Equipo.EliminarPorLider` no exige equipo vacío, a diferencia de `Salir`). Handler `EliminarMiEquipoCommandHandler`.
- [x] Un no-líder recibe 403 (`NoEsLiderException`); sin equipo activo → 404.
- [x] Al eliminar se borran las invitaciones **pendientes** (BR-E06) y se conserva el historial (BR-E11 — las filas de `historial_nombre_equipo` nunca se borran; los integrantes quedan libres porque las queries filtran `Estado==Activo`).
- [x] Se publica `EquipoEliminado` (`origen:"Lider"`, lista de miembros) y se notifica por correo a todos los integrantes (best-effort SMTP).
- [x] **Guard BR-E10:** si el equipo participa en una partida `Lobby`/`Iniciada`, la eliminación devuelve **409** y no produce efecto alguno (verificado con test de ausencia de efectos).
- [x] Mobile: pantalla "Eliminar equipo" con paso de **confirmación** antes de la llamada destructiva; en éxito navega a Home; el 409 muestra "…participa en una partida activa…".

### HU-09 — El administrador gestiona equipos
- [x] `AdminTeamsController` (`identity/admin/teams`, policy `AdminOnly`): listar (todos los estados), detalle (404 si no existe), crear (201+Location), renombrar, reasignar liderazgo, cambiar estado (Activo↔Desactivado), eliminar (204).
- [x] Crear exige un líder válido (`UserNotFoundException`→404) que no esté en otro equipo activo (409); el líder es el primer y único integrante inicial. **El admin no compone membresía (BR-E05 intacta):** no hay endpoint de agregar/quitar integrantes; editar = renombrar + reasignar liderazgo entre integrantes existentes.
- [x] **Identidad del líder:** el body de crear lleva el `Usuario.UsuarioId` **local** (el del directorio de usuarios); el handler lo resuelve y usa `Guid.Parse(usuario.KeycloakId)` como clave de membresía/historial/evento, de modo que el líder puede acceder al equipo desde mobile (el `sub` del JWT == KeycloakId). Cubierto por test dedicado.
- [x] Reasignar liderazgo publica `LiderazgoEquipoModificado` (`origen:"Admin"`) y notifica al líder anterior y al nuevo.
- [x] Web: página admin "Equipos" (`identidad/equipos`) con tabla + crear/renombrar/reasignar/desactivar-reactivar/eliminar (con confirmación y mensaje 409 de BR-E10).

### HU-48 — El participante ve su historial de nombres de equipo
- [x] `GET /identity/teams/mine/history` → 200 `{ historial: [{ nombreEquipo, equipoId, fechaRegistro }] }` ordenado ascendente por fecha; **siempre 200, lista vacía si no hay** (nunca error).
- [x] Se registra una fila al entrar a un equipo (creación, invitación aceptada, alta admin) y N filas (una por integrante) al renombrar. Backfill idempotente al arranque para equipos preexistentes.
- [x] Mobile: pantalla "Historial de equipos" con lista y estado vacío ("Aún no perteneces a ningún equipo"); lista vacía es estado de éxito, no error.

## Criterios por regla de negocio
- [x] **BR-E06** — borrado explícito de equipo (líder o admin) elimina las invitaciones pendientes y conserva la historia.
- [x] **BR-E10** — (1ª mitad) un equipo `Desactivado` no puede inscribirse en partidas nuevas: `GET /identity/teams/mine` solo devuelve equipos `Activo`, y la preinscripción de Operaciones rechaza 409 "sin equipo activo". (2ª mitad) no se elimina un equipo con participación activa en partida `Lobby`/`Iniciada` → 409, vía la proyección `participaciones_activas_equipo`.
- [x] **BR-E11** — historial de nombres por participante, preservado tras el borrado (soft-delete + filas de historial nunca borradas).

## Caveats aceptados
- **Consistencia eventual del guard BR-E10:** la proyección se alimenta por eventos RabbitMQ (`InscripcionEquipoCreada`/`Cancelada`/`PartidaFinalizada`/`PartidaCancelada`); una inscripción hecha instantes antes de un borrado puede no estar proyectada aún. Elegido deliberadamente sobre una consulta HTTP síncrona cross-service. Ventana de milisegundos-segundos en operación normal.
- **Cold start de la proyección:** al desplegar, la tabla arranca vacía; inscripciones de equipo activas previas al despliegue no estarían proyectadas hasta re-emitirse. Aceptable en el estado actual (sin producción).
- **Correo best-effort por SMTP directo:** un fallo de envío se loggea y no revierte la operación de dominio. El Bloque 5 (RNF-23) moverá el correo a consumidor RabbitMQ sin cambiar los eventos de este slice.

## Fuera de alcance (slice 4B)
- **HU-19** (el operador acepta/rechaza inscripciones en el lobby) vive en Operaciones de Sesión y cambia el flujo de inscripción; se trata como slice **4B** con su propio ciclo spec→plan.
