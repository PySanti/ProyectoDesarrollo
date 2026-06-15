## Levantamiento de infraestructura

Desde la raiz del repo:

```powershell
docker compose -f "infra/docker-compose.yml" up -d postgres rabbitmq keycloak
```

Verificar contenedores:

```powershell
docker compose -f "infra/docker-compose.yml" ps
```

Abrir RabbitMQ Management:

```txt
http://localhost:15672
```

Credenciales por defecto de la imagen RabbitMQ si no se configuran otras:

```txt
usuario: guest
password: guest
```

## Levantamiento de microservicios


* En la raiz del proyecto


### Linux

```cmd

# Terminal 1
cd ./services/identity-service/
./run-local.sh


# Terminal 2
cd ./services/team-service/
./run-local.sh

# Terminal 3
cd ./services/bdt-game-service/
./run-local.sh


# Terminal 4
cd ./services/trivia-game-service/
./run-local.sh

# Terminal 5
cd ./frontend/
npm run dev

# Terminal 6
cd ./mobile/
npm start
```

> Nota: `identity-service` necesita SMTP configurado para enviar el correo de bienvenida con la
> contraseña temporal al crear usuarios. Ver la sección "Correo de bienvenida (SMTP)" más abajo.

### Powershell

```cmd

# Terminal 1
cd ./services/identity-service/
./run-local.ps1


# Terminal 2
cd ./services/team-service/
./run-local.ps1

# Terminal 3
cd ./services/bdt-game-service/
./run-local.ps1


# Terminal 4
cd ./services/trivia-game-service/
./run-local.ps1

# Terminal 5
cd ./frontend/
npm run dev

# Terminal 6
cd ./mobile/
npm start
```

## Correo de bienvenida (SMTP)

Al crear un usuario (cualquier rol), `identity-service` le envía un correo con su **contraseña
temporal** y los estilos de la plataforma. El envío es síncrono: si el correo no se puede entregar,
la creación falla con `502` y no queda usuario creado (se compensa Keycloak + BD).

Configura estas variables en `services/identity-service/.env` (ver `.env.example`). Ejemplo con
Gmail + app password (requiere 2FA; crea el app password en
https://myaccount.google.com/apppasswords):

```
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USE_STARTTLS=true
SMTP_USERNAME=tu-cuenta@gmail.com
SMTP_PASSWORD=tu-app-password-de-16-caracteres
SMTP_FROM_ADDRESS=tu-cuenta@gmail.com
SMTP_FROM_NAME=UMBRAL
```
