# Diseño — Bloque 6: CI (RNF-11) + medición de cobertura (RNF-09) + compose completo (RNF-10)

Fecha: 2026-07-06
Rama: `feature/bloque6-ci` (desde `develop` `d956b58` — integración SP-4+SP-5)
Fuente: auditoría de cobertura de requisitos (`docs/04-sdd/auditorias/2026-07-06-auditoria-cobertura-requisitos.md`, Bloque 6)

## Contexto y problema

- **RNF-11** exige un pipeline de integración continua para compilación y ejecución de pruebas. **No existe** (`.github/workflows/` ausente). La calidad depende hoy de correr a mano 7 suites (~1.183 tests); durante la reconciliación SP-4+SP-5 nadie había corrido las suites cruzadas del árbol combinado hasta el final — con dos personas integrando a `develop`, el riesgo es real y ya se materializó como casi-incidente.
- **RNF-09** exige como meta académica ≥90% de cobertura **de backend**. Jamás se ha medido: no hay instrumento ni línea base, así que hoy no se puede afirmar ni negar el cumplimiento.
- **RNF-10** exige que la solución pueda ejecutarse localmente con Docker Compose. El compose actual está **a medias y roto**: define `identity-service` y `gateway` además de la infra, pero identity no tiene `Dockerfile`, el gateway hace `depends_on` de `partidas`/`operaciones-sesion`/`puntuaciones` que **no están definidos**, y ningún servicio aplica migraciones EF al arranque (contra una DB fresca los contenedores arrancarían sin esquema).

Hechos verificados: remoto GitHub `PySanti/ProyectoDesarrollo` (Actions disponible); `coverlet.collector` ya presente en los proyectos de test .NET; Node local 20.19.4 (mobile exige ≥20.19.4); .NET SDK 8.0; las suites no requieren servicios externos (los tests opt-in de RabbitMQ retornan vacío sin `RABBITMQ_TEST_HOST`); Dockerfiles existentes: gateway, partidas, operaciones-sesion, puntuaciones (falta identity); puertos locales de gateway→servicios: identity 5000, partidas 5010, operaciones 5020, puntuaciones 5030.

## Decisiones (confirmadas con el usuario)

1. **Triggers:** push a `develop` y `master` + todos los pull requests.
2. **Cobertura report-only:** el CI calcula y publica el % por servicio + total backend en cada corrida; **no** falla por cobertura baja. El gate duro se evaluará cuando exista línea base. Alcance de la métrica: **solo las 5 soluciones .NET** (RNF-09 dice "backend"); frontend/mobile corren tests sin métrica.
3. **Alcance del slice:** incluye RNF-10 (compose completo), además de RNF-11 y RNF-09.
4. **CI no construye imágenes Docker:** el compose se verifica localmente como gate del slice; el pipeline queda rápido (~5-6 min).
5. **Estructura del workflow — enfoque A:** un workflow, jobs paralelos por componente. Descartado B (secuencial: lento, un fallo tapa el resto) y C (path-filters: un cambio de contrato en un servicio no dispararía las suites de sus consumidores — exactamente el bug cross-service que el CI debe cazar).

## Diseño

### 1. Workflow `.github/workflows/ci.yml` (RNF-11)

```yaml
on:
  push: { branches: [develop, master] }
  pull_request: {}
```

**8 jobs:**

- **`backend` (matrix, 5 entradas):** `{ name, sln }` para `puntuaciones`, `identity`, `partidas`, `operaciones-sesion`, `gateway` con sus rutas `.sln` reales. Steps: checkout → `actions/setup-dotnet` (8.0.x) → `dotnet test "<sln>" --collect:"XPlat Code Coverage" --results-directory <dir>` → `actions/upload-artifact` con los `coverage.cobertura.xml`. Sin servicios auxiliares: los tests de RabbitMQ opt-in retornan vacío sin `RABBITMQ_TEST_HOST`.
- **`frontend`:** checkout → `actions/setup-node` (20) → `npm ci` → `npm test` → `npm run build` (compila `tsc` + vite; pesca errores de tipos que `npm test` no ve).
- **`mobile`:** checkout → setup-node (20) → `npm ci` → `npm test` → `npm run typecheck`.
- **`cobertura`** (`needs: backend`): descarga los 5 artifacts → `reportgenerator` (dotnet tool) fusiona los Cobertura XML → publica en `$GITHUB_STEP_SUMMARY` una tabla Markdown con **% de línea por servicio + total backend**, y sube el reporte HTML como artifact. **Sin umbral que falle** (decisión 2).

Cada job es independiente: un ❌ señala exactamente el componente roto; la duración total ≈ el job más lento.

### 2. Medición de cobertura (RNF-09)

- `coverlet.collector` ya está en los proyectos de test; `--collect:"XPlat Code Coverage"` genera `coverage.cobertura.xml` por proyecto de test sin cambiar ningún csproj.
- `reportgenerator` consolida por solución y global. La cifra queda visible y auditable en el summary de **cada** corrida — RNF-09 pasa de "no medido" a "medido continuamente"; la meta 90% se gestiona con la línea base a la vista.

### 3. Compose completo (RNF-10)

- **`services/identity-service/Dockerfile`** nuevo, espejo del de partidas (multi-stage sdk→aspnet, publica `Umbral.IdentityService.Api`).
- **3 entradas nuevas** en `infra/docker-compose.yml` espejo de la existente de identity: `partidas` (5010:8080), `operaciones-sesion` (5020:8080), `puntuaciones` (5030:8080); cada una con su connstring `Host=postgres;Port=5432;Database=umbral_<servicio>`, `RabbitMq` host `rabbitmq`, y env de Keycloak. Con esto el `depends_on` del gateway (ya escrito) deja de estar roto.
- **Init de bases:** script `infra/postgres-init/create-databases.sql` montado en `/docker-entrypoint-initdb.d` con los 4 `CREATE DATABASE` (corre solo con volumen fresco; para volúmenes existentes sigue valiendo el procedimiento manual del CLAUDE.md).
- **Migraciones al arranque, gated:** en el `Program.cs` de los 4 servicios (identity, partidas, operaciones-sesion, puntuaciones), si `EF_MIGRATE_ON_STARTUP=true` → `db.Database.Migrate()` al arrancar. Default **off**: `dotnet run` local, tests y comportamiento actual quedan idénticos; solo el compose activa la variable. **Único cambio a código de producción del slice** (~6 líneas por servicio + registro en compose).
- **Issuer/audiencia dentro de la red Docker:** los contenedores usan Authority `http://keycloak:8080/realms/UMBRAL-UCAB`; `KEYCLOAK_VALID_ISSUERS` incluye además `http://localhost:8080/realms/UMBRAL-UCAB` porque los tokens emitidos vía navegador llevan el issuer del host. Solo configuración de env ya soportada — cero código.

### 4. Fuera de alcance

- Gate duro de cobertura (se decide con línea base).
- Construcción/validación de imágenes Docker en CI (decisión 4).
- Publicación de imágenes (registry), CD/despliegue, badges en README.
- Cobertura de frontend/mobile (RNF-09 es de backend).
- E2E con navegador en CI (el harness Playwright `gov-visual-pass.mjs` sigue siendo manual).

## Verificación

1. **Local:** `docker compose -f infra/docker-compose.yml up -d --build` con volumen fresco → los 8 contenedores arriba; curl 200/401-esperado a los health/endpoints de los 5 componentes directo y a través del gateway; realm Keycloak importado.
2. **CI real:** push de `feature/bloque6-ci` a GitHub (decisión explícita del usuario en ese momento — la regla de no-push sigue) → los 8 jobs verdes en Actions → summary con la tabla de cobertura y la **línea base documentada** en el ledger.
3. Las 7 suites locales siguen verdes (el único cambio de código es el hook de migración gated, default off).

## Criterios de aceptación

- `.github/workflows/ci.yml` existe; en GitHub Actions corren 8 jobs en push a develop/master y en PRs; todos verdes sobre el árbol actual.
- El summary del workflow muestra % de cobertura por servicio backend + total; la línea base queda registrada.
- `docker compose up -d --build` levanta postgres + rabbitmq + keycloak + los 4 servicios + gateway desde cero (volumen fresco), con esquema aplicado y endpoints respondiendo vía gateway.
- `dotnet run` local y todas las suites quedan sin cambio de comportamiento (hook de migración off por default).
- Cero cambios en contratos, dominio, HUs.
