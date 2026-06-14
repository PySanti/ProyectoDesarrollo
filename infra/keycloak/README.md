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
  hechos en runtime sobrevivan a una recreación.

El import ya define `loginTheme=umbral`, así que la página de credenciales **sale con la marca
UMBRAL sin pasos manuales**.

Usuarios de prueba sembrados (password no temporal): `admin`/`admin` (Administrador),
`operador`/`operador` (Operador), `participante`/`participante` (Participante).

**Re-sembrar desde el JSON** (descarta datos de runtime de Keycloak):

```powershell
docker compose -f "infra/docker-compose.yml" rm -sfv keycloak
docker volume rm infra_umbral-keycloak-data
docker compose -f "infra/docker-compose.yml" up -d keycloak
```

> Editar `umbral-realm.json` y reiniciar **no** reimporta si el realm ya existe (estrategia
> `IGNORE_EXISTING`). Para aplicar cambios del JSON, re-siembra con los comandos de arriba.

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
