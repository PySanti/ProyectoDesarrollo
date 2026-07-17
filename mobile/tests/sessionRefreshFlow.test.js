import test from "node:test";
import assert from "node:assert/strict";
import { refrescarTokenFlow } from "../src/auth/sessionRefreshFlow.js";

const base = {
  tokenEndpoint: "http://kc:8080/realms/UMBRAL-UCAB/protocol/openid-connect/token",
  clientId: "umbral-mobile",
  refreshToken: "R1",
  buildUser: (token) => ({ sub: "u1", desde: token }),
};

test("refresh exitoso devuelve token nuevo, user y refresh rotado", async () => {
  let captured;
  const fetchImpl = async (url, init) => {
    captured = { url, init };
    return {
      ok: true,
      json: async () => ({ access_token: "A2", refresh_token: "R2" }),
    };
  };
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: true, token: "A2", user: { sub: "u1", desde: "A2" }, refreshToken: "R2" });
  assert.equal(captured.url, base.tokenEndpoint);
  assert.equal(captured.init.method, "POST");
  assert.match(captured.init.body, /grant_type=refresh_token/);
  assert.match(captured.init.body, /client_id=umbral-mobile/);
  assert.match(captured.init.body, /refresh_token=R1/);
});

test("sin refresh token devuelve ok:false sin llamar la red", async () => {
  const fetchImpl = async () => {
    throw new Error("no debe llamarse");
  };
  const r = await refrescarTokenFlow({ ...base, refreshToken: null, fetchImpl });
  assert.deepEqual(r, { ok: false });
});

test("HTTP no-ok devuelve ok:false", async () => {
  const fetchImpl = async () => ({ ok: false, json: async () => ({}) });
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: false });
});

test("respuesta sin access_token devuelve ok:false", async () => {
  const fetchImpl = async () => ({ ok: true, json: async () => ({}) });
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: false });
});

test("buildUser que lanza (JWT invalido) devuelve ok:false", async () => {
  const fetchImpl = async () => ({ ok: true, json: async () => ({ access_token: "basura" }) });
  const buildUser = () => {
    throw new Error("jwt invalido");
  };
  const r = await refrescarTokenFlow({ ...base, fetchImpl, buildUser });
  assert.deepEqual(r, { ok: false });
});

test("respuesta sin refresh_token nuevo conserva refreshToken null", async () => {
  const fetchImpl = async () => ({ ok: true, json: async () => ({ access_token: "A2" }) });
  const r = await refrescarTokenFlow({ ...base, fetchImpl });
  assert.deepEqual(r, { ok: true, token: "A2", user: { sub: "u1", desde: "A2" }, refreshToken: null });
});
