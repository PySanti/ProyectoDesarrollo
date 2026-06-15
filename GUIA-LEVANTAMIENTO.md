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
