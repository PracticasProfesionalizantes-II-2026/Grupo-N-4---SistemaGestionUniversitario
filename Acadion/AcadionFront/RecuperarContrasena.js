document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const form = document.getElementById("recoveryRequestForm");
  const message = document.getElementById("formMessage");
  const button = form.querySelector("button[type='submit']");

  AcadionApi.request("/api/auth/recuperacion/disponible")
    .then(result => {
      if (result.disponible) return;
      form.hidden = true;
      message.textContent = "La recuperación por correo no está habilitada. Contactá a Secretaría para restablecer tu acceso.";
      message.className = "error";
    })
    .catch(() => {
      form.hidden = true;
      message.textContent = "No fue posible verificar el servicio de recuperación. Contactá a Secretaría.";
      message.className = "error";
    });

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const email = form.email.value.trim().toLowerCase();
    message.textContent = "";
    button.disabled = true;
    button.textContent = "Enviando código…";
    try {
      const result = await AcadionApi.request("/api/auth/recuperacion/solicitar", {
        method: "POST",
        body: JSON.stringify({ email })
      });
      sessionStorage.setItem("acadion_recovery_email", email);
      sessionStorage.removeItem("acadion_recovery_token");
      message.textContent = result.mensaje;
      message.className = "success";
      window.setTimeout(() => { window.location.href = "IngresarCodigo.html"; }, 900);
    } catch (error) {
      message.textContent = error.message;
      message.className = "error";
    } finally {
      button.disabled = false;
      button.textContent = "Enviar código";
    }
  });
});
