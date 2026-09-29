document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const subjectSelect = document.getElementById("attendanceSubject");
  const dateInput = document.getElementById("attendanceDate");
  const body = document.getElementById("attendanceBody");
  const title = document.getElementById("attendanceTitle");
  const saveButton = document.getElementById("saveAttendance");
  const markAllButton = document.getElementById("markAllPresent");
  const message = document.getElementById("attendanceMessage");
  const classType = document.getElementById("classType");
  const absenceCount = document.getElementById("absenceCount");
  let subjects = [];
  let students = [];
  dateInput.value = new Date().toISOString().slice(0, 10);

  function showMessage(text, error = false) {
    message.hidden = !text; message.textContent = text;
    message.className = `notice-strip${error ? " error" : ""}`;
  }

  function option(value, text) {
    const item = document.createElement("option"); item.value = value; item.textContent = text; return item;
  }

  function statusSelect(current = "") {
    const select = document.createElement("select"); select.className = "attendance-select"; select.dataset.field = "status";
    [["", "Sin marcar"], ["Presente", "Presente"], ["Ausente", "Ausente"], ["Tardanza", "Tardanza"]].forEach(([value, label]) => select.append(option(value, label)));
    select.value = current; return select;
  }

  function render(existing) {
    const byEnrollment = new Map(existing.map(item => [item.idEstudianteMateria, item]));
    body.replaceChildren();
    if (!students.length) {
      const row = body.insertRow(); const cell = row.insertCell(); cell.colSpan = 4; cell.className = "empty-state"; cell.textContent = "No hay estudiantes inscriptos en esta comisión.";
      saveButton.disabled = true; markAllButton.disabled = true; return;
    }
    students.forEach(student => {
      const record = byEnrollment.get(student.idEstudianteMateria);
      const row = body.insertRow(); row.dataset.enrollmentId = student.idEstudianteMateria;
      const nameCell = row.insertCell(); nameCell.className = "student-name"; nameCell.textContent = Acadion.formatearNombre(student.estudiante);
      row.insertCell().textContent = student.legajo || "—";
      row.insertCell().append(statusSelect(record?.tipo || ""));
      const observation = document.createElement("input"); observation.className = "attendance-observation"; observation.dataset.field = "observation"; observation.maxLength = 250; observation.placeholder = "Opcional"; observation.value = record?.observaciones || "";
      row.insertCell().append(observation);
    });
    saveButton.disabled = false; markAllButton.disabled = false;
  }

  async function loadAttendance() {
    const subject = subjects.find(item => item.materiaId === Number(subjectSelect.value));
    if (!subject || !dateInput.value) return;
    classType.value = String(subject.modalidad || "").toLowerCase() === "virtual" ? "Virtual" : "Presencial";
    absenceCount.value = "1";
    title.textContent = `${subject.nombre} · ${subject.carrera || subject.anio || subject.cicloLectivo}`;
    body.replaceChildren(); const loadingRow = body.insertRow(); const loadingCell = loadingRow.insertCell(); loadingCell.colSpan = 4; loadingCell.className = "empty-state"; loadingCell.textContent = "Cargando comisión...";
    saveButton.disabled = true; markAllButton.disabled = true; showMessage("");
    try {
      students = await AcadionApi.request(`/api/docente/alumnos?materiaId=${subject.materiaId}&cicloLectivo=${subject.cicloLectivo}`);
      let existing = [];
      try { existing = await AcadionApi.request(`/api/docente/asistencias?materiaId=${subject.materiaId}&fecha=${dateInput.value}`); } catch { existing = []; }
      const classRecord = existing[0];
      if (classRecord) {
        classType.value = classRecord.tipoClase || classType.value;
        const absenceRecord = existing.find(item => item.cantidadInasistencias > 0);
        absenceCount.value = String(absenceRecord?.cantidadInasistencias || 1);
      }
      render(existing);
    } catch (error) { students = []; render([]); showMessage(error.message, true); }
  }

  async function loadSubjects() {
    try {
      subjects = await AcadionApi.request("/api/me/materias");
      subjectSelect.replaceChildren(option("", "Seleccionar materia"));
      subjects.sort((a, b) => b.cicloLectivo - a.cicloLectivo || a.nombre.localeCompare(b.nombre, "es")).forEach(subject => subjectSelect.append(option(subject.materiaId, `${subject.nombre} · ${subject.cicloLectivo}`)));
      const requested = Number(new URLSearchParams(location.search).get("materiaId"));
      if (subjects.some(item => item.materiaId === requested)) subjectSelect.value = requested;
      else if (subjects.length) subjectSelect.value = subjects[0].materiaId;
      if (subjectSelect.value) loadAttendance();
    } catch (error) { showMessage(error.message, true); }
  }

  document.getElementById("loadAttendance").addEventListener("click", loadAttendance);
  subjectSelect.addEventListener("change", loadAttendance);
  dateInput.addEventListener("change", loadAttendance);
  markAllButton.addEventListener("click", () => body.querySelectorAll('[data-field="status"]').forEach(select => { select.value = "Presente"; }));
  saveButton.addEventListener("click", async () => {
    const markedRows = [...body.querySelectorAll("tr[data-enrollment-id]")].filter(row => row.querySelector('[data-field="status"]').value);
    if (!markedRows.length) { showMessage("Marcá al menos un estado antes de guardar.", true); return; }
    if (Number(absenceCount.value) < 1 || Number(absenceCount.value) > 10) { showMessage("La cantidad de inasistencias debe estar entre 1 y 10.", true); absenceCount.focus(); return; }
    saveButton.disabled = true; saveButton.textContent = "Guardando..."; showMessage("");
    const results = await Promise.allSettled(markedRows.map(row => AcadionApi.request("/api/docente/asistencias", {
      method: "POST",
      body: JSON.stringify({
        idEstudianteMateria: Number(row.dataset.enrollmentId),
        fecha: `${dateInput.value}T00:00:00`,
        tipo: row.querySelector('[data-field="status"]').value,
        tipoClase: classType.value,
        temaDictado: "",
        cantidadInasistencias: Number(absenceCount.value),
        observaciones: row.querySelector('[data-field="observation"]').value.trim()
      })
    })));
    const failed = results.filter(result => result.status === "rejected");
    showMessage(failed.length ? `Se guardaron ${results.length - failed.length} registros; ${failed.length} no pudieron guardarse.` : "La asistencia se guardó correctamente.", failed.length > 0);
    saveButton.disabled = false; saveButton.textContent = "Guardar asistencia";
  });
  loadSubjects();
});
