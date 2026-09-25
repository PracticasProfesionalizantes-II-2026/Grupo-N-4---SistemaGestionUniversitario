// Comun.js: navegación y controles reutilizados entre archivos HTML.
window.Acadion = (() => {
  const STORAGE_ID_KEY = "acadion_usuario_id";
  const studentMenu = [
    ["MenuPrincipal", "Inicio", "⌂"],
    ["InscripcionAMateria", "Inscripción a materias", "▣"],
    ["InscripcionAExamen", "Inscripción a exámenes", "▤"],
    ["MateriasEnCurso", "Reportes", "◫"],
    ["MisDatosPersonales", "Mis datos personales", "♙"]
  ];
  const reportMenu = [
    ["MenuPrincipal", "Inicio", "⌂"],
    ["MateriasEnCurso", "Materias en curso", "◫"],
    ["MateriasAprobadas", "Materias aprobadas", "✓"],
    ["MateriasDesaprobadas", "Materias desaprobadas", "×"],
    ["PromedioYAvance", "Promedio y avance", "◒"],
    ["Inasistencias", "Inasistencias", "⊘"]
  ];
  const adminMenu = [
    ["PanelDirectivo", "Inicio", "⌂"],
    ["CrearCarrera", "Crear carrera", "+"],
    ["GestionCarreras", "Administrar carreras", "▦"],
    ["CrearMateria", "Crear materia", "+"],
    ["GestionMaterias", "Administrar materias", "▤"],
    ["SeleccionarRol", "Crear usuario", "+"],
    ["GestionUsuarios", "Administrar usuarios", "♙"],
    ["AsistenciaProfesores", "Asistencia docentes", "✓"],
    ["GestionPagos", "Matrículas y cuotas", "$"],
    ["GestionNotificaciones", "Notificaciones generales", "♧"]
  ];
  const adminAbsenceMenu = [
    ["PanelDirectivo", "Inicio", "⌂"],
    ["InasistenciasEstudiante", "Inasistencias", "⊘"]
  ];
  const teacherMenu = [
    ["PanelDocente", "Inicio", "⌂"],
    ["DocenteMaterias", "Mis materias", "▤"],
    ["DocenteAsistencias", "Asistencia de alumnos", "✓"],
    ["DocenteEvaluaciones", "Evaluaciones y notas", "▣"],
    ["DocenteNotificaciones", "Notificaciones", "♧"],
    ["DocentePerfil", "Mi perfil", "♙"]
  ];
  const reportPages = new Set([
    "MateriasEnCurso",
    "MateriasAprobadas",
    "MateriasDesaprobadas",
    "PromedioYAvance",
    "Inasistencias"
  ]);
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
  function iniciarPantalla() {
    const page = document.body.dataset.page;
    const isAdmin = document.body.dataset.section === "admin";
    const isTeacher = document.body.dataset.section === "teacher";
    const session = window.AcadionApi?.getSession();
    if (isAdmin && !window.AcadionApi?.requireAdmin()) return;
    if (isTeacher && !window.AcadionApi?.requireTeacher()) return;
    const menu = isAdmin
      ? (page === "InasistenciasEstudiante" ? adminAbsenceMenu : adminMenu)
      : isTeacher ? teacherMenu : reportPages.has(page) ? reportMenu : studentMenu;
    const sidebar = document.getElementById("sidebar");
    if (sidebar) {
      const home = isAdmin ? "PanelDirectivo.html" : isTeacher ? "PanelDocente.html" : "MenuPrincipal.html";
      sidebar.innerHTML = `<a class="logo" href="${home}">Acadion<span>.</span></a><nav class="menu" aria-label="Menú principal"></nav><button class="logout-button" id="logoutButton">Cerrar sesión</button>`;
      const nav = sidebar.querySelector("nav");
      menu.forEach(([file, label, menuIcon]) => {
        const link = document.createElement("a");
        link.href = page === "InasistenciasEstudiante" && file === page
          ? window.location.href
          : file + ".html";
        link.className = "menu-item" + (page === file ? " active" : "");
        if (menuIcon) {
          const icon = document.createElement("span");
          icon.className = "menu-icon";
          icon.setAttribute("aria-hidden", "true");
          icon.textContent = menuIcon;
          link.append(icon, document.createTextNode(label));
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
    if (headerActions && !headerActions.querySelector(".notification-button")) {
      const notifications = document.createElement("button");
      notifications.className = "notification-button";
      notifications.type = "button";
      notifications.setAttribute("aria-label", "Notificaciones");
      notifications.innerHTML = `♧<span class="notification-dot" aria-hidden="true"></span>`;
      notifications.addEventListener("click", () => {
        window.location.href = isAdmin
          ? "GestionNotificaciones.html"
          : isTeacher ? "DocenteNotificaciones.html" : "Notificaciones.html";
      });
      headerActions.prepend(notifications);
    }
    document.getElementById("logoutButton")?.addEventListener("click", () => {
      window.AcadionApi?.clearSession();
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
    document.querySelector(".recovery-form")?.addEventListener("submit", event => {
      event.preventDefault();
      const form = event.target;
      if (form.elements.nueva && form.elements.nueva.value !== form.elements.confirmacion.value) {
        document.getElementById("formMessage").textContent = "Las contraseñas no coinciden.";
        return;
      }
      window.location.href = form.dataset.next;
    });
    document.getElementById("resendCode")?.addEventListener("click", event => {
      const button = event.target;
      button.disabled = true;
      let seconds = 60;
      const timer = setInterval(() => {
        seconds -= 1;
        button.textContent = seconds > 0 ? "Reenviar código (" + seconds + "s)" : "Reenviar código";
        if (seconds <= 0) { clearInterval(timer); button.disabled = false; }
      }, 1000);
      document.getElementById("formMessage").textContent = "Vista de demostración: no se envía ningún correo.";
    });
    document.querySelectorAll("[data-page]").forEach(button => {
      if (button.tagName === "BUTTON") button.disabled = true;
    });
  }
  return { iniciarPantalla, mostrarMensaje, formatearNombre };
})();
