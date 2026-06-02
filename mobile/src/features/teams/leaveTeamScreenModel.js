import { submitLeaveTeam } from "./leaveTeamFlow.js";
import { getLeaveTeamSuccessMessage } from "./leaveTeamScreenContent.js";

export async function submitLeaveTeamFromScreen({
  apiBaseUrl,
  token,
  submitFn = submitLeaveTeam,
  onLeft,
  setLoading,
  setErrorMessage,
  setSuccessMessage,
  setHasActiveTeam,
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

  setSuccessMessage(getLeaveTeamSuccessMessage());
  setHasActiveTeam?.(false);
  onLeft?.(result.data);
}
