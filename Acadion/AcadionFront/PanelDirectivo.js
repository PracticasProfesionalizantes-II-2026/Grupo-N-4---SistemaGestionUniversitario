// PanelDirectivo.js
// Inicializa la navegación y los controles de esta pantalla.
document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  Promise.all([
    AcadionApi.request("/usuarios/"),
    AcadionApi.request("/carreras/"),
    AcadionApi.request("/materias/"),
    AcadionApi.request("/api/notificaciones/")
  ]).then(([users, careers, subjects, notifications]) => {
    const counts = { users: users.length, careers: careers.length, subjects: subjects.length, notifications: notifications.length };
    Object.entries(counts).forEach(([name, value]) => {
      document.querySelectorAll(`[data-count="${name}"]`).forEach(element => { element.textContent = value; });
    });
  }).catch(() => {
    document.querySelectorAll("[data-count]")
      .forEach(element => { element.textContent = "0"; });
  });
});
