import { InputHTMLAttributes } from "react";

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  /** Mensaje de error bajo el campo; marca el input como invalido para lectores de pantalla. */
  error?: string | null;
  /** Ayuda bajo el campo cuando no hay error. */
  hint?: string;
}

/**
 * Campo de formulario con label, mensaje de error por campo y accesibilidad:
 * aria-invalid + aria-describedby apuntando al mensaje. Espejo web del Field de mobile.
 */
export function Field({ id, label, error, hint, onBlur, ...rest }: FieldProps) {
  const describedBy = error ? `${id}-error` : hint ? `${id}-hint` : undefined;

  return (
    <label htmlFor={id}>
      {label}
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        onBlur={onBlur}
        {...rest}
      />
      {error ? (
        <span id={`${id}-error`} className="field-error" role="alert">
          {error}
        </span>
      ) : hint ? (
        <span id={`${id}-hint`} className="field-hint">
          {hint}
        </span>
      ) : null}
    </label>
  );
}
