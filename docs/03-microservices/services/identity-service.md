# Identity Service

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Identificación

| Campo | Valor |
|---|---|
| Nombre | Identity Service |
| Nombre en fuente | Microservicio de Identidad y Accesos |
| Contexto DDD | Identity Context |
| Tipo de subdominio | Genérico |
| Historias asignadas | HU-01, HU-02 |
| Persistencia indicada | Tabla única de usuarios y credenciales |

## Responsabilidad explícita

El Identity Service es responsable de:

- registro;
- autenticación;
- generación de tokens JWT;
- control de roles base:
  - Administrador;
  - Operador;
  - Participante.

## Reglas de negocio relacionadas

| Regla | Contenido |
|---|---|
| RB-U01 | La autenticación de usuarios será gestionada por Keycloak. |
| RB-U02 | Los roles base del sistema serán administrados mediante Keycloak: administrador, operador y participante. |
| RB-U03 | UMBRAL no almacenará contraseñas ni credenciales sensibles de usuarios en su base de datos. |
| RB-U04 | UMBRAL almacenará una referencia local al usuario autenticado mediante el identificador proveniente de Keycloak. |
| RB-U05 | El administrador podrá crear usuarios desde UMBRAL mediante integración con Keycloak. |
| RB-U06 | El administrador deberá asignar un rol inicial al usuario durante su creación. |
| RB-U07 | Desde UMBRAL no se permitirá modificar el rol de un usuario después de su creación. |
| RB-U08 | El administrador podrá consultar, editar datos generales y desactivar usuarios vinculados a Keycloak. |
| RB-U09 | Un usuario desactivado no podrá acceder a partidas ni ejecutar acciones dentro del sistema. |
| RB-U10 | El liderazgo de equipo no constituye un rol de Keycloak, sino una condición de negocio administrada dentro de UMBRAL. |

## Modelo de dominio asociado

| Elemento | Tipo |
|---|---|
| `Usuario` | Entidad / agregado raíz |
| `RolUsuario` | Enum |
| `EstadoUsuario` | Enum |
| `KeycloakId` | Value Object |

## Historias

| HU | Descripción |
|---|---|
| HU-01 | Crear usuarios y asignar rol inicial. |
| HU-02 | Consultar usuarios, editar datos generales y desactivar usuarios. |

## No responsabilidades

Identity Service no debe asumir ownership de:

- equipos;
- liderazgo de equipo como rol de Keycloak;
- partidas;
- formularios de Trivia;
- etapas BDT;
- puntajes;
- ranking;
- historial de partidas;
- QR;
- pistas.

## Dependencias externas indicadas

| Dependencia | Estado |
|---|---|
| Keycloak | Especificada por SRS. |
| PostgreSQL / EF Core | Especificada por SRS y persistencia del servicio. |
| RabbitMQ | No se define uso específico para este servicio en `microservicios(2).md`. |
| WebSockets | No se define uso específico para este servicio en `microservicios(2).md`. |

## Pendientes antes de implementar

- Definir contratos concretos para HU-01 y HU-02.
- Definir cómo se integra técnicamente con Keycloak.
- Definir qué metadata local se almacena exactamente.
