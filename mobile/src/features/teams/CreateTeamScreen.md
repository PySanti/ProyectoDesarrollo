# CreateTeamScreen Legacy Notes

This document is **legacy implementation evidence / migration debt**, not active current-doctrine
guidance for a new HU-03 implementation.

The screen notes below describe how the previous React Native team-creation slice behaved before the
documentation doctrine replacement. Do not use them to infer current endpoints, response shapes, or
service boundaries.

## Legacy implementation behavior

- Field: `nombreEquipo`.
- The old slice called the legacy team endpoint `POST /identity/teams` through `createTeamApi`.
- The old success response rendered `equipoId`, `codigoAcceso`, `liderUserId`, and `integrantes`.
- The old `409` participant message was `Ya perteneces a un equipo activo. No puedes crear otro equipo.`

## Current doctrine note

- Team creation belongs to the `Identity` target service behind the mandatory YARP gateway.
- Team membership now uses `InvitacionEquipo`; `codigoAcceso` is a legacy concept and must not be
  treated as current guidance.
- Do not introduce or implement a current endpoint from this file. A current-doctrine SDD must first
  register the feature and its contract under `contracts/http/identity-api.md`.
