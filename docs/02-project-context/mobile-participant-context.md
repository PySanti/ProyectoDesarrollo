# Mobile Participant Context

## Decision

Participant flows belong to the React Native mobile application.

## Client split

| Actor | Client |
|---|---|
| Administrador | React web |
| Operador | React web |
| Participante | React Native mobile |
| Líder de equipo | React Native mobile when acting as participant |
| Sistema | Backend |

## Participant-owned features

The mobile app covers:

- published Trivia list;
- published BDT list;
- filters by modality;
- team creation;
- join team by code;
- leave team;
- transfer leadership;
- delete team when allowed;
- join individual Trivia;
- preinscribe team in Trivia;
- accept/reject Trivia convocatorias;
- waiting screen;
- Trivia question display;
- Trivia answer submission;
- Trivia result display;
- Trivia cancellation notification;
- Trivia history;
- join individual BDT;
- preinscribe team in BDT;
- accept/reject BDT convocatorias;
- BDT active stage;
- QR treasure upload;
- BDT stage result;
- BDT clues;
- BDT cancellation notification;
- BDT ranking;
- in-app real-time updates.

## Web-owned features

The React web app covers:

- user administration;
- team administration by administrator;
- Trivia form creation;
- Trivia game creation;
- Trivia operator lobby;
- Trivia ranking supervision;
- BDT game creation;
- BDT stage configuration;
- BDT operator lobby;
- BDT clues sending;
- BDT uploaded treasure supervision;
- BDT geolocation map;
- history/audit views.

## Implementation rule

If the SDD references a participant HU, OpenCode must plan a mobile implementation task unless explicitly out of scope for the delivery.

If the SDD references an administrator/operator HU, OpenCode must plan a web implementation task unless explicitly out of scope.
