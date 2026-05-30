# Domain Entities by Context — UMBRAL

Este documento organiza las entidades del dominio por contexto y por microservicio físico vigente.

La topología aceptada está definida en:

```txt
docs/05-decisions/ADR-0006-four-service-topology.md
```

## Microservicios físicos vigentes

1. Identity Service
2. Team Service
3. Trivia Game Service
4. BDT Game Service

No existen como microservicios físicos activos:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service

## Identity Service

| Elemento | Tipo | Atributos clave | Ownership |
|---|---|---|---|
| Usuario | Entidad / agregado raíz | UsuarioId, KeycloakId, Nombre, Correo, Rol, Estado | Identity Service |
| KeycloakId | Value Object | Valor | Identity Service |
| RolUsuario | Enum | Administrador, Operador, Participante | Identity Service / Keycloak |
| EstadoUsuario | Enum | Activo, Desactivado | Identity Service |

No posee equipos, partidas, ranking de juego, QR, pistas ni formularios de Trivia.

## Team Service

| Elemento | Tipo | Atributos clave | Ownership |
|---|---|---|---|
| Equipo | Agregado raíz | EquipoId, NombreEquipo, CodigoAcceso, EstadoEquipo, Participantes | Team Service |
| Equipos.Participante | Entidad hija | ParticipanteId, UsuarioId, FechaUnion, EsLider | Team Service |
| EquipoId | Value Object | Valor | Team Service |
| NombreEquipo | Value Object | Valor | Team Service |
| CodigoAcceso | Value Object | Valor | Team Service |
| EstadoEquipo | Enum | Activo, Desactivado, Eliminado | Team Service |

No posee formularios de Trivia, partidas de Trivia, partidas BDT, respuestas, QR, pistas, ranking de partidas ni puntajes de partidas.

## Trivia Game Service

### FormularioTrivia

| Elemento | Tipo | Atributos clave | Ownership |
|---|---|---|---|
| FormularioTrivia | Agregado raíz | FormularioId, Titulo, Preguntas, OperadorId | Trivia Game Service |
| Pregunta | Entidad hija | PreguntaId, Texto, Opciones, PuntajeAsignado, TiempoLimite | Trivia Game Service |
| Opcion | Value Object | Texto, EsCorrecta | Trivia Game Service |
| PuntajeAsignado | Value Object | Valor | Trivia Game Service |
| TiempoLimite | Value Object | Segundos | Trivia Game Service |

### PartidaTrivia

| Elemento | Tipo | Atributos clave | Ownership |
|---|---|---|---|
| PartidaTrivia | Agregado raíz | PartidaId, Nombre, EstadoPartida, Modalidad, FormularioTriviaId, Participantes, PreguntaActualId, TiempoInicio | Trivia Game Service |
| Trivias.Participante | Entidad hija | ParticipanteId, PuntajeAcumulado, TiempoRespuestaAcumulado | Trivia Game Service |
| RespuestaTrivia | Entidad hija | RespuestaId, ParticipanteId, PreguntaId, OpcionSeleccionada, EsCorrecta, TiempoEmpleado | Trivia Game Service |
| EstadoPartida | Enum | Lobby, Iniciada, Cancelada, Terminada | Trivia Game Service |
| Modalidad | Enum | Individual, Equipos | Trivia Game Service |
| RankingTrivia | Proyección / consulta | Participantes ordenados por puntaje y criterio de desempate | Trivia Game Service |
| EventoHistorialTrivia | Registro histórico interno | TipoEvento, ActorId, Descripcion, FechaOcurrencia, Datos | Trivia Game Service |

No posee equipos como estructura social global ni datos maestros de usuario.

## BDT Game Service

| Elemento | Tipo | Atributos clave | Ownership |
|---|---|---|---|
| PartidaBDT | Agregado raíz | PartidaId, Nombre, EstadoPartida, Modalidad, AreaBusqueda, Etapas, Participantes, IndiceEtapaActual | BDT Game Service |
| EtapaBDT | Entidad hija | EtapaId, Orden, CodigoQREsperado, TiempoLimite, PuntajeEtapa, EstadoEtapa | BDT Game Service |
| Bdt.Participante | Entidad hija | ParticipanteId, PuntajeAcumulado, UbicacionActual | BDT Game Service |
| TesoroQR | Entidad hija | TesoroId, EtapaId, ParticipanteId, ImagenUrl, QrDecodificado, ResultadoValidacion, FechaEnvio | BDT Game Service |
| Pista | Entidad hija | PistaId, Texto, DestinatarioId, FechaEnvio | BDT Game Service |
| AreaBusqueda | Value Object | Descripcion | BDT Game Service |
| UbicacionGeografica | Value Object | Latitud, Longitud, FechaRegistro | BDT Game Service |
| CodigoQREsperado | Value Object | Valor | BDT Game Service |
| PuntajeEtapa | Value Object | Valor | BDT Game Service |
| EstadoEtapa | Enum | Pendiente, Activa, Resuelta, Cerrada | BDT Game Service |
| ResultadoValidacionQR | Enum | Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva | BDT Game Service |
| RankingBDT | Proyección / consulta | Participantes ordenados por puntaje y criterio de desempate | BDT Game Service |
| EventoHistorialBDT | Registro histórico interno | TipoEvento, ActorId, Descripcion, FechaOcurrencia, Datos | BDT Game Service |

No posee formularios de Trivia, preguntas de Trivia, respuestas de Trivia ni equipos como estructura social global.

## Clases transversales del diagrama

El diagrama contiene `InscripcionPartida` y `Convocatoria` como clases transversales.

Para evitar crear microservicios adicionales, su ownership se resuelve por modo:

| Concepto | Ownership vigente |
|---|---|
| Inscripcion de Trivia | Trivia Game Service |
| Convocatoria de Trivia | Trivia Game Service |
| Inscripcion de BDT | BDT Game Service |
| Convocatoria de BDT | BDT Game Service |

Cuando una inscripción por equipo requiera validar liderazgo, membresía o estado del equipo, el servicio de juego consulta al Team Service mediante contrato explícito. No accede a su base de datos.

## Auditoría e historial

El modelo contiene `RegistroAuditoria` y `EventoHistorial`.

En la topología vigente no existe `Audit Service`. Por tanto:

| Historial | Ownership vigente |
|---|---|
| Historial de usuarios | Identity Service |
| Historial de equipos | Team Service |
| Historial de Trivia | Trivia Game Service |
| Historial de BDT | BDT Game Service |

Si en el futuro se extrae un servicio de auditoría, debe crearse una nueva ADR.
