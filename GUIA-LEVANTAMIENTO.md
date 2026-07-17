## Prerequisitos

1. Ultima version de rama develop
2. Archivo .env global con 

LAN_IP
SMTP_USERNAME
SMTP_PASSWORD
SMTP_FROM_ADDRESS
KEYCLOAK_CLIENT_SECRET

3. Usar el comando

docker compose -f infra/docker-compose.yml --env-file .env up -d --build


### Puertos host

| Puerto | Qué |
|---|---|
| **5173** | Frontend web (admin/operador) — `http://localhost:5173` |
| **5080** | Gateway YARP (única entrada al backend; todo exige JWT) |
| **8080** | Keycloak (realm `UMBRAL-UCAB` se auto-importa con usuarios seed — ver `infra/keycloak/README.md`) |
| 15672 | RabbitMQ Management (guest/guest) |
| 55432 | PostgreSQL (umbral / ver `.env`) |
| 5001 / 5010 / 5020 / 5030 | identity / partidas / operaciones-sesion / puntuaciones directos (solo debug; los clientes SIEMPRE pasan por el gateway) |

## Reconstruir proyecto desde 0 eliminando db

Borra todos los datos (bases PostgreSQL + realm Keycloak) y levanta limpio.

```bash
docker compose -f infra/docker-compose.yml --env-file .env down -v
docker compose -f infra/docker-compose.yml --env-file .env up -d --build
```

## Móvil (Expo, fuera de compose)

Con el stack de compose arriba y `LAN_IP` correcta en el `.env` raíz:

```bash
cd mobile && ./run-local.sh
```

El script regenera `mobile/.env` con valores literales desde el `.env` raíz (no edites
`mobile/.env` a mano) y lanza `expo start --host lan`. Escanea el QR con Expo Go desde el
teléfono (misma Wi-Fi).

## En caso de tener problemas levantando movil con red lan

En este caso, levantaremos el movil usando un cable usb. Pasos:

1. Activar depuración USB en el celular (Android)

- Ajustes → Acerca del teléfono → tocá 7 veces "Número de compilación" (activa Opciones de desarrollador).
- Ajustes → Opciones de desarrollador → activá "Depuración USB".

2. Instalar adb en la PC (liviano, solo platform-tools)

Invoke-WebRequest -Uri "https://dl.google.com/android/repository/platform-tools-latest-windows.zip" -OutFile "$env:TEMP\platform-tools.zip"
Expand-Archive -Path "$env:TEMP\platform-tools.zip" -DestinationPath "$env:LOCALAPPDATA\Android" -Force
Esto deja adb.exe en %LOCALAPPDATA%\Android\platform-tools\adb.exe.

3. Conectar el cable y verificar

Conectá el celular por USB a la PC. En el teléfono va a aparecer un popup "¿Permitir depuración USB?" → aceptar (marcá "confiar siempre en esta PC" si querés).

$env:PATH += ";$env:LOCALAPPDATA\Android\platform-tools"
adb devices
Tiene que listar tu dispositivo (si dice "unauthorized", revisá el popup en el teléfono y aceptalo).

4. Redirigir los puertos

Luego de levantar expo y tener abierto expo en todos los dispositivos y confirmar que windows los ve a todos con adb:

 $adb = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
  & $adb devices | Where-Object { $_ -match '^\S+\s+device$' } | ForEach-Object {
      $s = ($_ -split '\s+')[0]
      foreach ($p in 8081, 8080, 5080) { & $adb -s $s reverse "tcp:$p" "tcp:$p" }
      Write-Host "reverse listo en $s"
  }?


5. Levantar app movil (expo)

cd mobile
./run-local.ps1
