
# Git

## Comando: partir desde `develop`

```bash
git switch develop
git pull origin develop
git switch -c feature/HU-X-slug
```

## Previo a implementacion

Pista A — Funcionalidad nueva (feature)

Aquí el flujo SDD es obligatorio (regla de CLAUDE.md: nunca implementar desde un prompt vago).

PRE (planificar):
1. /create-feature-sdd — solo si la HU aún no tiene carpeta SDD completa (spec.md → design.md → tasks.md → acceptance.md).
2. /plan-feature — valida que la HU esté en SPECS-LIST.md, no esté deprecada y confirma el microservicio dueño.
3. /review-boundaries — solo si la feature parece tocar más de un microservicio (decide propiedad y vía HTTP/RabbitMQ/SignalR antes de codificar).

DURANTE (desarrollar):
4. /implement-task — una sola tarea del tasks.md por vez. Repítelo tarea por tarea.
- Apóyate en las skills de la capa: ddd-modeling, cqrs-mediatr, efcore-postgres, rabbitmq-events, websocket-signalr, react-native-mobile, contract-design, testing.

POST (cerrar):
5. /review-feature — cumplimiento SDD, arquitectura, CQRS, ubicación de reglas.
6. /update-traceability — actualiza traceability-matrix.md con el nuevo estado.

---
Pista B — Fix / bug / algo pequeño

Aquí no necesitas la ceremonia SDD completa (no es una HU nueva; es código existente). El flujo se aligera:

PRE:
- Normalmente ningún comando del repo. Identifica el archivo/causa directamente.
- Excepción: si el bug toca comportamiento documentado en una HU, conviene leer su carpeta SDD (skill umbral-context si necesitas cargar contexto de dominio).
- Excepción: si el fix cruza microservicios, /review-boundaries.

DURANTE:
- Implementa el cambio directamente (no uses /implement-task, que está atado a una tarea de tasks.md).
- Usa la skill de la capa solo si aplica (testing para añadir/ajustar pruebas, react-native-mobile, etc.).

POST:
- /code-review (comando del harness) sobre el diff — es lo más adecuado para cambios pequeños; busca bugs y oportunidades de simplificación.
- /review-feature solo si el fix afecta una feature SDD concreta.
- /update-traceability solo si cambió el estado de una HU/tarea (un bugfix puro normalmente no lo cambia → puedes omitirlo).


# Git después de implementar la HU

```bash
git branch backup/HU-X-before-squash
git reset --soft develop
git commit -m "xxx"
git log --oneline --graph --decorate --all
git checkout develop
git merge --ff-only feature/HU-X-slug
git push origin develop
git checkout feature/HU-X-slug
git push -u origin feature/HU-X-slug --force-with-lease
```
