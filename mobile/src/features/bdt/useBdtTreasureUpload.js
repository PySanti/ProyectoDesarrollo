import { useEffect, useState } from "react";
import { submitTreasureUpload, validateTreasureImage } from "./bdtTreasureUploadFlow.js";

async function defaultRequestImagePermission() {
  return { granted: false, unavailable: true };
}

async function defaultRequestGeolocationPermission() {
  return { granted: false, unavailable: true };
}

async function defaultPickImage() {
  return { cancelled: true };
}

/**
 * Orquestación de la subida de tesoro QR (HU-45), extraída del antiguo controller para que la UI sea
 * presentacional (registro de juego). Conserva las dependencias inyectables (permiso de imagen y de
 * geolocalización, selector de imagen, fetch, FormData) y la lógica testeada:
 *   - gatea la subida tras los permisos (imagen + geolocalización),
 *   - valida la imagen seleccionada (tipo/tamaño) antes de habilitar el envío,
 *   - envía al backend, que es **autoritativo** para decodificar/validar el QR.
 * Expone además `uploadResult` (estado de procesamiento, QR) para la reacción de resultado.
 *
 * @param {{
 *   apiBaseUrl: string, token: string, partidaId: string, etapaId: string,
 *   requestImagePermission?: () => Promise<{ granted: boolean, unavailable?: boolean }>,
 *   requestGeolocationPermission?: () => Promise<{ granted: boolean }>,
 *   pickImage?: () => Promise<{ cancelled?: boolean, image?: any }>,
 *   fetchImpl?: typeof fetch,
 *   formDataFactory?: () => FormData,
 * }} props
 */
export function useBdtTreasureUpload({
  apiBaseUrl,
  token,
  partidaId,
  etapaId,
  requestImagePermission = defaultRequestImagePermission,
  requestGeolocationPermission = defaultRequestGeolocationPermission,
  pickImage = defaultPickImage,
  fetchImpl = fetch,
  formDataFactory = () => new FormData(),
}) {
  const [imagePermissionDenied, setImagePermissionDenied] = useState(false);
  const [geolocationDenied, setGeolocationDenied] = useState(false);
  const [selectedImage, setSelectedImage] = useState(null);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState(null);
  const [successMessage, setSuccessMessage] = useState(null);
  const [uploadResult, setUploadResult] = useState(null);

  useEffect(() => {
    let active = true;
    requestGeolocationPermission().then((permission) => {
      if (active) {
        setGeolocationDenied(!permission?.granted);
      }
    });

    return () => {
      active = false;
    };
  }, [requestGeolocationPermission]);

  const selectImage = async () => {
    setErrorMessage(null);
    setSuccessMessage(null);
    setUploadResult(null);
    const permission = await requestImagePermission();
    if (!permission?.granted) {
      setImagePermissionDenied(true);
      setSelectedImage(null);
      return;
    }

    setImagePermissionDenied(false);
    const result = await pickImage();
    if (result?.cancelled) {
      return;
    }

    const image = result?.image;
    const validation = validateTreasureImage(image);
    if (!validation.ok) {
      setErrorMessage(validation.message);
      setSelectedImage(null);
      return;
    }

    setSelectedImage(image);
  };

  const submit = async () => {
    if (imagePermissionDenied || geolocationDenied) {
      setErrorMessage("Debes conceder permisos de imagen y geolocalizacion para subir el tesoro.");
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setSuccessMessage(null);

    const result = await submitTreasureUpload({
      apiBaseUrl,
      token,
      partidaId,
      etapaId,
      image: selectedImage,
      fetchImpl,
      formDataFactory,
    });

    setLoading(false);
    if (!result.ok) {
      setErrorMessage(result.message);
      return;
    }

    setUploadResult(result.data);
    setSuccessMessage(result.data.mensaje ?? "Tesoro recibido para validacion.");
  };

  const canSubmit = Boolean(selectedImage) && !loading && !imagePermissionDenied && !geolocationDenied;

  return {
    imagePermissionDenied,
    geolocationDenied,
    selectedImage,
    loading,
    errorMessage,
    successMessage,
    uploadResult,
    canSubmit,
    selectImage,
    submit,
  };
}
