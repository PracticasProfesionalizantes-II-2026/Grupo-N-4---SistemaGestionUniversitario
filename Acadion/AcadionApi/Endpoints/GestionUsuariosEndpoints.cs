using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Logica;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class GestionUsuariosEndpoints
{
    public static void MapGestionUsuariosEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/auth/bootstrap-secretario",
            async (SecretarioInicialCrearDto dto, AppDbContext context,
                IGestionUsuariosService servicio) =>
            {
                if (await context.Usuarios.AnyAsync())
                    return Results.Conflict(new
                    {
                        mensaje = "El sistema ya fue inicializado. Las nuevas cuentas las crea un secretario."
                    });

                try
                {
                    var cuenta = await servicio.CrearCuentaAsync(new CuentaInstitucionalCrearDto
                    {
                        Nombre = dto.Nombre,
                        Apellido = dto.Apellido,
                        Dni = dto.Dni,
                        FechaNacimiento = dto.FechaNacimiento,
                        Direccion = dto.Direccion,
                        Localidad = dto.Localidad,
                        CodigoPostal = dto.CodigoPostal,
                        EmailPersonal = dto.EmailPersonal,
                        EmailInstitucional = dto.EmailInstitucional,
                        TelefonoContacto = dto.TelefonoContacto
                    }, RolesSistema.SecretarioId);
                    return Results.Created($"/usuarios/{cuenta.UsuarioId}", cuenta);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { mensaje = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { mensaje = ex.Message });
                }
            })
            .AllowAnonymous();

        var group = routes.MapGroup("/api/gestion/usuarios")
            .RequierePermiso(PermisosSistema.UsuariosGestionar);

        group.MapGet("/", async (int? rolId, AppDbContext context) =>
        {
            var rolesGestionables = new[]
            {
                RolesSistema.EstudianteId, RolesSistema.DocenteId, RolesSistema.DirectivoId
            };
            var consulta = context.Usuarios.AsNoTracking()
                .Include(u => u.Persona)
                .Include(u => u.Rol)
                .Include(u => u.Carrera)
                .Where(u => rolesGestionables.Contains(u.RolId));
            if (rolId.HasValue)
                consulta = consulta.Where(u => u.RolId == rolId.Value);

            var usuarios = await consulta
                .OrderBy(u => u.RolId == RolesSistema.DirectivoId ? 0 :
                    u.RolId == RolesSistema.DocenteId ? 1 : 2)
                .ThenBy(u => u.Persona.Apellido)
                .ThenBy(u => u.Persona.Nombre)
                .ToListAsync();
            return Results.Ok(usuarios.Select(MapearUsuario));
        });

        group.MapGet("/{id:int}", async (int id, AppDbContext context) =>
        {
            var usuario = await context.Usuarios.AsNoTracking()
                .Include(u => u.Persona)
                .Include(u => u.Rol)
                .Include(u => u.Carrera)
                .SingleOrDefaultAsync(u => u.Id == id);
            if (usuario is null || usuario.RolId == RolesSistema.SecretarioId)
                return Results.NotFound();
            return Results.Ok(MapearUsuario(usuario));
        });

        group.MapGet("/{id:int}/inasistencias", async (int id, int? cicloLectivo,
            AppDbContext context) =>
        {
            var ciclo = cicloLectivo ?? DateTime.UtcNow.Year;
            if (ciclo is < 2020 or > 2100)
                return Results.BadRequest(new { mensaje = "El ciclo lectivo no es válido." });

            var estudiante = await context.Usuarios.AsNoTracking()
                .Where(u => u.Id == id && u.RolId == RolesSistema.EstudianteId)
                .Include(u => u.Persona)
                .Include(u => u.Carrera)
                .Select(u => new
                {
                    u.Id,
                    Nombre = u.Persona.Nombre,
                    Apellido = u.Persona.Apellido,
                    u.Persona.Dni,
                    u.Legajo,
                    Carrera = u.Carrera == null ? "Sin carrera" : u.Carrera.Nombre
                })
                .SingleOrDefaultAsync();
            if (estudiante is null)
                return Results.NotFound(new { mensaje = "El estudiante indicado no existe." });

            var inasistencias = await context.Set<Asistencia>().AsNoTracking()
                .Where(a => a.Inscripcion != null &&
                    a.Inscripcion.IdEstudiante == id &&
                    a.Inscripcion.CicloLectivo == ciclo &&
                    (a.Tipo == "Ausente" || a.Tipo == "Justificada"))
                .OrderBy(a => a.Inscripcion!.Materia!.AnioCursada!.NumeroAnio)
                .ThenBy(a => a.Inscripcion!.Materia!.Nombre)
                .ThenByDescending(a => a.Fecha)
                .Select(a => new
                {
                    a.IdAsistencia,
                    a.Fecha,
                    TipoClase = string.IsNullOrEmpty(a.TipoClase)
                        ? a.Inscripcion!.Materia!.Modalidad
                        : a.TipoClase,
                    TemaDictado = string.IsNullOrEmpty(a.TemaDictado)
                        ? "Sin tema registrado"
                        : a.TemaDictado,
                    Justificada = a.Justificada || a.Tipo == "Justificada",
                    CantidadInasistencias = a.CantidadInasistencias > 0
                        ? a.CantidadInasistencias
                        : 1,
                    a.Observaciones,
                    MateriaId = a.Inscripcion!.IdMateria,
                    Materia = a.Inscripcion.Materia!.Nombre,
                    NumeroAnio = a.Inscripcion.Materia.AnioCursada!.NumeroAnio,
                    NombreAnio = a.Inscripcion.Materia.AnioCursada.NombreAnio
                })
                .ToListAsync();

            return Results.Ok(new { Estudiante = estudiante, CicloLectivo = ciclo, Inasistencias = inasistencias });
        });

        group.MapPut("/{id:int}/inasistencias/{asistenciaId:int}/justificacion",
            async (int id, int asistenciaId, JustificacionInasistenciaDto dto,
                AppDbContext context) =>
            {
                var asistencia = await context.Set<Asistencia>()
                    .Include(a => a.Inscripcion)
                    .SingleOrDefaultAsync(a => a.IdAsistencia == asistenciaId &&
                        a.Inscripcion != null && a.Inscripcion.IdEstudiante == id &&
                        (a.Tipo == "Ausente" || a.Tipo == "Justificada"));
                if (asistencia is null)
                    return Results.NotFound(new { mensaje = "La inasistencia indicada no existe." });

                asistencia.Justificada = dto.Justificada;
                asistencia.Tipo = "Ausente";
                if (asistencia.CantidadInasistencias <= 0)
                    asistencia.CantidadInasistencias = 1;
                await context.SaveChangesAsync();
                return Results.Ok(new
                {
                    asistencia.IdAsistencia,
                    asistencia.Justificada,
                    mensaje = dto.Justificada
                        ? "La inasistencia fue justificada correctamente."
                        : "Se quitó la justificación de la inasistencia."
                });
            });

        group.MapPost("/", async (CuentaInstitucionalCrearDto dto,
            IGestionUsuariosService servicio) =>
        {
            try
            {
                var cuenta = await servicio.CrearCuentaAsync(dto);
                return Results.Created($"/usuarios/{cuenta.UsuarioId}", cuenta);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { mensaje = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { mensaje = "La cuenta no pudo crearse porque existen datos duplicados." });
            }
        });

        group.MapPut("/{id:int}", async (int id, UsuarioGestionActualizarDto dto,
            AppDbContext context) =>
        {
            try
            {
                var usuario = await context.Usuarios
                    .Include(u => u.Persona)
                    .Include(u => u.Rol)
                    .Include(u => u.Carrera)
                    .SingleOrDefaultAsync(u => u.Id == id);
                if (usuario is null || usuario.RolId == RolesSistema.SecretarioId)
                    return Results.NotFound();

                if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
                    return Results.BadRequest(new { mensaje = "El nombre y el apellido son obligatorios." });
                NormalizadorDatos.ValidarNombrePersona(dto.Nombre, "El nombre");
                NormalizadorDatos.ValidarNombrePersona(dto.Apellido, "El apellido");
                NormalizadorDatos.ValidarTextoAcademico(dto.Localidad, "La localidad");
                if (usuario.RolId == RolesSistema.DocenteId)
                {
                    NormalizadorDatos.ValidarTextoAcademico(dto.Especialidad, "La especialidad");
                    NormalizadorDatos.ValidarTextoAcademico(dto.TituloAcademico, "El título académico");
                }
                if (dto.Dni <= 0 || dto.FechaNacimiento.Date >= DateTime.Today)
                    return Results.BadRequest(new { mensaje = "El DNI o la fecha de nacimiento no son válidos." });
                if (string.IsNullOrWhiteSpace(dto.NombreUsuario))
                    return Results.BadRequest(new { mensaje = "El nombre de usuario es obligatorio." });
                if (!Enum.TryParse<EstadoUsuario>(dto.Estado, true, out var estado))
                    return Results.BadRequest(new { mensaje = "El estado de la cuenta no es válido." });
                if (await context.Personas.AnyAsync(p => p.Dni == dto.Dni && p.Id != usuario.PersonaId))
                    return Results.Conflict(new { mensaje = "Ya existe otra persona con ese DNI." });
                var nombreUsuarioNormalizado = NormalizadorDatos.NombreUsuario(dto.NombreUsuario);
                if (await context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuarioNormalizado && u.Id != id))
                    return Results.Conflict(new { mensaje = "Ese nombre de usuario ya está en uso." });

                Carrera? carrera = null;
                if (usuario.RolId == RolesSistema.EstudianteId)
                {
                    if (!dto.CarreraId.HasValue)
                        return Results.BadRequest(new { mensaje = "La carrera es obligatoria para el estudiante." });
                    carrera = await context.Set<Carrera>().SingleOrDefaultAsync(c => c.IdCarrera == dto.CarreraId.Value);
                    if (carrera is null)
                        return Results.BadRequest(new { mensaje = "La carrera indicada no existe." });
                    if (usuario.CarreraId != carrera.IdCarrera)
                    {
                        var tieneCursadasActivas = await context.EstudianteMaterias.AnyAsync(i =>
                            i.IdEstudiante == usuario.Id && i.CicloLectivo == DateTime.UtcNow.Year &&
                            i.Estado != "Cancelada");
                        if (tieneCursadasActivas)
                            return Results.Conflict(new
                            {
                                mensaje = "No se puede cambiar la carrera mientras el estudiante tenga cursadas activas en el ciclo actual."
                            });
                        var cantidad = await context.Usuarios.CountAsync(u =>
                            u.RolId == RolesSistema.EstudianteId && u.CarreraId == carrera.IdCarrera);
                        if (cantidad >= carrera.CapacidadMaximaEstudiantes)
                            return Results.Conflict(new { mensaje = "La carrera seleccionada no tiene cupo disponible." });
                    }
                }

                usuario.NombreUsuario = nombreUsuarioNormalizado;
                usuario.EmailInstitucional = NormalizadorDatos.Correo(dto.EmailInstitucional);
                usuario.TelefonoContacto = dto.TelefonoContacto.Trim();
                usuario.Estado = estado;
                usuario.CarreraId = carrera?.IdCarrera;
                usuario.Especialidad = usuario.RolId == RolesSistema.DocenteId ? dto.Especialidad.Trim() : string.Empty;
                usuario.TituloAcademico = usuario.RolId == RolesSistema.DocenteId ? dto.TituloAcademico.Trim() : string.Empty;
                usuario.Persona.Nombre = NormalizadorDatos.NombrePropio(dto.Nombre);
                usuario.Persona.Apellido = NormalizadorDatos.NombrePropio(dto.Apellido);
                usuario.Persona.Dni = dto.Dni;
                usuario.Persona.FechaNacimiento = dto.FechaNacimiento;
                usuario.Persona.Direccion = dto.Direccion.Trim();
                usuario.Persona.Localidad = dto.Localidad.Trim();
                usuario.Persona.CodigoPostal = dto.CodigoPostal;
                usuario.Persona.Email = NormalizadorDatos.Correo(dto.EmailPersonal);

                await context.SaveChangesAsync();
                usuario.Carrera = carrera;
                return Results.Ok(MapearUsuario(usuario));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensaje = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { mensaje = "No fue posible guardar los cambios porque existen datos duplicados." });
            }
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext context) =>
        {
            var usuario = await context.Usuarios
                .Include(u => u.Persona)
                .SingleOrDefaultAsync(u => u.Id == id);
            if (usuario is null || usuario.RolId == RolesSistema.SecretarioId)
                return Results.NotFound(new { mensaje = "El usuario indicado no existe o no puede eliminarse." });

            var tieneHistorial = usuario.RolId switch
            {
                RolesSistema.EstudianteId =>
                    await context.EstudianteMaterias.AnyAsync(i => i.IdEstudiante == id) ||
                    await context.Set<NotaExamen>().AnyAsync(n => n.IdEstudiante == id) ||
                    await context.InscripcionesExamenes.AnyAsync(i => i.EstudianteId == id),
                RolesSistema.DocenteId =>
                    await context.EstudianteMaterias.AnyAsync(i => i.IdDocente == id) ||
                    await context.Set<Asistencia>().AnyAsync(a => a.IdDocente == id) ||
                    await context.Set<Examen>().AnyAsync(e => e.IdDocente == id) ||
                    await context.RegistrosAsistenciaPersonal.AnyAsync(r => r.UsuarioId == id),
                _ => false
            };

            if (tieneHistorial)
                return Results.Conflict(new
                {
                    mensaje = "El usuario tiene historial académico o de asistencia y no puede borrarse. Cambiá su estado a Inactivo para conservar los registros institucionales."
                });

            try
            {
                await using var transaccion = await context.Database.BeginTransactionAsync();
                context.NotificacionesGenerales.RemoveRange(
                    context.NotificacionesGenerales.Where(n => n.UsuarioDestinoId == id));
                if (usuario.RolId == RolesSistema.DocenteId)
                    context.DocentesMaterias.RemoveRange(
                        context.DocentesMaterias.Where(dm => dm.IdDocente == id));

                context.Usuarios.Remove(usuario);
                context.Personas.Remove(usuario.Persona);
                await context.SaveChangesAsync();
                await transaccion.CommitAsync();
                return Results.Ok(new { mensaje = "El usuario fue eliminado correctamente." });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new
                {
                    mensaje = "El usuario está relacionado con registros institucionales y no puede borrarse. Cambiá su estado a Inactivo."
                });
            }
        });
    }

    private static UsuarioGestionDto MapearUsuario(Usuario usuario) => new()
    {
        Id = usuario.Id,
        PersonaId = usuario.PersonaId,
        NombreUsuario = usuario.NombreUsuario,
        Nombre = usuario.Persona.Nombre,
        Apellido = usuario.Persona.Apellido,
        Dni = usuario.Persona.Dni,
        FechaNacimiento = usuario.Persona.FechaNacimiento,
        Direccion = usuario.Persona.Direccion,
        Localidad = usuario.Persona.Localidad,
        CodigoPostal = usuario.Persona.CodigoPostal,
        EmailPersonal = usuario.Persona.Email,
        EmailInstitucional = usuario.EmailInstitucional,
        TelefonoContacto = usuario.TelefonoContacto,
        RolId = usuario.RolId,
        Rol = usuario.Rol.Nombre,
        Estado = usuario.Estado.ToString(),
        CarreraId = usuario.CarreraId,
        Carrera = usuario.Carrera?.Nombre ?? string.Empty,
        Legajo = usuario.Legajo,
        Especialidad = usuario.Especialidad,
        TituloAcademico = usuario.TituloAcademico,
        FotoPerfilUrl = usuario.FotoPerfilUrl,
        FechaCreacion = usuario.FechaCreacion
    };
}
