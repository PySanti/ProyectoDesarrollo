// Validadores puros: devuelven el mensaje de error o null si el valor es valido.
// Misma semantica que el backend (ReglasTextoHumano / EmailAddress) y que el web
// (frontend/src/shared/validation.ts): un input que pasa aca deberia pasar el
// validator del servidor, que sigue siendo la autoridad.

const TIENE_LETRA = /\p{L}/u;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function nombreEquipo(value) {
  const v = typeof value === "string" ? value : "";
  if (!v.trim()) return "El nombre del equipo es obligatorio.";
  if (!TIENE_LETRA.test(v)) return "Debe contener al menos una letra.";
  if (v.trim().length > 120) return "Maximo 120 caracteres.";
  return null;
}

export function nombrePersona(value) {
  const v = typeof value === "string" ? value : "";
  if (!v.trim()) return "El nombre es obligatorio.";
  if (!TIENE_LETRA.test(v)) return "Debe contener al menos una letra.";
  if (v.trim().length > 120) return "Maximo 120 caracteres.";
  return null;
}

export function correo(value) {
  const v = typeof value === "string" ? value : "";
  if (!v.trim()) return "El correo es obligatorio.";
  if (!EMAIL.test(v.trim())) return "El correo no es valido.";
  if (v.trim().length > 320) return "Maximo 320 caracteres.";
  return null;
}
