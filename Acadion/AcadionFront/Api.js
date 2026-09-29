window.AcadionApi = (() => {
  // Localmente el frontend usa la API de desarrollo; en Azure ambas partes comparten origen.
  const isLocal = ["localhost", "127.0.0.1"].includes(window.location.hostname);
  const baseUrl = isLocal ? "http://localhost:5050" : window.location.origin;
  const sessionKey = "acadion_session";

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
      token: data.token,
      debeCambiarPassword: Boolean(data.debeCambiarPassword)
    };
    localStorage.setItem(sessionKey, JSON.stringify(session));
    localStorage.setItem("acadion_usuario_id", String(data.personaId));
    localStorage.setItem("acadion_token", data.token);
    return session;
  }

  function clearSession() {
    localStorage.removeItem(sessionKey);
    localStorage.removeItem("acadion_usuario_id");
    localStorage.removeItem("acadion_token");
  }

  async function request(path, options = {}) {
    const session = getSession();
    const headers = new Headers(options.headers || {});
    if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }
    if (session?.token) headers.set("Authorization", `Bearer ${session.token}`);

    let response;
    try {
      response = await fetch(`${baseUrl}${path}`, { ...options, headers });
    } catch {
      throw new Error("No se pudo conectar con el servidor.");
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
      const message = data?.mensaje || data?.title || (typeof data === "string" ? data : null);
      const error = new Error(message || `La operación falló (${response.status}).`);
      error.status = response.status;
      error.data = data;
      throw error;
    }
    return data;
  }

  function requireAdmin() {
    const session = getSession();
    if (!session?.token) {
      window.location.replace("Login.html");
      return null;
    }
    if (!session.permisos?.includes("usuarios.gestionar") && session.rol !== "Secretario") {
      window.location.replace("MenuPrincipal.html");
      return null;
    }
    return session;
  }

  function requireTeacher() {
    const session = getSession();
    if (!session?.token) {
      window.location.replace("Login.html");
      return null;
    }
    if (session.rol !== "Docente" && !session.permisos?.includes("docencia.gestionar")) {
      const target = session.rol === "Secretario" || session.permisos?.includes("usuarios.gestionar")
        ? "PanelDirectivo.html"
        : "MenuPrincipal.html";
      window.location.replace(target);
      return null;
    }
    return session;
  }

  return { baseUrl, getSession, saveSession, clearSession, request, requireAdmin, requireTeacher };
})();
