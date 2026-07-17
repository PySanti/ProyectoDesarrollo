# Validación de RNF-21 y RNF-22 — gateway YARP

**Fecha:** 2026-07-16 · **Alcance:** `gateway/` · **Tipo:** validación (no se modifica el gateway).

## Requisitos bajo prueba

**RNF-21** (`docs/01-project-source/srs.md:265`):

> El acceso al backend debe realizarse a través de un API Gateway implementado con YARP, que actúa como
> punto único de entrada; ningún cliente accede directamente a los microservicios. El gateway valida el
> token JWT emitido por Keycloak (RNF-13) en cada petición, rechaza las peticiones no autenticadas y
> enruta todo el tráfico —incluido el de tiempo real (WebSockets/SignalR)— hacia el microservicio
> correspondiente.

**RNF-22** (`srs.md:266`):

> El API Gateway debe aplicar la autorización de acceso por rol (Administrador, Operador, Participante)
> a nivel de ruta, usando los claims de rol contenidos en el token JWT, sin consultar a Identity en cada
> petición. La autorización fina por permisos funcionales (por ejemplo, "Gestionar partidas") permanece
> en los microservicios.

Se descomponen en cláusulas verificables por separado, porque cada una se prueba con una evidencia
distinta:

| # | Cláusula | Requisito | Evidencia |
|---|---|---|---|
| C1 | YARP es el punto único de entrada | RNF-21 | estática (código de clientes) |
| C2 | Ningún cliente accede directo a los microservicios | RNF-21 | estática (código de clientes) |
| C3 | Valida el JWT de Keycloak en cada petición | RNF-21 | **en vivo** (los tests no la cubren) |
| C4 | Rechaza las peticiones no autenticadas | RNF-21 | tests + en vivo |
| C5 | Enruta todo el tráfico, incluido tiempo real | RNF-21 | estática + en vivo |
| C6 | Autoriza por rol a nivel de ruta con los claims | RNF-22 | tests + en vivo |
| C7 | Sin consultar a Identity en cada petición | RNF-22 | estática + observación de logs |
| C8 | El permiso fino queda en los microservicios | RNF-22 | estática |

---

## 1. Evidencia estática

### 1.1 Políticas de autorización (C6)

`gateway/src/Umbral.Gateway/Program.cs:15-21` — cinco políticas, todas `RequireRole`, más un suelo
fail-secure:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Administrador", p => p.RequireRole("Administrador"))
    .AddPolicy("Operador", p => p.RequireRole("Operador"))
    .AddPolicy("Participante", p => p.RequireRole("Participante"))
    .AddPolicy("GestionarPartidas", p => p.RequireRole("GestionarPartidas"))
    .AddPolicy("GestionarEquipos", p => p.RequireRole("GestionarEquipos"))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
```

`SetFallbackPolicy` es la pieza que cierra C4 estructuralmente: **toda ruta que no declare política
hereda "hay que estar autenticado"**. Olvidarse de poner una política no abre un agujero.

Los dos privilegios gobernables (`GestionarPartidas`, `GestionarEquipos`) son también role claims del
token (ADR-0013), así que el mecanismo es idéntico al de los roles base — sigue siendo autorización
gruesa por ruta, no por recurso.

### 1.2 Mapa ruta → política (C6)

`gateway/src/Umbral.Gateway/appsettings.json:11-62`:

| Ruta | Path | Método | Order | Política |
|---|---|---|---|---|
| `identity-governance` | `/identity/governance/{**catch-all}` | todos | 1 | `Administrador` |
| `identity-users` | `/identity/users/{**catch-all}` | todos | 1 | `Administrador` |
| `identity-admin-teams` | `/identity/admin/teams/{**catch-all}` | todos | 1 | `GestionarEquipos` |
| `identity-teams-listing` | `/identity/teams` | **GET** | 0 | `GestionarEquipos` |
| `identity-teams` | `/identity/teams/{**catch-all}` | todos | 1 | `Participante` |
| `identity` | `/identity/{**catch-all}` | todos | 2 | `Default` |
| `partidas` | `/partidas/{**catch-all}` | todos | — | `GestionarPartidas` |
| `operaciones-sesion` | `/operaciones-sesion/{**catch-all}` | todos | — | `Default` |
| `puntuaciones` | `/puntuaciones/{**catch-all}` | todos | — | `Default` |

`Default` es palabra reservada de YARP → política por defecto de la aplicación = autenticado.
El `Order` importa: `identity-teams-listing` (0) gana sobre `identity-teams` (1), que a su vez gana
sobre el catch-all `identity` (2). Si esa precedencia se rompiera, un participante autenticado entraría
a `/identity/users` por el catch-all — hay un test que pinnea justo eso.

### 1.3 Qué se valida del token (C3)

`gateway/src/Umbral.Gateway/Security/KeycloakJwtExtensions.cs:38-47`:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuers = validIssuers,
    ValidateAudience = true,
    ValidAudiences = validAudiences,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    RoleClaimType = "roles"
};
```

Las cuatro validaciones exigidas están activas: **emisor, audiencia, expiración y firma**. La clave de
firma se obtiene del JWKS del `Authority` (`{BaseUrl}/realms/{Realm}`, `:26`). Emisores y audiencias
salen de `KEYCLOAK_VALID_ISSUERS` / `KEYCLOAK_VALID_AUDIENCES` (`:15-16`), con el authority y el
`clientId` añadidos como respaldo (`:28,:30`). `RequireHttpsMetadata` sigue al entorno (`:37`): relajado
sólo en Development.

### 1.4 De `realm_access` a claims de rol (C6)

`gateway/src/Umbral.Gateway/Security/KeycloakRoleClaims.cs` — pieza fácil de pasar por alto y sin la
cual RNF-22 no funcionaría: Keycloak **no** emite claims `roles` planos, sino un JSON anidado
(`realm_access.roles`, `resource_access.*.roles`). `AddRolesFromKeycloakClaims` lo desempaqueta en el
evento `OnTokenValidated` (`KeycloakJwtExtensions.cs:61-68`) y añade cada rol como claim plano, que es
lo que `RequireRole` sabe leer.

### 1.5 Tiempo real (C5)

- `Program.cs:46` — `app.MapReverseProxy()`; el passthrough de WebSocket es automático en YARP.
- `KeycloakJwtExtensions.cs:50-60` — el token se lee del **query string** `access_token` para las rutas
  `/operaciones-sesion/hubs` y `/puntuaciones/hubs`. Necesario porque SignalR sobre WebSocket no puede
  mandar la cabecera `Authorization`. Sin esto, el tiempo real autenticado no pasaría por el gateway.

### 1.6 Sin consultar a Identity (C7)

Búsqueda de `HttpClient|IdentityApi|http://identity` en `gateway/src/` → **una sola coincidencia**:
`appsettings.json:65`, la dirección del cluster de destino. **El gateway no tiene ningún cliente HTTP
propio**: no hay a quién consultar. La decisión sale íntegra del token.

### 1.7 Punto único de entrada (C1, C2)

- **Web:** `frontend/src/api/*.ts` leen **sólo** `VITE_GATEWAY_BASE_URL` y fallan explícitamente si
  falta (`adminTeamsApi.ts:50-54`: `"Missing VITE_GATEWAY_BASE_URL environment variable."`).
  `VITE_IDENTITY_API_BASE_URL`, `VITE_TRIVIA_API_BASE_URL` y `VITE_BDT_API_BASE_URL` **no aparecen en
  ningún archivo de `frontend/`** — búsqueda sin resultados.
- **Móvil:** `mobile/.env` → `EXPO_PUBLIC_GATEWAY_BASE_URL=http://localhost:5080`, único endpoint de
  backend.
- `frontend/.env.example` declara `VITE_GATEWAY_BASE_URL` como la única URL de backend.

El código de los clientes, por tanto, **no sabe cómo llegar a un microservicio**: sólo conoce el
gateway. C1 y C2 quedan cerradas estructuralmente, no por disciplina.

### 1.8 Permiso fino en los microservicios (C8)

Las políticas del gateway son por **ruta completa**, nunca por recurso (`appsettings.json`: todos los
match son de path/método). La autorización por recurso vive en los servicios; p. ej. Identity aplica su
propia policy `AdminOnly` en `UsersController` (`services/identity-service/src/.../UsersController.cs:13`),
duplicando la del gateway a propósito (defensa en profundidad).

---

## 2. Tests del gateway

Comando:

```powershell
dotnet test "gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj"
```

Resultado: _(pendiente — el clasificador de comandos del entorno estaba caído; reejecutar)_

`gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs` cubre 30 casos (`[Fact]`
contados en el archivo; la cifra "24" que arrastra `traceability-matrix.md` de un slice anterior está
desactualizada): `/health`
anónimo; 401 sin token en ruta con política explícita, con `Default` y en ambos hubs; el fallback
fail-secure comprobado **directamente** contra el `IAuthorizationPolicyProvider` (`:74-86`); la matriz
de roles por ruta; la precedencia `identity-users` sobre el catch-all (`:119-126`); privilegio-sin-rol
(`Participante,GestionarPartidas` pasa en `/partidas`, `:212-218`); y CORS preflight.

### ⚠️ Límite de estos tests (importante)

Usan `TestAuthHandler`, un esquema de autenticación **falso** inyectado por `ConfigureTestServices`
(`:88-100`): los roles llegan por la cabecera `X-Test-Roles`. **Prueban el cableado de políticas, no la
validación del JWT.** Firma, emisor, audiencia y expiración —el núcleo de C3— no los toca ningún test.
De ahí que el paso en vivo no sea opcional: es la **única** evidencia de C3.

---

## 3. Verificación en vivo

_(pendiente de ejecución — el entorno tenía los contenedores caídos y el clasificador de comandos
inoperativo al redactar esto)_

### 3.1 Cómo ver qué valida YARP contra el token (C3)

La vía directa es la cabecera **`WWW-Authenticate`** de la respuesta 401: ASP.NET publica ahí el motivo
exacto del rechazo. Contra `http://localhost:5080/identity/users`:

| Token enviado | Esperado |
|---|---|
| ninguno | `401`, sin `error_description` |
| basura (`Bearer abc.def.ghi`) | `401` + `error="invalid_token"` |
| expirado | `401` + `The token expired` |
| audiencia equivocada | `401` + `The audience is invalid` |
| emisor equivocado | `401` + `The issuer is invalid` |
| firma manipulada | `401` + firma inválida |

Vía complementaria (logs): `ASPNETCORE_ENVIRONMENT=Development` ya deja `Default: Debug` y `Yarp: Debug`
(`gateway/src/Umbral.Gateway/appsettings.Development.json`). Para ver además la validación del JWT y qué
política decidió, añadir al `environment` del servicio `gateway`:

```yaml
Logging__LogLevel__Microsoft.AspNetCore.Authentication: Debug
Logging__LogLevel__Microsoft.AspNetCore.Authorization: Debug
```

y seguir con `docker compose --env-file .env -f infra/docker-compose.yml logs -f gateway`.

### 3.2 Matriz de roles (C6)

`umbral-web` y `umbral-mobile` tienen `directAccessGrantsEnabled: false`
(`infra/keycloak/import/umbral-realm.json:51,79`), así que no se pueden pedir tokens por contraseña.
Opciones: habilitar el flag por Admin API y **revertirlo al terminar**, o extraer el token del navegador
tras un login real.

| Ruta | Administrador | Operador | Participante |
|---|---|---|---|
| `GET /identity/users` | pasa | 403 | 403 |
| `GET /identity/governance/roles` | pasa | 403 | 403 |
| `GET /identity/teams/mine` | 403 | 403 | pasa |
| `GET /partidas/...` | 403 (sin el privilegio) | pasa | 403 |
| `GET /identity/admin/teams` | pasa | 403 | 403 |

Defaults de privilegios: Administrador → `GestionarEquipos`; Operador → `GestionarPartidas`. Por eso un
Administrador da **403** en `/partidas`: es el comportamiento correcto, no un fallo.

### 3.3 Sin consultar a Identity (C7)

Con `logs -f identity-service` abierto, lanzar un 403 de la matriz y comprobar que Identity **no
registra ninguna petición**.

### 3.4 Tiempo real (C5)

```
GET  /operaciones-sesion/hubs/sesion                          -> 401
POST /operaciones-sesion/hubs/sesion/negotiate?access_token=… -> 200
```

---

## 4. Hallazgos

1. **`frontend/.env` desactualizado** (local, gitignoreado — **no es un incumplimiento de RNF-21**, el
   código cumple). Le falta `VITE_GATEWAY_BASE_URL`, la única URL de backend que el código lee, y
   conserva `VITE_IDENTITY_API_BASE_URL=http://localhost:5000` más las de los servicios legacy ya
   retirados (`:5015`, `:5016`), que no lee nadie. Con ese `.env`, la web fallaría con
   `"Missing VITE_GATEWAY_BASE_URL environment variable."` Arreglo: copiar de `frontend/.env.example`.
2. **Sin cobertura automatizada de la validación del JWT** (C3). Los 30 tests usan autenticación falsa.
   Un error de configuración en `ValidAudiences`/`ValidIssuers` no lo detectaría ningún test — sólo la
   prueba en vivo. Candidato a mejora: un test de integración que firme tokens con una clave de prueba
   y compruebe los rechazos por audiencia/emisor/expiración.

## 5. Veredicto

_(pendiente de completar los pasos en vivo)_
