# HU-24 — Acceptance: Iniciar manualmente Trivia

## Acceptance checklist

| CA | Descripción | Estado |
|----|-------------|--------|
| CA-01 | Inicio exitoso desde Lobby con mínimos y ModoInicio Manual | Pendiente |
| CA-02 | Rechazar 404 si partida no existe | Pendiente |
| CA-03 | Rechazar 409 si no está en Lobby | Pendiente |
| CA-04 | Rechazar 409 si no cumple mínimos | Pendiente |
| CA-05 | Rechazar 409 si ModoInicio = Automatico | Pendiente |
| CA-06 | Permitir inicio si ModoInicio = ManualYAutomatico | Pendiente |
| CA-07 | Rechazar 403 si no es Operador | Pendiente |

## Test results

| Suite | Tests | Resultado |
|-------|-------|-----------|
| Domain | — | — |
| Application handler | 6 tests (3 existentes + 2 nuevos) | — |
| API integration | 4 tests (2 existentes + 1 nuevo) | — |

## Business rules verified

| RB | Descripción | Verificado |
|----|-------------|------------|
| RB-T17 | Inicio manual o automático | Pendiente |
| RB-T18 | Cambio a estado Iniciada | Pendiente |
| RB-26 | No iniciar sin mínimos | Pendiente |
| RB-12 | Validar transición de estado | Pendiente |
| HU-24-R01 | Rechazar inicio manual si ModoInicio = Automatico | Pendiente |
| HU-24-R02 | Permitir inicio manual si ModoInicio = ManualYAutomatico | Pendiente |
