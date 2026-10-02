(() => {
  const CURRENT_YEAR = new Date().getFullYear();

  const normalize = value => String(value || "").trim().toLowerCase();
  const isApproved = value => ["aprobada", "aprobado", "aprobó", "promocionada", "promocionado", "promocionó"].includes(normalize(value));
  const isFailed = value => ["desaprobada", "desaprobado", "libre"].includes(normalize(value));

  function formatDate(value) {
    if (!value) return "";
    const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (match) return `${match[3]}/${match[2]}/${match[1]}`;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "" : date.toLocaleDateString("es-AR");
  }

  function createEmpty(message) {
    const empty = document.createElement("p");
    empty.className = "report-empty";
    empty.textContent = message;
    return empty;
  }

  function setError(container) {
    container.replaceChildren(createEmpty("No pudimos cargar la información en este momento."));
  }

  function setYearOptions(select, items, includeAll = false) {
    if (!select) return;
    const years = new Set([CURRENT_YEAR]);
    items.forEach(item => {
      const year = Number(item.cicloLectivo);
      if (year > 0) years.add(year);
    });
    const options = [...years]
      .sort((a, b) => b - a)
      .map(year => {
        const option = document.createElement("option");
        option.value = String(year);
        option.textContent = String(year);
        return option;
      });
    if (includeAll) {
      const all = document.createElement("option");
      all.value = "all";
      all.textContent = "Mostrar todo";
      options.unshift(all);
    }
    select.replaceChildren(...options);
  }

  function createCell(value) {
    const cell = document.createElement("td");
    cell.textContent = value === null || value === undefined || value === "" ? "—" : String(value);
    return cell;
  }

  function createEvaluationTable(exams, notes, teacher) {
    const wrapper = document.createElement("div");
    wrapper.className = "table-wrapper";
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const headRow = document.createElement("tr");
    ["Fecha", "Descripción", "Tipo", "Nota", "Resultado", "Corregido por"].forEach(label => {
      const header = document.createElement("th");
      header.textContent = label;
      headRow.append(header);
    });
    head.append(headRow);
    const body = document.createElement("tbody");
    const notesByExam = new Map(notes.map(note => [Number(note.idExamen), note]));
    exams.filter(exam => notesByExam.has(Number(exam.idExamen))).forEach(exam => {
      const note = notesByExam.get(Number(exam.idExamen));
      const grade = note?.nota;
      const result = note?.resultado || (grade === null || grade === undefined ? "" : Number(grade) >= 4 ? "Aprobado" : "Desaprobado");
      const row = document.createElement("tr");
      row.append(
        createCell(formatDate(exam.fecha)),
        createCell(note?.descripcion || note?.observaciones),
        createCell(formatExamType(exam.tipoExamen)),
        createCell(grade),
        createCell(result),
        createCell(note?.corregidoPor || (note ? teacher : ""))
      );
      body.append(row);
    });
    table.append(head, body);
    wrapper.append(table);
    return wrapper;
  }

  function formatTime(value) {
    return value ? String(value).slice(0, 5) : "";
  }

  function formatSchedules(schedules) {
    if (!Array.isArray(schedules) || !schedules.length) return "Sin horarios cargados";
    return schedules.map(schedule =>
      `${schedule.diaSemana} ${formatTime(schedule.horaInicio)}–${formatTime(schedule.horaFin)}`
    ).join("\n");
  }

  function createCourseInfo(label, value, className) {
    const item = document.createElement("div");
    item.className = className;
    const caption = document.createElement("span");
    caption.textContent = label;
    const data = document.createElement("strong");
    data.textContent = value || "—";
    item.append(caption, data);
    return item;
  }

  function attendanceSummary(items) {
    const units = item => Math.max(1, Number(item.cantidadInasistencias || 1));
    const presents = items.filter(item => normalize(item.tipo) === "presente").length;
    const absences = items
      .filter(item => ["ausente", "justificada"].includes(normalize(item.tipo)))
      .reduce((total, item) => total + units(item), 0);
    const justified = items
      .filter(item => item.justificada === true || normalize(item.tipo) === "justificada")
      .reduce((total, item) => total + units(item), 0);
    const late = items.filter(item => normalize(item.tipo) === "tardanza").length;
    return `${presents} presentes · ${absences} ausencias · ${justified} justificadas · ${late} tardanzas`;
  }

  function createCurrentSubject(subject, summary, exams, notes, attendance) {
    const article = document.createElement("article");
    article.className = "current-course";
    const header = document.createElement("header");
    header.className = "current-course-header";
    const heading = document.createElement("div");
    const year = document.createElement("p");
    year.className = "course-year";
    year.textContent = `${subject.anio || "Año no informado"} · Ciclo ${subject.cicloLectivo}`;
    const title = document.createElement("h2");
    title.textContent = subject.nombre || "Materia";
    heading.append(year, title);
    const status = document.createElement("span");
    status.className = "course-status";
    status.textContent = summary?.estado || subject.estado || "Cursando";
    header.append(heading, status);

    const details = document.createElement("div");
    details.className = "course-details";
    details.append(
      createCourseInfo("Docente", subject.docente || "Sin docente asignado", "course-detail"),
      createCourseInfo("Modalidad", subject.modalidad || "Sin definir", "course-detail"),
      createCourseInfo("Horarios", formatSchedules(subject.horarios), "course-detail"),
      createCourseInfo("Inicio del dictado", subject.inicioDictado ? formatDate(subject.inicioDictado) : "Pendiente", "course-detail")
    );

    const gradedCount = notes.length;
    const pendingCount = Math.max(0, exams.length - gradedCount);
    const metrics = document.createElement("div");
    metrics.className = "course-metrics";
    metrics.append(
      createCourseInfo("Asistencia", attendanceSummary(attendance), "course-metric"),
      createCourseInfo("Evaluaciones", `${gradedCount} calificadas · ${pendingCount} pendientes`, "course-metric"),
      createCourseInfo("Situación académica", summary?.estado || subject.estado || "Cursando", "course-metric")
    );

    const evaluations = document.createElement("section");
    evaluations.className = "course-evaluations";
    const evaluationsTitle = document.createElement("h3");
    evaluationsTitle.textContent = "Evaluaciones de cursada calificadas";
    evaluations.append(evaluationsTitle);
    const gradedExams = exams.filter(exam =>
      notes.some(note => Number(note.idExamen) === Number(exam.idExamen)));
    if (gradedExams.length) {
      evaluations.append(createEvaluationTable(gradedExams, notes, subject.docente));
    } else {
      const empty = document.createElement("p");
      empty.className = "course-empty";
      empty.textContent = "Todavía no hay parciales, trabajos prácticos, presentaciones o recuperatorios calificados.";
      evaluations.append(empty);
    }

    article.append(header, details, metrics, evaluations);
    return article;
  }

  async function renderCurrentSubjects() {
    const container = document.getElementById("reportContent");
    try {
      const [subjectsData, examsData, notesData, summaryData, attendanceData] = await Promise.all([
        AcadionApi.request("/api/me/materias"),
        AcadionApi.request("/api/me/evaluaciones"),
        AcadionApi.request("/api/me/notas"),
        AcadionApi.request("/api/me/resumen-academico"),
        AcadionApi.request("/api/me/asistencias")
      ]);
      const subjects = Array.isArray(subjectsData) ? subjectsData : [];
      const exams = Array.isArray(examsData) ? examsData : [];
      const notes = Array.isArray(notesData) ? notesData : [];
      const summaries = Array.isArray(summaryData) ? summaryData : [];
      const attendance = Array.isArray(attendanceData) ? attendanceData : [];
      // La cursada del ciclo debe permanecer visible aunque una modificación de notas
      // cambie su condición a Regular, Promocionada o Libre. El endpoint ya excluye
      // únicamente las inscripciones canceladas.
      const current = subjects.filter(subject => Number(subject.cicloLectivo) === CURRENT_YEAR);
      container.replaceChildren();
      if (!current.length) {
        container.append(createEmpty(`No tenés materias registradas en el ciclo ${CURRENT_YEAR}.`));
        return;
      }
      current.forEach(subject => {
        const summary = summaries.find(item => Number(item.inscripcionId) === Number(subject.inscripcionId));
        const subjectExams = exams.filter(exam =>
          Number(exam.idMateria) === Number(subject.materiaId) &&
          Number(exam.cicloLectivo) === CURRENT_YEAR &&
          normalize(exam.tipoExamen) !== "final");
        const examIds = new Set(subjectExams.map(exam => Number(exam.idExamen)));
        const subjectNotes = notes.filter(note => examIds.has(Number(note.idExamen)));
        const subjectAttendance = attendance.filter(item =>
          Number(item.materiaId) === Number(subject.materiaId) &&
          Number(item.cicloLectivo) === CURRENT_YEAR);
        container.append(createCurrentSubject(
          subject, summary, subjectExams, subjectNotes, subjectAttendance));
      });
    } catch (error) {
      console.error("No fue posible cargar la cursada actual.", error);
      setError(container);
    }
  }

  function createRecord(subject, fallbackText) {
    const article = document.createElement("article");
    article.className = "record";
    const title = document.createElement("h2");
    title.textContent = subject.nombre || "Materia";
    const detail = document.createElement("p");
    const parts = [subject.estado || fallbackText];
    if (subject.calificacionFinal !== undefined && subject.calificacionFinal !== null) parts.push(`Calificación ${Number(subject.calificacionFinal).toFixed(2)}`);
    if (subject.viaAprobacion) parts.push(subject.viaAprobacion);
    if (subject.fechaAprobacion) parts.push(formatDate(subject.fechaAprobacion));
    detail.textContent = parts.join(" — ");
    article.append(title, detail);
    return article;
  }

  const isFinalGrade = note => note.esFinal === true || normalize(note.examen) === "final";
  const formatExamType = value => ({
    trabajopractico: "Trabajo práctico",
    presentacion: "Presentación",
    recuperatorio: "Recuperatorio",
    parcial: "Parcial",
    final: "Final"
  }[normalize(value).replace(/\s+/g, "")] || value || "Evaluación");
  const isApprovedGrade = note => {
    const result = normalize(note.resultado);
    if (["aprobó", "aprobado", "regularizó", "regularizado", "promocionó", "promocionado"].includes(result)) return true;
    if (["desaprobó", "desaprobado", "libre"].includes(result)) return false;
    const grade = Number(note.nota);
    const minimum = Number(note.notaMinimaRegularizacion ?? 6);
    return Number.isFinite(grade) && grade >= minimum;
  };

  function courseYearLabel(note) {
    const number = Number(note.numeroAnio || 0);
    if (number > 0) return `${number}.º año`;
    return note.anioCursada || "Año de cursado no informado";
  }

  function groupByCourseYear(notes) {
    return notes.reduce((groups, note) => {
      const number = Number(note.numeroAnio || 0);
      const key = `${String(number).padStart(3, "0")}|${courseYearLabel(note)}`;
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key).push(note);
      return groups;
    }, new Map());
  }

  function createGradeGroup(label, notes, finalGrades) {
    const section = document.createElement("section");
    section.className = "panel grade-group";
    const title = document.createElement("h2");
    title.textContent = label;
    const wrapper = document.createElement("div");
    wrapper.className = "table-wrapper";
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const headRow = document.createElement("tr");
    const headers = finalGrades
      ? ["Materia", "Nota", "Fecha", "Corregido por", "Condición"]
      : ["Materia", "Evaluación", "Fecha", "Nota", "Condición", "Corregido por"];
    headers.forEach(text => {
      const th = document.createElement("th");
      th.textContent = text;
      headRow.append(th);
    });
    head.append(headRow);
    const body = document.createElement("tbody");
    notes
      .slice()
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))
      .forEach(note => {
        const approved = isApprovedGrade(note);
        const failedRecovery = !approved && normalize(note.examen) === "recuperatorio";
        const condition = finalGrades
          ? (approved ? "Aprobado" : "Desaprobado")
          : (failedRecovery ? "Libre" : note.resultado || (approved ? "Aprobado" : "Desaprobado"));
        const row = document.createElement("tr");
        row.append(createCell(note.materia));
        if (!finalGrades) row.append(createCell(formatExamType(note.examen)));
        row.append(
          createCell(finalGrades ? note.nota : formatDate(note.fecha)),
          createCell(finalGrades ? formatDate(note.fecha) : note.nota),
          createCell(finalGrades ? note.corregidoPor : condition),
          createCell(finalGrades ? condition : note.corregidoPor)
        );
        body.append(row);
      });
    table.append(head, body);
    wrapper.append(table);
    section.append(title, wrapper);
    return section;
  }

  async function renderCourseGrades(approved) {
    const container = document.getElementById("reportContent");
    const select = document.getElementById("yearFilter");
    try {
      const data = await AcadionApi.request("/api/me/notas");
      const notes = (Array.isArray(data) ? data : []).filter(note => !isFinalGrade(note));
      setYearOptions(select, notes);
      const render = () => {
        const year = Number(select?.value || CURRENT_YEAR);
        const visible = notes.filter(note =>
          Number(note.cicloLectivo) === year && isApprovedGrade(note) === approved);
        container.replaceChildren();
        if (!visible.length) {
          container.append(createEmpty(approved
            ? "Todavía no tenés evaluaciones de cursada aprobadas."
            : "No tenés evaluaciones de cursada desaprobadas."));
          return;
        }
        groupByCourseYear(visible).forEach((items, key) =>
          container.append(createGradeGroup(key.split("|")[1], items, false)));
      };
      select?.addEventListener("change", render);
      render();
    } catch (error) {
      console.error("No fue posible cargar las calificaciones de cursada.", error);
      setError(container);
    }
  }

  async function renderFinalGrades() {
    const container = document.getElementById("reportContent");
    const yearSelect = document.getElementById("yearFilter");
    const conditionSelect = document.getElementById("conditionFilter");
    try {
      const data = await AcadionApi.request("/api/me/notas");
      const notes = (Array.isArray(data) ? data : []).filter(isFinalGrade);
      setYearOptions(yearSelect, notes, true);
      const render = () => {
        const selectedYear = yearSelect?.value || "all";
        const selectedCondition = conditionSelect?.value || "all";
        const visible = notes.filter(note => {
          const matchesYear = selectedYear === "all" || Number(note.cicloLectivo) === Number(selectedYear);
          const approved = isApprovedGrade(note);
          const matchesCondition = selectedCondition === "all" ||
            (selectedCondition === "approved" ? approved : !approved);
          return matchesYear && matchesCondition;
        });
        container.replaceChildren();
        if (!visible.length) {
          container.append(createEmpty("No hay notas de finales para los filtros seleccionados."));
          return;
        }
        groupByCourseYear(visible).forEach((items, key) =>
          container.append(createGradeGroup(key.split("|")[1], items, true)));
      };
      yearSelect?.addEventListener("change", render);
      conditionSelect?.addEventListener("change", render);
      render();
    } catch (error) {
      console.error("No fue posible cargar las notas de finales.", error);
      setError(container);
    }
  }

  async function renderSubjectStatus(approved) {
    const container = document.getElementById("reportContent");
    const select = document.getElementById("yearFilter");
    try {
      const subjectsData = await AcadionApi.request("/api/me/resumen-academico");
      const subjects = Array.isArray(subjectsData) ? subjectsData : [];
      setYearOptions(select, subjects);
      const render = () => {
        const year = Number(select?.value || CURRENT_YEAR);
        const visible = subjects.filter(subject => Number(subject.cicloLectivo) === year && (approved ? isApproved(subject.estado) : isFailed(subject.estado)));
        container.replaceChildren();
        if (!visible.length) {
          container.append(createEmpty(approved ? "Todavía no tenés materias aprobadas." : "No tenés materias desaprobadas."));
          return;
        }
        const list = document.createElement("section");
        list.className = "record-list";
        visible.forEach(subject => {
          list.append(createRecord(subject, approved ? "Aprobada" : "Desaprobada"));
        });
        container.append(list);
      };
      select?.addEventListener("change", render);
      render();
    } catch (error) {
      console.error("No fue posible cargar el reporte de materias.", error);
      setError(container);
    }
  }

  async function renderProgress() {
    const container = document.getElementById("reportContent");
    try {
      const summaryData = await AcadionApi.request("/api/me/resumen-academico");
      const subjects = Array.isArray(summaryData) ? summaryData : [];
      const approved = subjects.filter(subject => isApproved(subject.estado) && subject.calificacionFinal !== null);
      const grades = approved.map(subject => Number(subject.calificacionFinal)).filter(Number.isFinite);
      const approvedSubjects = new Set(approved.map(subject => subject.materiaId));
      if (!grades.length) {
        container.replaceChildren(createEmpty("Todavía no hay calificaciones para calcular tu promedio y avance."));
        return;
      }
      const average = values => values.length ? (values.reduce((sum, value) => sum + value, 0) / values.length).toFixed(2) : "—";
      const totalSubjects = new Set(subjects.map(subject => subject.materiaId)).size;
      const progress = totalSubjects ? `${((approvedSubjects.size / totalSubjects) * 100).toFixed(1)}%` : "—";
      const values = [
        ["Promedio general", average(grades)],
        ["Promociones directas", approved.filter(subject => normalize(subject.viaAprobacion) === "promoción").length],
        ["Aprobadas por final", approved.filter(subject => normalize(subject.viaAprobacion) === "final").length],
        ["Porcentaje de avance", progress],
        ["Materias aprobadas", approvedSubjects.size]
      ];
      const list = document.createElement("section");
      list.className = "progress-list";
      values.forEach(([label, value]) => {
        const card = document.createElement("article");
        const copy = document.createElement("p"); copy.textContent = label;
        const strong = document.createElement("strong"); strong.textContent = String(value);
        card.append(copy, strong); list.append(card);
      });
      container.replaceChildren(list);
    } catch (error) {
      console.error("No fue posible calcular el promedio.", error);
      setError(container);
    }
  }

  function createAbsencePanel(subject, absences) {
    const panel = document.createElement("section");
    panel.className = "panel";
    const title = document.createElement("h2");
    title.textContent = subject;
    const wrapper = document.createElement("div"); wrapper.className = "table-wrapper";
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const headRow = document.createElement("tr");
    ["Fecha", "Estado", "Descripción", "Justificada"].forEach(label => {
      const header = document.createElement("th"); header.textContent = label; headRow.append(header);
    });
    head.append(headRow);
    const body = document.createElement("tbody");
    absences.forEach(absence => {
      const row = document.createElement("tr");
      row.append(
        createCell(formatDate(absence.fecha)),
        createCell(absence.tipo),
        createCell(absence.observaciones),
        createCell(normalize(absence.tipo) === "justificada" ? "Sí" : "No")
      );
      body.append(row);
    });
    table.append(head, body); wrapper.append(table); panel.append(title, wrapper);
    return panel;
  }

  async function renderAbsences() {
    const container = document.getElementById("reportContent");
    const select = document.getElementById("yearFilter");
    try {
      const data = await AcadionApi.request("/api/me/asistencias");
      const attendance = Array.isArray(data) ? data : [];
      setYearOptions(select, attendance);
      const render = () => {
        const year = Number(select?.value || CURRENT_YEAR);
        const absences = attendance.filter(item => Number(item.cicloLectivo) === year && ["ausente", "justificada"].includes(normalize(item.tipo)));
        container.replaceChildren();
        if (!absences.length) {
          container.append(createEmpty("No tenés inasistencias registradas."));
          return;
        }
        const grouped = absences.reduce((groups, item) => {
          const subject = item.materia || "Materia";
          if (!groups.has(subject)) groups.set(subject, []);
          groups.get(subject).push(item);
          return groups;
        }, new Map());
        grouped.forEach((items, subject) => container.append(createAbsencePanel(subject, items)));
      };
      select?.addEventListener("change", render);
      render();
    } catch (error) {
      console.error("No fue posible cargar las inasistencias.", error);
      setError(container);
    }
  }

  document.addEventListener("DOMContentLoaded", () => {
    Acadion.iniciarPantalla();
    document.querySelectorAll("[data-current-year]").forEach(element => {
      element.textContent = String(CURRENT_YEAR);
    });
    const page = document.body.dataset.page;
    if (page === "MateriasEnCurso") renderCurrentSubjects();
    else if (page === "MateriasAprobadas") renderCourseGrades(true);
    else if (page === "MateriasDesaprobadas") renderCourseGrades(false);
    else if (page === "NotasFinales") renderFinalGrades();
    else if (page === "PromedioYAvance") renderProgress();
    else if (page === "Inasistencias") renderAbsences();
  });
})();
