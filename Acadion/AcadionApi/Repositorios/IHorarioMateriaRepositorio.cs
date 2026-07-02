using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz para operaciones específicas de HorarioMateria
/// </summary>
public interface IHorarioMateriaRepositorio : IRepositorio<HorarioMateria>
{
    Task<IEnumerable<HorarioMateria>> GetByMateriaAsync(int idMateria);
    Task<IEnumerable<HorarioMateria>> GetByDiaSemanaAsync(string diaSemana);
    Task<IEnumerable<HorarioMateria>> GetHorariosMateriaAsync(int idMateria, string diaSemana);
}
