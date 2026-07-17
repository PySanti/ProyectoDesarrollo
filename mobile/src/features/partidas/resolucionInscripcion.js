// Copy de la resolucion de una solicitud (HU-19). El rechazo NO es terminal: el backend deja
// volver a solicitar (OcupaParticipacion = Pendiente|Activa), asi que el texto lo dice.
export function avisoResolucion(aceptada) {
  return aceptada
    ? { variant: "success", texto: "Tu solicitud fue aceptada. Estás dentro." }
    : { variant: "error", texto: "El operador rechazó tu solicitud. Puedes volver a solicitar." };
}
