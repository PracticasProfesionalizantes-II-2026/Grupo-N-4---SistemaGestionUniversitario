document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const email = sessionStorage.getItem("acadion_recovery_email");
  if (!email) {
    window.location.replace("RecuperarContrasena.html");
    return;
  }
  const form = document.getElementById("recoveryCodeForm");
  const message = document.getElementById("formMessage");
  const resend = document.getElementById("resendCode");
  document.getElementById("recoveryDestination").textContent = email.replace(/^(.{2}).*(@.*)$/, "$1••••$2");

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const button = form.querySelector("button[type='submit']");
    button.disabled = true;
    message.textContent = "Verificando…";
    try {
      const result = await AcadionApi.request("/api/auth/recuperacion/verificar", {
        method: "POST",
        body: JSON.stringify({ email, codigo: form.codigo.value.trim() })
      });
      sessionStorage.setItem("acadion_recovery_token", result.token);
      window.location.href = "NuevaContrasena.html";
    } catch (error) {
      message.textContent = error.message;
      message.className = "error";
    } finally {
      button.disabled = false;
    }
  });

  resend.addEventListener("click", async () => {
    resend.disabled = true;
    try {
      const result = await AcadionApi.request("/api/auth/recuperacion/solicitar", {
        method: "POST", body: JSON.stringify({ email })
      });
      message.textContent = result.mensaje;
      message.className = "success";
      let seconds = 60;
      const timer = window.setInterval(() => {
        seconds--;
        resend.textContent = seconds > 0 ? `Reenviar código (${seconds}s)` : "Reenviar código";
        if (seconds <= 0) { window.clearInterval(timer); resend.disabled = false; }
      }, 1000);
    } catch (error) {
      message.textContent = error.message;
      message.className = "error";
      resend.disabled = false;
    }
  });
});
