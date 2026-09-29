document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();

  const form = document.getElementById("evaluationForm");
  const finalForm = document.getElementById("finalForm");
  const createPanel = document.getElementById("evaluationCreatePanel");
  const editPanel = document.getElementById("examEditPanel");
  const editForm = document.getElementById("examEditForm");
  const subjectSelect = document.getElementById("evaluationSubject");
  const finalSubject = document.getElementById("finalSubject");
  const subjectFilter = document.getElementById("examSubjectFilter");
  const tableBody = document.getElementById("evaluationTableBody");
  const finalSubjectList = document.getElementById("finalSubjectList");
  const gradePanel = document.getElementById("gradePanel");
  const gradeBody = document.getElementById("gradeBody");
  const saveGrades = document.getElementById("saveGrades");
  const regularGrade = document.getElementById("regularGrade");
  const enablePromotion = document.getElementById("enablePromotion");
  const promotionGrade = document.getElementById("promotionGrade");
  const message = document.getElementById("evaluationMessage");

  let subjects = [];
  let exams = [];
  let selectedExam = null;
  let selectedEditExam = null;
  const expandedSubjects = new Set();
  form.fecha.value = new Date().toISOString().slice(0, 10);
  finalForm.fecha.value = localDateTimeInputValue(new Date());

  function showMessage(text, error = false) {
    message.hidden = !text;
    message.textContent = text;
    message.className = `notice-strip${error ? " error" : ""}`;
  }

  function option(value, text) {
    const item = document.createElement("option");
    item.value = value;
    item.textContent = text;
    return item;
  }

  function assignmentValue(subject) {
    return `${subject.materiaId}|${subject.cicloLectivo}`;
  }

  function parseAssignment(value) {
    const [materiaId, cicloLectivo] = String(value || "").split("|").map(Number);
    return subjects.find(item => item.materiaId === materiaId && item.cicloLectivo === cicloLectivo) || null;
  }

  function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    const includeTime = date.getHours() !== 0 || date.getMinutes() !== 0;
    return new Intl.DateTimeFormat("es-AR", {
      day: "2-digit", month: "2-digit", year: "numeric",
      ...(includeTime ? { hour: "2-digit", minute: "2-digit" } : {})
    }).format(date);
  }

  function localDateTimeInputValue(value) {
    const date = value instanceof Date ? value : new Date(value);
    const pad = number => String(number).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  function dateTimePayload(value) {
    return value.length === 10 ? `${value}T00:00:00` : `${value}:00`;
  }

  function examType(value) {
    const labels = { TrabajoPractico: "Trabajo práctico", Presentacion: "Presentación" };
    return labels[value] || value;
  }

  function actionButton(text, className, handler) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = className;
    button.textContent = text;
    button.addEventListener("click", handler);
    return button;
  }

  function emptyRow(body, text, columns) {
    body.replaceChildren();
    const row = body.insertRow();
    const cell = row.insertCell();
    cell.colSpan = columns;
    cell.className = "empty-state";
    cell.textContent = text;
  }

  function setCreatePanel(open) {
    createPanel.hidden = !open;
    document.getElementById("toggleEvaluationForm").textContent = open ? "Cancelar creación" : "+ Crear evaluación";
    if (open) createPanel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function setFinalForm(open) {
    finalForm.hidden = !open;
    document.getElementById("toggleFinalForm").textContent = open ? "Cancelar" : "+ Programar final";
  }

  function renderInternalExams() {
    const assignment = parseAssignment(subjectFilter.value);
    const visible = exams.filter(exam => exam.tipoExamen !== "Final" &&
      (!assignment || exam.idMateria === assignment.materiaId && exam.cicloLectivo === assignment.cicloLectivo));
    tableBody.replaceChildren();
    if (!visible.length) {
      emptyRow(tableBody, "No hay evaluaciones de cursada para la materia seleccionada.", 6);
      return;
    }

    visible.forEach(exam => {
      const row = tableBody.insertRow();
      const typeCell = row.insertCell();
      const badge = document.createElement("span");
      badge.className = "badge active";
      badge.textContent = examType(exam.tipoExamen);
      typeCell.append(badge);
      row.insertCell().textContent = exam.materia || "—";
      row.insertCell().textContent = formatDate(exam.fecha);
      row.insertCell().textContent = exam.cicloLectivo;
      row.insertCell().textContent = String(exam.cantidadNotas ?? 0);
      const actions = row.insertCell();
      actions.className = "exam-action-cell";
      actions.append(
        actionButton("Asignar notas", "primary-button compact-button", () => loadGradebook(exam)),
        actionButton("Modificar fecha", "secondary-button compact-button", () => beginDateEdit(exam)),
        actionButton("Eliminar", "danger-button compact-button", () => deleteExam(exam))
      );
    });
  }

  function renderFinals() {
    finalSubjectList.replaceChildren();
    if (!subjects.length) {
      const empty = document.createElement("p"); empty.className = "empty-state"; empty.textContent = "No tenés materias asignadas.";
      finalSubjectList.append(empty); return;
    }

    subjects.forEach(subject => {
      const key = assignmentValue(subject);
      const subjectFinals = exams.filter(exam => exam.tipoExamen === "Final" &&
        exam.idMateria === subject.materiaId && exam.cicloLectivo === subject.cicloLectivo);
      const article = document.createElement("article");
      article.className = "final-subject-card";
      const toggle = document.createElement("button");
      toggle.type = "button";
      toggle.className = "final-subject-toggle";
      const title = document.createElement("span");
      const subjectName = document.createElement("strong");
      subjectName.textContent = subject.nombre;
      const subjectMeta = document.createElement("small");
      subjectMeta.textContent = `${subject.cicloLectivo} · ${subject.carrera || "Carrera no informada"}`;
      title.append(subjectName, subjectMeta);
      const count = document.createElement("span");
      count.className = "final-count";
      count.textContent = `${subjectFinals.length} ${subjectFinals.length === 1 ? "mesa" : "mesas"}  ${expandedSubjects.has(key) ? "−" : "+"}`;
      toggle.append(title, count);
      const body = document.createElement("div");
      body.className = "final-subject-body";
      body.hidden = !expandedSubjects.has(key);
      toggle.addEventListener("click", () => {
        expandedSubjects.has(key) ? expandedSubjects.delete(key) : expandedSubjects.add(key);
        renderFinals();
      });
      article.append(toggle, body);

      if (!subjectFinals.length) {
        const empty = document.createElement("p"); empty.className = "final-empty"; empty.textContent = "No hay exámenes finales programados para esta materia.";
        body.append(empty);
      } else {
        subjectFinals.forEach(exam => {
          const row = document.createElement("div"); row.className = "final-exam-row";
          const info = document.createElement("div");
          const finalTitle = document.createElement("strong");
          finalTitle.textContent = `Final del ${formatDate(exam.fecha)}`;
          const finalMeta = document.createElement("small");
          finalMeta.textContent = `${exam.cantidadInscriptos ?? 0} estudiantes inscriptos · ${exam.cantidadNotas ?? 0} notas cargadas`;
          info.append(finalTitle, finalMeta);
          const actions = document.createElement("div"); actions.className = "final-exam-actions";
          actions.append(
            actionButton("Cargar notas", "primary-button compact-button", () => loadGradebook(exam)),
            actionButton("Modificar fecha", "secondary-button compact-button", () => beginDateEdit(exam)),
            actionButton("Eliminar", "danger-button compact-button", () => deleteExam(exam))
          );
          row.append(info, actions); body.append(row);
        });
      }
      finalSubjectList.append(article);
    });
  }

  function renderAll() {
    renderInternalExams();
    renderFinals();
  }

  async function loadExams({ preserveMessage = false } = {}) {
    emptyRow(tableBody, "Cargando evaluaciones...", 6);
    try {
      exams = await AcadionApi.request("/api/docente/examenes");
      renderAll();
      if (!preserveMessage) showMessage("");
    } catch (error) {
      exams = [];
      emptyRow(tableBody, "No fue posible cargar las evaluaciones.", 6);
      finalSubjectList.innerHTML = '<p class="empty-state">No fue posible cargar los exámenes finales.</p>';
      showMessage(error.message, true);
    }
  }

  function beginDateEdit(exam) {
    selectedEditExam = exam;
    document.getElementById("editExamTitle").textContent = `${examType(exam.tipoExamen)} · ${exam.materia}`;
    document.getElementById("editExamDate").value = localDateTimeInputValue(exam.fecha);
    editPanel.hidden = false;
    editPanel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  async function deleteExam(exam) {
    if (!window.confirm(`¿Eliminar ${examType(exam.tipoExamen).toLowerCase()} de ${exam.materia} del ${formatDate(exam.fecha)}? Esta acción también eliminará sus notas e inscripciones.`)) return;
    try {
      await AcadionApi.request(`/api/docente/examenes/${exam.idExamen}`, { method: "DELETE" });
      if (selectedExam?.idExamen === exam.idExamen) gradePanel.hidden = true;
      showMessage("La evaluación se eliminó correctamente.");
      await loadExams({ preserveMessage: true });
    } catch (error) { showMessage(error.message, true); }
  }

  function calculateCondition(grade) {
    if (!Number.isFinite(grade)) return "—";
    const regular = Number(regularGrade.value);
    if (selectedExam?.esFinal || selectedExam?.tipoExamen === "Final") {
      return grade >= regular ? "Aprobó" : "Desaprobó";
    }
    const promotion = enablePromotion.checked ? Number(promotionGrade.value) : null;
    if (enablePromotion.checked && Number.isFinite(promotion) && grade >= promotion) return "Promocionó";
    return grade >= regular ? "Regularizó" : "Desaprobó";
  }

  function updateConditions() {
    gradeBody.querySelectorAll("tr[data-student-id]").forEach(row => {
      const value = row.querySelector(".grade-input").value;
      row.querySelector(".grade-condition").textContent = value === "" ? "—" : calculateCondition(Number(value));
    });
  }

  async function loadGradebook(exam) {
    selectedExam = exam;
    gradePanel.hidden = false;
    document.getElementById("gradeTitle").textContent = exam.tipoExamen === "Final" ? "Acta de examen final" : "Asignar notas";
    document.getElementById("gradeSubtitle").textContent = `${examType(exam.tipoExamen)} · ${exam.materia} · ${formatDate(exam.fecha)}`;
    emptyRow(gradeBody, "Cargando estudiantes y calificaciones...", 4);
    saveGrades.disabled = true;
    showMessage("");
    gradePanel.scrollIntoView({ behavior: "smooth", block: "start" });

    try {
      const data = await AcadionApi.request(`/api/docente/examenes/${exam.idExamen}/planilla`);
      selectedExam = data.examen;
      regularGrade.value = data.examen.notaMinimaRegularizacion ?? 6;
      enablePromotion.checked = !data.examen.esFinal && data.examen.notaMinimaPromocion !== null;
      promotionGrade.value = data.examen.notaMinimaPromocion ?? 8;
      document.getElementById("promotionToggleField").hidden = data.examen.esFinal;
      document.getElementById("promotionGradeField").hidden = data.examen.esFinal || !enablePromotion.checked;
      const deadline = document.getElementById("gradeDeadline");
      deadline.textContent = data.examen.esFinal
        ? data.examen.cargaHabilitada
          ? `Carga habilitada hasta el ${formatDate(data.examen.fechaLimiteCarga)}.`
          : data.examen.motivoBloqueo
        : "La condición se calcula automáticamente con estos criterios.";
      deadline.className = `grading-deadline${data.examen.cargaHabilitada ? "" : " is-blocked"}`;

      gradeBody.replaceChildren();
      if (!data.estudiantes.length) {
        emptyRow(gradeBody, data.examen.esFinal
          ? "Todavía no hay estudiantes inscriptos en esta mesa final."
          : "No hay estudiantes inscriptos en esta comisión.", 4);
        return;
      }

      data.estudiantes.forEach(student => {
        const row = gradeBody.insertRow();
        row.dataset.studentId = student.idEstudiante;
        row.insertCell().textContent = `${student.estudiante}${student.legajo ? ` · ${student.legajo}` : ""}`;
        const grade = document.createElement("input");
        grade.className = "grade-input"; grade.type = "number"; grade.min = "0"; grade.max = "10"; grade.step = "0.01"; grade.placeholder = "0–10";
        if (student.nota !== null) grade.value = student.nota;
        grade.disabled = !data.examen.cargaHabilitada;
        grade.addEventListener("input", updateConditions);
        row.insertCell().append(grade);
        const condition = row.insertCell(); condition.className = "grade-condition"; condition.textContent = student.condicion || "—";
        const observation = document.createElement("input");
        observation.className = "grade-observation"; observation.maxLength = 300; observation.placeholder = "Opcional"; observation.value = student.observaciones || ""; observation.disabled = !data.examen.cargaHabilitada;
        row.insertCell().append(observation);
      });
      updateConditions();
      saveGrades.disabled = !data.examen.cargaHabilitada;
    } catch (error) {
      emptyRow(gradeBody, error.message, 4);
      showMessage(error.message, true);
    }
  }

  function populateSubjectSelectors() {
    subjectSelect.replaceChildren(option("", "Seleccionar materia"));
    finalSubject.replaceChildren(option("", "Seleccionar materia"));
    subjectFilter.replaceChildren(option("", "Todas las materias"));
    subjects.forEach(subject => {
      const value = assignmentValue(subject);
      const label = `${subject.nombre} · ${subject.cicloLectivo}`;
      subjectSelect.append(option(value, label));
      finalSubject.append(option(value, label));
      subjectFilter.append(option(value, label));
    });
    const requestedId = Number(new URLSearchParams(location.search).get("materiaId"));
    const initial = subjects.find(item => item.materiaId === requestedId) || subjects[0];
    if (initial) {
      subjectSelect.value = assignmentValue(initial);
      finalSubject.value = assignmentValue(initial);
      if (requestedId) subjectFilter.value = assignmentValue(initial);
    }
  }

  async function loadSubjects() {
    try {
      subjects = await AcadionApi.request("/api/me/materias");
      subjects.sort((a, b) => b.cicloLectivo - a.cicloLectivo || a.nombre.localeCompare(b.nombre, "es"));
      populateSubjectSelectors();
      await loadExams();
    } catch (error) {
      emptyRow(tableBody, error.message, 6);
      showMessage(error.message, true);
    }
  }

  async function createExam(assignment, type, date) {
    return AcadionApi.request("/api/docente/examenes", {
      method: "POST",
      body: JSON.stringify({ idMateria: assignment.materiaId, cicloLectivo: assignment.cicloLectivo, fecha: dateTimePayload(date), tipoExamen: type })
    });
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const assignment = parseAssignment(subjectSelect.value);
    if (!assignment) return showMessage("Seleccioná la materia de la evaluación.", true);
    const button = form.querySelector('button[type="submit"]'); button.disabled = true;
    try {
      await createExam(assignment, form.tipoExamen.value, form.fecha.value);
      subjectFilter.value = subjectSelect.value;
      setCreatePanel(false);
      showMessage("La evaluación se creó correctamente.");
      await loadExams({ preserveMessage: true });
    } catch (error) { showMessage(error.message, true); }
    finally { button.disabled = false; }
  });

  finalForm.addEventListener("submit", async event => {
    event.preventDefault();
    const assignment = parseAssignment(finalSubject.value);
    if (!assignment) return showMessage("Seleccioná la materia del examen final.", true);
    const button = finalForm.querySelector('button[type="submit"]'); button.disabled = true;
    try {
      await createExam(assignment, "Final", finalForm.fecha.value);
      expandedSubjects.add(assignmentValue(assignment));
      setFinalForm(false);
      showMessage("El examen final se programó correctamente.");
      await loadExams({ preserveMessage: true });
    } catch (error) { showMessage(error.message, true); }
    finally { button.disabled = false; }
  });

  editForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (!selectedEditExam) return;
    const button = editForm.querySelector('button[type="submit"]'); button.disabled = true;
    try {
      await AcadionApi.request(`/api/docente/examenes/${selectedEditExam.idExamen}/fecha`, {
        method: "PUT", body: JSON.stringify({ fecha: dateTimePayload(document.getElementById("editExamDate").value) })
      });
      editPanel.hidden = true; selectedEditExam = null;
      showMessage("La fecha se modificó correctamente.");
      await loadExams({ preserveMessage: true });
    } catch (error) { showMessage(error.message, true); }
    finally { button.disabled = false; }
  });

  saveGrades.addEventListener("click", async () => {
    if (!selectedExam) return;
    const regular = Number(regularGrade.value);
    const promotion = enablePromotion.checked ? Number(promotionGrade.value) : null;
    if (!Number.isFinite(regular) || regular < 0 || regular > 10)
      return showMessage("La nota para regularizar debe estar entre 0 y 10.", true);
    if (enablePromotion.checked && (!Number.isFinite(promotion) || promotion < regular || promotion > 10))
      return showMessage("La nota para promocionar no puede ser menor que la de regularización y debe estar entre 0 y 10.", true);

    const rows = [...gradeBody.querySelectorAll("tr[data-student-id]")].filter(row => row.querySelector(".grade-input").value !== "");
    if (rows.some(row => { const value = Number(row.querySelector(".grade-input").value); return value < 0 || value > 10; }))
      return showMessage("Todas las notas deben estar entre 0 y 10.", true);

    saveGrades.disabled = true; saveGrades.textContent = "Guardando...";
    try {
      await AcadionApi.request(`/api/docente/examenes/${selectedExam.idExamen}/criterios`, {
        method: "PUT",
        body: JSON.stringify({ notaMinimaRegularizacion: regular, notaMinimaPromocion: selectedExam.esFinal ? null : promotion })
      });
      const results = await Promise.allSettled(rows.map(row => AcadionApi.request("/api/docente/notas", {
        method: "POST",
        body: JSON.stringify({
          idExamen: selectedExam.idExamen,
          idEstudiante: Number(row.dataset.studentId),
          nota: Number(row.querySelector(".grade-input").value),
          observaciones: row.querySelector(".grade-observation").value.trim()
        })
      })));
      const failed = results.filter(result => result.status === "rejected");
      showMessage(failed.length ? `${failed.length} notas no pudieron guardarse.` : rows.length ? "Los criterios y las notas se guardaron correctamente." : "Los criterios se guardaron correctamente.", failed.length > 0);
      if (!failed.length) {
        await loadGradebook({ ...selectedExam, materia: selectedExam.materia });
        await loadExams({ preserveMessage: true });
      }
    } catch (error) { showMessage(error.message, true); }
    finally { saveGrades.disabled = false; saveGrades.textContent = "Guardar criterios y notas"; }
  });

  enablePromotion.addEventListener("change", () => {
    document.getElementById("promotionGradeField").hidden = !enablePromotion.checked;
    updateConditions();
  });
  regularGrade.addEventListener("input", updateConditions);
  promotionGrade.addEventListener("input", updateConditions);
  subjectFilter.addEventListener("change", renderInternalExams);
  document.getElementById("toggleEvaluationForm").addEventListener("click", () => setCreatePanel(createPanel.hidden));
  document.getElementById("closeEvaluationForm").addEventListener("click", () => setCreatePanel(false));
  document.getElementById("cancelEvaluation").addEventListener("click", () => setCreatePanel(false));
  document.getElementById("toggleFinalForm").addEventListener("click", () => setFinalForm(finalForm.hidden));
  document.getElementById("cancelFinal").addEventListener("click", () => setFinalForm(false));
  document.getElementById("cancelExamEdit").addEventListener("click", () => { editPanel.hidden = true; selectedEditExam = null; });
  document.getElementById("refreshEvaluations").addEventListener("click", () => loadExams());
  document.getElementById("closeGradePanel").addEventListener("click", () => { selectedExam = null; gradePanel.hidden = true; });

  loadSubjects();
});
