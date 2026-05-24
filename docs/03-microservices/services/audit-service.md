# Audit Service

## Responsibility

Owns immutable audit and session event history.

## Owns

- SystemAuditLogs
- SessionEventLogs

## Related stories

- HU-11
- HU-06

## Rules

- Relevant system actions must be recorded.
- Audit records should not modify business decisions.
- Audit Service consumes events from other services.

## Does not own

- Scores
- Session state transitions
- Business validations
