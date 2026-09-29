document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const yearInput = document.getElementById("paymentYear");
  const monthSelect = document.getElementById("paymentMonth");
  const body = document.getElementById("paymentsBody");
  const status = document.getElementById("paymentsStatus");
  const now = new Date();
  yearInput.value = now.getFullYear();
  ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"].forEach((name, index) => {
    const option = document.createElement("option"); option.value = String(index + 1); option.textContent = name; monthSelect.append(option);
  });
  monthSelect.value = String(now.getMonth() + 1);

  const states = [["Pendiente", "Pendiente"], ["AlDia", "Pagada / al día"], ["Exentado", "Exentado"], ["Vencida", "Vencida"], ["Impaga", "Impaga"]];
  function stateSelect(value) {
    const select = document.createElement("select"); select.className = "payment-select";
    states.forEach(([key, label]) => { const option = document.createElement("option"); option.value = key; option.textContent = label; select.append(option); });
    select.value = value || "Pendiente";
    return select;
  }
  function photoUrl(value, name) {
    if (value) return value.startsWith("/") ? AcadionApi.baseUrl + value : value;
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=ff7a00&color=fff&bold=true`;
  }
  async function saveStudent(student, enrollmentSelect, feeSelect, button) {
    button.disabled = true;
    try {
      const payload = state => JSON.stringify({ estado: state, comprobanteUrl: "" });
      await Promise.all([
        AcadionApi.request(`/api/gestion/pagos/${student.id}/matricula`, { method: "PUT", body: payload(enrollmentSelect.value) }),
        AcadionApi.request(`/api/gestion/pagos/${student.id}/cuotas/${yearInput.value}/${monthSelect.value}`, { method: "PUT", body: payload(feeSelect.value) })
      ]);
      status.className = "status-message success";
      status.textContent = `Se actualizaron los pagos de ${student.estudiante}.`;
    } catch (error) {
      status.className = "status-message error";
      status.textContent = error.message || "No fue posible actualizar el pago.";
    } finally { button.disabled = false; }
  }
  function render(students) {
    body.replaceChildren();
    students.forEach(student => {
      const row = document.createElement("tr");
      const identityCell = document.createElement("td");
      const identity = document.createElement("div"); identity.className = "student-identity";
      const image = document.createElement("img"); image.className = "student-photo"; image.src = photoUrl(student.fotoPerfilUrl, student.estudiante); image.alt = "";
      const copy = document.createElement("div"); const strong = document.createElement("strong"); strong.textContent = student.estudiante; const small = document.createElement("small"); small.textContent = `Legajo ${student.legajo || "—"}`; copy.append(strong, small); identity.append(image, copy); identityCell.append(identity);
      const enrollmentCell = document.createElement("td"); const enrollmentSelect = stateSelect(student.estadoMatricula); enrollmentCell.append(enrollmentSelect);
      const feeCell = document.createElement("td"); const feeSelect = stateSelect(student.estadoCuota); feeCell.append(feeSelect);
      const actionCell = document.createElement("td"); const save = document.createElement("button"); save.className = "save-payment"; save.type = "button"; save.textContent = "Guardar"; save.addEventListener("click", () => saveStudent(student, enrollmentSelect, feeSelect, save)); actionCell.append(save);
      row.append(identityCell, enrollmentCell, feeCell, actionCell); body.append(row);
    });
    if (!students.length) { const row = document.createElement("tr"); const cell = document.createElement("td"); cell.colSpan = 4; cell.className = "empty-state"; cell.textContent = "No hay estudiantes registrados."; row.append(cell); body.append(row); }
  }
  async function load() {
    status.className = "status-message"; status.textContent = "Cargando…";
    try {
      const students = await AcadionApi.request(`/api/gestion/pagos?anio=${yearInput.value}&mes=${monthSelect.value}`);
      render(Array.isArray(students) ? students : []); status.textContent = "";
    } catch (error) { status.className = "status-message error"; status.textContent = error.message || "No fue posible cargar los pagos."; }
  }
  document.getElementById("loadPayments").addEventListener("click", load);
  load();
});
