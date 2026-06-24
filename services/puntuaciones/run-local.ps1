Set-Location -Path $PSScriptRoot

function Import-DotEnv($path) {
  if (Test-Path $path) {
    Get-Content $path | Where-Object { $_ -match '^\s*[^#].*=' } | ForEach-Object {
      $name, $value = $_ -split '=', 2
      [Environment]::SetEnvironmentVariable($name.Trim(), $value.Trim().Trim("'`""))
    }
  }
}

Import-DotEnv (Join-Path $PSScriptRoot "../../.env")
Import-DotEnv (Join-Path $PSScriptRoot ".env")

dotnet run --project "src/Umbral.Puntuaciones.Api/Umbral.Puntuaciones.Api.csproj"
