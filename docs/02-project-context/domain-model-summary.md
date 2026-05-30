# Domain Model Summary — UMBRAL

## Actores

- Administrador.
- Operador.
- Participante.
- Líder de equipo.
- Miembro de equipo.
- Sistema.

## Conceptos principales

- Usuario.
- Equipo.
- Código de acceso.
- FormularioTrivia.
- Pregunta.
- Opción.
- Partida.
- PartidaTrivia.
- PartidaBDT.
- EtapaBDT.
- TesoroQR.
- UbicaciónGeografica.
- Pista.
- PuntajeAcumulado.
- TiempoRespuestaAcumulado.
- Ranking.
- RegistroAuditoria.
- EventoHistorial.
- InscripcionPartida.
- Convocatoria.

## Acciones principales del dominio

- RegistrarUsuario.
- CrearEquipo.
- UnirseAEquipo.
- AbandonarEquipo.
- TransferirLiderazgo.
- EliminarEquipo.
- CrearFormulario.
- CrearPartidaTrivia.
- CrearPartidaBDT.
- PublicarLobby.
- SolicitarInscripcionIndividual.
- SolicitarInscripcionEquipo.
- ResponderConvocatoria.
- IniciarPartida.
- RegistrarRespuestaDefinitiva.
- ValidarEnvioQR.
- DespacharPista.
- CancelarPartida.
- FinalizarPartida.

## Subdominios

| Tipo | Subdominio | Descripción |
|---|---|---|
| Core | Trivia | Evaluación síncrona de respuestas, temporizadores, puntaje y ranking. |
| Core | Búsqueda del Tesoro | Validación de QR, etapas, pistas, geolocalización y avance. |
| Soporte | Gestión de Equipos | Agrupación de participantes, códigos y liderazgo. |
| Soporte | Auditoría | Historial de eventos y trazabilidad. |
| Genérico | IAM | Autenticación, autorización y roles base vía Keycloak. |

## Contextos acotados

| Contexto | Namespace sugerido | Responsabilidad |
|---|---|---|
| Identity Context | `Umbral.Identity.Domain` | Usuarios, roles, estado y referencia Keycloak. |
| Team Context | `Umbral.Equipos.Domain` | Equipos, participantes de equipo, liderazgo y código de acceso. |
| Trivia Context | `Umbral.Trivias.Domain` | Formularios, preguntas, partidas, respuestas, participantes activos y ranking de Trivia. |
| BDT Context | `Umbral.Bdt.Domain` | Partidas BDT, etapas, QR, tesoros, pistas, ubicación y progreso. |
| Auditing Context | `Umbral.Auditoria.Domain` | Registro de auditoría y eventos históricos. |

## Nota sobre `Participante`

El nombre `Participante` aparece en varios contextos, pero no representa la misma cosa:

| Contexto | Significado |
|---|---|
| Equipos | Miembro de un equipo con fecha de unión y bandera `EsLider`. |
| Trivia | Competidor activo que puede representar un usuario o equipo y acumula puntaje/tiempo. |
| BDT | Explorador activo que puede representar un usuario o equipo y acumula puntaje/ubicación. |

Se debe evitar compartir la misma clase física entre estos contextos.

## Agregados principales

### Identity

| Agregado | Entidades / VO | Invariantes |
|---|---|---|
| Usuario | RolUsuario, EstadoUsuario, KeycloakId | No almacena contraseña; referencia a Keycloak; rol inicial no modificable desde UMBRAL. |

### Equipos

| Agregado | Entidades / VO | Invariantes |
|---|---|---|
| Equipo | Participante, EquipoId, NombreEquipo, CodigoAcceso, EstadoEquipo | Máximo 5 integrantes; un líder; código único; usuario no pertenece a más de un equipo. |

### Trivia

| Agregado | Entidades / VO | Invariantes |
|---|---|---|
| FormularioTrivia | Pregunta, Opcion, PuntajeAsignado, TiempoLimite | Debe tener preguntas completas antes de usarse. |
| PartidaTrivia | Participante, RespuestaTrivia, PartidaId, Modalidad, EstadoPartida | Solo formulario válido; una respuesta por jugador/equipo; primera respuesta del equipo es definitiva. |

### BDT

| Agregado | Entidades / VO | Invariantes |
|---|---|---|
| PartidaBDT | EtapaBDT, Participante, TesoroQR, Pista, AreaBusqueda, UbicacionGeografica, CodigoQREsperado, PuntajeEtapa | Al menos una etapa válida; QR esperado y tiempo por etapa; validación contra etapa activa. |

### Auditoría

| Agregado | Entidades / VO | Invariantes |
|---|---|---|
| RegistroAuditoria | EventoHistorial, TipoEventoHistorial | Eventos relevantes quedan asociados a partida y trazables. |

## Eventos de dominio identificados

| Evento | Origen | Uso |
|---|---|---|
| RespuestaTriviaValidada | Trivia | Auditoría, ranking, tiempo real. |
| PuntajeTriviaIncrementado | Trivia | Ranking, auditoría, trazabilidad. |
| HitoBDTEncontrado | BDT | Avance de etapa, auditoría, ranking. |
| PuntajeBDTIncrementado | BDT | Ranking, auditoría, trazabilidad. |
| PartidaTriviaFinalizada | Trivia | Historial y resultados. |
| PartidaBDTFinalizada | BDT | Historial y resultados. |
| PartidaCancelada | Trivia / BDT | Notificación, historial, bloqueo de acciones. |
| ConvocatoriaRespondida | Inscripción | Historial, lobby, operación. |

## Servicios de dominio

| Servicio | Contexto | Responsabilidad |
|---|---|---|
| ClasificadorRankingService | Trivia / BDT | Ordenar participantes por puntaje y desempates. |
| ValidadorFormularioTriviaService | Trivia | Validar formulario completo. |
| ValidadorInscripcionService | Transversal / por servicio | Validar estado, modalidad, liderazgo, cupo y equipo activo. |
| ValidadorQRService | BDT | Comparar QR decodificado con QR esperado de etapa activa. |

## Servicios de aplicación sugeridos

| Caso de uso | Handler sugerido |
|---|---|
| Crear equipo | `CrearEquipoCommandHandler` |
| Unirse a equipo | `UnirseAEquipoCommandHandler` |
| Crear formulario | `CrearFormularioTriviaCommandHandler` |
| Crear partida Trivia | `CrearPartidaTriviaCommandHandler` |
| Responder Trivia | `ProcesarRespuestaTriviaCommandHandler` |
| Crear BDT | `CrearPartidaBdtCommandHandler` |
| Subir QR | `SubirTesoroQrCommandHandler` |
| Validar QR | `ValidarEnvioQrCommandHandler` |
| Enviar pista | `DespacharPistaCommandHandler` |
