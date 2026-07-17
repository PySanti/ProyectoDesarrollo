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
//
// Ademas la posicion (juegoOrden) es MUTABLE: el wizard permite subir/bajar juegos, asi que
// un archivo ya descargado/impreso queda con el nombre congelado (el atributo `download` solo
// se lee al hacer click), pero el mismo juego puede terminar en otra posicion tras reordenar y
// enviar. Por eso juego+etapa sirven solo de pista legible y pueden quedar desactualizados; lo
// que garantiza unicidad *por construccion* es el prefijo del propio codigo de la etapa (unico
// por regla de dominio). Solo se rebana para el nombre de archivo: el codigo en si nunca se
// transforma en ningun otro lugar (se guarda, se envia y se codifica en el QR literal).
export function nombreArchivoQr(juegoOrden: number, etapaOrden: number, codigo: string): string {
  const prefijoCodigo = codigo.split("-")[0];
  return `tesoro-juego-${juegoOrden}-etapa-${etapaOrden}-${prefijoCodigo}.png`;
}
