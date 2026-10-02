document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const estudianteId = Number(new URLSearchParams(location.search).get("usuarioId"));
  const form = document.getElementById("equivalenceForm");
  const body = document.getElementById("equivalencesBody");
  const message = document.getElementById("equivalenceMessage");
  form.cicloLectivo.value = new Date().getFullYear();

  function option(value, label) { const item = document.createElement("option"); item.value = value; item.textContent = label; return item; }
  function showMessage(text, success = false) { message.textContent = text; message.className = `status-message ${success ? "success" : "error"}`; }
  function empty(text) { body.replaceChildren(); const row = body.insertRow(); const cell = row.insertCell(); cell.colSpan = 4; cell.className = "empty-state"; cell.textContent = text; }

  async function load() {
    if (!estudianteId) { empty("No se indicó qué estudiante se debe consultar."); form.hidden = true; return; }
    try {
      const [candidates, equivalences] = await Promise.all([
        AcadionApi.request(`/api/equivalencias/estudiante/${estudianteId}/candidatas`),
        AcadionApi.request(`/api/equivalencias/estudiante/${estudianteId}`)
      ]);
      document.getElementById("studentSummary").textContent = `${candidates.estudiante.nombre} · ${candidates.estudiante.legajo || "Sin legajo"} · ${candidates.estudiante.carrera} · ${candidates.estudiante.plan}`;
      form.materiaOrigenId.replaceChildren(option("", "Seleccionar materia"));
      form.materiaDestinoId.replaceChildren(option("", "Seleccionar materia"));
      candidates.materiasOrigen.forEach(item => form.materiaOrigenId.append(option(item.id, `${item.nombre} · ${item.carrera} · ${item.plan}`)));
      candidates.materiasDestino.forEach(item => form.materiaDestinoId.append(option(item.id, `${item.nombre} · ${item.anio}.º año`)));
      body.replaceChildren();
      if (!equivalences.length) { empty("Todavía no se otorgaron equivalencias a este estudiante."); return; }
      equivalences.forEach(item => {
        const row = body.insertRow();
        row.insertCell().textContent = `${item.materiaOrigen} · ${item.carreraOrigen}`;
        row.insertCell().textContent = `${item.materiaDestino} · ${item.carreraDestino}`;
        row.insertCell().textContent = new Intl.DateTimeFormat("es-AR").format(new Date(item.fechaOtorgamientoUtc));
        row.insertCell().textContent = item.observaciones || "Sin observaciones";
      });
    } catch (error) { empty(error.message); form.hidden = true; }
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    if (!confirm("¿Otorgar esta equivalencia y agregarla al historial académico?")) return;
    try {
      await AcadionApi.request("/api/equivalencias/", { method: "POST", body: JSON.stringify({
        estudianteId, materiaOrigenId: Number(form.materiaOrigenId.value), materiaDestinoId: Number(form.materiaDestinoId.value),
        cicloLectivo: Number(form.cicloLectivo.value), observaciones: form.observaciones.value.trim()
      }) });
      showMessage("La equivalencia se otorgó correctamente.", true); await load();
    } catch (error) { showMessage(error.message); }
  });
  load();
});
