$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

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
        $value = $parts[1].Trim().Trim("'").Trim('"')
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
$TeamPort     = Get-EnvOrDefault 'TEAM_PORT'     '5099'
$BdtPort      = Get-EnvOrDefault 'BDT_PORT'      '5016'
$TriviaPort   = Get-EnvOrDefault 'TRIVIA_PORT'   '5015'
$IdentityPort = Get-EnvOrDefault 'IDENTITY_PORT' '5000'
$MetroPort    = Get-EnvOrDefault 'METRO_PORT'    '8081'

@"
# ARCHIVO GENERADO por run-local.ps1 a partir del .env raiz del repo.
# No lo edites a mano: cambia los valores en <repo>/.env (LAN_IP, puertos...).
REACT_NATIVE_PACKAGER_HOSTNAME=$IP
EXPO_PUBLIC_KEYCLOAK_URL=http://${IP}:${KeycloakPort}
EXPO_PUBLIC_KEYCLOAK_REALM=$Realm
EXPO_PUBLIC_KEYCLOAK_CLIENT_ID=umbral-mobile
EXPO_PUBLIC_TEAM_API_BASE_URL=http://${IP}:${TeamPort}
EXPO_PUBLIC_BDT_API_BASE_URL=http://${IP}:${BdtPort}
EXPO_PUBLIC_TRIVIA_API_BASE_URL=http://${IP}:${TriviaPort}
EXPO_PUBLIC_IDENTITY_API_BASE_URL=http://${IP}:${IdentityPort}
EXPO_PUBLIC_APP_SCHEME=umbral
EXPO_PUBLIC_AUTH_REDIRECT_URI=exp://${IP}:${MetroPort}/--/auth
"@ | Set-Content -Path '.env' -Encoding utf8

npm start -c
