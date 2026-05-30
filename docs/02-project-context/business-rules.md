# Business Rules

## Global rules

| ID | Rule |
|---|---|
| BR-G01 | The system supports only two game modes: Trivia and Búsqueda del Tesoro / BDT. |
| BR-G02 | A game can only be in one of the states approved by the SRS and SDD. |
| BR-G03 | The system must differentiate behavior by authenticated role and by domain conditions such as team leadership. |
| BR-G04 | Keycloak handles authentication and base roles. UMBRAL stores only local references and domain state. |
| BR-G05 | User-visible real-time updates must reflect state already accepted by the backend. |
| BR-G06 | RabbitMQ events represent facts that already happened. |
| BR-G07 | A service must not access another service database directly. |

## Team rules

| ID | Rule |
|---|---|
| BR-E01 | A participant can belong to at most one active team. |
| BR-E02 | A team can exist with 1 to 5 members. |
| BR-E03 | The team creator is automatically registered as first member and leader. |
| BR-E04 | A team must not exceed 5 members. |
| BR-E05 | A participant can join a team only with a valid access code. |
| BR-E06 | A non-leader participant can leave the team directly. |
| BR-E07 | If the leader wants to leave and there are other members, leadership must be transferred first. |
| BR-E08 | If the leader wants to leave and is the only member, the team is deleted. |
| BR-E09 | A disabled or deleted team cannot be registered in new games. |
| BR-E10 | Team leadership is a UMBRAL domain condition, not a Keycloak role. |

## Trivia rules

| ID | Rule |
|---|---|
| BR-T01 | Only an operator can create Trivia forms. |
| BR-T02 | A Trivia form must contain questions, answer options, one correct answer, assigned score and time limit per question. |
| BR-T03 | An incomplete Trivia form cannot be used to create a Trivia game. |
| BR-T04 | Only an operator can create and publish Trivia games. |
| BR-T05 | Every Trivia game must be associated with a valid Trivia form. |
| BR-T06 | A Trivia game can be individual or team-based. |
| BR-T07 | In individual modality, the active participant represents a user. |
| BR-T08 | In team modality, the active participant represents a team. |
| BR-T09 | In team modality, only the team leader can register the team. |
| BR-T10 | A team can have 1 to 5 members; do not enforce a minimum of 2 members for team existence. |
| BR-T11 | One answer per participant is accepted per active question. |
| BR-T12 | In team modality, the first answer sent for the team is definitive. |
| BR-T13 | Repeated, late or out-of-state answers must be rejected. |
| BR-T14 | The timer controls question availability, closing and late-answer validation. |
| BR-T15 | The timer does not modify score. |
| BR-T16 | A correct answer adds the assigned score of the question directly. |
| BR-T17 | An incorrect answer adds 0 points unless the SDD explicitly defines otherwise. |
| BR-T18 | Trivia ranking is ordered by accumulated score descending. |
| BR-T19 | Ranking tie-breaking must be defined explicitly in the related SDD; do not assume time-based tie-breaking. |

## BDT rules

| ID | Rule |
|---|---|
| BR-B01 | Only an operator can create and publish BDT games. |
| BR-B02 | A BDT game is composed of stages. |
| BR-B03 | Each BDT stage has an expected QR value. |
| BR-B04 | A QR treasure submission is valid only if its decoded value matches the expected QR for the active stage. |
| BR-B05 | A valid QR submission can advance or resolve the active stage according to the SDD. |
| BR-B06 | The operator can send clues during BDT when allowed by the SDD. |
| BR-B07 | BDT geolocation is used only when approved by the SDD and authorized by the participant. |
| BR-B08 | BDT score and ranking belong to BDT Game Service. |
