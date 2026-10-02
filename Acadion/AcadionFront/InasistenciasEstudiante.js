document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const studentId = Number(new URLSearchParams(window.location.search).get("usuarioId"));
  const cycleFilter = document.getElementById("cycleFilter");
  const content = document.getElementById("absencesContent");
  const message = document.getElementById("absenceMessage");
  const currentYear = new Date().getFullYear();

  for (let year = currentYear + 1; year >= currentYear - 5; year -= 1) {
    cycleFilter.append(new Option(String(year), String(year), year === currentYear, year === currentYear));
  }

  function showMessage(text, error = false) {
    message.hidden = !text;
    message.textContent = text;
    message.className = `notice-strip${error ? " error" : ""}`;
  }

  function initials(student) {
    return `${student.nombre?.[0] || ""}${student.apellido?.[0] || ""}`.toUpperCase() || "ES";
  }

  function formatDate(value) {
    return String(value || "").slice(0, 10) || "—";
  }

  function yearLabel(number, fallback) {
    if (fallback) return fallback;
    return `${number}.º año`;
  }

  function groupBy(items, keySelector) {
    return items.reduce((groups, item) => {
      const key = keySelector(item);
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key).push(item);
      return groups;
    }, new Map());
  }

  async function updateJustification(item, select) {
    const justified = select.value === "true";
    if (!window.confirm(`¿${justified ? "Justificar" : "Quitar la justificación de"} la inasistencia del ${formatDate(item.fecha)}?`)) {
      select.value = String(item.justificada);
      return;
    }
    select.disabled = true;
    try {
      await AcadionApi.request(`/api/gestion/usuarios/${studentId}/inasistencias/${item.idAsistencia}/justificacion`, {
        method: "PUT",
        body: JSON.stringify({ justificada: justified })
      });
      item.justificada = justified;
      select.classList.toggle("is-justified", justified);
      showMessage(justified ? "Inasistencia justificada correctamente." : "Se quitó la justificación.");
    } catch (error) {
      select.value = String(item.justificada);
      showMessage(error.message, true);
    } finally {
      select.disabled = false;
    }
  }

  async function uploadJustification(item, input, button) {
    if (!input.files[0]) return;
    button.disabled = true;
    const data = new FormData(); data.append("archivo", input.files[0]);
    try {
      const result = await AcadionApi.request(`/api/gestion/usuarios/${studentId}/inasistencias/${item.idAsistencia}/justificacion-archivo`, { method: "POST", body: data });
      showMessage(result.mensaje);
      await loadAbsences();
    } catch (error) { showMessage(error.message, true); button.disabled = false; }
  }

  function createAbsenceRow(item) {
    const row = document.createElement("tr");
    [formatDate(item.fecha), item.tipoClase || "—", item.temaDictado || "Sin tema registrado"].forEach(value => {
      const cell = row.insertCell();
      cell.textContent = value;
    });
    const justificationCell = row.insertCell();
    const select = document.createElement("select");
    select.className = `justification-select${item.justificada ? " is-justified" : ""}`;
    select.setAttribute("aria-label", `Justificación de la inasistencia del ${formatDate(item.fecha)}`);
    select.append(new Option("No", "false"), new Option("Sí", "true"));
    select.value = String(Boolean(item.justificada));
    select.addEventListener("change", () => updateJustification(item, select));
    justificationCell.append(select);
    const documentCell = row.insertCell();
    if (item.tieneJustificativo) {
      const link = document.createElement("a"); link.className = "document-link"; link.target = "_blank";
      link.href = `${AcadionApi.baseUrl}/api/gestion/usuarios/${studentId}/inasistencias/${item.idAsistencia}/justificativo`;
      link.textContent = "Ver archivo"; documentCell.append(link);
    }
    const file = document.createElement("input"); file.type = "file"; file.accept = "application/pdf,image/jpeg,image/png"; file.hidden = true;
    const upload = document.createElement("button"); upload.type = "button"; upload.className = "upload-proof"; upload.textContent = item.tieneJustificativo ? "Reemplazar" : "Adjuntar";
    upload.addEventListener("click", () => file.click()); file.addEventListener("change", () => uploadJustification(item, file, upload)); documentCell.append(file, upload);
    const approvedByCell = row.insertCell(); approvedByCell.textContent = item.justificadaPor || "—";
    const countCell = row.insertCell();
    const count = document.createElement("span");
    count.className = "absence-count";
    count.textContent = item.cantidadInasistencias;
    countCell.append(count);
    return row;
  }

  function createSubject(subjectItems) {
    const wrapper = document.createElement("section");
    wrapper.className = "subject-absence";
    const title = document.createElement("h3");
    title.textContent = subjectItems[0].materia;
    const tableWrapper = document.createElement("div");
    tableWrapper.className = "table-wrapper";
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const headRow = document.createElement("tr");
    ["Fecha", "Tipo de clase", "Tema dictado", "Justificada", "Justificativo", "Aprobado por", "Cantidad"].forEach(label => {
      const th = document.createElement("th");
      th.textContent = label;
      headRow.append(th);
    });
    head.append(headRow);
    const body = document.createElement("tbody");
    subjectItems.forEach(item => body.append(createAbsenceRow(item)));
    table.append(head, body);
    tableWrapper.append(table);
    wrapper.append(title, tableWrapper);
    return wrapper;
  }

  function renderAbsences(items) {
    content.replaceChildren();
    if (!items.length) {
      const empty = document.createElement("section");
      empty.className = "panel empty-state";
      empty.textContent = `No se registraron inasistencias durante ${cycleFilter.value}.`;
      content.append(empty);
      return;
    }

    const years = groupBy(items, item => item.numeroAnio);
    [...years.entries()].sort((a, b) => a[0] - b[0]).forEach(([number, yearItems]) => {
      const panel = document.createElement("section");
      panel.className = "panel academic-year";
      const heading = document.createElement("div");
      heading.className = "academic-year-heading";
      const title = document.createElement("h2");
      title.textContent = yearLabel(number, yearItems[0].nombreAnio);
      const total = document.createElement("span");
      const amount = yearItems.reduce((sum, item) => sum + item.cantidadInasistencias, 0);
      total.textContent = `${amount} ${amount === 1 ? "inasistencia" : "inasistencias"}`;
      heading.append(title, total);
      panel.append(heading);
      const subjects = groupBy(yearItems, item => item.materiaId);
      subjects.forEach(subjectItems => panel.append(createSubject(subjectItems)));
      content.append(panel);
    });
  }

  async function loadAbsences() {
    if (!studentId) {
      content.innerHTML = '<section class="panel empty-state">No se indicó qué estudiante se debe consultar.</section>';
      return;
    }
    showMessage("");
    content.innerHTML = '<section class="panel empty-state">Cargando inasistencias...</section>';
    try {
      const result = await AcadionApi.request(`/api/gestion/usuarios/${studentId}/inasistencias?cicloLectivo=${cycleFilter.value}`);
      const student = result.estudiante;
      document.getElementById("studentInitials").textContent = initials(student);
      document.getElementById("studentName").textContent = `${student.nombre} ${student.apellido}`;
      document.getElementById("studentDetails").textContent = `${student.legajo || "Sin legajo"} · DNI ${student.dni} · ${student.carrera}`;
      renderAbsences(result.inasistencias || []);
    } catch (error) {
      content.innerHTML = '<section class="panel empty-state">No fue posible cargar las inasistencias.</section>';
      showMessage(error.message, true);
    }
  }

  cycleFilter.addEventListener("change", loadAbsences);
  loadAbsences();
});
