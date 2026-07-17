// Estado puro del wizard de creacion de partida (draft, fabricas, validadores, builders).
// Sin React. Los campos numericos del draft son string (form-friendly); los builders
// convierten con Number(...). localId es solo para React keys, jamas viaja al backend.
import type {
  AddJuegoBdtRequest,
  AddJuegoTriviaRequest,
  CreatePartidaRequest,
  Modalidad,
  ModoInicioPartida
} from "../../api/partidasApi";

export interface HeaderDraft {
  nombrePartida: string;
  modalidad: Modalidad;
  modoInicioPartida: ModoInicioPartida;
  tiempoInicio: string;
  minimosParticipacion: string;
  maximosParticipacion: string;
}

export interface OpcionDraft {
  texto: string;
  esCorrecta: boolean;
}

export interface PreguntaDraft {
  texto: string;
  opciones: OpcionDraft[];
  puntaje: string;
  tiempoLimiteSegundos: string;
}

export interface EtapaDraft {
  codigoQREsperado: string;
  puntaje: string;
  tiempoLimiteSegundos: string;
}

export interface JuegoTriviaDraft {
  tipo: "Trivia";
  localId: string;
  preguntas: PreguntaDraft[];
}

export interface JuegoBdtDraft {
  tipo: "BusquedaDelTesoro";
  localId: string;
  areaBusqueda: string;
  etapas: EtapaDraft[];
}

export type JuegoDraft = JuegoTriviaDraft | JuegoBdtDraft;

export interface CreatePartidaDraft {
  step: 1 | 2 | 3;
  header: HeaderDraft;
  juegos: JuegoDraft[];
}

export function initialDraft(): CreatePartidaDraft {
  return {
    step: 1,
    header: {
      nombrePartida: "",
      modalidad: "Individual",
      modoInicioPartida: "Manual",
      tiempoInicio: "",
      minimosParticipacion: "",
      maximosParticipacion: ""
    },
    juegos: []
  };
}

export function newJuegoTrivia(): JuegoTriviaDraft {
  return { tipo: "Trivia", localId: crypto.randomUUID(), preguntas: [] };
}

export function newJuegoBdt(): JuegoBdtDraft {
  return { tipo: "BusquedaDelTesoro", localId: crypto.randomUUID(), areaBusqueda: "", etapas: [] };
}

export function newPregunta(): PreguntaDraft {
  return {
    texto: "",
    opciones: [
      { texto: "", esCorrecta: true },
      { texto: "", esCorrecta: false }
    ],
    puntaje: "",
    tiempoLimiteSegundos: ""
  };
}

export function newEtapa(): EtapaDraft {
  return { codigoQREsperado: "", puntaje: "", tiempoLimiteSegundos: "" };
}

export function validateHeader(header: HeaderDraft): string[] {
  const errors: string[] = [];

  if (!header.nombrePartida.trim()) {
    errors.push("El nombre de la partida es obligatorio.");
  } else if (!/\p{L}/u.test(header.nombrePartida)) {
    errors.push("El nombre de la partida debe contener al menos una letra.");
  }

  const min = Number(header.minimosParticipacion);
  if (!Number.isInteger(min) || min < 1) {
    errors.push("El minimo de participacion debe ser un numero entero mayor o igual a 1.");
  }

  const max = Number(header.maximosParticipacion);
  if (!Number.isInteger(max) || max < min) {
    errors.push("El maximo de participacion debe ser mayor o igual al minimo.");
  }

  if (header.modoInicioPartida !== "Manual" && !header.tiempoInicio.trim()) {
    errors.push("El tiempo de inicio es obligatorio para el modo de inicio seleccionado.");
  }

  return errors;
}

function validatePregunta(pregunta: PreguntaDraft, posicion: number): string[] {
  const errors: string[] = [];
  const n = posicion + 1;

  if (!pregunta.texto.trim()) {
    errors.push(`La pregunta ${n} debe tener un texto.`);
  }
  if (pregunta.opciones.length < 2 || pregunta.opciones.some((o) => !o.texto.trim())) {
    errors.push(`La pregunta ${n} debe tener al menos 2 opciones con texto.`);
  }
  const correctas = pregunta.opciones.filter((o) => o.esCorrecta).length;
  if (correctas !== 1) {
    errors.push(`La pregunta ${n} debe tener exactamente una opcion correcta.`);
  }
  if (!(Number(pregunta.puntaje) > 0)) {
    errors.push(`La pregunta ${n} debe tener un puntaje mayor que 0.`);
  }
  if (!(Number(pregunta.tiempoLimiteSegundos) > 0)) {
    errors.push(`La pregunta ${n} debe tener un tiempo limite mayor que 0.`);
  }

  return errors;
}

function validateEtapa(etapa: EtapaDraft, posicion: number): string[] {
  const errors: string[] = [];
  const n = posicion + 1;

  if (!etapa.codigoQREsperado.trim()) {
    errors.push(`La etapa ${n} debe tener un codigo QR esperado.`);
  }
  if (!(Number(etapa.puntaje) > 0)) {
    errors.push(`La etapa ${n} debe tener un puntaje mayor que 0.`);
  }
  if (!(Number(etapa.tiempoLimiteSegundos) > 0)) {
    errors.push(`La etapa ${n} debe tener un tiempo limite mayor que 0.`);
  }

  return errors;
}

export function validateJuego(juego: JuegoDraft): string[] {
  if (juego.tipo === "Trivia") {
    const errors: string[] = [];
    if (juego.preguntas.length === 0) {
      errors.push("El juego de trivia debe tener al menos una pregunta.");
    }
    juego.preguntas.forEach((pregunta, i) => errors.push(...validatePregunta(pregunta, i)));
    return errors;
  }

  const errors: string[] = [];
  if (!juego.areaBusqueda.trim()) {
    errors.push("El area de busqueda es obligatoria.");
  } else if (!/\p{L}/u.test(juego.areaBusqueda)) {
    errors.push("El area de busqueda debe contener al menos una letra.");
  }
  if (juego.etapas.length === 0) {
    errors.push("El juego de busqueda del tesoro debe tener al menos una etapa.");
  }
  juego.etapas.forEach((etapa, i) => errors.push(...validateEtapa(etapa, i)));
  return errors;
}

export function validateDraft(draft: CreatePartidaDraft): string[] {
  const errors = [...validateHeader(draft.header)];

  if (draft.juegos.length === 0) {
    errors.push("La partida debe tener al menos un juego.");
  }
  draft.juegos.forEach((juego) => errors.push(...validateJuego(juego)));

  return errors;
}

export function buildCreatePartidaRequest(header: HeaderDraft): CreatePartidaRequest {
  return {
    nombrePartida: header.nombrePartida,
    modalidad: header.modalidad,
    modoInicioPartida: header.modoInicioPartida,
    tiempoInicio:
      header.modoInicioPartida === "Manual" ? null : new Date(header.tiempoInicio).toISOString(),
    minimosParticipacion: Number(header.minimosParticipacion),
    maximosParticipacion: Number(header.maximosParticipacion)
  };
}

export function buildJuegoRequest(
  juego: JuegoDraft,
  orden: number
): AddJuegoTriviaRequest | AddJuegoBdtRequest {
  if (juego.tipo === "Trivia") {
    return {
      orden,
      preguntas: juego.preguntas.map((p) => ({
        texto: p.texto,
        opciones: p.opciones.map((o) => ({ texto: o.texto, esCorrecta: o.esCorrecta })),
        puntaje: Number(p.puntaje),
        tiempoLimiteSegundos: Number(p.tiempoLimiteSegundos)
      }))
    };
  }

  return {
    orden,
    areaBusqueda: juego.areaBusqueda,
    etapas: juego.etapas.map((e, i) => ({
      orden: i + 1,
      codigoQREsperado: e.codigoQREsperado,
      puntaje: Number(e.puntaje),
      tiempoLimiteSegundos: Number(e.tiempoLimiteSegundos)
    }))
  };
}
