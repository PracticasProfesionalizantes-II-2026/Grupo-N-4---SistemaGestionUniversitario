document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const form = document.getElementById("careerForm");
  const body = document.getElementById("careersBody");
  const message = document.getElementById("careerMessage");
  const cancel = document.getElementById("cancelEdit");
  const save = document.getElementById("saveCareer");
  const editPanel = document.getElementById("careerEditPanel");
  const adminPanel = document.getElementById("careerAdminPanel");
  const adminTitle = document.getElementById("careerAdminTitle");
  const adminSummary = document.getElementById("careerAdminSummary");
  const adminCycle = document.getElementById("careerAdminCycle");
  const subjectsContainer = document.getElementById("careerSubjects");
  const plansContainer = document.getElementById("studyPlans");
  const planForm = document.getElementById("studyPlanForm");
  const planMessage = document.getElementById("studyPlanMessage");
  const periodForm = document.getElementById("careerPeriodForm");
  const periodCareer = document.getElementById("careerPeriodCareer");
  const periodCycle = document.getElementById("careerPeriodCycle");
  const periodStatus = document.getElementById("careerPeriodStatus");
  const periodMessage = document.getElementById("careerPeriodMessage");
  let careers = [];
  let periods = [];
  let editingId = null;
  let managedCareerId = null;

  adminCycle.value = new Date().getFullYear();
  periodCycle.value = new Date().getFullYear();

  function option(value, text) {
    const item = document.createElement("option");
    item.value = value;
    item.textContent = text;
    return item;
  }

  function showPeriodMessage(text, success = false) {
    periodMessage.textContent = text;
    periodMessage.className = `status-message ${success ? "success" : "error"}`;
  }

  function renderPeriodCareerOptions() {
    const previous = periodCareer.value;
    periodCareer.replaceChildren(option("", "Seleccionar carrera"));
    careers.forEach(career => periodCareer.append(option(
      career.idCarrera, `${career.nombre} · ${career.planEstudios}`)));
    if ([...periodCareer.options].some(item => item.value === previous))
      periodCareer.value = previous;
  }

  function renderPeriod() {
    const period = periods.find(item => item.idCarrera === Number(periodCareer.value));
    if (!period?.configurado) {
      periodForm.fechaInicio.value = "";
      periodForm.fechaFin.value = "";
      periodStatus.textContent = "Sin configurar";
      periodStatus.className = "badge inactive";
      return;
    }
    periodForm.fechaInicio.value = String(period.fechaInicio).slice(0, 10);
    periodForm.fechaFin.value = String(period.fechaFin).slice(0, 10);
    periodStatus.textContent = period.abierta ? "Inscripción abierta" : "Inscripción cerrada";
    periodStatus.className = `badge ${period.abierta ? "active" : "inactive"}`;
  }

  async function loadPeriods() {
    periods = await AcadionApi.request(
      `/api/gestion-academica/periodos-inscripcion-carreras?cicloLectivo=${Number(periodCycle.value)}`);
    renderPeriod();
  }

  function showMessage(text, success = false) {
    message.textContent = text;
    message.className = `status-message ${success ? "success" : "error"}`;
  }

  function resetForm() {
    editingId = null;
    form.reset();
    message.textContent = "";
    editPanel.hidden = true;
  }

  function render() {
    body.replaceChildren();
    if (!careers.length) {
      body.innerHTML = '<tr><td colspan="7" class="empty-state">Todavía no hay carreras registradas.</td></tr>';
      return;
    }
    careers.forEach(career => {
      const row = document.createElement("tr");
      [career.nombre, career.tipo, career.planEstudios, `${career.duracionAnios} años`].forEach(value => {
        const cell = row.insertCell(); cell.textContent = value;
      });
      const capacityCell = row.insertCell();
      const capacity = document.createElement("span");
      capacity.className = "capacity";
      const percent = Math.min(100, Math.round(career.estudiantesInscriptos * 100 / career.capacidadMaximaEstudiantes));
      const capacityBar = document.createElement("span"); capacityBar.className = "capacity-bar";
      const capacityFill = document.createElement("span"); capacityFill.style.width = `${percent}%`; capacityBar.append(capacityFill);
      const capacityText = document.createElement("strong"); capacityText.textContent = `${career.estudiantesInscriptos}/${career.capacidadMaximaEstudiantes}`;
      capacity.append(capacityBar, capacityText);
      capacityCell.append(capacity);
      const statusCell = row.insertCell();
      const status = document.createElement("span");
      status.className = `badge ${career.activa ? "active" : "inactive"}`;
      status.textContent = career.activa ? "Activa" : "Inactiva";
      statusCell.append(status);
      const actionsCell = row.insertCell();
      const actions = document.createElement("div");
      actions.className = "career-actions";
      const edit = document.createElement("button");
      edit.className = "secondary-button compact-button";
      edit.type = "button";
      edit.textContent = "Editar";
      edit.addEventListener("click", () => beginEdit(career));
      actions.append(edit);
      const manage = document.createElement("button");
      manage.className = "primary-button compact-button";
      manage.type = "button";
      manage.textContent = "Administrar";
      manage.addEventListener("click", () => openCareerManagement(career));
      actions.append(manage);
      actionsCell.append(actions);
      body.append(row);
    });
  }

  function formatDate(value) {
    if (!value) return "—";
    return new Intl.DateTimeFormat("es-AR", {
      day: "2-digit", month: "2-digit", year: "numeric"
    }).format(new Date(value));
  }

  function formatTime(value) {
    if (!value) return "";
    return String(value).slice(0, 5);
  }

  function appendLabelValue(parent, label, value) {
    const item = document.createElement("span");
    const strong = document.createElement("strong");
    strong.textContent = `${label}: `;
    item.append(strong, document.createTextNode(value));
    parent.append(item);
  }

  function renderPlans(plans) {
    plansContainer.replaceChildren();
    if (!plans.length) {
      const empty = document.createElement("p");
      empty.className = "empty-state";
      empty.textContent = "La carrera todavía no tiene versiones de plan registradas.";
      plansContainer.append(empty);
      return;
    }
    const list = document.createElement("div");
    list.className = "study-plan-list";
    plans.forEach(plan => {
      const card = document.createElement("article");
      card.className = `study-plan-card${plan.activo ? " active" : ""}`;
      const heading = document.createElement("div");
      const title = document.createElement("strong");
      title.textContent = plan.codigo;
      const badge = document.createElement("span");
      badge.className = `badge ${plan.activo ? "active" : "inactive"}`;
      badge.textContent = plan.activo ? "Vigente" : "Histórico";
      heading.append(title, badge);
      const detail = document.createElement("p");
      detail.textContent = `Desde ${plan.vigenteDesde}${plan.vigenteHasta ? ` hasta ${plan.vigenteHasta}` : ""} · ${plan.cantidadMaterias} materias · ${plan.cantidadEstudiantes} estudiantes`;
      card.append(heading, detail);
      list.append(card);
    });
    plansContainer.append(list);
  }

  async function loadPlans() {
    if (!managedCareerId) return;
    try {
      const plans = await AcadionApi.request(`/api/planes-estudio/carrera/${managedCareerId}`);
      renderPlans(plans);
    } catch (error) {
      plansContainer.replaceChildren();
      const failure = document.createElement("p");
      failure.className = "empty-state error";
      failure.textContent = error.message;
      plansContainer.append(failure);
    }
  }

  function renderStudents(parent, students) {
    if (!students.length) {
      const empty = document.createElement("p");
      empty.className = "subject-empty";
      empty.textContent = "No hay estudiantes inscriptos en esta materia para el ciclo seleccionado.";
      parent.append(empty);
      return;
    }

    const wrapper = document.createElement("div");
    wrapper.className = "table-wrapper students-table";
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const headRow = document.createElement("tr");
    ["Estudiante", "Legajo", "Estado", "Fecha de inscripción"].forEach(label => {
      const th = document.createElement("th");
      th.textContent = label;
      headRow.append(th);
    });
    head.append(headRow);
    const tableBody = document.createElement("tbody");
    students.forEach(student => {
      const row = document.createElement("tr");
      [
        Acadion.formatearNombre(`${student.apellido}, ${student.nombre}`),
        student.legajo || "Sin legajo",
        student.estado,
        formatDate(student.fechaInscripcion)
      ].forEach(value => {
        const cell = document.createElement("td");
        cell.textContent = value;
        row.append(cell);
      });
      tableBody.append(row);
    });
    table.append(head, tableBody);
    wrapper.append(table);
    parent.append(wrapper);
  }

  function renderCareerDetail(detail) {
    adminTitle.textContent = detail.nombre;
    adminSummary.textContent = `${detail.planEstudios} · Ciclo ${detail.cicloLectivo} · ${detail.totalMaterias} materias · ${detail.totalEstudiantes} estudiantes`;
    subjectsContainer.replaceChildren();

    if (!detail.anios.length || !detail.totalMaterias) {
      const empty = document.createElement("p");
      empty.className = "empty-state career-empty";
      empty.textContent = `No hay registros académicos para el ciclo ${detail.cicloLectivo}.`;
      subjectsContainer.append(empty);
      return;
    }

    detail.anios.forEach(year => {
      const yearSection = document.createElement("section");
      yearSection.className = "career-year";
      const yearHeader = document.createElement("div");
      yearHeader.className = "career-year-heading";
      const title = document.createElement("h3");
      title.textContent = year.nombreAnio || `${year.numeroAnio}.º año`;
      const count = document.createElement("span");
      count.textContent = `${year.materias.length} ${year.materias.length === 1 ? "materia" : "materias"}`;
      yearHeader.append(title, count);
      yearSection.append(yearHeader);

      year.materias.forEach(subject => {
        const card = document.createElement("details");
        card.className = "managed-subject";
        const summary = document.createElement("summary");
        const identity = document.createElement("span");
        identity.className = "subject-identity";
        const name = document.createElement("strong");
        name.textContent = subject.nombre;
        const professorNames = subject.profesores.map(professor => professor.nombre);
        const professor = document.createElement("small");
        professor.textContent = professorNames.length
          ? `Profesor: ${professorNames.join(", ")}`
          : "Profesor: a confirmar";
        identity.append(name, professor);
        const enrolled = document.createElement("span");
        enrolled.className = "enrolled-badge";
        enrolled.textContent = `${subject.estudiantes.length} ${subject.estudiantes.length === 1 ? "inscripto" : "inscriptos"}`;
        summary.append(identity, enrolled);
        card.append(summary);

        const content = document.createElement("div");
        content.className = "subject-content";
        const metadata = document.createElement("div");
        metadata.className = "subject-metadata";
        appendLabelValue(metadata, "Modalidad", subject.modalidad || "Sin definir");
        appendLabelValue(metadata, "Estado", subject.estado || "Sin definir");
        const schedules = subject.horarios.length
          ? subject.horarios.map(schedule => `${schedule.diaSemana} ${formatTime(schedule.horaInicio)} a ${formatTime(schedule.horaFin)}`).join(" · ")
          : "Sin horarios cargados";
        appendLabelValue(metadata, "Horarios", schedules);
        const prerequisites = subject.correlativas.length
          ? subject.correlativas.map(item => item.nombre).join(", ")
          : "Sin correlatividades";
        appendLabelValue(metadata, "Correlatividades", prerequisites);
        content.append(metadata);

        const studentTitle = document.createElement("h4");
        studentTitle.textContent = "Estudiantes inscriptos";
        content.append(studentTitle);
        renderStudents(content, subject.estudiantes);
        card.append(content);
        yearSection.append(card);
      });
      subjectsContainer.append(yearSection);
    });
  }

  async function loadCareerDetailFromExistingRoutes() {
    const cycle = Number(adminCycle.value) || new Date().getFullYear();
    const career = careers.find(item => item.idCarrera === managedCareerId);
    const [subjectsResult, schedulesResult, enrollmentsResult, usersResult] = await Promise.allSettled([
      AcadionApi.request(`/materias/?carreraId=${managedCareerId}`),
      AcadionApi.request("/horarios/"),
      AcadionApi.request("/inscripciones/"),
      AcadionApi.request("/api/gestion/usuarios/")
    ]);

    if (subjectsResult.status === "rejected") throw subjectsResult.reason;

    const subjects = subjectsResult.value || [];
    const schedules = schedulesResult.status === "fulfilled" ? schedulesResult.value : [];
    const enrollments = enrollmentsResult.status === "fulfilled" ? enrollmentsResult.value : [];
    const users = usersResult.status === "fulfilled" ? usersResult.value : [];
    const usersById = new Map(users.map(user => [user.id, user]));
    const yearsById = new Map((career?.anios || []).map(year => [year.idAnio, year]));

    const years = [...new Set(subjects.map(subject => subject.idAnio))]
      .map(yearId => {
        const year = yearsById.get(yearId);
        const yearSubjects = subjects
          .filter(subject => subject.idAnio === yearId)
          .sort((a, b) => a.nombre.localeCompare(b.nombre, "es"))
          .map(subject => {
            const currentEnrollments = enrollments.filter(item =>
              item.idMateria === subject.idMateria &&
              item.cicloLectivo === cycle &&
              String(item.estado).toLowerCase() !== "cancelada");
            const professorIds = [...new Set(currentEnrollments
              .map(item => item.idDocente)
              .filter(Boolean))];

            return {
              idMateria: subject.idMateria,
              nombre: subject.nombre,
              modalidad: subject.modalidad,
              estado: subject.estado,
              horarios: schedules.filter(schedule => schedule.idMateria === subject.idMateria),
              correlativas: subject.correlativas || [],
              profesores: professorIds.map(id => {
                const user = usersById.get(id);
                return {
                  idDocente: id,
                  nombre: user ? Acadion.formatearNombre(`${user.nombre} ${user.apellido}`) : `Docente #${id}`
                };
              }),
              estudiantes: currentEnrollments.map(enrollment => {
                const user = usersById.get(enrollment.idEstudiante);
                return {
                  idEstudiante: enrollment.idEstudiante,
                  nombre: user?.nombre || "Estudiante",
                  apellido: user?.apellido || `#${enrollment.idEstudiante}`,
                  legajo: user?.legajo || "",
                  estado: enrollment.estado,
                  fechaInscripcion: enrollment.fechaInscripcion
                };
              })
            };
          });

        return {
          idAnio: yearId,
          numeroAnio: year?.numeroAnio || yearSubjects[0]?.numeroAnio || 0,
          nombreAnio: year?.nombreAnio || `${yearSubjects[0]?.numeroAnio || ""}.º año`,
          materias: yearSubjects
        };
      })
      .sort((a, b) => a.numeroAnio - b.numeroAnio);

    const distinctStudents = new Set(enrollments
      .filter(item => subjects.some(subject => subject.idMateria === item.idMateria) &&
        item.cicloLectivo === cycle && String(item.estado).toLowerCase() !== "cancelada")
      .map(item => item.idEstudiante));

    return {
      idCarrera: managedCareerId,
      nombre: career?.nombre || "Carrera",
      planEstudios: career?.planEstudios || "Plan sin definir",
      cicloLectivo: cycle,
      totalMaterias: subjects.length,
      totalEstudiantes: distinctStudents.size,
      anios: years
    };
  }

  async function loadCareerDetail() {
    if (!managedCareerId) return;
    subjectsContainer.replaceChildren();
    const loading = document.createElement("p");
    loading.className = "empty-state career-empty";
    loading.textContent = "Cargando materias, profesores y estudiantes...";
    subjectsContainer.append(loading);
    try {
      const cycle = Number(adminCycle.value) || new Date().getFullYear();
      let detail;
      try {
        detail = await AcadionApi.request(`/api/gestion-academica/carreras/${managedCareerId}/detalle?cicloLectivo=${cycle}`);
      } catch {
        detail = await loadCareerDetailFromExistingRoutes();
      }
      renderCareerDetail(detail);
    } catch (error) {
      subjectsContainer.replaceChildren();
      const failure = document.createElement("p");
      failure.className = "empty-state career-empty error";
      failure.textContent = error.message;
      subjectsContainer.append(failure);
    }
  }

  function openCareerManagement(career) {
    managedCareerId = career.idCarrera;
    adminTitle.textContent = career.nombre;
    adminSummary.textContent = "Consultando la información académica...";
    adminPanel.hidden = false;
    periodCareer.value = String(career.idCarrera);
    renderPeriod();
    Promise.all([loadPlans(), loadCareerDetail()]);
    adminPanel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  function beginEdit(career) {
    editingId = career.idCarrera;
    form.nombre.value = career.nombre;
    form.tipo.value = career.tipo;
    form.planEstudios.value = career.planEstudios;
    form.duracionAnios.value = career.duracionAnios;
    form.capacidadMaximaEstudiantes.value = career.capacidadMaximaEstudiantes;
    form.activa.value = String(career.activa);
    editPanel.hidden = false;
    document.getElementById("formTitle").textContent = career.nombre;
    editPanel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  async function load() {
    try {
      careers = await AcadionApi.request("/carreras/");
      render();
      renderPeriodCareerOptions();
      await loadPeriods();
    }
    catch (error) {
      body.replaceChildren();
      const row = body.insertRow(); const cell = row.insertCell();
      cell.colSpan = 7; cell.className = "empty-state"; cell.textContent = error.message;
    }
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const payload = {
      nombre: form.nombre.value.trim(), tipo: form.tipo.value,
      planEstudios: form.planEstudios.value.trim(),
      duracionAnios: Number(form.duracionAnios.value),
      capacidadMaximaEstudiantes: Number(form.capacidadMaximaEstudiantes.value),
      activa: form.activa.value === "true"
    };
    try {
      if (!editingId) return;
      await AcadionApi.request(`/carreras/${editingId}`, {
        method: "PUT", body: JSON.stringify(payload)
      });
      showMessage("Carrera actualizada correctamente.", true);
      await load();
    } catch (error) { showMessage(error.message); }
  });

  periodForm.addEventListener("submit", async event => {
    event.preventDefault();
    const start = new Date(`${periodForm.fechaInicio.value}T00:00:00`);
    const end = new Date(`${periodForm.fechaFin.value}T23:59:59`);
    try {
      await AcadionApi.request("/api/gestion-academica/periodos-inscripcion-carreras", {
        method: "PUT",
        body: JSON.stringify({
          carreraId: Number(periodCareer.value),
          cicloLectivo: Number(periodCycle.value),
          fechaInicio: start.toISOString(),
          fechaFin: end.toISOString()
        })
      });
      showPeriodMessage("El período se aplicó a todas las materias de la carrera.", true);
      await loadPeriods();
    } catch (error) {
      showPeriodMessage(error.message);
    }
  });

  cancel.addEventListener("click", resetForm);
  document.getElementById("refreshCareers").addEventListener("click", load);
  periodCareer.addEventListener("change", renderPeriod);
  periodCycle.addEventListener("change", () => loadPeriods().catch(error => showPeriodMessage(error.message)));
  document.getElementById("refreshCareerAdmin").addEventListener("click", loadCareerDetail);
  adminCycle.addEventListener("change", loadCareerDetail);
  document.getElementById("closeCareerAdmin").addEventListener("click", () => {
    managedCareerId = null;
    adminPanel.hidden = true;
    subjectsContainer.replaceChildren();
  });
  document.getElementById("togglePlanForm").addEventListener("click", () => {
    planForm.hidden = false;
    planForm.codigo.value = `Plan ${new Date().getFullYear() + 1}`;
    planForm.vigenteDesde.value = new Date().getFullYear() + 1;
    planMessage.textContent = "";
    planForm.codigo.focus();
  });
  document.getElementById("cancelPlan").addEventListener("click", () => {
    planForm.reset();
    planForm.hidden = true;
  });
  planForm.addEventListener("submit", async event => {
    event.preventDefault();
    if (!managedCareerId) return;
    const codigo = planForm.codigo.value.trim();
    if (!confirm(`Se publicará ${codigo} como plan vigente. El plan actual quedará histórico. ¿Continuar?`)) return;
    try {
      await AcadionApi.request(`/api/planes-estudio/carrera/${managedCareerId}`, {
        method: "POST",
        body: JSON.stringify({
          codigo,
          vigenteDesde: Number(planForm.vigenteDesde.value),
          copiarMateriasDelPlanVigente: planForm.copiarMaterias.checked
        })
      });
      planMessage.className = "status-message success";
      planMessage.textContent = "El nuevo plan se publicó correctamente.";
      await Promise.all([loadPlans(), load(), loadCareerDetail()]);
      planForm.reset();
      planForm.hidden = true;
    } catch (error) {
      planMessage.className = "status-message error";
      planMessage.textContent = error.message;
    }
  });
  load();
});
