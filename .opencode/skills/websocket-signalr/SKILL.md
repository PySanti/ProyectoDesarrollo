---
name: websocket-signalr
description: Apply WebSocket/SignalR patterns for UMBRAL real-time session, ranking, clue and event updates.
compatibility: opencode
---

# WebSocket / SignalR

Based on:

- `docs/00-professor-source/skills/websocket-signalr-skill.md`

Use real-time updates for:

- Session state changes
- Ranking updates
- Timer updates
- Clue releases
- Evidence validation updates
- Trivia question activation and closing
- Participant reconnection state

Rules:

- Do not use SignalR as a replacement for persistence.
- Do not put business rules inside hubs.
- Hubs notify clients about state changes already accepted by the backend.