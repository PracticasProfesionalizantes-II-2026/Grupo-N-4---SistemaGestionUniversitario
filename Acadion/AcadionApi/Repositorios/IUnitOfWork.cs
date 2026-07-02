using System;
using System.Threading.Tasks;

namespace AcadionApi.Repositorios;

/// <summary>
/// Patrón UnitOfWork para centralizar todos los repositorios
/// Facilita el acceso a todos los repositorios desde una única clase
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositorios
    IPersonaRepositorio Personas { get; }
    IUsuarioRepositorio Usuarios { get; }
    ICarreraRepositorio Carreras { get; }
    IAnioRepositorio Anios { get; }
    IMateriaRepositorio Materias { get; }
    IHorarioMateriaRepositorio HorariosMaterias { get; }
    IEstudianteMateriaRepositorio EstudiantesMaterias { get; }
    IAsistenciaRepositorio Asistencias { get; }
    IExamenRepositorio Examenes { get; }
    INotaExamenRepositorio NotasExamenes { get; }

    // Guardar cambios
    Task<int> SaveChangesAsync();
    Task<bool> BeginTransactionAsync();
    Task<bool> CommitAsync();
    Task<bool> RollbackAsync();
}
