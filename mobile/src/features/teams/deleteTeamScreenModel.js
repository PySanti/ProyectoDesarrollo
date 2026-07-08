import { submitDeleteTeam } from "./deleteTeamFlow.js";
import { getDeleteTeamSuccessMessage } from "./deleteTeamScreenContent.js";

export async function submitDeleteTeamFromScreen({
  apiBaseUrl,
  token,
  submitFn = submitDeleteTeam,
  onDeleted,
  setLoading,
  setErrorMessage,
  setSuccessMessage,
}) {
  setLoading(true);
  setErrorMessage(null);
  setSuccessMessage(null);

  let result;
  try {
    result = await submitFn({ apiBaseUrl, token });
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

  setSuccessMessage(getDeleteTeamSuccessMessage());
  onDeleted?.(result.data);
}
