import { FormEvent, useState } from "react";
import {
  createIdentityUser,
  CreateUserResponse,
  IdentityApiError
} from "../../api/identityApi";

type Role = "Administrador" | "Operador" | "Participante";

const roles: Role[] = ["Administrador", "Operador", "Participante"];

interface CreateUserPageProps {
  accessToken: string;
}

interface FormState {
  name: string;
  email: string;
  initialRole: Role;
}

export function CreateUserPage({ accessToken }: CreateUserPageProps) {
  const [form, setForm] = useState<FormState>({
    name: "",
    email: "",
    initialRole: "Participante"
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<CreateUserResponse | null>(null);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setResult(null);

    if (!form.name.trim()) {
      setError("El nombre es obligatorio.");
      return;
    }

    if (!form.email.trim() || !form.email.includes("@")) {
      setError("El correo es invalido.");
      return;
    }

    setLoading(true);
    try {
      const created = await createIdentityUser(
        {
          name: form.name.trim(),
          email: form.email.trim(),
          initialRole: form.initialRole
        },
        accessToken
      );
      setResult(created);
      setForm({ name: "", email: "", initialRole: "Participante" });
    } catch (caught) {
      if (caught instanceof IdentityApiError) {
        const mapped = mapErrorMessage(caught.statusCode, caught.message);
        setError(mapped);
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
              Alta administrativa gestionada por Keycloak e Identity Service. El{" "}
              <strong>rol inicial</strong> se asigna ahora y no se cambia desde Gestión de usuarios.
              Al crear la cuenta se enviará un <strong>correo</strong> con la contraseña temporal
              para el primer inicio de sesión.
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
          <label htmlFor="name">
            Nombre
            <input
              id="name"
              name="name"
              value={form.name}
              onChange={(event) =>
                setForm((previous) => ({ ...previous, name: event.target.value }))
              }
              autoComplete="name"
            />
          </label>

          <div className="row">
            <label htmlFor="email">
              Correo
              <input
                id="email"
                name="email"
                type="email"
                value={form.email}
                onChange={(event) =>
                  setForm((previous) => ({ ...previous, email: event.target.value }))
                }
                autoComplete="email"
              />
            </label>

            <label htmlFor="initialRole">
              Rol inicial
              <select
                id="initialRole"
                name="initialRole"
                value={form.initialRole}
                onChange={(event) =>
                  setForm((previous) => ({
                    ...previous,
                    initialRole: event.target.value as Role
                  }))
                }
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
