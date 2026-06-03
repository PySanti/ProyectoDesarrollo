import { submitTransferLeadership } from "./transferLeadershipFlow.js";

export function getTransferLeadershipSuccessMessage(result) {
  const nuevoLider = result?.nuevoLiderUserId ? ` Nuevo lider: ${result.nuevoLiderUserId}.` : "";
  return `Liderazgo transferido correctamente.${nuevoLider} Ahora puedes salir del equipo desde HU-07 si lo deseas.`;
}

export async function submitTransferLeadershipFromScreen({
  apiBaseUrl,
  token,
  nuevoLiderUserId,
  submitFn = submitTransferLeadership,
  onTransferred,
  setLoading,
  setErrorMessage,
  setSuccessMessage,
}) {
  setLoading(true);
  setErrorMessage(null);
  setSuccessMessage(null);

  let result;
  try {
    result = await submitFn({ apiBaseUrl, token, nuevoLiderUserId });
  } catch {
    setLoading(false);
    setErrorMessage("Ocurrio un error inesperado. Intenta nuevamente.");
    return;
  }

  setLoading(false);

  if (!result.ok) {
    setErrorMessage(result.message);
    return;
  }

  setSuccessMessage(getTransferLeadershipSuccessMessage(result.data));
  onTransferred?.(result.data);
}
