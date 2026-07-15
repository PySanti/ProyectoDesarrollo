# Bloque 8 — Infra y levantamiento E2E — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `docker compose up` levanta la aplicación COMPLETA (frontend incluido) con correo real Gmail y teléfono físico por LAN — RNF-10 pleno (rubro 10%).

**Architecture:** El compose (Bloque 6) ya trae los 5 backend + postgres/rabbitmq/keycloak. Se añade: contenedor `frontend` (build Vite multi-stage → nginx con fallback SPA), interpolación desde el `.env` raíz (LAN_IP en issuers, SMTP_* y KEYCLOAK_CLIENT_SECRET a identity), y verificación de levantamiento desde cero (primer rebuild desde Bloque 6 — 7a-7f entra a docker por primera vez; se arregla lo que se rompa). Mobile NO se contenedoriza: Expo en el teléfono vía `mobile/run-local.sh` (mecanismo LAN ya existente por el `.env` raíz).

**Spec:** `docs/superpowers/specs/2026-07-13-bloque8-infra-levantamiento-design.md`. Desviación menor acordada por simplicidad: en vez de `infra/.env` nuevo, se reusa el **`.env` raíz existente** (ya es "fuente única de verdad" y ya tiene `LAN_IP`) con `--env-file .env`; misma sustancia, un archivo menos.

**Tech Stack:** Docker Compose · nginx:alpine · node:20-alpine · Vite 5 · .NET 8 imágenes existentes.

## Global Constraints

- Rama: `feature/bloque-7`. Commits con trailer `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- PROHIBIDO a implementadores: `git stash/reset/checkout/restore/clean`. Solo `git add <paths exactos>` + `git commit`.
- **NUNCA leer, imprimir ni commitear valores de `.env`** (contiene app password de Gmail y client secret). Verificaciones con `docker compose config` solo vía `grep` de NOMBRES de variables o de las líneas de issuers (sin secretos). `.env` está gitignored (`.gitignore:15-17`); solo `.env.example` se versiona.
- Comando canónico (desde la raíz del repo): `docker compose -f infra/docker-compose.yml --env-file .env up -d --build`. Sin `--env-file`, los defaults `${VAR:-...}` mantienen todo funcional en localhost (sin SMTP ni client secret).
- Sin dependencias nuevas de código; solo imágenes base estándar (node:20-alpine, nginx:alpine).
- Task 3 y Task 4 se ejecutan en la SESIÓN PRINCIPAL (son diagnóstico interactivo + verificación con el usuario, browser y teléfono) — no despachar a subagentes ciegos.

---

### Task 1: Contenedor frontend (Dockerfile multi-stage + nginx + compose)

**Files:**
- Create: `frontend/Dockerfile`
- Create: `frontend/nginx.conf`
- Create: `frontend/.dockerignore`
- Modify: `infra/docker-compose.yml` (añadir servicio `frontend`)

**Interfaces:**
- Produces: servicio compose `frontend` en host `:5173` (nginx `:80` interno), estáticos horneados con `VITE_GATEWAY_BASE_URL=http://localhost:5080` y `VITE_KEYCLOAK_URL=http://localhost:8080` (valores del NAVEGADOR en el host, no de la red docker).

- [ ] **Step 1: `frontend/.dockerignore`** — crítico: el `frontend/.env` local (generado con literales por run-local.sh) NO debe entrar a la imagen; Vite lo leería en el build y pisaría los ARGs.

```
node_modules
dist
.env
.env.*
*.tsbuildinfo
```

- [ ] **Step 2: `frontend/Dockerfile`** (mismo estilo 2-stage de los servicios .NET):

```dockerfile
FROM node:20-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG VITE_GATEWAY_BASE_URL=http://localhost:5080
ARG VITE_KEYCLOAK_URL=http://localhost:8080
ARG VITE_KEYCLOAK_REALM=UMBRAL-UCAB
ARG VITE_KEYCLOAK_CLIENT_ID=umbral-web
ENV VITE_GATEWAY_BASE_URL=$VITE_GATEWAY_BASE_URL \
    VITE_KEYCLOAK_URL=$VITE_KEYCLOAK_URL \
    VITE_KEYCLOAK_REALM=$VITE_KEYCLOAK_REALM \
    VITE_KEYCLOAK_CLIENT_ID=$VITE_KEYCLOAK_CLIENT_ID
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 3: `frontend/nginx.conf`** (SPA fallback):

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

- [ ] **Step 4: servicio en `infra/docker-compose.yml`** (después de `puntuaciones`, antes de `volumes:`):

```yaml
  frontend:
    build:
      context: ../frontend
    ports:
      - "5173:80"
    depends_on:
      - gateway
```

- [ ] **Step 5: Verificar build + arranque**

```bash
docker compose -f infra/docker-compose.yml build frontend
docker compose -f infra/docker-compose.yml up -d frontend
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5173/          # esperado: 200
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5173/partidas  # esperado: 200 (fallback SPA)
curl -s http://localhost:5173/ | grep -c '<div id="root">'               # esperado: 1
```

Si `npm run build` falla dentro del contenedor (tsc), diagnosticar con la salida del build — el gate local equivalente es `cd frontend && npx tsc -b && npm run build` (borrar artefactos `*.tsbuildinfo`, `vite.config.js/.d.ts`, `vitest.config.js/.d.ts` si se generan — nunca commitearlos).

- [ ] **Step 6: Commit**

```bash
git add frontend/Dockerfile frontend/nginx.conf frontend/.dockerignore infra/docker-compose.yml
git commit -m "feat(infra): contenedor frontend nginx en compose — RNF-10 pleno (8)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 2: Parametrización env — issuers LAN + SMTP Gmail + client secret

**Files:**
- Modify: `.env.example` (raíz)
- Modify: `infra/docker-compose.yml` (issuers en 5 servicios; SMTP + secret en identity)

**Interfaces:**
- Consumes: `.env` raíz gitignored (ya existe con `LAN_IP`); env vars SMTP que `DependencyInjection.cs:89-101` de identity ya lee.
- Produces: compose interpolable con `--env-file .env`; sin `.env`, defaults localhost funcionales.

- [ ] **Step 1: `.env.example`** — añadir al final:

```bash
# --- Correo real (Gmail app password) — identity lo usa via docker compose ---
# Genera una "contraseña de aplicación" en tu cuenta Google (requiere 2FA).
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USE_STARTTLS=true
SMTP_USERNAME=<tu-correo@gmail.com>
SMTP_PASSWORD=<app-password-16-chars>
SMTP_FROM_ADDRESS=<tu-correo@gmail.com>
SMTP_FROM_NAME=UMBRAL

# --- Client secret del cliente confidencial identity-service en Keycloak ---
KEYCLOAK_CLIENT_SECRET=<secret-del-cliente-identity-service>
```

- [ ] **Step 2: compose — issuers LAN.** En los 5 servicios (`identity-service`, `gateway`, `partidas`, `operaciones-sesion`, `puntuaciones`) reemplazar la línea `KEYCLOAK_VALID_ISSUERS` por (una sola línea):

```yaml
      KEYCLOAK_VALID_ISSUERS: http://localhost:8080/realms/UMBRAL-UCAB,http://keycloak:8080/realms/UMBRAL-UCAB,http://${LAN_IP:-localhost}:8080/realms/UMBRAL-UCAB
```

(Con `LAN_IP` sin definir queda una entrada localhost duplicada — inocuo.)

- [ ] **Step 3: compose — identity gana SMTP + secret** (en `environment:` de `identity-service`):

```yaml
      KEYCLOAK_CLIENT_SECRET: ${KEYCLOAK_CLIENT_SECRET:-}
      SMTP_HOST: ${SMTP_HOST:-}
      SMTP_PORT: ${SMTP_PORT:-587}
      SMTP_USE_STARTTLS: ${SMTP_USE_STARTTLS:-true}
      SMTP_USERNAME: ${SMTP_USERNAME:-}
      SMTP_PASSWORD: ${SMTP_PASSWORD:-}
      SMTP_FROM_ADDRESS: ${SMTP_FROM_ADDRESS:-}
      SMTP_FROM_NAME: ${SMTP_FROM_NAME:-UMBRAL}
```

- [ ] **Step 4: Verificar interpolación SIN imprimir secretos:**

```bash
docker compose -f infra/docker-compose.yml --env-file .env config | grep 'KEYCLOAK_VALID_ISSUERS'
# esperado: 5 líneas, cada una terminando en http://<LAN_IP-real>:8080/realms/UMBRAL-UCAB
docker compose -f infra/docker-compose.yml --env-file .env config | grep -o 'SMTP_[A-Z_]*' | sort -u
# esperado: los 7 nombres SMTP_* (solo nombres, sin valores)
```

- [ ] **Step 5: Paso manual del usuario (instrucción, el agente NO toca secretos):** copiar de `services/identity-service/.env` al `.env` raíz los valores reales de `SMTP_*` y `KEYCLOAK_CLIENT_SECRET`, y confirmar que `LAN_IP` es la IP Wi-Fi actual de la máquina (`ip addr` / `hostname -I`).

- [ ] **Step 6: Commit**

```bash
git add .env.example infra/docker-compose.yml
git commit -m "feat(infra): compose interpola LAN_IP en issuers y SMTP/client-secret a identity (8)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

### Task 3: Levantamiento completo desde cero + arreglar lo roto — SESIÓN PRINCIPAL

**Files:**
- Modify: lo que el arranque revele roto (desconocido a priori; primer rebuild desde Bloque 6).

- [ ] **Step 1: Confirmar con el usuario antes de borrar volúmenes** (pierde datos locales de Postgres/Keycloak runtime; el realm se re-siembra del import):

```bash
docker compose -f infra/docker-compose.yml down -v
```

- [ ] **Step 2: Levantar TODO desde cero:**

```bash
docker compose -f infra/docker-compose.yml --env-file .env up -d --build
docker compose -f infra/docker-compose.yml ps   # esperado: 9 contenedores Up (postgres/rabbitmq healthy)
```

- [ ] **Step 3: Smoke por contenedor (curls, sin browser):**

```bash
curl -s http://localhost:5080/health                                   # gateway: {"status":"healthy",...}
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8080/realms/UMBRAL-UCAB   # 200 (realm importado)
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5173/        # 200 (frontend)
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:15672/       # 200 (rabbitmq mgmt)
for s in identity-service partidas operaciones-sesion puntuaciones gateway; do
  echo "--- $s ---"; docker compose -f infra/docker-compose.yml logs --tail 5 "$s" | grep -i 'error\|fail\|exception' || echo OK
done
```

- [ ] **Step 4: Arreglar lo que falle** con superpowers:systematic-debugging (leer logs del servicio caído, root cause, fix mínimo). Un commit por fix con mensaje `fix(infra): <qué> (8)` + trailer. Repetir Steps 2-3 hasta 9/9 sanos.

- [ ] **Step 5: Ledger** — append en `.git/sdd/progress.md`: hashes de fixes + resultado del smoke.

### Task 4: Mobile por LAN + smoke E2E asistido — SESIÓN PRINCIPAL (con el usuario)

**Files:**
- Modify: solo si el smoke revela roturas (mobile/.env es generado, no se commitea).

- [ ] **Step 1: Regenerar `mobile/.env` desde el `.env` raíz** (script existente resuelve LAN_IP a literales — NO editar mobile/.env a mano):

```bash
cd mobile && ./run-local.sh   # levanta expo start --clear --host lan
```

Teléfono en la MISMA red Wi-Fi, escanear QR de Metro. Si no conecta: probar `npx expo start --tunnel`; firewall: `sudo ufw allow 8080/tcp && sudo ufw allow 5080/tcp && sudo ufw allow 8081/tcp`.

- [ ] **Step 2: Checklist E2E con el usuario** (browser en `http://localhost:5173` + teléfono):

1. Login admin (creds seed del realm — `infra/keycloak/README.md`).
2. Crear usuario participante con correo `<tu-gmail>+p1@gmail.com` → **correo real llega a tu bandeja Gmail** con la credencial temporal (RNF-23/7f).
3. Primer login del participante fuerza cambio de contraseña (Keycloak).
4. Operador crea partida (Trivia mínima) → publica → aparece en Lobby.
5. Teléfono: login participante → panel Partidas → ve la partida publicada → se inscribe (o solicita, HU-19).
6. Web: operador ve/acepta la solicitud → inicia la partida → teléfono recibe `PartidaIniciada` (SignalR vía gateway).

- [ ] **Step 3: Anotar en `.git/sdd/progress.md`** cada paso PASS/FAIL. Los FAIL se arreglan como en Task 3 Step 4 (o se difieren con nota si son de refinamiento, no de levantamiento).

### Task 5: GUIA-LEVANTAMIENTO reescrita + cierre

**Files:**
- Modify: `GUIA-LEVANTAMIENTO.md`

- [ ] **Step 1: Reescribir** con esta estructura (compose = flujo canónico ÚNICO; `run-local.*` quedan como apéndice "modo desarrollo opcional"):

1. **Requisitos** (Docker + teléfono con Expo Go / build dev).
2. **Setup una vez:** copiar `.env.example` → `.env`, rellenar `LAN_IP`, `SMTP_*`, `KEYCLOAK_CLIENT_SECRET`.
3. **Levantar todo:** `docker compose -f infra/docker-compose.yml --env-file .env up -d --build` + tabla de puertos (5173 web · 5080 gateway · 8080 keycloak · 15672 rabbit · 55432 postgres · 5001/5010/5020/5030 servicios directos solo debug).
4. **Mobile:** `cd mobile && ./run-local.sh` (misma Wi-Fi).
5. **Smoke E2E:** el checklist del Task 4 como guía de demo.
6. **Troubleshooting:** issuers LAN, firewall ufw, `--tunnel`, base sucia (`down -v` re-siembra), correo no llega (revisar SMTP_* y spam).

- [ ] **Step 2: Commit + ledger "8 DONE":**

```bash
git add GUIA-LEVANTAMIENTO.md
git commit -m "docs(infra): GUIA-LEVANTAMIENTO — compose como flujo canonico unico (8)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Append en `.git/sdd/progress.md`: `=== BLOQUE 8 CERRADO ===` + hashes + resultado smoke E2E.
