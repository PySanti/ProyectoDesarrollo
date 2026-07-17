# Mejora de validación de inputs (web + mobile + backend) — design

Fecha: 2026-07-16 · Rama: feature/fixes-santiago

## Alcance

Validación consistente de inputs de formulario en las tres capas, con feedback en tiempo real por
campo en los clientes y el backend como autoridad. Nace de un bug: crear usuario con nombre `****`
devuelve "Error de integración con Keycloak" (502) porque ni el front ni el validator del backend
rechazan un nombre compuesto solo de símbolos — llega a Keycloak y explota.

HUs: HU-01 / HU-02 (crear / editar usuario). Formularios cubiertos además: crear/renombrar equipo
(admin), crear partida (header + preguntas Trivia + etapas BDT). Fuente de reglas: RB-U0x, BR-E,
BR-T, BR-B.

## Decisiones tomadas

- Alcance = web completo + mobile.
- Backend autoridad; front espejo (usabilidad); arreglar el mensaje de error.
- Feedback en vivo: reglas de **formato** validan en cada tecla (campo dirty); "obligatorio" se
  muestra al blur o al submit — no arrancar en rojo.
- Reglas de caracteres de nombre: **defaults** (el SRS no las fija), documentadas abajo.

## Reglas (fuente única, misma semántica en las tres capas)

| Campo | Regla |
|---|---|
| Nombre de usuario | requerido; trim; 2–120; **≥1 letra Unicode** (rechaza `****` / solo símbolos / solo espacios) |
| Correo | requerido; formato email real; ≤320; normalizar a minúsculas (backend ya lo hace) |
| Nombre de equipo | requerido; trim; bounds de `CrearEquipoCommandValidator`; ≥1 letra |
| Nombre de partida | requerido; ≥1 letra; bounds de `createPartidaDraft.validateHeader` |
| Texto pregunta / opción | requerido; no solo espacios; bounds existentes |
| Área BDT / QR esperado / etapa | requerido; no solo espacios |
| Números (min/max part., puntaje, tiempo) | enteros > 0; `min ≤ max` (ya en `createPartidaDraft`) |

La regla "≥1 letra" es el corazón del fix.

## Arquitectura

### Backend (Identity, y Partidas donde aplique)
- Extensión FluentValidation `HumanText()` en `Application/Validators/` (trim + ≥1 letra Unicode +
  no-solo-símbolos, mensaje claro). Reusada en los validators de nombre.
- `ExceptionHandlingMiddleware`: `KeycloakIntegrationException` sigue 502 pero con mensaje
  entendible, sin filtrar detalle interno. Defensa en profundidad — con ≥1 letra, `****` ya no llega.
- La tubería de error por campo **ya existe**: `UsersController.ValidateAsync` →
  `ValidationProblemDetails` (400 con `errors` por campo). No se crea infraestructura nueva.

### Web
- `frontend/src/shared/validation.ts` — validadores puros `(value) => string | null` + hook mínimo
  `useFieldValidation` (dirty/blur/submit). Reusa semántica de `createPartidaDraft.ts`.
- `frontend/src/shared/Field.tsx` — espejo del `Field` de mobile: label + input + error, con
  `aria-invalid` + `aria-describedby`. Clase de error en `components.css`
  (`input[aria-invalid="true"]` borde `--danger`). No romper `data-testid`.
- `identityApi.ts` — parsear `ValidationProblemDetails.errors` del 400 y exponerlo por campo.
- Cablear formularios uno por uno: CreateUser → UserManagement → TeamsAdmin → CreatePartida.

### Mobile
- Reusar `mobile/src/shared/ui/Field.tsx` (ya tiene error/hint/borde). No crear componente nuevo.
- `mobile/src/shared/validation.js` — mismas reglas puras. Subir nivel de los `*Flow.js`.
- **NO tocar** archivos reservados de la otra sesión: `transferLeadershipFlow.js`,
  `TransferLeadershipScreen*`, sus tests.

## Testing
- Backend: casos de validator (`****`, solo espacios, solo símbolos → inválido; normal → válido);
  suite `Umbral.IdentityService.sln` verde. Repro vivo: `POST /identity/users name:"****"` → 400.
- Web: `validation.test.ts` (reglas puras) + integración por formulario; `tsc --noEmit -p tsconfig.app.json`.
- Mobile: `node --test tests/*.test.js` + `npm run typecheck`.

## Riesgos y caveats
- Reglas de nombre son defaults; si el usuario quiere otras (números en nombres, largo mínimo), se
  ajustan acá antes de codear.
- Partidas puede usar `ValidationBehavior` (pipeline MediatR) en vez del patrón controller de
  Identity — seguir el patrón del servicio, no imponer el de Identity.
- Rebuild de contenedor Identity: usar `--no-deps` para no re-disparar `keycloak-config`.

## Orden de entrega
SDD → Fase A (backend, mata el bug) → Fase B (web, empezando por CreateUser) → Fase C (mobile).
Un commit por fase; sin push/merge sin permiso.

## Fase D — Espacio de error estable (web + mobile)
El `Field` renderizaba el mensaje condicionalmente y el layout saltaba. Ahora el contenedor del
mensaje se renderiza siempre con alto de una línea reservado; el error/hint aparece adentro sin
mover el panel. Web: `span.field-msg` (`min-height: 1.15rem`). Mobile: `View` con `minHeight 18`.

## Fase E — Validación en el login de Keycloak (solo por correo, sin real-time)
El theme es CSS-only a propósito. Se agrega validación vía script de theme, sin tocar `.ftl`:
`resources/js/umbral-login.js` marca `#username` como `type=email` + `required` y `#password` como
`required` (confirmado: login solo por correo). Validación nativa del navegador al enviar, sin
feedback en vivo. `theme.properties` gana `scripts=js/umbral-login.js`. Verificado: la página de
login sirve el `<script>` y los campos objetivo existen.

## Fase F — Cierre de huecos backend Partidas + área BDT (autoridad)
El front bloqueaba nombre de partida solo-símbolos pero el backend de Partidas no (violaba
"backend autoridad"). Se agrega `ReglasTextoHumano.TextoHumano` propio de Partidas (aislamiento de
servicio — no importa el de Identity; `int? maximo = null` para omitir cota) y se aplica a
`NombrePartida` (≤120) y `AreaBusqueda` (sin cota, texto libre BR-B02). Frontend
`createPartidaDraft.validateJuego` valida área ≥1 letra (simetría con el header). Texto de
pregunta/opción/QR se queda en `NotEmpty()`: puede ser legítimamente numérico/simbólico.
