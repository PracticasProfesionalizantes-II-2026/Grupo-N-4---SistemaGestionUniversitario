document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  if (!AcadionApi.getSession() || document.body.dataset.section !== "director") return;

  const message = document.getElementById("settingsMessage");
  const photoInput = document.getElementById("profilePhoto");
  const passwordForm = document.getElementById("directorPasswordForm");

  function showMessage(text, error = false) {
    message.hidden = !text;
    message.textContent = text;
    message.className = `notice-strip${error ? " error" : ""}`;
  }

  const initials = value => String(value || "Directivo").split(/\s+/).filter(Boolean).slice(0, 2)
    .map(part => part[0]).join("").toUpperCase();

  function renderPhoto(name, url) {
    const current = document.getElementById("profileVisual");
    const replacement = url ? document.createElement("img") : document.createElement("div");
    replacement.id = "profileVisual";
    if (url) {
      replacement.className = "profile-photo";
      replacement.src = url;
      replacement.alt = `Foto de perfil de ${name}`;
    } else {
      replacement.className = "profile-initials";
      replacement.textContent = initials(name);
      replacement.setAttribute("aria-hidden", "true");
    }
    current.replaceWith(replacement);
  }

  async function loadProfile() {
    try {
      const profile = await AcadionApi.request("/api/me/perfil");
      const name = Acadion.formatearNombre([profile.nombre, profile.apellido].filter(Boolean).join(" ")) || "Directivo";
      document.getElementById("profileName").textContent = name;
      document.getElementById("profileUsername").textContent = profile.nombreUsuario || "Cuenta institucional";
      renderPhoto(name, profile.fotoPerfilUrl);
    } catch (error) {
      showMessage(error.message, true);
    }
  }

  photoInput.addEventListener("change", async () => {
    const file = photoInput.files[0];
    if (!file) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type) || file.size > 5 * 1024 * 1024) {
      showMessage("Seleccioná una imagen JPEG, PNG o WebP de hasta 5 MB.", true);
      photoInput.value = "";
      return;
    }
    const data = new FormData();
    data.append("foto", file);
    showMessage("Guardando foto...");
    try {
      await AcadionApi.request("/api/me/foto-perfil", { method: "POST", body: data });
      await loadProfile();
      showMessage("La foto de perfil se actualizó correctamente.");
    } catch (error) {
      showMessage(error.message, true);
    } finally {
      photoInput.value = "";
    }
  });

  passwordForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (passwordForm.nueva.value !== passwordForm.confirmacion.value) {
      showMessage("Las contraseñas nuevas no coinciden.", true);
      return;
    }
    const submit = passwordForm.querySelector("button[type='submit']");
    submit.disabled = true;
    try {
      await AcadionApi.request("/api/auth/cambiar-password", {
        method: "POST",
        body: JSON.stringify({
          passwordActual: passwordForm.actual.value,
          passwordNueva: passwordForm.nueva.value
        })
      });
      passwordForm.reset();
      showMessage("La contraseña se actualizó correctamente.");
    } catch (error) {
      showMessage(error.message, true);
    } finally {
      submit.disabled = false;
    }
  });

  loadProfile();
});
