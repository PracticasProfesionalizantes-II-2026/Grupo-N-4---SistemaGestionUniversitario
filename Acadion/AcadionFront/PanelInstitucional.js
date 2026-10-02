document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  if (!AcadionApi.getSession() || document.body.dataset.section !== "director") return;

  const cycleFilter = document.getElementById("cycleFilter");
  const careerFilter = document.getElementById("careerFilter");
  const applyButton = document.getElementById("applyFilters");
  const exportButton = document.getElementById("exportCsv");
  const message = document.getElementById("dashboardMessage");
  let currentData = null;

  const currentYear = new Date().getFullYear();
  for (let year = currentYear + 1; year >= currentYear - 6; year -= 1) {
    const option = document.createElement("option");
    option.value = year;
    option.textContent = year;
    option.selected = year === currentYear;
    cycleFilter.append(option);
  }

  const setText = (id, value) => { document.getElementById(id).textContent = value ?? "0"; };
  const formatDate = value => new Intl.DateTimeFormat("es-AR", {
    dateStyle: "medium", timeStyle: "short"
  }).format(new Date(value));

  function empty(container, text) {
    container.replaceChildren();
    const item = document.createElement("p");
    item.className = "empty-chart";
    item.textContent = text;
    container.append(item);
  }

  function renderBars(containerId, items, labelKey, valueKey,
    formatter = value => String(value), showZeroRows = false, usePercentageScale = false, onSelect = null) {
    const container = document.getElementById(containerId);
    container.replaceChildren();
    if (!items?.length || (!showZeroRows && !items.some(item => Number(item[valueKey]) > 0))) {
      empty(container, "No hay registros para los filtros seleccionados.");
      return;
    }
    const maximum = Math.max(...items.map(item => Number(item[valueKey]) || 0), 1);
    items.forEach(item => {
      const value = Number(item[valueKey]) || 0;
      const row = document.createElement("div"); row.className = "bar-row";
      if (onSelect) { row.classList.add("is-clickable"); row.tabIndex = 0; row.setAttribute("role", "button"); row.addEventListener("click", () => onSelect(item)); row.addEventListener("keydown", event => { if (event.key === "Enter") onSelect(item); }); }
      const label = document.createElement("span"); label.className = "bar-label"; label.textContent = item[labelKey]; label.title = item[labelKey];
      const track = document.createElement("span"); track.className = "bar-track";
      const width = usePercentageScale
        ? Math.min(Math.max(value, 0), 100)
        : Math.max(value * 100 / maximum, value ? 2 : 0);
      const fill = document.createElement("span"); fill.className = "bar-fill"; fill.style.width = `${width}%`;
      const display = document.createElement("span"); display.className = "bar-value"; display.textContent = formatter(value, item);
      track.append(fill); row.append(label, track, display); container.append(row);
    });
  }

  async function openDetail(path, kind) {
    const dialog = document.getElementById("detailDialog");
    document.getElementById("detailTitle").textContent = "Cargando detalle…";
    document.getElementById("detailSubtitle").textContent = "";
    document.getElementById("detailContent").replaceChildren(); dialog.showModal();
    try {
      const data = await AcadionApi.request(path);
      document.getElementById("detailTitle").textContent = data.titulo;
      document.getElementById("detailSubtitle").textContent = data.subtitulo;
      const table = document.createElement("table"), head = table.createTHead().insertRow();
      const columns = kind === "career" ? ["Estudiante","Legajo","Materias cursadas"] : ["Estudiante","Legajo","Inasistencias","Justificadas"];
      columns.forEach(label=>{const th=document.createElement("th");th.textContent=label;head.append(th);});
      const body=table.createTBody(); (data.estudiantes||[]).forEach(item=>{const row=body.insertRow();const values=kind==="career"?[item.nombre,item.legajo||"—",item.materias]:[item.nombre,item.legajo||"—",item.inasistencias,item.justificadas];values.forEach(value=>{const cell=row.insertCell();cell.textContent=value;});});
      if(!data.estudiantes?.length){const row=body.insertRow(),cell=row.insertCell();cell.colSpan=columns.length;cell.className="empty-state";cell.textContent="No hay registros para mostrar.";}
      document.getElementById("detailContent").append(table);
    } catch(error){document.getElementById("detailTitle").textContent="No se pudo cargar el detalle";document.getElementById("detailSubtitle").textContent=error.message;}
  }

  function renderDistribution(containerId, entries) {
    const container = document.getElementById(containerId);
    container.replaceChildren();
    entries.forEach(([label, value]) => {
      const item = document.createElement("div"); item.className = "distribution-item";
      const title = document.createElement("span"); title.textContent = label;
      const amount = document.createElement("strong"); amount.textContent = value ?? 0;
      item.append(title, amount); container.append(item);
    });
  }

  function renderTable(bodyId, rows, columns, emptyText) {
    const body = document.getElementById(bodyId);
    body.replaceChildren();
    if (!rows?.length) {
      const row = body.insertRow(); const cell = row.insertCell();
      cell.colSpan = columns.length; cell.className = "empty-state"; cell.textContent = emptyText;
      return;
    }
    rows.forEach(item => {
      const row = body.insertRow();
      columns.forEach(column => { const cell = row.insertCell(); cell.textContent = column(item); });
    });
  }

  function populateCareers(careers) {
    if (careerFilter.options.length > 1) return;
    careers.forEach(career => {
      const option = document.createElement("option");
      option.value = career.id; option.textContent = career.nombre; careerFilter.append(option);
    });
  }

  function render(data) {
    const summary = data.resumen;
    setText("activeStudents", summary.estudiantesActivos);
    setText("activeTeachers", summary.docentesActivos);
    setText("activeCareers", summary.carrerasVigentes);
    setText("activeSubjects", summary.materiasActivas);
    setText("upcomingFinals", summary.finalesProximos);
    populateCareers(data.carreras || []);

    renderBars("careerChart", data.alumnosPorCarrera, "carrera", "porcentajeOcupacion",
      (value, item) => `${item.estudiantes}/${item.capacidad} · ${value}%`, true, true,
      item => openDetail(`/api/directivo/detalle/carreras/${item.carreraId}?cicloLectivo=${cycleFilter.value}`, "career"));
    renderBars("conditionChart", data.condicionesAcademicas, "estado", "porcentaje",
      (value, item) => `${item.cantidad} · ${value}%`, false, true);
    renderBars("absenceChart", data.inasistenciasPorMateria, "materia", "inasistencias",
      value => String(value), false, false,
      item => openDetail(`/api/directivo/detalle/materias/${item.materiaId}/inasistencias?cicloLectivo=${cycleFilter.value}`, "absence"));
    renderDistribution("paymentSummary", [
      ["Al día", data.pagos.alDia], ["Pendientes o vencidas", data.pagos.pendientes], ["Exentados", data.pagos.exentados]
    ]);
    renderDistribution("teacherAttendance", [
      ["Presentes", data.asistenciaDocente.presentes], ["Ausentes", data.asistenciaDocente.ausentes], ["Justificadas", data.asistenciaDocente.justificadas]
    ]);
    renderTable("popularSubjectsBody", data.materiasConMasAlumnos,
      [item => item.materia, item => item.carrera, item => item.estudiantes], "No hay inscripciones registradas en este ciclo.");
    renderTable("finalsBody", data.proximosFinales,
      [item => item.materia, item => formatDate(item.fecha), item => item.inscriptos], "No hay finales próximos.");
    renderTable("teacherAbsencesBody", data.docentesConAusencias,
      [item => item.docente, item => item.ausencias, item => item.justificadas], "No hay ausencias docentes registradas.");
  }

  async function loadDashboard() {
    applyButton.disabled = true;
    exportButton.disabled = true;
    message.textContent = "Cargando información institucional...";
    const parameters = new URLSearchParams({ cicloLectivo: cycleFilter.value });
    if (careerFilter.value) parameters.set("carreraId", careerFilter.value);
    try {
      currentData = await AcadionApi.request(`/api/directivo/panel?${parameters}`);
      render(currentData);
      message.textContent = "";
      exportButton.disabled = false;
    } catch (error) {
      currentData = null;
      message.textContent = error.message;
    } finally {
      applyButton.disabled = false;
    }
  }

  const csvValue = value => `"${String(value ?? "").replaceAll('"', '""')}"`;
  function exportCsv() {
    if (!currentData) return;
    const data = currentData;
    const rows = [
      ["Informe institucional Acadion"],
      ["Ciclo lectivo", data.cicloLectivo],
      ["Carrera", careerFilter.selectedOptions[0]?.textContent || "Todas las carreras"],
      [],
      ["Indicador", "Cantidad"],
      ["Estudiantes activos", data.resumen.estudiantesActivos],
      ["Docentes activos", data.resumen.docentesActivos],
      ["Carreras vigentes", data.resumen.carrerasVigentes],
      ["Materias activas", data.resumen.materiasActivas],
      ["Finales próximos", data.resumen.finalesProximos],
      [], ["Alumnos por carrera", "Estudiantes", "Capacidad", "Ocupación"],
      ...data.alumnosPorCarrera.map(x => [x.carrera, x.estudiantes, x.capacidad, `${x.porcentajeOcupacion}%`]),
      [], ["Condición académica", "Cantidad", "Porcentaje"],
      ...data.condicionesAcademicas.map(x => [x.estado, x.cantidad, `${x.porcentaje}%`]),
      [], ["Materia", "Carrera", "Estudiantes inscriptos"],
      ...data.materiasConMasAlumnos.map(x => [x.materia, x.carrera, x.estudiantes]),
      [], ["Materia", "Inasistencias"],
      ...data.inasistenciasPorMateria.map(x => [x.materia, x.inasistencias]),
      [], ["Próximo final", "Fecha", "Inscriptos"],
      ...data.proximosFinales.map(x => [x.materia, formatDate(x.fecha), x.inscriptos]),
      [], ["Docente", "Ausencias", "Justificadas"],
      ...data.docentesConAusencias.map(x => [x.docente, x.ausencias, x.justificadas])
    ];
    const csv = "\ufeff" + rows.map(row => row.map(csvValue).join(";")).join("\r\n");
    const url = URL.createObjectURL(new Blob([csv], { type: "text/csv;charset=utf-8" }));
    const link = document.createElement("a");
    link.href = url; link.download = `informe-institucional-${data.cicloLectivo}.csv`; link.click();
    URL.revokeObjectURL(url);
  }

  applyButton.addEventListener("click", loadDashboard);
  exportButton.addEventListener("click", exportCsv);
  document.getElementById("closeDetail").addEventListener("click",()=>document.getElementById("detailDialog").close());
  loadDashboard();
});
