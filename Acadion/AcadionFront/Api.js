window.AcadionApi = (() => {
  // La cookie de sesión pertenece al host que responde el login. En desarrollo
  // conservamos el mismo host del navegador (localhost o 127.0.0.1) para evitar
  // que el login se acepte pero la pantalla siguiente pierda la autenticación.
  const localHosts = new Set(["localhost", "127.0.0.1", "::1"]);
  const isLocal = localHosts.has(window.location.hostname);
  const apiPorts = new Set(["5050", "7181"]);
  const isDevelopmentPreview = window.location.protocol === "file:" ||
    (isLocal && !apiPorts.has(window.location.port));
  if (isDevelopmentPreview) {
    const currentPage = window.location.pathname.split("/").pop() || "Login.html";
    window.location.replace(
      `http://localhost:5050/${encodeURIComponent(currentPage)}${window.location.search}${window.location.hash}`
    );
  }
  const normalizedHost = window.location.hostname === "::1"
    ? "[::1]"
    : window.location.hostname;
  const baseUrl = isLocal
    ? (apiPorts.has(window.location.port)
      ? window.location.origin
      : `http://${normalizedHost}:5050`)
    : window.location.origin;
  const sessionKey = "acadion_session";
  const mutationMethods = new Set(["POST", "PUT", "PATCH", "DELETE"]);

  function ensureFeedbackRegion() {
    let region = document.getElementById("acadion-feedback-region");
    if (region) return region;
    if (!document.body) return null;

    if (!document.getElementById("acadion-feedback-styles")) {
      const styles = document.createElement("style");
      styles.id = "acadion-feedback-styles";
      styles.textContent = `
        #acadion-feedback-region {
          position: fixed;
          top: 18px;
          right: 18px;
          z-index: 2147483646;
          display: grid;
          width: min(390px, calc(100vw - 32px));
          gap: 10px;
          pointer-events: none;
        }
        .acadion-feedback {
          display: grid;
          grid-template-columns: 28px minmax(0, 1fr) 28px;
          align-items: start;
          gap: 10px;
          padding: 14px 12px;
          border: 1px solid #d9d9d9;
          border-left: 5px solid #16803c;
          border-radius: 12px;
          background: #fff;
          color: #151515;
          box-shadow: 0 12px 32px #0002;
          pointer-events: auto;
          animation: acadion-feedback-in .18s ease-out;
        }
        .acadion-feedback--error { border-left-color: #b42318; }
        .acadion-feedback__icon {
          display: grid;
          width: 26px;
          height: 26px;
          place-items: center;
          border-radius: 50%;
          background: #ddf5e5;
          color: #126c33;
          font-size: 16px;
          font-weight: 900;
        }
        .acadion-feedback--error .acadion-feedback__icon {
          background: #fde7e5;
          color: #b42318;
        }
        .acadion-feedback__copy { min-width: 0; }
        .acadion-feedback__title {
          display: block;
          margin-bottom: 3px;
          font-size: 14px;
          line-height: 1.2;
        }
        .acadion-feedback__message {
          margin: 0;
          color: #4b4b4b;
          font-size: 13px;
          line-height: 1.35;
          overflow-wrap: anywhere;
        }
        .acadion-feedback__close {
          display: grid;
          width: 28px;
          height: 28px;
          padding: 0;
          place-items: center;
          border: 0;
          border-radius: 50%;
          background: transparent;
          color: #555;
          font-size: 20px;
          line-height: 1;
          cursor: pointer;
        }
        .acadion-feedback__close:hover { background: #f0f0f0; }
        @keyframes acadion-feedback-in {
          from { opacity: 0; transform: translateY(-8px); }
          to { opacity: 1; transform: translateY(0); }
        }
        @media (max-width: 600px) {
          #acadion-feedback-region { top: 10px; right: 10px; width: calc(100vw - 20px); }
        }
      `;
      document.head.append(styles);
    }

    region = document.createElement("div");
    region.id = "acadion-feedback-region";
    region.setAttribute("aria-live", "polite");
    region.setAttribute("aria-atomic", "true");
    document.body.append(region);
    return region;
  }

  function showFeedback(message, { type = "success", title, duration = 4800 } = {}) {
    const text = String(message || "").trim();
    if (!text) return null;
    const region = ensureFeedbackRegion();
    if (!region) {
      document.addEventListener("DOMContentLoaded", () => showFeedback(text, { type, title, duration }), { once: true });
      return null;
    }

    const key = `${type}|${text}`;
    const duplicate = [...region.children].find(item => item.dataset.feedbackKey === key);
    if (duplicate) {
      window.clearTimeout(duplicate.dismissTimer);
      duplicate.dismissTimer = window.setTimeout(() => duplicate.remove(), duration);
      return duplicate;
    }

    const toast = document.createElement("section");
    toast.className = `acadion-feedback${type === "error" ? " acadion-feedback--error" : ""}`;
    toast.dataset.feedbackKey = key;
    toast.setAttribute("role", type === "error" ? "alert" : "status");

    const icon = document.createElement("span");
    icon.className = "acadion-feedback__icon";
    icon.setAttribute("aria-hidden", "true");
    icon.textContent = type === "error" ? "!" : "✓";

    const copy = document.createElement("div");
    copy.className = "acadion-feedback__copy";
    const heading = document.createElement("strong");
    heading.className = "acadion-feedback__title";
    heading.textContent = title || (type === "error" ? "No se pudo completar" : "Cambio guardado");
    const paragraph = document.createElement("p");
    paragraph.className = "acadion-feedback__message";
    paragraph.textContent = text;
    copy.append(heading, paragraph);

    const close = document.createElement("button");
    close.className = "acadion-feedback__close";
    close.type = "button";
    close.setAttribute("aria-label", "Cerrar mensaje");
    close.textContent = "×";
    close.addEventListener("click", () => toast.remove());

    toast.append(icon, copy, close);
    region.append(toast);
    toast.dismissTimer = window.setTimeout(() => toast.remove(), duration);
    return toast;
  }

  function mutationSuccessMessage(path, method, data, customMessage) {
    if (typeof customMessage === "string" && customMessage.trim()) return customMessage.trim();
    if (data && typeof data === "object" && typeof data.mensaje === "string" && data.mensaje.trim()) {
      return data.mensaje.trim();
    }

    const route = String(path || "").toLowerCase();
    if (route.includes("/api/docente/notas")) return "La nota del estudiante se guardó correctamente.";
    if (route.includes("/criterios")) return "Los criterios de evaluación se guardaron correctamente.";
    if (route.includes("/fecha")) return "La fecha se modificó correctamente.";
    if (route.includes("/asistencias") || route.includes("/asistencia-personal")) return "La asistencia se guardó correctamente.";
    if (route.includes("/justificacion")) return route.includes("archivo")
      ? "El justificativo se adjuntó correctamente."
      : "La justificación se actualizó correctamente.";
    if (route.includes("/foto-perfil")) return "La foto de perfil se actualizó correctamente.";
    if (route.includes("/cambiar-password") || route.includes("/restablecer")) return "La contraseña se actualizó correctamente.";
    if (route.includes("/perfil")) return "Los datos personales se actualizaron correctamente.";
    if (route.includes("/allegados")) return "Los datos de allegados se guardaron correctamente.";
    if (route.includes("/financiamiento")) return "La información de financiamiento se guardó correctamente.";
    if (route.includes("/gestion/usuarios")) return method === "POST"
      ? "El usuario se creó correctamente."
      : "Los datos y el estado del usuario se actualizaron correctamente.";
    if (route.includes("/docentes-materias")) return "El profesor se asignó a la materia correctamente.";
    if (route.includes("/periodo") || route.includes("/periodos")) return "El período de inscripción se guardó correctamente.";
    if (route.includes("/carreras")) return method === "POST"
      ? "La carrera se creó correctamente."
      : "La carrera se actualizó correctamente.";
    if (route.includes("/materias")) return method === "POST"
      ? "La materia se creó correctamente."
      : "La materia se actualizó correctamente.";
    if (route.includes("/horarios")) return "Los horarios se guardaron correctamente.";
    if (route.includes("/comisiones")) return "La comisión se actualizó correctamente.";
    if (route.includes("/planes-estudio")) return "El plan de estudios se guardó correctamente.";
    if (route.includes("/calendario")) return method === "DELETE"
      ? "El evento del calendario se quitó correctamente."
      : "El calendario académico se actualizó correctamente.";
    if (route.includes("/turnos-final")) return method === "DELETE"
      ? "El turno de final se desactivó correctamente."
      : "El turno de final se guardó correctamente.";
    if (route.includes("/pagos")) return "El estado del pago se actualizó correctamente.";
    if (route.includes("/notificaciones")) return method === "POST"
      ? "La notificación se publicó correctamente."
      : "El estado de la notificación se actualizó correctamente.";
    if (route.includes("/equivalencias")) return "La equivalencia se otorgó correctamente.";
    if (route.includes("/inscripcion") || route.includes("/inscripciones")) return method === "DELETE"
      ? "La inscripción se canceló correctamente."
      : "La inscripción se registró correctamente.";
    if (route.includes("/examenes")) return method === "DELETE"
      ? "La evaluación se quitó correctamente."
      : method === "POST" ? "La evaluación se creó correctamente." : "La evaluación se actualizó correctamente.";
    if (method === "DELETE") return "La operación se completó correctamente.";
    if (method === "POST") return "La información se guardó correctamente.";
    return "Los cambios se guardaron correctamente.";
  }

  function getSession() {
    try {
      return JSON.parse(localStorage.getItem(sessionKey)) || null;
    } catch {
      return null;
    }
  }

  function saveSession(data) {
    const session = {
      personaId: data.personaId,
      usuarioId: data.usuarioId,
      nombreUsuario: data.nombreUsuario,
      rol: data.rol,
      permisos: data.permisos || [],
      debeCambiarPassword: Boolean(data.debeCambiarPassword)
    };
    localStorage.setItem(sessionKey, JSON.stringify(session));
    localStorage.setItem("acadion_usuario_id", String(data.personaId));
    localStorage.removeItem("acadion_token");
    return session;
  }

  function clearSession() {
    localStorage.removeItem(sessionKey);
    localStorage.removeItem("acadion_usuario_id");
    localStorage.removeItem("acadion_token");
  }

  async function request(path, options = {}) {
    const { feedback = true, successMessage, ...requestOptions } = options;
    const method = String(requestOptions.method || "GET").toUpperCase();
    const isMutation = mutationMethods.has(method);
    const session = getSession();
    const headers = new Headers(requestOptions.headers || {});
    if (requestOptions.body && !(requestOptions.body instanceof FormData) && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }
    let response;
    try {
      response = await fetch(`${baseUrl}${path}`, { ...requestOptions, headers, credentials: "include" });
    } catch {
      const error = new Error("No se pudo conectar con el servidor.");
      if (isMutation && feedback !== false) showFeedback(error.message, { type: "error" });
      throw error;
    }

    if (response.status === 401) {
      clearSession();
      window.location.href = "Login.html";
      throw new Error("Tu sesión venció. Volvé a iniciar sesión.");
    }

    const text = await response.text();
    let data = null;
    if (text) {
      try { data = JSON.parse(text); } catch { data = text; }
    }
    if (!response.ok) {
      const rawText = typeof data === "string" ? data.trim() : "";
      const containsTechnicalDetails = rawText.length > 400 ||
        /stack trace|microsoft\.aspnetcore|system\.| at [a-z0-9_.]+\(|headers\s*==|\.cs:line/i.test(rawText);
      const safeText = rawText && !containsTechnicalDetails ? rawText : null;
      const fallbackMessage = response.status >= 500
        ? "Ocurrió un error interno. Intentá nuevamente en unos instantes."
        : response.status === 400
          ? "La solicitud no pudo procesarse. Revisá los datos e intentá nuevamente."
          : `La operación falló (${response.status}).`;
      const message = data?.mensaje || data?.title || safeText;
      const error = new Error(message || fallbackMessage);
      error.status = response.status;
      error.data = data;
      if (isMutation && feedback !== false) showFeedback(error.message, { type: "error" });
      throw error;
    }
    if (isMutation && feedback !== false) {
      showFeedback(mutationSuccessMessage(path, method, data, successMessage));
    }
    return data;
  }

  async function logout() {
    try {
      await fetch(`${baseUrl}/api/auth/logout`, { method: "POST", credentials: "include" });
    } finally {
      clearSession();
    }
  }

  function requireAdmin() {
    const session = getSession();
    if (!session?.usuarioId) {
      window.location.replace("Login.html");
      return null;
    }
    if (!session.permisos?.includes("usuarios.gestionar") && session.rol !== "Secretario") {
      window.location.replace(session.rol === "Directivo" ? "PanelInstitucional.html" : "MenuPrincipal.html");
      return null;
    }
    return session;
  }

  function requireTeacher() {
    const session = getSession();
    if (!session?.usuarioId) {
      window.location.replace("Login.html");
      return null;
    }
    if (session.rol !== "Docente" && !session.permisos?.includes("docencia.gestionar")) {
      const target = session.rol === "Directivo"
        ? "PanelInstitucional.html"
        : session.rol === "Secretario" || session.permisos?.includes("usuarios.gestionar")
        ? "PanelSecretaria.html"
        : "MenuPrincipal.html";
      window.location.replace(target);
      return null;
    }
    return session;
  }

  function requireDirector() {
    const session = getSession();
    if (!session?.usuarioId) {
      window.location.replace("Login.html");
      return null;
    }
    if (session.rol !== "Directivo" || !session.permisos?.includes("reportes.leer")) {
      const target = session.rol === "Secretario" || session.permisos?.includes("usuarios.gestionar")
        ? "PanelSecretaria.html"
        : session.rol === "Docente" ? "PanelDocente.html" : "MenuPrincipal.html";
      window.location.replace(target);
      return null;
    }
    return session;
  }

  return { baseUrl, getSession, saveSession, clearSession, logout, request, showFeedback, requireAdmin, requireTeacher, requireDirector };
})();
