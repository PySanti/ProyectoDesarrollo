import { describe, expect, it } from "vitest";
import { etiquetaTipoEvento, TIPOS_EVENTO } from "./eventoLabels";

describe("TIPOS_EVENTO", () => {
  it("ofrece los eventos de inscripcion que el backend ya proyecta al historial", () => {
    // HistorialEventMapper proyecta 22 tipos; el filtro solo ofrecia 17, asi que estos
    // eventos llegaban (visibles con "Todos") pero no se podian aislar.
    expect(TIPOS_EVENTO).toEqual(
      expect.arrayContaining([
        "InscripcionSolicitada",
        "InscripcionAceptada",
        "InscripcionRechazada",
        "InscripcionEquipoCreada",
        "InscripcionEquipoCancelada"
      ])
    );
  });

  it("no ofrece filtros muertos: todo tipo listado lo proyecta el backend", () => {
    expect(new Set(TIPOS_EVENTO).size).toBe(TIPOS_EVENTO.length);
    expect(TIPOS_EVENTO).toHaveLength(22);
  });
});

describe("etiquetaTipoEvento", () => {
  it("respeta los acronimos del dominio en vez de partirlos", () => {
    expect(etiquetaTipoEvento("EtapaBDTGanada")).toBe("Etapa BDT ganada");
    expect(etiquetaTipoEvento("TesoroQRValidado")).toBe("Tesoro QR validado");
  });

  it("nombra los eventos de inscripcion distinguiendo individual de equipo", () => {
    expect(etiquetaTipoEvento("InscripcionAceptada")).toBe("Inscripción aceptada");
    expect(etiquetaTipoEvento("InscripcionEquipoCreada")).toBe("Preinscripción de equipo creada");
  });

  it("un tipo que el backend agregue despues se muestra humanizado, no en blanco", () => {
    expect(etiquetaTipoEvento("AlgoNuevoOcurrio")).toBe("Algo nuevo ocurrio");
  });
});
