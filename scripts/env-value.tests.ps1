# Casos de ConvertFrom-EnvValue. Correr: powershell -File scripts/env-value.tests.ps1
. (Join-Path $PSScriptRoot 'env-value.ps1')

$casos = @(
    @{ Entrada = 'smtp.gmail.com';                            Esperado = 'smtp.gmail.com' }
    @{ Entrada = '587';                                       Esperado = '587' }
    # El bug que rompio el correo: comentario al final de la linea.
    @{ Entrada = 'alguien@example.com   # misma cuenta';      Esperado = 'alguien@example.com' }
    @{ Entrada = '"aaaa bbbb cccc dddd"      # 16 chars';     Esperado = 'aaaa bbbb cccc dddd' }
    @{ Entrada = "'identity-service'";                        Esperado = 'identity-service' }
    # Un `#` pegado es parte del valor: los secretos pueden contenerlo.
    @{ Entrada = 'pass#word';                                 Esperado = 'pass#word' }
    @{ Entrada = '"a # b"';                                   Esperado = 'a # b' }
    # Las referencias ${VAR} se expanden despues, aca viajan literales.
    @{ Entrada = '"http://localhost:${PORT:-5000}"';          Esperado = 'http://localhost:${PORT:-5000}' }
)

$fallos = 0
foreach ($caso in $casos) {
    $obtenido = ConvertFrom-EnvValue $caso.Entrada
    if ($obtenido -ne $caso.Esperado) {
        Write-Output "FALLA: [$($caso.Entrada)] -> [$obtenido], esperado [$($caso.Esperado)]"
        $fallos++
    }
}

if ($fallos -gt 0) {
    Write-Output "$fallos de $($casos.Count) casos fallaron"
    exit 1
}
Write-Output "OK: $($casos.Count) casos"
