document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  document.querySelectorAll("[data-current-year]").forEach(element => {
    element.textContent = String(new Date().getFullYear());
  });

  const status = document.getElementById("profileMessage");
  const photoInput = document.getElementById("profilePhoto");
  const relativesList = document.getElementById("relativesList");
  const relativesEmpty = document.getElementById("relativesEmpty");
  const addRelativeButton = document.getElementById("addRelative");
  const passwordPanel = document.getElementById("studentPasswordPanel");
  const passwordForm = document.getElementById("studentPasswordForm");
  let profile = null;
  let relatives = [];

  function formatDate(value) {
    const match = String(value || "").match(/^(\d{4})-(\d{2})-(\d{2})/);
    return match ? `${match[3]}/${match[2]}/${match[1]}` : "—";
  }

  function setText(id, value) {
    document.getElementById(id).textContent = value === null || value === undefined || value === "" ? "—" : String(value);
  }

  function formatPaymentStatus(value) {
    const key = String(value || "pendiente").toLowerCase();
    return ({ aldia: "Al día", pendiente: "Pendiente", exentado: "Exentado", vencida: "Vencida", impaga: "Impaga" })[key] || value;
  }

  function showStatus(message, isError = false) {
    status.hidden = !message;
    status.textContent = message;
    status.className = `notice-strip${isError ? " error" : ""}`;
  }

  const initials = name => String(name || "ES").split(/\s+/).filter(Boolean).slice(0, 2)
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

  function fillProfile(data) {
    profile = data;
    const fullName = Acadion.formatearNombre([data.nombre, data.apellido].filter(Boolean).join(" "));
    setText("profileName", fullName);
    setText("profileCareer", data.carrera || "Carrera sin informar");
    setText("profileFile", data.legajo ? `Legajo ${data.legajo}` : "Legajo sin informar");
    setText("profileUsername", data.nombreUsuario);
    setText("dni", data.dni);
    setText("birthDate", formatDate(data.fechaNacimiento));
    setText("studentFile", data.legajo);
    setText("studentCareerName", data.carrera);
    setText("initialEnrollmentStatus", formatPaymentStatus(data.estadoMatriculaInicial));
    setText("currentTuitionStatus", `${formatPaymentStatus(data.estadoCuotaActual)} · ${data.periodoCuotaActual || "mes actual"}`);
    setText("finalExamEligibility", data.habilitadoFinales ? "Habilitado" : "Bloqueado: matrícula o cuotas pendientes");
    setText("institutionalEmail", data.emailInstitucional);
    setText("generalAverage", data.promedioCalculado !== null && Number.isFinite(Number(data.promedioCalculado))
      ? Number(data.promedioCalculado).toFixed(2)
      : "Sin calificaciones");
    setText("accountStatus", data.estado);
    renderPhoto(fullName, data.fotoPerfilUrl);

    document.getElementById("emailPersonal").value = data.emailPersonal || "";
    document.getElementById("phone").value = data.telefonoContacto || "";
    document.getElementById("address").value = data.direccion || "";
    document.getElementById("city").value = data.localidad || "";
    document.getElementById("postalCode").value = data.codigoPostal || "";
    setText("emailPersonalView", data.emailPersonal);
    setText("phoneView", data.telefonoContacto);
    setText("addressTextView", data.direccion);
    setText("cityTextView", [data.localidad, data.codigoPostal].filter(Boolean).join(" · "));

    const financing = data.financiamiento || {};
    document.querySelectorAll("#financeForm input[type='checkbox']").forEach(input => {
      input.checked = Boolean(financing[input.name]);
    });
    relatives = Array.isArray(data.allegados) ? data.allegados.map(item => ({ ...item })) : [];
    renderRelatives();
  }

  async function loadProfile() {
    showStatus("Cargando tus datos…");
    try {
      const [data, summaryData] = await Promise.all([
        AcadionApi.request("/api/me/perfil"),
        AcadionApi.request("/api/me/resumen-academico")
      ]);
      const grades = (Array.isArray(summaryData) ? summaryData : [])
        .filter(item => item.calificacionFinal !== null && item.calificacionFinal !== undefined)
        .map(item => Number(item.calificacionFinal)).filter(Number.isFinite);
      data.promedioCalculado = grades.length
        ? grades.reduce((total, grade) => total + grade, 0) / grades.length
        : null;
      fillProfile(data);
      showStatus("");
    } catch (error) {
      showStatus(error.message || "No pudimos cargar tus datos personales.", true);
    }
  }

  function setEditing(formId, editing) {
    const form = document.getElementById(formId);
    const view = document.getElementById(formId === "contactForm" ? "contactView" : "addressView");
    form.hidden = !editing;
    view.hidden = editing;
    document.querySelector(`[data-edit='${formId}']`).hidden = editing;
    if (editing) form.querySelector("input")?.focus();
  }

  function editablePayload() {
    return {
      emailPersonal: document.getElementById("emailPersonal").value.trim(),
      telefonoContacto: document.getElementById("phone").value.trim(),
      direccion: document.getElementById("address").value.trim(),
      localidad: document.getElementById("city").value.trim(),
      codigoPostal: Number(document.getElementById("postalCode").value || 0)
    };
  }

  async function saveEditableForm(formId) {
    try {
      await AcadionApi.request("/api/me/perfil", { method: "PUT", body: JSON.stringify(editablePayload()) });
      setEditing(formId, false);
      profile = { ...profile, ...editablePayload() };
      fillProfile(profile);
      Acadion.mostrarMensaje("Tus datos se guardaron correctamente.");
    } catch (error) {
      showStatus(error.message || "No fue posible guardar los cambios.", true);
    }
  }

  function relativeRow(relative, index) {
    const row = document.createElement("div");
    row.className = "relative-row";

    const nameLabel = document.createElement("label");
    nameLabel.textContent = "Nombre y apellido";
    const name = document.createElement("input");
    name.type = "text"; name.maxLength = 120; name.required = true; name.value = relative.nombreApellido || "";
    name.addEventListener("input", () => { relatives[index].nombreApellido = name.value; });
    nameLabel.append(name);

    const relationLabel = document.createElement("label");
    relationLabel.textContent = "Relación";
    const relation = document.createElement("select");
    relation.required = true;
    ["", "Madre", "Padre", "Hermano/a", "Pareja", "Tutor/a", "Otro familiar", "Otra"].forEach(value => {
      const option = document.createElement("option");
      option.value = value; option.textContent = value || "Seleccionar";
      relation.append(option);
    });
    relation.value = relative.relacion || "";
    relation.addEventListener("change", () => { relatives[index].relacion = relation.value; });
    relationLabel.append(relation);

    const phoneLabel = document.createElement("label");
    phoneLabel.textContent = "Teléfono (opcional)";
    const phone = document.createElement("input");
    phone.type = "tel"; phone.maxLength = 30; phone.value = relative.telefono || "";
    phone.addEventListener("input", () => { relatives[index].telefono = phone.value; });
    phoneLabel.append(phone);

    const remove = document.createElement("button");
    remove.type = "button"; remove.className = "remove-relative"; remove.textContent = "Eliminar";
    remove.addEventListener("click", () => { relatives.splice(index, 1); renderRelatives(); });
    row.append(nameLabel, relationLabel, phoneLabel, remove);
    return row;
  }

  function renderRelatives() {
    relativesList.replaceChildren(...relatives.map(relativeRow));
    relativesEmpty.hidden = relatives.length > 0;
    addRelativeButton.disabled = relatives.length >= 3;
  }

  document.querySelectorAll("[data-edit]").forEach(button => {
    button.addEventListener("click", () => setEditing(button.dataset.edit, true));
  });
  document.querySelectorAll("[data-cancel]").forEach(button => {
    button.addEventListener("click", () => {
      fillProfile(profile);
      setEditing(button.dataset.cancel, false);
    });
  });
  ["contactForm", "addressForm"].forEach(formId => {
    document.getElementById(formId).addEventListener("submit", event => {
      event.preventDefault();
      saveEditableForm(formId);
    });
  });

  document.getElementById("financeForm").addEventListener("submit", async event => {
    event.preventDefault();
    const payload = {};
    new FormData(event.currentTarget).forEach((_, key) => { payload[key] = true; });
    ["aporteFamiliares", "planesSociales", "trabajo", "beca", "otraFuente"].forEach(key => {
      payload[key] = Boolean(payload[key]);
    });
    try {
      await AcadionApi.request("/api/me/financiamiento", { method: "PUT", body: JSON.stringify(payload) });
      Acadion.mostrarMensaje("La información de financiamiento se guardó correctamente.");
    } catch (error) {
      showStatus(error.message || "No fue posible guardar el financiamiento.", true);
    }
  });

  photoInput.addEventListener("change", async () => {
    const file = photoInput.files[0];
    if (!file) return;
    if (!["image/png", "image/jpeg", "image/webp"].includes(file.type) || file.size > 5 * 1024 * 1024) {
      showStatus("Seleccioná una imagen JPEG, PNG o WebP de hasta 5 MB.", true);
      photoInput.value = "";
      return;
    }
    const data = new FormData();
    data.append("foto", file);
    showStatus("Guardando foto...");
    try {
      await AcadionApi.request("/api/me/foto-perfil", { method: "POST", body: data });
      await loadProfile();
      showStatus("La foto de perfil se actualizó correctamente.");
    } catch (error) { showStatus(error.message || "No fue posible actualizar la foto.", true); }
    finally { photoInput.value = ""; }
  });

  addRelativeButton.addEventListener("click", () => {
    if (relatives.length >= 3) return;
    relatives.push({ nombreApellido: "", relacion: "", telefono: "" });
    renderRelatives();
    relativesList.lastElementChild?.querySelector("input")?.focus();
  });

  document.getElementById("relativesForm").addEventListener("submit", async event => {
    event.preventDefault();
    if (!event.currentTarget.reportValidity()) return;
    try {
      await AcadionApi.request("/api/me/allegados", {
        method: "PUT",
        body: JSON.stringify({ allegados: relatives })
      });
      Acadion.mostrarMensaje("Tus allegados se guardaron correctamente.");
      await loadProfile();
    } catch (error) {
      showStatus(error.message || "No fue posible guardar los allegados.", true);
    }
  });

  passwordForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (passwordForm.nueva.value !== passwordForm.confirmacion.value) {
      showStatus("Las contraseñas nuevas no coinciden.", true);
      return;
    }
    const button = passwordForm.querySelector('button[type="submit"]');
    button.disabled = true;
    try {
      await AcadionApi.request("/api/auth/cambiar-password", {
        method: "POST",
        body: JSON.stringify({
          passwordActual: passwordForm.actual.value,
          passwordNueva: passwordForm.nueva.value
        })
      });
      passwordForm.reset();
      passwordPanel.hidden = true;
      showStatus("La contraseña se actualizó correctamente.");
    } catch (error) {
      showStatus(error.message || "No fue posible actualizar la contraseña.", true);
    } finally {
      button.disabled = false;
    }
  });
  document.getElementById("toggleStudentPassword").addEventListener("click", () => {
    passwordPanel.hidden = false;
    passwordPanel.scrollIntoView({ behavior: "smooth", block: "start" });
    passwordForm.actual.focus();
  });
  document.getElementById("closeStudentPassword").addEventListener("click", () => {
    passwordForm.reset();
    passwordPanel.hidden = true;
  });

  loadProfile();
});
