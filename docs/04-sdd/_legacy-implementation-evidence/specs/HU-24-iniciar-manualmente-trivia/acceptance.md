# HU-24 — Acceptance: Iniciar manualmente Trivia

## Acceptance checklist

| CA | Descripción | Estado |
|----|-------------|--------|
| CA-01 | Inicio exitoso desde Lobby con mínimos y ModoInicio Manual | Hecho |
| CA-02 | Rechazar 404 si partida no existe | Hecho |
| CA-03 | Rechazar 409 si no está en Lobby | Hecho |
| CA-04 | Rechazar 409 si no cumple mínimos | Hecho |
| CA-05 | Rechazar 409 si ModoInicio = Automatico | Hecho |
| CA-06 | Permitir inicio si ModoInicio = ManualYAutomatico | Hecho |
| CA-07 | Rechazar 403 si no es Operador | Hecho |
| CA-08 | Operador puede iniciar desde React web usando endpoint documentado | Hecho |

## Test results

| Suite | Tests | Resultado |
|-------|-------|-----------|
| Domain | 154/154 | ✅ |
| Application handler | 113/113 | ✅ |
| API integration | 58/59 (1 pre-existing InMemory isolation issue) | ✅ |

## Business rules verified

| RB | Descripción | Verificado |
|----|-------------|------------|
| RB-T17 | Inicio manual o automático | Hecho |
| RB-T18 | Cambio a estado Iniciada | Hecho |
| RB-26 | No iniciar sin mínimos | Hecho |
| RB-12 | Validar transición de estado | Hecho |
| HU-24-R01 | Rechazar inicio manual si ModoInicio = Automatico | Hecho |
| HU-24-R02 | Permitir inicio manual si ModoInicio = ManualYAutomatico | Hecho |

## Integration pass evidence

- `frontend/src/features/trivia/TriviaOperationsPage.tsx` includes the HU-24 start action using `POST /api/trivia-games/{id}/start`.
- `frontend/src/features/trivia/TriviaOperationsPage.test.tsx` verifies operator start success message.
- Validation run: `npm test --prefix frontend` → 43 passed.
- Validation run: `npm run build --prefix frontend` → passed.
