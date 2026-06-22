# Definition of Ready

A feature is ready to implement only when:

- The user story is identified.
- The owning microservice is identified (`Identity`, `Partidas`, `Operaciones de Sesion`, or `Puntuaciones`).
- The related requirement is identified from the current source documents (`docs/01-project-source/`).
- The business rules are listed.
- The four gateway-aware contract questions are answered:
  - Does the feature use HTTP through the gateway?
  - Does it require RabbitMQ events?
  - Does it require SignalR/WebSockets through the gateway?
  - Which target service owns the command/query?
- The API or event contract is described (referencing `contracts/`).
- The client target is confirmed (web for `Administrador`/`Operador`; mobile for `Participante`; backend for `Sistema`).
- The acceptance criteria are clear.
- The required tests are planned.
