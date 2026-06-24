# Gateway — context

The mandatory YARP entry point. Validates the Keycloak JWT and applies coarse, route-level
authorization by base role; routes to the four services; passes WebSockets through for SignalR.
Owns no domain logic, scores, rankings, or DB access.

Config-first: routes/clusters/role-mapping live in `appsettings.json` (`ReverseProxy`); the
security border (JWT scheme, 3 role policies, `RequireAuthenticatedUser` fallback) is the only code.
Status: SP-0 — routes to identity (5000) + the three shells (5010/5020/5030); legacy trivia/bdt
are not routed (clients hit them directly until SP-5).
