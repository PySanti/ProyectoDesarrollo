// Traduce el `detalle` de un evento del historial a campos legibles por el operador.
//
// El backend (HistorialEventMapper) guarda ahi el payload del evento MENOS partidaId,
// sesionPartidaId, juegoId y los ids que ya viajan en columna propia (participante/equipo).
// O sea: las claves son las del contrato de eventos, y el conjunto crece cuando el contrato
// crece. Por eso no hay lista blanca por tipo de evento: se etiqueta lo conocido y lo
// desconocido se sigue mostrando con su nombre humanizado en vez de tragarselo.
export interface CampoDetalle {
  label: string;
  value: string;
  mono?: boolean;
}

const ETIQUETAS: Record<string, string> = {
  preguntaId: "Pregunta",
  opcionId: "Opción",
  opcionCorrectaId: "Opción correcta",
  textoOpcionCorrecta: "Respuesta correcta",
  etapaId: "Etapa",
  convocatoriaId: "Convocatoria",
  inscripcionId: "Inscripción",
  primerJuegoId: "Primer juego",
  primerJuegoOrden: "Orden del primer juego",
  esCorrecta: "¿Correcta?",
  puntaje: "Puntaje",
  tiempoRespuestaMs: "Tiempo de respuesta",
  tiempoResolucionMs: "Tiempo de resolución",
  tiempoLimiteSegundos: "Tiempo límite",
  instante: "Instante",
  fechaCierre: "Cierre",
  fechaActivacion: "Activación",
  fechaInicio: "Inicio",
  fechaFin: "Fin",
  fechaCancelacion: "Cancelación",
  motivo: "Motivo",
  resultado: "Resultado",
  texto: "Texto",
  orden: "Orden",
  tipoJuego: "Tipo de juego",
  modalidad: "Modalidad",
  estadoConvocatoria: "Respuesta",
  minimosParticipacion: "Mínimo de participación",
  maximosParticipacion: "Máximo de participación",
  latitud: "Latitud",
  longitud: "Longitud"
};

// "campoNuevoDelBackend" -> "Campo nuevo del backend"
function humanizar(clave: string): string {
  const separado = clave.replace(/([a-z0-9])([A-Z])/g, "$1 $2").toLowerCase();
  return separado.charAt(0).toUpperCase() + separado.slice(1);
}

const esFecha = (clave: string) => clave === "instante" || clave.startsWith("fecha");

function formatear(clave: string, valor: unknown): CampoDetalle | null {
  if (valor === null || valor === undefined) return null;

  const label = ETIQUETAS[clave] ?? humanizar(clave);

  if (typeof valor === "boolean") return { label, value: valor ? "Sí" : "No" };

  if (typeof valor === "string") {
    if (esFecha(clave)) return { label, value: new Date(valor).toLocaleString() };
    // Los ids sueltos (pregunta, etapa, opcion…) no tienen nombre que resolver: se acortan
    // como en el resto de pantallas y van en mono por la regla Mono For Machine Strings.
    if (clave.endsWith("Id")) return { label, value: valor.slice(0, 8), mono: true };
    return { label, value: valor };
  }

  if (typeof valor === "number") {
    if (clave.endsWith("Ms")) {
      return {
        label,
        value: `${(valor / 1000).toLocaleString(undefined, { maximumFractionDigits: 1 })} s`
      };
    }
    if (clave.endsWith("Segundos")) return { label, value: `${valor} s` };
    return { label, value: String(valor) };
  }

  return { label, value: JSON.stringify(valor), mono: true };
}

export function describirDetalle(detalle: unknown): CampoDetalle[] {
  if (detalle === null || typeof detalle !== "object" || Array.isArray(detalle)) return [];

  const campos: CampoDetalle[] = [];
  for (const [clave, valor] of Object.entries(detalle as Record<string, unknown>)) {
    const campo = formatear(clave, valor);
    if (campo) campos.push(campo);
  }
  return campos;
}
