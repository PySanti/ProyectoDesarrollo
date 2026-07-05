# Keycloak Audience Mapper — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make real Keycloak tokens carry a valid API audience so an authenticated `Administrador`/`Operador`/`Participante` can call identity (and future services) without a 401 audience-mismatch.

**Architecture:** Add an `oidc-audience-mapper` protocol mapper to the `umbral-web` and `umbral-mobile` clients in the committed realm import (`infra/keycloak/import/umbral-realm.json`), each injecting its own `clientId` as an access-token audience. identity already accepts `umbral-web`/`umbral-mobile`, so no service or gateway config changes. `account` stays out of every `ValidAudiences` list (defense-in-depth intact).

**Tech Stack:** Keycloak 25.0 (`quay.io/keycloak/keycloak:25.0`, `start-dev --import-realm`, import strategy `IGNORE_EXISTING`), .NET 8 identity-service, Playwright (system chrome) for the E2E gate.

## Global Constraints

- Change **only** `infra/keycloak/import/umbral-realm.json` (plus this plan + the spec + the ledger). No code, no contracts, no service/gateway appsettings, no `.env` edits.
- `account` must NOT be added to any `ValidAudiences`. The mapper adds `umbral-web`/`umbral-mobile`, nothing else.
- Mapper `access.token.claim` = `"true"`, `id.token.claim` = `"false"`. Value via `included.custom.audience` (the client's own id); leave `included.client.audience` empty.
- Do not stage or commit any file other than the exact ones named in each task. No `git add -A`, no directory adds, no stash/reset/checkout/clean.
- Commit trailer, exactly: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`
- Spec of record: `docs/superpowers/specs/2026-07-05-keycloak-audience-mapper-design.md`.

---

### Task 1: Add audience mappers to `umbral-web` and `umbral-mobile`

**Files:**
- Modify: `infra/keycloak/import/umbral-realm.json` (the `umbral-web` client block ~L44-57 and the `umbral-mobile` client block ~L58-70)

**Interfaces:**
- Consumes: nothing.
- Produces: each user client now emits its own `clientId` in the access-token `aud` array. Downstream (identity, future services) rely on `aud` containing `umbral-web` / `umbral-mobile`.

- [ ] **Step 1: Write the failing check**

Save as `/tmp/check-audience-mappers.py` (scratch, not committed):

```python
import json, sys
d = json.load(open('infra/keycloak/import/umbral-realm.json'))
def has_aud(client_id):
    c = [c for c in d['clients'] if c['clientId'] == client_id][0]
    return any(
        m.get('protocolMapper') == 'oidc-audience-mapper'
        and m.get('config', {}).get('included.custom.audience') == client_id
        and m.get('config', {}).get('access.token.claim') == 'true'
        for m in c.get('protocolMappers', [])
    )
ok = has_aud('umbral-web') and has_aud('umbral-mobile')
print('OK both audience mappers present' if ok else 'MISSING audience mapper(s)')
sys.exit(0 if ok else 1)
```

- [ ] **Step 2: Run it to verify it fails**

Run: `python3 /tmp/check-audience-mappers.py`
Expected: prints `MISSING audience mapper(s)`, exit code 1.

- [ ] **Step 3: Add the mapper to `umbral-web`**

Edit `infra/keycloak/import/umbral-realm.json`. Replace exactly:

```json
      "attributes": {
        "pkce.code.challenge.method": "S256",
        "post.logout.redirect.uris": "http://localhost:5173/*"
      }
    },
```

with:

```json
      "attributes": {
        "pkce.code.challenge.method": "S256",
        "post.logout.redirect.uris": "http://localhost:5173/*"
      },
      "protocolMappers": [
        {
          "name": "umbral-web-audience",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "",
            "included.custom.audience": "umbral-web",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        }
      ]
    },
```

- [ ] **Step 4: Add the mapper to `umbral-mobile`**

Replace exactly (the trailing `{` anchors on the next client so the match is unique):

```json
      "attributes": {
        "pkce.code.challenge.method": "S256"
      }
    },
    {
      "clientId": "identity-service",
```

with:

```json
      "attributes": {
        "pkce.code.challenge.method": "S256"
      },
      "protocolMappers": [
        {
          "name": "umbral-mobile-audience",
          "protocol": "openid-connect",
          "protocolMapper": "oidc-audience-mapper",
          "consentRequired": false,
          "config": {
            "included.client.audience": "",
            "included.custom.audience": "umbral-mobile",
            "id.token.claim": "false",
            "access.token.claim": "true"
          }
        }
      ]
    },
    {
      "clientId": "identity-service",
```

- [ ] **Step 5: Run the check to verify it passes + JSON is valid**

Run: `python3 /tmp/check-audience-mappers.py && python3 -m json.tool infra/keycloak/import/umbral-realm.json > /dev/null && echo "JSON valid"`
Expected: `OK both audience mappers present` then `JSON valid` (exit 0).

- [ ] **Step 6: Commit**

```bash
git add infra/keycloak/import/umbral-realm.json
git commit -m "$(cat <<'EOF'
fix(sp5): audience mapper Keycloak — umbral-web/umbral-mobile inyectan su clientId en aud

Cierra el gap SP-5a: los tokens reales traían aud=account y identity los
rechazaba (ValidAudiences=umbral-web,umbral-mobile). oidc-audience-mapper por
client agrega su propio clientId al access token. Sin tocar appsettings ni
codigo; account sigue no aceptado.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: End-to-end verification gate (re-seed, no local override)

Proves the committed realm JSON alone fixes the 401 — no `.env` override, no appsettings change. **No commit** (verification only); records the result in the ledger.

**Files:** none modified.

**Interfaces:**
- Consumes: Task 1's realm JSON.
- Produces: evidence that a real `umbral-web` token is accepted by identity.

- [ ] **Step 1: Confirm no local audience override lingers**

Run: `grep -c 'Keycloak__ValidAudiences' services/identity-service/.env`
Expected: `0`. (If non-zero, remove those lines — the committed fix must stand alone.)

- [ ] **Step 2: Down Keycloak and remove its volume (forces re-import)**

Import strategy is `IGNORE_EXISTING`; the realm only reloads on a fresh volume. This wipes runtime-created users (seed users `admin`/`operador`/`participante` remain) and rotates signing keys.

Run:
```bash
docker compose -f infra/docker-compose.yml down
docker volume rm infra_umbral-keycloak-data
```
Expected: volume removed (or "No such volume" if compose down already dropped it — acceptable).

- [ ] **Step 3: Bring up postgres + keycloak, confirm realm import**

Run:
```bash
docker compose -f infra/docker-compose.yml up -d postgres keycloak
curl -s --retry 30 --retry-delay 1 --retry-all-errors --retry-connrefused -o /dev/null -w "realm %{http_code}\n" http://localhost:8080/realms/UMBRAL-UCAB
```
Expected: `realm 200`.

- [ ] **Step 4: Start identity on :5000**

Run (background, log to a concrete path): `cd services/identity-service && bash run-local.sh > /tmp/umbral-identity.log 2>&1 &`
Then from repo root:
`curl -s --retry 30 --retry-delay 1 --retry-all-errors --retry-connrefused -o /dev/null -w "identity %{http_code}\n" http://localhost:5000/identity/governance/roles`
Expected: `identity 401` (alive; rejects the unauthenticated probe).

- [ ] **Step 5: Start frontend + run the Playwright gate**

Run:
```bash
cd frontend && bash run-local.sh > /tmp/umbral-frontend.log 2>&1 &   # :5173
mkdir -p /tmp/gov-shots
SHOT_DIR=/tmp/gov-shots node scripts/gov-visual-pass.mjs
```
Expected stdout: `GOV cards=3 loadError=0` (before the fix this was `cards=0 loadError=1`). This only happens if the real `umbral-web` token was accepted by identity — i.e. `aud` now carries `umbral-web`. Cross-check: `grep -c 'SecurityTokenInvalidAudienceException' /tmp/umbral-identity.log` after the run should be `0`.

- [ ] **Step 6: Record the result in the ledger**

Append a line to `.git/sdd/progress.md` noting: audience-mapper fix verified end-to-end (cards=3, identity 200, no `.env` override, re-seeded realm). No code commit for this task.

---

## Notes for the executor

- `directAccessGrantsEnabled=false` on `umbral-web`, so you cannot mint a token via password grant to inspect `aud` directly; the Playwright login (auth-code + PKCE) is the intended path and `cards=3` is the acceptance signal.
- If Playwright complains about the bundled headless-shell build, `gov-visual-pass.mjs` already launches `channel: 'chrome'` (system Chrome at `/usr/bin/google-chrome`).
- Teardown after verification (if the user wants the env down): kill identity + frontend, `docker compose down`. Leave the realm volume gone or recreate on next `up`.
