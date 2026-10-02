document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const cycle = document.getElementById("calendarCycle");
  const career = document.getElementById("calendarCareer");
  const eventCareer = document.getElementById("eventCareer");
  const list = document.getElementById("calendarList");
  const form = document.getElementById("eventForm");
  const panel = document.getElementById("eventPanel");
  const turnForm = document.getElementById("turnForm");
  const turnList = document.getElementById("turnList");
  cycle.value = new Date().getFullYear();

  function option(value, label) { const item = document.createElement("option"); item.value = value; item.textContent = label; return item; }
  function format(value) { return new Intl.DateTimeFormat("es-AR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)); }

  async function loadCareers() {
    const careers = await AcadionApi.request("/carreras/");
    careers.forEach(item => {
      career.append(option(item.idCarrera, item.nombre));
      eventCareer.append(option(item.idCarrera, item.nombre));
    });
  }

  async function loadCalendar() {
    list.replaceChildren(Object.assign(document.createElement("p"), { className: "empty-state", textContent: "Cargando calendario..." }));
    try {
      const params = new URLSearchParams({ cicloLectivo: cycle.value });
      if (career.value) params.set("carreraId", career.value);
      const events = await AcadionApi.request(`/api/calendario/?${params}`);
      list.replaceChildren();
      if (!events.length) {
        list.append(Object.assign(document.createElement("p"), { className: "empty-state", textContent: `No hay eventos académicos para ${cycle.value}.` }));
        return;
      }
      events.forEach(event => {
        const card = document.createElement("article"); card.className = "calendar-event";
        const date = document.createElement("div"); date.className = "calendar-date"; date.textContent = format(event.fechaInicioUtc);
        const content = document.createElement("div");
        const heading = document.createElement("div"); heading.className = "calendar-heading";
        const title = document.createElement("h2"); title.textContent = event.titulo;
        const badge = document.createElement("span"); badge.className = "badge active"; badge.textContent = event.tipo;
        heading.append(title, badge);
        const meta = document.createElement("p"); meta.textContent = `${event.carrera} · Hasta ${format(event.fechaFinUtc)}`;
        const description = document.createElement("p"); description.textContent = event.descripcion || "Sin descripción";
        content.append(heading, meta, description); card.append(date, content);
        if (!event.automatico) {
          const remove = document.createElement("button"); remove.type = "button"; remove.className = "danger-button compact-button"; remove.textContent = "Eliminar";
          remove.addEventListener("click", async () => {
            if (!confirm(`¿Eliminar el evento ${event.titulo}?`)) return;
            await AcadionApi.request(`/api/calendario/${event.id}`, { method: "DELETE" });
            await loadCalendar();
          });
          card.append(remove);
        }
        list.append(card);
      });
    } catch (error) { list.replaceChildren(Object.assign(document.createElement("p"), { className: "empty-state error", textContent: error.message })); }
  }

  async function loadTurns() {
    turnList.replaceChildren(Object.assign(document.createElement("p"), { className: "empty-state", textContent: "Cargando turnos..." }));
    try {
      const turns = await AcadionApi.request(`/api/turnos-final/?cicloLectivo=${cycle.value}&incluirInactivos=true`);
      turnList.replaceChildren();
      if (!turns.length) {
        turnList.append(Object.assign(document.createElement("p"), { className: "empty-state", textContent: `No hay turnos de final configurados para ${cycle.value}.` }));
        return;
      }
      turns.forEach(turn => {
        const card = document.createElement("article"); card.className = "turn-card";
        const info = document.createElement("div");
        const title = document.createElement("strong"); title.textContent = `${turn.nombre} · ${turn.numeroLlamado}.º llamado`;
        const meta = document.createElement("small"); meta.textContent = `${format(turn.fechaInicioUtc)} al ${format(turn.fechaFinUtc)} · ${turn.cantidadExamenes} finales · ${turn.activo ? "Activo" : "Inactivo"}`;
        info.append(title, meta); card.append(info);
        if (turn.activo) {
          const deactivate = document.createElement("button"); deactivate.type = "button"; deactivate.className = "danger-button compact-button"; deactivate.textContent = "Desactivar";
          deactivate.addEventListener("click", async () => {
            if (!confirm(`¿Desactivar ${turn.nombre}, ${turn.numeroLlamado}.º llamado?`)) return;
            await AcadionApi.request(`/api/turnos-final/${turn.id}`, { method: "DELETE" });
            await loadTurns();
          });
          card.append(deactivate);
        }
        turnList.append(card);
      });
    } catch (error) { turnList.replaceChildren(Object.assign(document.createElement("p"), { className: "empty-state error", textContent: error.message })); }
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    const message = document.getElementById("eventMessage");
    try {
      await AcadionApi.request("/api/calendario/", { method: "POST", body: JSON.stringify({
        titulo: form.titulo.value.trim(), tipo: form.tipo.value,
        descripcion: form.descripcion.value.trim(), carreraId: form.carreraId.value ? Number(form.carreraId.value) : null,
        fechaInicio: new Date(form.fechaInicio.value).toISOString(), fechaFin: new Date(form.fechaFin.value).toISOString()
      }) });
      message.className = "status-message success"; message.textContent = "Evento creado correctamente.";
      form.reset(); panel.hidden = true; await loadCalendar();
    } catch (error) { message.className = "status-message error"; message.textContent = error.message; }
  });
  turnForm.addEventListener("submit", async event => {
    event.preventDefault();
    const message = document.getElementById("turnMessage");
    try {
      await AcadionApi.request("/api/turnos-final/", { method: "POST", body: JSON.stringify({
        cicloLectivo: Number(turnForm.cicloLectivo.value), nombre: turnForm.nombre.value.trim(),
        numeroLlamado: Number(turnForm.numeroLlamado.value),
        fechaInicio: new Date(turnForm.fechaInicio.value).toISOString(), fechaFin: new Date(turnForm.fechaFin.value).toISOString(), activo: true
      }) });
      message.className = "status-message success"; message.textContent = "Turno creado correctamente.";
      turnForm.reset(); turnForm.hidden = true; await loadTurns(); await loadCalendar();
    } catch (error) { message.className = "status-message error"; message.textContent = error.message; }
  });
  document.getElementById("toggleEventForm").addEventListener("click", () => { panel.hidden = false; form.titulo.focus(); });
  document.getElementById("cancelEvent").addEventListener("click", () => { panel.hidden = true; });
  document.getElementById("toggleTurnForm").addEventListener("click", () => { turnForm.hidden = false; turnForm.cicloLectivo.value = cycle.value; turnForm.nombre.focus(); });
  document.getElementById("cancelTurn").addEventListener("click", () => { turnForm.hidden = true; });
  document.getElementById("refreshCalendar").addEventListener("click", loadCalendar);
  cycle.addEventListener("change", () => { loadCalendar(); loadTurns(); }); career.addEventListener("change", loadCalendar);
  loadCareers().then(() => Promise.all([loadCalendar(), loadTurns()])).catch(error => { list.textContent = error.message; });
});
