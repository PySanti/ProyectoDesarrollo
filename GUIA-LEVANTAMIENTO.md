## Levantamiento de infraestructura

## Estado de migracion documental

La doctrina documental cambio antes de la migracion completa del codigo. Lee `docs/02-project-context/documentation-migration-status.md` antes de interpretar esta guia, los contextos de clientes o las carpetas bajo `services/`.

Doctrina actual: los servicios objetivo son `Identity`, `Partidas`, `Operaciones de Sesion` y `Puntuaciones`, detras del gateway YARP obligatorio. Las carpetas de implementacion con nombres anteriores pueden seguir existiendo como deuda de migracion; no son prueba de limites de servicio activos.

Desde la raiz del repo:

```powershell
docker compose -f "infra/docker-compose.yml" up -d postgres rabbitmq keycloak
```

Verificar contenedores:

```powershell
docker compose -f "infra/docker-compose.yml" ps
```

Abrir RabbitMQ Management:

```txt
http://localhost:15672
```

Credenciales por defecto de la imagen RabbitMQ si no se configuran otras:

```txt
usuario: guest
password: guest
```

## Variables de entorno (.env central)

Hay un **`.env` en la raíz del repo** que es la **fuente única de verdad** para los valores
compartidos entre todos los componentes: IP de LAN, puertos, realm de Keycloak y credenciales de
base de datos. Cópialo desde la plantilla y ajústalo:

```bash
cp .env.example .env
```

Los `.env` de cada microservicio y cliente **referencian** esos valores con `${VAR}` (con un
valor por defecto `${VAR:-default}` como respaldo). Los scripts `run-local.*` cargan primero el
`.env` raíz y luego el `.env` propio del componente, así que **solo editas la IP en un lugar**.

> ⚠️ **Al usar el móvil en un teléfono físico**: cambia únicamente `LAN_IP` en `<repo>/.env` por la
> IP de tu máquina en la Wi-Fi (`hostname -I` en Linux, `ipconfig` en Windows). Esa IP se propaga
> automáticamente a `mobile/.env` y al `KEYCLOAK_VALID_ISSUERS` de los servicios. Si la IP no
> coincide, Keycloak emite tokens con un `iss` que los servicios rechazan → error
> "Sesión expirada o no autorizada".

Para que el móvil y el frontend tomen el `.env` raíz, láncalos con su `run-local.sh`
(`mobile/run-local.sh`, `frontend/run-local.sh`); si los corres con `npm start` / `npm run dev`
directos, usan los valores por defecto (localhost).

## Levantamiento de microservicios


* En la raiz del proyecto


### Linux

```cmd

# Terminal 1
./services/identity-service/run-local.sh


# Terminal 2
./services/team-service/run-local.sh

# Terminal 3
./services/bdt-game-service/run-local.sh


# Terminal 4
./services/trivia-game-service/run-local.sh

# Terminal 5  (carga el .env raiz antes de Vite)
./frontend/run-local.sh

# Terminal 6  (carga el .env raiz antes de Expo)
./mobile/run-local.sh
```

> Nota: `identity-service` necesita SMTP configurado para enviar el correo de bienvenida con la
> contraseña temporal al crear usuarios. Ver la sección "Correo de bienvenida (SMTP)" más abajo.

### Powershell

```cmd

# Terminal 1
cd ./services/identity-service/
./run-local.ps1


# Terminal 2
cd ./services/team-service/
./run-local.ps1

# Terminal 3
cd ./services/bdt-game-service/
./run-local.ps1


# Terminal 4
cd ./services/trivia-game-service/
./run-local.ps1

# Terminal 5  (carga el .env raiz antes de Vite)
cd ./frontend/
./run-local.ps1

# Terminal 6  (carga el .env raiz antes de Expo)
cd ./mobile/
./run-local.ps1
```

## Correo de bienvenida (SMTP)

Al crear un usuario (cualquier rol), `identity-service` le envía un correo con su **contraseña
temporal** y los estilos de la plataforma. El envío es síncrono: si el correo no se puede entregar,
la creación falla con `502` y no queda usuario creado (se compensa Keycloak + BD).

Configura estas variables en `services/identity-service/.env` (ver `.env.example`). Ejemplo con
Gmail + app password (requiere 2FA; crea el app password en
https://myaccount.google.com/apppasswords):

```
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USE_STARTTLS=true
SMTP_USERNAME=tu-cuenta@gmail.com
SMTP_PASSWORD=tu-app-password-de-16-caracteres
SMTP_FROM_ADDRESS=tu-cuenta@gmail.com
SMTP_FROM_NAME=UMBRAL
```

## Event Broker (RabbitMQ)

`operaciones-sesion` y `puntuaciones` publican eventos a RabbitMQ para materializar auditoría y 
scoring sin bloquear el flujo principal (best-effort, ADR-0012). Configura estas variables en 
`.env` de ambos servicios (`services/operaciones-sesion/.env` y `services/puntuaciones/.env`):

```
RabbitMq__Enabled=true
RabbitMq__Host=localhost
RabbitMq__Port=5672
RabbitMq__User=guest
RabbitMq__Password=guest
```

Si `RabbitMq__Enabled=false` (default), el broker no se usa y los eventos no se publican (útil 
para tests). Los valores por defecto (usuario y contraseña `guest`, puerto `5672`) funcionan 
con la imagen de RabbitMQ del `docker-compose.yml` de desarrollo.

### Verificación manual (smoke test)

Una vez levantadas ambas imágenes y los dos servicios:

1. Abre RabbitMQ Management: `http://localhost:15672` (guest/guest)
2. Navega a **Queues and Streams** y busca `puntuaciones.operaciones-sesion.all` (debe estar visible)
3. Navega a **Exchanges** y busca `umbral.operaciones-sesion` (type: topic, durable)
4. Opera una partida con ambos servicios corriendo (publícala, inicia, responde preguntas o valida QRs)
5. La cola debe mostrar mensajes entrando: click en el nombre de la cola para ver los detalles

## Broker RabbitMQ (Identity)

Desde SP-5b, `identity-service` también publica eventos (equipos + gobernanza de rol/permisos) a
RabbitMQ, best-effort (ADR-0012) — mismo patrón que `operaciones-sesion`/`puntuaciones` de la
sección anterior. Configura estas variables en `services/identity-service/.env`:

```
RabbitMq__Enabled=true
RabbitMq__Host=localhost
RabbitMq__Port=5672
RabbitMq__User=guest
RabbitMq__Password=guest
RabbitMq__Exchange=umbral.identity
```

Si `RabbitMq__Enabled=false` (default), el servicio arranca igual y los eventos simplemente no se
publican (best-effort: la ausencia de broker nunca rompe el flujo HTTP). Los valores por defecto
(usuario y contraseña `guest`, puerto `5672`) funcionan con la imagen de RabbitMQ del
`docker-compose.yml` de desarrollo, igual que en Operaciones/Puntuaciones.

Smoke test opt-in con el broker vivo:

```
RABBITMQ_TEST_HOST=localhost dotnet test services/identity-service/tests/Umbral.IdentityService.IntegrationTests --filter RabbitMqRoundTripTests
```

Sin `RABBITMQ_TEST_HOST` el test retorna vacío (no falla) — mismo patrón opt-in que SP-3i.

## Autenticación JWT (Partidas)

Desde SP-5a, Partidas valida JWT y exige el permiso `GestionarPartidas` en mutaciones; sin estas
vars el servicio arranca sin validación JWT real (solo apto para tests). Configura estas variables
en `services/partidas/.env`:

```
KEYCLOAK_BASE_URL=http://localhost:8080
KEYCLOAK_REALM=UMBRAL-UCAB
KEYCLOAK_VALID_AUDIENCES=umbral-web,umbral-mobile,account
KEYCLOAK_VALID_ISSUERS=http://localhost:8080/realms/UMBRAL-UCAB
```
