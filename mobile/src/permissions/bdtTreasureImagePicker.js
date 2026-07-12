export async function requestBdtTreasureImagePermission(imagePickerModuleLoader = () => import("expo-image-picker")) {
  const ImagePicker = await loadImagePicker(imagePickerModuleLoader);
  if (!ImagePicker) {
    return { granted: false, unavailable: true };
  }

  const mediaPermission = typeof ImagePicker.requestMediaLibraryPermissionsAsync === "function"
    ? await ImagePicker.requestMediaLibraryPermissionsAsync()
    : null;
  const cameraPermission = typeof ImagePicker.requestCameraPermissionsAsync === "function"
    ? await ImagePicker.requestCameraPermissionsAsync()
    : null;

  return {
    granted: isGranted(mediaPermission) || isGranted(cameraPermission),
    unavailable: false,
  };
}

export async function pickBdtTreasureImage(imagePickerModuleLoader = () => import("expo-image-picker"), source = "library") {
  const ImagePicker = await loadImagePicker(imagePickerModuleLoader);
  if (!ImagePicker) {
    return { cancelled: true };
  }

  const launch = source === "camera" && typeof ImagePicker.launchCameraAsync === "function"
    ? ImagePicker.launchCameraAsync
    : ImagePicker.launchImageLibraryAsync;

  if (typeof launch !== "function") {
    return { cancelled: true };
  }

  const result = await launch({
    mediaTypes: ImagePicker.MediaTypeOptions?.Images ?? "Images",
    quality: 0.9,
    base64: true,
  });

  if (result?.canceled || result?.cancelled) {
    return { cancelled: true };
  }

  const asset = result?.assets?.[0];
  if (!asset?.uri) {
    return { cancelled: true };
  }

  return {
    image: {
      uri: asset.uri,
      base64: asset.base64,
      name: asset.fileName || buildFileName(asset.uri, asset.mimeType),
      type: asset.mimeType || inferContentType(asset.uri),
      size: asset.fileSize,
    },
  };
}

async function loadImagePicker(imagePickerModuleLoader) {
  try {
    return await imagePickerModuleLoader();
  } catch {
    return null;
  }
}

function isGranted(permission) {
  return permission?.status === "granted" || permission?.granted === true;
}

function buildFileName(uri, mimeType) {
  const lastSegment = uri.split("/").filter(Boolean).pop();
  if (lastSegment?.includes(".")) {
    return lastSegment;
  }

  return `tesoro.${mimeType === "image/png" ? "png" : "jpg"}`;
}

function inferContentType(uri) {
  return uri.toLowerCase().endsWith(".png") ? "image/png" : "image/jpeg";
}
