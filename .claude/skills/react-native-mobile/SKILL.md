---
name: react-native-mobile
description: Build React Native participant flows for UMBRAL using documented contracts and SDD.
compatibility: opencode
---

# React Native Mobile Skill

Use this skill whenever a feature affects the UMBRAL participant mobile app.

## Read

- `mobile/mobile-context.md`
- `docs/02-project-context/mobile-participant-context.md`
- `docs/02-project-context/srs-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/04-sdd/SPECS-LIST.md`
- related SDD folder under `docs/04-sdd/specs/`
- `contracts/http/`
- `contracts/events/`

## Client ownership

The mobile app owns participant flows:

- Game listing and filtering.
- Team management by participant.
- Joining individual games.
- Team preinscription by leader.
- Convocatoria accept/reject.
- Trivia answering.
- Trivia results.
- BDT stage view.
- QR treasure upload.
- BDT clues.
- BDT geolocation sharing.
- Participant notifications.

## Rules

- Do not implement participant gameplay flows in React web.
- Do not implement administrator/operator flows in React Native.
- Do not invent endpoints.
- Do not invent event names.
- Backend remains authoritative for business rules.
- UI validation is allowed only for usability.
- All mobile API calls must map to documented contracts.
- All real-time subscriptions must map to documented events/channels.
- Handle loading, empty, error and permission-denied states.
- Permissions must be requested only when needed and explained to the user.

## Permission-specific rules

### QR upload

For QR upload features:

- Support camera or image picker according to the SDD.
- Do not decode QR in the mobile app unless the SDD explicitly requires it.
- The backend is responsible for authoritative QR validation.

### Geolocation

For BDT geolocation:

- Ask for permission before sharing location.
- If permission is denied, block active BDT participation according to the SRS.
- Do not implement historical route analytics unless explicitly added to scope.

## Recommended mobile structure

```txt
mobile/
  src/
    features/
      teams/
      trivia/
      bdt/
      participation/
    api/
    realtime/
    navigation/
    permissions/
    shared/
```

## Done checklist

```txt
[ ] Uses documented HTTP contracts.
[ ] Uses documented real-time contracts if needed.
[ ] Does not duplicate backend rules.
[ ] Has loading/error/empty states.
[ ] Handles permissions if needed.
[ ] Updates acceptance.md.
[ ] Updates traceability if needed.
```
