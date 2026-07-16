$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

. (Join-Path $PSScriptRoot '..\scripts\env-value.ps1')

# Carga el .env raiz del repo (puertos, realm) en el proceso para que Vite
# pueda expandir las referencias ${VAR} de frontend/.env.
function Import-EnvFile {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) { return }
        $parts = $line -split '=', 2
        if ($parts.Count -ne 2) { return }
        $name = $parts[0].Trim()
        $value = ConvertFrom-EnvValue $parts[1]
        Set-Item -Path "Env:$name" -Value $value
    }
}

Import-EnvFile -Path (Join-Path $PSScriptRoot '..\.env')

npm run dev
