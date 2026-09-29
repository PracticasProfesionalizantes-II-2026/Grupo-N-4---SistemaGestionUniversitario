document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();

  const form = document.getElementById("passwordForm");
  const message = document.getElementById("formMessage");
  const submitButton = form?.querySelector("button[type='submit']");

  if (!form || !message || !submitButton) return;

  form.addEventListener("submit", async event => {
    event.preventDefault();
    message.textContent = "";
    submitButton.disabled = true;
    submitButton.textContent = "Guardando...";

    const actual = form.elements.actual.value;
    const nueva = form.elements.nueva.value;
    const confirmacion = form.elements.confirmacion.value;

    if (nueva !== confirmacion) {
      message.textContent = "Las contraseñas nuevas no coinciden.";
      submitButton.disabled = false;
      submitButton.textContent = "Guardar";
      return;
    }

    if (nueva.length < 8) {
      message.textContent = "La contraseña nueva no cumple con los requisitos: debe tener mínimo 8 caracteres.";
      submitButton.disabled = false;
      submitButton.textContent = "Guardar";
      return;
    }

    try {
      await AcadionApi.request("/api/auth/cambiar-password", {
        method: "POST",
        body: JSON.stringify({
          passwordActual: actual,
          passwordNueva: nueva
        })
      });
      window.location.href = "ContrasenaCambiada.html";
    } catch (error) {
      message.textContent = error.message;
      submitButton.disabled = false;
      submitButton.textContent = "Guardar";
    }
  });
});

