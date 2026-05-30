# Glossary — UMBRAL

## Actores

| Término | Definición |
|---|---|
| Administrador | Usuario que gestiona usuarios, roles iniciales y equipos desde una perspectiva administrativa. |
| Operador | Usuario que configura, publica, inicia, cancela y supervisa partidas. |
| Participante | Usuario que juega partidas individuales o por equipo. |
| Líder de equipo | Participante con permisos de negocio sobre un equipo: inscribir equipo, transferir liderazgo, eliminar equipo si aplica. |
| Miembro de equipo | Participante que pertenece a un equipo, pero no necesariamente puede inscribirlo en partidas. |

## Conceptos generales

| Término | Definición |
|---|---|
| UMBRAL | Plataforma web para operar partidas interactivas en tiempo real. |
| Partida | Ejecución configurada de un modo de juego. Puede ser Trivia o Búsqueda del Tesoro. |
| Modalidad | Forma de participación: individual o por equipos. |
| Lobby | Estado publicado donde se permiten inscripciones antes de iniciar. |
| Estado de partida | Estado del ciclo de vida: `lobby`, `iniciada`, `cancelada`, `terminada`. |
| Inscripción | Solicitud o registro de un participante/equipo en una partida. |
| Convocatoria | Invitación enviada a miembros cuando el líder inscribe un equipo en una partida por equipos. |
| Ranking | Ordenamiento de participantes/equipos por puntaje y criterio de desempate. |
| Historial | Registro de eventos relevantes ocurridos en una partida. |
| Auditoría | Mecanismo de trazabilidad inmutable o semi-inmutable de acciones y eventos. |
| Tiempo real | Actualización inmediata a través de WebSockets/SignalR sin recargar la pantalla. |
| Evento de dominio | Hecho relevante ocurrido en el dominio. |
| Evento de integración | Mensaje publicado para que otros servicios reaccionen de forma desacoplada. |

## Equipos

| Término | Definición |
|---|---|
| Equipo | Grupo global de participantes reutilizable en Trivia y BDT. |
| Código de acceso | Código único generado para permitir unirse a un equipo. |
| EstadoEquipo | Estado operativo: activo, desactivado o eliminado. |
| Transferencia de liderazgo | Acción por la cual el líder designa a otro miembro antes de salir. |

## Trivia

| Término | Definición |
|---|---|
| FormularioTrivia | Plantilla de preguntas usada para crear una partida de Trivia. |
| Pregunta | Unidad de evaluación con texto, opciones, respuesta correcta, puntaje y tiempo. |
| Opción | Alternativa de respuesta; una o más opciones pueden modelarse, pero debe existir una correcta según SRS. |
| PuntajeAsignado | Valor configurado para una pregunta. |
| TiempoLimite | Tiempo disponible para responder una pregunta. |
| RespuestaTrivia | Respuesta enviada por participante/equipo ante pregunta activa. |
| Pregunta activa | Pregunta vigente de la partida, con temporizador abierto. |
| Primera respuesta de equipo | En Trivia por equipos, la primera respuesta enviada por cualquier integrante se toma como definitiva para el equipo. |

## Búsqueda del Tesoro

| Término | Definición |
|---|---|
| BDT | Búsqueda del Tesoro. Modo de juego basado en etapas y validación de QR. |
| PartidaBDT | Agregado que controla etapas, participantes, tesoros, pistas y avance. |
| EtapaBDT | Fase del juego con QR esperado, tiempo límite y puntaje. |
| TesoroQR | Envío de imagen/QR realizado por participante o equipo. |
| CodigoQREsperado | Código configurado para validar la etapa activa. |
| ResultadoValidacionQR | Resultado de procesar el QR: válido, inválido, no legible o no corresponde a la etapa activa. |
| Pista | Mensaje enviado por el operador a participantes/equipos durante BDT. |
| Área de búsqueda | Descripción textual del espacio donde se juega una BDT. |
| Ubicación geográfica | Latitud, longitud y fecha asociada a un participante durante BDT. |

## Arquitectura

| Término | Definición |
|---|---|
| Clean Architecture | Organización donde dominio y aplicación no dependen de infraestructura. |
| Arquitectura Hexagonal | Arquitectura basada en puertos y adaptadores. |
| CQRS | Separación entre comandos que modifican estado y queries que consultan. |
| MediatR | Librería para implementar mediador en .NET. |
| EF Core | ORM usado para persistencia en PostgreSQL. |
| RabbitMQ | Broker para mensajería asíncrona. |
| SignalR | Implementación .NET de comunicación en tiempo real sobre WebSockets. |
| Keycloak | Proveedor de identidad y autorización base. |
