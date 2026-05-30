# Business Rules — UMBRAL

Este archivo normaliza las reglas de negocio del SRS y del modelo de dominio para que OpenCode pueda aplicarlas al generar SDD, diseño e implementación.

## Reglas generales

| ID | Regla |
|---|---|
| BR-G01 | El sistema solo soporta dos modos de juego: Trivia y Búsqueda del Tesoro. |
| BR-G02 | No se permite crear, configurar ni ejecutar modos adicionales. |
| BR-G03 | Las partidas tienen estados comunes: `lobby`, `iniciada`, `cancelada`, `terminada`. |
| BR-G04 | En `lobby` se permiten inscripciones. |
| BR-G05 | En `iniciada` se permiten acciones propias del modo: responder preguntas o subir tesoros. |
| BR-G06 | En `cancelada` o `terminada` se bloquean nuevas acciones de juego. |
| BR-G07 | Toda transición de estado debe validarse. |
| BR-G08 | Cada acción que afecte partida debe respetar rol, modalidad, estado, cupo y pertenencia/liderazgo. |
| BR-G09 | Los jugadores pueden visualizar partidas publicadas aunque sean individuales o por equipo. |
| BR-G10 | Si un jugador no líder intenta inscribir un equipo, se muestra: “Debes ser líder de un equipo para entrar en este evento”. |

## Reglas de identidad y roles

| ID | Regla |
|---|---|
| BR-U01 | Keycloak gestiona autenticación. |
| BR-U02 | Los roles base son Administrador, Operador y Participante. |
| BR-U03 | UMBRAL no almacena contraseñas ni credenciales sensibles. |
| BR-U04 | UMBRAL guarda solo referencia local al usuario autenticado. |
| BR-U05 | El rol se asigna durante creación y no se modifica desde UMBRAL después. |
| BR-U06 | Un usuario desactivado no puede acceder a partidas ni ejecutar acciones. |
| BR-U07 | El liderazgo de equipo no es rol de Keycloak; es estado/condición de negocio. |

## Reglas de equipos

| ID | Regla |
|---|---|
| BR-E01 | Los equipos son globales para Trivia y BDT. |
| BR-E02 | Un participante puede crear un equipo solo si no pertenece a otro. |
| BR-E03 | Al crear equipo, el creador queda como líder. |
| BR-E04 | Al crear equipo, se genera un código único. |
| BR-E05 | Un participante puede unirse a equipo mediante código válido si no pertenece a otro. |
| BR-E06 | Un participante pertenece como máximo a un equipo. |
| BR-E07 | Un equipo puede tener máximo cinco integrantes. |
| BR-E08 | Un participante no líder puede salir directamente. |
| BR-E09 | Si el líder sale y hay otros integrantes, debe transferir liderazgo primero. |
| BR-E10 | Si el líder está solo y sale, el equipo se elimina. |
| BR-E11 | El líder puede eliminar su equipo si las condiciones de inscripción/partida lo permiten. |
| BR-E12 | Al eliminar un equipo, sus integrantes deben ser informados. |
| BR-E13 | Un equipo desactivado no puede inscribirse en nuevas partidas. |
| BR-E14 | El administrador puede crear, consultar, editar y desactivar equipos. |
| BR-E15 | El administrador puede modificar liderazgo si el diseño aprobado lo mantiene; debe notificarse al antiguo y nuevo líder. |

## Reglas de inscripción y convocatoria

| ID | Regla |
|---|---|
| BR-I01 | Las inscripciones solo se realizan en partidas en estado `lobby`. |
| BR-I02 | En partidas individuales se inscriben usuarios. |
| BR-I03 | En partidas por equipo se inscriben equipos, gestionados por el líder. |
| BR-I04 | La inscripción debe validar cupo disponible. |
| BR-I05 | Al inscribir equipo, se convocan sus integrantes. |
| BR-I06 | Cada integrante puede aceptar o rechazar la convocatoria. |
| BR-I07 | La aceptación o rechazo debe quedar registrado. |
| BR-I08 | Una partida no inicia si no cumple mínimos de participación. |
| BR-I09 | En modo por equipo, los mínimos/máximos por equipo definidos por operador deben validarse. |

## Reglas de Trivia

| ID | Regla |
|---|---|
| BR-T01 | Solo el operador puede crear formularios de Trivia. |
| BR-T02 | Un formulario debe tener preguntas, opciones, respuesta correcta, puntaje y tiempo por pregunta. |
| BR-T03 | No se puede usar un formulario incompleto para crear partida. |
| BR-T04 | Solo el operador puede crear partidas de Trivia. |
| BR-T05 | Toda partida de Trivia debe estar asociada a un formulario válido. |
| BR-T06 | El operador define nombre, modalidad, mínimos, máximos y tiempo de inicio. |
| BR-T07 | En modalidad individual, el máximo corresponde a jugadores. |
| BR-T08 | En modalidad equipos, el máximo corresponde a equipos. |
| BR-T09 | En modalidad equipos, el operador define mínimo y máximo de jugadores por equipo. |
| BR-T10 | Al iniciar lobby, la partida aparece en el panel de Trivia. |
| BR-T11 | En individual, se acepta una respuesta por jugador por pregunta activa. |
| BR-T12 | En equipos, se acepta una respuesta por equipo; la primera respuesta del equipo es definitiva. |
| BR-T13 | Se rechazan respuestas repetidas, tardías o fuera de estado válido. |
| BR-T14 | Al cerrar una pregunta, se valida respuesta y se actualiza ranking. |
| BR-T15 | El operador no interviene respuestas; solo ve ranking y puede cancelar. |
| BR-T16 | El ranking se ordena por puntaje acumulado y usa tiempo de respuesta acumulado para desempate si aplica. |

## Reglas de Búsqueda del Tesoro

| ID | Regla |
|---|---|
| BR-B01 | Solo el operador puede crear partidas BDT. |
| BR-B02 | Una BDT puede ser individual o por equipo. |
| BR-B03 | El operador define nombre, área de búsqueda, modalidad y mínimos/máximos. |
| BR-B04 | Una BDT debe tener al menos una etapa válida. |
| BR-B05 | Cada etapa debe tener QR esperado y tiempo límite. |
| BR-B06 | No se puede publicar una BDT sin etapas válidas. |
| BR-B07 | Al iniciar BDT, se activa la primera etapa. |
| BR-B08 | El participante ve etapa activa, temporizador y opción de subir tesoro. |
| BR-B09 | El sistema decodifica el QR de la imagen enviada. |
| BR-B10 | El QR decodificado debe compararse contra el QR esperado de la etapa activa. |
| BR-B11 | El resultado puede ser válido, inválido, no legible o no correspondiente a etapa activa. |
| BR-B12 | Todo envío de tesoro queda registrado. |
| BR-B13 | La etapa se cierra al validar correctamente el QR o al vencer el tiempo. |
| BR-B14 | Al cerrar etapa se avanza a la siguiente o se termina la partida si era la última. |
| BR-B15 | El operador puede enviar pistas durante una BDT iniciada. |
| BR-B16 | Cada pista enviada debe registrarse en historial. |
| BR-B17 | La geolocalización requiere autorización del participante. |
| BR-B18 | La ubicación se actualiza cada 2 segundos durante BDT iniciada. |

## Reglas de auditoría y trazabilidad

| ID | Regla |
|---|---|
| BR-A01 | Todo cambio relevante de estado debe registrarse. |
| BR-A02 | Toda inscripción y convocatoria debe registrarse. |
| BR-A03 | Toda respuesta de Trivia debe registrarse. |
| BR-A04 | Todo tesoro QR subido y su validación debe registrarse. |
| BR-A05 | Toda pista enviada debe registrarse. |
| BR-A06 | Toda variación de puntaje debe tener trazabilidad de origen. |
| BR-A07 | Toda cancelación y resultado final debe registrarse. |
| BR-A08 | Los eventos relevantes deben poder alimentar auditoría, ranking, historial y tiempo real. |
