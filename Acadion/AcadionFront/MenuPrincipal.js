const STORAGE_ID_KEY = "acadion_usuario_id";

const getSessionId = () =>
  localStorage.getItem(STORAGE_ID_KEY) ||
  sessionStorage.getItem(STORAGE_ID_KEY);

const clearSession = async () => {
  await AcadionApi.logout();
  localStorage.removeItem(STORAGE_ID_KEY);
  localStorage.removeItem("acadion_token");
  sessionStorage.removeItem(STORAGE_ID_KEY);
  sessionStorage.removeItem("acadion_token");
};

const redirectToLogin = () => {
  window.location.href = "Login.html";
};

const getPersonName = persona =>
  [persona.nombre, persona.apellido].filter(Boolean).join(" ") ||
  [persona.Nombre, persona.Apellido].filter(Boolean).join(" ") ||
  persona.nombreCompleto || persona.NombreCompleto || "Estudiante";

const getFirstName = persona =>
  persona.nombre || persona.Nombre || getPersonName(persona).split(" ")[0];

const getProfileImage = (persona, nombre) =>
  persona.fotoPerfilUrl || persona.fotoPerfil || persona.FotoPerfil ||
  `https://ui-avatars.com/api/?name=${encodeURIComponent(nombre)}&background=ff7a00&color=fff&bold=true`;

const normalizeText = value => String(value || "")
  .normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim().toLowerCase();

const normalizeTime = value => String(value || "").slice(0, 5);

const formatName = value => String(value || "").trim().toLocaleLowerCase("es-AR")
  .replace(/(^|[\s'-])([a-záéíóúüñ])/g, (_, separator, letter) =>
    separator + letter.toLocaleUpperCase("es-AR"));

function isCurrentAcademicPeriod(subject, now = new Date()) {
  if (Number(subject.cicloLectivo) !== now.getFullYear()) return false;
  const state = normalizeText(subject.estado);
  if (["cancelada", "aprobada", "promocionada"].includes(state)) return false;

  const term = normalizeText(subject.cuatrimestre);
  const month = now.getMonth() + 1;
  if (term.includes("1") || term.includes("primer")) return month >= 3 && month <= 7;
  if (term.includes("2") || term.includes("segundo")) return month >= 8 && month <= 12;
  return month >= 3 && month <= 12;
}

function dayColumn(value) {
  return ({ lunes: 1, martes: 2, miercoles: 3, jueves: 4, viernes: 5 })[normalizeText(value)] || 0;
}

function subjectChip(subject, index) {
  const chip = document.createElement("span");
  chip.className = `subject ${["subject-orange", "subject-dark", "subject-gray"][index % 3]}`;
  chip.textContent = subject.nombre;
  chip.title = [subject.nombre, subject.carrera, subject.anio, subject.cuatrimestre].filter(Boolean).join(" · ");
  return chip;
}

function renderSchedule(subjects) {
  const body = document.getElementById("scheduleBody");
  const counter = document.getElementById("scheduleCount");
  const activeSubjects = subjects.filter(subject => isCurrentAcademicPeriod(subject));
  const slots = new Map();

  activeSubjects.forEach((subject, subjectIndex) => {
    (subject.horarios || []).forEach(schedule => {
      const column = dayColumn(schedule.diaSemana);
      const start = normalizeTime(schedule.horaInicio);
      const end = normalizeTime(schedule.horaFin);
      if (!column || !start || !end) return;
      const key = `${start}-${end}`;
      if (!slots.has(key)) slots.set(key, { start, end, cells: new Map() });
      const slot = slots.get(key);
      if (!slot.cells.has(column)) slot.cells.set(column, []);
      slot.cells.get(column).push({ ...subject, subjectIndex });
    });
  });

  body.replaceChildren();
  const orderedSlots = [...slots.values()].sort((a, b) => a.start.localeCompare(b.start));
  if (!orderedSlots.length) {
    const row = document.createElement("tr");
    const cell = document.createElement("td");
    cell.colSpan = 6;
    cell.className = "schedule-empty";
    cell.textContent = activeSubjects.length
      ? "Tus materias activas todavía no tienen horarios cargados."
      : "No tenés materias activas para el período académico actual.";
    row.append(cell);
    body.append(row);
  } else {
    orderedSlots.forEach(slot => {
      const row = document.createElement("tr");
      const time = document.createElement("td");
      time.textContent = `${slot.start} - ${slot.end}`;
      row.append(time);
      for (let column = 1; column <= 5; column += 1) {
        const cell = document.createElement("td");
        (slot.cells.get(column) || []).forEach(subject => cell.append(subjectChip(subject, subject.subjectIndex)));
        row.append(cell);
      }
      body.append(row);
    });
  }

  const total = activeSubjects.length;
  counter.textContent = total === 1
    ? "Mostrando 1 materia del período actual"
    : `Mostrando ${total} materias del período actual`;
}

async function loadStudentDashboard() {
  try {
    const [profile, subjects] = await Promise.all([
      AcadionApi.request("/api/me/perfil"),
      AcadionApi.request("/api/me/materias")
    ]);
    const firstName = formatName(getFirstName(profile));
    const fullName = formatName(getPersonName(profile));
    document.getElementById("welcomeTitle").textContent = `¡Hola, ${firstName}!`;
    document.getElementById("studentProposal").textContent = `Propuesta: ${profile.carrera || "Carrera sin informar"}`;

    renderSchedule(Array.isArray(subjects) ? subjects : []);
  } catch (error) {
    console.error("No se pudo cargar el inicio del estudiante.", error);
    renderSchedule([]);
  }
}

document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  if (!getSessionId()) {
    redirectToLogin();
    return;
  }

  loadStudentDashboard();

  document.getElementById("subjectSearch").addEventListener("input", event => {
    const searchTerm = normalizeText(event.target.value);
    document.querySelectorAll("#scheduleBody tr").forEach(row => {
      row.hidden = !normalizeText(row.textContent).includes(searchTerm);
    });
  });

});
