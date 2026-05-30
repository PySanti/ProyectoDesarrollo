# Service Model Impact — UMBRAL

Este archivo traduce el modelo de dominio y las historias de usuario a la topología física aceptada.

La topología vigente está definida en:

```txt
docs/05-decisions/ADR-0006-four-service-topology.md
```

## Regla principal

El modelo de dominio es global, pero la implementación usa cuatro microservicios físicos:

1. Identity Service
2. Team Service
3. Trivia Game Service
4. BDT Game Service

No se deben crear estos servicios físicos:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Reglas de implementación

- No implementar todas las clases UML en un solo backend.
- No crear una base de datos global compartida.
- No crear un DbContext global.
- No acceder directamente a la base de datos de otro servicio.
- Usar HTTP para consultas directas entre servicios cuando una acción lo requiera.
- Usar RabbitMQ para hechos asíncronos entre servicios cuando el SDD lo justifique.
- Usar SignalR/WebSockets para actualizaciones visibles en tiempo real.
- Resolver contratos en `contracts/` antes de implementar comunicación entre servicios.

## Impacto por servicio

### Identity Service

Implementa:

- usuarios;
- roles base;
- estado de usuario;
- referencia local a Keycloak;
- historial propio de cambios de usuario, si una HU lo requiere.

No implementa:

- equipos;
- partidas;
- puntajes de juego;
- rankings de juego;
- QR;
- pistas.

### Team Service

Implementa:

- equipos;
- miembros;
- códigos de acceso;
- liderazgo;
- estado del equipo;
- reglas de pertenencia;
- historial propio de equipo, si una HU lo requiere.

No implementa:

- partidas de Trivia;
- partidas BDT;
- respuestas;
- QR;
- pistas;
- ranking de partidas.

### Trivia Game Service

Implementa:

- formularios de Trivia;
- preguntas;
- opciones;
- partidas de Trivia;
- lobby de Trivia;
- inscripciones y convocatorias de Trivia;
- respuestas de Trivia;
- puntaje de Trivia;
- ranking de Trivia;
- historial de Trivia;
- actualizaciones en tiempo real de Trivia.

No implementa:

- equipos como dato maestro;
- reglas internas de membresía;
- BDT;
- QR BDT;
- pistas BDT;
- geolocalización BDT.

### BDT Game Service

Implementa:

- partidas BDT;
- área de búsqueda;
- etapas;
- QR esperado;
- tesoros / evidencias QR;
- validación de QR;
- pistas;
- geolocalización;
- progreso BDT;
- puntaje BDT;
- ranking BDT;
- historial BDT;
- actualizaciones en tiempo real BDT.

No implementa:

- equipos como dato maestro;
- formularios de Trivia;
- preguntas de Trivia;
- respuestas de Trivia.

### Gateway

El gateway no es un microservicio de dominio. Si existe en la implementación, solo enruta, compone o reenvía requests. No posee reglas de negocio ni persistencia de dominio.

## Responsabilidades que antes podían parecer servicios

| Responsabilidad | Ubicación vigente |
|---|---|
| Scoring / puntaje Trivia | Trivia Game Service |
| Ranking Trivia | Trivia Game Service |
| Historial Trivia | Trivia Game Service |
| Scoring / puntaje BDT | BDT Game Service |
| Ranking BDT | BDT Game Service |
| Historial BDT | BDT Game Service |
| Historial de equipo | Team Service |
| Historial de usuario | Identity Service |
