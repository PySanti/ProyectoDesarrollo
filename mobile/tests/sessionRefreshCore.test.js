import test from "node:test";
import assert from "node:assert/strict";
import { crearSessionRefreshCore } from "../src/auth/sessionRefreshCore.js";

function armar(refrescarOk = true) {
  const llamadas = { refrescar: 0, modal: [], expirada: 0 };
  const core = crearSessionRefreshCore({
    refrescar: async () => {
      llamadas.refrescar += 1;
      return refrescarOk;
    },
    onModal: (v) => llamadas.modal.push(v),
    onExpirada: () => {
      llamadas.expirada += 1;
    },
  });
  return { core, llamadas };
}

test("tick con actividad refresca en silencio y consume la actividad", async () => {
  const { core, llamadas } = armar();
  core.marcarActividad();
  await core.tick();
  assert.equal(llamadas.refrescar, 1);
  assert.deepEqual(llamadas.modal, []);
  await core.tick();
  assert.deepEqual(llamadas.modal, [true]);
  assert.equal(llamadas.refrescar, 1);
});

test("tick sin actividad abre modal sin refrescar", async () => {
  const { core, llamadas } = armar();
  await core.tick();
  assert.deepEqual(llamadas.modal, [true]);
  assert.equal(llamadas.refrescar, 0);
});

test("con modal abierto los ticks se ignoran", async () => {
  const { core, llamadas } = armar();
  await core.tick();
  core.marcarActividad();
  await core.tick();
  await core.tick();
  assert.equal(llamadas.refrescar, 0);
  assert.deepEqual(llamadas.modal, [true]);
});

test("continuar refresca y cierra el modal en exito", async () => {
  const { core, llamadas } = armar();
  await core.tick();
  await core.continuar();
  assert.equal(llamadas.refrescar, 1);
  assert.deepEqual(llamadas.modal, [true, false]);
});

test("continuar sin modal es no-op", async () => {
  const { core, llamadas } = armar();
  await core.continuar();
  assert.equal(llamadas.refrescar, 0);
});

test("refresh fallido en tick dispara onExpirada", async () => {
  const { core, llamadas } = armar(false);
  core.marcarActividad();
  await core.tick();
  assert.equal(llamadas.expirada, 1);
});

test("refresh fallido desde continuar deja el modal abierto y expira", async () => {
  const { core, llamadas } = armar(false);
  await core.tick();
  await core.continuar();
  assert.equal(llamadas.expirada, 1);
  assert.deepEqual(llamadas.modal, [true]);
});
