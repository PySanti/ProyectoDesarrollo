import { describe, expect, it } from "vitest";
import { correo, nombreEquipo, nombrePersona } from "./validation";

describe("nombrePersona", () => {
  it.each(["****", "1234", "!@#$", "   ", ""])(
    "rechaza %j (sin letra u obligatorio)",
    (value) => {
      expect(nombrePersona(value)).not.toBeNull();
    }
  );

  it("acepta un nombre real con acentos", () => {
    expect(nombrePersona("José Pérez")).toBeNull();
  });

  it("rechaza mas de 120 caracteres", () => {
    expect(nombrePersona("a".repeat(121))).not.toBeNull();
  });
});

describe("nombreEquipo", () => {
  it("rechaza solo simbolos", () => {
    expect(nombreEquipo("****")).not.toBeNull();
  });
  it("acepta un nombre valido", () => {
    expect(nombreEquipo("Los Gordos")).toBeNull();
  });
});

describe("correo", () => {
  it.each(["", "sin-arroba", "a@b", "a@b.", "@b.com", "espacio @b.com"])(
    "rechaza %j",
    (value) => {
      expect(correo(value)).not.toBeNull();
    }
  );

  it("acepta un correo valido", () => {
    expect(correo("ana@test.com")).toBeNull();
  });
});
