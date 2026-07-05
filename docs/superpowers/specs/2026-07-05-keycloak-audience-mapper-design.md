# Diseño — Audience mapper en Keycloak para tokens de cliente (fix del gap SP-5a)

Fecha: 2026-07-05
Rama: feature/code-migration-SP-5
Relacionado: ADR-0013 (composites Keycloak SP-5a), memoria `env-keycloak-audience-gotcha`

## Contexto y problema

Durante el pase visual en navegador de la superficie **Gobernanza** (SP-5c, 2026-07-05) con Keycloak
+ identity-service + login `admin` real, `GET /identity/governance/roles` devolvía **401**:

```
SecurityTokenInvalidAudienceException: Audiences: 'account'.
Did not match ValidAudiences: 'umbral-web, umbral-mobile, identity-service'.
```

Un `Administrador` real autenticado por `umbral-web` **no puede cargar ninguna pantalla que llame a
identity** en el estado commiteado. Los tests de identity (167/41/41) usan un `TestAuthHandler`, no un
token real de Keycloak, así que no atrapan la regresión.

### Causa raíz

1. Los tokens que emite `umbral-web` (y `umbral-mobile`) traen **solo `aud='account'`** — proviene del
   scope por defecto del realm; no hay ningún *audience protocol mapper* que agregue la audiencia de la
   API. El realm commiteado (`infra/keycloak/import/umbral-realm.json`) tiene **cero** audience mappers
   (clients `umbral-web`/`umbral-mobile`/`identity-service` con `protocolMappers: []`).
2. identity valida audiencia contra `Keycloak:ValidAudiences = "umbral-web,umbral-mobile"` (appsettings)
   + append de su propio `ClientId` (`identity-service`). `account` no está en la lista → rechaza.
3. SP-5a re-homeó las audiencias a valores service-oriented pero **nunca agregó el mapper** que las pone
   en el token. El pase visual anterior (2026-06-14, 5 superficies) funcionó porque era **pre-SP-5a**,
   cuando los services aceptaban `account`.

### Alcance real (verificado)

Solo **identity** tiene `ValidAudiences` en appsettings y está vivo validando hoy. `gateway`, `partidas`,
`operaciones-sesion`, `puntuaciones` **no** tienen config de audiencia en appsettings (puntuaciones aún
no tiene JWT), están en migración, sin `.env` y sin correr. → El único validador afectado en la práctica
es identity. Aun así el fix es a nivel-token, por lo que los services futuros lo heredan.

## Decisión

Agregar un **`oidc-audience-mapper`** a los clients de usuario `umbral-web` y `umbral-mobile` en el realm
JSON, cada uno inyectando **su propio `clientId`** como audiencia del access token:

```json
{
  "name": "umbral-web-audience",
  "protocol": "openid-connect",
  "protocolMapper": "oidc-audience-mapper",
  "consentRequired": false,
  "config": {
    "included.client.audience": "",
    "included.custom.audience": "umbral-web",
    "id.token.claim": "false",
    "access.token.claim": "true",
    "lightweight.claim": "false"
  }
}
```

(análogo para `umbral-mobile` con `included.custom.audience": "umbral-mobile"`).

Resultado: el access token pasa a `aud = ["account", "umbral-web"]` (o `umbral-mobile`). identity ya
acepta `umbral-web`/`umbral-mobile` → carga **sin tocar appsettings de ningún service**.

### Por qué esta opción

- **Idiomática Keycloak**: la audiencia se resuelve en el token, no relajando cada validador.
- **`account` sigue NO aceptado** → la validación de audiencia sigue siendo real (defense-in-depth
  intacto). Descartada la alternativa de agregar `account` a `ValidAudiences` (haría la validación un
  no-op: `account` está en todo token del realm).
- **El propio clientId** (no `identity-service`) es el valor generalizable: identity ya lista
  `umbral-web,umbral-mobile`; cualquier service futuro que copie ese patrón de appsettings lo hereda.
- **Mínimo**: 2 mappers en 1 archivo. Sin código, sin contratos, sin appsettings.

## Fuera de alcance

- No se toca ningún appsettings de service ni del gateway.
- No se toca código (`Program.cs`, `KeycloakJwtExtensions.cs`) ni contratos.
- No se wirea auth de `puntuaciones` ni se configuran audiencias de `partidas`/`operaciones`/`gateway`
  (eso corresponde a sus propios slices de migración; heredarán el fix cuando listen `umbral-web`/`umbral-mobile`).
- No se elimina el `account` de ninguna lista de audiencias válidas.

## Verificación

El artefacto commiteado es el realm JSON; la verificación honesta es re-sembrar desde ese JSON:

1. Bajar Keycloak **con volumen** (`docker compose down` + eliminar `infra_umbral-keycloak-data`) — el
   import es `IGNORE_EXISTING`, así que sin borrar el volumen el realm viejo persiste.
2. `up` Keycloak → import del realm nuevo (verificar en logs). Levantar identity (:5000) **sin** el
   override local `Keycloak__ValidAudiences` en `.env` (probar que el fix commiteado se sostiene solo).
3. Login `admin`/`admin`; inspeccionar el access token → `aud` contiene `umbral-web`.
4. `GET /identity/governance/roles` → **200** (antes 401).
5. Re-correr `frontend/scripts/gov-visual-pass.mjs` → matriz carga (cards=3, loadError=0).

**Costo del re-seed** (conocido, aceptable en dev): borra los usuarios creados en runtime (quedan los del
seed: `admin`/`operador`/`participante`); rota las llaves de firma del realm → hay que re-loguear en web
y reiniciar los backend que ya estuvieran corriendo.

## Criterios de aceptación

- El realm JSON tiene un `oidc-audience-mapper` en `umbral-web` (aud `umbral-web`) y en `umbral-mobile`
  (aud `umbral-mobile`), `access.token.claim=true`.
- Tras re-seed, el access token de `admin` (umbral-web) trae `umbral-web` en `aud`.
- `GET /identity/governance/roles` responde 200 con el token real, **sin** override local en `.env`.
- `account` sigue sin estar en las `ValidAudiences` de ningún componente.
- Cero cambios fuera de `infra/keycloak/import/umbral-realm.json` (+ este spec y el plan).
