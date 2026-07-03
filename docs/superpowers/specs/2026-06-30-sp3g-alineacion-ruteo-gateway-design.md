# SP-3g — Alineación de ruteo Operaciones de Sesión ↔ gateway (prefijo)

- **Fecha:** 2026-06-30
- **Slice:** SP-3g (corrige la deuda de prefijo detectada en el review final de SP-3f-2)
- **Servicio dueño:** Operaciones de Sesión (+ verificación en el gateway; sin cambios de gateway)
- **Cliente objetivo:** backend
- **Estado:** Diseño aprobado — pendiente de plan de implementación
- **Rama:** `feature/code-migration-SP-3`

## Contexto y problema

El gateway YARP es el único punto de entrada. La ruta del gateway
`/operaciones-sesion/{**catch-all}` **no** lleva un transform `PathRemovePrefix`,
así que YARP reenvía el **path completo** al servicio. El servicio de referencia
que funciona, **Partidas**, hostea sus controllers bajo `[Route("partidas")]`, de
modo que el path reenviado `/partidas/...` calza con el servicio. **Operaciones de
Sesión es el único outlier:** su `SesionesController` usa `[Route("")]` (hostea en
la raíz: `partidas/{id}/...`, `mi-sesion`, ...) y el hub SignalR se mapea en
`/hubs/sesion`. Resultado: a través del gateway, `/operaciones-sesion/...` y
`/operaciones-sesion/hubs/sesion` llegan al servicio como paths que **no** matchean
ninguna ruta → **404**, y todos los endpoints de Operaciones (más el hub de
SP-3f-2) son inalcanzables por el único punto de entrada. Hoy solo están cubiertos
por tests directos-al-servicio (que golpean los paths sin prefijo).

Esta deuda fue marcada como *Important* en el review final de SP-3f-2 y adjudicada
como pre-existente/fuera-de-alcance de aquel slice. SP-3g la resuelve.

Fuentes de contexto que fijan el diseño:
- Partidas: `[Route("partidas")]` (convención que funciona). Operaciones:
  `[Route("")]` (outlier). identity/puntuaciones aún no tienen controllers de
  dominio (solo `HealthController`), así que no hay nada que alinear ahí.
- Los tests de integración del gateway **no alcanzan un downstream vivo** (los
  destinos apuntan a `localhost:50xx`, que no corren en el test); solo asertan
  autorización (401 antes de proxear) y configuración. Un e2e real
  gateway→servicio-vivo **no es testeable** en el harness actual.
- El contrato HTTP (`contracts/http/operaciones-sesion-api.md`) ya documenta los
  paths públicos **prefijados** (`/operaciones-sesion/partidas/...`, `/mi-sesion`
  documentado como `/operaciones-sesion/mi-sesion`), es decir el destino correcto.

## Enfoque elegido

**Alinear Operaciones a la convención de Partidas:** el servicio hostea bajo su
prefijo de nombre; el gateway sigue siendo reenvío-puro (sin transforms). Esto
elimina el outlier permanentemente, mantiene el gateway uniforme ("routes only") y
hace que el path interno del servicio coincida 1:1 con el contrato público.

Alternativas descartadas:
- **`app.UsePathBase("/operaciones-sesion")` en el servicio:** una línea, cero
  churn de tests, pero deja el servicio laxo (responde a paths prefijados **y** sin
  prefijo) y usa una convención distinta a Partidas.
- **`PathRemovePrefix` en el gateway:** una línea de config, pero rompe la simetría
  (transform solo para Operaciones), aleja al gateway de "routes only" y deja dos
  convenciones opuestas conviviendo (trampa para el próximo servicio).

## Cambios de producción (servicio Operaciones)

1. `SesionesController`: `[Route("")]` → `[Route("operaciones-sesion")]`. Las
   acciones son relativas (`partidas/{partidaId:guid}/...`, `mi-sesion`,
   `pregunta-actual/...`, `etapa-actual/...`, etc.) y pasan a resolver bajo
   `operaciones-sesion/...`, coincidiendo exactamente con el contrato ya
   documentado.
2. Hub SignalR: `app.MapHub<SesionHub>("hubs/sesion")` →
   `app.MapHub<SesionHub>("operaciones-sesion/hubs/sesion")`.
3. Guard JWT del servicio (`OnMessageReceived`, rama Keycloak configurada):
   `StartsWithSegments("/hubs/sesion")` → `StartsWithSegments("/operaciones-sesion/hubs/sesion")`
   (con el path completo reenviado, el path que ve el servicio es el gateway-facing).
4. `HealthController` **no cambia** (`[Route("health")]`): es un liveness
   service-local, no proxeado vía `/operaciones-sesion/{**}` — mismo trato que en
   Partidas; fuera de alcance.

## Gateway — sin cambios

- La ruta `/operaciones-sesion/{**catch-all}` (policy `Default`) sin transform ya
  es correcta para este enfoque.
- El guard JWT del gateway (`KeycloakJwtExtensions.OnMessageReceived`) ya usa
  `StartsWithSegments("/operaciones-sesion/hubs")` — correcto, sin cambios.
- Se mantiene el gateway como reenvío-puro uniforme para los cuatro servicios.

## Estrategia de pruebas

- **ContractTests (e2e HTTP vía `OperacionesSesionWebFactory`/WebApplicationFactory):**
  actualizar todos los literales de path (~107, en `SesionEndpointsTests`,
  `TriviaRuntimeEndpointsTests`, `BdtRuntimeEndpointsTests`,
  `ReconexionEndpointsTests`) al path público **prefijado**. Introducir una
  **const base compartida** (p. ej. `Rutas.Base = "/operaciones-sesion"` en una
  clase estática del proyecto de test) y expresar los paths a través de ella, para
  centralizar el prefijo en un solo lugar (defensivo si el ADR de migración renombra
  el slug). El cambio de `[Route]` invalida los 4 archivos e2e a la vez, así que se
  actualizan de forma atómica para mantener la suite verde.
- **Realidad del harness:** un e2e real gateway→servicio-vivo no es posible aquí.
  La prueba significativa es que los ContractTests golpeen el **path público
  prefijado** contra el host real del servicio → demuestran que el servicio responde
  donde el gateway reenvía. El proxy-vivo end-to-end queda como **gap documentado**
  (misma clase que el gap WS de SP-3f-2).
- **RealtimeContractTests / RealtimeWiringTests:** el doc ya usa
  `/operaciones-sesion/hubs/sesion`; tras mapear el hub ahí, ambos quedan
  consistentes (ajustar cualquier assert de path del hub si aplica).
- **Gateway:** `Hub_de_operaciones_requiere_autenticacion` (401 anon en
  `/operaciones-sesion/hubs/sesion`) sigue válido. Opcional: un test que asevere que
  la ruta `operaciones-sesion` del gateway **no** define `PathRemovePrefix` (fija el
  contrato de reenvío-puro; una regresión que añadiera un transform lo rompería).

## Contrato y documentación

- Eliminar/actualizar el **caveat de SP-3f-2** en la sección Realtime del contrato
  (`operaciones-sesion-api.md`) que decía que la ruta vía gateway estaba pendiente
  del fix de prefijo — SP-3g la habilita.
- El resto del contrato ya documenta paths prefijados → sin cambios de contenido.
- Fila de traceability SP-3g (carve-out: se escribe, no se commitea).
- Al cerrar el slice, actualizar la memoria `sp3f2-signalr-gateway-prefix` (blocker
  resuelto).

## Fuera de alcance / forward-looking

- No se refactoriza Partidas (ya cumple la convención).
- identity y puntuaciones: cuando ganen controllers de dominio, deben usar
  `[Route("<nombre-servicio>")]` para mantener la convención uniforme (nota
  forward-looking; sin trabajo en este slice).
- No se construye un harness de e2e gateway→servicio-vivo (queda como gap
  documentado; sería su propia inversión de infraestructura de test).

## Riesgos

- **Churn amplio de paths de test (~107 literales):** mecánico y auto-checkeable
  (un path olvidado produce un 404 ruidoso, no un fallo silencioso). La const base
  compartida reduce el riesgo de divergencia y el costo de cambios futuros.
- **Orden del middleware para el guard WS:** el guard del servicio corre sobre el
  path que ve el pipeline; como el gateway reenvía el path completo (sin strip), el
  servicio ve `/operaciones-sesion/hubs/sesion` directamente — no hace falta
  `UsePathBase`. Verificar que el `StartsWithSegments` actualizado matchea ese path.

## Descomposición tentativa (se detalla en writing-plans)

1. `SesionesController` → `[Route("operaciones-sesion")]` + const `Rutas.Base` +
   actualizar los 4 archivos e2e de ContractTests (atómico; suite verde).
2. Hub `MapHub("operaciones-sesion/hubs/sesion")` + guard JWT del servicio
   prefijado + confirmar Realtime/Wiring tests.
3. Quitar el caveat obsoleto del contrato + (opcional) test de config del gateway
   (sin PathRemovePrefix) + fila de traceability (carve-out).
