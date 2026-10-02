document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  Promise.all([
    AcadionApi.request("/api/gestion/usuarios/"),
    AcadionApi.request("/carreras/"),
    AcadionApi.request("/materias/"),
    AcadionApi.request("/api/notificaciones/")
  ]).then(([users, careers, subjects, notifications]) => {
    const counts = {
      users: users.length,
      careers: careers.length,
      subjects: subjects.length,
      notifications: notifications.length
    };
    Object.entries(counts).forEach(([name, value]) => {
      document.querySelectorAll(`[data-count="${name}"]`)
        .forEach(element => { element.textContent = value; });
    });
  }).catch(() => {
    document.querySelectorAll("[data-count]")
      .forEach(element => { element.textContent = "0"; });
  });
  AcadionApi.request(`/api/directivo/panel?cicloLectivo=${new Date().getFullYear()}`)
    .then(data => {
      const values = {
        students: data.resumen.estudiantesActivos,
        fees: data.pagos.pendientes,
        finals: data.resumen.finalesProximos,
        subjects: data.resumen.materiasActivas,
        fullCareers: data.alumnosPorCarrera.filter(item => item.capacidad > 0 && item.estudiantes >= item.capacidad).length
      };
      Object.entries(values).forEach(([key, value]) => {
        const element = document.querySelector(`[data-indicator="${key}"]`);
        if (element) element.textContent = value;
      });
    })
    .catch(() => document.querySelectorAll("[data-indicator]").forEach(item => { item.textContent = "0"; }));
});
