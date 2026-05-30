# SRS Summary — UMBRAL

## Alcance funcional general

UMBRAL debe permitir operar partidas interactivas en tiempo real bajo dos modos: Trivia y Búsqueda del Tesoro.

El sistema cubre:

- autenticación y roles mediante Keycloak;
- gestión de usuarios;
- gestión de equipos;
- publicación de partidas;
- inscripción individual o por equipo;
- convocatoria a integrantes;
- lobbies;
- ejecución de partidas;
- validación de respuestas de Trivia;
- validación de tesoros QR en BDT;
- cálculo y acumulación de puntaje;
- ranking en tiempo real;
- historial y auditoría;
- geolocalización operativa en BDT;
- consultas separadas de comandos.

## Requisitos funcionales por grupo

### Identidad y acceso

- Integración con Keycloak.
- Roles base: Administrador, Operador y Participante.
- El rol se asigna inicialmente durante creación.
- UMBRAL no almacena contraseñas.
- UMBRAL conserva referencia local al usuario autenticado.
- Los usuarios desactivados no deben operar dentro del sistema.
- El liderazgo no es rol de Keycloak; es una condición de negocio dentro de equipos.

### Modos de juego

- Solo existen Trivia y Búsqueda del Tesoro.
- Toda partida pertenece exactamente a uno de esos modos.
- No se permiten modos adicionales.
- Todas las partidas usan estados comunes: `lobby`, `iniciada`, `cancelada`, `terminada`.

### Equipos

- Un participante puede crear equipo solo si no pertenece a otro.
- El creador queda como líder.
- El sistema genera un código único de equipo.
- Un participante puede unirse mediante código válido.
- Un participante solo puede pertenecer a un equipo a la vez.
- Cada equipo puede tener máximo cinco jugadores.
- Los equipos son globales y pueden usarse tanto en Trivia como en BDT.
- El líder puede eliminar el equipo, con notificación a integrantes.
- El líder debe transferir liderazgo antes de salir si hay otros integrantes.
- Si el líder está solo y sale, el equipo se elimina.
- Equipos desactivados no pueden inscribirse en nuevas partidas.

### Listado, filtros y acceso

- Cada participante ve paneles separados para Trivia y BDT.
- Cada panel muestra partidas publicadas.
- Cada panel permite filtrar por modalidad individual o equipo.
- Un participante puede jugar partidas individuales aunque pertenezca a un equipo.
- Un participante no líder no puede inscribir un equipo en una partida por equipo.
- Mensaje obligatorio: “Debes ser líder de un equipo para entrar en este evento”.

### Trivia

- El operador crea formularios con preguntas, opciones, respuesta correcta, puntaje y tiempo límite.
- Un formulario incompleto no puede usarse para crear partida.
- El operador crea partida asociada a un formulario válido.
- La partida puede ser individual o por equipos.
- Se definen mínimos y máximos según modalidad.
- En lobby se habilitan inscripciones.
- La partida inicia manualmente o al cumplirse el tiempo configurado.
- Todos reciben la misma pregunta y opciones al mismo tiempo.
- En individual se acepta una respuesta por jugador por pregunta.
- En equipo se acepta una respuesta por equipo; la primera enviada por cualquier integrante fija la respuesta.
- Se rechazan respuestas repetidas, tardías o fuera del estado válido.
- Al cerrar pregunta se valida respuesta, se actualiza ranking y se avanza o finaliza.
- El operador ve ranking y opción de cancelar; no interviene respuestas.

### Búsqueda del Tesoro

- El operador crea BDT con nombre, área de búsqueda, modalidad y mínimos/máximos.
- La BDT debe tener una o más etapas válidas.
- Cada etapa tiene QR esperado y tiempo límite.
- La partida se publica en lobby.
- La partida inicia cuando cumple mínimos.
- Al iniciar se activa la primera etapa.
- El participante ve etapa activa, temporizador y opción de subir tesoro.
- El sistema procesa imagen, intenta decodificar QR y compara con QR esperado.
- Cada envío queda registrado con participante/equipo, partida, etapa, fecha, QR decodificado y resultado.
- La etapa se cierra por QR válido o por agotamiento de tiempo.
- Si hay ganador, se muestra quién encontró el tesoro y tiempo usado.
- Si no hay ganador, se muestra “nadie consiguió el tesoro”.
- El operador puede enviar pistas durante una partida iniciada.
- La geolocalización requiere autorización y se actualiza cada 2 segundos durante BDT iniciada.

### Historial, auditoría y tiempo real

- Se registran cambios de estado, inscripciones, convocatorias, respuestas, tesoros, validaciones, pistas, puntajes, ubicaciones, cancelaciones y resultados.
- El sistema debe publicar eventos relevantes para auditoría, historial, notificaciones internas, ranking, trazabilidad y tiempo real.
- El canal de tiempo real actualiza lobby, preguntas, ranking, etapas, pistas, geolocalización, resultados y estados.

## Requisitos no funcionales

| ID | Resumen |
|---|---|
| RNF-01 | Frontend React y backend .NET Core. |
| RNF-02 | PostgreSQL + Entity Framework Core. |
| RNF-03 | WebSockets para tiempo real. |
| RNF-04 | MediatR y CQRS. |
| RNF-05 | RabbitMQ para procesos asíncronos. |
| RNF-06 | Arquitectura hexagonal / limpia. |
| RNF-07 | Dominio independiente de infraestructura y framework web. |
| RNF-08 | Logging, excepciones y validaciones consistentes. |
| RNF-09 | Meta de cobertura backend mínima de 90%. |
| RNF-10 | Ejecución local con Docker Compose. |
| RNF-11 | CI para compilación y pruebas. |
| RNF-12 | Interfaz clara, usable y coherente. |
| RNF-13 | Keycloak con tokens seguros. |
| RNF-14 | No almacenar contraseñas ni credenciales sensibles. |
| RNF-15 | Geolocalización BDT cada 2 segundos sin bloquear. |
| RNF-16 | Decodificación de QR desde imágenes en web responsive. |
| RNF-17 | Tiempo real para lobby, preguntas, ranking, etapas, pistas, geolocalización y estados. |
