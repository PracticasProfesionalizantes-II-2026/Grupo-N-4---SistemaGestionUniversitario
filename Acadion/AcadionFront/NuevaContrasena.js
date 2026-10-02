document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const token = sessionStorage.getItem("acadion_recovery_token");
  if (!token) {
    window.location.replace("RecuperarContrasena.html");
    return;
  }
  const form = document.getElementById("newPasswordForm");
  const message = document.getElementById("formMessage");
  form.addEventListener("submit", async event => {
    event.preventDefault();
    const nueva = form.nueva.value;
    const confirmacion = form.confirmacion.value;
    if (nueva !== confirmacion) {
      message.textContent = "Las contraseñas no coinciden.";
      message.className = "error";
      return;
    }
    const button = form.querySelector("button[type='submit']");
    button.disabled = true;
    try {
      await AcadionApi.request("/api/auth/recuperacion/restablecer", {
        method: "POST", body: JSON.stringify({ token, passwordNueva: nueva })
      });
      sessionStorage.removeItem("acadion_recovery_email");
      sessionStorage.removeItem("acadion_recovery_token");
      window.location.href = "ContrasenaCambiada.html";
    } catch (error) {
      message.textContent = error.message;
      message.className = "error";
      button.disabled = false;
    }
  });
});
