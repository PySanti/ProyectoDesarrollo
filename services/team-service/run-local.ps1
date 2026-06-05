$ErrorActionPreference = 'Stop'

Set-Location $PSScriptRoot

if (Test-Path '.env') {
    Get-Content '.env' | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) { return }
        $parts = $line -split '=', 2
        if ($parts.Count -ne 2) { return }
        $name = $parts[0].Trim()
        $value = $parts[1].Trim().Trim("'").Trim('"')
        Set-Item -Path "Env:$name" -Value $value
    }
}

dotnet run --project 'src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj'
