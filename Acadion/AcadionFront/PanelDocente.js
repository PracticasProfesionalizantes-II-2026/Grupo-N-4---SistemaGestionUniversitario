document.addEventListener("DOMContentLoaded", async () => {
  Acadion.iniciarPantalla();
  const cycle = new Date().getFullYear();
  document.getElementById("currentCycle").textContent = cycle;
  const message = document.getElementById("dashboardMessage");
  const results = await Promise.allSettled([
    AcadionApi.request("/api/me/perfil"),
    AcadionApi.request("/api/me/materias"),
    AcadionApi.request(`/api/docente/alumnos?cicloLectivo=${cycle}`),
    AcadionApi.request(`/api/docente/examenes?cicloLectivo=${cycle}`),
    AcadionApi.request("/api/me/notificaciones")
  ]);
  const value = (index, fallback) => results[index].status === "fulfilled" ? results[index].value : fallback;
  const profile = value(0, null);
  const subjects = value(1, []);
  const students = value(2, []);
  const exams = value(3, []);
  const notifications = value(4, []);
  if (profile) document.getElementById("teacherName").textContent = profile.nombre || profile.nombreUsuario || "docente";
  document.getElementById("subjectCount").textContent = subjects.filter(item => item.cicloLectivo === cycle).length;
  document.getElementById("studentCount").textContent = new Set(students.map(item => item.idEstudiante)).size;
  document.getElementById("examCount").textContent = exams.filter(item => new Date(item.fecha) >= new Date()).length;
  document.getElementById("notificationCount").textContent = notifications.filter(item => !item.leida).length;
  if (results.some(result => result.status === "rejected")) {
    message.hidden = false;
    message.textContent = "Parte del resumen no pudo actualizarse. Podés seguir trabajando desde cada módulo.";
  }
});
