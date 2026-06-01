import test from "node:test";
import assert from "node:assert/strict";
import { buildAuthUser } from "../src/auth/tokenClaims.js";

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

test("buildAuthUser should throw when sub claim is missing", () => {
  const token = buildToken({
    preferred_username: "no-sub-user",
    realm_access: { roles: ["Participante"] },
  });

  assert.throws(() => buildAuthUser(token), /sub claim/);
});
