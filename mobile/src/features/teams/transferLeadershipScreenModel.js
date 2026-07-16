import { submitTransferLeadership } from "./transferLeadershipFlow.js";

export function getTransferLeadershipSuccessMessage() {
  return "Liderazgo transferido correctamente. Ahora puedes salir del equipo si lo deseas.";
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
    const message = "Ocurrio un error inesperado. Intenta nuevamente.";
    setErrorMessage(message);
    return { ok: false, message };
  }

  setLoading(false);

  if (!result.ok) {
    setErrorMessage(result.message);
    return result;
  }

  setSuccessMessage(getTransferLeadershipSuccessMessage(result.data));
  onTransferred?.(result.data);
  return result;
}
