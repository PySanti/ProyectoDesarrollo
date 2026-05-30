# Adaptación del enunciado académico

## Propósito

Este documento explica cómo el SRS de UMBRAL adapta el enunciado académico original sin perder los objetivos técnicos del proyecto integrador.

## Enunciado original

El enunciado describe una plataforma web para experiencias de investigación inmersiva con:

- misiones;
- sesiones en vivo;
- equipos;
- pistas;
- respuestas/evidencias;
- ranking;
- trazabilidad;
- WebSockets;
- RabbitMQ;
- CQRS;
- PostgreSQL;
- .NET Core;
- React;
- arquitectura limpia/hexagonal.

## Adaptación del equipo

El equipo concreta el dominio en dos modos de juego:

1. Trivia.
2. Búsqueda del Tesoro / BDT.

Esta adaptación conserva la esencia del enunciado:

| Enunciado académico | SRS UMBRAL |
|---|---|
| Misión | FormularioTrivia / Configuración BDT |
| Etapas o nodos | Preguntas de Trivia / Etapas BDT |
| Sesión en vivo | PartidaTrivia / PartidaBDT |
| Equipo participante | Equipo |
| Evidencia o respuesta | RespuestaTrivia / TesoroQR |
| Pista | Pista BDT |
| Ranking | Ranking Trivia / Ranking BDT |
| Historial | RegistroAuditoria / EventoHistorial |
| Panel operador | Web React |
| Panel equipo | Mobile React Native |

## Aplicación móvil

El SRS incorpora una app móvil React Native para participantes.

Esta decisión se justifica porque:

- los flujos de participación requieren interacción inmediata;
- BDT requiere cámara/imagen;
- BDT requiere geolocalización operativa;
- Trivia requiere experiencia sincronizada de respuesta;
- la web se conserva para administración y operación.

## Geolocalización BDT

La geolocalización se limita a supervisión operativa durante BDT iniciada.

No incluye:

- rutas históricas complejas;
- analítica geoespacial avanzada;
- mapas predictivos;
- geofencing complejo;
- inteligencia artificial.

## Regla para OpenCode

OpenCode debe tratar esta adaptación como una decisión aceptada del proyecto.

No debe intentar volver al modelo genérico de misiones/sesiones si el SRS, modelo de dominio y diagrama de clases ya definen Trivia y BDT como modos concretos.
