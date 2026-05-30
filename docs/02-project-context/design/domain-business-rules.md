# Domain Business Rules by Aggregate — UMBRAL

## Identity / Usuario

| Regla | Ubicación sugerida |
|---|---|
| No almacenar contraseñas | Infrastructure/Auth Adapter + política de persistencia. |
| Guardar referencia Keycloak | `Usuario` / `KeycloakId`. |
| Rol inicial no modificable desde UMBRAL | Application handler de creación / actualización. |
| Usuario desactivado no opera | Guards de Application + autorización. |

## Team / Equipo

| Regla | Ubicación sugerida |
|---|---|
| Crear equipo solo si usuario no pertenece a otro | `CrearEquipoCommandHandler` consulta repositorio antes de crear. |
| Creador queda líder | `Equipo.Crear(...)`. |
| Código único generado | `CodigoAcceso.Generar()` + validación de unicidad en repositorio. |
| Máximo 5 integrantes | `Equipo.AgregarParticipante(...)`. |
| Un participante no puede pertenecer a dos equipos | Application handler + repositorio. |
| Transferencia obligatoria si líder sale con miembros | `Equipo.RemoverParticipante(...)` o método específico. |
| Si líder está solo y sale, equipo se elimina | `Equipo.Eliminar()` coordinado por handler. |
| Equipo desactivado no se inscribe | Validación desde servicios dueños de partidas usando Team contract. |

## Trivia / FormularioTrivia

| Regla | Ubicación sugerida |
|---|---|
| Solo operador crea formulario | Autorización + handler. |
| Formulario debe tener preguntas | `FormularioTrivia.ValidarFormulario()`. |
| Pregunta debe tener opciones | `Pregunta.EsValida()`. |
| Pregunta debe tener respuesta correcta | `Pregunta.EsValida()`. |
| Pregunta debe tener puntaje y tiempo | `Pregunta.EsValida()`. |
| Formulario incompleto no crea partida | `CrearPartidaTriviaCommandHandler`. |

## Trivia / PartidaTrivia

| Regla | Ubicación sugerida |
|---|---|
| Partida usa formulario válido | `PartidaTrivia.Crear(...)` o handler. |
| Solo estados válidos | `PartidaTrivia.PublicarLobby()`, `IniciarPartida()`, `CancelarPartida()`, `FinalizarPartida()`. |
| Inscripción solo en lobby | `PartidaTrivia.RegistrarInscripcion(...)`. |
| Respuesta solo en partida iniciada y pregunta activa | `PartidaTrivia.RegistrarRespuestaDefinitiva(...)`. |
| Una respuesta por jugador/equipo | `PartidaTrivia.RegistrarRespuestaDefinitiva(...)`. |
| Primera respuesta de equipo es definitiva | `PartidaTrivia.RegistrarRespuestaDefinitiva(...)`. |
| Rechazar respuesta tardía/repetida/fuera de estado | `PartidaTrivia.RegistrarRespuestaDefinitiva(...)`. |
| Ranking actualizado al cerrar pregunta | Handler + domain service `ClasificadorRankingService`. |
| Cancelación bloquea nuevas acciones | Métodos del agregado + guards. |

## BDT / PartidaBDT

| Regla | Ubicación sugerida |
|---|---|
| Solo operador crea BDT | Autorización + handler. |
| BDT debe tener etapas válidas | `PartidaBDT.ValidarConfiguracion()` o `Crear(...)`. |
| Etapa debe tener QR esperado y tiempo | `EtapaBDT.EsValida()`. |
| Iniciar BDT activa primera etapa | `PartidaBDT.IniciarPartida()`. |
| Subida QR solo en iniciada y etapa activa | `PartidaBDT.ValidarHito(...)`. |
| Validación compara contra QR esperado | `CodigoQREsperado.CoincideCon(...)`. |
| Cierre por QR válido o tiempo agotado | `EtapaBDT.Resolver()` / `Cerrar()`. |
| Avance a siguiente etapa o finalización | `PartidaBDT.AvanzarEtapa()`. |
| Enviar pista solo durante BDT iniciada | `PartidaBDT.DespacharPista(...)`. |
| Ubicación solo con autorización | Application/UI + handler de ubicación. |

## Auditoría / RegistroAuditoria

| Regla | Ubicación sugerida |
|---|---|
| Cambios relevantes generan evento histórico | Event consumer / Audit application handler. |
| Eventos son inmutables para consulta | `RegistroAuditoria.AgregarEvento(...)`. |
| Puntajes deben tener trazabilidad de origen | Eventos de puntaje + historial. |

## Cross-service

| Regla | Ubicación sugerida |
|---|---|
| Servicios no acceden a base de datos ajena | Architecture rule / integration tests. |
| Comunicación directa para consultas puntuales | HTTP contracts. |
| Comunicación desacoplada para efectos secundarios | RabbitMQ events. |
| Tiempo real visible al usuario | SignalR/WebSocket hubs del servicio dueño o gateway. |
