document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const state = { users: [], careers: [], selected: null, informationUserId: null, page: 1, pageSize: 10 };
  const body = document.getElementById("usersBody");
  const roleFilter = document.getElementById("roleFilter");
  const search = document.getElementById("userSearch");
  const panel = document.getElementById("editPanel");
  const informationPanel = document.getElementById("informationPanel");
  const informationContent = document.getElementById("informationContent");
  const form = document.getElementById("editUserForm");
  const message = document.getElementById("editMessage");

  const dateValue = value => String(value || "").slice(0, 10);
  const displayDate = value => value
    ? new Intl.DateTimeFormat("es-AR").format(new Date(value))
    : "Sin informar";
  const paymentStatus = value => ({
    AlDia: "Al día", Pendiente: "Pendiente", Exentado: "Exentado",
    Vencida: "Vencida", Impaga: "Impaga", EnRevision: "En revisión"
  })[value] || value || "Sin registro";
  const avatarFor = user => user.fotoPerfilUrl
    ? (user.fotoPerfilUrl.startsWith("http") ? user.fotoPerfilUrl : `${AcadionApi.baseUrl}${user.fotoPerfilUrl}`)
    : `https://ui-avatars.com/api/?name=${encodeURIComponent(`${user.nombre} ${user.apellido}`)}&background=ff7a00&color=fff&bold=true`;

  function showMessage(text, success = false) {
    message.textContent = text;
    message.className = `status-message ${success ? "success" : "error"}`;
  }

  function visibleUsers() {
    const role = Number(roleFilter.value) || null;
    const term = search.value.trim().toLowerCase();
    return state.users.filter(user => {
      const matchesRole = !role || user.rolId === role;
      const content = `${user.nombre} ${user.apellido} ${user.nombreUsuario} ${user.dni} ${user.legajo} ${user.carrera} ${user.especialidad}`.toLowerCase();
      return matchesRole && (!term || content.includes(term));
    });
  }

  function render() {
    body.replaceChildren();
    const filteredUsers = visibleUsers();
    const pages = Math.max(1, Math.ceil(filteredUsers.length / state.pageSize));
    state.page = Math.min(state.page, pages);
    const users = filteredUsers.slice((state.page - 1) * state.pageSize, state.page * state.pageSize);
    document.getElementById("usersPageLabel").textContent = `Página ${state.page} de ${pages} · ${filteredUsers.length} usuarios`;
    document.getElementById("previousUsers").disabled = state.page <= 1;
    document.getElementById("nextUsers").disabled = state.page >= pages;
    if (!users.length) {
      body.innerHTML = '<tr><td colspan="9" class="empty-state">No hay usuarios para mostrar.</td></tr>';
      return;
    }
    users.forEach(user => {
      const row = document.createElement("tr");
      const identity = row.insertCell();
      const wrapper = document.createElement("div"); wrapper.className = "user-cell";
      const image = document.createElement("img"); image.className = "user-avatar"; image.src = avatarFor(user); image.alt = "";
      const text = document.createElement("div");
      const name = document.createElement("strong"); name.textContent = Acadion.formatearNombre(`${user.apellido}, ${user.nombre}`);
      const username = document.createElement("small"); username.textContent = user.nombreUsuario;
      text.append(name, username); wrapper.append(image, text); identity.append(wrapper);
      [user.rol, user.rolId === 1 ? `${user.dni} · ${user.legajo || "Sin legajo"}` : String(user.dni), user.rolId === 1 ? (user.carrera || "Sin carrera") : user.rolId === 2 ? (user.especialidad || "Sin especialidad") : "—"].forEach(value => {
        const cell = row.insertCell(); cell.textContent = value;
      });
      const status = row.insertCell(); const badge = document.createElement("span");
      badge.className = `badge ${user.estado === "Activo" ? "active" : "inactive"}`; badge.textContent = user.estado; status.append(badge);
      const editCell = row.insertCell();
      const edit = document.createElement("button");
      edit.className = "secondary-button"; edit.type = "button"; edit.textContent = "Editar";
      edit.addEventListener("click", () => beginEdit(user)); editCell.append(edit);

      const informationCell = row.insertCell();
      if (user.rolId === 1 || user.rolId === 2) {
        const information = document.createElement("button");
        information.className = "secondary-button";
        information.type = "button";
        information.textContent = "Información";
        information.addEventListener("click", () => openInformation(user, information));
        informationCell.append(information);
      } else informationCell.textContent = "—";

      const absencesCell = row.insertCell();
      if (user.rolId === 1 || user.rolId === 2) {
        const absences = document.createElement("a");
        absences.className = "secondary-button";
        absences.href = user.rolId === 1
          ? `InasistenciasEstudiante.html?usuarioId=${user.id}`
          : `AsistenciaProfesores.html?docenteId=${user.id}`;
        absences.textContent = "Inasistencias";
        absencesCell.append(absences);
      } else absencesCell.textContent = "—";

      const equivalencesCell = row.insertCell();
      if (user.rolId === 1) {
        const equivalences = document.createElement("a");
        equivalences.className = "secondary-button";
        equivalences.href = `EquivalenciasEstudiante.html?usuarioId=${user.id}`;
        equivalences.textContent = "Equivalencias";
        equivalencesCell.append(equivalences);
      } else equivalencesCell.textContent = "—";
      body.append(row);
    });
  }

  function addDefinition(list, label, value) {
    const item = document.createElement("div");
    const term = document.createElement("dt"); term.textContent = label;
    const description = document.createElement("dd"); description.textContent = value || "Sin informar";
    item.append(term, description); list.append(item);
  }

  function informationCard(title, values, wide = false) {
    const card = document.createElement("article");
    card.className = `student-information-card${wide ? " student-information-wide" : ""}`;
    const heading = document.createElement("h3"); heading.textContent = title;
    const list = document.createElement("dl");
    values.forEach(([label, value]) => addDefinition(list, label, value));
    card.append(heading, list);
    return card;
  }

  function closeInformation() {
    state.informationUserId = null;
    informationPanel.hidden = true;
    informationContent.replaceChildren();
  }

  async function openInformation(user, button) {
    button.disabled = true;
    try {
      const data = await AcadionApi.request(`/api/gestion/usuarios/${user.id}/detalle`);
      const person = data.usuario;
      const isStudent = person.rolId === 1;
      state.informationUserId = person.id;
      if (!panel.hidden) closeEdit();
      informationContent.replaceChildren();
      document.getElementById("informationTitle").textContent = Acadion.formatearNombre(`${person.apellido}, ${person.nombre}`);

      const summary = document.createElement("article"); summary.className = "student-information-card student-information-summary";
      const photo = document.createElement("img"); photo.className = "student-information-photo"; photo.src = avatarFor(person); photo.alt = `Foto de ${person.nombre} ${person.apellido}`;
      const name = document.createElement("strong"); name.textContent = Acadion.formatearNombre(`${person.nombre} ${person.apellido}`);
      const career = document.createElement("span"); career.textContent = isStudent ? (person.carrera || "Sin carrera") : (person.especialidad || "Sin especialidad");
      const file = document.createElement("span"); file.textContent = isStudent ? (person.legajo ? `Legajo ${person.legajo}` : "Sin legajo") : person.rol;
      summary.append(photo, name, career, file);

      const personal = informationCard("Datos personales", [
        ["DNI", String(person.dni)], ["Fecha de nacimiento", displayDate(person.fechaNacimiento)],
        ["Dirección", person.direccion], ["Localidad", `${person.localidad || "Sin informar"}${person.codigoPostal ? ` · CP ${person.codigoPostal}` : ""}`]
      ]);
      const contact = informationCard("Contacto y cuenta", [
        ["Usuario", person.nombreUsuario], ["Correo personal", person.emailPersonal],
        ["Correo institucional", person.emailInstitucional], ["Teléfono", person.telefonoContacto],
        ["Estado", person.estado]
      ]);
      const academic = isStudent ? informationCard("Información académica y pagos", [
        ["Carrera", person.carrera], ["Plan de estudios", person.planEstudio],
        ["Matrícula inicial", `${paymentStatus(data.matriculaInicial?.estado)}${data.matriculaInicial?.periodoLectivo ? ` · ${data.matriculaInicial.periodoLectivo}` : ""}`],
        ["Cuota actual", `${paymentStatus(data.cuotaActual?.estado)}${data.cuotaActual?.periodo ? ` · ${data.cuotaActual.periodo}` : ""}`],
        ["Medio de pago registrado", data.cuotaActual?.metodoPago || data.matriculaInicial?.metodoPago || "Sin informar"]
      ], true) : informationCard("Información docente", [
        ["Especialidad", person.especialidad], ["Título académico", person.tituloAcademico],
        ["Alta de la cuenta", displayDate(person.fechaCreacion)]
      ], true);

      informationContent.append(summary, personal, contact, academic);
      if (isStudent) {
        const financing = document.createElement("article"); financing.className = "student-information-card student-information-wide";
        const financingTitle = document.createElement("h3"); financingTitle.textContent = "Cómo abona y financia sus estudios"; financing.append(financingTitle);
      const financingOptions = [
        ["aporteFamiliares", "Aporte de familiares"], ["planesSociales", "Planes sociales"],
        ["trabajo", "Trabajo"], ["beca", "Beca"], ["otraFuente", "Otra fuente"]
      ].filter(([key]) => data.financiamiento?.[key]).map(([, label]) => label);
      if (financingOptions.length) {
        const tags = document.createElement("ul"); tags.className = "information-tags";
        financingOptions.forEach(label => { const tag = document.createElement("li"); tag.textContent = label; tags.append(tag); });
        financing.append(tags);
      } else {
        const empty = document.createElement("p"); empty.className = "information-empty"; empty.textContent = "El estudiante todavía no informó cómo financia sus estudios."; financing.append(empty);
      }

        informationContent.append(financing);

        const relatives = document.createElement("article"); relatives.className = "student-information-card student-information-wide";
        const relativesTitle = document.createElement("h3"); relativesTitle.textContent = "Allegados"; relatives.append(relativesTitle);
        if (data.allegados?.length) {
          const list = document.createElement("dl");
          data.allegados.forEach(item => addDefinition(list, item.relacion, `${item.nombreApellido}${item.telefono ? ` · ${item.telefono}` : ""}`));
          relatives.append(list);
        } else {
          const empty = document.createElement("p"); empty.className = "information-empty"; empty.textContent = "El estudiante todavía no registró allegados."; relatives.append(empty);
        }
        informationContent.append(relatives);
      }
      informationPanel.hidden = false;
      informationPanel.scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (error) {
      window.alert(error.message);
    } finally {
      button.disabled = false;
    }
  }

  function beginEdit(user) {
    if (!informationPanel.hidden) closeInformation();
    state.selected = user;
    form.nombre.value = user.nombre;
    form.apellido.value = user.apellido;
    form.dni.value = user.dni;
    form.fechaNacimiento.value = dateValue(user.fechaNacimiento);
    form.direccion.value = user.direccion;
    form.localidad.value = user.localidad;
    form.codigoPostal.value = user.codigoPostal;
    form.emailPersonal.value = user.emailPersonal;
    form.emailInstitucional.value = user.emailInstitucional;
    form.telefonoContacto.value = user.telefonoContacto;
    form.nombreUsuario.value = user.nombreUsuario;
    form.estado.value = user.estado;
    form.especialidad.value = user.especialidad || "";
    form.tituloAcademico.value = user.tituloAcademico || "";
    document.getElementById("userRole").value = user.rol;
    document.getElementById("editCareer").value = user.carreraId || "";
    document.getElementById("editLegajo").value = user.legajo || "";
    document.getElementById("studentFields").hidden = user.rolId !== 1;
    document.getElementById("teacherFields").hidden = user.rolId !== 2;
    document.getElementById("editCareer").required = user.rolId === 1;
    document.getElementById("editTitle").textContent = Acadion.formatearNombre(`${user.nombre} ${user.apellido}`);
    showMessage(""); panel.hidden = false;
    panel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function closeEdit() {
    state.selected = null;
    panel.hidden = true;
    form.reset();
    document.getElementById("studentFields").hidden = true;
    document.getElementById("teacherFields").hidden = true;
    showMessage("");
  }

  async function load() {
    try {
      [state.users, state.careers] = await Promise.all([
        AcadionApi.request("/api/gestion/usuarios/"), AcadionApi.request("/carreras/")
      ]);
      const career = document.getElementById("editCareer");
      career.replaceChildren();
      const empty = document.createElement("option"); empty.value = ""; empty.textContent = "Seleccionar carrera"; career.append(empty);
      state.careers.forEach(item => {
        const option = document.createElement("option"); option.value = item.idCarrera;
        option.textContent = `${item.nombre} · ${item.planEstudios}`; career.append(option);
      });
      render();
    } catch (error) {
      body.replaceChildren(); const row = body.insertRow(); const cell = row.insertCell();
      cell.colSpan = 9; cell.className = "empty-state"; cell.textContent = error.message;
    }
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!state.selected) return;
    const payload = {
      nombre: form.nombre.value.trim(), apellido: form.apellido.value.trim(), dni: Number(form.dni.value),
      fechaNacimiento: form.fechaNacimiento.value, direccion: form.direccion.value.trim(),
      localidad: form.localidad.value.trim(), codigoPostal: Number(form.codigoPostal.value),
      emailPersonal: form.emailPersonal.value.trim(), emailInstitucional: form.emailInstitucional.value.trim(),
      telefonoContacto: form.telefonoContacto.value.trim(), nombreUsuario: form.nombreUsuario.value.trim(),
      estado: form.estado.value, carreraId: state.selected.rolId === 1 ? Number(form.carreraId.value) : null,
      especialidad: state.selected.rolId === 2 ? form.especialidad.value.trim() : "",
      tituloAcademico: state.selected.rolId === 2 ? form.tituloAcademico.value.trim() : ""
    };
    try {
      const updated = await AcadionApi.request(`/api/gestion/usuarios/${state.selected.id}`, { method: "PUT", body: JSON.stringify(payload) });
      const index = state.users.findIndex(user => user.id === updated.id);
      if (index >= 0) state.users[index] = updated;
      state.selected = updated; render(); showMessage("Los datos se guardaron correctamente.", true);
    } catch (error) { showMessage(error.message); }
  });

  roleFilter.addEventListener("change", () => { state.page = 1; render(); });
  search.addEventListener("input", () => { state.page = 1; render(); });
  document.getElementById("previousUsers").addEventListener("click", () => { if (state.page > 1) { state.page--; render(); } });
  document.getElementById("nextUsers").addEventListener("click", () => { state.page++; render(); });
  document.getElementById("closeEdit").addEventListener("click", closeEdit);
  document.getElementById("cancelEdit").addEventListener("click", closeEdit);
  document.getElementById("closeInformation").addEventListener("click", closeInformation);
  load();
});
