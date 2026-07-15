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

- `down -v` elimina los volúmenes `umbral-postgres-data` y `umbral-keycloak-data`.
- Al levantar se recrean las 4 bases, cada servicio aplica su esquema, el realm
  `UMBRAL-UCAB` se re-importa (usuarios seed `admin`/`operador`/`participante`) y
  RabbitMQ vuelve a declarar sus colas.
- Se pierden todos los usuarios, equipos y partidas creados durante las pruebas.

## Móvil (Expo, fuera de compose)

Con el stack de compose arriba y `LAN_IP` correcta en el `.env` raíz:

```bash
cd mobile && ./run-local.sh
```

El script regenera `mobile/.env` con valores literales desde el `.env` raíz (no edites
`mobile/.env` a mano) y lanza `expo start --host lan`. Escanea el QR con Expo Go desde el
teléfono (misma Wi-Fi).
