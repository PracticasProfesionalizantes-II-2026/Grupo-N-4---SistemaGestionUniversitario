document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const body = document.getElementById("auditBody");
  const message = document.getElementById("auditMessage");
  const info = document.getElementById("pageInfo");
  const totalLabel = document.getElementById("auditTotal");
  let page = 1;
  let total = 0;
  const size = 40;

  const formatDate = value => new Intl.DateTimeFormat("es-AR", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit", second: "2-digit"
  }).format(new Date(value));

  function friendlyDetail(item) {
    const detail = String(item.detalle || "").trim();
    if (detail && !["Operación completada", "Operación rechazada o fallida"].includes(detail)) return detail;
    const path = String(item.ruta || "");
    if (path.includes("/auth/logout")) return "Finalizó su sesión en Acadion.";
    if (path.includes("/notas")) return "Actualizó una calificación académica.";
    if (path.includes("/asistencias")) return "Actualizó un registro de asistencia.";
    if (path.includes("/pagos")) return "Actualizó información de pagos.";
    if (path.includes("/usuarios")) return "Actualizó la información de un usuario.";
    if (path.includes("/materias")) return "Actualizó información académica de una materia.";
    if (path.includes("/carreras")) return "Actualizó información de una carrera.";
    return "La operación se procesó correctamente.";
  }

  function appendCell(row, text, className = "") {
    const cell = row.insertCell();
    cell.textContent = text || "—";
    if (className) cell.className = className;
    return cell;
  }

  async function load() {
    message.className = "status-message";
    message.textContent = "Cargando historial…";
    const params = new URLSearchParams({ pagina: String(page), cantidad: String(size) });
    const from = document.getElementById("dateFrom").value;
    const to = document.getElementById("dateTo").value;
    const user = document.getElementById("userFilter").value.trim();
    const action = document.getElementById("actionFilter").value;
    if (from) params.set("desde", from);
    if (to) params.set("hasta", `${to}T23:59:59`);
    if (user) params.set("usuario", user);
    if (action) params.set("accion", action);

    try {
      const result = await AcadionApi.request(`/api/auditoria?${params}`);
      total = Number(result.total || 0);
      body.replaceChildren();
      (result.registros || []).forEach(item => {
        const row = body.insertRow();
        appendCell(row, formatDate(item.fechaUtc), "audit-date");
        appendCell(row, item.nombreUsuario || "Sistema");
        appendCell(row, item.rol || "—");
        appendCell(row, item.accion, "audit-action");
        const outcome = appendCell(row, item.resultado, `audit-result ${item.resultado === "Correcto" ? "ok" : "fail"}`);
        outcome.setAttribute("aria-label", `Resultado: ${item.resultado}`);
        appendCell(row, friendlyDetail(item), "audit-detail");
      });
      if (!result.registros?.length) {
        const row = body.insertRow();
        const cell = row.insertCell();
        cell.colSpan = 6;
        cell.className = "empty-state";
        cell.textContent = "No hay actividad para los filtros seleccionados.";
      }
      const pages = Math.max(1, Math.ceil(total / size));
      info.textContent = `Página ${page} de ${pages}`;
      totalLabel.textContent = `${total} ${total === 1 ? "registro" : "registros"}`;
      document.getElementById("previousPage").disabled = page <= 1;
      document.getElementById("nextPage").disabled = page >= pages;
      message.textContent = "";
    } catch (error) {
      message.className = "status-message error";
      message.textContent = error.message;
    }
  }

  document.getElementById("searchAudit").addEventListener("click", () => { page = 1; load(); });
  document.getElementById("previousPage").addEventListener("click", () => { if (page > 1) { page--; load(); } });
  document.getElementById("nextPage").addEventListener("click", () => { if (page * size < total) { page++; load(); } });
  load();
});
