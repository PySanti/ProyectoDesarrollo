# Design Context Index — UMBRAL

Este directorio contiene los documentos de diseño derivados del project-source.

## Archivos

| Archivo | Uso |
|---|---|
| `domain-business-rules.md` | Ubicar reglas dentro de agregados, entidades y servicios de dominio. |
| `domain-entities-by-context.md` | Consultar entidades, agregados, value objects y enums por contexto. |
| `class-design-by-layer.md` | Traducir clases a Clean Architecture / Hexagonal Architecture. |
| `service-model-impact.md` | Determinar qué microservicio toca cada feature. |
| `design-patterns-catalog.md` | Elegir patrones de diseño de forma justificada. |

## Uso dentro de SDD

Al crear `docs/04-sdd/specs/<HU>/design.md`, OpenCode debe consultar estos documentos y responder:

1. ¿Qué contexto acotado toca la HU?
2. ¿Qué agregado protege las reglas?
3. ¿Qué servicio o microservicio es dueño?
4. ¿Qué comandos/queries se necesitan?
5. ¿Qué eventos deben publicarse?
6. ¿Qué reglas requieren pruebas?
7. ¿Qué patrón de diseño se justifica y dónde?
