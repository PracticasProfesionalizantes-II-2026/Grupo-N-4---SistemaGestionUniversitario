document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const session = AcadionApi.getSession();
  if (!session?.usuarioId) {
    window.location.replace("Login.html");
    return;
  }

  const subjectsList = document.getElementById("subjectsList");
  const loadingState = document.getElementById("loadingState");
  const emptyState = document.getElementById("emptyState");
  const emptyMessage = document.getElementById("emptyMessage");
  const subjectsCount = document.getElementById("subjectsCount");
  const alertModal = document.getElementById("alertModal");
  const alertBox = document.getElementById("alertBox");
  const alertIcon = document.getElementById("alertIcon");
  const alertTitle = document.getElementById("alertTitle");
  const alertMessage = document.getElementById("alertMessage");

  function showAlert(title, message, success = false) {
    alertTitle.textContent = title;
    alertMessage.textContent = message;
    alertIcon.textContent = success ? "✓" : "!";
    alertBox.classList.toggle("is-success", success);
    alertModal.classList.remove("is-hidden");
  }

  function hideAlert() { alertModal.classList.add("is-hidden"); }

  function formatSchedules(schedules) {
    if (!Array.isArray(schedules) || schedules.length === 0) return "Horario a confirmar";
    return schedules.map(item => `${item.diaSemana} ${String(item.horaInicio).slice(0, 5)}–${String(item.horaFin).slice(0, 5)}`).join(" · ");
  }

  function formatDate(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "—" : date.toLocaleDateString("es-AR");
  }

  async function loadEnrollmentWindow() {
    const element = document.getElementById("enrollmentWindow");
    try {
      const period = await AcadionApi.request("/api/me/periodo-inscripcion-materias");
      element.classList.toggle("is-open", Boolean(period.abierta));
      if (!period.configurado) {
        element.textContent = "Secretaría todavía no configuró el período de inscripción.";
      } else {
        element.textContent = `${period.abierta ? "Inscripción abierta" : "Inscripción cerrada"} · ${formatDate(period.fechaInicio)} al ${formatDate(period.fechaFin)}`;
      }
    } catch (error) { element.textContent = error.message || "No se pudo consultar el período de inscripción."; }
  }

  function createSubjectCard(subject) {
    const card = document.createElement("article");
    card.className = "subject-card";
    const content = document.createElement("div"); content.className = "subject-main";
    const title = document.createElement("h3"); title.textContent = subject.nombre;
    const details = document.createElement("p"); details.className = "subject-details";
    const teacher = document.createElement("span"); teacher.className = "subject-code";
    teacher.textContent = `Docente: ${subject.docente || "A confirmar"}`;
    details.append(teacher, document.createTextNode(` · ${subject.modalidad} · ${formatSchedules(subject.horarios)}`));
    content.append(title, details);

    let commissionSelect = null;
    const commissions = Array.isArray(subject.comisiones) ? subject.comisiones : [];
    if (commissions.length) {
      const commissionField = document.createElement("label");
      commissionField.className = "commission-field";
      commissionField.append(document.createTextNode("Comisión"));
      commissionSelect = document.createElement("select");
      commissions.forEach(commission => {
        const option = document.createElement("option");
        option.value = commission.comisionId;
        option.textContent = `${commission.nombre} · ${commission.turno} · ${commission.vacantes} vacantes${commission.cupoCompleto ? " (lista de espera)" : ""}`;
        commissionSelect.append(option);
      });
      const commissionDetail = document.createElement("small");
      const renderCommission = () => {
        const selected = commissions.find(item => item.comisionId === Number(commissionSelect.value));
        commissionDetail.textContent = selected
          ? `${selected.docente} · ${formatSchedules(selected.horarios)}`
          : "Seleccioná una comisión";
      };
      commissionSelect.addEventListener("change", renderCommission);
      commissionField.append(commissionSelect, commissionDetail);
      content.append(commissionField);
      renderCommission();
    }

    const pending = Array.isArray(subject.correlativasPendientes) ? subject.correlativasPendientes : [];
    if (pending.length) {
      const requirements = document.createElement("p"); requirements.className = "subject-requirements";
      requirements.textContent = `Correlativas pendientes: ${pending.join(", ")}`;
      content.append(requirements);
    }

    const button = document.createElement("button"); button.type = "button"; button.className = "enroll-button";
    if (subject.habilitada) {
      button.textContent = "Inscribirse";
      button.addEventListener("click", () => enrollSubject(subject, button, card, commissionSelect));
    } else {
      button.textContent = "Ver requisitos";
      button.classList.add("requirements");
      button.addEventListener("click", () => showAlert(
        "Tenés correlativas pendientes",
        `Para inscribirte a ${subject.nombre} primero debés regularizar: ${pending.join(", ")}.`
      ));
    }
    card.append(content, button);
    return card;
  }

  function showEmpty(message = "Actualmente no tenés materias habilitadas para inscripción.") {
    loadingState.classList.add("is-hidden");
    subjectsList.replaceChildren();
    subjectsCount.textContent = "0 materias disponibles";
    emptyMessage.textContent = message;
    emptyState.classList.remove("is-hidden");
  }

  async function loadSubjects() {
    loadingState.classList.remove("is-hidden");
    emptyState.classList.add("is-hidden");
    subjectsList.replaceChildren();
    try {
      const subjects = await AcadionApi.request("/api/me/materias-disponibles");
      loadingState.classList.add("is-hidden");
      if (!Array.isArray(subjects) || subjects.length === 0) { showEmpty(); return; }
      subjectsCount.textContent = `${subjects.length} materia${subjects.length === 1 ? "" : "s"} disponible${subjects.length === 1 ? "" : "s"}`;
      subjects.forEach(subject => subjectsList.append(createSubjectCard(subject)));
    } catch (error) {
      showEmpty(error.message || "No hay materias disponibles para mostrar por el momento.");
    }
  }

  async function enrollSubject(subject, button, card, commissionSelect) {
    button.disabled = true;
    button.textContent = "Inscribiendo...";
    try {
      const response = await AcadionApi.request("/api/me/inscripciones", {
        method: "POST",
        body: JSON.stringify({
          materiaId: subject.materiaId,
          comisionId: commissionSelect ? Number(commissionSelect.value) : null
        })
      });
      card.remove();
      const remaining = subjectsList.children.length;
      subjectsCount.textContent = `${remaining} materia${remaining === 1 ? "" : "s"} disponible${remaining === 1 ? "" : "s"}`;
      if (remaining === 0) showEmpty();
      showAlert(response?.enListaEspera ? "Solicitud registrada" : "Inscripción exitosa",
        response?.mensaje || `Te inscribiste correctamente a ${subject.nombre}.`, true);
    } catch (error) {
      button.disabled = false;
      button.textContent = "Inscribirse";
      showAlert("No fue posible realizar la inscripción", error.message || "Revisá los requisitos de la materia e intentá nuevamente.");
    }
  }

  document.getElementById("closeAlert").addEventListener("click", hideAlert);
  document.getElementById("acceptAlert").addEventListener("click", hideAlert);
  alertModal.addEventListener("click", event => { if (event.target === alertModal) hideAlert(); });
  document.addEventListener("keydown", event => { if (event.key === "Escape") hideAlert(); });
  loadEnrollmentWindow();
  loadSubjects();
});
