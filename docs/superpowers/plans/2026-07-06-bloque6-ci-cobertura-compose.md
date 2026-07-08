# Bloque 6 — CI + cobertura + compose completo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pipeline de CI en GitHub Actions (RNF-11) con cobertura backend visible por corrida (RNF-09, report-only) y Docker Compose que levanta la solución completa (RNF-10).

**Architecture:** Un workflow con 8 jobs paralelos (5 .NET por matrix + frontend + mobile + agregador de cobertura). Compose: completar las entradas ya existentes (env Keycloak/RabbitMQ/URLs inter-servicio), Dockerfile de identity faltante, init SQL de las 4 bases, y migraciones EF al arranque gated por `EF_MIGRATE_ON_STARTUP` (único cambio a código de producción).

**Tech Stack:** GitHub Actions, coverlet (ya instalado), reportgenerator, Docker Compose, .NET 8, Node 20.

## Global Constraints

- Spec de referencia: `docs/superpowers/specs/2026-07-06-bloque6-ci-cobertura-compose-design.md`.
- Rama: `feature/bloque6-ci`. Cero cambios en contratos, dominio, HUs, gateway routing, o suites existentes.
- Cobertura **report-only** (ningún umbral que falle) y **solo backend** (las 5 soluciones .NET).
- CI **no** construye imágenes Docker.
- `EF_MIGRATE_ON_STARTUP` default **off**: `dotnet run` local y tests quedan idénticos; solo el compose lo activa.
- Las entradas legacy del compose (`trivia-game-service`, `bdt-game-service`) **no se tocan** (se retiran en el Bloque 3, otro slice).
- Git: stage SOLO archivos exactos uno por uno (nunca directorios, nunca `-A`); prohibido `git stash/reset/checkout/restore/clean`.
- Trailer de commit, exacto y con línea en blanco antes: `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`
- Comandos de test: `dotnet test "<sln>"` por servicio; frontend `npm test` + `npm run build`; mobile `npm test` + `npm run typecheck`.

---

### Task 1: Workflow `.github/workflows/ci.yml`

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: rutas reales de las 5 soluciones; `coverlet.collector` ya presente en los proyectos de test.
- Produces: workflow `CI` con jobs `backend-<name>` (5), `frontend`, `mobile`, `cobertura`. La Task 6 lo corre de verdad en GitHub.

- [ ] **Step 1: Escribir el workflow**

Contenido completo de `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [develop, master]
  pull_request:

jobs:
  backend:
    name: backend-${{ matrix.name }}
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        include:
          - name: puntuaciones
            sln: services/puntuaciones/Umbral.Puntuaciones.sln
          - name: identity
            sln: services/identity-service/Umbral.IdentityService.sln
          - name: partidas
            sln: services/partidas/Umbral.Partidas.sln
          - name: operaciones-sesion
            sln: services/operaciones-sesion/Umbral.OperacionesSesion.sln
          - name: gateway
            sln: gateway/Umbral.Gateway.sln
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - name: Test con cobertura
        run: dotnet test "${{ matrix.sln }}" --collect:"XPlat Code Coverage" --results-directory coverage-results
      - uses: actions/upload-artifact@v4
        with:
          name: coverage-${{ matrix.name }}
          path: coverage-results/**/coverage.cobertura.xml

  frontend:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm test
      - run: npm run build

  mobile:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: mobile
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: mobile/package-lock.json
      - run: npm ci
      - run: npm test
      - run: npm run typecheck

  cobertura:
    needs: backend
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          pattern: coverage-*
          path: artifacts
      - name: Instalar reportgenerator
        run: dotnet tool install -g dotnet-reportgenerator-globaltool
      - name: Resumen por servicio + total backend
        run: |
          {
            echo "## Cobertura backend (líneas)"
            echo ""
            echo "| Servicio | Cobertura |"
            echo "|---|---|"
          } >> "$GITHUB_STEP_SUMMARY"
          for dir in artifacts/coverage-*; do
            name="${dir#artifacts/coverage-}"
            reportgenerator "-reports:$dir/**/coverage.cobertura.xml" "-targetdir:resumen-$name" "-reporttypes:JsonSummary" > /dev/null
            pct=$(jq -r '.summary.linecoverage' "resumen-$name/Summary.json")
            echo "| $name | ${pct}% |" >> "$GITHUB_STEP_SUMMARY"
          done
          reportgenerator "-reports:artifacts/coverage-*/**/coverage.cobertura.xml" "-targetdir:reporte-total" "-reporttypes:JsonSummary;Html" > /dev/null
          total=$(jq -r '.summary.linecoverage' reporte-total/Summary.json)
          {
            echo "| **Total backend** | **${total}%** |"
            echo ""
            echo "_Meta académica RNF-09: 90% (report-only, sin gate)._"
          } >> "$GITHUB_STEP_SUMMARY"
      - uses: actions/upload-artifact@v4
        with:
          name: reporte-cobertura-html
          path: reporte-total
```

- [ ] **Step 2: Validar sintaxis YAML localmente**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/ci.yml')); print('YAML OK')"`
Expected: `YAML OK`. (La validación real del workflow es la corrida en GitHub — Task 6.)

- [ ] **Step 3: Sanity de las rutas de soluciones**

Run: `for s in services/puntuaciones/Umbral.Puntuaciones.sln services/identity-service/Umbral.IdentityService.sln services/partidas/Umbral.Partidas.sln services/operaciones-sesion/Umbral.OperacionesSesion.sln gateway/Umbral.Gateway.sln; do [ -f "$s" ] && echo "OK $s" || echo "FALTA $s"; done`
Expected: 5 líneas `OK`.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: workflow GitHub Actions — 5 suites .NET en matrix + frontend + mobile + cobertura backend (RNF-11, RNF-09)"
```

---

### Task 2: Dockerfile de identity-service

**Files:**
- Create: `services/identity-service/Dockerfile`

**Interfaces:**
- Consumes: proyecto `src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj` (existente).
- Produces: imagen construible que el compose (`identity-service.build.context: ../services/identity-service`, ya escrito) necesita. Task 5 lo levanta.

- [ ] **Step 1: Escribir el Dockerfile** (espejo del de partidas)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj"
RUN dotnet publish "src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Umbral.IdentityService.Api.dll"]
```

- [ ] **Step 2: Verificar que la imagen construye**

Run: `docker build -t umbral-identity-test services/identity-service/ 2>&1 | tail -3`
Expected: `naming to docker.io/library/umbral-identity-test` sin errores. (Si Docker no está corriendo: `sudo systemctl start docker` o reportar BLOCKED.)

- [ ] **Step 3: Commit**

```bash
git add services/identity-service/Dockerfile
git commit -m "build(identity): Dockerfile multi-stage (faltante para el compose, RNF-10)"
```

---

### Task 3: Compose completo — init de bases + env faltante

**Files:**
- Create: `infra/postgres-init/01-create-databases.sql`
- Modify: `infra/docker-compose.yml`

**Interfaces:**
- Consumes: entradas ya existentes de `partidas`/`operaciones-sesion`/`puntuaciones`/`gateway`/`identity-service`; claves de config reales: `KEYCLOAK_BASE_URL/REALM/CLIENT_ID/VALID_AUDIENCES/VALID_ISSUERS` (los 3 servicios nuevos las leen por env — sus appsettings no tienen sección Keycloak), `IdentityApi__BaseUrl` y `PartidasApi__BaseUrl` (operaciones), `RabbitMq__Host` (operaciones y puntuaciones), `RabbitMqHistorial__Host` (puntuaciones, segunda cola).
- Produces: compose que la Task 5 levanta completo; `EF_MIGRATE_ON_STARTUP: "true"` en los 4 servicios (la Task 4 implementa el hook).

- [ ] **Step 1: Crear el script de init de bases**

`infra/postgres-init/01-create-databases.sql` (corre solo con volumen fresco):

```sql
CREATE DATABASE umbral_identity;
CREATE DATABASE umbral_partidas;
CREATE DATABASE umbral_operaciones_sesion;
CREATE DATABASE umbral_puntuaciones;
```

- [ ] **Step 2: Cambiar el mount de init en postgres**

En `infra/docker-compose.yml`, reemplazar exactamente:

```yaml
      - ./postgres/init:/docker-entrypoint-initdb.d
```

con:

```yaml
      # Init SQL versionado (el viejo ./postgres/init quedó vacío y root-owned).
      - ./postgres-init:/docker-entrypoint-initdb.d
```

- [ ] **Step 3: Completar la entrada `partidas`**

Reemplazar exactamente:

```yaml
  partidas:
    build:
      context: ../services/partidas
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PartidasDatabase: Host=postgres;Port=5432;Database=umbral_partidas;Username=umbral;Password=16102005
    depends_on:
      - postgres
```

con:

```yaml
  partidas:
    build:
      context: ../services/partidas
    ports:
      - "5010:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PartidasDatabase: Host=postgres;Port=5432;Database=umbral_partidas;Username=umbral;Password=16102005
      EF_MIGRATE_ON_STARTUP: "true"
      KEYCLOAK_BASE_URL: http://keycloak:8080
      KEYCLOAK_REALM: UMBRAL-UCAB
      KEYCLOAK_VALID_AUDIENCES: umbral-web,umbral-mobile
      KEYCLOAK_VALID_ISSUERS: http://localhost:8080/realms/UMBRAL-UCAB,http://keycloak:8080/realms/UMBRAL-UCAB
    depends_on:
      - postgres
      - keycloak
```

- [ ] **Step 4: Completar la entrada `operaciones-sesion`**

Reemplazar exactamente:

```yaml
  operaciones-sesion:
    build:
      context: ../services/operaciones-sesion
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__OperacionesSesionDatabase: Host=postgres;Port=5432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005
    depends_on:
      - postgres
```

con:

```yaml
  operaciones-sesion:
    build:
      context: ../services/operaciones-sesion
    ports:
      - "5020:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__OperacionesSesionDatabase: Host=postgres;Port=5432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005
      EF_MIGRATE_ON_STARTUP: "true"
      RabbitMq__Host: rabbitmq
      IdentityApi__BaseUrl: http://identity-service:8080
      PartidasApi__BaseUrl: http://partidas:8080
      KEYCLOAK_BASE_URL: http://keycloak:8080
      KEYCLOAK_REALM: UMBRAL-UCAB
      KEYCLOAK_VALID_AUDIENCES: umbral-web,umbral-mobile
      KEYCLOAK_VALID_ISSUERS: http://localhost:8080/realms/UMBRAL-UCAB,http://keycloak:8080/realms/UMBRAL-UCAB
    depends_on:
      - postgres
      - rabbitmq
      - keycloak
      - partidas
      - identity-service
```

- [ ] **Step 5: Completar la entrada `puntuaciones`**

Reemplazar exactamente:

```yaml
  puntuaciones:
    build:
      context: ../services/puntuaciones
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PuntuacionesDatabase: Host=postgres;Port=5432;Database=umbral_puntuaciones;Username=umbral;Password=16102005
    depends_on:
      - postgres
```

con:

```yaml
  puntuaciones:
    build:
      context: ../services/puntuaciones
    ports:
      - "5030:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PuntuacionesDatabase: Host=postgres;Port=5432;Database=umbral_puntuaciones;Username=umbral;Password=16102005
      EF_MIGRATE_ON_STARTUP: "true"
      RabbitMq__Host: rabbitmq
      RabbitMqHistorial__Host: rabbitmq
      KEYCLOAK_BASE_URL: http://keycloak:8080
      KEYCLOAK_REALM: UMBRAL-UCAB
      KEYCLOAK_VALID_AUDIENCES: umbral-web,umbral-mobile
      KEYCLOAK_VALID_ISSUERS: http://localhost:8080/realms/UMBRAL-UCAB,http://keycloak:8080/realms/UMBRAL-UCAB
    depends_on:
      - postgres
      - rabbitmq
      - keycloak
```

- [ ] **Step 6: Completar identity y gateway**

En la entrada `identity-service`, agregar dentro de su `environment:` (después de la línea `KEYCLOAK_VALID_AUDIENCES: ...`):

```yaml
      KEYCLOAK_VALID_ISSUERS: http://localhost:8080/realms/UMBRAL-UCAB,http://keycloak:8080/realms/UMBRAL-UCAB
      EF_MIGRATE_ON_STARTUP: "true"
```

En la entrada `gateway`, agregar dentro de su `environment:` (después de `KEYCLOAK_VALID_AUDIENCES: ...`):

```yaml
      KEYCLOAK_VALID_ISSUERS: http://localhost:8080/realms/UMBRAL-UCAB,http://keycloak:8080/realms/UMBRAL-UCAB
```

- [ ] **Step 7: Validar el compose**

Run: `docker compose -f infra/docker-compose.yml config > /dev/null && echo "COMPOSE OK"`
Expected: `COMPOSE OK`.

- [ ] **Step 8: Commit**

```bash
git add infra/postgres-init/01-create-databases.sql infra/docker-compose.yml
git commit -m "infra(compose): init de las 4 bases + env Keycloak/RabbitMQ/URLs inter-servicio + puertos (RNF-10)"
```

---

### Task 4: Migraciones EF al arranque, gated por `EF_MIGRATE_ON_STARTUP`

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Api/Program.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs`

**Interfaces:**
- Consumes: `IdentityDbContext`, `PartidasDbContext`, `OperacionesSesionDbContext`, `PuntuacionesDbContext` (registrados en DI de cada servicio; el namespace exacto es `<Servicio>.Infrastructure.Persistence` — verificarlo en los `using` existentes de cada Infrastructure antes de escribir).
- Produces: con `EF_MIGRATE_ON_STARTUP=true`, el servicio aplica `Database.Migrate()` al arrancar; con la variable ausente (default), comportamiento idéntico al actual. El compose de la Task 3 ya activa la variable.

- [ ] **Step 1: Insertar el hook en cada `Program.cs`**

En cada uno de los 4 archivos, localizar la línea `var app = builder.Build();` e insertar inmediatamente después (ajustando `XDbContext` al contexto del servicio):

```csharp
// RNF-10: el compose activa EF_MIGRATE_ON_STARTUP=true para aplicar el esquema
// contra una base fresca. Default off: dotnet run local y tests quedan idénticos.
if (Environment.GetEnvironmentVariable("EF_MIGRATE_ON_STARTUP") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<XDbContext>().Database.Migrate();
}
```

Sustituciones de `XDbContext` por archivo:
- identity → `IdentityDbContext`
- partidas → `PartidasDbContext`
- operaciones-sesion → `OperacionesSesionDbContext`
- puntuaciones → `PuntuacionesDbContext`

Agregar los `using` que falten en cada archivo (típicamente `using Microsoft.EntityFrameworkCore;` para `Migrate()` y el namespace `...Infrastructure.Persistence` del DbContext). Mantener el estilo del archivo.

- [ ] **Step 2: Compilar los 4 servicios**

Run: `dotnet build services/identity-service/Umbral.IdentityService.sln && dotnet build services/partidas/Umbral.Partidas.sln && dotnet build services/operaciones-sesion/Umbral.OperacionesSesion.sln && dotnet build services/puntuaciones/Umbral.Puntuaciones.sln`
Expected: 4 builds sin errores.

- [ ] **Step 3: Regresión — las 4 suites completas**

Run (una por una): `dotnet test "services/identity-service/Umbral.IdentityService.sln"` · `dotnet test "services/partidas/Umbral.Partidas.sln"` · `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"` · `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: identity 249, partidas 112, operaciones 456, puntuaciones 187 — todos verdes (el hook está off en tests).

- [ ] **Step 4: Commit**

```bash
git add services/identity-service/src/Umbral.IdentityService.Api/Program.cs services/partidas/src/Umbral.Partidas.Api/Program.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs
git commit -m "feat(infra): migraciones EF al arranque gated por EF_MIGRATE_ON_STARTUP (RNF-10, default off)"
```

---

### Task 5: Bring-up local del compose completo (gate RNF-10) + GUIA

**Ejecutada por el controller (no subagente): toca Docker y el entorno de la máquina.**

**Files:**
- Modify: `GUIA-LEVANTAMIENTO.md` (sección nueva: levantamiento completo por compose + `EF_MIGRATE_ON_STARTUP`)

- [ ] **Step 1: Volumen fresco de postgres** (los datos actuales de dev se pierden — es el punto del gate; keycloak conserva su volumen)

```bash
docker compose -f infra/docker-compose.yml down
docker volume rm infra_umbral-postgres-data
```

- [ ] **Step 2: Levantar TODO (sin legacy)**

```bash
docker compose -f infra/docker-compose.yml up -d --build postgres rabbitmq keycloak identity-service partidas operaciones-sesion puntuaciones gateway
```

Expected: 8 contenedores `Up` en `docker compose ps` (los build tardan varios minutos la primera vez).

- [ ] **Step 3: Verificar salud**

- Realm: `curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/realms/UMBRAL-UCAB` → `200`
- Directo: puertos 5001/5010/5020/5030 — endpoint `health` si existe, o cualquier endpoint conocido esperando `200`/`401` (401 = vivo con auth activa).
- Vía gateway (5080): `curl -s -o /dev/null -w "%{http_code}" http://localhost:5080/puntuaciones/health` (y análogos) → vivo.
- Logs sin crash-loop: `docker compose -f infra/docker-compose.yml logs --tail 20 identity-service partidas operaciones-sesion puntuaciones | grep -i -c 'fatal\|unhandled'` → `0`; confirmar en logs que las migraciones corrieron.

- [ ] **Step 4: Documentar en GUIA-LEVANTAMIENTO.md** — sección "Levantamiento completo con Docker Compose (RNF-10)": comando del Step 2, puertos (gateway 5080, identity 5001, partidas 5010, operaciones 5020, puntuaciones 5030), nota `EF_MIGRATE_ON_STARTUP`, nota volumen fresco vs `CREATE DATABASE` manual.

- [ ] **Step 5: Registrar el gate en el ledger y commitear la GUIA**

```bash
git add GUIA-LEVANTAMIENTO.md
git commit -m "docs(guia): levantamiento completo por Docker Compose (RNF-10)"
```

---

### Task 6: Corrida real del CI + línea base de cobertura + traceability

**Ejecutada por el controller. El push de la rama es decisión explícita del usuario (regla no-push).**

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (fila Bloque 6)

- [ ] **Step 1: Pedir autorización de push al usuario** (rama `feature/bloque6-ci` → `origin`). Sin push no hay validación real del workflow.
- [ ] **Step 2: Push + observar la corrida** — `git push -u origin feature/bloque6-ci` no dispara `on.push` (solo develop/master): abrir PR draft hacia develop o correr con `gh workflow run`/`gh pr create --draft`. Camino recomendado: `gh pr create --draft --base develop --title "Bloque 6: CI + cobertura + compose" --body "..."` → el `pull_request` trigger corre los 8 jobs. Observar: `gh pr checks --watch` o `gh run watch`.
- [ ] **Step 3: Registrar la línea base** — anotar en el ledger el % por servicio + total backend del summary (primera medición RNF-09 del proyecto).
- [ ] **Step 4: Fila Bloque 6 en la traceability-matrix** (formato de las filas existentes, columnas exactas): fuentes RNF-09/RNF-10/RNF-11; artefactos ci.yml, compose, Dockerfile identity, hook migraciones, GUIA; estado Implemented con los 8 jobs verdes + línea base.
- [ ] **Step 5: Commit docs**

```bash
git add docs/04-sdd/traceability-matrix.md
git commit -m "docs(bloque6): traceability — CI + cobertura + compose (RNF-09/10/11) con linea base"
```

---

## Notas para el executor

- Task 1-3 son transcripción (haiku); Task 4 requiere anclar en 4 `Program.cs` distintos (sonnet); Tasks 5-6 las corre el controller.
- Si `docker build` o `docker compose` fallan por daemon apagado → BLOCKED, no sudo silencioso.
- El gate real de RNF-11 es la corrida en GitHub (Task 6); todo lo anterior es preparación verificable localmente.
