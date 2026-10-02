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

  const states = [["Pendiente", "Pendiente"], ["EnRevision", "En revisión"], ["AlDia", "Aprobado / al día"], ["Rechazado", "Rechazado"], ["Exentado", "Exentado"], ["Vencida", "Vencida"], ["Impaga", "Impaga"]];
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
  async function saveStudent(student, enrollmentSelect, feeSelect, amountInput, methodSelect, button) {
    if (!window.confirm(`¿Guardar la matrícula como “${enrollmentSelect.selectedOptions[0].textContent}” y la cuota como “${feeSelect.selectedOptions[0].textContent}” para ${student.estudiante}?`)) return;
    button.disabled = true;
    try {
      const payload = (state, details = {}) => JSON.stringify({ estado: state, comprobanteUrl: "", importe: 0, metodoPago: "", observaciones: "", ...details });
      await Promise.all([
        AcadionApi.request(`/api/gestion/pagos/${student.id}/matricula`, { method: "PUT", body: payload(enrollmentSelect.value) }),
        AcadionApi.request(`/api/gestion/pagos/${student.id}/cuotas/${yearInput.value}/${monthSelect.value}`, { method: "PUT", body: payload(feeSelect.value, {
          importe: Number(amountInput.value || 0), metodoPago: methodSelect.value
        }) })
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
      const amountCell = document.createElement("td"); const amount = document.createElement("input"); amount.type = "number"; amount.min = "0"; amount.step = "0.01"; amount.className = "payment-input"; amount.value = student.importeCuota || 0; amountCell.append(amount);
      const methodCell = document.createElement("td"); const method = document.createElement("select"); method.className = "payment-select"; ["", "Efectivo", "Transferencia", "Tarjeta", "Beca", "Otro"].forEach(value => { const item = document.createElement("option"); item.value = value; item.textContent = value || "Sin indicar"; method.append(item); }); method.value = student.metodoPagoCuota || ""; methodCell.append(method);
      const receiptCell = document.createElement("td");
      [[student.comprobanteMatricula,"Matrícula",`/api/gestion/pagos/${student.id}/matricula/${yearInput.value}/comprobante`],[student.comprobanteCuota,"Cuota",`/api/gestion/pagos/${student.id}/cuotas/${yearInput.value}/${monthSelect.value}/comprobante`]].forEach(([exists,label,path])=>{if(!exists)return;const link=document.createElement("a");link.className="receipt-link";link.href=AcadionApi.baseUrl+path;link.target="_blank";link.textContent=`Ver ${label.toLowerCase()}`;receiptCell.append(link);});
      if(!receiptCell.childElementCount){const empty=document.createElement("span");empty.className="muted";empty.textContent="Sin comprobante";receiptCell.append(empty);}
      const actionCell = document.createElement("td"); const save = document.createElement("button"); save.className = "save-payment"; save.type = "button"; save.textContent = "Guardar revisión"; save.addEventListener("click", () => saveStudent(student, enrollmentSelect, feeSelect, amount, method, save)); actionCell.append(save);
      row.append(identityCell, enrollmentCell, feeCell, amountCell, methodCell, receiptCell, actionCell); body.append(row);
    });
    if (!students.length) { const row = document.createElement("tr"); const cell = document.createElement("td"); cell.colSpan = 7; cell.className = "empty-state"; cell.textContent = "No hay estudiantes registrados."; row.append(cell); body.append(row); }
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
