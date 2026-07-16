$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

# Carga un archivo .env expandiendo referencias ${VAR} y ${VAR:-default}
# usando las variables de entorno ya definidas en el proceso.
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
        $value = [regex]::Replace($value, '\$\{([A-Za-z_][A-Za-z0-9_]*)(?::-([^}]*))?\}', {
            param($m)
            $existing = [System.Environment]::GetEnvironmentVariable($m.Groups[1].Value)
            if ([string]::IsNullOrEmpty($existing)) { return $m.Groups[2].Value }
            return $existing
        })
        Set-Item -Path "Env:$name" -Value $value
    }
}

# 1) Variables GLOBALES del repo (LAN_IP, puertos, Keycloak, BD)
Import-EnvFile -Path (Join-Path $PSScriptRoot '..\..\.env')
# 2) Valores propios del servicio (referencian ${VAR} de las globales)
Import-EnvFile -Path (Join-Path $PSScriptRoot '.env')

dotnet run --project 'src/Umbral.IdentityService.Api/Umbral.IdentityService.Api.csproj'
