# QR generado para la etapa BDT — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el operador pulse un botón y el sistema le entregue el QR del tesoro listo para imprimir, en vez de pedirle que teclee un texto y se fabrique el QR por su cuenta.

**Architecture:** El frontend genera el código (`crypto.randomUUID()`) porque el operador tiene que verlo y poder regenerarlo **antes de guardar**, y Partidas no admite edición. La regla no se queda en el cliente: `EtapaBDT.Crear` exige que lo recibido sea un UUID y `JuegoBDT.Crear` exige unicidad dentro del juego. La validación del tesoro en Operaciones **no se toca**.

**Tech Stack:** .NET 8 (Partidas: dominio puro, sin I/O), xUnit. Frontend: React 18 + TypeScript + Vite, vitest, librería `qrcode`.

**Spec:** `docs/superpowers/specs/2026-07-16-qr-generado-etapa-bdt-design.md`

## Global Constraints

- **El código se guarda LITERAL.** `EtapaBDT.Crear` valida el formato pero **no transforma** el valor (solo el `Trim()` que ya hace). Prohibido normalizar casing o formato (`Guid.Parse(x).ToString()`, `ToLowerInvariant()`, etc.): Operaciones compara `texto == activa.CodigoQREsperado` de forma **exacta** contra el texto embebido en el QR impreso, y normalizar haría que **ningún tesoro validara jamás**.
- **La regla vive SOLO en el dominio.** No añadir el chequeo de UUID a `AgregarJuegoBDTCommandValidator`: sería la misma regla en dos sitios, libre de divergir en silencio. El validador comprueba forma (`NotEmpty`), el dominio comprueba la regla. `EtapaBDTInvalidaException` ya mapea a **400** en `ExceptionHandlingMiddleware`.
- **Unicidad case-insensitive** (`StringComparer.OrdinalIgnoreCase`) — más estricta que la comparación de runtime, a propósito.
- **"Regenerar" solo existe en el formulario de creación.** En el detalle de partida **no hay** botón de regenerar: Partidas no admite edición y no habría dónde escribir el código nuevo.
- **El QR nunca se muestra en sesión activa** ni al participante. No tocar `BdtRuntimePanel.tsx`.
- **Cero cambios** en Operaciones de Sesión, Puntuaciones, Identity, gateway, móvil y eventos.
- La data BDT existente es **descartable**: no hay compatibilidad hacia atrás con códigos de texto libre.
- Ejecutar tests con **un solo `.csproj` por invocación** (MSB1008 si se pasan dos rutas).

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `services/partidas/src/Umbral.Partidas.Domain/Entities/EtapaBDT.cs` | Exige UUID; guarda literal |
| `services/partidas/src/Umbral.Partidas.Domain/Entities/JuegoBDT.cs` | `ValidarCodigosUnicos()` |
| `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs` | Tests de dominio (helper `Etapa` con UUID) |
| `frontend/src/features/partidas/createPartidaDraft.ts` | Mensaje de validación |
| `frontend/src/features/partidas/qrTesoro.ts` | **Nuevo.** Generar código + renderizar data-URL |
| `frontend/src/features/partidas/CreatePartidaPage.tsx` | Botón Generar/Regenerar + preview + descarga |
| `frontend/src/features/partidas/PartidaDetailPage.tsx` | QR + descarga en `BdtView` |
| `contracts/http/partidas-config.md` | `codigoQREsperado` = UUID único |

`qrTesoro.ts` existe como archivo propio para que la generación y el render se testeen sin montar la página entera, y para que las dos pantallas (creación y detalle) compartan una única implementación.

---

## Task 1: El dominio exige UUID y lo guarda literal

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Domain/Entities/EtapaBDT.cs:20-21`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs`

**Interfaces:**
- Consumes: nada.
- Produces: `EtapaBDT.Crear(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)` lanza `EtapaBDTInvalidaException` si `codigoQr` no parsea como `Guid`. Firma sin cambios.

- [ ] **Step 1: Arreglar el helper de los tests existentes**

`JuegoBDTTests.cs:13` usa `qr = "QR-TEXT"`, que con la regla nueva será inválido. Cambiar el helper y la aserción que lo comprueba:

```csharp
    private static string NuevoQr() => Guid.NewGuid().ToString();

    private static EtapaSpec Etapa(int orden, string? qr = null) => new(orden, qr ?? NuevoQr(), 50, 120);
```

Y en `Crear_builds_game_with_stages_and_pendiente_state`, sustituir `Assert.Equal("QR-TEXT", juego.Etapas[0].CodigoQREsperado);` por:

```csharp
        Assert.True(Guid.TryParse(juego.Etapas[0].CodigoQREsperado, out _));
```

- [ ] **Step 2: Escribir los tests que fallan**

Añadir a `JuegoBDTTests.cs`:

```csharp
    [Theory]
    [InlineData("hola")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("QR-TEXT")]
    [InlineData("12345678-1234-1234-1234-12345678")]
    public void Crear_rechaza_un_codigo_que_no_es_uuid(string codigo)
    {
        Assert.Throws<EtapaBDTInvalidaException>(
            () => JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central", new[] { Etapa(1, codigo) }));
    }

    // El codigo se guarda TAL CUAL: Operaciones compara el texto de forma exacta contra el QR
    // impreso. Normalizar el casing aqui haria que ningun tesoro validara jamas.
    [Fact]
    public void Crear_guarda_el_codigo_literal_sin_normalizar()
    {
        var codigo = Guid.NewGuid().ToString().ToUpperInvariant();

        var juego = JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central", new[] { Etapa(1, codigo) });

        Assert.Equal(codigo, juego.Etapas[0].CodigoQREsperado);
    }
```

- [ ] **Step 3: Correr los tests y verificar que fallan por la razón esperada**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj --filter "FullyQualifiedName~JuegoBDTTests" --nologo`

Expected: FAIL. `Crear_rechaza_un_codigo_que_no_es_uuid` falla en los casos `"hola"` y `"QR-TEXT"` (no lanza nada, hoy se aceptan). Los casos `""` y `"   "` ya pasan — los cubre el `IsNullOrWhiteSpace` actual. `Crear_guarda_el_codigo_literal_sin_normalizar` **pasa** desde el principio: es un test de regresión que blinda el comportamiento actual contra el Step 4.

- [ ] **Step 4: Implementar**

En `EtapaBDT.cs`, sustituir la guarda del código (líneas 20-21):

```csharp
        if (string.IsNullOrWhiteSpace(codigoQr))
            throw new EtapaBDTInvalidaException("el codigo QR esperado es requerido.");
```

por:

```csharp
        // El codigo lo genera el sistema (UUID), no lo teclea el operador: un texto libre seria
        // adivinable y no habria forma de garantizar que dos etapas no comparten tesoro.
        if (!Guid.TryParse(codigoQr?.Trim(), out _))
            throw new EtapaBDTInvalidaException("el codigo QR esperado debe ser un identificador generado por el sistema.");
```

**No tocar** la asignación `CodigoQREsperado = codigoQr.Trim()`: guardar literal es un requisito, no un descuido.

- [ ] **Step 5: Correr los tests y verificar que pasan**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj --filter "FullyQualifiedName~JuegoBDTTests" --nologo`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain/Entities/EtapaBDT.cs services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs
git commit -m "feat(partidas): el codigo QR de la etapa debe ser un identificador generado"
```

---

## Task 2: Dos etapas no pueden compartir tesoro

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Domain/Entities/JuegoBDT.cs:43-44`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs`

**Interfaces:**
- Consumes: `EtapaBDT.Crear` de la Task 1.
- Produces: `JuegoBDT.Crear(...)` lanza `EtapaBDTInvalidaException` si dos etapas comparten `CodigoQREsperado` (comparación `OrdinalIgnoreCase`).

- [ ] **Step 1: Escribir el test que falla**

Añadir a `JuegoBDTTests.cs`:

```csharp
    // Sin esto, el QR de la etapa 1 ganaria tambien la etapa 2: ClasificarQr compara el texto
    // decodificado contra el de la etapa activa, y dos etapas con el mismo codigo son
    // indistinguibles.
    [Fact]
    public void Crear_rechaza_dos_etapas_con_el_mismo_codigo()
    {
        var codigo = Guid.NewGuid().ToString();

        Assert.Throws<EtapaBDTInvalidaException>(
            () => JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central",
                new[] { Etapa(1, codigo), Etapa(2, codigo) }));
    }

    [Fact]
    public void Crear_rechaza_dos_codigos_que_solo_difieren_en_mayusculas()
    {
        var codigo = Guid.NewGuid().ToString();

        Assert.Throws<EtapaBDTInvalidaException>(
            () => JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central",
                new[] { Etapa(1, codigo.ToLowerInvariant()), Etapa(2, codigo.ToUpperInvariant()) }));
    }

    [Fact]
    public void Crear_acepta_dos_etapas_con_codigos_distintos()
    {
        var juego = JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central",
            new[] { Etapa(1), Etapa(2) });

        Assert.Equal(2, juego.Etapas.Count);
    }
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj --filter "FullyQualifiedName~JuegoBDTTests" --nologo`

Expected: FAIL. Los dos tests de rechazo no lanzan nada (hoy se aceptan códigos duplicados). `Crear_acepta_dos_etapas_con_codigos_distintos` pasa ya.

- [ ] **Step 3: Implementar**

En `JuegoBDT.cs`, dentro de `Crear`, añadir la llamada junto a la que ya existe:

```csharp
        juego.ValidarOrdenContiguo();
        juego.ValidarCodigosUnicos();
        return juego;
```

Y el método nuevo, justo debajo de `ValidarOrdenContiguo()`:

```csharp
    // OrdinalIgnoreCase a proposito: mas estricto que la comparacion de runtime (que es exacta).
    // Dos codigos que solo difieren en mayusculas serian dos tesoros distintos para el operador
    // pero indistinguibles a simple vista, y eso no ayuda a nadie.
    private void ValidarCodigosUnicos()
    {
        var distintos = _etapas
            .Select(e => e.CodigoQREsperado)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (distintos != _etapas.Count)
            throw new EtapaBDTInvalidaException("cada etapa debe tener un codigo QR distinto.");
    }
```

Verificar que `JuegoBDT.cs` tiene `using System;` y `using System.Linq;` en la cabecera; añadirlos si faltan.

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj --filter "FullyQualifiedName~JuegoBDTTests" --nologo`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain/Entities/JuegoBDT.cs services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs
git commit -m "feat(partidas): dos etapas de un juego BDT no pueden compartir tesoro"
```

---

## Task 3: Arreglar el resto de tests de Partidas

Los demás tests usan `"QR"`, `"QR-1"` como código y ahora fallan. **Que rompan es correcto**: afirmaban que cualquier texto vale, y esa es justo la regla que cambiamos.

**Files:**
- Modify: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoBDTCommandHandlerTests.cs`
- Modify: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoBDTCommandValidatorTests.cs`
- Modify: `services/partidas/tests/Umbral.Partidas.UnitTests/Api/PartidasControllerTests.cs`
- Modify: `services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaPersistenceTests.cs`
- Modify: `services/partidas/tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs`

- [ ] **Step 1: Localizar todos los sitios**

Run: `grep -rn "\"QR\"\|\"QR-1\"\|\"QR-TEXT\"\|\"TESORO" services/partidas/tests --include=*.cs`

- [ ] **Step 2: Sustituir cada literal por un UUID**

En cada sitio, cambiar el literal por `Guid.NewGuid().ToString()`.

**Excepción — `AgregarJuegoBDTCommandValidatorTests`:** ese validador **no cambia** (constraint global). Sus tests comprueban `NotEmpty`, así que un código `"QR"` sigue siendo válido **para el validador**. Solo hay que tocarlos si el test llega hasta el dominio. Leer cada test antes de tocarlo: si solo ejercita el validador, **dejarlo como está**.

Para los tests de contrato que construyen JSON, usar un UUID literal fijo y legible en vez de generarlo, para que el JSON esperado sea estable:

```csharp
    private const string QrEtapa1 = "11111111-1111-1111-1111-111111111111";
```

- [ ] **Step 3: Correr cada proyecto de tests por separado**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj --nologo`
Run: `dotnet test services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj --nologo`
Run: `dotnet test services/partidas/tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj --nologo`

Expected: PASS los tres.

- [ ] **Step 4: Commit**

```bash
git add services/partidas/tests
git commit -m "test(partidas): los codigos QR de los tests son identificadores validos"
```

---

## Task 4: Contrato

**Files:**
- Modify: `contracts/http/partidas-config.md:77,81,106`

- [ ] **Step 1: Actualizar el ejemplo del POST (línea 77)**

```json
    { "orden": 1, "codigoQREsperado": "11111111-1111-1111-1111-111111111111", "puntaje": 50, "tiempoLimiteSegundos": 120 }
```

- [ ] **Step 2: Actualizar la línea de validación (línea 81)**

Sustituir `each stage: non-empty codigoQREsperado, puntaje > 0, tiempoLimiteSegundos > 0.` por:

```markdown
- Non-empty `areaBusqueda`; at least one stage with contiguous `orden` from 1; each stage: `codigoQREsperado` **must be a UUID generated by the client** (the operator does not type it — the web app generates it and renders the printable QR) and **unique within the game** (case-insensitive), `puntaje > 0`, `tiempoLimiteSegundos > 0`. A non-UUID or duplicated code → `400`.
```

- [ ] **Step 3: Actualizar el ejemplo del GET (línea 106)**

```json
      "bdt": { "areaBusqueda": "Plaza central", "etapas": [ { "etapaBDTId": "<guid>", "orden": 1, "codigoQREsperado": "11111111-1111-1111-1111-111111111111", "puntajeAsignado": 50, "tiempoLimiteSegundos": 120 } ] } }
```

- [ ] **Step 4: Verificar que los contract tests siguen en verde**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj --nologo`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add contracts/http/partidas-config.md
git commit -m "docs(contracts): codigoQREsperado es un UUID unico dentro del juego"
```

---

## Task 5: El módulo de QR del frontend

**Files:**
- Create: `frontend/src/features/partidas/qrTesoro.ts`
- Create: `frontend/src/features/partidas/qrTesoro.test.ts`
- Modify: `frontend/package.json`

**Interfaces:**
- Consumes: nada.
- Produces:
  - `generarCodigoTesoro(): string` → un UUID nuevo.
  - `renderizarQrDataUrl(codigo: string): Promise<string>` → data-URL PNG del QR.
  - `nombreArchivoQr(orden: number): string` → `"tesoro-etapa-1.png"`.

- [ ] **Step 1: Instalar la dependencia**

```bash
cd frontend && npm install qrcode && npm install --save-dev @types/qrcode
```

- [ ] **Step 2: Escribir los tests que fallan**

Crear `frontend/src/features/partidas/qrTesoro.test.ts`:

```ts
import { describe, expect, it } from "vitest";
import { generarCodigoTesoro, nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe("generarCodigoTesoro", () => {
  it("genera un UUID", () => {
    expect(generarCodigoTesoro()).toMatch(UUID_RE);
  });

  it("genera un codigo distinto cada vez", () => {
    expect(generarCodigoTesoro()).not.toBe(generarCodigoTesoro());
  });
});

describe("renderizarQrDataUrl", () => {
  it("devuelve un data-URL PNG", async () => {
    const dataUrl = await renderizarQrDataUrl(generarCodigoTesoro());
    expect(dataUrl.startsWith("data:image/png;base64,")).toBe(true);
  });
});

describe("nombreArchivoQr", () => {
  it("nombra el archivo por el orden de la etapa", () => {
    expect(nombreArchivoQr(3)).toBe("tesoro-etapa-3.png");
  });
});
```

- [ ] **Step 3: Correr los tests y verificar que fallan**

Run: `cd frontend && npx vitest run src/features/partidas/qrTesoro.test.ts`
Expected: FAIL — `Failed to resolve import "./qrTesoro"`.

- [ ] **Step 4: Implementar**

Crear `frontend/src/features/partidas/qrTesoro.ts`:

```ts
import QRCode from "qrcode";

// El codigo del tesoro lo genera el cliente porque el operador tiene que verlo y poder
// regenerarlo antes de guardar, y Partidas no admite editar un juego ya creado. El backend
// no se fia: EtapaBDT.Crear exige que sea un UUID.
export function generarCodigoTesoro(): string {
  return crypto.randomUUID();
}

// El data-URL sirve a la vez para el <img> de la previsualizacion y para el href del enlace
// de descarga: no hace falta renderizar dos veces ni pasar por un blob.
export function renderizarQrDataUrl(codigo: string): Promise<string> {
  return QRCode.toDataURL(codigo, { width: 320, margin: 2 });
}

export function nombreArchivoQr(orden: number): string {
  return `tesoro-etapa-${orden}.png`;
}
```

- [ ] **Step 5: Correr los tests y verificar que pasan**

Run: `cd frontend && npx vitest run src/features/partidas/qrTesoro.test.ts`
Expected: PASS (4 tests).

Si `crypto.randomUUID` no existe en el entorno de jsdom, añadir a `frontend/vitest.setup.ts` (o crearlo y registrarlo en `vite.config.ts` bajo `test.setupFiles`):

```ts
import { webcrypto } from "node:crypto";

if (!globalThis.crypto?.randomUUID) {
  Object.defineProperty(globalThis, "crypto", { value: webcrypto });
}
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/features/partidas/qrTesoro.ts frontend/src/features/partidas/qrTesoro.test.ts frontend/package.json frontend/package-lock.json
git commit -m "feat(web): modulo de generacion y render del QR del tesoro"
```

---

## Task 6: El botón en el formulario de creación

**Files:**
- Modify: `frontend/src/features/partidas/CreatePartidaPage.tsx:686-693`
- Modify: `frontend/src/features/partidas/createPartidaDraft.ts:151`
- Modify: `frontend/src/features/partidas/CreatePartidaPage.test.tsx:190,229`
- Modify: `frontend/src/features/partidas/createPartidaDraft.test.ts:172-175`

**Interfaces:**
- Consumes: `generarCodigoTesoro()`, `renderizarQrDataUrl(codigo)`, `nombreArchivoQr(orden)` de la Task 5.
- Produces: el draft sigue llevando `codigoQREsperado: string` (vacío = sin generar). El `POST` no cambia de forma.

- [ ] **Step 1: Actualizar el mensaje de validación del draft**

En `createPartidaDraft.ts:151`, el bloque actual comprueba `if (!etapa.codigoQREsperado.trim())`. Dejar la condición y cambiar solo el mensaje al que ve el operador ahora:

```ts
      errores.push(`Genera el código QR de la etapa ${i + 1}`);
```

Ajustar la aserción del mensaje en `createPartidaDraft.test.ts` (test `rechaza etapa sin codigo QR`) al texto nuevo.

- [ ] **Step 2: Escribir el test que falla**

En `CreatePartidaPage.test.tsx`, sustituir en los dos tests (líneas ~190 y ~229) la línea:

```tsx
    await user.type(within(bdtRegion).getByLabelText(/c[oó]digo qr esperado/i), "QR-1");
```

por:

```tsx
    await user.click(within(bdtRegion).getByRole("button", { name: /^generar qr/i }));
```

Y añadir un test nuevo. **No existe un helper `addBdtGame`**: el montaje del juego BDT está inline en los tests actuales (ver líneas ~185-192). Los helpers que sí existen y hay que reutilizar son `fillValidHeader(user)` y `addValidTriviaGame(user)`. El test nuevo replica ese montaje:

```tsx
  it("genera el QR de la etapa y ofrece descargarlo", async () => {
    const user = userEvent.setup();

    render(<CreatePartidaPage accessToken="token-xyz" />);
    await fillValidHeader(user);
    await addValidTriviaGame(user);

    await user.click(screen.getByTestId("btn-agregar-bdt"));
    const bdtRegion = screen.getByRole("region", { name: "Juego 2" });
    await user.type(within(bdtRegion).getByLabelText(/[aá]rea de b[uú]squeda/i), "Patio");
    await user.click(within(bdtRegion).getByRole("button", { name: /agregar etapa/i }));

    await user.click(within(bdtRegion).getByRole("button", { name: /^generar qr/i }));

    expect(await within(bdtRegion).findByRole("img", { name: /qr del tesoro/i })).toBeInTheDocument();
    expect(within(bdtRegion).getByRole("link", { name: /descargar qr/i })).toHaveAttribute(
      "download",
      "tesoro-etapa-1.png"
    );
    expect(within(bdtRegion).getByRole("button", { name: /regenerar qr/i })).toBeInTheDocument();
  });
```

- [ ] **Step 3: Correr los tests y verificar que fallan**

Run: `cd frontend && npx vitest run src/features/partidas/CreatePartidaPage.test.tsx`
Expected: FAIL — `Unable to find an accessible element with the role "button" and name /^generar qr/i`.

- [ ] **Step 4: Implementar**

En `CreatePartidaPage.tsx`, sustituir el bloque del `<label>` + `<input>` (líneas 686-693) por el botón, la previsualización y la descarga. Cada etapa guarda su data-URL en un estado local indexado por el `codigoQREsperado`, para no re-renderizar el QR en cada tecla del formulario:

```tsx
              <div className="stack">
                <button
                  type="button"
                  onClick={async () => {
                    const codigo = generarCodigoTesoro();
                    patchEtapa(eIndex, { codigoQREsperado: codigo });
                    setQrDataUrls((prev) => ({ ...prev, [codigo]: await renderizarQrDataUrl(codigo) }));
                  }}
                >
                  {etapa.codigoQREsperado ? `Regenerar QR etapa ${n}` : `Generar QR etapa ${n}`}
                </button>

                {etapa.codigoQREsperado && qrDataUrls[etapa.codigoQREsperado] ? (
                  <>
                    <img
                      src={qrDataUrls[etapa.codigoQREsperado]}
                      alt={`QR del tesoro de la etapa ${n}`}
                      width={160}
                      height={160}
                    />
                    <a
                      href={qrDataUrls[etapa.codigoQREsperado]}
                      download={nombreArchivoQr(n)}
                    >
                      Descargar QR etapa {n}
                    </a>
                  </>
                ) : null}
              </div>
```

Declarar el estado junto al resto de `useState` del componente:

```tsx
  const [qrDataUrls, setQrDataUrls] = useState<Record<string, string>>({});
```

E importar arriba:

```tsx
import { generarCodigoTesoro, nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";
```

- [ ] **Step 5: Correr los tests y verificar que pasan**

Run: `cd frontend && npx vitest run src/features/partidas/CreatePartidaPage.test.tsx src/features/partidas/createPartidaDraft.test.ts`
Expected: PASS.

- [ ] **Step 6: Verificar que no queda ningún input de QR**

Run: `grep -n "codigoQREsperado" frontend/src/features/partidas/CreatePartidaPage.tsx`
Expected: solo apariciones dentro del bloque nuevo (botón/preview/descarga). Ningún `<input>`.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/features/partidas/CreatePartidaPage.tsx frontend/src/features/partidas/CreatePartidaPage.test.tsx frontend/src/features/partidas/createPartidaDraft.ts frontend/src/features/partidas/createPartidaDraft.test.ts
git commit -m "feat(web): el operador genera el QR del tesoro en vez de teclearlo"
```

---

## Task 7: Reimprimir el QR desde el detalle

**Files:**
- Modify: `frontend/src/features/partidas/PartidaDetailPage.tsx:203-232`
- Modify: `frontend/src/features/partidas/PartidaDetailPage.test.tsx:76`

**Interfaces:**
- Consumes: `renderizarQrDataUrl(codigo)`, `nombreArchivoQr(orden)` de la Task 5.
- Produces: nada.

Cero backend: `GET /partidas/{id}` ya devuelve `codigoQREsperado` y el gateway ya enruta `/partidas/**` solo a `OperadorOAdministrador`. **Sin botón de regenerar** (constraint global).

- [ ] **Step 1: Escribir el test que falla**

En `PartidaDetailPage.test.tsx`, cambiar el código del fixture (línea 76) de `"TESORO-1"` a un UUID:

```tsx
            codigoQREsperado: "11111111-1111-1111-1111-111111111111",
```

Y añadir:

```tsx
  it("muestra el QR de cada etapa para reimprimirlo", async () => {
    renderPage();

    expect(await screen.findByRole("img", { name: /qr del tesoro de la etapa 1/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /descargar qr etapa 1/i })).toHaveAttribute(
      "download",
      "tesoro-etapa-1.png"
    );
  });

  it("no ofrece regenerar el QR", async () => {
    renderPage();
    await screen.findByRole("img", { name: /qr del tesoro de la etapa 1/i });

    expect(screen.queryByRole("button", { name: /regenerar/i })).not.toBeInTheDocument();
  });
```

`renderPage()` es el helper que ya existe en ese archivo (línea 21) y funciona sin argumentos; reutilizarlo tal cual. El fixture de la etapa tiene `etapaBDTId: "e1"` y `orden: 1`.

- [ ] **Step 2: Correr los tests y verificar que fallan**

Run: `cd frontend && npx vitest run src/features/partidas/PartidaDetailPage.test.tsx`
Expected: FAIL — no encuentra el `img` con ese nombre accesible.

- [ ] **Step 3: Implementar**

En `PartidaDetailPage.tsx`, dentro de `BdtView`, renderizar los QR al montar y añadir una columna a la tabla. Añadir arriba del `return`:

```tsx
  const [qrDataUrls, setQrDataUrls] = useState<Record<string, string>>({});

  useEffect(() => {
    let vigente = true;
    Promise.all(
      etapas.map(async (e) => [e.etapaBDTId, await renderizarQrDataUrl(e.codigoQREsperado)] as const)
    ).then((pares) => {
      if (vigente) setQrDataUrls(Object.fromEntries(pares));
    });
    return () => {
      vigente = false;
    };
  }, [etapas]);
```

Añadir la cabecera de columna tras `<th scope="col">QR esperado</th>`:

```tsx
              <th scope="col">QR</th>
```

Y la celda tras `<td className="mono">{etapa.codigoQREsperado}</td>`:

```tsx
                <td>
                  {qrDataUrls[etapa.etapaBDTId] ? (
                    <>
                      <img
                        src={qrDataUrls[etapa.etapaBDTId]}
                        alt={`QR del tesoro de la etapa ${etapa.orden}`}
                        width={96}
                        height={96}
                      />
                      <a href={qrDataUrls[etapa.etapaBDTId]} download={nombreArchivoQr(etapa.orden)}>
                        Descargar QR etapa {etapa.orden}
                      </a>
                    </>
                  ) : null}
                </td>
```

Importar arriba:

```tsx
import { nombreArchivoQr, renderizarQrDataUrl } from "./qrTesoro";
```

Verificar que `useEffect` y `useState` están en el import de React del archivo.

- [ ] **Step 4: Correr los tests y verificar que pasan**

Run: `cd frontend && npx vitest run src/features/partidas/PartidaDetailPage.test.tsx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/features/partidas/PartidaDetailPage.tsx frontend/src/features/partidas/PartidaDetailPage.test.tsx
git commit -m "feat(web): el detalle de partida muestra el QR de cada etapa para reimprimirlo"
```

---

## Task 8: Verificación end-to-end

- [ ] **Step 1: Suite completa de Partidas**

Run: `dotnet test services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj --nologo`
Run: `dotnet test services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj --nologo`
Run: `dotnet test services/partidas/tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj --nologo`

Expected: PASS los tres.

- [ ] **Step 2: Suite y typecheck del frontend**

Run: `cd frontend && npm test`
Run: `cd frontend && npm run build`

Expected: PASS y build limpio.

- [ ] **Step 3: Confirmar que no se toco nada fuera de alcance**

Run: `git diff --name-only <commit-base>..HEAD`

Expected: solo `services/partidas`, `frontend`, `contracts/http/partidas-config.md` y `docs/`. **Ningún** archivo de `services/operaciones-sesion`, `services/puntuaciones`, `services/identity-service`, `gateway` o `mobile`.

- [ ] **Step 4: Ejercitar el flujo real**

Levantar la infraestructura, Partidas, el gateway y el frontend. Como operador:

1. Crear una partida con un juego BDT y dos etapas.
2. Pulsar "Generar QR" en cada etapa → aparece el QR → descargarlos.
3. Pulsar "Regenerar QR" en la etapa 1 → el QR cambia.
4. Guardar la partida → `201`.
5. Abrir el detalle → los dos QR se ven y se descargan, y **no** hay botón de regenerar.
6. Abrir uno de los PNG descargados con un lector de QR y comprobar que el texto decodificado es **exactamente** el `codigoQREsperado` que muestra el detalle. Este paso es el que prueba el requisito de "guardar literal": si no coinciden, ningún tesoro validaría en juego.

- [ ] **Step 5: Actualizar trazabilidad**

Añadir la fila correspondiente a `docs/04-sdd/SPECS-LIST.md` y `docs/04-sdd/traceability-matrix.md` (HU-28 · RF-25 · BR-B03 refinada), siguiendo el formato de las filas existentes.

- [ ] **Step 6: Commit**

```bash
git add docs/04-sdd/SPECS-LIST.md docs/04-sdd/traceability-matrix.md
git commit -m "docs: trazabilidad del QR generado para la etapa BDT"
```
