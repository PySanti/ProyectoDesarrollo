# Guía de levantamiento de UMBRAL

Flujo canónico (RNF-10): **toda la aplicación se levanta con un solo comando de Docker Compose** —
frontend web, gateway, los 4 microservicios, PostgreSQL, RabbitMQ y Keycloak. La app móvil corre
en el teléfono con Expo (no se contenedoriza) y se conecta por la Wi-Fi de la casa.

## 1. Requisitos

- Docker + Docker Compose (plugin v2).
- Para el móvil: teléfono con **Expo Go** en la misma red Wi-Fi que la máquina, y Node ≥ 20.19.4
  en la máquina (para el bundler Metro).
- Cuenta Gmail con verificación en 2 pasos y una **contraseña de aplicación**
  (https://myaccount.google.com/apppasswords) para el correo real de credenciales (RNF-23).

## 2. Setup una sola vez: el `.env` raíz

El `.env` de la raíz del repo es la fuente única de configuración local (está gitignored; la
plantilla versionada es `.env.example`):

```bash
cp .env.example .env
```

Edita y rellena:

| Variable | Valor |
|---|---|
| `LAN_IP` | IP de tu máquina en la Wi-Fi (`hostname -I` en Linux, `ipconfig` en Windows). Solo importa para el teléfono físico. |
| `SMTP_USERNAME` | Tu Gmail **completo** (`usuario@gmail.com`). |
| `SMTP_PASSWORD` | La contraseña de aplicación de 16 caracteres, **sin espacios y sin comillas** (Google la muestra como `xxxx xxxx xxxx xxxx`; escríbela pegada). |
| `SMTP_FROM_ADDRESS` | Normalmente el mismo Gmail. |
| `KEYCLOAK_CLIENT_SECRET` | Secret del cliente confidencial `identity-service` en Keycloak (realm `UMBRAL-UCAB` → Clients → identity-service → Credentials). |

El resto de valores de la plantilla (puertos, realm, credenciales de BD dev) funcionan tal cual.

## 3. Levantar todo (un comando)

Desde la raíz del repo:

```bash
docker compose -f infra/docker-compose.yml --env-file .env up -d --build
```

> El `--env-file .env` es importante: sin él, el compose usa defaults localhost (funciona en la
> máquina, pero sin correo, sin client secret y sin acceso desde el teléfono).

Primera vez: el build tarda unos minutos. Arranques siguientes: `up -d` sin `--build` levanta
todo en ~30 segundos con los datos persistidos.

Verificar:

```bash
docker compose -f infra/docker-compose.yml ps            # 9 contenedores Up
curl -s http://localhost:5080/health                     # {"status":"healthy","service":"gateway"}
```

### Puertos host

| Puerto | Qué |
|---|---|
| **5173** | Frontend web (admin/operador) — `http://localhost:5173` |
| **5080** | Gateway YARP (única entrada al backend; todo exige JWT) |
| **8080** | Keycloak (realm `UMBRAL-UCAB` se auto-importa con usuarios seed — ver `infra/keycloak/README.md`) |
| 15672 | RabbitMQ Management (guest/guest) |
| 55432 | PostgreSQL (umbral / ver `.env`) |
| 5001 / 5010 / 5020 / 5030 | identity / partidas / operaciones-sesion / puntuaciones directos (solo debug; los clientes SIEMPRE pasan por el gateway) |

### Qué queda resuelto automáticamente

- **Bases de datos:** con volumen fresco, `infra/postgres-init/01-create-databases.sql` crea las
  4 bases; cada servicio aplica su esquema al arrancar (migraciones EF con
  `EF_MIGRATE_ON_STARTUP=true`; identity con su bootstrap idempotente).
- **Realm Keycloak:** `--import-realm` siembra `UMBRAL-UCAB` (roles, clientes, usuarios de prueba,
  tema de login) al primer arranque del volumen.
- **RabbitMQ:** exchanges/colas durables se declaran solos; quedan 4 colas con consumidor:
  `identity.correo-credenciales` (correo de credenciales, RNF-23),
  `identity.operaciones-sesion.participaciones` (guard BR-E10),
  `puntuaciones.operaciones-sesion.proyecciones` y `...historial` (scoring/auditoría).
- **Issuers:** los tokens del navegador (`localhost`), de la red interna (`keycloak`) y del
  teléfono (`LAN_IP`) son todos válidos — `KEYCLOAK_VALID_ISSUERS` lista los tres.
- **Correo:** al crear un usuario, identity publica `CredencialTemporalEmitida` a RabbitMQ y un
  consumidor envía el correo real por Gmail **sin bloquear el request** (best-effort ADR-0012: si
  el SMTP falla, el usuario queda creado igual y el error se loguea).

## 4. Móvil (Expo, fuera de compose)

Con el stack de compose arriba y `LAN_IP` correcta en el `.env` raíz:

```bash
cd mobile && ./run-local.sh
```

El script regenera `mobile/.env` con valores literales desde el `.env` raíz (no edites
`mobile/.env` a mano) y lanza `expo start --host lan`. Escanea el QR con Expo Go desde el
teléfono (misma Wi-Fi).

## 5. Smoke E2E (guía de demo)

1. **Login admin** en `http://localhost:5173` (credenciales seed: `infra/keycloak/README.md`).
2. **Crear usuario participante** con correo `tucuenta+p1@gmail.com` → el correo real con la
   contraseña temporal llega a tu bandeja (los alias `+algo` llegan todos a tu Gmail y para el
   sistema son usuarios distintos).
3. **Primer login del participante** → Keycloak fuerza el cambio de contraseña.
4. Como **operador**: crear partida (Trivia mínima) → publicar → queda en Lobby.
5. **Teléfono**: login participante → panel Partidas → ver la partida publicada → inscribirse.
6. **Web**: aceptar la solicitud → iniciar la partida → el teléfono recibe el arranque en vivo
   (SignalR vía gateway).

## 6. Troubleshooting

| Síntoma | Causa/fix |
|---|---|
| El teléfono no abre Keycloak o da "Sesión expirada o no autorizada" | `LAN_IP` del `.env` no es la IP real de la Wi-Fi. Corrígela y recrea: `docker compose -f infra/docker-compose.yml --env-file .env up -d` + relanza `mobile/run-local.sh`. |
| El teléfono no alcanza la máquina | Firewall: `sudo ufw allow 8080/tcp && sudo ufw allow 5080/tcp && sudo ufw allow 8081/tcp`. Si la Wi-Fi aísla clientes, `cd mobile && npx expo start --tunnel`. |
| No llega el correo de credenciales | `docker compose -f infra/docker-compose.yml logs identity-service \| grep -i smtp`. `5.7.0 Authentication Required` = credenciales mal: `SMTP_USERNAME` debe ser el Gmail completo y `SMTP_PASSWORD` la app password de 16 caracteres sin espacios ni comillas. Tras corregir el `.env`: `up -d identity-service`. Revisa también spam. |
| Crear usuarios falla contra Keycloak | Falta `KEYCLOAK_CLIENT_SECRET` en el `.env` (o el compose se levantó sin `--env-file`). |
| Base/realm en estado raro | Desde cero: `docker compose -f infra/docker-compose.yml down -v` (borra datos; el realm se re-siembra) y volver al paso 3. |
| Cambié código y no se refleja | Los contenedores corren imágenes: `up -d --build` para reconstruir. |
| RabbitMQ sin colas o sin consumidores | Espera ~30 s tras el arranque (reintento de conexión). Ver colas: `docker exec umbral-rabbitmq rabbitmqctl list_queues name consumers`. |

## Apéndice: modo desarrollo (opcional, sin contenedores de apps)

Para iterar código con recarga rápida se conservan los `run-local.*` por componente (cargan el
`.env` raíz y el propio): infra mínima con
`docker compose -f infra/docker-compose.yml up -d postgres rabbitmq keycloak` y luego
`./gateway/run-local.sh`, `./services/<servicio>/run-local.sh`, `./frontend/run-local.sh`,
`./mobile/run-local.sh` en terminales separadas. Los `.env` por servicio
(`services/<servicio>/.env`, ver cada `.env.example`) configuran Keycloak/SMTP/RabbitMQ en este
modo — secciones `RabbitMq__*` (publisher), `RabbitMqConsumer__*` y
`RabbitMqCredencialesConsumer__*` (consumidores de identity) con host `localhost`. En modo
compose todo esto ya viene configurado.

Smoke test opt-in del broker con el round-trip real:

```bash
RABBITMQ_TEST_HOST=localhost dotnet test services/identity-service/tests/Umbral.IdentityService.IntegrationTests --filter RabbitMqRoundTripTests
```
