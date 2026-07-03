# SP-3d — Runtime BDT (Búsqueda del Tesoro), modalidad Individual — Design

**Fecha:** 2026-06-28
**Servicio:** Operaciones de Sesión
**Slice:** SP-3d (continúa la migración SP-3; sucede a SP-3c runtime Trivia)
**Base prevista:** HEAD de la rama `feature/code-migration-SP-3` al iniciar ejecución (registrar en plan/ledger)

## 1. Alcance

Materializa el runtime en vivo de un `JuegoBDT` **modalidad Individual** dentro de Operaciones de Sesión: validación automática de tesoros QR, secuencia de etapas con ventana de tiempo, cierre por hallazgo o por tiempo, avance secuencial automático, y emisión de eventos de dominio por puerto **No-Op**. Espejo estructural de SP-3c (runtime Trivia).

**Incluye:**
- Snapshot de etapas BDT al publicar (extiende el snapshot de config ya existente).
- Validación de QR: decodificar la imagen subida y comparar con el `CodigoQREsperado` de la etapa activa (RF-29). Autoridad del backend.
- Registro de **cada** intento de tesoro (RF-30).
- Cierre de etapa por primera validación correcta o por agotamiento de tiempo (RF-31); avance automático a la siguiente etapa o finalización del juego (RF-32).
- Ranking BDT nativo **solo a nivel de eventos** (suma de puntajes de etapas ganadas; tie-break por menor tiempo de esas etapas — RF-38/RF-46). El cálculo real vive en Puntuaciones (SP-4).

**Difiere explícitamente (NO implementar aquí):**
- Pistas (`Pista` / `EnviarPista`).
- Geolocalización (~2s al operador) → **SP-3f** (requiere transporte SignalR).
- Modalidad **Equipo** BDT → slice-E (paralelo a SP-3a-E de Trivia).
- SignalR / WebSockets + **barrido automático de timeout** + scheduler de inicio automático → **SP-3f**.
- Scoring/ranking real (`RankingBDTActualizado`, proyección) → **SP-4**.
- Reconexión → **SP-3e**.

## 2. Decisiones (locked en brainstorming)

1. **Scope = Core Individual** (espejo de SP-3c). Difiere pistas/geoloc/Equipo/SignalR/scoring/reconexión.
2. **Validación QR = puerto `IQrDecoder` + impl ZXing.Net.** Imagen recibida como **base64 en JSON** (consistente con el resto de endpoints del servicio). `FakeQrDecoder` en unit tests. Autoridad backend (doctrina: "el backend decodifica la imagen subida").
3. **Avance de etapas = automático** (doctrina RF-32 / HU-34), idéntico a RF-22 de Trivia: al cerrar una etapa (ganada o por tiempo) se activa la siguiente si existe; al cerrar la última, el juego finaliza y la partida activa el siguiente juego o pasa a `Terminada`.
4. **Modelado = tipos separados, paralelo.** `JuegoResumen` se extiende de forma aditiva (gana `_etapas` junto a `_preguntas`), sin tocar la lógica Trivia de 3c. `EtapaSnapshot` y `TesoroQR` son entidades nuevas independientes de `PreguntaSnapshot`/`RespuestaTrivia`. Cero refactor de 3c (sin abstracción "paso" compartida, sin herencia EF TPH).
5. **Endpoint operador de avance/cierre de etapa** como puente: el barrido automático de timeout es SP-3f, así que el operador puede cerrar la etapa activa sin ganador (motivo `Tiempo` si vencida, `AvanceOperador` si no). Espejo del `AvanzarPregunta` de Trivia. El diagrama de clases lo respalda (`JuegoBDT.AvanzarEtapa()`/`CerrarEtapa()`).

## 3. Modelo de dominio (nuevo, Domain/)

### Enums (Domain/Enums)
- `EstadoEtapa { Pendiente, Activa, Ganada, CerradaPorTiempo, Cerrada }` (alineado al diagrama de clases): `Ganada` = cerrada con ganador; `CerradaPorTiempo` = vencida sin ganador; `Cerrada` = cerrada por operador sin ganador (etapa no vencida).
- `ResultadoValidacionQR { Valido, Invalido, NoLegible, NoCorrespondeEtapaActiva }`
- `MotivoCierreEtapa { Ganador, Tiempo, AvanceOperador }`

### `EtapaSnapshot` (entidad, espejo de `PreguntaSnapshot`)
Props (private-set): `EtapaId` (Guid; = `etapaBDTId` del config), `Orden`, `CodigoQREsperado` (string), `Puntaje` (int), `TiempoLimiteSegundos` (int), `Estado`, `FechaActivacion?`, `FechaCierre?`, `MotivoCierre?` (`MotivoCierreEtapa?`), `GanadorParticipanteId?` (Guid?), `TiempoResolucionMs?` (long?). Colección `_tesoros` (`IReadOnlyList<TesoroQR> Tesoros`).
- Ctor privado (EF) + ctor público de construcción.
- `internal void Activar(DateTime now)` — `Pendiente → Activa`, set `FechaActivacion`.
- `internal ResultadoRegistroTesoro RegistrarTesoro(Guid participanteId, string? qrDecodificado, ResultadoValidacionQR resultado, DateTime now)`:
  - guard `Estado==Activa` (si no, `InvalidOperationException` — defensivo, inalcanzable desde runtime).
  - **siempre** crea y añade `TesoroQR` (RF-30).
  - si `resultado==Valido` **y** dentro de ventana (`now < FechaActivacion+limite`): `Estado=Ganada`, `GanadorParticipanteId=participante`, `TiempoResolucionMs = (now − FechaActivacion).TotalMilliseconds`, `FechaCierre=now`, `MotivoCierre=Ganador` → devuelve `CerroEtapa=true, Gano=true, Puntaje`.
  - else (no válido, o válido fuera de ventana): solo registra; el cierre por tiempo lo decide el agregado.
- `internal void CerrarPorTiempo(DateTime now)` — `Activa → CerradaPorTiempo`, `MotivoCierre=Tiempo`, set `FechaCierre`.
- `internal void CerrarPorOperador(DateTime now)` — `Activa → Cerrada`, `MotivoCierre=AvanceOperador`, set `FechaCierre`.

### `TesoroQR` (entidad hija, espejo de `RespuestaTrivia`)
`Id` (Guid auto), `EtapaId`, `ParticipanteId`, `QrDecodificado?` (string?), `Resultado` (`ResultadoValidacionQR`), `FechaEnvio`. Ctor privado (EF) + ctor público; un registro por intento.

### Puerto `IQrDecoder` (Domain/Abstractions)
```csharp
public interface IQrDecoder { string? Decodificar(byte[] imagen); } // null = no legible
```

### Results (Domain/Results, records)
- `ResultadoRegistroTesoro(ResultadoValidacionQR Resultado, bool CerroEtapa, bool Gano, int? Puntaje, Guid EtapaId, Guid? GanadorParticipanteId, long? TiempoResolucionMs, string? QrDecodificado)`
- `ResultadoAvanceEtapa(MotivoCierreEtapa MotivoCierre, int EtapaCerradaOrden, Guid EtapaCerradaId, int? EtapaActivadaOrden, Guid? EtapaActivadaId, bool SinMasEtapas)` (+ datos para eventos)

### `JuegoResumen` (additive — NO tocar la rama Trivia)
Gana: `_etapas` (List<EtapaSnapshot>), `IReadOnlyList<EtapaSnapshot> Etapas`, `EtapaSnapshot? EtapaActiva` (la `Activa`), `bool TieneEtapasAbiertas`, `internal EtapaSnapshot? ActivarSiguienteEtapa(DateTime now)` (OrderBy Orden, primera `Pendiente`). `Activar(now)` activa la primera etapa si el juego es BDT (RF-27 / RB-B19), análogo a cómo activa la primera pregunta si es Trivia.

### `SesionPartida` (additive)
- `private JuegoResumen JuegoBDTActivo()` — `Estado==Iniciada` → `Single(Activo)` → `TipoJuego==BusquedaDelTesoro` else `JuegoActivoNoEsBDTException`.
- `ResultadoRegistroTesoro ValidarTesoro(Guid participanteId, byte[] imagen, DateTime now, IQrDecoder decoder)`:
  1. `JuegoBDTActivo()`
  2. `EtapaActiva` null-check → `NoHayEtapaActivaException`
  3. inscripción activa → `ParticipanteNoInscritoException` (**403**, **antes** de las 409 — corrige el Minor de ordenación de 3c)
  4. decodificar imagen → `resultado` (siempre, para registrar el intento):
     - `decoder.Decodificar==null` → `NoLegible`
     - `== CodigoQREsperado` (etapa activa) → `Valido`
     - `== CodigoQREsperado` de **otra** etapa del juego → `NoCorrespondeEtapaActiva`
     - else → `Invalido`
  5. `etapa.RegistrarTesoro(participanteId, qrDecodificado, resultado, now)` — corre con `Estado==Activa`; **siempre** registra el `TesoroQR` (RF-30); si `resultado==Valido` **y** dentro de ventana (`now < FechaActivacion+limite`) cierra como `Ganada` (ganador/puntaje/tiempo).
  6. si **ganó**: `ActivarSiguienteEtapa(now)`; si no hay siguiente, el agregado deja el juego apto para `FinalizarJuegoActual` (la finalización efectiva la dispara el handler/operador, igual que Trivia).
  7. si **no ganó y la etapa venció** (`now ≥ FechaActivacion+limite`): `etapa.CerrarPorTiempo(now)` + `ActivarSiguienteEtapa(now)` (registro del intento ya hecho en el paso 5).
  8. `return resultado with { EtapaId = etapa.EtapaId }` (con `CerroEtapa`/`Gano`/`Puntaje` reflejando 6/7).
- `ResultadoAvanceEtapa AvanzarEtapa(DateTime now)` (operador): `JuegoBDTActivo()` → `EtapaActiva` null-check → cerrar (`Tiempo` si vencida, else `AvanceOperador`) → `ActivarSiguienteEtapa(now)` → `SinMasEtapas = siguiente is null`.
- `FinalizarJuegoActual`: añade guard BDT — si el juego activo es BDT y `TieneEtapasAbiertas` → `JuegoConEtapasPendientesException` (colocado tras `Single(Activo)`, antes de `Finalizar()`, espejo del guard Trivia).

### Excepciones (Domain/Exceptions)
`JuegoActivoNoEsBDTException`, `NoHayEtapaActivaException`, `JuegoConEtapasPendientesException`. (`ParticipanteNoInscritoException` ya existe de 3c y se reusa.)

### Invariantes
- A lo sumo una `EtapaActiva` por juego (la activación solo ocurre sobre una etapa recién cerrada o al activar el juego).
- Una etapa cierra exactamente una vez (guard de estado).
- El ganador y el puntaje solo se fijan en cierre por validación `Valido` dentro de ventana.
- Etapa que nadie gana → sin puntaje (RF-46).

## 4. Application

### Config snapshot (extender lo de SP-3a/3c)
- `JuegoResumenDto` gana `Bdt` opcional (`= null`) con `BdtConfigDto { AreaBusqueda, EtapaConfigDto[] Etapas }`, `EtapaConfigDto { EtapaBDTId, Orden, CodigoQREsperado, PuntajeAsignado, TiempoLimiteSegundos }`.
- `PartidasConfigHttpClient` deserializa la rama anidada `juego.bdt.etapas[]` (hoy solo `trivia`).
- `PublicarPartidaCommandHandler.MapearJuego`: añade rama — `tipo==BDT && j.Bdt != null` → construye `JuegoResumen` con `EtapaSnapshot`s desde `j.Bdt.Etapas` (snapshot-at-publish); el guard existente (no-Trivia/Trivia null → vacío) deja de tragarse BDT.

### Comandos / queries / handlers
- `ValidarTesoroCommand(PartidaId, ParticipanteId, ImagenBase64) : IRequest<ValidacionTesoroResponse>` + validator (PartidaId + ImagenBase64 NotEmpty; **no** ParticipanteId — viene del claim). Handler: carga → `SesionNoEncontrada` / `now=TimeProvider` / decodifica base64 a `byte[]` / `sesion.ValidarTesoro(..., _qrDecoder)` / **SAVE** then publish. Inyecta `IQrDecoder`.
- `AvanzarEtapaCommand(PartidaId) : IRequest<AvanceEtapaResponse>` + validator + handler (paralelo a `AvanzarPregunta`).
- `ObtenerEtapaActualQuery(PartidaId) : IRequest<EtapaActualDto>` + handler read-only.

### DTOs (Application/DTOs)
- `ValidacionTesoroResponse(Guid PartidaId, Guid EtapaId, string Resultado, bool Gano, bool CerroEtapa, int? Puntaje)`
- `AvanceEtapaResponse(Guid PartidaId, int EtapaCerradaOrden, int? EtapaActivadaOrden, bool SinMasEtapas)`
- `EtapaActualDto(Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, string AreaBusqueda, int TiempoLimiteSegundos, DateTime FechaActivacion)` — **NO-LEAK: nunca `CodigoQREsperado`.**

### Eventos (Application/Interfaces, records; No-Op)
- `TesoroQRValidadoEvent(PartidaId, SesionPartidaId, JuegoId, EtapaId, ParticipanteId, Resultado /*string*/, Instante)` — cada intento (RF-30).
- `EtapaBDTGanadaEvent(PartidaId, SesionPartidaId, JuegoId, EtapaId, ParticipanteId, Puntaje, TiempoResolucionMs)` — al ganar (RF-46).
- `EtapaBDTCerradaEvent(PartidaId, SesionPartidaId, JuegoId, EtapaId, Motivo /*string*/, FechaCierre, GanadorParticipanteId?)`.
- `EtapaBDTActivadaEvent(PartidaId, SesionPartidaId, JuegoId, EtapaId, Orden, TiempoLimiteSegundos, FechaActivacion)` — inicio juego BDT + auto-avance + avance operador.

`ISesionEventsPublisher` +4 métodos; `NoOpSesionEventsPublisher` +4 (`=> Task.CompletedTask`); `FakeSesionEventsPublisher` +4 listas. `RankingBDTActualizado` queda en SP-4.

**Orden de emisión (post-save):**
- Validar y gana → `TesoroQRValidado` → `EtapaBDTGanada` → `EtapaBDTCerrada(Ganador)` → `EtapaBDTActivada(siguiente)` (o `PartidaFinalizada` si era el último juego, vía finalización).
- Validar sin ganar → solo `TesoroQRValidado`.
- Validar tarde (cierra por tiempo) → `TesoroQRValidado` → `EtapaBDTCerrada(Tiempo)` → `EtapaBDTActivada(siguiente)`.
- Avance operador → `EtapaBDTCerrada(Tiempo|AvanceOperador)` (+`EtapaBDTActivada` si hay siguiente).

**Activación del juego BDT:** gemelo del helper `PublicarPreguntaActivadaSiTriviaAsync` de 3c — `PublicarEtapaActivadaSiBdtAsync` cableado tras `JuegoActivado` en `IniciarPartida` (rama Iniciada) y en `FinalizarJuegoActual` (rama Avanzado), cuando el juego activado es BDT (short-circuit si no hay etapa activa).

## 5. Infrastructure

- Mapeos EF: `EtapaSnapshot → etapas_snapshot` (12 cols, nullable las de cierre; `_tesoros` field-access cascade FK `etapaid`), `TesoroQR → tesoros_qr` (Id PK, `EtapaId` col), `JuegoResumen +HasMany(_etapas)` field-access cascade FK `juegoid`.
- Migración **aditiva**: `Up` = 2 `CreateTable` (etapas_snapshot, tesoros_qr) + índices; `Down` = drop (dependientes primero). Cero ALTER destructivo sobre tablas existentes.
- `SesionPartidaRepository.GetByPartidaIdAsync` += `Include(Juegos).ThenInclude(Etapas).ThenInclude(Tesoros)` junto a los Include de Trivia ya presentes (mismo plan EF8 multi-branch).
- `ZXingQrDecoder : IQrDecoder` en Infra/Services (NuGet `ZXing.Net` + decodificación de imagen). Registrado en DI (Scoped/Singleton). En entornos de test se sustituye por `FakeQrDecoder`.

## 6. Api

- `SesionesController` += 3 endpoints thin (Validar `POST etapa-actual/tesoro` [ParticipanteId del claim `sub`, body `{ imagenBase64 }`], Avanzar `POST etapa-actual/avance`, ObtenerEtapaActual `GET etapa-actual`), colocados antes de `ObtenerParticipanteId`. Los 11 endpoints existentes intactos.
- Middleware `MapStatus`: +`JuegoActivoNoEsBDT`/`NoHayEtapaActiva`/`JuegoConEtapasPendientes` → 409. `ParticipanteNoInscrito` → 403 ya existe. Todos los arms previos retenidos.

### Tabla de endpoints (contrato)
| Acción | Verbo | Ruta | Rol | OK | Errores |
|---|---|---|---|---|---|
| Validar tesoro | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/tesoro` | Participante | 200 + `ValidacionTesoroResponse` | 401 sin identidad · 403 no inscrito · 404 sesión no existe · 409 no iniciada / juego no BDT / sin etapa activa |
| Avanzar/cerrar etapa | POST | `/operaciones-sesion/partidas/{partidaId}/etapa-actual/avance` | Operador | 200 + `AvanceEtapaResponse` | 404 · 409 no iniciada / juego no BDT / sin etapa activa |
| Etapa actual | GET | `/operaciones-sesion/partidas/{partidaId}/etapa-actual` | Operador/Participante | 200 + `EtapaActualDto` | 404 · 409 sin etapa activa |

## 7. Testing (espejo de SP-3c)

- **Unit dominio:** `EtapaSnapshot` (activar; 4 resultados QR; gana dentro de ventana; multi-intento sin dedup; cierre por tiempo; ganador/puntaje/tiempo solo en `Valido`); `JuegoResumen` etapas (activar primera; siguiente; cero-etapas BDT compat); `SesionPartida` (`ValidarTesoro` happy/guards/orden 403-antes-409; `AvanzarEtapa`; guard finalización con etapa abierta).
- **Unit handlers:** `ValidarTesoroCommandHandler` (con `FakeQrDecoder`: gana → 3-4 eventos + SaveCount1; inválido → solo `TesoroQRValidado`); `AvanzarEtapaCommandHandler`; `ObtenerEtapaActualQueryHandler` (no-leak por reflexión: `EtapaActualDto` sin `CodigoQREsperado`). `PublicarPartidaCommandHandler` rama BDT snapshot.
- **Unit controller + middleware:** 3 endpoints (dispatch + ParticipanteId del claim); 403/409 nuevos.
- **Integration:** round-trip persistencia (publicar BDT → reload con etapas+tesoros).
- **Contract (WebApplicationFactory):** lifecycle end-to-end (publicar→inscribir→iniciar→validar gana→auto-avance→GET etapa Orden2→avance operador última→finalizar→Terminada); validar inválido→registrado sin ganar; sin inscripción→403; finalizar con etapa abierta→409; no-leak (GET nunca trae `codigoQREsperado`).
- Stub del config client trae al menos un juego BDT con ≥2 etapas.

## 8. Doctrina, límites y trazabilidad

- **R1 / estructura graduada:** Application = conjunto exacto de carpetas; `Handlers/{Commands,Queries}`; interfaces de repo en `Domain/Abstractions/Persistence`; `IQrDecoder` en `Domain/Abstractions`; `ZXingQrDecoder` en `Infrastructure/Services`; `Program.cs` solo `MapControllers` + middleware; cada controller con unit tests.
- **Boundary / ADR-0010:** Operaciones nunca lee/escribe otra DB; las etapas llegan solo por snapshot HTTP `GET /partidas/{id}` (read-only). Estado runtime en `umbral_operaciones_sesion`.
- **Eventos** por `NoOpSesionEventsPublisher` (sin broker real en SP-3d); **save-before-publish** en todos los handlers mutadores.
- **Pureza/clock:** Domain puro (`now` como parámetro); handlers vía `TimeProvider`. Migración aditiva.
- **Contratos:** actualizar `contracts/http/operaciones-sesion-api.md` (3 endpoints + DTOs) y `contracts/events/operaciones-sesion-events.md` (4 eventos BDT, registrados con payload; nota No-Op); fila SP-3d en `docs/04-sdd/traceability-matrix.md`.

## 9. Watch-items

- **Concurrencia (SP-3f):** `SesionPartida` sigue sin token optimista (rowversion/xmin). Inocuo hoy (No-Op, sin scheduler); riesgo de doble-publicación cuando aterricen el scheduler + broker real en SP-3f. Trazado en memoria `sp3f-concurrency-token`.
- **Higiene git:** los implementers solo hacen `git add <rutas específicas>`, nunca `-A`/`.`; jamás `git checkout/restore/clean/reset` amplio.
- **NoCorrespondeEtapaActiva:** requiere comparar el texto decodificado contra los `CodigoQREsperado` de todas las etapas del snapshot (ya disponibles); coincidencia con etapa ≠ activa ⇒ `NoCorrespondeEtapaActiva`; sin coincidencia ⇒ `Invalido`.

## 10. Diferimientos (resumen)

| Diferido | Slice |
|---|---|
| Pistas (entrega) | SP-3f (o slice propio) |
| Geolocalización ~2s al operador | SP-3f (transporte SignalR) |
| Modalidad Equipo BDT | slice-E |
| SignalR / WebSockets | SP-3f |
| Barrido automático de timeout + scheduler inicio automático | SP-3f |
| Scoring / ranking real (RankingBDTActualizado, proyección) | SP-4 |
| Reconexión | SP-3e |
