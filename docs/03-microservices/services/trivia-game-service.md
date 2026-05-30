# Trivia Game Service

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Identificación

| Campo | Valor |
|---|---|
| Nombre | Trivia Game Service |
| Nombre en fuente | Microservicio Motor de Trivia |
| Contexto DDD | Trivia Context |
| Tipo de subdominio | Core |
| Historias asignadas | HU-11, HU-13, HU-15, HU-17, HU-18, HU-19, HU-21, HU-22, HU-23, HU-24, HU-26, HU-27, HU-28, HU-29, HU-30, HU-35 |
| Persistencia indicada | Formularios, estado transitorio de partidas activas y resultados |

## Responsabilidad explícita

El Trivia Game Service es responsable de:

- ciclo de vida de la trivia;
- temporizadores de rondas;
- procesamiento de respuestas;
- respuestas individuales;
- primera respuesta del equipo;
- acumulación de puntajes;
- formularios de Trivia;
- estado de partidas activas;
- resultados de Trivia.

## Reglas de negocio relacionadas

| Regla | Contenido |
|---|---|
| RF-15 | Crear, editar y consultar formularios de Trivia con preguntas, opciones, respuesta correcta, puntaje asignado y tiempo límite. |
| RF-16 | Validar que el formulario esté completo antes de usarlo en una partida. |
| RF-17 | Crear partidas de Trivia asociadas a un formulario válido. |
| RF-18 | Publicar lobby e iniciar partida manual o automáticamente. |
| RF-19 | Mostrar la misma pregunta y opciones a todos al mismo tiempo. |
| RF-20 | Aceptar una respuesta por jugador en individual y una por equipo en modalidad equipos. |
| RF-21 | Rechazar respuestas repetidas, tardías o fuera de estado válido. |
| RF-22 | Cerrar preguntas, avanzar, actualizar ranking y calcular puntaje según fórmula del SRS. |
| RF-23 | Durante una Trivia iniciada, el operador solo ve ranking y opción de cancelar. |
| RF-24 | Consultar historial de partidas de Trivia. |

## Modelo de dominio asociado

| Elemento | Tipo |
|---|---|
| `FormularioTrivia` | Agregado raíz |
| `Pregunta` | Entidad hija |
| `Opcion` | Value Object |
| `PuntajeAsignado` | Value Object |
| `TiempoLimite` | Value Object |
| `PartidaTrivia` | Agregado raíz |
| `Trivias.Participante` | Entidad hija / competidor activo |
| `RespuestaTrivia` | Entidad hija |
| `PartidaId` | Value Object |
| `Modalidad` | Enum |
| `EstadoPartida` | Enum |

## Historias asignadas por `microservicios(2).md`

| HU | Descripción |
|---|---|
| HU-11 | Filtrar partidas de Trivia por modalidad. |
| HU-13 | Advertencia al entrar a Trivia por equipo sin ser líder. |
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

## Eventos nombrados relacionados

| Evento | Estado |
|---|---|
| `RespuestaTriviaValidada` | Nombrado en modelo. |
| `PuntajeTriviaIncrementado` | Nombrado en modelo. |
| `PartidaTriviaFinalizada` | Nombrado en modelo. |

## Comunicación en tiempo real relacionada

El SRS exige actualización en tiempo real para preguntas, ranking, temporizadores, lobby, estados y resultados.

No se especifican nombres de canales, hubs o payloads.

## Dependencias conceptuales

| Dependencia | Motivo | Estado técnico |
|---|---|---|
| Team Service | HU-13 y HU-19 requieren saber si el participante es líder de equipo. | Contrato no especificado. |
| Identity / Keycloak | El usuario debe estar autenticado y tener rol/condición válida. | Mecanismo técnico no especificado. |
| Historial / Auditoría | RF-12 y RF-37 exigen trazabilidad de respuestas, puntajes, cancelaciones y resultados. | Ownership no asignado como microservicio. |

## No responsabilidades

Trivia Game Service no debe asumir ownership de:

- equipos como estructura social global;
- códigos de acceso de equipo;
- partidas BDT;
- etapas BDT;
- QR de BDT;
- pistas de BDT;
- geolocalización BDT.

## Pendientes antes de implementar

- Resolver el conflicto entre fórmula de puntaje del SRS y acumulación directa indicada en modelo/diagrama antes de HU-29.
- Definir ownership de inscripción y convocatoria para HU-19/HU-20 si se implementan.
- Definir contratos de tiempo real para preguntas, resultados y ranking.
- Definir cómo se registra historial de Trivia si no hay microservicio de auditoría explícito.
