# AcadionFront

El frontend se publica junto con `AcadionApi`. En desarrollo consume `http://localhost:5050`; en Azure utiliza el mismo dominio de la aplicación web.

## Sesión y roles

El JWT no se guarda en `localStorage`: la API lo entrega mediante una cookie segura `HttpOnly`. En el navegador solo se conserva información no sensible para adaptar la navegación.

- **Estudiante:** inicio, inscripciones, reportes, documentos y perfil.
- **Docente:** materias, comisiones, asistencias, evaluaciones, finales y perfil.
- **Secretaría:** ABM institucional, planes, comisiones, pagos, calendario y notificaciones.
- **Directivo:** tablero institucional de solo lectura.

## Pantallas principales

### Secretaría

- `PanelSecretaria.html`: inicio e indicadores operativos.
- `SeleccionarRol.html`: creación de estudiantes, docentes, secretarios y directivos.
- `GestionUsuarios.html`: listado, filtros, edición, inasistencias, equivalencias y eliminación.
- `CrearCarrera.html` y `GestionCarreras.html`: carreras y planes de estudio versionados.
- `CrearMateria.html` y `GestionMaterias.html`: materias, correlatividades, comisiones, horarios y listas de espera.
- `AsistenciaProfesores.html`: asistencia docente y justificaciones.
- `GestionPagos.html`: matrícula, cuotas, comprobantes y validación.
- `CalendarioAcademico.html`: calendario institucional central y turnos de examen final.
- `GestionNotificaciones.html`: comunicaciones generales o por rol.

`PanelDirectivo.html` se conserva únicamente como redirección de compatibilidad hacia `PanelSecretaria.html`; ya no contiene la pantalla administrativa.

### Directivos

- `PanelInstitucional.html`: indicadores académicos y administrativos de solo lectura, con filtros y exportación.

### Docentes

- `PanelDocente.html`: resumen docente.
- `DocenteMaterias.html`: materias, carreras, comisiones y horarios.
- `DocenteAsistencias.html`: clases y asistencia de estudiantes.
- `DocenteEvaluaciones.html`: evaluaciones, finales, criterios y notas.
- `DocentePerfil.html`: datos institucionales y datos personales editables.

### Estudiantes

- `MenuPrincipal.html`: materias vigentes y horarios del ciclo actual.
- `InscripcionAMateria.html`: inscripción por comisión y lista de espera.
- `InscripcionAExamen.html`: inscripción y baja de finales dentro del plazo permitido.
- `MateriasEnCurso.html`, `MateriasAprobadas.html`, `MateriasDesaprobadas.html`, `PromedioYAvance.html` e `Inasistencias.html`: reportes académicos.
- `MisDatosPersonales.html`: perfil, situación financiera y descarga de documentos.
- `Notificaciones.html`: avisos institucionales y recordatorios automáticos.

## Componentes compartidos

`Comun.css`, `Comun.js`, `Admin.css` y `Api.js` centralizan el diseño, la navegación, los iconos, la validación básica, el comportamiento responsive y las llamadas autenticadas. Las validaciones del navegador mejoran la experiencia; la API vuelve a validar todos los datos y aplica las reglas de negocio.
