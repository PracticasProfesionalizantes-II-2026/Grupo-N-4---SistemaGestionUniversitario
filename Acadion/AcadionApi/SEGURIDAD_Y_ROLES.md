# Seguridad y roles de AcadionApi

## Puesta en marcha

1. Aplicar las migraciones con `dotnet ef database update` desde `AcadionApi`.
2. Configurar una clave JWT segura mediante la variable `Jwt__Key` en producción.
3. Iniciar la API y ejecutar una única vez `POST /api/auth/bootstrap-secretario`.
4. Iniciar sesión en `POST /api/auth/login` y enviar el token en `Authorization: Bearer <token>`.

La inicialización queda bloqueada en cuanto existe el primer usuario. Las cuentas siguientes se crean con `POST /api/gestion/usuarios`.

## Roles

- **Estudiante:** accede mediante `/api/me` únicamente a su perfil, materias, asistencias, exámenes y notas.
- **Docente:** consulta sus materias y alumnos; sólo puede cargar asistencia, evaluaciones y notas de sus comisiones desde `/api/docente`.
- **Secretario:** administra cuentas, carreras, años, materias, inscripciones, asignaciones docentes y fichado del personal.
- **Directivo:** dispone de permisos de lectura institucional y reportes, sin permisos de modificación.

Los permisos no se deducen en el frontend. Se almacenan en `Roles`, `Permisos` y `RolesPermisos`, y la API los valida en cada operación protegida.

## Reglas principales

- El nombre de usuario se genera con `nombre.apellido`; si ya existe, se agrega un número.
- La contraseña inicial es el DNI y la cuenta queda marcada para cambio de contraseña.
- Secretaría sólo puede crear cuentas de estudiantes y docentes desde la operación normal.
- Un docente debe estar asignado a una materia y ciclo lectivo antes de recibir estudiantes.
- Un estudiante no puede inscribirse dos veces en la misma materia y ciclo.
- Sólo puede existir una asistencia por inscripción y fecha, y una nota por estudiante y examen.
- La calificación válida está comprendida entre 0 y 10.

## Rutas nuevas principales

- `POST /api/auth/bootstrap-secretario`
- `POST /api/auth/login`
- `POST /api/auth/cambiar-password`
- `GET /api/roles`
- `POST /api/gestion/usuarios`
- `POST /api/gestion-academica/anios-con-materias`
- `POST /api/gestion-academica/docentes-materias`
- `GET|POST|PUT /api/asistencia-personal`
- `GET /api/me/perfil|materias|asistencias|examenes|notas|asistencia-personal`
- `GET /api/docente/alumnos`
- `POST /api/docente/asistencias|examenes|notas`
