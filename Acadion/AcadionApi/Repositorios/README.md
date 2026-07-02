# Repositorios - Documentación

## Descripción General

Esta carpeta contiene la capa de acceso a datos de la aplicación Acadion, implementando el patrón Repository con una interfaz genérica base y repositorios específicos para cada entidad.

## Estructura

### Archivos Principales

1. **IRepositorio.cs** - Interfaz genérica base
   - Define operaciones CRUD comunes (Add, Get, Update, Delete)
   - Contiene métodos para búsquedas y conteos

2. **Repositorio.cs** - Implementación genérica base
   - Implementa IRepositorio<T>
   - Proporciona las operaciones CRUD básicas
   - Usa Entity Framework Core para acceso a datos

3. **IUnitOfWork.cs** - Interfaz del patrón UnitOfWork
   - Centraliza acceso a todos los repositorios
   - Maneja transacciones

4. **UnitOfWork.cs** - Implementación del UnitOfWork
   - Proporciona acceso lazy-loaded a todos los repositorios
   - Implementa manejo de transacciones

### Repositorios Específicos

Cada entidad tiene su propio par interfaz-implementación:

| Entidad | Interfaz | Implementación |
|---------|----------|----------------|
| Persona | IPersonaRepository | PersonaRepository |
| Usuario | IUsuarioRepository | UsuarioRepository |
| Carrera | ICarreraRepository | CarreraRepository |
| Anio | IAnioRepository | AnioRepository |
| Materia | IMateriaRepository | MateriaRepository |
| HorarioMateria | IHorarioMateriaRepository | HorarioMateriaRepository |
| EstudianteMateria | IEstudianteMateriaRepository | EstudianteMateriaRepository |
| Asistencia | IAsistenciaRepository | AsistenciaRepository |
| Examen | IExamenRepository | ExamenRepository |
| NotaExamen | INotaExamenRepository | NotaExamenRepository |

## Métodos Disponibles

### Métodos Base (en IRepository<T>)

```csharp
// CREAR
Task<T> AddAsync(T entity)
Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)

// LEER
Task<T?> GetByIdAsync(int id)
Task<IEnumerable<T>> GetAllAsync()
Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)

// ACTUALIZAR
Task<T> UpdateAsync(T entity)

// ELIMINAR
Task<bool> DeleteAsync(int id)
Task<bool> DeleteAsync(T entity)
Task<int> DeleteRangeAsync(IEnumerable<T> entities)

// OTROS
Task<int> CountAsync()
Task<bool> ExistsAsync(int id)
Task<int> SaveChangesAsync()
```

### Métodos Específicos por Repositorio

#### PersonaRepository
- `GetByDniAsync(long dni)` - Buscar por DNI
- `GetByEmailAsync(string email)` - Buscar por email
- `GetByApellidoAsync(string apellido)` - Buscar por apellido
- `GetByLocalidadAsync(string localidad)` - Buscar por localidad

#### UsuarioRepository
- `GetByNombreUsuarioAsync(string nombreUsuario)` - Buscar por nombre de usuario
- `GetByEmailInstitucionalAsync(string email)` - Buscar por email institucional
- `GetByRolAsync(Rol rol)` - Obtener usuarios por rol
- `GetByEstadoAsync(EstadoUsuario estado)` - Obtener usuarios por estado
- `GetEstudiantesAsync()` - Obtener todos los estudiantes
- `GetDocentesAsync()` - Obtener todos los docentes
- `GetActivosAsync()` - Obtener usuarios activos
- `ValidarCredencialesAsync(string nombreUsuario, string contrasena)` - Validar login

#### CarreraRepository
- `GetByNombreAsync(string nombre)` - Buscar por nombre
- `GetAllWithAniosAsync()` - Obtener con años académicos
- `GetWithMateriasAsync(int id)` - Obtener con todas las materias
- `GetAllWithAlumnosAsync()` - Obtener con alumnos inscritos

#### MateriaRepository
- `GetByNombreAsync(string nombre)` - Buscar por nombre
- `GetByAnioAsync(int idAnio)` - Obtener materias de un año
- `GetWithHorariosAsync(int id)` - Obtener con horarios
- `GetWithCorrelativasAsync(int id)` - Obtener con correlativas
- `GetByModalidadAsync(string modalidad)` - Filtrar por modalidad
- `GetActivasAsync()` - Obtener materias activas

#### EstudianteMateriaRepository
- `GetByEstudianteAsync(int idEstudiante)` - Materias de un estudiante
- `GetByMateriaAsync(int idMateria)` - Estudiantes de una materia
- `GetByDocenteAsync(int idDocente)` - Inscripciones de un docente
- `GetInscripcionAsync(int idEstudiante, int idMateria)` - Inscripción específica
- `GetByCicloLectivoAsync(int cicloLectivo)` - Filtrar por ciclo lectivo
- `GetByEstadoAsync(string estado)` - Filtrar por estado
- `GetWithInformacionCompletaAsync(int idEstudiante)` - Con todas las relaciones

#### ExamenRepository
- `GetByMateriaAsync(int idMateria)` - Exámenes de una materia
- `GetByDocenteAsync(int idDocente)` - Exámenes de un docente
- `GetByFechaAsync(DateTime fecha)` - Exámenes de una fecha
- `GetByTipoAsync(string tipoExamen)` - Filtrar por tipo
- `GetByCicloLectivoAsync(int cicloLectivo)` - Filtrar por ciclo
- `GetWithNotasAsync(int id)` - Con todas las notas
- `GetProximosExamenesAsync()` - Próximos exámenes

#### NotaExamenRepository
- `GetByExamenAsync(int idExamen)` - Notas de un examen
- `GetByEstudianteAsync(int idEstudiante)` - Notas de un estudiante
- `GetNotaEstudianteExamenAsync(int idExamen, int idEstudiante)` - Nota específica
- `GetNotasAprobatorasAsync()` - Notas ≥ 4
- `GetNotasDesaprobatorasAsync()` - Notas < 4
- `GetPromedioEstudianteAsync(int idEstudiante)` - Promedio del estudiante
- `GetNotaMaximaExamenAsync(int idExamen)` - Nota máxima
- `GetNotaMinimaExamenAsync(int idExamen)` - Nota mínima

## Cómo Usar

### Inyección de Dependencia en Program.cs

```csharp
// Registrar el DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Registrar los repositorios
builder.Services.AddScoped<IPersonaRepository, PersonaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
// ... registrar todos los repositorios

// O registrar el UnitOfWork (recomendado)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### Usar en un Servicio o Controlador

```csharp
public class UsuarioService
{
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario)
    {
        return await _unitOfWork.Usuarios.GetByNombreUsuarioAsync(nombreUsuario);
    }

    public async Task<IEnumerable<Usuario>> ObtenerTodosLosEstudiantesAsync()
    {
        return await _unitOfWork.Usuarios.GetEstudiantesAsync();
    }

    public async Task CrearUsuarioAsync(Usuario usuario)
    {
        await _unitOfWork.Usuarios.AddAsync(usuario);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Usar Transacciones

```csharp
public async Task TransferirEstudianteAsync(int idEstudiante, int idCarreraOrigen, int idCarreraDestino)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();

        // Realizar múltiples operaciones
        var estudiante = await _unitOfWork.Usuarios.GetByIdAsync(idEstudiante);
        // ... lógica de negocio

        await _unitOfWork.CommitAsync();
    }
    catch
    {
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

## Patrones Implementados

1. **Repository Pattern** - Abstrae acceso a datos
2. **Unit of Work Pattern** - Centraliza acceso a repositorios
3. **Dependency Injection** - Facilita testing y mantenimiento
4. **Async/Await** - Operaciones asincrónicas para mejor rendimiento
5. **Generic Base Class** - DRY (Don't Repeat Yourself)

## Consideraciones Importantes

- Todos los métodos son **asincrónico** (async/await)
- Las relaciones entre entidades se cargan con **Include()** cuando es necesario
- Implementar **validaciones de negocio** en servicios, no en repositorios
- Los repositorios solo manejan **persistencia de datos**
- Siempre llamar a **SaveChangesAsync()** después de cambios

## Próximos Pasos

- Crear servicios de aplicación que usen estos repositorios
- Implementar logging y manejo de excepciones
- Crear DTOs para transferencia de datos
- Implementar validaciones de negocio
- Crear tests unitarios para los repositorios
