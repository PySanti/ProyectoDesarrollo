# QR generado para la etapa BDT

**Fecha:** 2026-07-16
**Servicio:** Partidas (backend) + frontend web (operador)
**Tipo:** cambio de regla + UX sobre HU ya implementada (HU-28 · RF-25). No introduce HU nueva.
**Reglas:** BR-B03 (refinada), BR-B04 (sin cambios).

## 1. Problema

Al crear un juego BDT, al operador se le pide el tesoro como un **campo de texto libre**
(`CreatePartidaPage.tsx:688`): escribe lo que quiera y luego tiene que fabricarse el QR por su
cuenta, fuera del sistema, con alguna herramienta externa. El sistema le pide el contenido de un
QR pero no le da el QR.

Queremos que el sistema **genere el código y le entregue el QR listo para imprimir**.

## 2. Hechos verificados

Comprobado contra el código, no contra la documentación.

| # | Hecho | Evidencia |
|---|---|---|
| H1 | La validación del tesoro es **automática**: ZXing decodifica la imagen y se compara el texto | `ValidarTesoroCommandHandler`, `ZXingQrDecoder`, `SesionPartida.ValidarTesoro` |
| H2 | **No existe validación manual del operador**: cero ocurrencias de aprobar/rechazar tesoro | búsqueda en `services/operaciones-sesion/src` y `frontend/src` |
| H3 | El SRS lo exige explícito: *"validación automática de tesoros mediante la decodificación del QR… y la comparación de su contenido con el QR esperado"* | `srs.md:105` |
| H4 | La comparación es **exacta**: `texto == activa.CodigoQREsperado` | `SesionPartida.ClasificarQr` |
| H5 | Partidas **no tiene endpoints de edición**: solo crear y leer. Un juego creado es inmutable | `PartidasController` |
| H6 | `GET /partidas/{partidaId}` **ya devuelve** `codigoQREsperado` | `contracts/http/partidas-config.md:106` |
| H7 | El gateway enruta `/partidas/**` con policy **`OperadorOAdministrador`**: el participante no llega | `gateway/src/Umbral.Gateway/appsettings.json` |
| H8 | Los DTOs de runtime que llegan a clientes (`EtapaActualDto`, `EtapaActivadaPayload`) **no llevan** el código | `BdtRuntimeDtos.cs`, `SesionRealtimePayloads.cs` |
| H9 | `EtapaBDT.Crear` hoy solo valida `IsNullOrWhiteSpace` sobre el código | `EtapaBDT.cs` |
| H10 | `JuegoBDT.Crear` ya valida invariantes de la colección (`ValidarOrdenContiguo`) tras montar las etapas | `JuegoBDT.cs` |
| H11 | El detalle de partida ya pinta el código como texto | `PartidaDetailPage.tsx:223` |
| H12 | El frontend **no tiene** librería de QR | `frontend/package.json` |
| H13 | El panel BDT en vivo del operador **no muestra** el código esperado | `BdtRuntimePanel.tsx` |

## 3. Decisiones

### D1 — El frontend genera; el backend valida

El botón hace `crypto.randomUUID()` y el backend exige que lo recibido **sea un UUID**.

El backend no genera porque el operador tiene que **ver y regenerar el QR antes de guardar**, y
Partidas no admite edición (H5): un código generado en el servidor sería inmutable desde el
instante en que existe. Generarlo en el cliente es lo único que da esa UX.

Pero la regla **no se queda solo en el cliente**: `EtapaBDT.Crear` rechaza cualquier cosa que no
parsee como UUID. Un `POST` con `"hola"` da 400. El backend no *genera* el código, pero sí
*garantiza* que todo código es un identificador único e impredecible — que es lo que protege al
tesoro.

Residuo aceptado: un operador con Postman puede elegir su propio UUID. Es irrelevante — el QR es
**su** secreto, no una barrera contra él.

### D2 — El código se guarda literal, sin normalizar

`EtapaBDT.Crear` valida el formato pero **no transforma** el valor (solo `Trim()`, como hoy).

No es cosmético. La comparación en Operaciones es exacta (H4) contra el texto embebido en el QR
físico. Si el backend normalizara el casing o el formato, el texto guardado dejaría de coincidir
con el impreso y **ningún tesoro validaría jamás**. La validación mira, no toca.

### D3 — Unicidad dentro del juego

`JuegoBDT.Crear` gana `ValidarCodigosUnicos()`, junto a `ValidarOrdenContiguo()` (H10).

Sin esto, dos etapas podrían compartir código y el QR de la etapa 1 ganaría también la 2. Con UUIDs
la colisión es imposible por azar, pero el contrato acepta el código del cliente: hay que cerrarlo.
Comparación **case-insensitive**, más estricta que la de runtime, para que dos códigos que solo
difieren en mayúsculas no convivan.

### D4 — El operador ve el QR al generarlo y al reimprimir; nunca en vivo

| momento | ¿ve el QR? |
|---|---|
| Al pulsar "Generar QR" (creación) | Sí, con descarga PNG |
| Detalle de la partida, antes de iniciar | Sí, para reimprimir |
| Sesión activa (`Iniciada`) | **No** |
| Participante | **Nunca** |

En sesión activa no se muestra **a propósito**: en BDT el operador está en el sitio, con los
participantes cerca y su pantalla a la vista. Enseñar ahí el tesoro es regalar la respuesta, y no
aporta nada — la validación es automática (H1), el operador no necesita el QR para operar.

Reimprimir **no cuesta backend**: `GET /partidas/{id}` ya devuelve el código (H6), la vista ya lo
pinta (H11) y el gateway ya impide que el participante llegue (H7).

**"Regenerar" existe solo en el formulario de creación**, antes de guardar, donde todavía no hay
nada persistido. En el detalle **no hay botón de regenerar**: Partidas no admite edición (H5), así
que no habría dónde escribir el código nuevo. El detalle solo muestra y descarga.

### D5 — La regla vive **solo** en el dominio, no en el validador

`AgregarJuegoBDTCommandValidator.EtapaRequestValidator` valida hoy
`RuleFor(e => e.CodigoQREsperado).NotEmpty()`. **Se queda como está: no se le añade el chequeo de
UUID.**

Tentador duplicarlo ahí para dar un error de validación bonito, pero sería la misma regla escrita en
dos sitios, libre de divergir en silencio — el defecto que ya nos costó un slice hoy (el nombre del
índice renombrado en el esquema y olvidado en dos literales de `EquipoRepository`). El dominio es el
único dueño: el validador comprueba forma (no vacío), `EtapaBDT.Crear` comprueba la regla.

No se pierde nada por el camino: `EtapaBDTInvalidaException` ya mapea a **400** en
`ExceptionHandlingMiddleware`, igual que un `ValidationException`. El cliente ve el mismo status.

### D6 — Una dependencia: `qrcode`

`qrcode.toDataURL(codigo)` devuelve un data-URL que sirve **a la vez** para el `<img>` de la
previsualización y para el `href` del `<a download>`. Una librería resuelve mostrar y descargar.

## 4. Alcance

**Backend (Partidas)**

- `EtapaBDT.Crear`: `IsNullOrWhiteSpace` → **`Guid.TryParse`**. Mensaje de error nuevo.
- `JuegoBDT.Crear`: `+ ValidarCodigosUnicos()`.
- `AgregarJuegoBDTCommandValidator`: **no se toca** (D5).

**Frontend**

- `CreatePartidaPage.tsx`: el `<input>` de QR → botón "Generar QR" / "Regenerar" + previsualización + descarga PNG.
- `createPartidaDraft.ts`: el mensaje de validación pasa de "código requerido" a "genera el QR de la etapa N".
- `PartidaDetailPage.tsx` (`BdtView`): junto al código, el QR y su descarga.
- `package.json`: `+ qrcode`.

**Contrato** (`contracts/http/partidas-config.md`)

- `codigoQREsperado`: de "texto no vacío" a "UUID único dentro del juego". Ejemplos del `POST` (:77) y del `GET` (:106) pasan a UUID. Línea de validación (:81) actualizada. `400` cubre ambos rechazos.

**Sin tocar:** Operaciones de Sesión, Puntuaciones, Identity, gateway, móvil, eventos, ranking.
La validación del tesoro no cambia una línea.

## 5. Testing

**Dominio (nuevos, tests-primero):**

- `EtapaBDT.Crear` rechaza `"hola"`, `""` y un UUID mal formado.
- `EtapaBDT.Crear` acepta un UUID y **lo guarda literal** — el test que protege D2.
- `JuegoBDT.Crear` rechaza dos etapas con el mismo código.

**Frontend (nuevos):** el botón genera y pinta el QR; regenerar da un código distinto; el draft
bloquea si no se generó; existe el enlace de descarga; el detalle pinta el QR.

**Tests existentes que romperán, y deben romper:**

| dónde | por qué |
|---|---|
| Partidas: unit / integration / contract (~7 archivos) | Usan `"QR"`, `"QR-1"`. Pasan a UUIDs. |
| `CreatePartidaPage.test.tsx:190,229` | Teclean en el input que desaparece → pulsar el botón. |
| `createPartidaDraft.test.ts` | Cambia el mensaje de validación. |

Afirman *"cualquier texto vale"*: es exactamente la regla que estamos cambiando.

Los tests de Partidas usan `UseInMemoryDatabase`, que no valida esquema — aquí da igual: la regla es
**dominio puro**, sin I/O, y los unitarios la cubren de verdad.

## 6. Alternativas descartadas

| Alternativa | Por qué no |
|---|---|
| Backend genera el código al crear el juego | Doctrinalmente lo más limpio, pero el operador solo vería el QR **después** de guardar y no podría regenerar: Partidas no admite edición (H5). Incumple el requisito. |
| Frontend genera y el backend no valida nada | Cero backend, pero la regla no existiría en ningún lado: el backend seguiría aceptando `"hola"`. Es el anti-patrón que CLAUDE.md prohíbe. |
| Botón que pide el código a un endpoint nuevo | El backend genera, pero al seguir aceptando el código en el `POST` la garantía es idéntica a D1. Añade un endpoint sin cerrar nada. |
| Mostrar el QR en el panel en vivo | Riesgo de filtrar el tesoro (pantalla del operador a la vista) sin ningún beneficio: la validación es automática. |
| Renderizar el QR en el backend | El código aún no existe en el servidor cuando el operador lo genera: exigiría un endpoint extra y un roundtrip para pintar una imagen que el cliente sabe pintar. |
