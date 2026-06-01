import { submitJoinTeamByCode } from "./joinTeamFlow.js";

export async function submitJoinTeamFromScreen({
  apiBaseUrl,
  token,
  accessCode,
  submitFn = submitJoinTeamByCode,
  onJoined,
  setLoading,
  setErrorMessage,
  setSuccessMessage,
  setAccessCode,
}) {
  setLoading(true);
  setErrorMessage(null);
  setSuccessMessage(null);

  let result;
  try {
    result = await submitFn({
      apiBaseUrl,
      token,
      accessCode,
    });
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

  setAccessCode("");
  setSuccessMessage("Te uniste al equipo con exito.");
  onJoined?.(result.data);
}
