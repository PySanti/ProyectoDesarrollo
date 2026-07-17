// UMBRAL login theme — validacion de input en el formulario de login SIN tocar los
// templates .ftl (el theme es CSS-only a proposito, resiliente entre versiones de
// Keycloak). Se inyecta via `scripts=` en theme.properties.
//
// El login de UMBRAL es SOLO por correo, asi que marcamos el campo de identidad como
// email + requerido y la contrasena como requerida: el navegador bloquea el envio con
// correo mal formado o campos vacios (validacion nativa al enviar, sin feedback en vivo).
// Keycloak sigue siendo la autoridad del lado del servidor.

(function () {
  function reforzarLogin() {
    var usuario = document.getElementById("username");
    if (usuario) {
      usuario.type = "email";
      usuario.setAttribute("required", "");
      usuario.setAttribute("autocomplete", "email");
    }

    var password = document.getElementById("password");
    if (password) {
      password.setAttribute("required", "");
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", reforzarLogin);
  } else {
    reforzarLogin();
  }
})();
