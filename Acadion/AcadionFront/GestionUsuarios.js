document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const state = { users: [], careers: [], selected: null };
  const body = document.getElementById("usersBody");
  const roleFilter = document.getElementById("roleFilter");
  const search = document.getElementById("userSearch");
  const panel = document.getElementById("editPanel");
  const form = document.getElementById("editUserForm");
  const message = document.getElementById("editMessage");

  const dateValue = value => String(value || "").slice(0, 10);
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
    const users = visibleUsers();
    if (!users.length) {
      body.innerHTML = '<tr><td colspan="6" class="empty-state">No hay usuarios para mostrar.</td></tr>';
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
      const actions = row.insertCell();
      const controls = document.createElement("div"); controls.className = "user-actions";
      const edit = document.createElement("button");
      edit.className = "secondary-button"; edit.type = "button"; edit.textContent = "Editar";
      edit.addEventListener("click", () => beginEdit(user)); controls.append(edit);
      if (user.rolId === 1 || user.rolId === 2) {
        const absences = document.createElement("a");
        absences.className = "primary-button";
        absences.href = user.rolId === 1
          ? `InasistenciasEstudiante.html?usuarioId=${user.id}`
          : `AsistenciaProfesores.html?docenteId=${user.id}`;
        absences.textContent = "Inasistencias";
        controls.append(absences);
      }
      const remove = document.createElement("button");
      remove.className = "danger-button";
      remove.type = "button";
      remove.textContent = "Eliminar";
      remove.addEventListener("click", () => deleteUser(user, remove));
      controls.append(remove);
      actions.append(controls); body.append(row);
    });
  }

  async function deleteUser(user, button) {
    const displayName = Acadion.formatearNombre(`${user.apellido}, ${user.nombre}`);
    if (!window.confirm(`¿Eliminar definitivamente a ${displayName}? Esta acción no se puede deshacer.`)) return;
    button.disabled = true;
    try {
      const result = await AcadionApi.request(`/api/gestion/usuarios/${user.id}`, { method: "DELETE" });
      state.users = state.users.filter(item => item.id !== user.id);
      if (state.selected?.id === user.id) closeEdit();
      render();
      window.alert(result.mensaje || "El usuario fue eliminado correctamente.");
    } catch (error) {
      window.alert(error.message);
      button.disabled = false;
    }
  }

  function beginEdit(user) {
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
      cell.colSpan = 6; cell.className = "empty-state"; cell.textContent = error.message;
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

  roleFilter.addEventListener("change", render);
  search.addEventListener("input", render);
  document.getElementById("closeEdit").addEventListener("click", closeEdit);
  document.getElementById("cancelEdit").addEventListener("click", closeEdit);
  load();
});
