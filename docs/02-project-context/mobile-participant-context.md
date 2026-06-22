# Mobile Participant Context

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Decision

Participant flows belong to the React Native (Expo) mobile application. Administrator/operator flows belong to the React (Vite) web application. All client↔backend traffic, including real-time, passes through the mandatory YARP gateway.

## Client split

| Actor | Client |
|---|---|
| Administrador | React web |
| Operador | React web |
| Participante | React Native mobile |
| Líder de equipo | React Native mobile when acting as participant (business attribute, not a Keycloak role) |
| Sistema | Backend |

## Participant-owned features (mobile)

The mobile app covers a single `Partidas` panel and participation flows:

- single `Partidas` panel listing all published partidas (any game type), with filter by modality (`Individual` / `Equipo`);
- team creation;
- join a team by accepting an `InvitacionEquipo` (there is no team access code);
- view and respond to received team invitations;
- leave team; transfer leadership; delete team when allowed;
- join individual partidas;
- inscribe the team in team partidas (leader only);
- accept/reject partida convocatorias;
- waiting screen while a partida is in `Lobby`;
- Trivia: synchronized question display and answer submission, result display;
- BDT: active `EtapaBDT` view, QR treasure upload (camera/image), stage result, received clues;
- BDT geolocation sharing (mandatory for an active BDT game, ~every 2 seconds, after authorization);
- partida cancellation notification (in-app);
- single partida history; team-name history; team performance;
- native and consolidated ranking views;
- in-app real-time updates.

## Web-owned features (operator/admin)

The React web app covers:

- user administration; role modification; per-role permission/governance panel;
- administrative team management;
- partida creation and configuration with sequential `Juego`s, Trivia `Pregunta`s, and BDT `EtapaBDT`s;
- publishing, lobby, manual/automatic start, cancellation;
- live operation: ranking supervision, clue delivery, uploaded-treasure supervision, BDT geolocation map;
- history/audit views (admin views operations read-only).

## Implementation rule

If an SDD references a participant HU, plan a mobile implementation task unless explicitly out of scope. If an SDD references an administrator/operator HU, plan a web implementation task unless explicitly out of scope. The frontend redesign is a visual + IA reconstruction only and must not change contracts, business rules, HUs, or test-relied identifiers.
