document.addEventListener("DOMContentLoaded", async () => {
  Acadion.iniciarPantalla();
  const list = document.getElementById("notificationList");
  const markAll = document.getElementById("markAllRead");
  let notifications = [];
  function updateBadge() {
    const unread = notifications.filter(item => !item.leida).length;
    const button = document.querySelector(".notification-button");
    const dot = button?.querySelector(".notification-dot");
    if (button) button.setAttribute("aria-label", unread ? `Notificaciones: ${unread} sin leer` : "Notificaciones: ninguna sin leer");
    if (dot) { dot.hidden = unread === 0; dot.textContent = unread > 9 ? "9+" : unread ? String(unread) : ""; }
  }
  async function markRead(notification, article) {
    if (notification.leida) return;
    await AcadionApi.request(`/api/me/notificaciones/${notification.id}/leida`, { method: "PATCH" });
    notification.leida = true;
    article.classList.remove("unread");
    markAll.hidden = !notifications.some(item => !item.leida);
    updateBadge();
  }
  async function load() {
  try {
    notifications = await AcadionApi.request("/api/me/notificaciones");
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
      article.classList.toggle("unread", !notification.leida);
      article.tabIndex = 0;
      article.addEventListener("click", () => markRead(notification, article).catch(() => {}));
      article.addEventListener("keydown", event => { if (event.key === "Enter") article.click(); });
      article.append(title, content, meta); list.append(article);
    });
    markAll.hidden = !notifications.some(item => !item.leida);
    updateBadge();
  } catch (error) {
    list.replaceChildren();
    const panel = document.createElement("section"); panel.className = "panel";
    const title = document.createElement("h2"); title.textContent = "No se pudieron cargar";
    const detail = document.createElement("p"); detail.textContent = error.message;
    panel.append(title, detail); list.append(panel);
  }}
  markAll?.addEventListener("click", async () => {
    markAll.disabled = true;
    try {
      await AcadionApi.request("/api/me/notificaciones/marcar-todas-leidas", { method: "PATCH" });
      await load();
    } finally { markAll.disabled = false; }
  });
  load();
});
