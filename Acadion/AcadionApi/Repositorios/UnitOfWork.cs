using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using AcadionApi.Datos;

namespace AcadionApi.Repositorios;

/// <summary>
/// Implementación del patrón UnitOfWork
/// Centraliza todos los repositorios y maneja transacciones
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IPersonaRepositorio? _personasRepository;
    private IUsuarioRepositorio? _usuariosRepository;
    private ICarreraRepositorio? _carrerasRepository;
    private IAnioRepositorio? _aniosRepository;
    private IMateriaRepositorio? _materiasRepository;
    private IHorarioMateriaRepositorio? _horariosMaterialRepository;
    private IEstudianteMateriaRepositorio? _estudiantesMaterialRepository;
    private IAsistenciaRepositorio? _asistenciasRepository;
    private IExamenRepositorio? _examenesRepository;
    private INotaExamenRepositorio? _notasExamenesRepository;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Propiedades para acceder a los repositorios
    public IPersonaRepositorio Personas => _personasRepository ??= new PersonaRepositorio(_context);
    public IUsuarioRepositorio Usuarios => _usuariosRepository ??= new UsuarioRepositorio(_context);
    public ICarreraRepositorio Carreras => _carrerasRepository ??= new CarreraRepositorio(_context);
    public IAnioRepositorio Anios => _aniosRepository ??= new AnioRepositorio(_context);
    public IMateriaRepositorio Materias => _materiasRepository ??= new MateriaRepositorio(_context);
    public IHorarioMateriaRepositorio HorariosMaterias => _horariosMaterialRepository ??= new HorarioMateriaRepositorio(_context);
    public IEstudianteMateriaRepositorio EstudiantesMaterias => _estudiantesMaterialRepository ??= new EstudianteMateriaRepositorio(_context);
    public IAsistenciaRepositorio Asistencias => _asistenciasRepository ??= new AsistenciaRepositorio(_context);
    public IExamenRepositorio Examenes => _examenesRepository ??= new ExamenRepositorio(_context);
    public INotaExamenRepositorio NotasExamenes => _notasExamenesRepository ??= new NotaExamenRepositorio(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<bool> BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
        return true;
    }

    public async Task<bool> CommitAsync()
{
    try
    {
        await SaveChangesAsync();
        
        // Verificamos con un IF tradicional si la transacción existe
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
        }
        
        return true;
    }
    catch
    {
        await RollbackAsync();
        throw;
    }
    finally
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        _transaction = null;
    }
}

    public async Task<bool> RollbackAsync()
{
    try
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }
        return true;
    }
    finally
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        _transaction = null;
    }
}

    public void Dispose()
    {
        _context.Dispose();
    }
}
