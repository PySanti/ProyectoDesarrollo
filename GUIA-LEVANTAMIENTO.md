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

## Levantamiento completo con Docker Compose (RNF-10)

Alternativa al `dotnet run` por servicio: levantar la solución entera (infra + 4 servicios + gateway) con un solo comando desde la raíz del repo:

```powershell
docker compose -f "infra/docker-compose.yml" up -d --build postgres rabbitmq keycloak identity-service partidas operaciones-sesion puntuaciones gateway
```

Puertos host: gateway **5080**, identity **5001**, partidas **5010**, operaciones-sesion **5020**, puntuaciones **5030** (health anónimo en `/health` de cada servicio; vía gateway todo exige JWT por diseño SP-5a). Keycloak 8080, Postgres 55432, RabbitMQ 5672/15672.

Notas:

- **Base fresca:** con el volumen de Postgres recién creado, `infra/postgres-init/01-create-databases.sql` crea las 4 bases (`umbral_identity`, `umbral_partidas`, `umbral_operaciones_sesion`, `umbral_puntuaciones`) y cada servicio aplica su esquema al arrancar — partidas/operaciones/puntuaciones vía migraciones EF (`EF_MIGRATE_ON_STARTUP=true`, solo activo en compose; `dotnet run` local no migra), identity vía su bootstrap propio (`EnsureCreated` + SQL idempotente, incondicional). Para un volumen ya existente sigue valiendo el `CREATE DATABASE` manual del CLAUDE.md.
- **Orden de arranque:** postgres y rabbitmq tienen healthcheck; los servicios esperan `service_healthy` — no hay carrera contra la DB en el primer arranque.
- **Dentro de la red Docker** los servicios hablan con Keycloak como `http://keycloak:8080`; los tokens emitidos desde el navegador llevan issuer `http://localhost:8080`, por eso `KEYCLOAK_VALID_ISSUERS` lista ambos.
- Los servicios legacy (`trivia-game-service`, `bdt-game-service`) fueron retirados del repositorio (Bloque 3 de cobertura, 2026-07-11). Si tu volumen local de Postgres aún tiene las bases `umbral_trivia_game`/`umbral_bdt_game`, son residuo inocuo; opcional: `docker exec -it umbral-postgres psql -U umbral -d umbral -c "DROP DATABASE umbral_trivia_game;"` (ídem `umbral_bdt_game`).

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

> ⚠️ **Desde el Bloque 2a, web y mobile consumen identity/equipos A TRAVÉS del gateway** (`GATEWAY_PORT`, default 5080). El gateway es parte obligatoria del levantamiento local: `./gateway/run-local.sh` antes de abrir los clientes.

> **Desde el Bloque 2d, el mobile ya no usa los servicios trivia/bdt viejos** (:5015/:5016 — código y vars `EXPO_PUBLIC_BDT/TRIVIA_API_BASE_URL` retirados). El participante descubre partidas con `GET /operaciones-sesion/partidas-publicadas` (solo sesiones en Lobby, participant-safe), se inscribe/preinscribe y responde convocatorias desde el panel "Partidas" y el inbox "Convocatorias"; el lobby recibe `PartidaIniciada`/`PartidaCancelada` por SignalR (`@microsoft/signalr@^8` en mobile, mismo hub vía gateway). Levantamiento del flujo mobile: infra + `identity-service` + `partidas` + `operaciones-sesion` + gateway. El gameplay mobile (responder, QR, pistas, geoloc) llega en 2e.

> **Desde el Bloque 2e-1, el participante mobile juega Trivia en vivo** (`PartidaLive`: pregunta activa con countdown, responder una vez, ranking del juego y consolidado al finalizar). El flujo mobile de gameplay requiere además **`puntuaciones`** (rankings por `GET /puntuaciones/**` con token de participante): incluir `./services/puntuaciones/run-local.sh` en el levantamiento. BDT mobile (QR, pistas, geoloc) llega en 2e-2.

> **Desde el Bloque 2e-2 el participante mobile juega BDT completo** (subir QR por cámara/galería con reintentos ilimitados, pistas del operador en vivo, geolocalización enviada al operador ~cada 2s mientras el juego BDT está activo — requiere permiso de ubicación en el dispositivo). **Bloque 2e completo**: el gameplay mobile (Trivia + BDT) queda operativo end-to-end con el mismo levantamiento del punto anterior.

> **Desde el Bloque 2b, la creación/configuración de partidas multi-juego vive en la web** (`/partidas` contra `services/partidas`): incluir `./services/partidas/run-local.sh` en el levantamiento del flujo de configuración.

* En la raiz del proyecto


### Linux

```cmd

# Terminal 0 (OBLIGATORIA: entrada única del backend para los clientes)
./gateway/run-local.sh

# Terminal 1
./services/identity-service/run-local.sh

# Terminal 5  (carga el .env raiz antes de Vite)
./frontend/run-local.sh

# Terminal 6  (carga el .env raiz antes de Expo)
./mobile/run-local.sh
```

> Nota: `identity-service` necesita SMTP configurado para enviar el correo de bienvenida con la
> contraseña temporal al crear usuarios. Ver la sección "Correo de bienvenida (SMTP)" más abajo.

### Powershell

```cmd

# Terminal 0 (OBLIGATORIA: entrada única del backend para los clientes)
cd ./gateway/
./run-local.ps1

# Terminal 1
cd ./services/identity-service/
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

### Consumidor de inscripciones de equipo (Bloque 4A — guard BR-E10)

Desde el Bloque 4A, `identity-service` también **consume** eventos de Operaciones de Sesión para
mantener la proyección `participaciones_activas_equipo` que respalda el guard de borrado de equipo
(BR-E10: no se elimina un equipo con participación activa en partida `Lobby`/`Iniciada`). Es el
primer consumidor de Identity (`OperacionesInscripcionesConsumer`, best-effort ack-siempre,
ADR-0012). Se configura en su propia sección `RabbitMqConsumer` en `services/identity-service/.env`:

```
RabbitMqConsumer__Enabled=true
RabbitMqConsumer__Host=localhost
RabbitMqConsumer__Port=5672
RabbitMqConsumer__User=guest
RabbitMqConsumer__Password=guest
RabbitMqConsumer__Exchange=umbral.operaciones-sesion
RabbitMqConsumer__Queue=identity.operaciones-sesion.participaciones
```

Notas:
- El **exchange es el de Operaciones** (`umbral.operaciones-sesion`), no el de Identity — el
  consumidor se ata a los eventos que **produce Operaciones**. La cola
  `identity.operaciones-sesion.participaciones` (durable) se enlaza a 4 routing keys:
  `operaciones-sesion.inscripcion-equipo-creada.v1`, `…inscripcion-equipo-cancelada.v1`,
  `…partida-finalizada.v1`, `…partida-cancelada.v1`.
- Si `RabbitMqConsumer__Enabled=false` (o el host vacío), el consumidor **no arranca** y el guard
  BR-E10 queda inerte (el borrado no se bloqueará por participación activa). Para probar el guard
  end-to-end en dev, habilítalo junto con `RabbitMq__Enabled=true` en Operaciones para que los
  eventos de inscripción realmente se publiquen.
- La proyección es **eventualmente consistente** (caveat aceptado): una inscripción hecha instantes
  antes de un borrado puede no estar proyectada aún. Al desplegar, la tabla arranca vacía
  (cold start): las inscripciones de equipo previas al arranque del consumidor no estarán
  proyectadas hasta re-emitirse.

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

## Consola de sesión del operador (web, Bloque 2c-1)

La consola (`/partidas/:id/sesion`: publicar → lobby → inicio manual/automático) necesita corriendo:
**gateway** (5080) + **partidas** (5010, config handoff interno) + **operaciones-sesion** (5020) +
infra (Postgres/Keycloak; RabbitMQ opcional para 2c-1). `services/operaciones-sesion/.env` requiere
las mismas `KEYCLOAK_*` que Partidas (sección anterior). El tiempo real usa SignalR vía gateway
(`/operaciones-sesion/hubs/sesion`, WebSocket passthrough); el conteo de inscritos del lobby se
refresca por polling (5s) porque el hub no pushea inscripciones.

El **runtime Trivia** (Bloque 2c-2: pregunta activa, avance, finalizar juego, ranking del juego en vivo)
añade dos requisitos al levantar: **puntuaciones** (5030) y **RabbitMQ** arriba — el ranking (`GET
/puntuaciones/partidas/{id}/juegos/{juegoId}/ranking`) lo sirve una proyección de Puntuaciones
alimentada por eventos RabbitMQ (best-effort, ADR-0012), y la consola la refetchea al recibir
`PreguntaActivada`/`PreguntaCerrada` del hub de sesión (sin segundo hub). `services/puntuaciones/.env`
requiere las mismas `KEYCLOAK_*` y las `RabbitMq__*` (ver §Event Broker).

El **runtime BDT** (Bloque 2c-3: etapas, avance, pistas, mapa de geolocalización, ranking) usa el
mismo stack de servicios que 2c-2 (gateway + operaciones + partidas + **puntuaciones** + RabbitMQ).
Añade en el cliente el **mapa de leaflet**: `GeoMapPanel` carga tiles de OpenStreetMap desde un
servidor externo (`https://tile.openstreetmap.org/...`), por lo que la máquina que abre la web
necesita **internet** para ver el fondo del mapa (los marcadores de participantes funcionan sin
internet, pero sin fondo geográfico). La geolocalización llega por SignalR (`UbicacionActualizada`,
solo al operador vía el grupo `operador:partida:{id}`); las pistas se envían por `POST
/operaciones-sesion/partidas/{id}/pistas` a un participante o equipo del roster.

El **cierre de la partida** (Bloque 2c-4) muestra el **ranking consolidado** en la vista terminada
de la consola: `GET /puntuaciones/partidas/{id}/ranking-consolidado` (mismo stack que 2c-2/2c-3;
requiere la partida `Terminada` — antes responde 409, y la consola reintenta hasta 3 veces con
1.5s para cubrir el lag de proyección). Las páginas legacy `trivia/operar` y `bdt/partidas`
fueron retiradas: toda la operación en vivo pasa por `/partidas/:id/sesion`.

## Consultas web de Puntuaciones (Bloque 2f)

La web consulta el historial cronológico de una partida en `/partidas/:id/historial` y el
rendimiento histórico de un equipo en `/puntuaciones/equipos`. Ambas vistas consumen Puntuaciones
exclusivamente a través del gateway: `GET /puntuaciones/partidas/{id}/historial` y
`GET /puntuaciones/equipos/{equipoId}/rendimiento`. Requieren el mismo stack completo de 2c-2/2c-3;
el historial exige rol `Operador` o `Administrador` y devuelve `403` a `Participante`.

Con Bloque 2f queda **BLOQUE 2 COMPLETO**: configuración y operación web, participación y gameplay
mobile, y consultas web de historial/rendimiento funcionan sobre los cuatro servicios detrás del
gateway obligatorio.

> **Desde el Bloque 3a los rankings se actualizan en vivo por push** (hub `puntuaciones/hubs/ranking`, SP-4c): la consola web del operador y el live mobile del participante reciben `RankingTriviaActualizado`/`RankingBDTActualizado` intra-pregunta/etapa y `RankingConsolidadoCalculado` al finalizar. El push es aditivo: los GET HTTP siguen siendo la fuente recuperable (un push perdido no se reintenta). Mismo levantamiento; el hub viaja por el gateway como el de sesión.
