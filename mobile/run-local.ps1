$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

# Resuelve las referencias estilo bash `${VAR}` / `${VAR:-default}` que usa el
# .env raiz, igual que hace `source ../.env` en run-local.sh. Sin esto, un valor
# auto-referente como KEYCLOAK_REALM="${KEYCLOAK_REALM:-UMBRAL-UCAB}" entraria
# crudo al entorno, lo heredaria `npm start` y dotenv-expand entraria en
# recursion infinita (RangeError: Maximum call stack size exceeded).
# Solo la forma con llaves: `$VAR` pelado no se toca para no romper secretos
# que contengan `$` (SMTP_PASSWORD).
function Expand-EnvRefs {
    param([string]$Value)
    return [regex]::Replace($Value, '\$\{(\w+)(?::-([^}]*))?\}', {
        param($match)
        $resolved = [System.Environment]::GetEnvironmentVariable($match.Groups[1].Value)
        if ([string]::IsNullOrEmpty($resolved)) { return $match.Groups[2].Value }
        return $resolved
    })
}

# Carga las variables GLOBALES del repo (LAN_IP, puertos, realm de Keycloak)
# en el proceso para poder resolverlas al generar mobile/.env.
function Import-EnvFile {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) { return }
        $parts = $line -split '=', 2
        if ($parts.Count -ne 2) { return }
        $name = $parts[0].Trim()
        $value = Expand-EnvRefs ($parts[1].Trim().Trim("'").Trim('"'))
        Set-Item -Path "Env:$name" -Value $value
    }
}

Import-EnvFile -Path (Join-Path $PSScriptRoot '..\.env')

# Genera mobile/.env con valores LITERALES ya resueltos.
#
# IMPORTANTE: no se puede dejar ${VAR} dentro de mobile/.env. Expo/Babel
# (babel-preset-expo) construye un "modulo virtual .env" que reescribe los
# accesos a process.env.EXPO_PUBLIC_* y NO expande ${VAR}: se quedaria con el
# default (localhost), y la app intentaria abrir Keycloak en localhost desde el
# telefono -> "no se pudo cargar la pagina". Por eso resolvemos aqui, con las
# variables ya cargadas del .env raiz, y escribimos el archivo literal.
function Get-EnvOrDefault {
    param([string]$Name, [string]$Default)
    $val = [System.Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrEmpty($val)) { return $Default }
    return $val
}

$IP           = Get-EnvOrDefault 'LAN_IP'        'localhost'
$KeycloakPort = Get-EnvOrDefault 'KEYCLOAK_PORT' '8080'
$Realm        = Get-EnvOrDefault 'KEYCLOAK_REALM' 'UMBRAL-UCAB'
$GatewayPort  = Get-EnvOrDefault 'GATEWAY_PORT'  '5080'
$MetroPort    = Get-EnvOrDefault 'METRO_PORT'    '8081'

@"
# ARCHIVO GENERADO por run-local.ps1 a partir del .env raiz del repo.
# No lo edites a mano: cambia los valores en <repo>/.env (LAN_IP, puertos...).
REACT_NATIVE_PACKAGER_HOSTNAME=$IP
EXPO_PUBLIC_KEYCLOAK_URL=http://${IP}:${KeycloakPort}
EXPO_PUBLIC_KEYCLOAK_REALM=$Realm
EXPO_PUBLIC_KEYCLOAK_CLIENT_ID=umbral-mobile
EXPO_PUBLIC_GATEWAY_BASE_URL=http://${IP}:${GatewayPort}
EXPO_PUBLIC_APP_SCHEME=umbral
EXPO_PUBLIC_AUTH_REDIRECT_URI=exp://${IP}:${MetroPort}/--/auth
"@ | Set-Content -Path '.env' -Encoding utf8

npx expo start -c
