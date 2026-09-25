document.addEventListener("DOMContentLoaded", async () => {
  Acadion.iniciarPantalla();
  const list = document.getElementById("notificationList");
  try {
    const notifications = await AcadionApi.request("/api/me/notificaciones");
    list.replaceChildren();
    if (!notifications.length) {
      const empty = document.createElement("section"); empty.className = "panel";
      empty.innerHTML = "<h2>Estás al día</h2><p>No tenés notificaciones nuevas.</p>";
      list.append(empty); return;
    }
    notifications.forEach(notification => {
      const article = document.createElement("article"); article.className = "panel notification-card";
      const title = document.createElement("h2"); title.textContent = notification.titulo;
      const content = document.createElement("p"); content.textContent = notification.mensaje;
      const meta = document.createElement("small");
      meta.textContent = `${notification.destinatarios} · Prioridad ${notification.prioridad} · ${new Date(notification.fechaPublicacionUtc).toLocaleString("es-AR")}`;
      article.append(title, content, meta); list.append(article);
    });
  } catch (error) {
    list.replaceChildren();
    const panel = document.createElement("section"); panel.className = "panel";
    const title = document.createElement("h2"); title.textContent = "No se pudieron cargar";
    const detail = document.createElement("p"); detail.textContent = error.message;
    panel.append(title, detail); list.append(panel);
  }
});
