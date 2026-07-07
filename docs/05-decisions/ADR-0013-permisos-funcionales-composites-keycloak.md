# ADR-0013 — Permisos funcionales como realm roles técnicos composite en Keycloak

- **Estado:** Aceptado (2026-07-03, SP-5a)
- **Contexto:** CLAUDE.md manda que el token lleve rol base y permisos funcionales
  (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`), que el gateway autorice
  coarse por rol sin consultar Identity, y que la autorización fina por permiso viva en cada
  microservicio. BR-R02/R03 definen permisos gestionados POR ROL con defaults fijos. No existía
  representación de permisos en el token (cero mappers).
- **Decisión:** Cada permiso funcional es un **realm role técnico** de Keycloak, asignado como
  **composite** de los roles base según BR-R03 (Operador→GestionarPartidas;
  Participante→GestionarEquipos+ParticiparEnPartidas; Administrador→ninguno: sus privilegios de
  gobernanza son el rol base, protegidos). Keycloak expande composites en `realm_access.roles`
  automáticamente — sin mappers custom. Los servicios enforcean con policies
  `RequireRole("<permiso>")` tras normalizar claims (`KeycloakRoleClaims`).
- **Los roles técnicos NO son roles de usuario.** La regla del dominio "no se crean roles
  nuevos" refiere a roles base de negocio; los 3 roles base siguen siendo los únicos asignables
  a usuarios. `scripts/check-realm-composites.py` verifica que ningún usuario tenga roles
  técnicos directos.
- **Gobernanza (SP-5b):** Identity persistirá las asignaciones permiso↔rol en su DB (panel +
  auditoría) y propagará cambios a Keycloak vía Admin API (add/remove composite) — mismo patrón
  "propagado a Keycloak" que el cambio de rol. Cambios efectivos al siguiente refresh del token.
- **Alternativas descartadas:** (B) Identity DB + eventos RabbitMQ + cache por servicio — token
  sin permisos contradice la directiva; alto costo transversal. (C) consulta HTTP a Identity con
  cache TTL — acoplamiento runtime y latencia.
- **Consecuencias:** los tokens emitidos antes del re-seed del realm no llevan permisos (403
  hasta refresh); la revocación de un permiso a un rol tarda hasta el TTL del token; el realm
  import es fuente de defaults, la gobernanza dinámica llega en SP-5b.
