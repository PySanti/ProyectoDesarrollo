// Politica de reconexion de SignalR para los hubs en vivo (sesion y ranking).
//
// El default de withAutomaticReconnect() reintenta 4 veces (0,2,10,30s) y se RINDE
// para siempre. En BDT el jugador camina compartiendo ubicacion y cruza zonas muertas
// de red; rendirse a los 30s lo deja sordo el resto de la partida (no llegan preguntas,
// pistas ni etapas). nextRetryDelayInMilliseconds nunca devuelve null => reintenta
// indefinidamente: backoff corto al principio (microcortes) y luego cada 30s por siempre.
export const reconexionIndefinida = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount }) => {
    const backoff = [0, 2000, 5000, 10000];
    return previousRetryCount < backoff.length ? backoff[previousRetryCount] : 30000;
  },
};
