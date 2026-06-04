export async function requestBdtGeolocationPermission(locationModuleLoader = () => import("expo-location")) {
  let Location;
  try {
    Location = await locationModuleLoader();
  } catch {
    return { granted: false, unavailable: true };
  }

  if (typeof Location.requestForegroundPermissionsAsync !== "function") {
    return { granted: false, unavailable: true };
  }

  const permission = await Location.requestForegroundPermissionsAsync();
  return {
    granted: permission?.status === "granted" || permission?.granted === true,
    unavailable: false,
  };
}
