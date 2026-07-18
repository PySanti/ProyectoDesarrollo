

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
