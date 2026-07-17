# Nombre de partida en el historial del participante (móvil) (plan)

Diseño: `docs/superpowers/specs/2026-07-15-nombre-partida-historial-movil-design.md`
HU gobernante: HU-27 (refinamiento transversal — no introduce HU nueva)

Una tarea a la vez. Cada tarea deja la suite verde antes de pasar a la siguiente.

## Backend — Operaciones de Sesión

### T1 — Contrato de lectura en el repositorio

- `Domain/`: record `NombrePartidaProyeccion(Guid PartidaId, string Nombre)` junto a
  `ISesionPartidaRepository`.
- `ISesionPartidaRepository.GetNombresByPartidaIdsAsync(IReadOnlyList<Guid>, CancellationToken)`.
- Implementación EF en `Infrastructure/Persistence/`: `Where(s => ids.Contains(s.PartidaId))` +
  `Select` de los dos campos. No materializar `SesionPartida` entera.
- Actualizar `FakeSesionPartidaRepository` (tests) para la firma nueva.

Verificación: la solución de Operaciones compila y su suite sigue verde.

### T2 — Query + validator + handler

- `Queries/ResolverNombresPartidaQuery(IReadOnlyList<Guid> PartidaIds)`.
- `DTOs/NombrePartidaDto(Guid PartidaId, string Nombre)` +
  `DTOs/ResolverNombresPartidaResponse(IReadOnlyList<NombrePartidaDto> Partidas)`.
- `Validators/ResolverNombresPartidaQueryValidator` — `PartidaIds.Count <= 200`.
- `Handlers/Queries/ResolverNombresPartidaQueryHandler` — lista vacía → `partidas: []` sin tocar el
  repositorio; ids desconocidos simplemente no vuelven.

Tests (unit): handler resuelve por lote · id desconocido omitido · lista vacía no toca repositorio ·
validator acepta 200 y rechaza 201.

### T3 — DirectoryController ✅

- `Application/DTOs/ResolverNombresPartidaRequest { Guid[]? PartidaIds }` — **no** `Api/Contracts/`:
  esa carpeta no existe en Operaciones y la regla graded pide requests en `DTOs/`.
- `Api/Controllers/DirectoryController` — `[ApiController]`, `[Route("operaciones-sesion/directory")]`,
  `[Authorize]` de clase, `[HttpPost("partidas")]`. **Despachador puro**: no inyecta `IValidator<T>`
  como el de Identity, porque Operaciones valida en el `ValidationBehavior` del pipeline de MediatR
  y el middleware mapea la `ValidationException` a 400 (doctrine audit M-2).

Tests (unit de controller — regla graded): 200 en caso feliz · body sin `partidaIds` → lote vacío.
El 400 del tope se movió a T4: sin pipeline, `FakeSender` no puede producirlo.

### T4 — Integration + contract ✅

- Integration (4): nombre resuelto tras persistir · solo los ids pedidos · id desconocido omitido ·
  **partida en estado terminal resuelta** (el caso real del historial).
- Contract (6): forma `{ partidas: [{ partidaId, nombre }] }` · id desconocido → 200 vacío · body sin
  `partidaIds` → vacío · **200 con rol `Participante`** (el punto del slice) · 401 sin token ·
  **400 del tope** con el pipeline real, cuerpo `{ message }` (no `ValidationProblemDetails`).
- **Corrección:** el integration test NO prueba la traducción a SQL. Todo el suite de persistencia de
  Operaciones usa InMemory; la traducción Npgsql de `partidaIds.Contains(...)` queda sin verificar,
  igual que la de todos los demás métodos del repositorio. Hueco sistémico preexistente, anotado en
  el diseño § Testing y no resuelto en este slice.

Verificación: suite completa de Operaciones verde + mutación del handler comprobada (tumba 2 unit y
2 contract, así que los tests muerden).

## Cliente — móvil

### T5 — Cliente HTTP del directorio

- `mobile/src/features/shared/partidaDirectoryApi.js` — `resolverNombresPartida(...)`, espejo exacto
  de `directoryApi.js`, reusando `mapCommonError`/`networkError` de `partidasPublicadasApi.js`.

Tests (`node --test`): ok · error mapeado · fallo de red.

### T6 — Hook de resolución

- `mobile/src/features/shared/useNombresPartida.js` — caché de módulo, `trocearPartidas` (lotes de
  200), `resetNombresPartidaCache`, fallback a GUID corto. Forma de `useNombres.js`.

Tests (`node --test`): `trocearPartidas` puro · resuelve y cachea · degrada al GUID si `{ ok: false }`.

### T7 — HistorialPartidasScreen

- Título → nombre; `{modalidad} · {puntos} pts` baja a la línea de contexto con posición y fecha.
- Sin tocar `data-testid` ni roles.

### T8 — RendimientoEquipoScreen

- Título → nombre; `Posición {n}` baja a la línea de contexto.

### T9 — Fix de "Mi partida"

- `PartidasPanelScreenContainer.tsx:25-26` — resolver el nombre antes de navegar; conservar
  `"Mi partida"` como fallback si el resolver falla.

Verificación tras T7–T9: `npm test` y `npm run typecheck` verdes en `mobile/`.

## Cierre

### T10 — Documentación

- `contracts/http/operaciones-sesion-api.md` — endpoint + DTO en la tabla de capacidades; matriz de
  autorización, fila "Lectura compartida" de 5 → 6 entradas.
- `docs/04-sdd/SPECS-LIST.md` — fila del slice.
- `docs/04-sdd/traceability-matrix.md` — fila del slice.

## Verificación final

- `dotnet test services/operaciones-sesion/<Solution>.sln` verde.
- `cd mobile && npm test && npm run typecheck` verdes.
- **Regresión esperada cero**: ningún test existente debería cambiar. Si alguno cambia, parar y
  entender por qué antes de tocarlo.
- Puntuaciones, Partidas, gateway y frontend web: **sin tocar**. Confirmar con `git diff --stat`.
