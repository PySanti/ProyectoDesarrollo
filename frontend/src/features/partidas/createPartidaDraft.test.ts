import { describe, expect, it } from "vitest";
import {
  buildCreatePartidaRequest,
  buildJuegoRequest,
  initialDraft,
  newEtapa,
  newJuegoBdt,
  newJuegoTrivia,
  newPregunta,
  validateDraft,
  validateHeader,
  validateJuego,
  type EtapaDraft,
  type HeaderDraft,
  type JuegoDraft,
  type PreguntaDraft
} from "./createPartidaDraft";

function validHeader(overrides: Partial<HeaderDraft> = {}): HeaderDraft {
  return {
    nombrePartida: "Partida demo",
    modalidad: "Individual",
    modoInicioPartida: "Manual",
    tiempoInicio: "",
    minimosParticipacion: "1",
    maximosParticipacion: "5",
    ...overrides
  };
}

function validPregunta(overrides: Partial<PreguntaDraft> = {}): PreguntaDraft {
  return {
    texto: "Cual es la capital de Venezuela?",
    opciones: [
      { texto: "Caracas", esCorrecta: true },
      { texto: "Maracaibo", esCorrecta: false }
    ],
    puntaje: "100",
    tiempoLimiteSegundos: "30",
    ...overrides
  };
}

function validEtapa(overrides: Partial<EtapaDraft> = {}): EtapaDraft {
  return {
    codigoQREsperado: "TESORO-1",
    puntaje: "50",
    tiempoLimiteSegundos: "60",
    ...overrides
  };
}

describe("initialDraft", () => {
  it("arranca en step 1, header vacio con modalidad Individual y modo Manual, cero juegos", () => {
    const draft = initialDraft();

    expect(draft.step).toBe(1);
    expect(draft.juegos).toEqual([]);
    expect(draft.header).toEqual({
      nombrePartida: "",
      modalidad: "Individual",
      modoInicioPartida: "Manual",
      tiempoInicio: "",
      minimosParticipacion: "",
      maximosParticipacion: ""
    });
  });
});

describe("validateHeader", () => {
  it("rechaza nombre vacio", () => {
    const errors = validateHeader(validHeader({ nombrePartida: "" }));
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza minimosParticipacion = 0", () => {
    const errors = validateHeader(validHeader({ minimosParticipacion: "0" }));
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza maximosParticipacion < minimosParticipacion", () => {
    const errors = validateHeader(
      validHeader({ minimosParticipacion: "5", maximosParticipacion: "2" })
    );
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza modo Automatico sin tiempoInicio", () => {
    const errors = validateHeader(
      validHeader({ modoInicioPartida: "Automatico", tiempoInicio: "" })
    );
    expect(errors.length).toBeGreaterThan(0);
  });

  it("acepta el caso feliz Manual sin tiempoInicio", () => {
    const errors = validateHeader(validHeader());
    expect(errors).toEqual([]);
  });
});

describe("validateJuego - Trivia", () => {
  function triviaWith(preguntas: PreguntaDraft[]): JuegoDraft {
    return { ...newJuegoTrivia(), preguntas };
  }

  it("rechaza un juego sin preguntas", () => {
    const errors = validateJuego(triviaWith([]));
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza pregunta con 1 sola opcion", () => {
    const errors = validateJuego(
      triviaWith([validPregunta({ opciones: [{ texto: "Unica", esCorrecta: true }] })])
    );
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza pregunta con dos opciones correctas", () => {
    const errors = validateJuego(
      triviaWith([
        validPregunta({
          opciones: [
            { texto: "A", esCorrecta: true },
            { texto: "B", esCorrecta: true }
          ]
        })
      ])
    );
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza pregunta sin ninguna opcion correcta", () => {
    const errors = validateJuego(
      triviaWith([
        validPregunta({
          opciones: [
            { texto: "A", esCorrecta: false },
            { texto: "B", esCorrecta: false }
          ]
        })
      ])
    );
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza pregunta con puntaje 0", () => {
    const errors = validateJuego(triviaWith([validPregunta({ puntaje: "0" })]));
    expect(errors.length).toBeGreaterThan(0);
  });

  it("acepta una pregunta bien formada", () => {
    const errors = validateJuego(triviaWith([validPregunta()]));
    expect(errors).toEqual([]);
  });
});

describe("validateJuego - BDT", () => {
  function bdtWith(areaBusqueda: string, etapas: EtapaDraft[]): JuegoDraft {
    return { ...newJuegoBdt(), areaBusqueda, etapas };
  }

  it("rechaza area de busqueda vacia", () => {
    const errors = validateJuego(bdtWith("", [validEtapa()]));
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza un juego sin etapas", () => {
    const errors = validateJuego(bdtWith("Plaza Bolivar", []));
    expect(errors.length).toBeGreaterThan(0);
  });

  it("rechaza etapa sin codigo QR", () => {
    const errors = validateJuego(
      bdtWith("Plaza Bolivar", [validEtapa({ codigoQREsperado: "" })])
    );
    expect(errors).toContain("Genera el código QR de la etapa 1");
  });

  it("rechaza etapa con tiempo limite 0", () => {
    const errors = validateJuego(
      bdtWith("Plaza Bolivar", [validEtapa({ tiempoLimiteSegundos: "0" })])
    );
    expect(errors.length).toBeGreaterThan(0);
  });

  it("acepta el caso feliz", () => {
    const errors = validateJuego(bdtWith("Plaza Bolivar", [validEtapa()]));
    expect(errors).toEqual([]);
  });
});

describe("validateDraft", () => {
  it("acumula errores de header y de cada juego", () => {
    const draft = {
      step: 1 as const,
      header: validHeader({ nombrePartida: "" }),
      juegos: [{ ...newJuegoTrivia(), preguntas: [] }]
    };
    const errors = validateDraft(draft);
    expect(errors.length).toBeGreaterThanOrEqual(2);
  });

  it("rechaza un draft sin juegos", () => {
    const draft = { step: 1 as const, header: validHeader(), juegos: [] };
    const errors = validateDraft(draft);
    expect(errors.length).toBeGreaterThan(0);
  });

  it("acepta un draft completo y valido", () => {
    const draft = {
      step: 1 as const,
      header: validHeader(),
      juegos: [{ ...newJuegoTrivia(), preguntas: [validPregunta()] }]
    };
    expect(validateDraft(draft)).toEqual([]);
  });
});

describe("buildCreatePartidaRequest", () => {
  it("modo Manual produce tiempoInicio: null", () => {
    const request = buildCreatePartidaRequest(validHeader());
    expect(request).toEqual({
      nombrePartida: "Partida demo",
      modalidad: "Individual",
      modoInicioPartida: "Manual",
      tiempoInicio: null,
      minimosParticipacion: 1,
      maximosParticipacion: 5
    });
  });

  it("modo Automatico produce tiempoInicio como ISO string", () => {
    const header = validHeader({
      modoInicioPartida: "Automatico",
      tiempoInicio: "2026-08-01T10:00"
    });
    const request = buildCreatePartidaRequest(header);
    expect(request.tiempoInicio).toBe(new Date("2026-08-01T10:00").toISOString());
  });
});

describe("buildJuegoRequest", () => {
  it("Trivia produce { orden, preguntas } con numeros convertidos", () => {
    const juego = { ...newJuegoTrivia(), preguntas: [validPregunta()] };
    const request = buildJuegoRequest(juego, 1);

    expect(request).toEqual({
      orden: 1,
      preguntas: [
        {
          texto: "Cual es la capital de Venezuela?",
          opciones: [
            { texto: "Caracas", esCorrecta: true },
            { texto: "Maracaibo", esCorrecta: false }
          ],
          puntaje: 100,
          tiempoLimiteSegundos: 30
        }
      ]
    });
  });

  it("BDT produce etapas con orden contiguo desde 1", () => {
    const juego = {
      ...newJuegoBdt(),
      areaBusqueda: "Plaza Bolivar",
      etapas: [validEtapa({ codigoQREsperado: "Q1" }), validEtapa({ codigoQREsperado: "Q2" })]
    };
    const request = buildJuegoRequest(juego, 2);

    expect(request).toEqual({
      orden: 2,
      areaBusqueda: "Plaza Bolivar",
      etapas: [
        { orden: 1, codigoQREsperado: "Q1", puntaje: 50, tiempoLimiteSegundos: 60 },
        { orden: 2, codigoQREsperado: "Q2", puntaje: 50, tiempoLimiteSegundos: 60 }
      ]
    });
  });
});

describe("factories", () => {
  it("newJuegoTrivia y newJuegoBdt generan localId unicos", () => {
    expect(newJuegoTrivia().localId).not.toBe(newJuegoTrivia().localId);
    expect(newJuegoBdt().localId).not.toBe(newJuegoBdt().localId);
  });

  it("newPregunta arranca con 2 opciones vacias, la primera esCorrecta", () => {
    const pregunta = newPregunta();
    expect(pregunta.opciones).toEqual([
      { texto: "", esCorrecta: true },
      { texto: "", esCorrecta: false }
    ]);
  });

  it("newEtapa arranca vacia", () => {
    expect(newEtapa()).toEqual({
      codigoQREsperado: "",
      puntaje: "",
      tiempoLimiteSegundos: ""
    });
  });
});
