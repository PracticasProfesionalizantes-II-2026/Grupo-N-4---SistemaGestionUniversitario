document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const form = document.getElementById("notificationForm");
  const list = document.getElementById("notificationList");
  const message = document.getElementById("notificationMessage");

  function showMessage(value, success = false) {
    message.textContent = value;
    message.className = `status-message ${success ? "success" : "error"}`;
  }

  function formatDate(value) {
    return value ? new Intl.DateTimeFormat("es-AR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : "Sin vencimiento";
  }

  function createCard(notification) {
    const article = document.createElement("article");
    article.className = "notification-card";
    const heading = document.createElement("div"); heading.className = "panel-heading";
    const title = document.createElement("h3"); title.textContent = notification.titulo;
    const toggle = document.createElement("button"); toggle.type = "button"; toggle.className = "secondary-button";
    toggle.textContent = notification.activa ? "Desactivar" : "Activar";
    toggle.addEventListener("click", async () => {
      try { await AcadionApi.request(`/api/notificaciones/${notification.id}/estado?activa=${!notification.activa}`, { method: "PATCH" }); await loadNotifications(); }
      catch (error) { showMessage(error.message); }
    });
    heading.append(title, toggle);
    const content = document.createElement("p"); content.textContent = notification.mensaje;
    const meta = document.createElement("div"); meta.className = "notification-meta";
    [notification.destinatarios, `Prioridad ${notification.prioridad}`, formatDate(notification.fechaPublicacionUtc), `Hasta: ${formatDate(notification.fechaExpiracionUtc)}`, notification.activa ? "Activa" : "Inactiva"]
      .forEach(value => { const span = document.createElement("span"); span.textContent = value; meta.append(span); });
    article.append(heading, content, meta);
    return article;
  }

  async function loadNotifications() {
    try {
      const notifications = await AcadionApi.request("/api/notificaciones/");
      list.replaceChildren();
      if (!notifications.length) { const empty = document.createElement("p"); empty.className = "empty-state"; empty.textContent = "Todavía no hay notificaciones publicadas."; list.append(empty); return; }
      notifications.forEach(item => list.append(createCard(item)));
    } catch (error) { list.textContent = error.message; list.className = "empty-state"; }
  }

  form.addEventListener("submit", async event => {
    event.preventDefault(); const fields = form.elements;
    const expiration = fields.fechaExpiracion.value ? new Date(fields.fechaExpiracion.value).toISOString() : null;
    try {
      await AcadionApi.request("/api/notificaciones/", { method: "POST", body: JSON.stringify({ titulo: fields.titulo.value.trim(), mensaje: fields.mensaje.value.trim(), rolDestinoId: fields.rolDestinoId.value ? Number(fields.rolDestinoId.value) : null, prioridad: fields.prioridad.value, fechaExpiracionUtc: expiration }) });
      form.reset(); showMessage("Notificación publicada correctamente.", true); await loadNotifications();
    } catch (error) { showMessage(error.message); }
  });

  document.getElementById("refreshNotifications").addEventListener("click", loadNotifications);
  loadNotifications();
});
