# Auditoría de completitud — estado vs. documentación (2026-07-11)

Rama: `develop` (HEAD `6d36247`)
Tipo: auditoría de cobertura requisito-por-requisito (refresh de `2026-07-06-auditoria-cobertura-requisitos.md`).
Pregunta que responde: **¿cuánto falta exactamente hoy para que el proyecto cumpla al 100% con la documentación?**

## Contexto

Desde la auditoría de cobertura del 2026-07-06 (47% pleno / 78% contando backend listo) se integraron a develop:
Bloque 1 (SP-4 Puntuaciones a/b/c/d), Bloque 2 (re-cableado clientes 2a-2f vía gateway), Bloque 3 de roadmap
(3a rankings push, 3b vista web equipos, 3c RNF-24), Bloque 4A (equipos-admin HU-06/09/48) y 4B (HU-19 backend),
Bloque 5 parcial (RNF-24 sí; RNF-23 por verificar), Bloque 6 (CI + cobertura 48.1% + compose), retiro físico de
trivia-game-service/bdt-game-service, y la remediación de la auditoría de conformidad de Bloques 2+3.

## Inventario (sin cambios vs 2026-07-06)

| Eje | Fuente | Cantidad |
|---|---|---|
| HU | `docs/01-project-source/srs.md` §Historias (HU-01..HU-50) | 50 |
| BR | `docs/02-project-context/business-rules.md` (BR-G/R/E/T/B/C) | 46 |
| RNF | `docs/01-project-source/srs.md` §RNF (RNF-01..RNF-24) | 24 |
| **Total** | | **120** |

## Criterio de estados (endurecido vs 2026-07-06)

- **Pleno**: el actor documentado ejecuta la HU end-to-end en el cliente documentado, contra los 4 servicios
  doctrinales, **vía gateway** (el criterio laxo "sin gateway" de julio-06 ya no aplica: RNF-21 está implementado).
- **Backend listo, cliente sin cablear**: lógica en servicio doctrinal, verde y auditada; falta consumo de cliente.
- **Parcial**: implementado con hueco documentado (se cita el hueco).
- **Falta**: sin implementación en los servicios doctrinales.

## Método

1. **Corrida de cobertura** (única ejecución de gates; suites no se repiten — árbol `6d36247` byte-idéntico a
   `c351398`, verificado verde el 2026-07-11): coverlet sobre los 4 servicios, dato fresco para RNF-09.
2. **5 clústeres paralelos read-only** (subagentes), evidencia `archivo:línea` por requisito:

| Cluster | Cubre | Notas |
|---|---|---|
| A1 | HU-01..09, HU-46..48 + BR-G/R/E | Identity: usuarios, gobernanza, equipos, equipos-admin; incluye RNF-23/BR-R05 (correo asíncrono, por verificar vs SmtpTeamLifecycleNotifier de 4A) |
| A2 | HU-10..24, HU-28..42, HU-45 + BR-T/B | Partidas config + Operaciones runtime en ambos clientes; verificar catch-up UI de HU-19 (aprobación en web, estado Pendiente en mobile), parciales viejos HU-35/38, caveat HU-24 (respuesta correcta al cierre) |
| A3 | HU-25..27, HU-43, HU-44, HU-49, HU-50 + BR-C | Puntuaciones: acumulación, rankings nativos y consolidado, historial, rendimiento de equipo, y su UI web |
| A4 | RNF-01..RNF-24 | Recibe el dato fresco de cobertura para RNF-09 |
| A5 | Cross-check | Matriz de trazabilidad vs realidad, contratos HTTP/eventos vs código, doctrina CLAUDE.md (servicios prohibidos ausentes, estructura graded, límites de servicio) |

3. **Adjudicación**: yo consolido, resuelvo duplicados/discrepancias entre clústeres y asigno estado final.

## Entregables

- Este plan (committed antes de ejecutar).
- `2026-07-11-informe-completitud.md`: tabla de los 120 requisitos con estado + evidencia, % global comparado
  contra el 47%/78% de julio-06, y gaps agrupados en bloques de trabajo restantes con tamaño estimado.
- La remediación de gaps es un slice aparte (no forma parte de esta auditoría).
