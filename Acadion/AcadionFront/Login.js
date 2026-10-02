const loginForm = document.getElementById("loginForm");
const usuarioInput = document.getElementById("usuario");
const passwordInput = document.getElementById("password");
const errorMessage = document.getElementById("errorMessage");
const togglePasswordButton = document.getElementById("togglePassword");
const submitButton = document.getElementById("submitButton");
const forgotPasswordLink = document.getElementById("forgotPasswordLink");

fetch(`${AcadionApi.baseUrl}/api/auth/recuperacion/disponible`)
  .then(response => response.ok ? response.json() : { disponible: false })
  .then(result => { forgotPasswordLink.hidden = !result.disponible; })
  .catch(() => { forgotPasswordLink.hidden = true; });

togglePasswordButton.addEventListener("click", () => {
  const passwordIsHidden = passwordInput.type === "password";

  passwordInput.type = passwordIsHidden ? "text" : "password";
  togglePasswordButton.setAttribute(
    "aria-label",
    passwordIsHidden ? "Ocultar contraseña" : "Mostrar contraseña"
  );
  togglePasswordButton.setAttribute("aria-pressed", String(passwordIsHidden));
});

loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  errorMessage.classList.remove("is-visible");

  const NombreUsuario = usuarioInput.value.trim();
  const Password = passwordInput.value;

  if (!NombreUsuario || !Password) {
    errorMessage.textContent = "Completa usuario y contraseña";
    errorMessage.classList.add("is-visible");
    return;
  }
  if (!/^[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*$/.test(NombreUsuario)) {
    errorMessage.textContent = "El usuario solo puede contener letras, números y puntos";
    errorMessage.classList.add("is-visible");
    return;
  }
  if (NombreUsuario.length > 80 || Password.length > 256) {
    errorMessage.textContent = "Los datos ingresados superan el tamaño permitido";
    errorMessage.classList.add("is-visible");
    return;
  }

  submitButton.disabled = true;
  submitButton.textContent = "Validando...";

  try {
    const response = await fetch(`${AcadionApi.baseUrl}/api/auth/login`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        NombreUsuario,
        Password
      })
    });

    if (response.ok) {
      const loginResult = await response.json();

      if (!loginResult.personaId || !loginResult.usuarioId) {
        throw new Error("La respuesta de inicio de sesión está incompleta");
      }

      AcadionApi.saveSession(loginResult);
      const sessionCheck = await fetch(`${AcadionApi.baseUrl}/api/auth/session`, {
        credentials: "include"
      });
      if (!sessionCheck.ok) {
        AcadionApi.clearSession();
        throw new Error("La sesión no pudo guardarse en el navegador. Volvé a intentarlo desde la dirección local de Acadion.");
      }
      const isAdmin = loginResult.rol === "Secretario" ||
        loginResult.permisos?.includes("usuarios.gestionar");
      const isTeacher = loginResult.rol === "Docente" ||
        loginResult.permisos?.includes("docencia.gestionar");
      const isDirector = loginResult.rol === "Directivo" &&
        loginResult.permisos?.includes("reportes.leer");
      window.location.href = isDirector
        ? "PanelInstitucional.html"
        : isAdmin
        ? "PanelSecretaria.html"
        : isTeacher ? "PanelDocente.html" : "MenuPrincipal.html";
      return;
    }

    let errorData = null;
    try { errorData = await response.json(); } catch { errorData = null; }
    errorMessage.textContent = response.status === 401
      ? "Usuario o contraseña incorrecta"
      : errorData?.mensaje || "No fue posible iniciar sesión. Intentá nuevamente.";

    errorMessage.classList.add("is-visible");
  } catch (error) {
    errorMessage.textContent = error?.message || "No se pudo conectar con el servidor.";
    errorMessage.classList.add("is-visible");
  } finally {
    submitButton.disabled = false;
    submitButton.textContent = "Continuar";
  }
});
