# Source Priority — UMBRAL

Este archivo define cómo resolver prioridades documentales cuando OpenCode encuentre diferencias entre los documentos fuente.

## Fuentes originales esperadas

| Fuente | Uso |
|---|---|
| `docs/01-project-source/enunciado-proyecto.md` | Marco académico, tecnologías base y expectativas generales del curso. |
| `docs/01-project-source/srs.md` | Fuente principal de requisitos funcionales, no funcionales, reglas de negocio, actores y alcance. |
| `docs/01-project-source/modelo-de-dominio.puml` o `.md` | Fuente principal de conceptos de dominio, subdominios, contextos y agregados. |
| `docs/01-project-source/diagrama-de-clases.puml` o `.md` | Fuente principal de clases, métodos, relaciones, enums y value objects. |
| `docs/01-project-source/lista-microservicios.md` | Fuente inicial para ownership y separación de servicios. |
| `docs/01-project-source/historias-de-usuario-primera-entrega.md` | Fuente para alcance de primera entrega. |

## Prioridad recomendada

1. **SRS**: requisitos, reglas, actores, alcance y criterios de aceptación.
2. **Historias de usuario de primera entrega**: selección real de trabajo para la entrega.
3. **Modelo de dominio**: lenguaje ubicuo, contextos, agregados y reglas internas.
4. **Diagrama de clases**: estructura táctica de clases, relaciones y métodos.
5. **Lista de microservicios**: ownership operativo inicial.
6. **Enunciado académico**: restricciones tecnológicas y expectativas de evaluación.

## Regla ante contradicción

Cuando una regla funcional del SRS contradiga un diseño del modelo o diagrama:

1. No escribir código todavía.
2. Registrar el conflicto en el SDD de la historia.
3. Consultar `known-ambiguities-and-decisions.md`.
4. Elegir una decisión explícita antes de implementar.
5. Si se toma una decisión, actualizar SRS, modelo o ADR según corresponda.

## Regla de fuente derivada

Los archivos de `docs/02-project-context` no son fuente primaria. Son guías operativas. Si se detecta que un archivo de contexto contradice al project-source, debe corregirse el contexto.
