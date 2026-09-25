document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const container = document.getElementById("teacherSubjects");
  const cycleInput = document.getElementById("cycleFilter");
  const message = document.getElementById("subjectsMessage");
  let subjects = [];
  cycleInput.value = new Date().getFullYear();

  const formatTime = value => String(value || "").slice(0, 5);
  const scheduleText = subject => subject.horarios?.length
    ? subject.horarios.map(item => `${item.diaSemana} ${formatTime(item.horaInicio)}–${formatTime(item.horaFin)}`).join("\n")
    : "Sin horarios cargados";
  const initials = name => String(name || "E").split(/\s+/).filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase();

  function emptyState(text) {
    const element = document.createElement("p"); element.className = "empty-state"; element.textContent = text; return element;
  }

  function studentItem(student) {
    const row = document.createElement("article"); row.className = "student-item";
    let avatar;
    if (student.fotoPerfilUrl) {
      avatar = document.createElement("img"); avatar.className = "student-photo"; avatar.src = student.fotoPerfilUrl; avatar.alt = "";
    } else {
      avatar = document.createElement("div"); avatar.className = "student-initials"; avatar.textContent = initials(student.estudiante); avatar.setAttribute("aria-hidden", "true");
    }
    const info = document.createElement("div");
    const name = document.createElement("strong"); name.textContent = Acadion.formatearNombre(student.estudiante);
    const meta = document.createElement("small"); meta.textContent = `${student.legajo || "Sin legajo"} · ${student.estado}`;
    info.append(name, meta); row.append(avatar, info); return row;
  }

  async function toggleStudents(button, drawer, subject) {
    if (!drawer.hidden) { drawer.hidden = true; button.textContent = "Ver estudiantes"; return; }
    drawer.hidden = false; button.textContent = "Ocultar estudiantes"; drawer.replaceChildren(emptyState("Cargando estudiantes..."));
    try {
      const students = await AcadionApi.request(`/api/docente/alumnos?materiaId=${subject.materiaId}&cicloLectivo=${subject.cicloLectivo}`);
      drawer.replaceChildren();
      if (!students.length) { drawer.append(emptyState("No hay estudiantes inscriptos en esta comisión.")); return; }
      const list = document.createElement("div"); list.className = "student-list";
      students.forEach(student => list.append(studentItem(student))); drawer.append(list);
    } catch (error) { drawer.replaceChildren(emptyState(error.message)); }
  }

  function metaItem(label, value) {
    const item = document.createElement("div"); const key = document.createElement("span"); const data = document.createElement("strong");
    key.textContent = label; data.textContent = value;
    if (label === "Horarios") data.className = "schedule-lines";
    item.append(key, data); return item;
  }

  function render() {
    const visible = subjects.filter(item => item.cicloLectivo === Number(cycleInput.value));
    container.replaceChildren();
    if (!visible.length) { container.append(emptyState("No tenés materias asignadas para este ciclo lectivo.")); return; }
    visible.forEach(subject => {
      const card = document.createElement("article"); card.className = "teacher-subject-card";
      const header = document.createElement("header"); header.className = "teacher-subject-header";
      const title = document.createElement("div");
      const kicker = document.createElement("p"); kicker.className = "section-kicker"; kicker.textContent = subject.carrera || "Carrera sin informar";
      const heading = document.createElement("h2"); heading.textContent = subject.nombre;
      const detail = document.createElement("p"); detail.textContent = `${subject.anio || "Año sin informar"} · ${subject.planEstudios || "Plan sin informar"}`;
      title.append(kicker, heading, detail);
      const badge = document.createElement("span"); badge.className = "badge active"; badge.textContent = subject.cuatrimestre;
      header.append(title, badge);
      const meta = document.createElement("div"); meta.className = "teacher-subject-meta";
      meta.append(metaItem("Modalidad", subject.modalidad || "Sin definir"), metaItem("Horarios", scheduleText(subject)), metaItem("Ciclo", subject.cicloLectivo), metaItem("Estudiantes", subject.cantidadEstudiantes ?? "Ver listado"));
      const actions = document.createElement("div"); actions.className = "teacher-subject-actions";
      const studentsButton = document.createElement("button"); studentsButton.type = "button"; studentsButton.className = "secondary-button"; studentsButton.textContent = "Ver estudiantes";
      const attendance = document.createElement("a"); attendance.className = "primary-button"; attendance.textContent = "Tomar asistencia"; attendance.href = `DocenteAsistencias.html?materiaId=${subject.materiaId}`;
      const exams = document.createElement("a"); exams.className = "secondary-button"; exams.textContent = "Evaluaciones y notas"; exams.href = `DocenteEvaluaciones.html?materiaId=${subject.materiaId}`;
      const drawer = document.createElement("div"); drawer.className = "student-drawer"; drawer.hidden = true;
      studentsButton.addEventListener("click", () => toggleStudents(studentsButton, drawer, subject));
      actions.append(studentsButton, attendance, exams); card.append(header, meta, actions, drawer); container.append(card);
    });
  }

  async function load() {
    try { subjects = await AcadionApi.request("/api/me/materias"); message.hidden = true; render(); }
    catch (error) { message.hidden = false; message.className = "notice-strip error"; message.textContent = error.message; container.replaceChildren(); }
  }
  cycleInput.addEventListener("change", render); load();
});
