import { useState } from "react";

// Validadores puros: devuelven el mensaje de error o null si el valor es valido.
// Misma semantica que el backend (ReglasTextoHumano / EmailAddress): un input que
// pasa aca deberia pasar el validator del servidor, que sigue siendo la autoridad.

export type Validator = (value: string) => string | null;

const TIENE_LETRA = /\p{L}/u;
// Formato de correo pragmatico (no RFC completo): algo@algo.algo sin espacios.
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function nombrePersona(value: string): string | null {
  if (!value.trim()) return "El nombre es obligatorio.";
  if (!TIENE_LETRA.test(value)) return "Debe contener al menos una letra.";
  if (value.trim().length > 120) return "Maximo 120 caracteres.";
  return null;
}

export function nombreEquipo(value: string): string | null {
  if (!value.trim()) return "El nombre del equipo es obligatorio.";
  if (!TIENE_LETRA.test(value)) return "Debe contener al menos una letra.";
  if (value.trim().length > 120) return "Maximo 120 caracteres.";
  return null;
}

export function correo(value: string): string | null {
  if (!value.trim()) return "El correo es obligatorio.";
  if (!EMAIL.test(value.trim())) return "El correo no es valido.";
  if (value.trim().length > 320) return "Maximo 320 caracteres.";
  return null;
}

/**
 * Estado de un campo con validacion en vivo.
 * - Errores de formato se ven desde la primera tecla (valor no vacio).
 * - "Obligatorio" (valor vacio) se ve recien al salir del campo (blur) o al enviar,
 *   para no arrancar el formulario en rojo. `markTouched()` fuerza mostrarlo (submit).
 */
export function useField(initial: string, validate: Validator) {
  const [value, setValue] = useState(initial);
  const [touched, setTouched] = useState(false);

  const error = validate(value);
  const visibleError = value.trim() !== "" || touched ? error : null;

  return {
    value,
    error,
    visibleError,
    onChange: setValue,
    markTouched: () => setTouched(true),
    reset: () => {
      setValue(initial);
      setTouched(false);
    },
  };
}
