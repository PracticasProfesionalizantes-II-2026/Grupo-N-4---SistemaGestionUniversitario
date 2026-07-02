using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AcadionApi.Repositorios;

/// <summary>
/// Interfaz genérica base para operaciones CRUD
/// </summary>
public interface IRepositorio<T> where T : class
{
    // CREAR
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

    // LEER
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    // ACTUALIZAR
    Task<T> UpdateAsync(T entity);

    // ELIMINAR
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAsync(T entity);
    Task<int> DeleteRangeAsync(IEnumerable<T> entities);

    // OTROS
    Task<int> CountAsync();
    Task<bool> ExistsAsync(int id);
    Task<int> SaveChangesAsync();
}
