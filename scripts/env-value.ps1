# Parseo del valor de una linea `NOMBRE=valor` de un .env, con las mismas reglas que
# `source .env` en bash (lo que hacen los run-local.sh):
#
#   - Entre comillas: se toma el contenido literal y se ignora lo que venga despues.
#   - Sin comillas: un `#` precedido de espacio abre un comentario y se descarta.
#   - Un `#` pegado al valor (pass#word) es parte del valor, no un comentario.
#
# Los run-local.ps1 solo saltaban comentarios de linea completa, asi que un comentario
# al final de una linea entraba al valor y lo corrompia en silencio. Eso rompio el correo
# de credenciales: SMTP_FROM_ADDRESS quedaba con el comentario pegado, MailAddress lo
# rechazaba por formato, y el consumidor best-effort (ADR-0012) tragaba el error sin
# senal visible. Los .sh nunca tuvieron el bug porque bash ya corta ahi.
function ConvertFrom-EnvValue {
    param([string]$Raw)
    $value = $Raw.Trim()
    if ($value -match '^"([^"]*)"') { return $matches[1] }
    if ($value -match "^'([^']*)'") { return $matches[1] }
    return ([regex]::Replace($value, '\s+#.*$', '')).Trim()
}
