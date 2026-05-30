# Mobile Context — UMBRAL Participant App

## Purpose

This document defines the React Native mobile client for UMBRAL participant flows.

The mobile app is the primary client for users with role `Participante`.

## Client ownership

The React Native mobile app owns participant-facing flows:

- View published Trivia games.
- View published BDT games.
- Filter games by modality: individual or team.
- Create team.
- Join team by code.
- Leave team.
- Transfer leadership when applicable.
- Delete team when allowed.
- Join individual games.
- Preinscribe team in team games when the participant is leader.
- Accept or reject convocatorias.
- View waiting/lobby screens.
- Answer Trivia questions.
- View Trivia question result and correct answer.
- View rankings where applicable.
- View BDT active stage and timer.
- Upload/take QR treasure image.
- Receive BDT clues.
- Share geolocation during active BDT games.
- Receive in-app real-time notifications.

## Not owned by mobile

The mobile app must not own:

- User administration.
- Operator game creation.
- Trivia form creation.
- BDT stage configuration.
- Operator lobby supervision.
- Operator BDT geolocation map.
- Administrative team management.
- Backend rules.
- Direct database access.

## Source of truth

The mobile app consumes:

- HTTP contracts from `contracts/http/`.
- Real-time contracts/events from `contracts/events/`.
- Backend validation errors and domain decisions.

The mobile app must not duplicate authoritative business rules. It can perform UI validation for usability, but backend services remain authoritative.

## Mandatory stack

- React Native.
- TypeScript.
- API clients generated or implemented from documented HTTP contracts.
- Real-time communication using the project-approved WebSockets/SignalR strategy.
- Camera/image picker permissions for BDT QR upload.
- Geolocation permission for BDT active sessions.

## Feature routing rule

If the SRS story actor is `Participante`, implement it in the mobile app unless the SDD explicitly says otherwise.

If the story actor is `Administrador` or `Operador`, implement it in React web unless the SDD explicitly says otherwise.

If the story actor is `Sistema`, implement it in backend/application/domain.

## Mobile architecture recommendation

Use a simple layered frontend structure:

```txt
mobile/
  src/
    app/
    screens/
    components/
    features/
      teams/
      trivia/
      bdt/
      participation/
    api/
    realtime/
    hooks/
    state/
    navigation/
    permissions/
    shared/
```

## Feature folders

Recommended feature boundaries:

```txt
features/teams
features/trivia
features/bdt
features/participation
features/notifications
```

## Required SDD references

Every mobile feature must reference:

- HU ID.
- Owning backend service.
- HTTP contract.
- Real-time updates, if any.
- Required permissions, if any.
- Error states.
- Loading/empty states.
- Acceptance criteria.

## Permission rules

### Camera / image picker

Required for:

- HU-45 Subida de tesoro BDT.

The UI must explain why the permission is needed.

### Geolocation

Required for active BDT participation.

If the participant denies geolocation permission during active BDT, the app must block BDT participation and show a clear message based on backend/SRS rules.

## Real-time rules

Mobile may subscribe to real-time updates for:

- Lobby changes.
- Trivia question changes.
- Trivia timers.
- Trivia ranking.
- Trivia cancellation.
- BDT stage changes.
- BDT timer.
- BDT clues.
- BDT cancellation.
- BDT ranking.
- Notifications inside the app.

## Anti-rules

Do not:

- Put domain rules in UI components.
- Infer endpoints.
- Hardcode undocumented event names.
- Access databases directly.
- Implement operator/admin screens in mobile.
- Implement participant gameplay screens in React web.
