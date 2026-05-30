# Project Brief — UMBRAL

## Identidad del proyecto

**UMBRAL** es una plataforma web para operar experiencias interactivas en tiempo real bajo dos modos de juego:

1. **Trivia**
2. **Búsqueda del Tesoro** o **BDT**

No se deben crear, modelar ni implementar modos adicionales.

## Problema que resuelve

La organización actualmente coordina experiencias inmersivas mediante procesos manuales y dispersos: hojas de cálculo, mensajería, decisiones manuales del operador y seguimiento no centralizado. Esto genera retrasos, inconsistencias en puntajes, falta de trazabilidad y una experiencia poco uniforme para operadores y participantes.

UMBRAL centraliza la creación, publicación, inscripción, ejecución, validación, puntuación, ranking, geolocalización operativa y trazabilidad de partidas.

## Objetivo del sistema

Centralizar y controlar partidas interactivas en tiempo real, permitiendo:

- creación de partidas de Trivia y BDT;
- gestión de equipos;
- inscripción individual o por equipos;
- operación de lobbies;
- ejecución sincronizada;
- validación de respuestas o tesoros QR;
- cálculo y acumulación de puntajes;
- ranking en tiempo real;
- historial de eventos;
- comunicación en tiempo real;
- publicación de eventos asíncronos.

## Actores

| Actor | Responsabilidad |
|---|---|
| Administrador | Gestiona usuarios, roles iniciales, datos generales, desactivación de usuarios y gestión administrativa de equipos. |
| Operador | Crea formularios, partidas, etapas, lobbies; inicia/cancela partidas; supervisa ranking, participantes, tesoros, pistas e historial. |
| Participante | Crea o se une a equipos, participa individualmente o como líder/miembro, responde Trivia, sube QR en BDT, recibe actualizaciones. |
| Líder de equipo | Condición de negocio de un participante dentro de un equipo. Puede inscribir al equipo en partidas por equipo. |
| Sistema | Ejecuta validaciones automáticas, publicación de eventos, actualización de ranking y comunicación en tiempo real. |

## Modos de juego

### Trivia

Modo síncrono basado en formularios de preguntas. Una partida de Trivia se crea a partir de un formulario válido, se publica en lobby, acepta participantes/equipos y ejecuta preguntas sincronizadas con temporizador.

### Búsqueda del Tesoro

Modo basado en etapas. Cada etapa tiene un QR esperado y un temporizador. El participante o equipo sube una imagen del QR; el sistema intenta decodificarla y validar el contenido contra el QR esperado de la etapa activa.

## Arquitectura obligatoria

- Frontend: React.
- Backend: .NET Core.
- Persistencia: PostgreSQL.
- ORM: Entity Framework Core.
- Casos de uso: CQRS + MediatR.
- Tiempo real: WebSockets / SignalR.
- Mensajería asíncrona: RabbitMQ.
- Autenticación y autorización base: Keycloak.
- Ejecución local: Docker Compose.
- Arquitectura: Hexagonal / Clean Architecture.
- Pruebas: unitarias, integración y E2E cuando aplique.

## Principios obligatorios

- El dominio no depende de infraestructura.
- Los controladores no contienen reglas de negocio.
- Los comandos modifican estado.
- Las consultas no modifican estado.
- Las reglas de negocio viven en agregados, servicios de dominio o handlers de aplicación según corresponda.
- Los eventos relevantes deben publicarse para auditoría, ranking, historial, trazabilidad y tiempo real.
- Las acciones de usuario deben respetar rol, liderazgo, estado de partida, modalidad y cupos.
