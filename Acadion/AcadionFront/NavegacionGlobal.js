document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("[data-history-back], .back-button").forEach(element => {
    element.addEventListener("click", event => {
      event.preventDefault();
      window.history.back();
    });
  });

  document.querySelectorAll(".notification-button, [data-notifications]").forEach(element => {
    element.addEventListener("click", event => {
      event.preventDefault();
      window.location.href = "Notificaciones.html";
    });
  });

  document.querySelectorAll(".profile-image, .profile-avatar, [data-profile-link]").forEach(element => {
    element.addEventListener("click", event => {
      event.preventDefault();
      window.location.href = "MisDatosPersonales.html";
    });
  });
});
