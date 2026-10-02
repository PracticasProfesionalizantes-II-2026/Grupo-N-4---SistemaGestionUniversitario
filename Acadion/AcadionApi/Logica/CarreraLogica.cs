using AcadionApi.Datos;
using AcadionApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica;

public class CarreraLogica : ICarreraLogica
{
    private static readonly string[] TiposPermitidos =
        ["Tecnicatura", "Licenciatura", "Ingeniería"];

    private readonly AppDbContext _context;

    public CarreraLogica(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CarreraDto> RegistrarCarreraAsync(CarreraCrearDto dto)
    {
        Validar(dto.Nombre, dto.PlanEstudios, dto.Tipo, dto.DuracionAnios,
            dto.CapacidadMaximaEstudiantes);
        var nombre = dto.Nombre.Trim();
        var plan = dto.PlanEstudios.Trim();
        if (await _context.Set<Carrera>().AnyAsync(c => c.Nombre == nombre && c.PlanEstudios == plan))
            throw new InvalidOperationException("Ya existe esa carrera con el mismo plan de estudios.");

        var carrera = new Carrera
        {
            Nombre = nombre,
            PlanEstudios = plan,
            Tipo = NormalizarTipo(dto.Tipo),
            DuracionAnios = dto.DuracionAnios,
            CapacidadMaximaEstudiantes = dto.CapacidadMaximaEstudiantes,
            Activa = true,
            AniosAcademicos = CrearAnios(1, dto.DuracionAnios),
            PlanesEstudio =
            [
                new PlanEstudio
                {
                    Codigo = plan,
                    VigenteDesde = ExtraerVigencia(plan),
                    Activo = true
                }
            ]
        };
        _context.Add(carrera);
        await _context.SaveChangesAsync();
        return Mapear(carrera);
    }

    public async Task<IEnumerable<CarreraDto>> ObtenerCarrerasAsync()
    {
        var carreras = await _context.Set<Carrera>().AsNoTracking()
            .Include(c => c.AniosAcademicos)
            .Include(c => c.AlumnosInscritos)
            .Include(c => c.PlanesEstudio).ThenInclude(p => p.Materias)
            .Include(c => c.PlanesEstudio).ThenInclude(p => p.Estudiantes)
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.PlanEstudios)
            .ToListAsync();
        return carreras.Select(Mapear);
    }

    public async Task<CarreraDto?> ObtenerCarreraPorIdAsync(int id)
    {
        var carrera = await _context.Set<Carrera>().AsNoTracking()
            .Include(c => c.AniosAcademicos)
            .Include(c => c.AlumnosInscritos)
            .Include(c => c.PlanesEstudio).ThenInclude(p => p.Materias)
            .Include(c => c.PlanesEstudio).ThenInclude(p => p.Estudiantes)
            .SingleOrDefaultAsync(c => c.IdCarrera == id);
        return carrera is null ? null : Mapear(carrera);
    }

    public async Task<bool> ActualizarCarreraAsync(int id, CarreraActualizarDto dto)
    {
        Validar(dto.Nombre, dto.PlanEstudios, dto.Tipo, dto.DuracionAnios,
            dto.CapacidadMaximaEstudiantes);
        var carrera = await _context.Set<Carrera>()
            .Include(c => c.AniosAcademicos)
            .Include(c => c.AlumnosInscritos)
            .Include(c => c.PlanesEstudio)
            .SingleOrDefaultAsync(c => c.IdCarrera == id);
        if (carrera is null) return false;

        if (dto.CapacidadMaximaEstudiantes < carrera.AlumnosInscritos.Count)
            throw new InvalidOperationException(
                $"El cupo no puede ser menor a los {carrera.AlumnosInscritos.Count} estudiantes ya asignados.");
        var maximoExistente = carrera.AniosAcademicos.Select(a => a.NumeroAnio).DefaultIfEmpty(0).Max();
        if (dto.DuracionAnios < maximoExistente)
            throw new InvalidOperationException(
                "No se puede reducir la duración porque existen años académicos que quedarían fuera del plan.");

        var nombre = dto.Nombre.Trim();
        var plan = dto.PlanEstudios.Trim();
        if (await _context.Set<Carrera>().AnyAsync(c => c.IdCarrera != id &&
            c.Nombre == nombre && c.PlanEstudios == plan))
            throw new InvalidOperationException("Ya existe esa carrera con el mismo plan de estudios.");

        carrera.Nombre = nombre;
        if (!string.Equals(carrera.PlanEstudios, plan, StringComparison.OrdinalIgnoreCase))
        {
            var nuevaVigencia = ExtraerVigencia(plan);
            foreach (var anterior in carrera.PlanesEstudio.Where(p => p.Activo))
            {
                anterior.Activo = false;
                anterior.VigenteHasta ??= Math.Max(anterior.VigenteDesde, nuevaVigencia - 1);
            }
            if (!carrera.PlanesEstudio.Any(p =>
                string.Equals(p.Codigo, plan, StringComparison.OrdinalIgnoreCase)))
            {
                carrera.PlanesEstudio.Add(new PlanEstudio
                {
                    Codigo = plan,
                    VigenteDesde = nuevaVigencia,
                    Activo = true
                });
            }
            else
            {
                var existente = carrera.PlanesEstudio.Single(p =>
                    string.Equals(p.Codigo, plan, StringComparison.OrdinalIgnoreCase));
                existente.Activo = true;
                existente.VigenteHasta = null;
            }
        }
        carrera.PlanEstudios = plan;
        carrera.Tipo = NormalizarTipo(dto.Tipo);
        carrera.DuracionAnios = dto.DuracionAnios;
        carrera.CapacidadMaximaEstudiantes = dto.CapacidadMaximaEstudiantes;
        carrera.Activa = dto.Activa ?? carrera.Activa;
        if (dto.DuracionAnios > maximoExistente)
            carrera.AniosAcademicos.AddRange(CrearAnios(maximoExistente + 1, dto.DuracionAnios));

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarCarreraAsync(int id)
    {
        var carrera = await _context.Set<Carrera>()
            .Include(c => c.AlumnosInscritos)
            .Include(c => c.AniosAcademicos).ThenInclude(a => a.Materias)
            .SingleOrDefaultAsync(c => c.IdCarrera == id);
        if (carrera is null) return false;
        if (carrera.AlumnosInscritos.Count > 0 || carrera.AniosAcademicos.Any(a => a.Materias.Count > 0))
            throw new InvalidOperationException(
                "No se puede eliminar una carrera que tiene estudiantes o materias asociadas.");
        if (carrera.AniosAcademicos.Count > 0)
            throw new InvalidOperationException(
                "No se puede eliminar una carrera con años académicos creados. Podés editar sus datos y cupo.");

        _context.Remove(carrera);
        await _context.SaveChangesAsync();
        return true;
    }

    private static List<Anio> CrearAnios(int desde, int hasta) =>
        Enumerable.Range(desde, hasta - desde + 1)
            .Select(numero => new Anio
            {
                NumeroAnio = numero,
                NombreAnio = $"{numero}.º año"
            }).ToList();

    private static CarreraDto Mapear(Carrera carrera) => new()
    {
        IdCarrera = carrera.IdCarrera,
        Nombre = carrera.Nombre,
        PlanEstudios = carrera.PlanEstudios,
        Tipo = carrera.Tipo,
        DuracionAnios = carrera.DuracionAnios,
        CapacidadMaximaEstudiantes = carrera.CapacidadMaximaEstudiantes,
        Activa = carrera.Activa,
        EstudiantesInscriptos = carrera.AlumnosInscritos.Count,
        Anios = carrera.AniosAcademicos.OrderBy(a => a.NumeroAnio).Select(a => new AnioCarreraDto
        {
            IdAnio = a.IdAnio,
            NumeroAnio = a.NumeroAnio,
            NombreAnio = a.NombreAnio
        }).ToList(),
        Planes = carrera.PlanesEstudio.OrderByDescending(p => p.Activo)
            .ThenByDescending(p => p.VigenteDesde)
            .Select(p => new PlanEstudioDto
            {
                Id = p.Id,
                CarreraId = p.CarreraId,
                Codigo = p.Codigo,
                VigenteDesde = p.VigenteDesde,
                VigenteHasta = p.VigenteHasta,
                Activo = p.Activo,
                CantidadMaterias = p.Materias.Count,
                CantidadEstudiantes = p.Estudiantes.Count
            }).ToList()
    };

    private static void Validar(string nombre, string plan, string tipo, int duracion, int capacidad)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la carrera es obligatorio.");
        if (nombre.Trim().Length > 150)
            throw new ArgumentException("El nombre de la carrera no puede superar los 150 caracteres.");
        NormalizadorDatos.ValidarTextoAcademico(nombre, "El nombre de la carrera");
        if (string.IsNullOrWhiteSpace(plan))
            throw new ArgumentException("El plan de estudios es obligatorio.");
        if (plan.Trim().Length > 80)
            throw new ArgumentException("El plan de estudios no puede superar los 80 caracteres.");
        NormalizadorDatos.ValidarTextoAcademico(plan, "El plan de estudios");
        if (!TiposPermitidos.Any(t => string.Equals(t, tipo?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("El tipo debe ser Tecnicatura, Licenciatura o Ingeniería.");
        if (duracion is < 1 or > 10)
            throw new ArgumentException("La duración debe estar entre 1 y 10 años.");
        if (capacidad < 1)
            throw new ArgumentException("La capacidad máxima debe ser mayor a cero.");
    }

    private static string NormalizarTipo(string tipo) => TiposPermitidos.Single(t =>
        string.Equals(t, tipo.Trim(), StringComparison.OrdinalIgnoreCase));

    private static int ExtraerVigencia(string codigo)
    {
        var anio = System.Text.RegularExpressions.Regex.Match(codigo, @"\b(20\d{2}|21\d{2})\b");
        return anio.Success && int.TryParse(anio.Value, out var valor)
            ? valor
            : DateTime.UtcNow.Year;
    }
}
