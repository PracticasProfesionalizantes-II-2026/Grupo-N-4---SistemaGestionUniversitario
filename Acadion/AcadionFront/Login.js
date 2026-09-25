const loginForm = document.getElementById("loginForm");
const usuarioInput = document.getElementById("usuario");
const passwordInput = document.getElementById("password");
const errorMessage = document.getElementById("errorMessage");
const togglePasswordButton = document.getElementById("togglePassword");
const submitButton = document.getElementById("submitButton");

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

  submitButton.disabled = true;
  submitButton.textContent = "Validando...";

  try {
    const response = await fetch("http://localhost:5050/api/auth/login", {
      method: "POST",
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

      if (!loginResult.personaId || !loginResult.token) {
        throw new Error("La respuesta de inicio de sesión está incompleta");
      }

      AcadionApi.saveSession(loginResult);
      const isAdmin = loginResult.rol === "Secretario" ||
        loginResult.permisos?.includes("usuarios.gestionar");
      const isTeacher = loginResult.rol === "Docente" ||
        loginResult.permisos?.includes("docencia.gestionar");
      window.location.href = isAdmin
        ? "PanelDirectivo.html"
        : isTeacher ? "PanelDocente.html" : "MenuPrincipal.html";
      return;
    }

    let errorData = null;
    try { errorData = await response.json(); } catch { errorData = null; }
    errorMessage.textContent = response.status === 401
      ? "Usuario o contraseña incorrecta"
      : errorData?.mensaje || "No fue posible iniciar sesión. Intentá nuevamente.";

    errorMessage.classList.add("is-visible");
  } catch {
    errorMessage.textContent = "No se pudo conectar con el servidor.";
    errorMessage.classList.add("is-visible");
  } finally {
    submitButton.disabled = false;
    submitButton.textContent = "Continuar";
  }
});
