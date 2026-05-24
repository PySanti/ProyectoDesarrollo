# team-service Context

## Responsibility

Manages teams, team members and team status.

## Owns

- Teams
- TeamMembers

## Related stories

- HU-02
- HU-03
- HU-05
- HU-07

## Rules

- Inactive teams cannot be associated to new sessions.
- Team history must be preserved.
- Participant-team association must be registered.

## Publishes events

- TeamCreated
- TeamUpdated
- TeamDeactivated
- ParticipantAssignedToTeam

## Does not own

- Scores
- Rankings
- Trivia content
- Treasure Hunt missions
- Audit history
