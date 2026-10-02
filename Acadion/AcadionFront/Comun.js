// Comun.js: navegación y controles reutilizados entre archivos HTML.
window.Acadion = (() => {
  const STORAGE_ID_KEY = "acadion_usuario_id";
  const studentMenu = [
    ["MenuPrincipal", "Inicio", "⌂"],
    ["InscripcionAMateria", "Inscripción a materias", "▣"],
    ["InscripcionAExamen", "Inscripción a exámenes", "▤"],
    ["CalendarioPersonal", "Calendario académico", "□"],
    ["MisPagos", "Comprobantes de pago", "$"],
    ["MateriasEnCurso", "Reportes", "◫"],
    ["MisDatosPersonales", "Mis datos personales", "♙"]
  ];
  const reportMenu = [
    ["MenuPrincipal", "Inicio", "⌂"],
    ["MateriasEnCurso", "Cursada actual", "◫"],
    ["MateriasAprobadas", "Materias aprobadas", "✓"],
    ["MateriasDesaprobadas", "Materias desaprobadas", "×"],
    ["NotasFinales", "Notas finales", "▤"],
    ["PromedioYAvance", "Promedio y avance", "◒"],
    ["Inasistencias", "Inasistencias", "⊘"]
  ];
  const adminMenu = [
    ["PanelSecretaria", "Inicio", "⌂"],
    ["CrearCarrera", "Crear carrera", "+"],
    ["GestionCarreras", "Administrar carreras", "▦"],
    ["CrearMateria", "Crear materia", "+"],
    ["GestionMaterias", "Administrar materias", "▤"],
    ["SeleccionarRol", "Crear usuario", "+"],
    ["GestionUsuarios", "Administrar usuarios", "♙"],
    ["AsistenciaProfesores", "Asistencia docentes", "✓"],
    ["GestionPagos", "Matrículas y cuotas", "$"],
    ["CalendarioAcademico", "Calendario académico", "□"],
    ["GestionNotificaciones", "Notificaciones generales", "♧"],
    ["HistorialActividad", "Historial de actividad", "◫"]
  ];
  const adminAbsenceMenu = [
    ["PanelSecretaria", "Inicio", "⌂"],
    ["InasistenciasEstudiante", "Inasistencias", "⊘"]
  ];
  const adminEquivalenceMenu = [
    ["PanelSecretaria", "Inicio", "⌂"],
    ["EquivalenciasEstudiante", "Equivalencias", "✓"]
  ];
  const teacherMenu = [
    ["PanelDocente", "Inicio", "⌂"],
    ["DocenteMaterias", "Mis materias", "▤"],
    ["DocenteAsistencias", "Asistencia de alumnos", "✓"],
    ["DocenteEvaluaciones", "Evaluaciones y notas", "▣"],
    ["CalendarioPersonal", "Calendario académico", "□"],
    ["DocenteNotificaciones", "Notificaciones", "♧"],
    ["DocentePerfil", "Mi perfil", "♙"]
  ];
  const directorMenu = [
    ["PanelInstitucional", "Panel institucional", "▦"],
    ["AjustesDirectivo", "Ajustes", "⚙"]
  ];
  const reportPages = new Set([
    "MateriasEnCurso",
    "MateriasAprobadas",
    "MateriasDesaprobadas",
    "NotasFinales",
    "PromedioYAvance",
    "Inasistencias"
  ]);
  const iconPaths = {
    home: "M3 10.5 12 3l9 7.5V21h-6v-6H9v6H3z",
    plus: "M12 5v14M5 12h14",
    book: "M4 4h12a4 4 0 0 1 4 4v12H8a4 4 0 0 0-4 0zm0 0v16m4-12h8m-8 4h8",
    users: "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2m7-10a4 4 0 1 0 0-8 4 4 0 0 0 0 8m13 10v-2a4 4 0 0 0-3-3.87m-2-12a4 4 0 0 1 0 7.75",
    check: "m5 12 4 4L19 6",
    chart: "M4 20V10m6 10V4m6 16v-7m6 7H2",
    bell: "M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9m-8 13h4",
    money: "M12 2v20m5-16.5C16 4.5 14 4 12 4 9 4 7 5.5 7 8s2 3.5 5 4 5 1.5 5 4-2 4-5 4c-2 0-4-.5-5-1.5",
    user: "M20 21a8 8 0 0 0-16 0m8-10a4 4 0 1 0 0-8 4 4 0 0 0 0 8",
    warning: "M12 9v4m0 4h.01M10.3 3.8 2.4 18a2 2 0 0 0 1.75 3h15.7a2 2 0 0 0 1.75-3L13.7 3.8a2 2 0 0 0-3.4 0",
    settings: "M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7m7.4-3.5a7.5 7.5 0 0 0-.08-1l2.02-1.58-2-3.46-2.5 1a8 8 0 0 0-1.72-1L14.75 1h-4l-.37 2.96a8 8 0 0 0-1.72 1l-2.5-1-2 3.46L6.18 9a7.5 7.5 0 0 0 0 2L4.16 12.58l2 3.46 2.5-1a8 8 0 0 0 1.72 1l.37 2.96h4l.37-2.96a8 8 0 0 0 1.72-1l2.5 1 2-3.46z"
  };
  function menuIconFor(file) {
    if (/^Panel|MenuPrincipal/.test(file)) return "home";
    if (/^Crear|SeleccionarRol/.test(file)) return "plus";
    if (/Usuario|Perfil|Datos/.test(file)) return "users";
    if (/Asistencia|Inasistencia/.test(file)) return "check";
    if (/Pago|Matricula/.test(file)) return "money";
    if (/Notificacion/.test(file)) return "bell";
    if (/Promedio/.test(file)) return "chart";
    if (/Ajustes/.test(file)) return "settings";
    if (/Desaprobada/.test(file)) return "warning";
    return "book";
  }
  function createMenuIcon(file) {
    const namespace = "http://www.w3.org/2000/svg";
    const wrapper = document.createElement("span"); wrapper.className = "menu-icon"; wrapper.setAttribute("aria-hidden", "true");
    const svg = document.createElementNS(namespace, "svg"); svg.setAttribute("viewBox", "0 0 24 24"); svg.setAttribute("fill", "none"); svg.setAttribute("stroke", "currentColor"); svg.setAttribute("stroke-width", "1.8"); svg.setAttribute("stroke-linecap", "round"); svg.setAttribute("stroke-linejoin", "round");
    const path = document.createElementNS(namespace, "path"); path.setAttribute("d", iconPaths[menuIconFor(file)]); svg.append(path); wrapper.append(svg); return wrapper;
  }
  function createNotificationGlyph() {
    const namespace = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(namespace, "svg");
    svg.classList.add("notification-glyph");
    svg.setAttribute("viewBox", "0 0 24 24");
    svg.setAttribute("fill", "none");
    svg.setAttribute("stroke", "currentColor");
    svg.setAttribute("stroke-width", "1.8");
    svg.setAttribute("stroke-linecap", "round");
    svg.setAttribute("stroke-linejoin", "round");
    const path = document.createElementNS(namespace, "path");
    path.setAttribute("d", iconPaths.bell);
    svg.append(path);
    return svg;
  }
  function formatearNombre(value) {
    return String(value || "").trim().toLocaleLowerCase("es-AR")
      .replace(/(^|[\s'-])([a-záéíóúüñ])/g, (_, separator, letter) =>
        separator + letter.toLocaleUpperCase("es-AR"));
  }
  function mostrarMensaje(message) {
    const dialog = document.getElementById("statusDialog");
    if (!dialog) return;
    document.getElementById("dialogMessage").textContent = message;
    dialog.showModal();
  }
  function configurarValidacionFormularios() {
    if (document.documentElement.dataset.validationReady === "true") return;
    document.documentElement.dataset.validationReady = "true";

    const claveCampo = campo => String(campo.name || campo.id || "").toLowerCase();
    const esNombrePersona = campo => {
      const clave = claveCampo(campo);
      const formulario = campo.form?.id || "";
      return clave === "apellido" || clave === "nombreapellido" || /^nombre\d+$/.test(clave) ||
        (clave === "nombre" && ["userForm", "editUserForm"].includes(formulario));
    };
    const esTextoAcademico = campo => {
      const clave = claveCampo(campo);
      return ["nombre", "localidad", "especialidad", "tituloacademico", "planestudios", "nombreanio"]
        .includes(clave) && !esNombrePersona(campo);
    };
    const establecerMaximo = (campo, maximo) => {
      if (!campo.hasAttribute("maxlength")) campo.maxLength = maximo;
    };
    const aplicarReglas = campo => {
      if (!(campo instanceof HTMLInputElement || campo instanceof HTMLTextAreaElement)) return;
      const clave = claveCampo(campo);
      if (campo.type === "password") {
        establecerMaximo(campo, 256);
        if (["nueva", "confirmacion", "passwordnueva"].includes(clave)) campo.minLength = 10;
        return;
      }
      if (campo.type === "email" || clave.includes("email") || clave === "gmail") {
        establecerMaximo(campo, 254);
        return;
      }
      if (campo.type === "tel" || clave.includes("telefono") || clave === "celular") {
        establecerMaximo(campo, 30);
        campo.inputMode = "tel";
        campo.dataset.validationType = "phone";
        return;
      }
      if (clave === "dni") {
        campo.inputMode = "numeric";
        campo.min = "1000000";
        campo.max = "99999999";
        return;
      }
      if (["codigopostal", "postalcode", "código postal"].includes(clave)) {
        campo.inputMode = "numeric";
        if (!campo.min) campo.min = "0";
        campo.max = "99999999";
        return;
      }
      if (esNombrePersona(campo)) {
        establecerMaximo(campo, 80);
        campo.dataset.validationType = "person-name";
        return;
      }
      if (clave === "nombreusuario") {
        establecerMaximo(campo, 80);
        campo.dataset.validationType = "username";
        return;
      }
      if (esTextoAcademico(campo)) {
        establecerMaximo(campo, 150);
        campo.dataset.validationType = "academic";
        return;
      }
      if (campo instanceof HTMLTextAreaElement) establecerMaximo(campo, 2000);
      else establecerMaximo(campo, 300);
    };
    const limpiarValor = campo => {
      const tipo = campo.dataset.validationType;
      if (!tipo) return;
      if (tipo === "person-name") {
        campo.value = campo.value.replace(/[^\p{L}\p{M} -]/gu, "");
      } else if (tipo === "academic") {
        campo.value = campo.value.replace(/[^\p{L}\p{M}\p{N} .()/\-]/gu, "");
      } else if (tipo === "phone") {
        campo.value = campo.value.replace(/[^0-9+() -]/g, "");
      } else if (tipo === "username") {
        campo.value = campo.value.replace(/[^a-zA-Z0-9.]/g, "");
      }
    };

    document.querySelectorAll("input, textarea").forEach(aplicarReglas);
    document.addEventListener("focusin", event => aplicarReglas(event.target));
    document.addEventListener("input", event => {
      aplicarReglas(event.target);
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement)
        limpiarValor(event.target);
    });
  }
  function iniciarPantalla() {
    configurarValidacionFormularios();
    const page = document.body.dataset.page;
    const isAdmin = document.body.dataset.section === "admin";
    const isTeacher = document.body.dataset.section === "teacher";
    const isDirector = document.body.dataset.section === "director";
    const session = window.AcadionApi?.getSession();
    if (!isAdmin && !isTeacher && !isDirector && session?.rol !== "Estudiante") {
      const target = session?.rol === "Directivo"
        ? "PanelInstitucional.html"
        : session?.rol === "Docente" ? "PanelDocente.html" : "PanelSecretaria.html";
      window.location.replace(target);
      return;
    }
    if (isAdmin && !window.AcadionApi?.requireAdmin()) return;
    if (isTeacher && !window.AcadionApi?.requireTeacher()) return;
    if (isDirector && !window.AcadionApi?.requireDirector()) return;
    const menu = isAdmin
      ? (page === "InasistenciasEstudiante" ? adminAbsenceMenu : page === "EquivalenciasEstudiante" ? adminEquivalenceMenu : adminMenu)
      : isTeacher ? teacherMenu : isDirector ? directorMenu : reportPages.has(page) ? reportMenu : studentMenu;
    const sidebar = document.getElementById("sidebar");
    if (sidebar) {
      const home = isAdmin ? "PanelSecretaria.html" : isTeacher ? "PanelDocente.html" : isDirector ? "PanelInstitucional.html" : "MenuPrincipal.html";
      sidebar.innerHTML = `<a class="logo" href="${home}">Acadion<span>.</span></a><nav class="menu" aria-label="Menú principal"></nav><button class="logout-button" id="logoutButton">Cerrar sesión</button>`;
      const nav = sidebar.querySelector("nav");
      menu.forEach(([file, label, menuIcon]) => {
        const link = document.createElement("a");
        link.href = page === "InasistenciasEstudiante" && file === page
          ? window.location.href
          : file + ".html";
        link.className = "menu-item" + (page === file ? " active" : "");
        if (menuIcon) {
          link.append(createMenuIcon(file), document.createTextNode(label));
        } else {
          link.textContent = label;
        }
        if (page === file) link.setAttribute("aria-current", "page");
        nav.appendChild(link);
      });
    }
    if (document.body.dataset.section === "recovery") {
      document.querySelector(".topbar")?.remove();
    }

    if (isAdmin) {
      document.querySelector(".header-actions")?.remove();
    }

    document.querySelectorAll(".back-button").forEach(button => {
      button.addEventListener("click", event => {
        event.preventDefault();
        window.history.back();
      });
    });
    document.querySelectorAll(".table-wrapper").forEach(wrapper => {
      wrapper.tabIndex = 0;
      wrapper.setAttribute("role", "region");
      wrapper.setAttribute("aria-label", "Tabla desplazable horizontalmente");
    });

    const profileLinks = [...document.querySelectorAll(".profile-avatar")];
    const profileTarget = isTeacher ? "DocentePerfil.html" : "MisDatosPersonales.html";
    const setProfileImage = (link, name, photoUrl = "") => {
      link.href = profileTarget;
      link.replaceChildren();
      if (photoUrl) {
        const image = document.createElement("img");
        image.className = "profile-avatar-image";
        image.src = photoUrl;
        image.alt = `Foto de perfil de ${name}`;
        link.append(image);
      } else {
        const initials = document.createElement("span");
        initials.className = "profile-avatar-initials";
        initials.textContent = String(name || "U").split(/\s+/).filter(Boolean).slice(0, 2)
          .map(part => part[0]).join("").toUpperCase();
        initials.setAttribute("aria-label", `Perfil de ${name}`);
        link.append(initials);
      }
    };
    const fallbackProfileName = session?.nombreUsuario || (isTeacher ? "Docente" : "Estudiante");
    profileLinks.forEach(link => setProfileImage(link, fallbackProfileName));
    if (profileLinks.length && window.AcadionApi?.request) {
      window.AcadionApi.request("/api/me/perfil")
        .then(profile => {
          const name = [profile.nombre, profile.apellido].filter(Boolean).join(" ") || fallbackProfileName;
          const photoUrl = profile.fotoPerfilUrl || "";
          profileLinks.forEach(link => setProfileImage(link, name, photoUrl));
        })
        .catch(() => {});
    }

    const headerActions = document.querySelector(".header-actions");
    if (headerActions) {
      let notifications = headerActions.querySelector(".notification-button");
      if (!notifications) {
        notifications = document.createElement("button");
        notifications.className = "notification-button";
        notifications.type = "button";
        notifications.setAttribute("aria-label", "Notificaciones");
        headerActions.prepend(notifications);
      }
      let dot = notifications.querySelector(".notification-dot");
      if (!dot) {
        dot = document.createElement("span");
        dot.className = "notification-dot";
        dot.setAttribute("aria-hidden", "true");
      }
      notifications.replaceChildren(createNotificationGlyph(), dot);
      notifications.addEventListener("click", () => {
        window.location.href = isAdmin
          ? "GestionNotificaciones.html"
          : isTeacher ? "DocenteNotificaciones.html" : "Notificaciones.html";
      });
      window.AcadionApi?.request("/api/me/notificaciones/no-leidas")
        .then(result => {
          const count = Number(result.cantidad || 0);
          notifications.setAttribute("aria-label", count ? `Notificaciones: ${count} sin leer` : "Notificaciones: ninguna sin leer");
          const badge = notifications.querySelector(".notification-dot");
          if (badge) {
            badge.hidden = count === 0;
            badge.textContent = count > 9 ? "9+" : count ? String(count) : "";
          }
        }).catch(() => {});
    }
    document.getElementById("logoutButton")?.addEventListener("click", async () => {
      await window.AcadionApi?.logout();
      [localStorage, sessionStorage].forEach(storage => {
        storage.removeItem(STORAGE_ID_KEY);
        storage.removeItem("acadion_token");
        storage.removeItem("acadion_session");
      });
      window.location.href = "Login.html";
    });
    document.getElementById("closeDialog")?.addEventListener("click", () => document.getElementById("statusDialog").close());
    document.getElementById("tableSearch")?.addEventListener("input", event => {
      const searchTerm = event.target.value.trim().toLowerCase();
      document.querySelectorAll("tbody tr").forEach(row => { row.hidden = !row.textContent.toLowerCase().includes(searchTerm); });
    });
    document.querySelectorAll(".local-form").forEach(form => {
      form.addEventListener("submit", event => {
        event.preventDefault();
        const values = {};
        new FormData(form).forEach((value, key) => { values[key] = value; });
        localStorage.setItem("acadion_borrador_" + page, JSON.stringify(values));
        mostrarMensaje("Borrador guardado en este navegador. Esta operación todavía no modifica la API.");
      });
    });
    document.querySelectorAll("[data-page]").forEach(button => {
      if (button.tagName === "BUTTON") button.disabled = true;
    });
  }
  return { iniciarPantalla, mostrarMensaje, formatearNombre };
})();
