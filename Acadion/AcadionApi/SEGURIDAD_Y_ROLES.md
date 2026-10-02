# Seguridad y roles de AcadionApi

## Puesta en marcha

1. Aplicar las migraciones con `dotnet ef database update` antes de publicar.
2. Configurar una clave JWT de 32 bytes o más mediante `Jwt__Key`.
3. Configurar el correo SMTP mediante las variables detalladas más abajo.
4. Iniciar la API y ejecutar una única vez `POST /api/auth/bootstrap-secretario`.
5. Iniciar sesión en `POST /api/auth/login`. El servidor guarda la sesión en una cookie segura `HttpOnly`; el navegador no recibe ni almacena el JWT.

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
- Secretaría puede crear y administrar estudiantes, docentes, secretarios y directivos.
- Un docente debe estar asignado a una materia y ciclo lectivo antes de recibir estudiantes.
- Un estudiante no puede inscribirse dos veces en la misma materia y ciclo.
- Sólo puede existir una asistencia por inscripción y fecha, y una nota por estudiante y examen.
- La calificación válida está comprendida entre 0 y 10.
- Cada estudiante queda asociado a una versión concreta del plan de estudios.
- Las materias pueden dividirse en comisiones con docente, cupo y horarios propios.
- Cuando una comisión completa su cupo, la inscripción queda en lista de espera hasta que Secretaría confirme una vacante.
- La asistencia estudiantil queda vinculada a una clase concreta y no a una fecha aislada.
- Los finales solo muestran y califican estudiantes que se inscribieron en esa mesa.
- Secretaría define los turnos de final y sus llamados; la fecha de cada mesa debe quedar dentro del turno elegido.
- Las evaluaciones realizadas o con notas quedan protegidas contra eliminación y cambio de fecha.
- Los cambios de nota guardan el valor y la condición anterior y nueva junto con el docente y la fecha.

## Rutas nuevas principales

- `POST /api/auth/bootstrap-secretario`
- `POST /api/auth/login`
- `POST /api/auth/cambiar-password`
- `POST /api/auth/recuperacion/solicitar|verificar|restablecer`
- `GET /api/roles`
- `POST /api/gestion/usuarios`
- `POST /api/gestion-academica/anios-con-materias`
- `POST /api/gestion-academica/docentes-materias`
- `GET|POST|PUT /api/asistencia-personal`
- `GET /api/me/perfil|materias|asistencias|examenes|notas|asistencia-personal`
- `GET /api/docente/alumnos`
- `POST /api/docente/asistencias|examenes|notas`
- `GET|POST|PUT /api/planes-estudio`
- `GET|POST|PUT|DELETE /api/comisiones`
- `GET|POST|PUT|DELETE /api/calendario`
- `GET|POST|PUT|DELETE /api/turnos-final`
- `GET|POST /api/equivalencias`
- `GET /api/me/documentos/*`
- `GET /api/directivo/panel`

## Secretos y configuración

No se guardan claves JWT, contraseñas SMTP ni cadenas productivas en Git. En desarrollo, `Jwt:Key` se almacena con .NET User Secrets. En Azure deben cargarse como variables de entorno:

- `Jwt__Key`: clave aleatoria larga y privada.
- `Smtp__Host`, `Smtp__Puerto`, `Smtp__UsarSsl`.
- `Smtp__Usuario`, `Smtp__Password`.
- `Smtp__Remitente`, `Smtp__NombreRemitente`.
- `Uploads__RutaPerfiles`: directorio persistente externo al repositorio; en Azure puede ser `D:\home\data\Acadion\perfiles`.

La recuperación de contraseña utiliza códigos de seis cifras con vencimiento de 10 minutos, máximo de cinco intentos y un token de cambio de un solo uso. Si SMTP no está configurado, la API informa claramente que el servicio no está disponible.

## Base de datos y auditoría

En desarrollo, `Database:ApplyMigrationsOnStartup` está habilitado. En producción queda deshabilitado por defecto: las migraciones deben aplicarse como paso controlado del despliegue antes de iniciar la nueva versión.

Las operaciones autenticadas `POST`, `PUT`, `PATCH` y `DELETE` exitosas se registran en `RegistrosAuditoria`, sin almacenar contraseñas ni cuerpos de solicitudes. Los usuarios con `reportes.leer` pueden consultar `GET /api/auditoria` con paginación y filtros.

## Archivos y despliegue

`bin`, `obj`, secretos locales y archivos subidos por usuarios están excluidos mediante `.gitignore`. Las fotos de perfil se guardan en almacenamiento persistente externo al repositorio.

Antes de desplegar una versión nueva se debe generar y revisar el script de migración, hacer una copia de seguridad y aplicar las migraciones de forma controlada. La aplicación no modifica automáticamente el esquema de producción.
