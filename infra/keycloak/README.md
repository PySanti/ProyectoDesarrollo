# Keycloak — realm import + tema de login UMBRAL

## Realm import (UMBRAL-UCAB)

`start-dev` guarda el realm en una base H2 dentro del contenedor. Si el contenedor se
**recrea** (p. ej. al cambiar volúmenes), esa base se pierde y el realm `UMBRAL-UCAB`
desaparece → el flujo de login muestra **"page not found"** (Keycloak no encuentra el realm).

Para evitarlo, el realm se siembra de forma reproducible:

```
infra/keycloak/import/umbral-realm.json   # realm, roles, clientes y usuarios de prueba
```

El `docker-compose.yml`:
- monta `./keycloak/import` en `/opt/keycloak/data/import`,
- arranca con `start-dev --import-realm` (importa si el realm no existe; estrategia `IGNORE_EXISTING`),
- persiste `/opt/keycloak/data` en el volumen `umbral-keycloak-data` para que los cambios
  hechos en runtime sobrevivan a una recreación,
- corre el one-shot **`keycloak-config`** ([keycloak-config-cli](https://github.com/adorsys/keycloak-config-cli))
  en cada `up`: aplica `umbral-realm.json` sobre el realm **existente** de forma idempotente
  (config-as-code), creando/actualizando roles, clientes, audience mappers y usuarios sembrados
  **sin borrar** los usuarios creados en runtime. Esto corrige la deriva que `--import-realm`
  no cubre (volumen persistido con un realm viejo → tokens sin audiencia/permisos → 401/403).

El import ya define `loginTheme=umbral`, así que la página de credenciales **sale con la marca
UMBRAL sin pasos manuales**.

Usuarios de prueba sembrados (password no temporal): `admin`/`admin` (Administrador),
`operador`/`operador` (Operador), `participante`/`participante` (Participante).

**Aplicar cambios del JSON a un realm ya importado** (conserva usuarios de runtime):

```powershell
docker compose -f "infra/docker-compose.yml" --env-file .env up keycloak-config
```

(Corre solo también en cada `up -d` del stack; termina con exit 0 y no queda corriendo.)

**Re-sembrar desde cero** (descarta TODOS los datos de runtime de Keycloak, incluidos
usuarios creados durante las pruebas):

```powershell
docker compose -f "infra/docker-compose.yml" rm -sfv keycloak
docker volume rm infra_umbral-keycloak-data
docker compose -f "infra/docker-compose.yml" --env-file .env up -d keycloak
```

## Permisos funcionales (ADR-0013)

El realm define 3 realm roles técnicos — `GestionarPartidas`, `GestionarEquipos`,
`ParticiparEnPartidas` — que Keycloak expande automáticamente en `realm_access.roles`
del token cuando son **composite** de un rol base. No se asignan a usuarios directamente.

**El realm declara sólo lo fijo.** Su único composite es `Participante → ParticiparEnPartidas`:
por eso ese permiso no es asignable desde el panel (el PUT lo rechaza con 400).

**Los privilegios gobernables no se declaran aquí.** `GestionarPartidas` y `GestionarEquipos` los
gobierna la tabla `permisos_rol`, y el reconciliador de Identity converge Keycloak hacia ella al
arrancar. Por defecto: Administrador → `GestionarEquipos`; Operador → `GestionarPartidas`;
Participante → ninguno.

Los dos conjuntos no se solapan, y eso es deliberado: `keycloak-config` reaplica este realm en cada
`up`, así que declarar aquí un privilegio gobernable borraría lo que el administrador hubiera
asignado desde el panel — que es exactamente el bug que este reparto resuelve.

**Entornos con el realm ya importado:** re-importar el realm (o crear los 3 roles
técnicos y sus composites a mano en la consola admin). Los tokens emitidos antes del
re-seed no llevan los permisos → 403 hasta re-login/refresh.

Verificación: `python3 scripts/check-realm-composites.py`

## Tema de login

Tema custom que reemplaza la interfaz genérica de Keycloak por la identidad de UMBRAL
(tema claro, marca magenta, tipografías Inter / Space Grotesk). Vive en:

```
infra/keycloak/themes/umbral/login/
  theme.properties              # extiende el tema base "keycloak" y añade umbral.css
  resources/css/umbral.css      # overlay de marca (tokens de DESIGN.md)
```

Es un **overlay de CSS** sobre la plantilla base (no se reescriben los `.ftl`), así que es
resiliente entre versiones de Keycloak.

## Activación

1. El `docker-compose.yml` ya monta los temas en el contenedor:
   ```yaml
   keycloak:
     volumes:
       - ./keycloak/themes:/opt/keycloak/themes
   ```
   Con `start-dev` la caché de temas está desactivada, así que los cambios en CSS se
   reflejan al recargar la página de login (sin reconstruir la imagen).

2. Levantar/recrear Keycloak:
   ```powershell
   docker compose -f "infra/docker-compose.yml" up -d --force-recreate keycloak
   ```

3. El realm import ya fija `loginTheme=umbral`, así que **no hace falta activarlo a mano**.
   (Si lo cambiaste en runtime: consola de admin `http://localhost:8080` → realm
   **UMBRAL-UCAB** → **Realm settings → Themes → Login theme → `umbral`** → Save.)

4. Abrir el flujo de login (p. ej. desde la web `umbral-web`) y verificar.

## Verificación (no se pudo automatizar aquí)

Este tema **no se levantó/validó** en el entorno donde se creó. Al activarlo, revisar:

- Fondo claro, tarjeta blanca con borde y sombra suave (sin el gradiente/imagen genéricos).
- Botón primario en **magenta** (`#982f93`), hover más profundo.
- Inputs con foco magenta (anillo `#fbe8f8`).
- Tipografía Inter en el cuerpo; encabezados en Space Grotesk.

Si la versión de Keycloak usa nombres de hoja de estilo base distintos y el layout se ve roto,
ajustar la línea `styles=` en `theme.properties` (revisar el `theme.properties` del tema padre
`keycloak` en esa versión) y/o los selectores en `umbral.css`. Keycloak 25 (la imagen del compose,
`quay.io/keycloak/keycloak:25.0`) usa PatternFly v5; los selectores `.pf-v5-c-*` cubren ese caso.
