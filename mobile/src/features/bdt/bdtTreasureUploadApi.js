export async function uploadBdtTreasure(apiBaseUrl, token, partidaId, etapaId, image, fetchImpl = fetch, formDataFactory = () => new FormData()) {
  const formData = formDataFactory();
  formData.append("image", {
    uri: image.uri,
    name: image.name,
    type: image.type,
  });

  let response;
  try {
    response = await fetchImpl(
      `${apiBaseUrl}/api/bdt/games/${encodeURIComponent(partidaId)}/stages/${encodeURIComponent(etapaId)}/treasures`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: formData,
      },
    );
  } catch {
    return {
      ok: false,
      type: "network",
      message: "No se pudo conectar con el servidor. Verifica tu conexion e intenta de nuevo.",
    };
  }

  if (response.status === 400) {
    return { ok: false, type: "invalidRequest", message: "La imagen o la etapa seleccionada no es valida." };
  }

  if (response.status === 401) {
    return { ok: false, type: "unauthorized", message: "Sesion expirada o no autorizada." };
  }

  if (response.status === 403) {
    return { ok: false, type: "forbidden", message: "No estas registrado como participante activo de esta BDT." };
  }

  if (response.status === 404) {
    return { ok: false, type: "notFound", message: "La partida o etapa BDT ya no esta disponible." };
  }

  if (response.status === 409) {
    return { ok: false, type: "conflict", message: "La etapa ya no acepta subidas de tesoro." };
  }

  if (response.status === 413) {
    return { ok: false, type: "tooLarge", message: "La imagen no puede superar 5 MB." };
  }

  if (response.status === 415) {
    return { ok: false, type: "unsupportedType", message: "Solo se aceptan imagenes JPEG o PNG." };
  }

  if (!response.ok) {
    return { ok: false, type: "error", message: "No se pudo subir el tesoro QR." };
  }

  const data = await response.json();
  return { ok: true, data };
}
