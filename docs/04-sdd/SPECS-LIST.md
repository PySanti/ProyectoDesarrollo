# Active Specs List

No active current-doctrine implementation specs are registered yet.

## Rule

Add a spec row only after the feature has been selected from the current source documents and assigned to one of the target services.

| Feature | Owning service | Client target | Actor | SDD folder | Status |
|---|---|---|---|---|---|
| Equipos-admin — ciclo de vida + CRUD admin + historial (Bloque 4A) | Identity | web (HU-09) + mobile (HU-06/HU-48) | Líder de equipo / Administrador / Participante | docs/superpowers/specs/2026-07-08-bloque4a-equipos-admin-design.md | Implemented (23 tasks A1..I3, review-clean; HU-06/09/48 · BR-E06/E10/E11). HU-19 diferida al slice 4B. |
| Nombres de competidores en pantallas de operador y participante (refinamiento transversal) | Identity | web + mobile | Operador / Participante | docs/superpowers/specs/2026-07-14-nombres-competidores-design.md | Implemented (12 tasks). Refinamiento transversal de usabilidad sobre HU ya implementadas de lobby, ranking en vivo, consolidado, pistas, geolocalización BDT e historial — no introduce HU nueva. Endpoint nuevo `POST /identity/directory/names`. **Diferido:** nombres de partida y de juego (servicio Partidas)→slice propio. |
| Nombres de partida y de juego en las pantallas (refinamiento transversal) | Puntuaciones + Operaciones de Sesión | web + mobile | Operador / Participante | docs/superpowers/specs/2026-07-15-nombres-partida-juego-design.md | Implemented (10 tasks). Slice hermano del de nombres de competidores. `Juego` no tiene nombre en el dominio: la columna muestra "Juego 1 · Trivia" derivado de orden y tipo. Sin endpoints nuevos, sin cambios en Partidas ni en el gateway. |
| Tiempo real en las pantallas del participante (corrección) | Operaciones de Sesión | mobile | Participante | docs/superpowers/specs/2026-07-15-tiempo-real-participante-design.md | Implemented (7 tasks). Aceptación/rechazo y arranque de partida sin pulsar Recargar. Causa raíz única: la pertenencia a grupos de SignalR exigía participación previa, pero todo lo notificable ocurre antes. Sin endpoints nuevos; BR-G09 sin cambios. |
