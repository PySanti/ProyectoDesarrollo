# Guia de levantamiento - UMBRAL

Esta guia describe una ruta local para levantar UMBRAL con infraestructura en Docker y aplicaciones en modo desarrollo.

## Requisitos previos

- Docker Desktop.
- .NET SDK 8.
- Node.js 20 o superior.
- npm.
- Expo CLI mediante `npx expo`.
- Un telefono con Expo Go o un emulador Android/iOS para la app movil.

## Topologia local

Servicios aprobados del backend:

- Identity Service.
- Team Service.
- Trivia Game Service.
- BDT Game Service.

Clientes:

- React web para Administrador y Operador.
- React Native mobile para Participante.

Infraestructura:

- PostgreSQL.
- RabbitMQ.
- Keycloak.

No levantar ni crear servicios llamados Audit Service, Scoring Service, Trivia Service, Treasure Hunt Service o Notification Service.

## Puertos recomendados

| Componente | URL local recomendada |
|---|---|
| Keycloak | `http://localhost:8080` |
| RabbitMQ Management | `http://localhost:15672` |
| PostgreSQL | `localhost:55432` |
| Identity Service | `http://localhost:5000` |
| Team Service | `http://localhost:5099` |
| Trivia Game Service | `http://localhost:5015` |
| BDT Game Service | `http://localhost:5016` |
| React web | `http://localhost:5173` |
| Expo mobile | URL que muestre Expo al ejecutar `npm run start` |

## Levantamiento de infraestructura

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

## Preparacion de PostgreSQL

El compose crea el servidor PostgreSQL y una base inicial. Para trabajar con servicios separados, crear las bases por microservicio:

```powershell
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_identity;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_team;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_trivia_game;"
docker exec -it umbral-postgres psql -U umbral -d umbral -c "CREATE DATABASE umbral_bdt_game;"
```

Si una base ya existe, PostgreSQL devolvera error. En ese caso puedes continuar.

Cadena base para servicios ejecutados desde Windows contra el contenedor:

```txt
Host=localhost;Port=55432;Database=<database>;Username=umbral;Password=16102005
```

## Levantamiento de Keycloak

El contenedor se levanta con:

```txt
usuario admin: admin
password admin: admin
```

Abrir:

```txt
http://localhost:8080
```

### Crear realm

Crear el realm:

```txt
UMBRAL-UCAB
```

### Crear roles de realm

Crear estos roles:

- `Administrador`
- `Operador`
- `Participante`

### Crear cliente web

Crear cliente publico:

```txt
Client ID: umbral-web
Client authentication: Off
Standard flow: On
```

Configurar:

```txt
Valid redirect URIs: http://localhost:5173/*
Web origins: http://localhost:5173
```

### Crear cliente mobile

Crear cliente publico:

```txt
Client ID: umbral-mobile
Client authentication: Off
Standard flow: On
PKCE: S256
```

Configurar:

```txt
Valid redirect URIs for Expo Go: exp://<IP-LAN>:8081/--/auth
Valid redirect URIs for development build / installed app: umbral://auth
```

La app mobile envia a Keycloak el valor exacto de `EXPO_PUBLIC_AUTH_REDIRECT_URI`. Si usas Expo Go, ese valor debe ser `exp://<IP-LAN>:8081/--/auth` y debe estar registrado igual en Keycloak. Si usas `npx expo start --port 8099`, cambia tambien el puerto en `.env` y Keycloak. Si usas un development build o app instalada, usa `umbral://auth`.

### Crear clientes de servicios backend

Crear clientes para validacion de audiencia JWT cuando aplique:

- `identity-service`
- `team-service`
- `bdt-game-service`

Para desarrollo local pueden ser clientes confidenciales o publicos segun el flujo que se pruebe. Para Identity Service, si se usa creacion real de usuarios en Keycloak, configurar un cliente con permisos administrativos y proveer `KEYCLOAK_CLIENT_SECRET`.

### Crear usuarios de prueba

Crear usuarios de prueba y asignar roles de realm:

| Usuario sugerido | Rol |
|---|---|
| `admin` | `Administrador` |
| `operador` | `Operador` |
| `participante` | `Participante` |

Asignar password no temporal para pruebas manuales.

## Levantamiento de microservicios

Ruta recomendada: levantar infraestructura con Docker y ejecutar APIs con `dotnet run`.

Nota: `infra/docker-compose.yml` define builds para los cuatro servicios, pero actualmente solo `team-service` tiene `Dockerfile`. Por eso, para desarrollo local completo, usa `dotnet run` para las APIs.

Ejecutar cada microservicio en una terminal separada.

Para que Expo Go en un telefono fisico pueda consumir las APIs, los servicios usados por mobile deben escuchar en todas las interfaces (`0.0.0.0`), no solo en `localhost`. Las URLs del `.env` mobile deben seguir usando la IP LAN de la computadora.

### Identity Service

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://localhost:5000'
$env:ConnectionStrings__IdentityDatabase='Host=localhost;Port=55432;Database=umbral_identity;Username=umbral;Password=16102005'
$env:KEYCLOAK_BASE_URL='http://localhost:8080'
$env:KEYCLOAK_REALM='UMBRAL-UCAB'
$env:KEYCLOAK_CLIENT_ID='identity-service'
$env:KEYCLOAK_VALID_AUDIENCES='identity-service,umbral-web,account'
$env:KEYCLOAK_VALID_ISSUERS='http://localhost:8080/realms/UMBRAL-UCAB,http://192.168.1.104:8080/realms/UMBRAL-UCAB'
$env:KEYCLOAK_CLIENT_SECRET='<client-secret-de-identity-service>'
dotnet run --project "services/identity-service/src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj"
```

Si solo se van a probar endpoints protegidos con tokens ya emitidos y no la creacion real de usuarios en Keycloak, el secreto puede no ser necesario para todos los flujos. Para HU-01, la integracion Keycloak real si requiere configuracion completa.

### Team Service

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://0.0.0.0:5099'
$env:ConnectionStrings__TeamDatabase='Host=localhost;Port=55432;Database=umbral_team;Username=umbral;Password=16102005'
$env:KEYCLOAK_BASE_URL='http://localhost:8080'
$env:KEYCLOAK_REALM='UMBRAL-UCAB'
$env:KEYCLOAK_CLIENT_ID='team-service'
$env:KEYCLOAK_VALID_AUDIENCES='team-service,umbral-mobile,account'
$env:KEYCLOAK_VALID_ISSUERS='http://localhost:8080/realms/UMBRAL-UCAB,http://192.168.1.104:8080/realms/UMBRAL-UCAB'
dotnet run --project "services/team-service/src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj"
```

### Trivia Game Service

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://0.0.0.0:5015'
$env:ConnectionStrings__TriviaGameDb='Host=localhost;Port=55432;Database=umbral_trivia_game;Username=umbral;Password=16102005'
dotnet run --project "services/trivia-game-service/src/Umbral.TriviaGame.Api/Umbral.TriviaGame.Api.csproj"
```

Nota: este servicio usa validacion JWT relajada en desarrollo segun su `Program.cs`. Para pruebas manuales con Keycloak, revisar tambien claims/roles usados por los endpoints de Trivia.

### BDT Game Service

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://0.0.0.0:5016'
$env:ConnectionStrings__BdtDatabase='Host=localhost;Port=55432;Database=umbral_bdt_game;Username=umbral;Password=16102005'
$env:KEYCLOAK_BASE_URL='http://localhost:8080'
$env:KEYCLOAK_REALM='UMBRAL-UCAB'
$env:KEYCLOAK_CLIENT_ID='bdt-game-service'
$env:KEYCLOAK_VALID_AUDIENCES='bdt-game-service,umbral-web,umbral-mobile,account'
$env:KEYCLOAK_VALID_ISSUERS='http://localhost:8080/realms/UMBRAL-UCAB,http://192.168.1.104:8080/realms/UMBRAL-UCAB'
dotnet run --project "services/bdt-game-service/src/Umbral.BdtGameService.Api/Umbral.BdtGameService.Api.csproj"
```

Verificar desde Windows que las APIs respondan por IP LAN antes de probar mobile:

```powershell
Test-NetConnection -ComputerName "<IP-LAN>" -Port 5099
Test-NetConnection -ComputerName "<IP-LAN>" -Port 5015
Test-NetConnection -ComputerName "<IP-LAN>" -Port 5016
```

Los tres deben mostrar `TcpTestSucceeded : True`. Si siguen en `False`, revisar que las APIs hayan sido reiniciadas con `0.0.0.0` y permitir esos puertos en Windows Defender Firewall para red privada.

## Levantamiento de front web

Entrar al directorio:

```powershell
cd frontend
```

Instalar dependencias:

```powershell
npm install
```

Crear o actualizar `.env`:

```env
VITE_IDENTITY_API_BASE_URL=http://localhost:5000
VITE_TRIVIA_API_BASE_URL=http://localhost:5015
VITE_BDT_API_BASE_URL=http://localhost:5016
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=UMBRAL-UCAB
VITE_KEYCLOAK_CLIENT_ID=umbral-web
```

Levantar:

```powershell
npm run dev
```

Abrir:

```txt
http://localhost:5173
```

Flujos web disponibles segun rol:

- `Administrador`: HU-01 crear usuario, HU-02 gestionar usuarios.
- `Operador`: HU-34 crear BDT, HU-37 listar BDT, flujos de Trivia implementados en frontend si estan activos en la rama.

## Levantamiento de movil

Para telefono fisico, no usar `localhost` en `.env`; usar la IP LAN de la computadora.

Obtener IP LAN en Windows:

```powershell
ipconfig
```

Buscar la IPv4 de la red Wi-Fi o Ethernet, por ejemplo:

```txt
192.168.1.20
```

Entrar al directorio mobile:

```powershell
cd mobile
```

Instalar dependencias:

```powershell
npm install
```

Crear `.env` a partir de `.env.example` y completar todas las URLs necesarias:

```env
EXPO_PUBLIC_KEYCLOAK_URL=http://<IP-LAN>:8080
EXPO_PUBLIC_KEYCLOAK_REALM=UMBRAL-UCAB
EXPO_PUBLIC_KEYCLOAK_CLIENT_ID=umbral-mobile
EXPO_PUBLIC_TEAM_API_BASE_URL=http://<IP-LAN>:5099
EXPO_PUBLIC_BDT_API_BASE_URL=http://<IP-LAN>:5016
EXPO_PUBLIC_TRIVIA_API_BASE_URL=http://<IP-LAN>:5015
EXPO_PUBLIC_APP_SCHEME=umbral
EXPO_PUBLIC_AUTH_REDIRECT_URI=exp://<IP-LAN>:8081/--/auth
```

Ejemplo:

```env
EXPO_PUBLIC_KEYCLOAK_URL=http://192.168.1.20:8080
EXPO_PUBLIC_KEYCLOAK_REALM=UMBRAL-UCAB
EXPO_PUBLIC_KEYCLOAK_CLIENT_ID=umbral-mobile
EXPO_PUBLIC_TEAM_API_BASE_URL=http://192.168.1.20:5099
EXPO_PUBLIC_BDT_API_BASE_URL=http://192.168.1.20:5016
EXPO_PUBLIC_TRIVIA_API_BASE_URL=http://192.168.1.20:5015
EXPO_PUBLIC_APP_SCHEME=umbral
EXPO_PUBLIC_AUTH_REDIRECT_URI=exp://192.168.1.20:8081/--/auth
```

Levantar Expo:

```powershell
npm run start
```

Abrir el QR con Expo Go.

Si usas emulador Android puedes probar:

```powershell
npm run android
```

Si usas iOS en macOS:

```powershell
npm run ios
```

## Orden recomendado de levantamiento completo

1. Levantar infraestructura:

```powershell
docker compose -f "infra/docker-compose.yml" up -d postgres rabbitmq keycloak
```

2. Crear bases PostgreSQL si no existen.

3. Configurar Keycloak: realm, roles, clientes y usuarios.

4. Levantar microservicios en terminales separadas:

```txt
Identity Service -> http://localhost:5000
Team Service -> http://localhost:5099
Trivia Game Service -> http://localhost:5015
BDT Game Service -> http://localhost:5016
```

5. Levantar React web:

```powershell
cd frontend
npm run dev
```

6. Levantar mobile:

```powershell
cd mobile
npm run start
```

## Verificacion rapida

### Infraestructura

```powershell
docker compose -f "infra/docker-compose.yml" ps
```

### Front web

```powershell
cd frontend
npm test -- --run
npm run build
```

### Mobile

```powershell
cd mobile
npm test
npm run typecheck
```

### Backend

Identity:

```powershell
dotnet test "services/identity-service/Umbral.IdentityService.sln"
```

Team:

```powershell
dotnet test "services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj"
dotnet test "services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj"
dotnet test "services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj"
```

Trivia:

```powershell
dotnet test "services/trivia-game-service/Umbral.TriviaGame.sln"
```

BDT:

```powershell
dotnet test "services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj"
dotnet test "services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj"
dotnet test "services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj"
```

## Problemas comunes

### El telefono no conecta con APIs

- No uses `localhost` en mobile.
- Usa la IP LAN de la computadora.
- Verifica que telefono y computadora esten en la misma red.
- Permite el puerto en firewall de Windows si aplica.

### Keycloak redirige pero vuelve sin rol

- Verifica que el usuario tenga rol de realm, no solo rol de cliente.
- Roles esperados: `Administrador`, `Operador`, `Participante`.

### Error de audiencia JWT

- Agrega el cliente correspondiente a `KEYCLOAK_VALID_AUDIENCES`.
- Para mobile suele requerirse incluir `umbral-mobile`.
- Para web suele requerirse incluir `umbral-web`.

### PostgreSQL no encuentra base de datos

- Crea la base con `docker exec ... psql ... CREATE DATABASE ...`.
- Verifica que la cadena use puerto `55432` desde Windows.
- Dentro de Docker el puerto es `5432`; desde host Windows es `55432`.

### Docker Compose completo falla al construir microservicios

- La ruta recomendada actual es levantar solo `postgres`, `rabbitmq` y `keycloak` con Docker.
- Ejecuta los microservicios con `dotnet run`.
- Esto evita fallos por Dockerfiles faltantes en algunos servicios.

### Trivia tests tienen una falla conocida

- `services/trivia-game-service/AGENTS.md` documenta una falla preexistente en un test API por aislamiento InMemory.
- Si aparece `GetAll_NoGames_ReturnsEmptyList`, revisar ese archivo antes de asumir regresion.

## Apagado

Detener frontend/mobile con `Ctrl+C` en sus terminales.

Detener infraestructura:

```powershell
docker compose -f "infra/docker-compose.yml" down
```

Detener y borrar volumen PostgreSQL local si necesitas reiniciar datos:

```powershell
docker compose -f "infra/docker-compose.yml" down -v
```

Usar `down -v` elimina los datos persistidos de PostgreSQL.
