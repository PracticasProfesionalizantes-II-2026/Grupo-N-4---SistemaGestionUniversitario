document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const contactForm = document.getElementById("teacherContactForm");
  const passwordForm = document.getElementById("teacherPasswordForm");
  const photoInput = document.getElementById("profilePhoto");
  const message = document.getElementById("profileMessage");
  const contactView = document.getElementById("teacherContactView");
  const passwordPanel = document.getElementById("teacherPasswordPanel");
  let currentProfile = null;

  function showMessage(text, error = false) {
    message.hidden = !text; message.textContent = text; message.className = `notice-strip${error ? " error" : ""}`;
  }
  const initials = name => String(name || "DO").split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase();
  function setText(id, value) { document.getElementById(id).textContent = value || "—"; }
  function renderContactView(profile) {
    setText("teacherEmailView", profile.emailPersonal);
    setText("teacherPhoneView", profile.telefonoContacto);
    setText("teacherAddressView", profile.direccion);
    setText("teacherCityView", [profile.localidad, profile.codigoPostal].filter(Boolean).join(" · "));
  }
  function setContactEditing(editing) {
    contactView.hidden = editing;
    contactForm.hidden = !editing;
    document.getElementById("editTeacherContact").hidden = editing;
    if (editing) contactForm.emailPersonal.focus();
  }
  function renderPhoto(name, url) {
    const currentVisual = document.getElementById("profileVisual");
    if (url) {
      const image = document.createElement("img"); image.className = "profile-photo"; image.src = url; image.alt = `Foto de perfil de ${name}`; image.id = "profileVisual"; currentVisual.replaceWith(image);
    } else {
      const fallback = document.createElement("div"); fallback.className = "profile-initials"; fallback.id = "profileVisual"; fallback.textContent = initials(name); fallback.setAttribute("aria-hidden", "true"); currentVisual.replaceWith(fallback);
    }
  }

  async function loadProfile() {
    try {
      const profile = await AcadionApi.request("/api/me/perfil");
      currentProfile = profile;
      const name = Acadion.formatearNombre([profile.nombre, profile.apellido].filter(Boolean).join(" "));
      setText("profileName", name); setText("profileSpecialty", profile.especialidad || "Especialidad sin informar"); setText("profileTitle", profile.tituloAcademico || "Título académico sin informar"); setText("profileDni", String(profile.dni || "")); setText("profileUsername", profile.nombreUsuario); setText("profileInstitutionalEmail", profile.emailInstitucional); setText("profileStatus", profile.estado);
      contactForm.emailPersonal.value = profile.emailPersonal || ""; contactForm.telefonoContacto.value = profile.telefonoContacto || ""; contactForm.direccion.value = profile.direccion || ""; contactForm.localidad.value = profile.localidad || ""; contactForm.codigoPostal.value = profile.codigoPostal || "";
      renderContactView(profile);
      renderPhoto(name, profile.fotoPerfilUrl);
    } catch (error) { showMessage(error.message, true); }
  }

  contactForm.addEventListener("submit", async event => {
    event.preventDefault(); const button = contactForm.querySelector("button"); button.disabled = true;
    try {
      await AcadionApi.request("/api/me/perfil", { method: "PUT", body: JSON.stringify({ emailPersonal: contactForm.emailPersonal.value.trim(), telefonoContacto: contactForm.telefonoContacto.value.trim(), direccion: contactForm.direccion.value.trim(), localidad: contactForm.localidad.value.trim(), codigoPostal: Number(contactForm.codigoPostal.value || 0) }) });
      await loadProfile();
      setContactEditing(false);
      showMessage("Tus datos de contacto se guardaron correctamente.");
    } catch (error) { showMessage(error.message, true); }
    finally { button.disabled = false; }
  });

  photoInput.addEventListener("change", async () => {
    const file = photoInput.files[0]; if (!file) return;
    if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) { showMessage("Seleccioná una imagen JPEG, PNG o WebP de hasta 5 MB.", true); photoInput.value = ""; return; }
    const data = new FormData(); data.append("foto", file); showMessage("Guardando foto...");
    try { await AcadionApi.request("/api/me/foto-perfil", { method: "POST", body: data }); showMessage("La foto de perfil se actualizó correctamente."); await loadProfile(); }
    catch (error) { showMessage(error.message, true); }
    finally { photoInput.value = ""; }
  });

  passwordForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (passwordForm.nueva.value !== passwordForm.confirmacion.value) { showMessage("Las contraseñas nuevas no coinciden.", true); return; }
    const button = passwordForm.querySelector("button"); button.disabled = true;
    try { await AcadionApi.request("/api/auth/cambiar-password", { method: "POST", body: JSON.stringify({ passwordActual: passwordForm.actual.value, passwordNueva: passwordForm.nueva.value }) }); passwordForm.reset(); passwordPanel.hidden = true; showMessage("La contraseña se actualizó correctamente."); }
    catch (error) { showMessage(error.message, true); }
    finally { button.disabled = false; }
  });
  document.getElementById("editTeacherContact").addEventListener("click", () => setContactEditing(true));
  document.getElementById("cancelTeacherContact").addEventListener("click", () => {
    if (currentProfile) {
      contactForm.emailPersonal.value = currentProfile.emailPersonal || "";
      contactForm.telefonoContacto.value = currentProfile.telefonoContacto || "";
      contactForm.direccion.value = currentProfile.direccion || "";
      contactForm.localidad.value = currentProfile.localidad || "";
      contactForm.codigoPostal.value = currentProfile.codigoPostal || "";
    }
    setContactEditing(false);
  });
  document.getElementById("toggleTeacherPassword").addEventListener("click", () => {
    passwordPanel.hidden = false;
    passwordPanel.scrollIntoView({ behavior: "smooth", block: "start" });
    passwordForm.actual.focus();
  });
  document.getElementById("closeTeacherPassword").addEventListener("click", () => {
    passwordForm.reset(); passwordPanel.hidden = true;
  });
  loadProfile();
});
