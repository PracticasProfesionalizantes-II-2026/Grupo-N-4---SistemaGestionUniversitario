using Microsoft.EntityFrameworkCore;


namespace AcadionApi.Datos;

public class AppDbContext : DbContext
    {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Persona> Personas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Materia> Materias { get; set; }
    public DbSet<EstudianteMateria> EstudianteMaterias { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    // Indicamos que queremos tablas separadas para la herencia
    modelBuilder.Entity<Persona>().ToTable("Personas");
    modelBuilder.Entity<Usuario>().ToTable("Usuarios");

    // Configuración de la relación entre Usuario y EstudianteMateria
    modelBuilder.Entity<EstudianteMateria>()
        .HasOne(em => em.Estudiante)
        .WithMany()
        .HasForeignKey(em => em.IdEstudiante)
        .OnDelete(DeleteBehavior.Restrict);

    // Configuración de la relación entre Materia y EstudianteMateria
    modelBuilder.Entity<EstudianteMateria>()
        .HasOne(em => em.Materia)
        .WithMany()
        .HasForeignKey(em => em.IdMateria)
        .OnDelete(DeleteBehavior.Restrict);

    // Configuración de la relación entre Usuario (Docente) y EstudianteMateria
    modelBuilder.Entity<EstudianteMateria>()
        .HasOne(em => em.Docente)
        .WithMany()
        .HasForeignKey(em => em.IdDocente)
        .OnDelete(DeleteBehavior.Restrict);

    // Configuración de Asistencia
    // Relación con EstudianteMateria
    modelBuilder.Entity<Asistencia>()
        .HasOne(a => a.Inscripcion)
        .WithMany()
        .HasForeignKey(a => a.IdEstudianteMateria)
        .OnDelete(DeleteBehavior.Cascade);

    // Relación con Usuario (Docente que toma la asistencia)
    modelBuilder.Entity<Asistencia>()
        .HasOne(a => a.Docente)
        .WithMany()
        .HasForeignKey(a => a.IdDocente)
        .OnDelete(DeleteBehavior.Restrict);

    // Configuración de Examen
    // Relación con Materia
    modelBuilder.Entity<Examen>()
        .HasOne(e => e.Materia)
        .WithMany()
        .HasForeignKey(e => e.IdMateria)
        .OnDelete(DeleteBehavior.Restrict);

    // Relación con Usuario (Docente)
    modelBuilder.Entity<Examen>()
        .HasOne(e => e.Docente)
        .WithMany()
        .HasForeignKey(e => e.IdDocente)
        .OnDelete(DeleteBehavior.Restrict);

    // Relación con NotaExamen
    modelBuilder.Entity<Examen>()
        .HasMany(e => e.Notas)
        .WithOne(n => n.Examen)
        .HasForeignKey(n => n.IdExamen)
        .OnDelete(DeleteBehavior.Cascade);

    // Configuración de Carrera
    // Relación con Anio
    modelBuilder.Entity<Carrera>()
        .HasMany(c => c.AniosAcademicos)
        .WithOne(a => a.Carrera)
        .HasForeignKey(a => a.IdCarrera)
        .OnDelete(DeleteBehavior.Cascade);

    // Configuración de Materia
    // Relación con HorarioMateria
    modelBuilder.Entity<Materia>()
        .HasMany(m => m.Horarios)
        .WithOne()
        .HasForeignKey(h => h.IdMateria)
        .OnDelete(DeleteBehavior.Cascade);

    // Configuración de Anio
    // Relación con Carrera (ya configurada arriba)
    // Relación con Materia
    modelBuilder.Entity<Anio>()
        .HasMany(a => a.Materias)
        .WithOne(m => m.AnioCursada)
        .HasForeignKey(m => m.IdAnio)
        .OnDelete(DeleteBehavior.Cascade);

    // Configuración de NotaExamen
    // Relación con Examen (ya configurada arriba)
    // Relación con Usuario (Estudiante)
    modelBuilder.Entity<NotaExamen>()
        .HasOne(n => n.Estudiante)
        .WithMany()
        .HasForeignKey(n => n.IdEstudiante)
        .OnDelete(DeleteBehavior.Restrict);
    }
}
