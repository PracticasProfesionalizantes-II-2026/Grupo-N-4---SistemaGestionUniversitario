document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const form = document.getElementById("careerForm");
  const message = document.getElementById("careerMessage");
  const saveButton = document.getElementById("saveCareer");
  [form.nombre, form.planEstudios].forEach(input => input.addEventListener("input", () => {
    input.value = input.value.replace(/[!"#$%]/g, "");
  }));

  function showMessage(text, success = false) {
    message.textContent = text;
    message.className = `status-message ${success ? "success" : "error"}`;
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const academicText = /^[\p{L}\p{M}\p{N} .()/\-]+$/u;
    if (!academicText.test(form.nombre.value.trim()) ||
        !academicText.test(form.planEstudios.value.trim())) {
      showMessage("El nombre y el plan de estudios contienen caracteres no permitidos.");
      return;
    }
    saveButton.disabled = true;
    const payload = {
      nombre: form.nombre.value.trim(),
      tipo: form.tipo.value,
      planEstudios: form.planEstudios.value.trim(),
      duracionAnios: Number(form.duracionAnios.value),
      capacidadMaximaEstudiantes: Number(form.capacidadMaximaEstudiantes.value)
    };

    try {
      await AcadionApi.request("/carreras/", { method: "POST", body: JSON.stringify(payload) });
      form.reset();
      showMessage("Carrera creada correctamente con todos sus años académicos.", true);
    } catch (error) {
      showMessage(error.message);
    } finally {
      saveButton.disabled = false;
    }
  });
});
