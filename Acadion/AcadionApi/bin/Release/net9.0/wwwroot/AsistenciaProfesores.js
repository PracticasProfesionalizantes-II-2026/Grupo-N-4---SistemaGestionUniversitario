document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const state = { teachers: [], selectedTeacherId: null };
  const form = document.getElementById("attendanceForm");
  const teachersList = document.getElementById("teachersList");
  const search = document.getElementById("teacherSearch");
  const subjectSelect = document.getElementById("subjectSelect");
  const attendancePanel = document.getElementById("attendancePanel");
  const body = document.getElementById("attendanceBody");
  const message = document.getElementById("attendanceMessage");
  const saveButton = document.getElementById("saveAttendance");
  const currentYear = new Date().getFullYear();
  const dateInput = document.getElementById("attendanceDate");
  dateInput.min = `${currentYear}-01-01`;
  dateInput.max = `${currentYear}-12-31`;
  dateInput.valueAsDate = new Date();

  function initials(teacher) {
    return `${teacher.nombre?.[0] || ""}${teacher.apellido?.[0] || ""}`.toUpperCase() || "PR";
  }

  function formatDate(value) {
    const date = new Date(`${String(value).slice(0, 10)}T00:00:00`);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString("es-AR");
  }

  function showMessage(value, success = false) {
    message.textContent = value;
    message.className = `status-message ${success ? "success" : "error"}`;
  }

  function createTeacherCard(teacher) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `teacher-card${teacher.id === state.selectedTeacherId ? " active" : ""}`;
    button.setAttribute("aria-pressed", String(teacher.id === state.selectedTeacherId));
    const avatar = document.createElement("span");
    avatar.className = "teacher-avatar";
    avatar.textContent = initials(teacher);
    const copy = document.createElement("span");
    copy.className = "teacher-card-copy";
    const name = document.createElement("strong");
    name.textContent = Acadion.formatearNombre(`${teacher.apellido}, ${teacher.nombre}`);
    const details = document.createElement("small");
    details.textContent = `DNI ${teacher.dni} · ${teacher.materias.length} ${teacher.materias.length === 1 ? "materia" : "materias"}`;
    copy.append(name, details);
    button.append(avatar, copy);
    button.addEventListener("click", () => selectTeacher(teacher.id));
    return button;
  }

  function renderTeachers() {
    const term = search.value.trim().toLocaleLowerCase("es");
    const visible = state.teachers.filter(teacher =>
      `${teacher.nombre} ${teacher.apellido} ${teacher.dni}`.toLocaleLowerCase("es").includes(term));
    teachersList.replaceChildren();
    document.getElementById("teachersCount").textContent = `${visible.length} ${visible.length === 1 ? "docente" : "docentes"}`;
    if (!visible.length) {
      const empty = document.createElement("p");
      empty.className = "empty-state";
      empty.textContent = "No se encontraron profesores con ese nombre o DNI.";
      teachersList.append(empty);
      return;
    }
    visible.forEach(teacher => teachersList.append(createTeacherCard(teacher)));
  }

  function selectTeacher(id) {
    state.selectedTeacherId = id;
    const teacher = state.teachers.find(item => item.id === id);
    if (!teacher) return;
    renderTeachers();
    attendancePanel.hidden = false;
    document.getElementById("selectedTeacherInitials").textContent = initials(teacher);
    document.getElementById("selectedTeacherName").textContent = Acadion.formatearNombre(`${teacher.nombre} ${teacher.apellido}`);
    document.getElementById("selectedTeacherDetails").textContent = `DNI ${teacher.dni}${teacher.especialidad ? ` · ${teacher.especialidad}` : ""}`;
    subjectSelect.replaceChildren(new Option("Seleccionar materia", ""));
    teacher.materias.forEach(subject => subjectSelect.append(new Option(
      `${subject.materia} · ${subject.carrera} · ${subject.numeroAnio}.º año`, subject.materiaId)));
    subjectSelect.disabled = teacher.materias.length === 0;
    saveButton.disabled = teacher.materias.length === 0;
    document.getElementById("subjectHelp").textContent = teacher.materias.length
      ? `Se muestran las materias asignadas al docente durante el ciclo ${currentYear}.`
      : `Este profesor no tiene materias asignadas durante el ciclo ${currentYear}.`;
    message.textContent = "";
    attendancePanel.scrollIntoView({ behavior: "smooth", block: "nearest" });
  }

  async function loadTeachers() {
    state.teachers = await AcadionApi.request(`/api/asistencia-personal/docentes?cicloLectivo=${currentYear}`);
    renderTeachers();
    const requestedTeacherId = Number(new URLSearchParams(window.location.search).get("docenteId"));
    if (requestedTeacherId && state.teachers.some(teacher => teacher.id === requestedTeacherId)) {
      selectTeacher(requestedTeacherId);
    }
  }

  async function loadAttendance() {
    try {
      const records = await AcadionApi.request("/api/asistencia-personal/");
      body.replaceChildren();
      if (!records.length) {
        const row = body.insertRow();
        const cell = row.insertCell();
        cell.colSpan = 8;
        cell.className = "empty-state";
        cell.textContent = "Todavía no hay asistencias registradas.";
        return;
      }
      records.forEach(record => {
        const row = body.insertRow();
        [formatDate(record.fecha), Acadion.formatearNombre(record.nombreCompleto), record.materia, record.carrera || "—"].forEach(value => {
          const cell = row.insertCell();
          cell.textContent = value;
        });
        const statusCell = row.insertCell();
        const status = document.createElement("span");
        status.className = `attendance-state ${record.estado?.toLowerCase() === "presente" ? "present" : "absent"}`;
        status.textContent = record.estado;
        statusCell.append(status);
        const justified = row.insertCell();
        const isAbsent = record.estado?.toLowerCase() === "ausente";
        justified.textContent = isAbsent ? (record.justificada ? "Sí" : "No") : "—";
        const observations = row.insertCell();
        observations.textContent = record.observaciones || "—";
        const actions = row.insertCell();
        if (isAbsent) {
          const toggle = document.createElement("button");
          toggle.type = "button";
          toggle.className = "secondary-button compact-button";
          toggle.textContent = record.justificada ? "Quitar justificación" : "Justificar";
          toggle.addEventListener("click", async () => {
            toggle.disabled = true;
            try {
              const result = await AcadionApi.request(`/api/asistencia-personal/${record.id}/justificacion`, {
                method: "PUT",
                body: JSON.stringify({ justificada: !record.justificada })
              });
              showMessage(result.mensaje, true);
              await loadAttendance();
            } catch (error) {
              showMessage(error.message);
              toggle.disabled = false;
            }
          });
          actions.append(toggle);
        }
      });
    } catch (error) {
      body.replaceChildren();
      const row = body.insertRow();
      const cell = row.insertCell();
      cell.colSpan = 8;
      cell.className = "empty-state";
      cell.textContent = error.message;
    }
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!state.selectedTeacherId) return;
    saveButton.disabled = true;
    try {
      await AcadionApi.request("/api/asistencia-personal/", {
        method: "POST",
        body: JSON.stringify({
          usuarioId: state.selectedTeacherId,
          materiaId: Number(subjectSelect.value),
          fecha: form.elements.fecha.value,
          estado: form.elements.estado.value,
          observaciones: form.elements.observaciones.value.trim() || null
        })
      });
      showMessage("Asistencia guardada correctamente.", true);
      form.elements.observaciones.value = "";
      await loadAttendance();
    } catch (error) {
      showMessage(error.message);
    } finally {
      saveButton.disabled = false;
    }
  });

  search.addEventListener("input", renderTeachers);
  document.getElementById("refreshAttendance").addEventListener("click", loadAttendance);
  Promise.all([loadTeachers(), loadAttendance()]).catch(error => {
    teachersList.innerHTML = '<p class="empty-state">No fue posible cargar el listado de profesores.</p>';
    showMessage(error.message);
  });
});
