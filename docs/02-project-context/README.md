# UMBRAL — Project Context

Este directorio contiene el contexto operativo del proyecto UMBRAL derivado de `docs/01-project-source`.

Su propósito es permitir que OpenCode trabaje con una versión resumida, normalizada y accionable del SRS, el enunciado, el modelo de dominio, el diagrama de clases, la lista de microservicios y las historias de usuario.

## Regla de uso

- `docs/01-project-source/` conserva la fuente original.
- `docs/02-project-context/` contiene contexto derivado para planificación, diseño e implementación.
- Si hay contradicción entre documentos fuente, no se debe improvisar: revisar `known-ambiguities-and-decisions.md`.
- Ninguna historia debe implementarse directamente desde un prompt: siempre debe existir SDD en `docs/04-sdd/specs/<HU>/`.

## Archivos principales

| Archivo | Propósito |
|---|---|
| `project-brief.md` | Resumen ejecutivo del producto, problema, actores, modos y arquitectura. |
| `srs-summary.md` | Resumen funcional y no funcional del SRS. |
| `business-rules.md` | Reglas de negocio normalizadas por área. |
| `first-delivery-scope.md` | Historias de usuario seleccionadas para la primera entrega. |
| `glossary.md` | Vocabulario ubicuo del dominio. |
| `domain-model-summary.md` | Subdominios, contextos, agregados, eventos y servicios de dominio. |
| `class-design-summary.md` | Resumen del diagrama de clases por contexto. |
| `source-priority.md` | Prioridad documental y reglas ante contradicciones. |
| `known-ambiguities-and-decisions.md` | Decisiones pendientes o contradicciones detectadas. |

## Archivos de diseño

| Archivo | Propósito |
|---|---|
| `design/design-index.md` | Índice de diseño para OpenCode. |
| `design/domain-business-rules.md` | Reglas ubicadas dentro de agregados y servicios de dominio. |
| `design/domain-entities-by-context.md` | Entidades, agregados, value objects y enums por contexto. |
| `design/class-design-by-layer.md` | Traducción del diseño a Clean/Hexagonal Architecture. |
| `design/service-model-impact.md` | Impacto del modelo de dominio sobre microservicios. |
| `design/design-patterns-catalog.md` | Política de patrones de diseño para las features. |

## Regla para OpenCode

Antes de crear o modificar código, OpenCode debe leer como mínimo:

1. `project-brief.md`
2. `srs-summary.md`
3. `business-rules.md`
4. `first-delivery-scope.md`
5. `domain-model-summary.md`
6. `design/domain-entities-by-context.md`
7. `design/class-design-by-layer.md`
8. `design/service-model-impact.md`
9. `known-ambiguities-and-decisions.md`
