document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const form = document.getElementById("photoForm");
  const input = document.getElementById("photoFile");
  const preview = document.getElementById("photoPreview");
  const message = document.getElementById("formMessage");
  const submit = form.querySelector("button[type='submit']");
  let previewUrl;

  async function loadCurrentPhoto() {
    try {
      const profile = await AcadionApi.request("/api/me/perfil");
      if (!profile.fotoPerfilUrl) return;
      preview.src = profile.fotoPerfilUrl;
      preview.alt = "Foto de perfil actual";
      preview.hidden = false;
    } catch {
      // El avatar generado permanece disponible si todavía no existe una foto.
    }
  }

  input.addEventListener("change", () => {
    const file = input.files[0];
    message.textContent = "";
    if (!file) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type) || file.size > 5 * 1024 * 1024) {
      input.value = "";
      message.textContent = "Seleccioná una imagen JPEG, PNG o WebP de hasta 5 MB.";
      return;
    }
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    previewUrl = URL.createObjectURL(file);
    preview.src = previewUrl;
    preview.alt = "Vista previa de la nueva foto";
    preview.hidden = false;
  });

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const file = input.files[0];
    if (!file) return;
    submit.disabled = true;
    submit.textContent = "Guardando…";
    message.textContent = "";
    const body = new FormData();
    body.append("foto", file);
    try {
      const result = await AcadionApi.request("/api/me/foto-perfil", { method: "POST", body });
      preview.src = result.fotoPerfilUrl;
      preview.alt = "Foto de perfil actual";
      input.value = "";
      document.querySelectorAll(".profile-avatar-image").forEach(image => { image.src = result.fotoPerfilUrl; });
      Acadion.mostrarMensaje("La foto de perfil se actualizó correctamente.");
    } catch (error) {
      message.textContent = error.message || "No fue posible guardar la foto.";
    } finally {
      submit.disabled = false;
      submit.textContent = "Guardar foto";
    }
  });

  loadCurrentPhoto();
});
