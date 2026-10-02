document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const session = AcadionApi.getSession();
  if (!session?.usuarioId) {
    window.location.replace("Login.html");
    return;
  }

  const subjectsList = document.getElementById("subjectsList");
  const detailPanel = document.getElementById("detailPanel");
  const masterDetail = document.getElementById("masterDetail");
  const globalState = document.getElementById("globalState");
  const statusMessage = document.getElementById("statusMessage");
  let exams = [];
  let activeFilter = "all";

  function setStatus(message = "", type = "success") {
    statusMessage.textContent = message;
    statusMessage.className = `status-message ${message ? type : "is-hidden"}`;
  }

  function showGlobalState(title, message, icon = "○") {
    globalState.replaceChildren();
    const iconElement = document.createElement("div"); iconElement.className = "state-icon"; iconElement.textContent = icon;
    const heading = document.createElement("h2"); heading.textContent = title;
    const copy = document.createElement("p"); copy.textContent = message;
    globalState.append(iconElement, heading, copy);
    globalState.classList.remove("is-hidden");
    masterDetail.classList.add("is-hidden");
  }

  function hideGlobalState() {
    globalState.classList.add("is-hidden");
    masterDetail.classList.remove("is-hidden");
  }

  function formatDate(value, includeTime = false) {
    if (!value) return "Fecha a confirmar";
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("es-AR", {
      day: "2-digit", month: "2-digit", year: "numeric",
      ...(includeTime ? { hour: "2-digit", minute: "2-digit" } : {})
    }).format(date);
  }

  function renderDetail(exam) {
    detailPanel.replaceChildren();
    const card = document.createElement("article"); card.className = "detail-card";
    const eyebrow = document.createElement("p"); eyebrow.className = "eyebrow"; eyebrow.textContent = "DETALLE DE LA MESA";
    const title = document.createElement("h2"); title.textContent = exam.materia || "Examen";
    const copy = document.createElement("p"); copy.textContent = `${exam.tipoExamen || "Examen"} correspondiente al ciclo lectivo ${exam.cicloLectivo || "actual"}.`;
    const grid = document.createElement("div"); grid.className = "detail-grid";
    [["Tipo", exam.tipoExamen || "A confirmar"], ["Turno", exam.turnoFinal ? `${exam.turnoFinal} · ${exam.numeroLlamado}.º llamado` : "A confirmar"], ["Fecha", formatDate(exam.fecha)], ["Materia", exam.materia || "A confirmar"], ["Ciclo lectivo", exam.cicloLectivo || "A confirmar"]]
      .forEach(([label, value]) => {
        const item = document.createElement("div"); item.className = "detail-item";
        const itemLabel = document.createElement("span"); itemLabel.className = "detail-label"; itemLabel.textContent = label;
        const itemValue = document.createElement("strong"); itemValue.className = "detail-value"; itemValue.textContent = value;
        item.append(itemLabel, itemValue); grid.append(item);
      });
    const actions = document.createElement("div"); actions.className = "detail-actions";
    const button = document.createElement("button"); button.type = "button"; button.className = "primary-button"; button.textContent = "Inscribirme";
    if (exam.inscripto) {
      if (exam.puedeDarseDeBaja) {
        button.className = "secondary-button";
        button.textContent = "Darme de baja";
        button.addEventListener("click", () => withdrawExam(exam, button));
        const deadline = document.createElement("p");
        deadline.className = "withdrawal-deadline";
        deadline.textContent = `Podés darte de baja hasta el ${formatDate(exam.limiteBaja, true)}.`;
        actions.append(deadline);
      } else {
        button.textContent = "Inscripto";
        button.disabled = true;
        const deadline = document.createElement("p");
        deadline.className = "withdrawal-deadline is-closed";
        deadline.textContent = "El plazo para darte de baja finalizó (24 horas antes del examen).";
        actions.append(deadline);
      }
    } else if (exam.habilitado === false) {
      button.textContent = "No disponible";
      button.disabled = true;
      const warning = document.createElement("p");
      warning.className = "status-message danger";
      warning.textContent = exam.motivoBloqueo || "No estás habilitado para inscribirte a este final.";
      actions.append(warning);
    } else {
      button.addEventListener("click", () => enrollExam(exam, button));
    }
    actions.append(button); card.append(eyebrow, title, copy, grid, actions); detailPanel.append(card);
  }

  async function enrollExam(exam, button) {
    button.disabled = true;
    button.textContent = "Inscribiendo…";
    try {
      await AcadionApi.request(`/api/me/examenes/${exam.idExamen}/inscripcion`, { method: "POST" });
      exam.inscripto = true;
      button.textContent = "Ya estás inscripto";
      setStatus("La inscripción al examen se realizó correctamente.", "success");
      renderExams();
    } catch (error) {
      button.disabled = false;
      button.textContent = "Inscribirme";
      setStatus(error.message || "No fue posible realizar la inscripción.", "danger");
    }
  }

  async function withdrawExam(exam, button) {
    if (!window.confirm(`¿Querés darte de baja del final de ${exam.materia}?`)) return;
    button.disabled = true;
    button.textContent = "Procesando…";
    try {
      await AcadionApi.request(`/api/me/examenes/${exam.idExamen}/inscripcion`, { method: "DELETE" });
      exam.inscripto = false;
      exam.puedeDarseDeBaja = false;
      setStatus("Te diste de baja del examen correctamente.", "success");
      renderDetail(exam);
      renderExams();
    } catch (error) {
      button.disabled = false;
      button.textContent = "Darme de baja";
      setStatus(error.message || "No fue posible realizar la baja.", "danger");
    }
  }

  function renderExams() {
    const query = document.getElementById("searchInput").value.trim().toLowerCase();
    const visible = exams.filter(exam => {
      const matchesSearch = `${exam.materia} ${exam.tipoExamen}`.toLowerCase().includes(query);
      const matchesFilter = activeFilter === "all" || exam.inscripto === true;
      return matchesSearch && matchesFilter;
    });
    subjectsList.replaceChildren();
    if (!visible.length) {
      const empty = document.createElement("p"); empty.className = "subject-empty";
      empty.textContent = activeFilter === "enrolled" ? "Todavía no tenés inscripciones a exámenes." : "No hay exámenes que coincidan con la búsqueda.";
      subjectsList.append(empty); return;
    }
    visible.forEach(exam => {
      const button = document.createElement("button"); button.type = "button"; button.className = "subject-button";
      const title = document.createElement("strong"); title.textContent = exam.materia || "Examen";
      const turn = exam.turnoFinal ? `${exam.turnoFinal}, ${exam.numeroLlamado}.º llamado · ` : "";
      const meta = document.createElement("small"); meta.textContent = `${turn}${exam.tipoExamen || "Examen"} · ${formatDate(exam.fecha)}`;
      button.append(title, meta);
      button.addEventListener("click", () => {
        document.querySelectorAll(".subject-button").forEach(item => item.classList.remove("is-selected"));
        button.classList.add("is-selected"); renderDetail(exam); setStatus();
      });
      subjectsList.append(button);
    });
  }

  async function loadExams() {
    setStatus("Cargando exámenes...");
    hideGlobalState();
    try {
      const data = await AcadionApi.request("/api/me/examenes");
      exams = Array.isArray(data) ? data : [];
      setStatus();
      if (!exams.length) {
        showGlobalState("No hay exámenes disponibles", "Actualmente no hay mesas de examen habilitadas para tus materias.");
        return;
      }
      renderExams();
    } catch {
      setStatus();
      showGlobalState("No pudimos cargar los exámenes", "No fue posible conectarse con el servidor. Intentá nuevamente más tarde.", "!");
    }
  }

  document.getElementById("searchInput").addEventListener("input", renderExams);
  document.querySelectorAll(".toggle").forEach(button => button.addEventListener("click", () => {
    document.querySelectorAll(".toggle").forEach(item => item.classList.remove("is-active"));
    button.classList.add("is-active"); activeFilter = button.dataset.filter; renderExams();
  }));
  loadExams();
});
