document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const state = { careers: [], subjects: [], schedules: [], examPeriod: null, teachers: [], commissions: [], editingId: null };
  const form = document.getElementById("subjectForm");
  const editPanel = document.getElementById("subjectEditPanel");
  const scheduleList = document.getElementById("scheduleList");
  const careerSelect = document.getElementById("subjectCareer");
  const yearSelect = document.getElementById("subjectYear");
  const prerequisites = document.getElementById("subjectPrerequisites");
  const typeSelect = document.getElementById("subjectType");
  const periodSelect = document.getElementById("subjectPeriod");
  const filterCareer = document.getElementById("filterCareer");
  const filterYear = document.getElementById("filterYear");
  const assignmentForm = document.getElementById("teacherAssignmentForm");
  const assignmentTeacher = document.getElementById("assignmentTeacher");
  const assignmentSubject = document.getElementById("assignmentSubject");
  const assignmentCycle = document.getElementById("assignmentCycle");
  const examPeriodForm = document.getElementById("examPeriodForm");
  const examPeriodCycle = document.getElementById("examPeriodCycle");
  const commissionForm = document.getElementById("commissionForm");
  const commissionSubject = document.getElementById("commissionSubject");
  const commissionTeacher = document.getElementById("commissionTeacher");
  const commissionsList = document.getElementById("commissionsList");
  assignmentCycle.value = new Date().getFullYear();
  examPeriodCycle.value = new Date().getFullYear();

  function setMessage(text, success = false) {
    const element = document.querySelector('[data-message="subject"]');
    element.textContent = text;
    element.className = `status-message ${success ? "success" : "error"}`;
  }

  function option(value, text) {
    const item = document.createElement("option"); item.value = value; item.textContent = text; return item;
  }

  function selectedCareer(select = careerSelect) {
    return state.careers.find(c => c.idCarrera === Number(select.value));
  }

  function renderSubjectPeriodOptions(selectedValue = null) {
    const type = typeSelect.value;
    periodSelect.replaceChildren();
    if (type === "Anual") {
      periodSelect.append(option("", "No corresponde"));
      periodSelect.disabled = true;
      periodSelect.required = false;
      return;
    }
    const total = type === "Cuatrimestral" ? 2 : 5;
    const label = type === "Cuatrimestral" ? "cuatrimestre" : "bimestre";
    for (let number = 1; number <= total; number += 1) {
      periodSelect.append(option(number, `${number}.º ${label}`));
    }
    periodSelect.disabled = false;
    periodSelect.required = true;
    if (selectedValue) periodSelect.value = String(selectedValue);
  }

  function subjectPeriodLabel(subject) {
    if (subject.tipoCursada === "Cuatrimestral") return `${subject.numeroPeriodo}.º cuatrimestre`;
    if (subject.tipoCursada === "Bimestral") return `${subject.numeroPeriodo}.º bimestre`;
    return "Anual";
  }

  function fillYears(select, career, allLabel) {
    const previous = select.value;
    select.replaceChildren(option("", allLabel));
    (career?.anios || []).forEach(year => select.append(option(year.idAnio, year.nombreAnio)));
    select.disabled = !career;
    if ([...select.options].some(item => item.value === previous)) select.value = previous;
  }

  function renderCareerOptions() {
    [careerSelect, filterCareer].forEach((select, index) => {
      const previous = select.value;
      select.replaceChildren(option("", index === 1 ? "Todas las carreras" : "Seleccionar carrera"));
      state.careers.forEach(c => select.append(option(c.idCarrera, `${c.nombre} · ${c.planEstudios}`)));
      select.value = previous;
    });
  }

  function renderAssignmentOptions() {
    assignmentTeacher.replaceChildren(option("", "Seleccionar profesor"));
    state.teachers.forEach(teacher => assignmentTeacher.append(option(
      teacher.id, `${teacher.apellido}, ${teacher.nombre} · ${teacher.especialidad || "Sin especialidad"}`)));
    assignmentSubject.replaceChildren(option("", "Seleccionar materia"));
    state.subjects.forEach(subject => assignmentSubject.append(option(
      subject.idMateria, `${subject.carrera} · ${subject.numeroAnio}.º año · ${subject.nombre}`)));
    commissionSubject.replaceChildren(option("", "Seleccionar materia"));
    state.subjects.filter(subject => subject.estado === "Activa").forEach(subject =>
      commissionSubject.append(option(subject.idMateria,
        `${subject.carrera} · ${subject.numeroAnio}.º año · ${subject.nombre}`)));
    commissionTeacher.replaceChildren(option("", "A confirmar"));
    state.teachers.forEach(teacher => commissionTeacher.append(option(
      teacher.id, `${teacher.apellido}, ${teacher.nombre}`)));
  }

  function commissionScheduleLabel(commission) {
    return commission.horarios?.length
      ? commission.horarios.map(h => `${h.diaSemana} ${String(h.horaInicio).slice(0, 5)}–${String(h.horaFin).slice(0, 5)}`).join(" · ")
      : "Sin horario";
  }

  async function expandCommission(commission, content) {
    content.replaceChildren();
    const loading = document.createElement("p");
    loading.className = "empty-state";
    loading.textContent = "Cargando estudiantes y lista de espera...";
    content.append(loading);
    try {
      const [students, waiting] = await Promise.all([
        AcadionApi.request(`/api/comisiones/${commission.id}/estudiantes`),
        AcadionApi.request(`/api/comisiones/${commission.id}/lista-espera`)
      ]);
      content.replaceChildren();
      const enrolledTitle = document.createElement("h4");
      enrolledTitle.textContent = `Estudiantes inscriptos (${students.length})`;
      content.append(enrolledTitle);
      const enrolledList = document.createElement("ul");
      enrolledList.className = "commission-people";
      if (!students.length) enrolledList.append(Object.assign(document.createElement("li"), { textContent: "No hay estudiantes inscriptos." }));
      students.forEach(student => enrolledList.append(Object.assign(document.createElement("li"), {
        textContent: `${student.apellido}, ${student.nombre} · ${student.legajo || "Sin legajo"}`
      })));
      content.append(enrolledList);

      const waitingTitle = document.createElement("h4");
      waitingTitle.textContent = `Lista de espera (${waiting.filter(item => item.estado === "EnEspera").length})`;
      content.append(waitingTitle);
      const waitingList = document.createElement("div");
      waitingList.className = "waiting-list";
      const pending = waiting.filter(item => item.estado === "EnEspera");
      if (!pending.length) waitingList.append(Object.assign(document.createElement("p"), { textContent: "No hay solicitudes pendientes." }));
      pending.forEach(item => {
        const row = document.createElement("div");
        const name = document.createElement("span");
        name.textContent = `${item.apellido}, ${item.nombre} · ${item.legajo || "Sin legajo"}`;
        const confirmButton = document.createElement("button");
        confirmButton.type = "button";
        confirmButton.className = "primary-button compact-button";
        confirmButton.textContent = "Confirmar vacante";
        confirmButton.addEventListener("click", async () => {
          if (!confirm(`¿Confirmar una vacante para ${item.apellido}, ${item.nombre}?`)) return;
          confirmButton.disabled = true;
          try {
            await AcadionApi.request(`/api/comisiones/${commission.id}/lista-espera/${item.id}/confirmar`, { method: "POST" });
            await loadCommissions();
          } catch (error) {
            alert(error.message);
            confirmButton.disabled = false;
          }
        });
        row.append(name, confirmButton);
        waitingList.append(row);
      });
      content.append(waitingList);
    } catch (error) {
      content.replaceChildren(Object.assign(document.createElement("p"), {
        className: "empty-state error", textContent: error.message
      }));
    }
  }

  function renderCommissions() {
    commissionsList.replaceChildren();
    if (!state.commissions.length) {
      commissionsList.append(Object.assign(document.createElement("p"), {
        className: "empty-state", textContent: "No hay comisiones registradas para el ciclo actual."
      }));
      return;
    }
    state.commissions.forEach(commission => {
      const details = document.createElement("details");
      details.className = "commission-card";
      const summary = document.createElement("summary");
      const identity = document.createElement("span");
      const title = document.createElement("strong");
      title.textContent = `${commission.materia} · ${commission.nombre}`;
      const subtitle = document.createElement("small");
      subtitle.textContent = `${commission.turno} · ${commission.docente} · ${commissionScheduleLabel(commission)}`;
      identity.append(title, subtitle);
      const capacity = document.createElement("span");
      capacity.className = "badge active";
      capacity.textContent = `${commission.inscriptos}/${commission.cupo} · ${commission.enEspera} en espera`;
      summary.append(identity, capacity);
      const content = document.createElement("div");
      content.className = "commission-detail";
      details.addEventListener("toggle", () => {
        if (details.open && !content.dataset.loaded) {
          content.dataset.loaded = "true";
          expandCommission(commission, content);
        }
      });
      details.append(summary, content);
      commissionsList.append(details);
    });
  }

  async function loadCommissions() {
    state.commissions = await AcadionApi.request(`/api/comisiones/?cicloLectivo=${new Date().getFullYear()}`);
    renderCommissions();
  }

  function showAssignmentMessage(text, success = false) {
    const element = document.getElementById("assignmentMessage");
    element.textContent = text;
    element.className = `status-message ${success ? "success" : "error"}`;
  }

  function showExamPeriodMessage(text, success = false) {
    const element = document.getElementById("examPeriodMessage");
    element.textContent = text;
    element.className = `status-message ${success ? "success" : "error"}`;
  }

  function renderExamPeriod() {
    const status = document.getElementById("examPeriodStatus");
    if (!state.examPeriod?.configurado) {
      examPeriodForm.fechaInicio.value = "";
      examPeriodForm.fechaFin.value = "";
      status.textContent = "Sin configurar";
      status.className = "badge inactive";
      return;
    }
    examPeriodForm.fechaInicio.value = String(state.examPeriod.fechaInicio).slice(0, 10);
    examPeriodForm.fechaFin.value = String(state.examPeriod.fechaFin).slice(0, 10);
    status.textContent = state.examPeriod.abierta ? "Inscripción abierta" : "Inscripción cerrada";
    status.className = `badge ${state.examPeriod.abierta ? "active" : "inactive"}`;
  }

  async function loadExamPeriod() {
    state.examPeriod = await AcadionApi.request(`/api/gestion-academica/periodo-inscripcion-examenes?cicloLectivo=${Number(examPeriodCycle.value)}`);
    renderExamPeriod();
  }

  function renderPrerequisites(selectedIds = null) {
    const career = selectedCareer();
    const year = career?.anios.find(y => y.idAnio === Number(yearSelect.value));
    const selected = selectedIds || new Set([...prerequisites.selectedOptions].map(item => Number(item.value)));
    prerequisites.replaceChildren();
    state.subjects
      .filter(subject => subject.idCarrera === career?.idCarrera && subject.numeroAnio < (year?.numeroAnio || 0) && subject.idMateria !== state.editingId)
      .forEach(subject => {
        const item = option(subject.idMateria, `${subject.numeroAnio}.º año · ${subject.nombre}`);
        item.selected = selected.has(subject.idMateria);
        prerequisites.append(item);
      });
    prerequisites.disabled = !year || prerequisites.options.length === 0;
  }

  function addSchedule(values = {}) {
    const row = document.createElement("div");
    row.className = "schedule-row";
    if (values.id) row.dataset.id = values.id;
    row.innerHTML = '<label class="form-field">Día<select name="diaSemana" required><option>Lunes</option><option>Martes</option><option>Miércoles</option><option>Jueves</option><option>Viernes</option><option>Sábado</option></select></label><label class="form-field">Desde<input name="horaInicio" type="time" required></label><label class="form-field">Hasta<input name="horaFin" type="time" required></label><button class="icon-button" type="button" aria-label="Quitar horario">×</button>';
    row.querySelector("select").value = values.day || "Lunes";
    row.querySelector('[name="horaInicio"]').value = String(values.start || "08:00").slice(0, 5);
    row.querySelector('[name="horaFin"]').value = String(values.end || "10:00").slice(0, 5);
    row.querySelector("button").addEventListener("click", () => {
      if (scheduleList.children.length > 1) row.remove();
    });
    scheduleList.append(row);
  }

  function getSchedulePayloads() {
    return [...scheduleList.querySelectorAll(".schedule-row")].map(row => ({
      id: row.dataset.id ? Number(row.dataset.id) : null,
      diaSemana: row.querySelector('[name="diaSemana"]').value,
      horaInicio: `${row.querySelector('[name="horaInicio"]').value}:00`,
      horaFin: `${row.querySelector('[name="horaFin"]').value}:00`
    }));
  }

  function durationMinutes(schedule) {
    const [startHour, startMinute] = schedule.horaInicio.split(":").map(Number);
    const [endHour, endMinute] = schedule.horaFin.split(":").map(Number);
    return endHour * 60 + endMinute - startHour * 60 - startMinute;
  }

  function renderSubjects() {
    const tbody = document.getElementById("subjectsBody");
    const careerId = Number(filterCareer.value) || null;
    const yearId = Number(filterYear.value) || null;
    const visible = state.subjects.filter(s => (!careerId || s.idCarrera === careerId) && (!yearId || s.idAnio === yearId));
    tbody.replaceChildren();
    if (!visible.length) {
      tbody.innerHTML = '<tr><td colspan="8" class="empty-state">No hay materias para los filtros seleccionados.</td></tr>';
      return;
    }
    visible.forEach(subject => {
      const schedules = state.schedules.filter(h => h.idMateria === subject.idMateria)
        .map(h => `${h.diaSemana} ${String(h.horaInicio).slice(0, 5)}–${String(h.horaFin).slice(0, 5)}`).join(" · ") || "Sin horario";
      const correlations = subject.correlativas?.map(c => c.nombre).join(", ") || "Sin correlativas";
      const row = document.createElement("tr");
      [subject.nombre, subject.carrera, `${subject.numeroAnio}.º año`, subjectPeriodLabel(subject), correlations, schedules].forEach(value => {
        const cell = row.insertCell(); cell.textContent = value;
      });
      const statusCell = row.insertCell();
      const badge = document.createElement("span");
      badge.className = `badge ${subject.estado?.toLowerCase() === "activa" ? "active" : "inactive"}`;
      badge.textContent = subject.estado; statusCell.append(badge);
      const actions = row.insertCell();
      const controls = document.createElement("div");
      controls.className = "subject-actions";
      const edit = document.createElement("button");
      edit.className = "secondary-button"; edit.type = "button"; edit.textContent = "Editar";
      edit.addEventListener("click", () => beginEdit(subject));
      controls.append(edit); actions.append(controls); tbody.append(row);
    });
  }

  async function loadData() {
    try {
      [state.careers, state.subjects, state.schedules, state.teachers, state.examPeriod, state.commissions] = await Promise.all([
        AcadionApi.request("/carreras/"), AcadionApi.request("/materias/"), AcadionApi.request("/horarios/"),
        AcadionApi.request("/api/gestion/usuarios/?rolId=2"),
        AcadionApi.request(`/api/gestion-academica/periodo-inscripcion-examenes?cicloLectivo=${Number(examPeriodCycle.value)}`),
        AcadionApi.request(`/api/comisiones/?cicloLectivo=${new Date().getFullYear()}`)
      ]);
      renderCareerOptions();
      renderAssignmentOptions();
      fillYears(filterYear, selectedCareer(filterCareer), "Todos los años");
      renderPrerequisites(); renderSubjects(); renderExamPeriod(); renderCommissions();
    } catch (error) {
      const tbody = document.getElementById("subjectsBody");
      tbody.replaceChildren(); const row = tbody.insertRow(); const cell = row.insertCell();
      cell.colSpan = 8; cell.className = "empty-state"; cell.textContent = error.message;
    }
  }

  function beginEdit(subject) {
    state.editingId = subject.idMateria;
    careerSelect.value = subject.idCarrera;
    fillYears(yearSelect, selectedCareer(), "Seleccionar año");
    yearSelect.value = subject.idAnio;
    form.nombre.value = subject.nombre;
    form.modalidad.value = subject.modalidad;
    form.estado.value = subject.estado;
    typeSelect.value = subject.tipoCursada || "Anual";
    renderSubjectPeriodOptions(subject.numeroPeriodo);
    renderPrerequisites(new Set((subject.correlativas || []).map(c => c.idMateria)));
    scheduleList.replaceChildren();
    const schedules = state.schedules.filter(h => h.idMateria === subject.idMateria);
    schedules.forEach(h => addSchedule({ id: h.idHorarioMateria, day: h.diaSemana, start: h.horaInicio, end: h.horaFin }));
    if (!schedules.length) addSchedule();
    editPanel.hidden = false;
    document.getElementById("subjectTitle").textContent = subject.nombre;
    editPanel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function resetForm() {
    state.editingId = null;
    form.reset(); yearSelect.replaceChildren(option("", "Seleccionar año")); yearSelect.disabled = true;
    renderSubjectPeriodOptions();
    prerequisites.replaceChildren(); prerequisites.disabled = true;
    scheduleList.replaceChildren(); addSchedule();
    editPanel.hidden = true;
    document.getElementById("subjectTitle").textContent = "Datos académicos";
  }

  async function syncSchedules(subjectId, schedules) {
    const old = state.schedules.filter(h => h.idMateria === subjectId);
    const retained = new Set(schedules.filter(h => h.id).map(h => h.id));
    await Promise.all(old.filter(h => !retained.has(h.idHorarioMateria))
      .map(h => AcadionApi.request(`/horarios/${h.idHorarioMateria}`, { method: "DELETE" })));
    await Promise.all(schedules.map(schedule => AcadionApi.request(schedule.id ? `/horarios/${schedule.id}` : "/horarios/", {
      method: schedule.id ? "PUT" : "POST",
      body: JSON.stringify({ idMateria: subjectId, diaSemana: schedule.diaSemana, horaInicio: schedule.horaInicio, horaFin: schedule.horaFin })
    })));
  }

  careerSelect.addEventListener("change", () => {
    fillYears(yearSelect, selectedCareer(), "Seleccionar año"); renderPrerequisites(new Set());
  });
  yearSelect.addEventListener("change", () => renderPrerequisites(new Set()));
  filterCareer.addEventListener("change", () => {
    fillYears(filterYear, selectedCareer(filterCareer), "Todos los años"); renderSubjects();
  });
  filterYear.addEventListener("change", renderSubjects);
  examPeriodCycle.addEventListener("change", () => loadExamPeriod().catch(error => showExamPeriodMessage(error.message)));

  examPeriodForm.addEventListener("submit", async event => {
    event.preventDefault();
    const start = new Date(`${examPeriodForm.fechaInicio.value}T00:00:00`);
    const end = new Date(`${examPeriodForm.fechaFin.value}T23:59:59`);
    try {
      await AcadionApi.request("/api/gestion-academica/periodo-inscripcion-examenes", {
        method: "PUT",
        body: JSON.stringify({
          cicloLectivo: Number(examPeriodCycle.value),
          fechaInicio: start.toISOString(),
          fechaFin: end.toISOString()
        })
      });
      showExamPeriodMessage("Período de inscripción a finales actualizado correctamente.", true);
      await loadExamPeriod();
    } catch (error) { showExamPeriodMessage(error.message); }
  });

  assignmentForm.addEventListener("submit", async event => {
    event.preventDefault();
    const button = assignmentForm.querySelector('button[type="submit"]');
    button.disabled = true;
    try {
      await AcadionApi.request("/api/gestion-academica/docentes-materias", {
        method: "POST",
        body: JSON.stringify({
          docenteId: Number(assignmentTeacher.value),
          materiaId: Number(assignmentSubject.value),
          cicloLectivo: Number(assignmentCycle.value)
        })
      });
      showAssignmentMessage("El profesor fue asignado correctamente. La materia ya está disponible en su portal.", true);
      assignmentTeacher.value = "";
      assignmentSubject.value = "";
    } catch (error) { showAssignmentMessage(error.message); }
    finally { button.disabled = false; }
  });

  document.getElementById("toggleCommissionForm").addEventListener("click", () => {
    commissionForm.hidden = false;
    commissionForm.cicloLectivo.value = new Date().getFullYear();
    commissionForm.cupo.value = 30;
    commissionForm.horaInicio.value = "08:00";
    commissionForm.horaFin.value = "10:00";
    commissionForm.nombre.focus();
  });
  document.getElementById("cancelCommission").addEventListener("click", () => {
    commissionForm.reset();
    commissionForm.hidden = true;
  });
  commissionForm.addEventListener("submit", async event => {
    event.preventDefault();
    const start = commissionForm.horaInicio.value;
    const end = commissionForm.horaFin.value;
    const message = document.getElementById("commissionMessage");
    const [startHour, startMinute] = start.split(":").map(Number);
    const [endHour, endMinute] = end.split(":").map(Number);
    if (endHour * 60 + endMinute - startHour * 60 - startMinute < 40) {
      message.className = "status-message error";
      message.textContent = "El horario de la comisión debe durar al menos 40 minutos.";
      return;
    }
    try {
      await AcadionApi.request("/api/comisiones/", {
        method: "POST",
        body: JSON.stringify({
          materiaId: Number(commissionForm.materiaId.value),
          nombre: commissionForm.nombre.value.trim(),
          turno: commissionForm.turno.value,
          cicloLectivo: Number(commissionForm.cicloLectivo.value),
          cupo: Number(commissionForm.cupo.value),
          docenteId: commissionForm.docenteId.value ? Number(commissionForm.docenteId.value) : null,
          horarios: [{
            diaSemana: commissionForm.diaSemana.value,
            horaInicio: `${start}:00`,
            horaFin: `${end}:00`
          }]
        })
      });
      message.className = "status-message success";
      message.textContent = "La comisión fue creada correctamente.";
      await loadCommissions();
      commissionForm.reset();
      commissionForm.hidden = true;
    } catch (error) {
      message.className = "status-message error";
      message.textContent = error.message;
    }
  });

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const schedules = getSchedulePayloads();
    if (schedules.some(h => h.horaInicio >= h.horaFin)) {
      setMessage("La hora de finalización debe ser posterior al inicio."); return;
    }
    if (schedules.some(schedule => durationMinutes(schedule) < 40)) {
      setMessage("Cada horario de cursado debe durar como mínimo 40 minutos."); return;
    }
    if (!/^[\p{L}\p{M}\p{N} .()/\-]+$/u.test(form.nombre.value.trim())) {
      setMessage("El nombre de la materia contiene caracteres no permitidos."); return;
    }
    const payload = {
      nombre: form.nombre.value.trim(), modalidad: form.modalidad.value,
      estado: form.estado.value, idAnio: Number(form.idAnio.value),
      tipoCursada: typeSelect.value,
      numeroPeriodo: periodSelect.disabled ? null : Number(periodSelect.value),
      correlativasIds: [...form.correlativas.selectedOptions].map(item => Number(item.value))
    };
    try {
      const subjectId = state.editingId;
      if (!subjectId) return;
      await AcadionApi.request(`/materias/${subjectId}`, { method: "PUT", body: JSON.stringify(payload) });
      await syncSchedules(subjectId, schedules);
      setMessage("Materia actualizada correctamente.", true);
      await loadData();
    } catch (error) { setMessage(error.message); }
  });

  document.getElementById("addSchedule").addEventListener("click", () => addSchedule());
  typeSelect.addEventListener("change", () => renderSubjectPeriodOptions());
  document.getElementById("cancelSubjectEdit").addEventListener("click", resetForm);
  document.getElementById("refreshSubjects").addEventListener("click", loadData);
  renderSubjectPeriodOptions();
  loadData();
});
