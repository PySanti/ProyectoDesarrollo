import { describe, expect, it } from "vitest";
import { describirDetalle } from "./detalleEvento";

describe("describirDetalle", () => {
  it("sin detalle util devuelve lista vacia", () => {
    expect(describirDetalle(null)).toEqual([]);
    expect(describirDetalle(undefined)).toEqual([]);
    expect(describirDetalle({})).toEqual([]);
    expect(describirDetalle("texto suelto")).toEqual([]);
  });

  it("EtapaBDTGanada: etiqueta el puntaje y pasa el tiempo de ms a segundos", () => {
    const campos = describirDetalle({
      etapaId: "abcdef12-0000-0000-0000-000000000000",
      puntaje: 50,
      tiempoResolucionMs: 1234
    });
    expect(campos).toEqual([
      { label: "Etapa", value: "abcdef12", mono: true },
      { label: "Puntaje", value: "50" },
      { label: "Tiempo de resolución", value: `${(1.234).toLocaleString(undefined, { maximumFractionDigits: 1 })} s` }
    ]);
  });

  it("las fechas se muestran en hora local, no en ISO crudo", () => {
    const campos = describirDetalle({ instante: "2026-07-17T09:46:00Z" });
    expect(campos).toEqual([
      { label: "Instante", value: new Date("2026-07-17T09:46:00Z").toLocaleString() }
    ]);
  });

  it("RespuestaTriviaValidada: el booleano se lee como Si/No", () => {
    expect(describirDetalle({ esCorrecta: true })).toEqual([{ label: "¿Correcta?", value: "Sí" }]);
    expect(describirDetalle({ esCorrecta: false })).toEqual([{ label: "¿Correcta?", value: "No" }]);
  });

  it("PartidaCancelada: motivo y fecha de cancelacion", () => {
    const campos = describirDetalle({
      motivo: "MinimosNoAlcanzados",
      fechaCancelacion: "2026-07-17T09:46:00Z"
    });
    expect(campos[0]).toEqual({ label: "Motivo", value: "MinimosNoAlcanzados" });
    expect(campos[1].label).toBe("Cancelación");
  });

  it("PistaEnviada: el texto de la pista se muestra tal cual", () => {
    const campos = describirDetalle({ texto: "mira bajo el banco", instante: "2026-07-17T09:46:00Z" });
    expect(campos[0]).toEqual({ label: "Texto", value: "mira bajo el banco" });
  });

  it("EtapaBDTActivada: el tiempo limite lleva su unidad", () => {
    const campos = describirDetalle({ orden: 1, tiempoLimiteSegundos: 60 });
    expect(campos).toEqual([
      { label: "Orden", value: "1" },
      { label: "Tiempo límite", value: "60 s" }
    ]);
  });

  it("los nulos se omiten en vez de ensuciar la celda", () => {
    // PreguntaTriviaCerrada por tiempo: textoOpcionCorrecta puede venir null.
    const campos = describirDetalle({ motivo: "Tiempo", textoOpcionCorrecta: null });
    expect(campos).toEqual([{ label: "Motivo", value: "Tiempo" }]);
  });

  it("una clave no documentada no se pierde: se muestra con su nombre humanizado", () => {
    // El backend manda el payload entero menos los ids extraidos: si el contrato crece,
    // la celda debe seguir contando lo que llega en vez de tragarselo.
    const campos = describirDetalle({ campoNuevoDelBackend: "valor" });
    expect(campos).toEqual([{ label: "Campo nuevo del backend", value: "valor" }]);
  });
});
