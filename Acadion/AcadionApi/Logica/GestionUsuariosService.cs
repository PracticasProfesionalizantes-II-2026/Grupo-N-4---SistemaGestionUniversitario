using System.Globalization;
using System.Data;
using AcadionApi.Datos;
using AcadionApi.DTOs;
using AcadionApi.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica;

public interface IGestionUsuariosService
{
    Task<CuentaInstitucionalCreadaDto> CrearCuentaAsync(
        CuentaInstitucionalCrearDto dto, int? rolForzado = null);
}

public class GestionUsuariosService : IGestionUsuariosService
{
    private readonly AppDbContext _context;

    public GestionUsuariosService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CuentaInstitucionalCreadaDto> CrearCuentaAsync(
        CuentaInstitucionalCrearDto dto, int? rolForzado = null)
    {
        var rolId = rolForzado ?? dto.RolId;
        Validar(dto, rolId);
        if (rolForzado is null && rolId is not (RolesSistema.EstudianteId or RolesSistema.DocenteId or RolesSistema.SecretarioId or RolesSistema.DirectivoId))
            throw new ArgumentException("Secretaría sólo puede crear cuentas de estudiantes, docentes, secretarios o directivos.");
        var rol = await _context.Roles.SingleOrDefaultAsync(r => r.Id == rolId)
            ?? throw new ArgumentException("El rol indicado no existe.");

        var estrategia = _context.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            // Si Azure SQL reintenta la unidad, no reutilizamos entidades de un
            // intento anterior que haya sido revertido.
            _context.ChangeTracker.Clear();
            await using var transaccion = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            Carrera? carrera = null;
            PlanEstudio? planEstudio = null;
            if (rolId == RolesSistema.EstudianteId)
            {
                if (!dto.CarreraId.HasValue)
                    throw new ArgumentException("La carrera es obligatoria para crear un estudiante.");
                carrera = await _context.Set<Carrera>().SingleOrDefaultAsync(c => c.IdCarrera == dto.CarreraId.Value)
                    ?? throw new ArgumentException("La carrera indicada no existe.");
                if (!carrera.Activa)
                    throw new InvalidOperationException("La carrera seleccionada está inactiva y no admite nuevos estudiantes.");
                planEstudio = dto.PlanEstudioId.HasValue
                    ? await _context.PlanesEstudio.SingleOrDefaultAsync(p => p.Id == dto.PlanEstudioId.Value)
                    : await _context.PlanesEstudio.SingleOrDefaultAsync(p =>
                        p.CarreraId == carrera.IdCarrera && p.Activo);
                if (planEstudio is null || planEstudio.CarreraId != carrera.IdCarrera)
                    throw new ArgumentException("La carrera no tiene un plan de estudios vigente válido.");
                var cantidadActual = await _context.Usuarios.CountAsync(u =>
                    u.RolId == RolesSistema.EstudianteId && u.CarreraId == carrera.IdCarrera);
                if (cantidadActual >= carrera.CapacidadMaximaEstudiantes)
                    throw new InvalidOperationException("La carrera alcanzó su capacidad máxima de estudiantes.");
            }

            if (await _context.Personas.AnyAsync(p => p.Dni == dto.Dni))
                throw new InvalidOperationException("Ya existe una persona registrada con ese DNI.");

            var persona = new Persona
            {
                Nombre = NormalizadorDatos.NombrePropio(dto.Nombre),
                Apellido = NormalizadorDatos.NombrePropio(dto.Apellido),
                Dni = dto.Dni,
                FechaNacimiento = dto.FechaNacimiento,
                Direccion = Limpiar(dto.Direccion),
                Localidad = Limpiar(dto.Localidad),
                CodigoPostal = dto.CodigoPostal,
                Email = NormalizadorDatos.Correo(dto.EmailPersonal)
            };
            _context.Personas.Add(persona);

            var nombreUsuario = await GenerarNombreUsuarioAsync(dto.Nombre, dto.Apellido);
            var usuario = new Usuario
            {
                Persona = persona,
                NombreUsuario = nombreUsuario,
                RolId = rolId,
                Estado = EstadoUsuario.Activo,
                DebeCambiarPassword = true,
                FechaCreacion = DateTime.UtcNow,
                EmailInstitucional = NormalizadorDatos.Correo(dto.EmailInstitucional),
                TelefonoContacto = Limpiar(dto.TelefonoContacto),
                CarreraId = carrera?.IdCarrera,
                PlanEstudioId = planEstudio?.Id,
                Matricula = string.Empty,
                Legajo = string.Empty,
                Especialidad = Limpiar(dto.Especialidad),
                TituloAcademico = Limpiar(dto.TituloAcademico)
            };
            usuario.PasswordHash = new PasswordHasher<Usuario>()
                .HashPassword(usuario, dto.Dni.ToString(CultureInfo.InvariantCulture));
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            if (rolId == RolesSistema.EstudianteId)
            {
                usuario.Legajo = $"AC-{DateTime.UtcNow.Year}-{usuario.Id:D6}";
                CrearSituacionFinancieraInicial(usuario, dto);
            }

            await CrearAsignacionesInicialesAsync(usuario, dto.Materias);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return new CuentaInstitucionalCreadaDto
            {
                UsuarioId = usuario.Id,
                PersonaId = persona.Id,
                NombreUsuario = usuario.NombreUsuario,
                PasswordInicial = dto.Dni.ToString(CultureInfo.InvariantCulture),
                Rol = rol.Nombre,
                Legajo = usuario.Legajo,
                CarreraId = usuario.CarreraId,
                PlanEstudioId = usuario.PlanEstudioId,
                PlanEstudio = planEstudio?.Codigo ?? string.Empty,
                Carrera = carrera?.Nombre ?? string.Empty,
                DebeCambiarPassword = true
            };
        });
    }

    private void CrearSituacionFinancieraInicial(Usuario estudiante, CuentaInstitucionalCrearDto dto)
    {
        var ahora = DateTime.UtcNow;
        var estadoMatricula = ParsearEstadoPago(dto.EstadoMatriculaInicial);
        var estadoCuota = ParsearEstadoPago(dto.EstadoCuotaActual);
        _context.MatriculasIniciales.Add(new MatriculaInicial
        {
            EstudianteId = estudiante.Id,
            PeriodoLectivo = ahora.Year,
            Estado = estadoMatricula,
            FechaPago = estadoMatricula == EstadoPago.AlDia ? ahora : null
        });
        _context.CuotasMensuales.Add(new CuotaMensual
        {
            EstudianteId = estudiante.Id,
            Anio = ahora.Year,
            Mes = ahora.Month,
            Estado = estadoCuota,
            FechaVencimiento = new DateTime(ahora.Year, ahora.Month, 10, 23, 59, 59, DateTimeKind.Utc),
            FechaPago = estadoCuota == EstadoPago.AlDia ? ahora : null
        });
    }

    private static EstadoPago ParsearEstadoPago(string? valor)
    {
        return (valor ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PAGADA" or "AL_DIA" or "ALDIA" => EstadoPago.AlDia,
            "EXENTADO" => EstadoPago.Exentado,
            "VENCIDA" => EstadoPago.Vencida,
            "IMPAGA" => EstadoPago.Impaga,
            "PENDIENTE" or "" => EstadoPago.Pendiente,
            _ => throw new ArgumentException("El estado de pago indicado no es válido.")
        };
    }

    private async Task CrearAsignacionesInicialesAsync(
        Usuario usuario, IEnumerable<AsignacionInicialMateriaDto> materias)
    {
        foreach (var asignacion in materias)
        {
            if (!await _context.Materias.AnyAsync(m => m.IdMateria == asignacion.MateriaId))
                throw new ArgumentException($"La materia {asignacion.MateriaId} no existe.");

            if (usuario.RolId == RolesSistema.EstudianteId)
            {
                if (asignacion.DocenteId is null ||
                    !await _context.DocentesMaterias.AnyAsync(dm =>
                        dm.IdDocente == asignacion.DocenteId &&
                        dm.IdMateria == asignacion.MateriaId &&
                        dm.CicloLectivo == asignacion.CicloLectivo && dm.Activa))
                    throw new ArgumentException("La materia debe tener un docente asignado en el ciclo lectivo indicado.");

                _context.EstudianteMaterias.Add(new EstudianteMateria
                {
                    IdEstudiante = usuario.Id,
                    IdMateria = asignacion.MateriaId,
                    IdDocente = asignacion.DocenteId.Value,
                    CicloLectivo = asignacion.CicloLectivo,
                    Cuatrimestre = Limpiar(asignacion.Cuatrimestre),
                    FechaInscripcion = DateTime.UtcNow,
                    Estado = "Cursando"
                });
            }
            else if (usuario.RolId == RolesSistema.DocenteId)
            {
                _context.DocentesMaterias.Add(new DocenteMateria
                {
                    IdDocente = usuario.Id,
                    IdMateria = asignacion.MateriaId,
                    CicloLectivo = asignacion.CicloLectivo,
                    Cuatrimestre = Limpiar(asignacion.Cuatrimestre)
                });
            }
        }
    }

    private async Task<string> GenerarNombreUsuarioAsync(string nombre, string apellido)
    {
        var baseNombre = NormalizadorDatos.NombreUsuario($"{nombre}.{apellido}");
        var candidato = baseNombre;
        var sufijo = 2;
        while (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == candidato))
            candidato = $"{baseNombre}{sufijo++}";

        return candidato;
    }

    private static void Validar(CuentaInstitucionalCrearDto dto, int rolId)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            throw new ArgumentException("El nombre y el apellido son obligatorios.");
        NormalizadorDatos.ValidarNombrePersona(dto.Nombre, "El nombre");
        NormalizadorDatos.ValidarNombrePersona(dto.Apellido, "El apellido");
        if (rolId is not (RolesSistema.SecretarioId or RolesSistema.DirectivoId) || !string.IsNullOrWhiteSpace(dto.Localidad))
            NormalizadorDatos.ValidarTextoAcademico(dto.Localidad, "La localidad");
        if (rolId == RolesSistema.DocenteId)
        {
            NormalizadorDatos.ValidarTextoAcademico(dto.Especialidad, "La especialidad");
            NormalizadorDatos.ValidarTextoAcademico(dto.TituloAcademico, "El título académico");
        }
        if (dto.Dni <= 0)
            throw new ArgumentException("El DNI es obligatorio.");
        if (dto.FechaNacimiento.Date >= DateTime.Today)
            throw new ArgumentException("La fecha de nacimiento no es válida.");
    }

    private static string Limpiar(string? valor) => valor?.Trim() ?? string.Empty;
}
