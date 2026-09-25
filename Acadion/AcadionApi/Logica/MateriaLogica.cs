using AcadionApi.Datos;
using AcadionApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Logica;

public class MateriaLogica : IMateriaLogica
{
    private readonly AppDbContext _context;

    public MateriaLogica(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MateriaDto> RegistrarMateriaAsync(MateriaCrearDto dto)
    {
        ValidarDatosBasicos(dto.Nombre, dto.Modalidad, dto.Estado, dto.IdAnio,
            dto.TipoCursada, dto.NumeroPeriodo);
        var anio = await _context.Set<Anio>().Include(a => a.Carrera)
            .SingleOrDefaultAsync(a => a.IdAnio == dto.IdAnio)
            ?? throw new ArgumentException("El año académico indicado no existe.");
        await ValidarNombreUnicoAsync(dto.Nombre, anio.IdCarrera);
        var correlativas = await ObtenerCorrelativasValidasAsync(dto.CorrelativasIds, anio, null);

        var materia = new Materia
        {
            Nombre = dto.Nombre.Trim(),
            Modalidad = dto.Modalidad.Trim(),
            Estado = dto.Estado.Trim(),
            TipoCursada = NormalizarTipoCursada(dto.TipoCursada),
            NumeroPeriodo = NormalizarNumeroPeriodo(dto.TipoCursada, dto.NumeroPeriodo),
            IdAnio = dto.IdAnio,
            AnioCursada = anio,
            Correlativas = correlativas
        };
        _context.Materias.Add(materia);
        await _context.SaveChangesAsync();
        return MapearDetalle(materia);
    }

    public async Task<IEnumerable<MateriaListaDto>> ObtenerMateriasAsync(
        int? carreraId = null, int? numeroAnio = null)
    {
        var consulta = _context.Materias.AsNoTracking()
            .Include(m => m.AnioCursada)!.ThenInclude(a => a!.Carrera)
            .Include(m => m.Correlativas)
            .AsQueryable();
        if (carreraId.HasValue)
            consulta = consulta.Where(m => m.AnioCursada!.IdCarrera == carreraId.Value);
        if (numeroAnio.HasValue)
            consulta = consulta.Where(m => m.AnioCursada!.NumeroAnio == numeroAnio.Value);

        return await consulta
            .OrderBy(m => m.AnioCursada!.Carrera!.Nombre)
            .ThenBy(m => m.AnioCursada!.NumeroAnio)
            .ThenBy(m => m.Nombre)
            .Select(m => new MateriaListaDto
            {
                IdMateria = m.IdMateria,
                Nombre = m.Nombre,
                Modalidad = m.Modalidad,
                Estado = m.Estado,
                TipoCursada = m.TipoCursada,
                NumeroPeriodo = m.NumeroPeriodo,
                IdAnio = m.IdAnio,
                IdCarrera = m.AnioCursada!.IdCarrera,
                Carrera = m.AnioCursada.Carrera!.Nombre,
                NumeroAnio = m.AnioCursada.NumeroAnio,
                Correlativas = m.Correlativas.OrderBy(c => c.Nombre).Select(c => new MateriaResumenDto
                {
                    IdMateria = c.IdMateria,
                    Nombre = c.Nombre
                }).ToList()
            }).ToListAsync();
    }

    public async Task<MateriaDto?> ObtenerMateriaPorIdAsync(int id)
    {
        var materia = await _context.Materias.AsNoTracking()
            .Include(m => m.AnioCursada)!.ThenInclude(a => a!.Carrera)
            .Include(m => m.Correlativas)
            .SingleOrDefaultAsync(m => m.IdMateria == id);
        return materia is null ? null : MapearDetalle(materia);
    }

    public async Task<bool> ActualizarMateriaAsync(int id, MateriaActualizarDto dto)
    {
        ValidarDatosBasicos(dto.Nombre, dto.Modalidad, dto.Estado, dto.IdAnio,
            dto.TipoCursada, dto.NumeroPeriodo);
        var materia = await _context.Materias
            .Include(m => m.Correlativas)
            .SingleOrDefaultAsync(m => m.IdMateria == id);
        if (materia is null) return false;

        var anio = await _context.Set<Anio>().Include(a => a.Carrera)
            .SingleOrDefaultAsync(a => a.IdAnio == dto.IdAnio)
            ?? throw new ArgumentException("El año académico indicado no existe.");
        await ValidarNombreUnicoAsync(dto.Nombre, anio.IdCarrera, id);
        await ValidarHorariosAlCambiarAnioAsync(id, dto.IdAnio);
        var correlativas = await ObtenerCorrelativasValidasAsync(dto.CorrelativasIds, anio, id);

        materia.Nombre = dto.Nombre.Trim();
        materia.Modalidad = dto.Modalidad.Trim();
        materia.Estado = dto.Estado.Trim();
        materia.TipoCursada = NormalizarTipoCursada(dto.TipoCursada);
        materia.NumeroPeriodo = NormalizarNumeroPeriodo(dto.TipoCursada, dto.NumeroPeriodo);
        materia.IdAnio = dto.IdAnio;
        materia.AnioCursada = anio;
        materia.Correlativas.Clear();
        materia.Correlativas.AddRange(correlativas);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarMateriaAsync(int id)
    {
        var materia = await _context.Materias.SingleOrDefaultAsync(m => m.IdMateria == id);
        if (materia is null) return false;
        var tieneActividad = await _context.EstudianteMaterias.AnyAsync(e => e.IdMateria == id) ||
            await _context.DocentesMaterias.AnyAsync(d => d.IdMateria == id) ||
            await _context.Set<Examen>().AnyAsync(e => e.IdMateria == id);
        if (tieneActividad)
            throw new InvalidOperationException(
                "No se puede eliminar una materia con inscripciones, docentes o evaluaciones asociadas. Marcala como inactiva.");
        var esCorrelativa = await _context.Materias.AnyAsync(m => m.Correlativas.Any(c => c.IdMateria == id));
        if (esCorrelativa)
            throw new InvalidOperationException(
                "No se puede eliminar la materia porque es correlativa de otra. Quitá primero esa correlatividad o marcala como inactiva.");
        _context.PeriodosInscripcionMaterias.RemoveRange(
            _context.PeriodosInscripcionMaterias.Where(p => p.MateriaId == id));
        _context.Materias.Remove(materia);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ValidarNombreUnicoAsync(string nombre, int carreraId, int? excluirId = null)
    {
        var normalizado = nombre.Trim();
        if (await _context.Materias.AnyAsync(m => m.IdMateria != excluirId &&
            m.AnioCursada!.IdCarrera == carreraId && m.Nombre == normalizado))
            throw new InvalidOperationException("Ya existe una materia con ese nombre dentro de la carrera.");
    }

    private async Task<List<Materia>> ObtenerCorrelativasValidasAsync(
        IEnumerable<int>? ids, Anio anioDestino, int? materiaId)
    {
        var distintos = (ids ?? []).Distinct().ToList();
        if (materiaId.HasValue && distintos.Contains(materiaId.Value))
            throw new ArgumentException("Una materia no puede ser correlativa de sí misma.");
        if (distintos.Count == 0) return [];

        var materias = await _context.Materias
            .Include(m => m.AnioCursada)
            .Where(m => distintos.Contains(m.IdMateria))
            .ToListAsync();
        if (materias.Count != distintos.Count)
            throw new ArgumentException("Una o más correlativas no existen.");
        if (materias.Any(m => m.AnioCursada!.IdCarrera != anioDestino.IdCarrera))
            throw new ArgumentException("Todas las correlativas deben pertenecer a la misma carrera.");
        if (materias.Any(m => m.AnioCursada!.NumeroAnio >= anioDestino.NumeroAnio))
            throw new ArgumentException("Las correlativas deben pertenecer a un año anterior de la misma carrera.");
        return materias;
    }

    private async Task ValidarHorariosAlCambiarAnioAsync(int materiaId, int anioDestinoId)
    {
        var propios = await _context.Set<HorarioMateria>().AsNoTracking()
            .Where(h => h.IdMateria == materiaId)
            .ToListAsync();
        if (propios.Count == 0) return;

        var otros = await _context.Set<HorarioMateria>().AsNoTracking()
            .Where(h => h.IdMateria != materiaId)
            .Join(_context.Materias.AsNoTracking().Where(m => m.IdAnio == anioDestinoId),
                h => h.IdMateria, m => m.IdMateria,
                (h, m) => new { Horario = h, Materia = m })
            .ToListAsync();
        var conflicto = otros.FirstOrDefault(otro => propios.Any(propio =>
            propio.DiaSemana == otro.Horario.DiaSemana &&
            propio.HoraInicio < otro.Horario.HoraFin &&
            otro.Horario.HoraInicio < propio.HoraFin));
        if (conflicto is not null)
            throw new InvalidOperationException(
                $"La materia no puede moverse a ese año porque su horario se superpone con {conflicto.Materia.Nombre}.");
    }

    private static void ValidarDatosBasicos(string nombre, string modalidad, string estado, int idAnio,
        string tipoCursada, int? numeroPeriodo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la materia es obligatorio.");
        NormalizadorDatos.ValidarTextoAcademico(nombre, "El nombre de la materia");
        if (idAnio <= 0)
            throw new ArgumentException("El año académico es obligatorio.");
        if (modalidad is not ("Presencial" or "Virtual" or "Híbrida"))
            throw new ArgumentException("La modalidad indicada no es válida.");
        if (estado is not ("Activa" or "Inactiva"))
            throw new ArgumentException("El estado indicado no es válido.");

        var tipo = NormalizarTipoCursada(tipoCursada);
        if (tipo == "Cuatrimestral" && numeroPeriodo is not (1 or 2))
            throw new ArgumentException("Indicá si la materia corresponde al primer o segundo cuatrimestre.");
        if (tipo == "Bimestral" && (numeroPeriodo is null or < 1 or > 5))
            throw new ArgumentException("El bimestre debe estar comprendido entre 1 y 5.");
    }

    private static string NormalizarTipoCursada(string? tipoCursada)
    {
        var tipo = tipoCursada?.Trim();
        return tipo?.ToLowerInvariant() switch
        {
            "anual" => "Anual",
            "cuatrimestral" => "Cuatrimestral",
            "bimestral" => "Bimestral",
            _ => throw new ArgumentException("El tipo de cursada debe ser anual, cuatrimestral o bimestral.")
        };
    }

    private static int? NormalizarNumeroPeriodo(string tipoCursada, int? numeroPeriodo) =>
        NormalizarTipoCursada(tipoCursada) == "Anual" ? null : numeroPeriodo;

    private static MateriaDto MapearDetalle(Materia materia) => new()
    {
        IdMateria = materia.IdMateria,
        Nombre = materia.Nombre,
        Modalidad = materia.Modalidad,
        Estado = materia.Estado,
        TipoCursada = materia.TipoCursada,
        NumeroPeriodo = materia.NumeroPeriodo,
        IdAnio = materia.IdAnio,
        IdCarrera = materia.AnioCursada!.IdCarrera,
        Carrera = materia.AnioCursada.Carrera!.Nombre,
        NumeroAnio = materia.AnioCursada.NumeroAnio,
        Correlativas = materia.Correlativas.OrderBy(c => c.Nombre).Select(c => new MateriaResumenDto
        {
            IdMateria = c.IdMateria,
            Nombre = c.Nombre
        }).ToList()
    };
}
