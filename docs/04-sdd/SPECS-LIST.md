# Active Specs List

No active current-doctrine implementation specs are registered yet.

## Rule

Add a spec row only after the feature has been selected from the current source documents and assigned to one of the target services.

| Feature | Owning service | Client target | Actor | SDD folder | Status |
|---|---|---|---|---|---|
| Equipos-admin — ciclo de vida + CRUD admin + historial (Bloque 4A) | Identity | web (HU-09) + mobile (HU-06/HU-48) | Líder de equipo / Administrador / Participante | docs/superpowers/specs/2026-07-08-bloque4a-equipos-admin-design.md | Implemented (23 tasks A1..I3, review-clean; HU-06/09/48 · BR-E06/E10/E11). HU-19 diferida al slice 4B. |
| Mejora de validación de inputs (formato + reglas + feedback en vivo) | Identity (+ Partidas) | web + mobile | Administrador / Operador / Participante | docs/superpowers/specs/2026-07-16-mejora-validacion-inputs-design.md | Implemented — Fase A backend (regla `TextoHumano` en validators de usuario/equipo + mensaje Keycloak) · Fase B web (`shared/validation.ts` + `Field` con error por campo + aria + parseo de `ValidationProblemDetails`; crear/editar usuario, crear/renombrar equipo, nombre de partida) · Fase C mobile (`shared/validation.js` + `createTeamFlow` + feedback en vivo en `CreateTeamScreen`) · Fase D espacio de error estable (web+mobile) · Fase E login Keycloak (script de theme, solo correo) · Fase F cierre de huecos backend Partidas (`TextoHumano` en `NombrePartida`/`AreaBusqueda`) + área BDT front + trazabilidad. Fix del bug "Error de integración con Keycloak" al crear usuario con nombre solo-símbolos. HU-01/HU-02 · RB-U0x · BR-B02. |
