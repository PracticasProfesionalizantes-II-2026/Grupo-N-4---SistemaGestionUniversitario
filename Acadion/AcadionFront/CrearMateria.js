document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const state = { careers: [], subjects: [] };
  const form = document.getElementById("subjectForm");
  const careerSelect = document.getElementById("subjectCareer");
  const yearSelect = document.getElementById("subjectYear");
  const planSelect = document.getElementById("subjectPlan");
  const prerequisites = document.getElementById("subjectPrerequisites");
  const scheduleList = document.getElementById("scheduleList");
  const typeSelect = document.getElementById("subjectType");
  const periodSelect = document.getElementById("subjectPeriod");
  const message = document.getElementById("subjectMessage");
  const saveButton = document.getElementById("saveSubject");

  function option(value, text) {
    const item = document.createElement("option");
    item.value = value;
    item.textContent = text;
    return item;
  }

  function showMessage(text, success = false) {
    message.textContent = text;
    message.className = `status-message ${success ? "success" : "error"}`;
  }

  function selectedCareer() {
    return state.careers.find(career => career.idCarrera === Number(careerSelect.value));
  }

  function renderPeriodOptions() {
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
  }

  function renderYears() {
    const career = selectedCareer();
    yearSelect.replaceChildren(option("", "Seleccionar año"));
    (career?.anios || []).forEach(year => yearSelect.append(option(year.idAnio, year.nombreAnio)));
    yearSelect.disabled = !career;
    renderPrerequisites();
  }

  function renderPlans() {
    const career = selectedCareer();
    planSelect.replaceChildren(option("", "Seleccionar plan"));
    (career?.planes || []).forEach(plan => {
      const item = option(plan.id, `${plan.codigo}${plan.activo ? " · vigente" : " · histórico"}`);
      item.selected = plan.activo;
      planSelect.append(item);
    });
    planSelect.disabled = !career || !career.planes?.length;
    renderYears();
  }

  function renderPrerequisites() {
    const career = selectedCareer();
    const year = career?.anios.find(item => item.idAnio === Number(yearSelect.value));
    prerequisites.replaceChildren();
    state.subjects
      .filter(subject => subject.idCarrera === career?.idCarrera &&
        subject.planEstudioId === Number(planSelect.value) &&
        subject.numeroAnio < (year?.numeroAnio || 0))
      .forEach(subject => prerequisites.append(option(subject.idMateria, `${subject.numeroAnio}.º año · ${subject.nombre}`)));
    prerequisites.disabled = !year || !prerequisites.options.length;
  }

  function addSchedule(values = {}) {
    const row = document.createElement("div");
    row.className = "schedule-row";
    row.innerHTML = '<label class="form-field">Día<select name="diaSemana" required><option>Lunes</option><option>Martes</option><option>Miércoles</option><option>Jueves</option><option>Viernes</option><option>Sábado</option></select></label><label class="form-field">Desde<input name="horaInicio" type="time" required></label><label class="form-field">Hasta<input name="horaFin" type="time" required></label><button class="icon-button" type="button" aria-label="Quitar horario">×</button>';
    row.querySelector("select").value = values.day || "Lunes";
    row.querySelector('[name="horaInicio"]').value = values.start || "08:00";
    row.querySelector('[name="horaFin"]').value = values.end || "10:00";
    row.querySelector("button").addEventListener("click", () => {
      if (scheduleList.children.length > 1) row.remove();
    });
    scheduleList.append(row);
  }

  function getSchedules() {
    return [...scheduleList.querySelectorAll(".schedule-row")].map(row => ({
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

  async function loadData() {
    try {
      [state.careers, state.subjects] = await Promise.all([
        AcadionApi.request("/carreras/"),
        AcadionApi.request("/materias/")
      ]);
      state.careers = state.careers.filter(career => career.activa !== false);
      careerSelect.replaceChildren(option("", "Seleccionar carrera"));
      state.careers.forEach(career => careerSelect.append(option(career.idCarrera, `${career.nombre} · ${career.planEstudios}`)));
    } catch (error) {
      showMessage(error.message);
    }
  }

  function resetForm() {
    form.reset();
    yearSelect.replaceChildren(option("", "Seleccionar año"));
    yearSelect.disabled = true;
    planSelect.replaceChildren(option("", "Seleccionar plan"));
    planSelect.disabled = true;
    prerequisites.replaceChildren();
    prerequisites.disabled = true;
    scheduleList.replaceChildren();
    addSchedule();
    renderPeriodOptions();
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const schedules = getSchedules();
    if (schedules.some(schedule => schedule.horaInicio >= schedule.horaFin)) {
      showMessage("La hora de finalización debe ser posterior a la hora de inicio.");
      return;
    }
    if (schedules.some(schedule => durationMinutes(schedule) < 40)) {
      showMessage("Cada horario de cursado debe durar como mínimo 40 minutos.");
      return;
    }
    if (!/^[\p{L}\p{M}\p{N} .()/\-]+$/u.test(form.nombre.value.trim())) {
      showMessage("El nombre de la materia contiene caracteres no permitidos.");
      return;
    }
    const overlap = schedules.some((current, index) => schedules.some((other, otherIndex) =>
      index !== otherIndex && current.diaSemana === other.diaSemana &&
      current.horaInicio < other.horaFin && other.horaInicio < current.horaFin));
    if (overlap) {
      showMessage("La materia tiene horarios superpuestos entre sí.");
      return;
    }

    saveButton.disabled = true;
    let created = null;
    try {
      created = await AcadionApi.request("/materias/", {
        method: "POST",
        body: JSON.stringify({
          nombre: form.nombre.value.trim(),
          modalidad: form.modalidad.value,
          estado: form.estado.value,
          tipoCursada: typeSelect.value,
          numeroPeriodo: periodSelect.disabled ? null : Number(periodSelect.value),
          idAnio: Number(form.idAnio.value),
          planEstudioId: Number(form.planEstudioId.value),
          correlativasIds: [...form.correlativas.selectedOptions].map(item => Number(item.value))
        })
      });
      for (const schedule of schedules) {
        await AcadionApi.request("/horarios/", {
          method: "POST",
          body: JSON.stringify({ idMateria: created.idMateria, ...schedule })
        });
      }
      showMessage("Materia creada correctamente con sus horarios y correlatividades.", true);
      await loadData();
      resetForm();
    } catch (error) {
      if (created?.idMateria) {
        try { await AcadionApi.request(`/materias/${created.idMateria}`, { method: "DELETE" }); } catch { /* La API conserva el error original. */ }
      }
      showMessage(error.message);
    } finally {
      saveButton.disabled = false;
    }
  });

  careerSelect.addEventListener("change", renderPlans);
  planSelect.addEventListener("change", renderPrerequisites);
  yearSelect.addEventListener("change", renderPrerequisites);
  typeSelect.addEventListener("change", renderPeriodOptions);
  form.nombre.addEventListener("input", () => {
    form.nombre.value = form.nombre.value.replace(/[!"#$%]/g, "");
  });
  document.getElementById("addSchedule").addEventListener("click", () => addSchedule());
  addSchedule();
  renderPeriodOptions();
  loadData();
});
