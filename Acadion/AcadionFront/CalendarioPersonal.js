document.addEventListener("DOMContentLoaded", () => {
  const session = AcadionApi.getSession();
  const isTeacher = session?.rol === "Docente";
  document.body.dataset.section = isTeacher ? "teacher" : "student";
  document.getElementById("calendarBack").href = isTeacher ? "PanelDocente.html" : "MenuPrincipal.html";
  Acadion.iniciarPantalla();

  const cycle = document.getElementById("calendarCycle");
  const list = document.getElementById("personalCalendar");
  const summary = document.getElementById("calendarSummary");
  const tabs = [...document.querySelectorAll("[data-filter]")];
  const evaluationTypes = new Set(["Parcial", "Final", "Presentación", "Trabajo práctico", "Recuperatorio", "Mesa final"]);
  let events = [];
  let activeFilter = "Todos";
  cycle.value = new Date().getFullYear();

  const formatDate = value => new Intl.DateTimeFormat("es-AR", {
    weekday: "short", day: "2-digit", month: "short", year: "numeric"
  }).format(new Date(value));
  const formatTime = value => new Intl.DateTimeFormat("es-AR", {
    hour: "2-digit", minute: "2-digit"
  }).format(new Date(value));
  const category = event => event.tipo === "Inscripción"
    ? "Inscripciones"
    : evaluationTypes.has(event.tipo) ? "Evaluaciones" : "Institucional";

  function render() {
    const visible = activeFilter === "Todos"
      ? events
      : events.filter(event => category(event) === activeFilter);
    list.replaceChildren();
    summary.textContent = `${visible.length} ${visible.length === 1 ? "evento" : "eventos"} en ${cycle.value}`;
    if (!visible.length) {
      const empty = document.createElement("p");
      empty.className = "empty-state";
      empty.textContent = `No hay eventos de ${activeFilter.toLowerCase()} para ${cycle.value}.`;
      list.append(empty);
      return;
    }

    const groups = new Map();
    visible.forEach(event => {
      const key = new Intl.DateTimeFormat("es-AR", { month: "long", year: "numeric" })
        .format(new Date(event.fechaInicioUtc));
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key).push(event);
    });
    groups.forEach((items, month) => {
      const group = document.createElement("section");
      group.className = "calendar-month";
      const heading = document.createElement("h2");
      heading.textContent = month.charAt(0).toUpperCase() + month.slice(1);
      group.append(heading);
      items.forEach(event => {
        const card = document.createElement("article");
        card.className = `personal-event event-${category(event).toLowerCase()}`;
        const date = document.createElement("div");
        date.className = "event-date";
        const day = document.createElement("strong");
        day.textContent = new Date(event.fechaInicioUtc).getDate().toString().padStart(2, "0");
        const weekday = document.createElement("span");
        weekday.textContent = new Intl.DateTimeFormat("es-AR", { weekday: "short" }).format(new Date(event.fechaInicioUtc));
        date.append(day, weekday);
        const content = document.createElement("div");
        const top = document.createElement("div"); top.className = "event-heading";
        const title = document.createElement("h3"); title.textContent = event.titulo;
        const badge = document.createElement("span"); badge.className = "event-badge"; badge.textContent = event.tipo;
        top.append(title, badge);
        const details = document.createElement("p");
        details.className = "event-details";
        const start = new Date(event.fechaInicioUtc);
        const end = new Date(event.fechaFinUtc);
        const sameDay = start.toDateString() === end.toDateString();
        details.textContent = sameDay
          ? `${formatDate(start)} · ${formatTime(start)} a ${formatTime(end)} · ${event.carrera}`
          : `${formatDate(start)} ${formatTime(start)} — ${formatDate(end)} ${formatTime(end)} · ${event.carrera}`;
        const description = document.createElement("p");
        description.className = "event-description";
        description.textContent = event.descripcion || "Sin descripción adicional.";
        content.append(top, details, description);
        card.append(date, content);
        group.append(card);
      });
      list.append(group);
    });
  }

  async function loadCalendar() {
    list.innerHTML = '<p class="empty-state">Cargando calendario…</p>';
    summary.textContent = "";
    try {
      events = await AcadionApi.request(`/api/calendario/personal?cicloLectivo=${cycle.value}`);
      render();
    } catch (error) {
      list.innerHTML = "";
      const message = document.createElement("p");
      message.className = "empty-state error";
      message.textContent = error.message;
      list.append(message);
    }
  }

  tabs.forEach(tab => tab.addEventListener("click", () => {
    tabs.forEach(item => item.classList.toggle("active", item === tab));
    activeFilter = tab.dataset.filter;
    render();
  }));
  cycle.addEventListener("change", loadCalendar);
  loadCalendar();
});
