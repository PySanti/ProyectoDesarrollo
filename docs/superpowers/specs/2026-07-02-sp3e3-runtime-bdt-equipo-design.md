# SP-3e-3 — Runtime BDT en modalidad Equipo (identidad dual sobre ValidarTesoro/TesoroQR)

- **Slice:** SP-3e-3 (tercero de slice-E). Base: SP-3e-2 (runtime Trivia Equipo, commits `d9d63f3..97d8308`, APPROVED).
- **Servicio:** Operaciones de Sesión (único servicio tocado).
- **Habilita:** SP-4 (Puntuaciones) recibe `EtapaBDTGanada` con `EquipoId` para acreditar el `Puntaje` de la etapa al equipo; SP-3e-4 (pistas Equipo) hereda la resolución de participación.
- **Fuera de alcance:** pistas Equipo (`PrepararPista` sigue con guard individual — SP-3e-4), geolocalización (relay puro sin guard de inscripción, no afectado por modalidad), minors diferidos de SP-3e-2, scoring/ranking (SP-4), broker RabbitMQ real, clientes móvil/web.

## 1. Objetivo

Hoy el runtime BDT es Individual-only: `SesionPartida.ValidarTesoro` exige inscripción individual (`_inscripciones.Any(i => i.ParticipanteId == emisor && i.EsActiva)`), así que un miembro de equipo recibe 403 `ParticipanteNoInscrito`. Este slice habilita subir/validar QR en modalidad Equipo con la regla del SRS — **una validación correcta de cualquier miembro activo gana la etapa para todo el equipo** — aplicando el patrón identidad dual aprobado en SP-3e-2.

## 2. Reglas de dominio

- **Reintentos ilimitados, espejo de Individual** (decisión confirmada, lectura literal del SRS: no existe regla de 1 intento en BDT, a diferencia de Trivia). Cualquier miembro con convocatoria Aceptada puede intentar cuantas veces quiera hasta que la etapa cierre; un QR incorrecto solo registra el `TesoroQR` (con autor + equipo) y no sella a nadie.
- **Miembro activo = convocatoria Aceptada** en la inscripción activa del equipo (fundación SP-3e-1). Pendiente o Rechazada → 403 `ParticipanteNoInscrito` (misma resolución que Trivia C1).
- **Ganador en Equipo = el equipo.** Primera validación correcta dentro de la ventana → etapa `Ganada`, `GanadorParticipanteId` = autor material, nuevo `GanadorEquipoId` = su equipo (null en Individual).
- **Cierre global sin cambios:** la primera validación correcta (de cualquier equipo o participante) gana la etapa para todos y activa la siguiente; timeout y `AvanzarEtapa` (operador) sin cambios — cierre sin ganador.
- **Concurrencia:** dos miembros simultáneos escriben en el aggregate vía `xmin` (SP-3f-1): uno gana, el otro recibe 409 de concurrencia; aceptable (sin dedup no hay caso "duplicada").

## 3. Diseño — identidad dual (enfoque A aprobado; B guard-mínimo descartado por dejar a SP-4 sin equipo, C refactor-común descartado por tocar código Trivia aprobado)

Autor real y equipo viajan juntos: `ParticipanteId` = quién subió la imagen (auditoría, SP-4), `EquipoId?` = a quién cuenta. `EquipoId == null` ⇔ modalidad Individual. Individual no cambia de comportamiento (campo nuevo siempre null).

### 3.1 Dominio

**`SesionPartida.ValidarTesoro(participanteId, imagen, now, decoder)`** — resolución de participación idéntica a `ResponderPregunta` (C1):
- Sesión Individual: guard actual sin cambios.
- Sesión Equipo: inscripción activa con convocatoria Aceptada de `participanteId` → `equipoId = inscripcion.EquipoId`. Sin match → `ParticipanteNoInscritoException` (403).
- Pasa `equipoId` (Guid?) a `RegistrarTesoro`.

**`EtapaSnapshot.RegistrarTesoro(participanteId, equipoId?, qrDecodificado, resultado, now)`:**
- Sin dedup. Cada intento agrega `TesoroQR` con `EquipoId?`.
- Rama ganadora: además de `GanadorParticipanteId = participanteId`, setea nuevo `GanadorEquipoId = equipoId`.
- `TesoroQR` += property `EquipoId?` + parámetro de ctor con default null.
- `ResultadoRegistroTesoro` += `EquipoId? = null` y `GanadorEquipoId? = null` (trailing, cero ripple).

### 3.2 Aplicación

**`ValidarTesoroCommandHandler`:** sin cambios de flujo; propaga a los eventos.

**Eventos (seam `ISesionEventsPublisher` — firmas extendidas con trailing defaults, sin métodos nuevos):**
- `TesoroQRValidadoEvent` += `EquipoId? = null` (cada intento).
- `EtapaBDTGanadaEvent` += `EquipoId? = null` (SP-4 acredita puntos al equipo).
- `EtapaBDTCerradaEvent` += `GanadorEquipoId? = null` (solo la rama "Ganador" lo llena; Tiempo/AvanceOperador → null).
- Sitios de construcción por timeout/avance (`BarrerTimeoutsCommandHandler`, `AvanzarEtapaCommandHandler`) no se tocan — compilan por defaults.
- **Payloads SignalR sin cambios** (verificado: `EtapaActivadaPayload`/`EtapaCerradaPayload`/`EtapaGanadaPayload` no portan identidad).

### 3.3 HTTP

- `POST .../etapa-actual/tesoro` no cambia de forma: mismo request `{ imagenBase64 }`, mismos códigos; en Equipo el 403 significa "sin convocatoria aceptada". Sin endpoints nuevos.
- Contrato: nota de semántica Equipo en la fila existente de `contracts/http/operaciones-sesion-api.md`.

### 3.4 Persistencia (EF, migración additiva `SP3e3RuntimeBdtEquipo`)

- `tesoros_qr.equipoid` (uuid, nullable) y `etapas_snapshot.ganadorequipoid` (uuid, nullable). Filas existentes intactas (null). Tipos primitivos nullable ya soportados — sin riesgo de converter (lección B3→B10).

### 3.5 Proyecciones

- **Sin cambios.** BDT no tiene equivalente de `yaRespondioPreguntaActual` (reintentos libres); `EtapaActualDto` no porta identidad.

## 4. Testing

- **Dominio:** convocado aceptado sube QR válido (etapa Ganada, `GanadorParticipanteId`=autor + `GanadorEquipoId`=equipo); QR incorrecto no sella (mismo miembro y otro miembro reintentan); Pendiente/Rechazado → `ParticipanteNoInscrito` (ambos casos — cierra el minor C1 de SP-3e-2 en BDT); QR válido de equipo A cierra etapa para equipo B; Individual regression (EquipoId null en todo el flujo).
- **Aplicación/handler:** los 3 eventos portan `EquipoId`/`GanadorEquipoId` en Equipo y null en Individual; rama timeout sin ganador de equipo.
- **Integration (InMemory):** round-trip de `equipoid`/`ganadorequipoid` con contextos write/read separados (patrón B11-fix).
- **Regresión:** suites completas de Operaciones (Unit/Integration/Contract) verdes; cambios en tests existentes solo por arity de records/métodos extendidos, nunca de comportamiento.

## 5. Riesgos y mitigaciones

- **Arity ripple:** extender `ResultadoRegistroTesoro`, `RegistrarTesoro` y los event records rompe construction sites en tests — el plan debe listar la búsqueda repo-wide de sitios como paso explícito (lección B13/C1). `RegistrarTesoro` es `internal` con un solo caller productivo (`ValidarTesoro`); decidir en el plan si `equipoId` va con default o posicional obligatorio (como C1 hizo con `RegistrarRespuesta`).
- **Ventana EF:** tarea de dominio y de migración adyacentes en el plan (lección SP-3e-1).

## 6. Follow-ups diferidos (no bloqueantes)

- SP-3e-4: pistas Equipo (guard de `PrepararPista` + destino por equipo).
- Minors SP-3e-2 (el real: `ObtenerMiSesionQueryHandler` resuelve convocatoria first-match sin preferir Aceptada).
- Minors SP-3e-1 heredados (lobby con `Guid.Empty`, xmin child-only, índice `inscripciones.equipoid`).
