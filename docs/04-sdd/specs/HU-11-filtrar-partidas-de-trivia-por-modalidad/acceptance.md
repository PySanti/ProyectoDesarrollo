# HU-11 — Acceptance: Filtrar partidas de Trivia por modalidad

## CA-01 — Filtro Individual

| Paso | Resultado |
| --- | --- |
| Handler con `Modalidad="Individual"` filtra solo individuales | ✅ `Handle_FilterIndividual_ReturnsOnlyIndividualGames` |
| Controller recibe `?modalidad=Individual` y retorna 200 | ✅ `GetAll_FilterByModalidad_Returns200` |

## CA-02 — Filtro Equipo

| Paso | Resultado |
| --- | --- |
| Handler con `Modalidad="Equipo"` filtra solo equipos | ✅ `Handle_FilterEquipo_ReturnsOnlyEquipoGames` |

## CA-03 — Sin filtro

| Paso | Resultado |
| --- | --- |
| Handler sin `Modalidad` retorna todas | ✅ `Handle_NoFilter_ReturnsAllGames` |
| Endpoint sin query string retorna 200 | ✅ Tests existentes HU-09 (*) |

## CA-04 — Filtro sin resultados

| Paso | Resultado |
| --- | --- |
| Handler con filtro sin coincidencias retorna vacío | ✅ `Handle_FilterNoMatches_ReturnsEmptyList` |

## CA-05 — Modalidad inválida

| Paso | Resultado |
| --- | --- |
| Handler con modalidad inválida lanza excepción | ✅ `Handle_InvalidModalidad_ThrowsArgumentOutOfRangeException` |
| Controller recibe `?modalidad=Invalido` y retorna 400 | ✅ `GetAll_FilterByInvalidModalidad_Returns400` |

## Resumen

| Criterio | Estado |
| --- | --- |
| CA-01 Filtro Individual | ✅ |
| CA-02 Filtro Equipo | ✅ |
| CA-03 Sin filtro | ✅ |
| CA-04 Filtro sin resultados | ✅ |
| CA-05 Modalidad inválida | ✅ |
| Backward compatible con HU-09 | ✅ |
| Sin cambios en Domain | ✅ |
| Sin cambios en Infrastructure | ✅ |

\* `GetAll_AsParticipante_Returns200WithList`, `GetAll_AsOperador_Returns200WithList` y `GetAll_NoGames_ReturnsEmptyList` fueron creados en HU-09 y continúan pasando sin modificación.
