import QRCode from "qrcode";

// El codigo del tesoro lo genera el cliente porque el operador tiene que verlo y poder
// regenerarlo antes de guardar, y Partidas no admite editar un juego ya creado. El backend
// no se fia: EtapaBDT.Crear exige que sea un UUID.
export function generarCodigoTesoro(): string {
  return crypto.randomUUID();
}

// El data-URL sirve a la vez para el <img> de la previsualizacion y para el href del enlace
// de descarga: no hace falta renderizar dos veces ni pasar por un blob.
export function renderizarQrDataUrl(codigo: string): Promise<string> {
  return QRCode.toDataURL(codigo, { width: 320, margin: 2 });
}

// orden es unico por juego, no por partida: una partida puede tener varios juegos BDT (1..*,
// sin tope), y cada uno numera sus propias etapas desde 1. Si el nombre solo mirara el orden
// de la etapa, "juego 2 etapa 1" y "juego 3 etapa 1" colisionarian en el mismo archivo pese a
// ser tesoros distintos.
export function nombreArchivoQr(juegoOrden: number, etapaOrden: number): string {
  return `tesoro-juego-${juegoOrden}-etapa-${etapaOrden}.png`;
}
