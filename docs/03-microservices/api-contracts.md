# API Contracts

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Estado del project-source

El contexto anexado no define contratos HTTP concretos.

No se especifican:

- rutas base;
- endpoints;
- métodos HTTP;
- payloads de request;
- payloads de response;
- códigos de error;
- formatos de paginación;
- autenticación por endpoint;
- versionado de API.

Por lo tanto, este archivo no inventa endpoints.

## Regla para crear contratos HTTP

Cada contrato HTTP debe ser creado durante el SDD de una HU concreta, cuando el `spec.md` y el `design.md` hayan definido:

- historia de usuario;
- microservicio dueño;
- acción del usuario;
- datos de entrada;
- datos de salida;
- validaciones;
- errores;
- rol autorizado;
- cambio de estado o consulta;
- efectos en tiempo real o eventos.

## Capacidades HTTP por microservicio según historias asignadas

Estas son capacidades funcionales derivadas de las HUs asignadas por `microservicios(2).md`. No son endpoints.

### Identity Service

| HU | Capacidad |
|---|---|
| HU-01 | Crear usuarios y asignar rol inicial. |
| HU-02 | Consultar usuarios, editar datos generales y desactivar usuarios. |

### Team Service

| HU | Capacidad |
|---|---|
| HU-03 | Crear equipo. |
| HU-04 | Unirse a equipo usando código. |
| HU-05 | Eliminar equipo creado. |
| HU-06 | Transferir liderazgo antes de salir del equipo. |
| HU-07 | Salir del equipo. |

### Trivia Game Service

| HU | Capacidad |
|---|---|
| HU-11 | Filtrar partidas de Trivia por modalidad. |
| HU-13 | Mostrar advertencia al intentar entrar a Trivia por equipo sin ser líder. |
| HU-15 | Crear formularios de Trivia. |
| HU-17 | Crear y publicar partida de Trivia. |
| HU-18 | Unirse a Trivia individual. |
| HU-19 | Unir equipo a Trivia por equipos. |
| HU-21 | Ver pantalla de espera de Trivia. |
| HU-22 | Ver participantes unidos a Trivia publicada. |
| HU-23 | Ver equipos unidos a Trivia publicada. |
| HU-24 | Iniciar manualmente Trivia. |
| HU-26 | Responder Trivia individual. |
| HU-27 | Responder Trivia por equipo. |
| HU-28 | Ver resultado al cerrar pregunta de Trivia. |
| HU-29 | Calcular puntaje de respuesta en Trivia. |
| HU-30 | Ver ranking durante Trivia. |
| HU-35 | Ver lista de partidas de Trivia publicadas. |

### BDT Game Service

| HU | Capacidad |
|---|---|
| HU-12 | Filtrar partidas de BDT por modalidad. |
| HU-14 | Mostrar advertencia al intentar entrar a BDT por equipo sin ser líder. |
| HU-34 | Crear partida de Búsqueda del Tesoro. |
| HU-37 | Ver lista de partidas de BDT publicadas. |
| HU-39 | Unirse a BDT individual. |
| HU-40 | Unir equipo a BDT por equipos. |
| HU-42 | Ver participantes unidos a BDT publicada. |
| HU-43 | Iniciar partida BDT. |
| HU-44 | Ver etapa activa y opción de subir tesoro. |
| HU-45 | Subir foto del tesoro QR. |
| HU-46 | Validar automáticamente QR enviado. |
| HU-47 | Cerrar etapa BDT. |
| HU-49 | Enviar pistas a participantes durante BDT. |

## Plantilla obligatoria para futuros contratos

Cuando una HU requiera contrato HTTP, documentarlo en `contracts/http/<service>.md` con este formato:

```md
## <Nombre de la capacidad>

| Campo | Valor |
|---|---|
| HU | HU-XX |
| Microservicio dueño | <service> |
| Tipo | Command / Query |
| Método HTTP | Pendiente de definir en SDD |
| Ruta | Pendiente de definir en SDD |
| Rol autorizado | Administrador / Operador / Participante |
| Cambia estado | Sí / No |
| Publica evento | Sí / No |
| Actualiza tiempo real | Sí / No |

### Request

Pendiente de definir en SDD.

### Response

Pendiente de definir en SDD.

### Errores de negocio

Pendiente de definir en SDD.
```
