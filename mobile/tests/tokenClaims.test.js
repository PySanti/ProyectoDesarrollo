import test from "node:test";
import assert from "node:assert/strict";
import { buildAuthUser, isJwtExpired } from "../src/auth/tokenClaims.js";

if (typeof global.atob !== "function") {
  global.atob = (value) => Buffer.from(value, "base64").toString("binary");
}

function buildToken(payload) {
  const header = Buffer.from(JSON.stringify({ alg: "none", typ: "JWT" })).toString("base64url");
  const body = Buffer.from(JSON.stringify(payload)).toString("base64url");
  return `${header}.${body}.`;
}

test("buildAuthUser should parse sub, username and roles", () => {
  const token = buildToken({
    sub: "11111111-1111-1111-1111-111111111111",
    preferred_username: "participante.demo",
    realm_access: { roles: ["Participante"] },
  });

  const user = buildAuthUser(token);
  assert.equal(user.sub, "11111111-1111-1111-1111-111111111111");
  assert.equal(user.username, "participante.demo");
  assert.deepEqual(user.roles, ["Participante"]);
});

test("buildAuthUser expone nombre desde given_name para el saludo del Home (S9)", () => {
  const token = buildToken({
    sub: "22222222-2222-2222-2222-222222222222",
    preferred_username: "juan.perez@correo.com",
    given_name: "Juan",
    name: "Juan Pérez",
    realm_access: { roles: ["Participante"] },
  });

  const user = buildAuthUser(token);
  assert.equal(user.nombre, "Juan");
  // username sigue siendo el id de cuenta (correo), para RoleRestrictedScreen.
  assert.equal(user.username, "juan.perez@correo.com");
});

test("buildAuthUser cae de given_name a name y luego a preferred_username para nombre", () => {
  const soloName = buildAuthUser(buildToken({ sub: "s", preferred_username: "u@c.com", name: "Ana Gil" }));
  assert.equal(soloName.nombre, "Ana Gil");

  const soloPreferred = buildAuthUser(buildToken({ sub: "s", preferred_username: "u@c.com" }));
  assert.equal(soloPreferred.nombre, "u@c.com");
});

test("buildAuthUser should throw when sub claim is missing", () => {
  const token = buildToken({
    preferred_username: "no-sub-user",
    realm_access: { roles: ["Participante"] },
  });

  assert.throws(() => buildAuthUser(token), /identificador de usuario/);
});

test("isJwtExpired should detect expired tokens", () => {
  const expiredToken = buildToken({ exp: Math.floor(Date.now() / 1000) - 60 });

  assert.equal(isJwtExpired(expiredToken), true);
});

test("isJwtExpired should accept valid tokens", () => {
  const validToken = buildToken({ exp: Math.floor(Date.now() / 1000) + 3600 });

  assert.equal(isJwtExpired(validToken), false);
});
