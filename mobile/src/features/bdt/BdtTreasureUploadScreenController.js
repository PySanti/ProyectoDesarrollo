import React, { useEffect, useState } from "react";
import { submitTreasureUpload, validateTreasureImage } from "./bdtTreasureUploadFlow.js";

const emptyStyles = {};

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
 * @param {{
 *   apiBaseUrl: string,
 *   token: string,
 *   partidaId: string,
 *   etapaId: string,
 *   components: any,
 *   styles?: any,
 *   requestImagePermission?: () => Promise<{ granted: boolean, unavailable?: boolean }>,
 *   requestGeolocationPermission?: () => Promise<{ granted: boolean }>,
 *   pickImage?: () => Promise<{ cancelled?: boolean, image?: { uri: string, name: string, type: string, size?: number } }>,
 *   fetchImpl?: typeof fetch,
 *   formDataFactory?: () => FormData,
 * }} props
 */
export function BdtTreasureUploadScreenController({
  apiBaseUrl,
  token,
  partidaId,
  etapaId,
  components,
  styles = emptyStyles,
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

  const { ActivityIndicator, Pressable, SafeAreaView, ScrollView, Text, View } = components;

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

    setSuccessMessage(result.data.mensaje ?? "Tesoro recibido para validacion.");
  };

  const canSubmit = Boolean(selectedImage) && !loading && !imagePermissionDenied && !geolocationDenied;

  return React.createElement(
    SafeAreaView,
    { style: styles.safeArea },
    React.createElement(
      ScrollView,
      { contentContainerStyle: styles.container },
      React.createElement(Text, { style: styles.title }, "Subir tesoro QR"),
      React.createElement(Text, { style: styles.description }, "Toma o selecciona una foto del QR encontrado. La validacion autoritativa la realiza el backend."),
      imagePermissionDenied ? React.createElement(Text, { style: styles.error }, "Debes permitir camara o imagenes para subir el tesoro QR.") : null,
      geolocationDenied ? React.createElement(Text, { style: styles.error }, "Debes permitir geolocalizacion para participar en la BDT activa.") : null,
      errorMessage ? React.createElement(Text, { style: styles.error }, errorMessage) : null,
      successMessage ? React.createElement(Text, { style: styles.success }, successMessage) : null,
      selectedImage
        ? React.createElement(
            View,
            { style: styles.card },
            React.createElement(Text, { style: styles.cardTitle }, "Imagen seleccionada"),
            React.createElement(Text, { style: styles.cardLine }, selectedImage.name),
            React.createElement(Text, { style: styles.cardLine }, selectedImage.type),
          )
        : React.createElement(Text, { style: styles.empty }, "Aun no has seleccionado una imagen."),
      loading ? React.createElement(ActivityIndicator, { color: "#0b5fff" }) : null,
      React.createElement(
        Pressable,
        { accessibilityRole: "button", onPress: selectImage, style: styles.secondaryButton },
        React.createElement(Text, { style: styles.secondaryButtonText }, "Tomar o seleccionar foto"),
      ),
      React.createElement(
        Pressable,
        { accessibilityRole: "button", disabled: !canSubmit, onPress: submit, style: canSubmit ? styles.joinButton : styles.disabledButton },
        React.createElement(Text, { style: styles.joinButtonText }, loading ? "Subiendo..." : "Subir tesoro"),
      ),
    ),
  );
}
