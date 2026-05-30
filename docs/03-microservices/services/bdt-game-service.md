# BDT Game Service

> **Regla de generación:** este contenido fue generado exclusivamente a partir de los archivos `diagrama de clases(2).md`, `enunciado-proyecto(1).md`, `historias de usuario(2).md`, `microservicios(2).md`, `modelo de dominio(2).md` y `srs(2).md`.
>
> **No se agregan microservicios, endpoints, colas, eventos, bases de datos, rutas HTTP ni contratos que no estén indicados explícitamente en esas fuentes.** Cuando una responsabilidad aparece en el SRS/modelo pero no está asignada a un microservicio en `microservicios(2).md`, queda marcada como **no asignada / pendiente de decisión**.


## Identificación

| Campo | Valor |
|---|---|
| Nombre | BDT Game Service |
| Nombre en fuente | Microservicio de Búsqueda del Tesoro |
| Contexto DDD | BDT Context |
| Tipo de subdominio | Core |
| Historias asignadas | HU-12, HU-14, HU-34, HU-37, HU-39, HU-40, HU-42, HU-43, HU-44, HU-45, HU-46, HU-47, HU-49 |
| Persistencia indicada | Configuración de rutas, hitos físicos esperados y progreso del jugador/equipo activo |

## Responsabilidad explícita

El BDT Game Service es responsable de:

- gestión de etapas de búsqueda;
- recepción de strings decodificadas de QR enviadas desde celulares;
- validación textual contra el hito esperado;
- avance de fase;
- envío de pistas de texto del operador;
- progreso del jugador/equipo activo.

## Reglas de negocio relacionadas

| Regla | Contenido |
|---|---|
| RF-25 | Crear partidas BDT con nombre, área de búsqueda, modalidad y límites. |
| RF-26 | Configurar etapas con QR esperado y tiempo límite. |
| RF-27 | Publicar lobby e iniciar BDT si se cumplen condiciones mínimas. |
| RF-28 | Mostrar etapa activa, temporizador y opción de subir tesoro. |
| RF-29 | Procesar imagen, decodificar QR y comparar con QR esperado. |
| RF-30 | Registrar cada tesoro subido con participante/equipo, partida, etapa, fecha, contenido decodificado y resultado. |
| RF-31 | Cerrar etapa por QR válido o por agotamiento del tiempo. |
| RF-32 | Avanzar etapa o terminar partida al cerrar última etapa. |
| RF-33 | Enviar pistas y registrar cada pista en historial. |
| RF-34 | Solicitar autorización de ubicación y mostrar geolocalización cada 2 segundos en BDT iniciada. |

## Modelo de dominio asociado

| Elemento | Tipo |
|---|---|
| `PartidaBDT` | Agregado raíz |
| `EtapaBDT` | Entidad hija |
| `Bdt.Participante` | Entidad hija / explorador activo |
| `TesoroQR` | Entidad hija |
| `Pista` | Entidad hija |
| `AreaBusqueda` | Value Object |
| `UbicacionGeografica` | Value Object |
| `CodigoQREsperado` | Value Object |
| `PuntajeEtapa` | Value Object |
| `EstadoEtapa` | Enum |
| `ResultadoValidacionQR` | Enum |

## Historias asignadas por `microservicios(2).md`

| HU | Descripción |
|---|---|
| HU-12 | Filtrar partidas de BDT por modalidad. |
| HU-14 | Advertencia al entrar a BDT por equipo sin ser líder. |
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

## Eventos nombrados relacionados

| Evento | Estado |
|---|---|
| `HitoBDTEncontrado` | Nombrado en modelo. |
| `PuntajeBDTIncrementado` | Nombrado en modelo. |
| `PartidaBDTFinalizada` | Nombrado en modelo. |

## Comunicación en tiempo real relacionada

El SRS exige actualización en tiempo real para:

- etapas;
- temporizadores;
- pistas;
- geolocalización;
- resultados;
- ranking;
- estado de partida.

No se especifican nombres de canales, hubs o payloads.

## Dependencias conceptuales

| Dependencia | Motivo | Estado técnico |
|---|---|---|
| Team Service | HU-14 y HU-40 requieren saber si el participante es líder de equipo. | Contrato no especificado. |
| Identity / Keycloak | El usuario debe estar autenticado y tener rol/condición válida. | Mecanismo técnico no especificado. |
| Historial / Auditoría | RF-12, RF-33 y RF-37 exigen registrar pistas, QR, validaciones, ubicaciones, puntajes y resultados. | Ownership no asignado como microservicio. |

## No responsabilidades

BDT Game Service no debe asumir ownership de:

- formularios de Trivia;
- preguntas de Trivia;
- respuestas de Trivia;
- códigos de acceso de equipo;
- miembros de equipo como estructura social global;
- roles base de usuario.

## Pendientes antes de implementar

- Definir cómo se decodifica técnicamente el QR desde imagen; el SRS exige la capacidad pero no especifica librería ni componente.
- Definir persistencia de geolocalización operativa sin almacenar trayectorias históricas complejas.
- Definir ownership de inscripción y convocatoria para HU-40/HU-41 si se implementan.
- Definir contratos de tiempo real para pistas, etapas, temporizadores y ubicación.
- Definir cómo se registra historial BDT si no hay microservicio de auditoría explícito.
