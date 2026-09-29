document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const form = document.getElementById("userForm");
  if (!form) return;

  const roleId = Number(document.body.dataset.roleId);
  [form.nombre, form.apellido, form.localidad, form.especialidad, form.tituloAcademico]
    .filter(Boolean)
    .forEach(input => input.addEventListener("input", () => {
      input.value = input.value.replace(/[!"#$%]/g, "");
    }));
  const message = document.getElementById("formMessage");
  const result = document.getElementById("credentialResult");
  const button = form.querySelector("button[type='submit']");
  const careerSelect = document.getElementById("studentCareer");
  let careers = [];

  if (careerSelect) {
    AcadionApi.request("/carreras/").then(data => {
      careers = data;
      data.forEach(career => {
        const available = career.estudiantesInscriptos < career.capacidadMaximaEstudiantes;
        const item = document.createElement("option");
        item.value = career.idCarrera;
        item.disabled = !available;
        item.textContent = `${career.nombre} · ${career.planEstudios} (${career.estudiantesInscriptos}/${career.capacidadMaximaEstudiantes})${available ? "" : " · sin cupo"}`;
        careerSelect.append(item);
      });
    }).catch(error => {
      message.className = "status-message error";
      message.textContent = `No se pudieron cargar las carreras: ${error.message}`;
      button.disabled = true;
    });
    careerSelect.addEventListener("change", () => {
      const selected = careers.find(c => c.idCarrera === Number(careerSelect.value));
      document.getElementById("careerCapacity").textContent = selected
        ? `Cupo disponible: ${selected.capacidadMaximaEstudiantes - selected.estudiantesInscriptos} de ${selected.capacidadMaximaEstudiantes}.`
        : "Seleccioná la carrera a la que pertenecerá el estudiante.";
    });
  }

  form.addEventListener("submit", async event => {
    event.preventDefault();
    message.className = "status-message";
    message.textContent = "";
    result.hidden = true;
    const personName = /^[\p{L}\p{M}]+(?:[ -][\p{L}\p{M}]+)*$/u;
    const academicText = /^[\p{L}\p{M}\p{N} .()/\-]+$/u;
    const fields = form.elements;
    const invalidIdentity = !personName.test(fields.nombre.value.trim()) ||
      !personName.test(fields.apellido.value.trim()) ||
      !academicText.test(fields.localidad.value.trim());
    const invalidTeacherProfile = roleId === 2 &&
      (!academicText.test(fields.especialidad.value.trim()) ||
       !academicText.test(fields.tituloAcademico.value.trim()));
    if (invalidIdentity || invalidTeacherProfile) {
      message.className = "status-message error";
      message.textContent = "Los nombres y datos académicos contienen caracteres no permitidos.";
      return;
    }
    button.disabled = true;
    button.textContent = "Creando cuenta...";

    const payload = {
      nombre: fields.nombre.value.trim(), apellido: fields.apellido.value.trim(),
      dni: Number(fields.dni.value), fechaNacimiento: fields.fechaNacimiento.value,
      direccion: fields.direccion.value.trim(), localidad: fields.localidad.value.trim(),
      codigoPostal: Number(fields.codigoPostal.value),
      emailPersonal: fields.emailPersonal.value.trim(),
      emailInstitucional: fields.emailInstitucional.value.trim(),
      telefonoContacto: fields.telefonoContacto.value.trim(), rolId: roleId,
      carreraId: fields.carreraId?.value ? Number(fields.carreraId.value) : null,
      estadoMatriculaInicial: fields.estadoMatriculaInicial?.value || "PENDIENTE",
      estadoCuotaActual: fields.estadoCuotaActual?.value || "PENDIENTE",
      especialidad: fields.especialidad?.value.trim() || "",
      tituloAcademico: fields.tituloAcademico?.value.trim() || "", materias: []
    };

    try {
      const created = await AcadionApi.request("/api/gestion/usuarios", { method: "POST", body: JSON.stringify(payload) });
      document.getElementById("createdUsername").textContent = created.nombreUsuario;
      document.getElementById("createdPassword").textContent = created.passwordInicial;
      document.getElementById("createdRole").textContent = created.rol;
      const legajo = document.getElementById("createdStudentFile");
      if (legajo) legajo.textContent = created.legajo || "—";
      result.hidden = false;
      message.className = "status-message success";
      message.textContent = "La cuenta se creó correctamente.";
      form.reset();
      result.scrollIntoView({ behavior: "smooth", block: "center" });
    } catch (error) {
      message.className = "status-message error";
      message.textContent = error.message;
    } finally {
      button.disabled = false;
      button.textContent = "Crear cuenta";
    }
  });
});
