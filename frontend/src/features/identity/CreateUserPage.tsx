import { FormEvent, useState } from "react";
import {
  createIdentityUser,
  CreateUserResponse,
  IdentityApiError
} from "../../api/identityApi";
import { Field } from "../../shared/Field";
import { correo, nombrePersona, useField } from "../../shared/validation";

type Role = "Administrador" | "Operador" | "Participante";

const roles: Role[] = ["Administrador", "Operador", "Participante"];

interface CreateUserPageProps {
  accessToken: string;
}

export function CreateUserPage({ accessToken }: CreateUserPageProps) {
  const name = useField("", nombrePersona);
  const email = useField("", correo);
  const [initialRole, setInitialRole] = useState<Role>("Participante");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Errores por campo devueltos por el backend (400), tienen prioridad hasta editar el campo.
  const [serverFieldErrors, setServerFieldErrors] = useState<Record<string, string>>({});
  const [result, setResult] = useState<CreateUserResponse | null>(null);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setResult(null);
    setServerFieldErrors({});

    // Fuerza mostrar "obligatorio" en campos vacios no tocados.
    name.markTouched();
    email.markTouched();
    if (name.error || email.error) {
      return;
    }

    setLoading(true);
    try {
      const created = await createIdentityUser(
        {
          name: name.value.trim(),
          email: email.value.trim(),
          initialRole
        },
        accessToken
      );
      setResult(created);
      name.reset();
      email.reset();
      setInitialRole("Participante");
    } catch (caught) {
      if (caught instanceof IdentityApiError) {
        if (caught.fieldErrors) {
          setServerFieldErrors(caught.fieldErrors);
        }
        setError(mapErrorMessage(caught.statusCode, caught.message));
      } else {
        setError("Error inesperado al crear usuario.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="page">
      <div className="card stack">
        <header className="create-head">
          <div>
            <h1>Crear usuario con rol inicial</h1>
            <p className="muted">
              Panel de creación de usuarios.
            </p>
          </div>
        </header>

        {error ? (
          <div role="alert" className="notice error">
            {error}
          </div>
        ) : null}

        {result ? (
          <div className="notice success" data-testid="create-success">
            Usuario creado: <strong>{result.name}</strong> ({result.email}) - rol {" "}
            <strong>{result.role}</strong>
          </div>
        ) : null}

        <form onSubmit={onSubmit} noValidate>
          <Field
            id="name"
            name="name"
            label="Nombre"
            value={name.value}
            error={serverFieldErrors.name || name.visibleError}
            onChange={(event) => {
              name.onChange(event.target.value);
              setServerFieldErrors((previous) => ({ ...previous, name: "" }));
            }}
            onBlur={name.markTouched}
            autoComplete="name"
          />

          <div className="row">
            <Field
              id="email"
              name="email"
              label="Correo"
              type="email"
              value={email.value}
              error={serverFieldErrors.email || email.visibleError}
              onChange={(event) => {
                email.onChange(event.target.value);
                setServerFieldErrors((previous) => ({ ...previous, email: "" }));
              }}
              onBlur={email.markTouched}
              autoComplete="email"
            />

            <label htmlFor="initialRole">
              Rol inicial
              <select
                id="initialRole"
                name="initialRole"
                value={initialRole}
                onChange={(event) => setInitialRole(event.target.value as Role)}
              >
                {roles.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <button type="submit" disabled={loading}>
            {loading ? "Creando usuario y enviando correo…" : "Crear usuario"}
          </button>
          {loading ? (
            <p className="muted" role="status" data-testid="create-status">
              Creando el usuario y enviando el correo con su contraseña temporal…
            </p>
          ) : null}
        </form>
      </div>
    </div>
  );
}

function mapErrorMessage(statusCode: number, fallbackMessage: string): string {
  switch (statusCode) {
    case 400:
      return "Solicitud invalida. Verifica los campos.";
    case 403:
      return "No autorizado. Debes tener rol Administrador.";
    case 409:
      return "El correo ya existe en UMBRAL o Keycloak.";
    case 500:
      return "Error de persistencia local en Identity Service. El usuario no fue creado.";
    case 502:
      // El 502 puede venir de Keycloak o del envio del correo de bienvenida. En ambos casos
      // el backend revierte el alta (Keycloak + base local), asi que el usuario no queda creado.
      if (/smtp|email|correo/i.test(fallbackMessage)) {
        return "No se pudo enviar el correo de bienvenida con la contrasena temporal. El usuario no fue creado; revisa la configuracion de correo (SMTP) e intentalo nuevamente.";
      }
      return "Error de integracion con Keycloak. El usuario no fue creado; intentalo nuevamente.";
    default:
      return fallbackMessage;
  }
}
