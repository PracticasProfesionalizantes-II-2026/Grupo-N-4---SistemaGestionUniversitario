# AcadionFront

Esta carpeta funciona junto a `AcadionApi`. Abrí `index.html`: si no hay una sesión activa te lleva al login y, después de ingresar como secretario, abre el panel administrativo.

## Organización
Cada pantalla mantiene la organización HTML, CSS y JavaScript del proyecto. `Comun.css`, `Comun.js`, `Admin.css` y `Api.js` reúnen estilos, navegación, sesión JWT y llamadas repetidas. Las rutas usan enlaces directos a archivos HTML y funcionan con Live Server.

## Integración existente
El frontend consume la API en `http://localhost:5050`, guarda el JWT y envía el token en las operaciones protegidas. El secretario puede crear estudiantes, docentes y directivos, administrar la estructura académica, fichar docentes y publicar notificaciones.

## Nuevas pantallas
Las pantallas administrativas principales escriben datos reales en la API. Las credenciales iniciales se muestran después de crear una cuenta; el nombre de usuario se genera con nombre y apellido y la contraseña inicial es el DNI. Las pantallas heredadas que todavía usan la clase `local-form` continúan siendo demostrativas.

## Panel administrativo
- `PanelDirectivo.html` — inicio y accesos rápidos
- `SeleccionarRol.html` — selección de tipo de usuario
- `CrearEstudiante.html`, `CrearProfesor.html`, `CrearDirectivo.html` — altas institucionales
- `GestionMaterias.html` — carreras, años, materias, días y horarios
- `AsistenciaProfesores.html` — asistencia de docentes por materia
- `GestionNotificaciones.html` — avisos generales o por rol

## Alcance visual
Se usaron las referencias de Figma consultadas en esta conversación. Figma alcanzó el límite de consultas Starter durante el relevamiento. Por ese motivo esta entrega cubre las pantallas principales identificadas, pero no certifica la réplica de todos los frames duplicados, estados de error y variantes del archivo. Los estados requeridos, contraseña distinta y foto inválida se representan mediante validación y mensajes.

## Rutas
- Login.html
- MenuPrincipal.html
- InscripcionAMateria.html
- InscripcionAExamen.html
- MateriasEnCurso.html — Historia Académica
- MateriasAprobadas.html — Materias Aprobadas
- MateriasDesaprobadas.html — Materias Desaprobadas
- PromedioYAvance.html — Promedio y avance
- Inasistencias.html — Reporte de inasistencias
- MisDatosPersonales.html — Datos personales
- EditarContacto.html — Editar datos de contacto
- EditarDomicilio.html — Editar domicilio
- EditarAllegados.html — Editar allegados
- Ajustes.html — Ajustes
- CambiarContrasena.html — Ajustes
- CambiarFoto.html — Ajustes
- RecuperarContrasena.html — ¿Olvidaste tu contraseña?
- IngresarCodigo.html — Ingresar código
- NuevaContrasena.html — Nueva contraseña
- ContrasenaCambiada.html — Contraseña cambiada
- PanelDirectivo.html — Bienvenido
- Estudiantes.html — Alumnos
- Profesores.html — Profesores
- Secretarios.html — Secretarios
- SeleccionarRol.html — Añadir
- CrearEstudiante.html — Crear Estudiante
- CrearProfesor.html — Crear Profesor
- CrearDirectivo.html — Crear Directivo
- GestionMaterias.html — Materias y horarios
- AsistenciaProfesores.html — Asistencia docente
- GestionNotificaciones.html — Notificaciones generales
- Clases.html — Clases
- CrearClase.html — Crear clase
- Materia.html — Materia
- Calificaciones.html — Calificaciones
- Asistencia.html — Asistencia
- Reuniones.html — Reuniones
- CrearReunion.html — Crear reunión
