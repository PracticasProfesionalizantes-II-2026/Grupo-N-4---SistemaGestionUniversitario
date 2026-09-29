(() => {
  const CURRENT_YEAR = new Date().getFullYear();

  const normalize = value => String(value || "").trim().toLowerCase();
  const isApproved = value => ["aprobada", "aprobado", "aprobó", "promocionada", "promocionado", "promocionó"].includes(normalize(value));
  const isFailed = value => ["desaprobada", "desaprobado", "libre"].includes(normalize(value));
  const isCurrent = value => !isApproved(value) && !isFailed(value);

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

  function setYearOptions(select, items) {
    if (!select) return;
    const years = new Set([CURRENT_YEAR]);
    items.forEach(item => {
      const year = Number(item.cicloLectivo);
      if (year > 0) years.add(year);
    });
    select.replaceChildren(...[...years]
      .sort((a, b) => b - a)
      .map(year => {
        const option = document.createElement("option");
        option.value = String(year);
        option.textContent = String(year);
        return option;
      }));
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
        createCell(exam.tipoExamen),
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

  function createCurrentSubject(subject, exams, notes) {
    const article = document.createElement("article");
    article.className = "course-report";
    const header = document.createElement("header");
    const title = document.createElement("h2");
    title.textContent = subject.nombre || "Materia";
    const status = document.createElement("span");
    const start = subject.inicioDictado
      ? `Inicio de dictado ${formatDate(subject.inicioDictado)}`
      : "Inicio de dictado pendiente";
    const modality = subject.modalidad ? ` · ${subject.modalidad}` : "";
    status.textContent = `En curso — ${start}${modality}`;
    header.append(title, status);
    article.append(header);
    if (exams.length) article.append(createEvaluationTable(exams, notes, subject.docente));
    return article;
  }

  async function renderCurrentSubjects() {
    const container = document.getElementById("reportContent");
    const select = document.getElementById("yearFilter");
    try {
      const [subjectsData, examsData, notesData, summaryData] = await Promise.all([
        AcadionApi.request("/api/me/materias"),
        AcadionApi.request("/api/me/evaluaciones"),
        AcadionApi.request("/api/me/notas"),
        AcadionApi.request("/api/me/resumen-academico")
      ]);
      const subjects = Array.isArray(subjectsData) ? subjectsData : [];
      const exams = Array.isArray(examsData) ? examsData : [];
      const notes = Array.isArray(notesData) ? notesData : [];
      const summaries = Array.isArray(summaryData) ? summaryData : [];
      setYearOptions(select, subjects);

      const render = () => {
        const year = Number(select?.value || CURRENT_YEAR);
        const current = subjects.filter(subject => {
          const summary = summaries.find(item => Number(item.inscripcionId) === Number(subject.inscripcionId));
          return Number(subject.cicloLectivo) === year && !isApproved(summary?.estado || subject.estado);
        });
        container.replaceChildren();
        if (!current.length) {
          container.append(createEmpty("Todavía no tenés materias en curso."));
          return;
        }
        current.forEach(subject => {
          const subjectExams = exams.filter(exam => Number(exam.idMateria) === Number(subject.materiaId) && Number(exam.cicloLectivo) === year);
          const examIds = new Set(subjectExams.map(exam => Number(exam.idExamen)));
          const subjectNotes = notes.filter(note => examIds.has(Number(note.idExamen)));
          const gradedExams = subjectExams.filter(exam => subjectNotes.some(note => Number(note.idExamen) === Number(exam.idExamen)));
          container.append(createCurrentSubject(subject, gradedExams, subjectNotes));
        });
      };
      select?.addEventListener("change", render);
      render();
    } catch (error) {
      console.error("No fue posible cargar las materias en curso.", error);
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
    else if (page === "MateriasAprobadas") renderSubjectStatus(true);
    else if (page === "MateriasDesaprobadas") renderSubjectStatus(false);
    else if (page === "PromedioYAvance") renderProgress();
    else if (page === "Inasistencias") renderAbsences();
  });
})();
