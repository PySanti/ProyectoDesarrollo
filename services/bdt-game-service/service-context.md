# BDT Game Service Context

## Responsibility

Manages Búsqueda del Tesoro content and live BDT execution.

The BDT Game Service owns the BDT game lifecycle, BDT stages, QR treasure validation, clues, BDT ranking, BDT history, BDT geolocation updates and BDT real-time updates.

## Owns

- PartidaBDT
- EtapaBDT
- TesoroQR
- Pista
- AreaBusqueda
- UbicacionGeografica
- CodigoQREsperado
- ExploradorBDT
- EtapasGanadas
- TiempoAcumuladoEtapasGanadas
- BDT inscriptions and convocations related to BDT execution
- BDT ranking
- BDT history
- BDT geolocation updates
- BDT real-time updates

## Does not own as active BDT ranking concepts

- PuntajeEtapa
- PuntajeAcumulado for BDT ranking
- PuntajeBDTIncrementado

BDT ranking must not be calculated from numeric accumulated score unless a future ADR changes this decision.

## BDT ranking rule

BDT ranking is based on:

1. highest number of stages won;
2. if tied, lowest accumulated time only across stages won.

Recommended domain fields:

```txt
ExploradorBDT.EtapasGanadas
ExploradorBDT.TiempoAcumuladoEtapasGanadas
EtapaBDT.TiempoResolucion
```

Recommended ranking event:

```txt
RankingBDTActualizado
```

## QR validation rule

The expected QR is stored as textual content:

```txt
CodigoQREsperado
```

The participant uploads an image from the mobile app. The backend decodes the QR content and compares it with the expected textual content for the active stage.

The mobile app must not be treated as authoritative for QR validation.

## Area and geolocation rule

`AreaBusqueda` is a simple textual description.

`UbicacionGeografica` belongs to live BDT supervision and is required for active BDT participation according to the SRS.

Do not implement advanced geospatial analytics, route history or polygon validation unless explicitly introduced by a future SDD.

## Related stories

- HU-10
- HU-12
- HU-14
- HU-34
- HU-37
- HU-38
- HU-39
- HU-40
- HU-41
- HU-42
- HU-43
- HU-44
- HU-45
- HU-46
- HU-47
- HU-48
- HU-49
- HU-51
- HU-52
- HU-53
- HU-54
- HU-55
- HU-56
- HU-57

## Does not own

- Team master data
- Team membership source of truth
- Team leadership source of truth
- User identity data
- Keycloak roles
- Trivia forms
- Trivia questions
- Trivia answers
- Trivia scoring
- Trivia ranking

## Supporting service interactions

The BDT Game Service may call Team Service through documented HTTP contracts to validate:

- team existence;
- active team status;
- leader authorization;
- membership;
- team size when required by BDT rules.

It may use Identity Service through documented contracts or token claims to validate user identity and base role.

It must not read or write another service's database.

## Real-time responsibilities

The BDT Game Service may publish user-visible real-time updates for:

- BDT lobby changes;
- participant/equipment joined updates;
- BDT state changes;
- active stage changes;
- QR treasure submission result;
- stage closing;
- clues sent;
- BDT ranking updates;
- BDT geolocation updates;
- cancellation notifications.

SignalR/WebSocket adapters must not contain business rules.
