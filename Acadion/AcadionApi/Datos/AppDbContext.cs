using Microsoft.EntityFrameworkCore;
using AcadionApi.Seguridad;


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
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Permiso> Permisos { get; set; }
    public DbSet<RolPermiso> RolesPermisos { get; set; }
    public DbSet<RegistroAsistenciaPersonal> RegistrosAsistenciaPersonal { get; set; }
    public DbSet<DocenteMateria> DocentesMaterias { get; set; }
    public DbSet<NotificacionGeneral> NotificacionesGenerales { get; set; }
    public DbSet<NotificacionLectura> NotificacionesLecturas { get; set; }
    public DbSet<PerfilFinanciamiento> PerfilesFinanciamiento { get; set; }
    public DbSet<Allegado> Allegados { get; set; }
    public DbSet<MatriculaInicial> MatriculasIniciales { get; set; }
    public DbSet<CuotaMensual> CuotasMensuales { get; set; }
    public DbSet<InscripcionExamen> InscripcionesExamenes { get; set; }
    public DbSet<PeriodoInscripcionMateria> PeriodosInscripcionMaterias { get; set; }
    public DbSet<PeriodoInscripcionExamen> PeriodosInscripcionExamenes { get; set; }
    public DbSet<RecuperacionContrasena> RecuperacionesContrasena { get; set; }
    public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }
    public DbSet<PlanEstudio> PlanesEstudio { get; set; }
    public DbSet<Comision> Comisiones { get; set; }
    public DbSet<HorarioComision> HorariosComisiones { get; set; }
    public DbSet<ListaEsperaComision> ListasEsperaComisiones { get; set; }
    public DbSet<ClaseAcademica> ClasesAcademicas { get; set; }
    public DbSet<EventoCalendario> EventosCalendario { get; set; }
    public DbSet<EquivalenciaMateria> EquivalenciasMaterias { get; set; }
    public DbSet<TurnoExamenFinal> TurnosExamenFinal { get; set; }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    // Indicamos que queremos tablas separadas para la herencia
    modelBuilder.Entity<Persona>().ToTable("Personas");
    modelBuilder.Entity<Usuario>().ToTable("Usuarios");

    modelBuilder.Entity<Usuario>()
        .Property(u => u.RolId)
        .HasColumnName("Rol");

    modelBuilder.Entity<Usuario>()
        .HasOne(u => u.Rol)
        .WithMany(r => r.Usuarios)
        .HasForeignKey(u => u.RolId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Usuario>()
        .HasIndex(u => u.NombreUsuario)
        .IsUnique();

    modelBuilder.Entity<Usuario>()
        .HasIndex(u => u.Legajo)
        .HasFilter("[Legajo] <> ''")
        .IsUnique();

    modelBuilder.Entity<Usuario>()
        .HasOne(u => u.Carrera)
        .WithMany(c => c.AlumnosInscritos)
        .HasForeignKey(u => u.CarreraId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Usuario>()
        .HasOne(u => u.PlanEstudio)
        .WithMany(p => p.Estudiantes)
        .HasForeignKey(u => u.PlanEstudioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PlanEstudio>()
        .HasOne(p => p.Carrera)
        .WithMany(c => c.PlanesEstudio)
        .HasForeignKey(p => p.CarreraId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PlanEstudio>()
        .HasIndex(p => new { p.CarreraId, p.Codigo })
        .IsUnique();

    modelBuilder.Entity<PlanEstudio>()
        .HasIndex(p => new { p.CarreraId, p.Activo });

    modelBuilder.Entity<RecuperacionContrasena>()
        .HasOne(r => r.Usuario)
        .WithMany(u => u.RecuperacionesContrasena)
        .HasForeignKey(r => r.UsuarioId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<RecuperacionContrasena>()
        .HasIndex(r => new { r.UsuarioId, r.FechaExpiracionUtc });

    modelBuilder.Entity<RecuperacionContrasena>()
        .HasIndex(r => r.TokenHash)
        .HasFilter("[TokenHash] IS NOT NULL");

    modelBuilder.Entity<RegistroAuditoria>().Property(r => r.NombreUsuario).HasMaxLength(80);
    modelBuilder.Entity<RegistroAuditoria>().Property(r => r.Rol).HasMaxLength(40);
    modelBuilder.Entity<RegistroAuditoria>().Property(r => r.Metodo).HasMaxLength(10);
    modelBuilder.Entity<RegistroAuditoria>().Property(r => r.Ruta).HasMaxLength(500);
    modelBuilder.Entity<RegistroAuditoria>().Property(r => r.Ip).HasMaxLength(64);
    modelBuilder.Entity<RegistroAuditoria>().Property(r => r.Detalle).HasMaxLength(1000);
    modelBuilder.Entity<RegistroAuditoria>().HasIndex(r => r.FechaUtc);
    modelBuilder.Entity<RegistroAuditoria>().HasIndex(r => new { r.UsuarioId, r.FechaUtc });

    modelBuilder.Entity<Persona>()
        .HasIndex(p => p.Dni)
        .IsUnique();

    modelBuilder.Entity<Permiso>()
        .HasIndex(p => p.Codigo)
        .IsUnique();

    modelBuilder.Entity<RolPermiso>()
        .HasKey(rp => new { rp.RolId, rp.PermisoId });

    modelBuilder.Entity<RolPermiso>()
        .HasOne(rp => rp.Rol)
        .WithMany(r => r.RolPermisos)
        .HasForeignKey(rp => rp.RolId);

    modelBuilder.Entity<RolPermiso>()
        .HasOne(rp => rp.Permiso)
        .WithMany(p => p.RolPermisos)
        .HasForeignKey(rp => rp.PermisoId);

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

    modelBuilder.Entity<EstudianteMateria>()
        .HasOne(em => em.Comision)
        .WithMany(c => c.Inscripciones)
        .HasForeignKey(em => em.ComisionId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Comision>()
        .HasOne(c => c.Materia)
        .WithMany(m => m.Comisiones)
        .HasForeignKey(c => c.MateriaId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Comision>()
        .HasOne(c => c.Docente)
        .WithMany()
        .HasForeignKey(c => c.DocenteId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Comision>()
        .HasIndex(c => new { c.MateriaId, c.CicloLectivo, c.Nombre })
        .IsUnique();

    modelBuilder.Entity<HorarioComision>()
        .HasOne(h => h.Comision)
        .WithMany(c => c.Horarios)
        .HasForeignKey(h => h.ComisionId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<ListaEsperaComision>()
        .HasOne(l => l.Comision)
        .WithMany(c => c.ListaEspera)
        .HasForeignKey(l => l.ComisionId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<ListaEsperaComision>()
        .HasOne(l => l.Estudiante)
        .WithMany()
        .HasForeignKey(l => l.EstudianteId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ListaEsperaComision>()
        .HasOne(l => l.ResueltoPor)
        .WithMany()
        .HasForeignKey(l => l.ResueltoPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ListaEsperaComision>()
        .HasIndex(l => new { l.ComisionId, l.EstudianteId })
        .IsUnique();

    modelBuilder.Entity<EstudianteMateria>()
        .HasIndex(em => new { em.IdEstudiante, em.IdMateria, em.CicloLectivo })
        .IsUnique();

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

    modelBuilder.Entity<Asistencia>()
        .HasIndex(a => new { a.IdEstudianteMateria, a.Fecha })
        .IsUnique();

    modelBuilder.Entity<Asistencia>()
        .HasOne(a => a.JustificadaPor)
        .WithMany()
        .HasForeignKey(a => a.JustificadaPorUsuarioId)
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
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Examen>()
        .HasOne(e => e.ExamenRecuperado)
        .WithMany(e => e.Recuperatorios)
        .HasForeignKey(e => e.ExamenRecuperadoId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Examen>()
        .HasIndex(e => e.ExamenRecuperadoId)
        .IsUnique()
        .HasFilter("[ExamenRecuperadoId] IS NOT NULL");

    modelBuilder.Entity<Examen>()
        .HasOne(e => e.TurnoExamenFinal)
        .WithMany(t => t.Examenes)
        .HasForeignKey(e => e.TurnoExamenFinalId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<TurnoExamenFinal>()
        .HasIndex(t => new { t.CicloLectivo, t.Nombre, t.NumeroLlamado })
        .IsUnique();

    modelBuilder.Entity<Carrera>()
        .HasIndex(c => new { c.Nombre, c.PlanEstudios })
        .IsUnique();

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

    modelBuilder.Entity<NotaExamen>()
        .HasIndex(n => new { n.IdExamen, n.IdEstudiante })
        .IsUnique();

    modelBuilder.Entity<NotaExamen>()
        .Property(n => n.Nota)
        .HasPrecision(4, 2);

    modelBuilder.Entity<RegistroAsistenciaPersonal>()
        .HasOne(r => r.Usuario)
        .WithMany()
        .HasForeignKey(r => r.UsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<RegistroAsistenciaPersonal>()
        .HasOne(r => r.RegistradoPor)
        .WithMany()
        .HasForeignKey(r => r.RegistradoPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<RegistroAsistenciaPersonal>()
        .HasOne(r => r.Materia)
        .WithMany()
        .HasForeignKey(r => r.MateriaId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<RegistroAsistenciaPersonal>()
        .HasOne(r => r.JustificadaPor)
        .WithMany()
        .HasForeignKey(r => r.JustificadaPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<RegistroAsistenciaPersonal>()
        .HasIndex(r => new { r.UsuarioId, r.Fecha })
        .HasFilter("[MateriaId] IS NULL")
        .IsUnique();

    modelBuilder.Entity<RegistroAsistenciaPersonal>()
        .HasIndex(r => new { r.UsuarioId, r.MateriaId, r.Fecha })
        .HasFilter("[MateriaId] IS NOT NULL")
        .IsUnique();

    modelBuilder.Entity<DocenteMateria>()
        .HasOne(dm => dm.Docente)
        .WithMany()
        .HasForeignKey(dm => dm.IdDocente)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<DocenteMateria>()
        .HasOne(dm => dm.Materia)
        .WithMany()
        .HasForeignKey(dm => dm.IdMateria)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<DocenteMateria>()
        .HasIndex(dm => new { dm.IdDocente, dm.IdMateria, dm.CicloLectivo, dm.Cuatrimestre })
        .IsUnique();

    modelBuilder.Entity<NotificacionGeneral>()
        .HasOne(n => n.RolDestino)
        .WithMany()
        .HasForeignKey(n => n.RolDestinoId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<NotificacionGeneral>()
        .HasOne(n => n.CreadaPor)
        .WithMany()
        .HasForeignKey(n => n.CreadaPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<NotificacionGeneral>()
        .HasOne(n => n.UsuarioDestino)
        .WithMany()
        .HasForeignKey(n => n.UsuarioDestinoId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<NotificacionGeneral>()
        .HasIndex(n => n.ClaveAutomatica)
        .HasFilter("[ClaveAutomatica] IS NOT NULL")
        .IsUnique();

    modelBuilder.Entity<NotificacionLectura>()
        .HasKey(l => new { l.NotificacionId, l.UsuarioId });
    modelBuilder.Entity<NotificacionLectura>()
        .HasOne(l => l.Notificacion)
        .WithMany()
        .HasForeignKey(l => l.NotificacionId)
        .OnDelete(DeleteBehavior.Cascade);
    modelBuilder.Entity<NotificacionLectura>()
        .HasOne(l => l.Usuario)
        .WithMany()
        .HasForeignKey(l => l.UsuarioId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<PerfilFinanciamiento>()
        .HasOne(p => p.Usuario)
        .WithOne()
        .HasForeignKey<PerfilFinanciamiento>(p => p.UsuarioId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Asistencia>()
        .HasOne(a => a.Clase)
        .WithMany(c => c.Asistencias)
        .HasForeignKey(a => a.ClaseId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ClaseAcademica>()
        .HasOne(c => c.Materia)
        .WithMany()
        .HasForeignKey(c => c.MateriaId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ClaseAcademica>()
        .HasOne(c => c.Comision)
        .WithMany()
        .HasForeignKey(c => c.ComisionId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ClaseAcademica>()
        .HasOne(c => c.Docente)
        .WithMany()
        .HasForeignKey(c => c.DocenteId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ClaseAcademica>()
        .HasIndex(c => new { c.MateriaId, c.ComisionId, c.Fecha })
        .IsUnique();

    modelBuilder.Entity<EventoCalendario>()
        .HasOne(e => e.Carrera)
        .WithMany()
        .HasForeignKey(e => e.CarreraId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<EventoCalendario>()
        .HasOne(e => e.CreadoPor)
        .WithMany()
        .HasForeignKey(e => e.CreadoPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<EventoCalendario>()
        .HasIndex(e => new { e.FechaInicioUtc, e.FechaFinUtc });

    modelBuilder.Entity<EquivalenciaMateria>().HasOne(e => e.Estudiante).WithMany()
        .HasForeignKey(e => e.EstudianteId).OnDelete(DeleteBehavior.Restrict);
    modelBuilder.Entity<EquivalenciaMateria>().HasOne(e => e.MateriaOrigen).WithMany()
        .HasForeignKey(e => e.MateriaOrigenId).OnDelete(DeleteBehavior.Restrict);
    modelBuilder.Entity<EquivalenciaMateria>().HasOne(e => e.MateriaDestino).WithMany()
        .HasForeignKey(e => e.MateriaDestinoId).OnDelete(DeleteBehavior.Restrict);
    modelBuilder.Entity<EquivalenciaMateria>().HasOne(e => e.OtorgadaPor).WithMany()
        .HasForeignKey(e => e.OtorgadaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
    modelBuilder.Entity<EquivalenciaMateria>()
        .HasIndex(e => new { e.EstudianteId, e.MateriaDestinoId }).IsUnique();

    modelBuilder.Entity<Materia>()
        .HasOne(m => m.PlanEstudio)
        .WithMany(p => p.Materias)
        .HasForeignKey(m => m.PlanEstudioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Examen>()
        .Property(e => e.NotaMinimaRegularizacion)
        .HasPrecision(4, 2);

    modelBuilder.Entity<Examen>()
        .Property(e => e.NotaMinimaPromocion)
        .HasPrecision(4, 2);

    modelBuilder.Entity<Anio>()
        .HasIndex(a => new { a.IdCarrera, a.NumeroAnio })
        .IsUnique();

    // Una materia puede ser correlativa de muchas materias y cada materia puede
    // requerir varias correlativas. La tabla intermedia evita la relación 1:N
    // implícita que impedía reutilizar una correlativa en distintos niveles.
    modelBuilder.Entity<Materia>()
        .HasMany(m => m.Correlativas)
        .WithMany()
        .UsingEntity<Dictionary<string, object>>(
            "MateriaCorrelativa",
            derecha => derecha.HasOne<Materia>().WithMany()
                .HasForeignKey("IdCorrelativa").OnDelete(DeleteBehavior.Restrict),
            izquierda => izquierda.HasOne<Materia>().WithMany()
                .HasForeignKey("IdMateria").OnDelete(DeleteBehavior.Cascade),
            relacion =>
            {
                relacion.HasKey("IdMateria", "IdCorrelativa");
                relacion.ToTable("MateriaCorrelativa");
            });

    modelBuilder.Entity<Allegado>()
        .HasOne(a => a.Estudiante)
        .WithMany()
        .HasForeignKey(a => a.EstudianteId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Allegado>()
        .HasIndex(a => new { a.EstudianteId, a.NombreApellido });

    modelBuilder.Entity<MatriculaInicial>()
        .HasOne(m => m.Estudiante)
        .WithMany()
        .HasForeignKey(m => m.EstudianteId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<MatriculaInicial>()
        .HasIndex(m => new { m.EstudianteId, m.PeriodoLectivo })
        .IsUnique();

    modelBuilder.Entity<MatriculaInicial>()
        .Property(m => m.Estado)
        .HasConversion<string>();

    modelBuilder.Entity<MatriculaInicial>().Property(m => m.Importe).HasPrecision(12, 2);
    modelBuilder.Entity<MatriculaInicial>().HasOne(m => m.ValidadoPor).WithMany()
        .HasForeignKey(m => m.ValidadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<CuotaMensual>()
        .HasOne(c => c.Estudiante)
        .WithMany()
        .HasForeignKey(c => c.EstudianteId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CuotaMensual>()
        .HasIndex(c => new { c.EstudianteId, c.Anio, c.Mes })
        .IsUnique();

    modelBuilder.Entity<CuotaMensual>()
        .Property(c => c.Estado)
        .HasConversion<string>();

    modelBuilder.Entity<CuotaMensual>().Property(c => c.Importe).HasPrecision(12, 2);
    modelBuilder.Entity<CuotaMensual>().HasOne(c => c.ValidadoPor).WithMany()
        .HasForeignKey(c => c.ValidadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<InscripcionExamen>()
        .HasOne(i => i.Examen)
        .WithMany()
        .HasForeignKey(i => i.ExamenId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<InscripcionExamen>()
        .HasOne(i => i.Estudiante)
        .WithMany()
        .HasForeignKey(i => i.EstudianteId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<InscripcionExamen>()
        .HasIndex(i => new { i.ExamenId, i.EstudianteId })
        .IsUnique();

    modelBuilder.Entity<PeriodoInscripcionMateria>()
        .HasOne(p => p.Carrera)
        .WithMany()
        .HasForeignKey(p => p.CarreraId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PeriodoInscripcionMateria>()
        .HasOne(p => p.Materia)
        .WithMany()
        .HasForeignKey(p => p.MateriaId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PeriodoInscripcionMateria>()
        .HasOne(p => p.ModificadoPor)
        .WithMany()
        .HasForeignKey(p => p.ModificadoPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PeriodoInscripcionMateria>()
        .HasIndex(p => new { p.CarreraId, p.CicloLectivo })
        .HasFilter("[MateriaId] IS NULL")
        .IsUnique();

    modelBuilder.Entity<PeriodoInscripcionExamen>()
        .HasOne(p => p.ModificadoPor)
        .WithMany()
        .HasForeignKey(p => p.ModificadoPorUsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<PeriodoInscripcionExamen>()
        .HasIndex(p => p.CicloLectivo)
        .IsUnique();

    modelBuilder.Entity<PeriodoInscripcionMateria>()
        .HasIndex(p => new { p.MateriaId, p.CicloLectivo })
        .HasFilter("[MateriaId] IS NOT NULL")
        .IsUnique();

    SembrarRolesYPermisos(modelBuilder);
    }

    private static void SembrarRolesYPermisos(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = RolesSistema.EstudianteId, Nombre = RolesSistema.Estudiante, Descripcion = "Acceso únicamente a su información académica." },
            new Rol { Id = RolesSistema.DocenteId, Nombre = RolesSistema.Docente, Descripcion = "Gestión de sus comisiones, asistencias, evaluaciones y notas." },
            new Rol { Id = RolesSistema.SecretarioId, Nombre = RolesSistema.Secretario, Descripcion = "Administración académica y de cuentas institucionales." },
            new Rol { Id = RolesSistema.DirectivoId, Nombre = RolesSistema.Directivo, Descripcion = "Consulta institucional y reportes." });

        modelBuilder.Entity<Permiso>().HasData(
            new Permiso { Id = 1, Codigo = PermisosSistema.UsuariosLeer, Descripcion = "Consultar usuarios." },
            new Permiso { Id = 2, Codigo = PermisosSistema.UsuariosGestionar, Descripcion = "Crear y administrar cuentas." },
            new Permiso { Id = 3, Codigo = PermisosSistema.AcademicoLeer, Descripcion = "Consultar carreras, años y materias." },
            new Permiso { Id = 4, Codigo = PermisosSistema.AcademicoGestionar, Descripcion = "Administrar la estructura académica." },
            new Permiso { Id = 5, Codigo = PermisosSistema.InscripcionesPropiasLeer, Descripcion = "Consultar sus materias." },
            new Permiso { Id = 6, Codigo = PermisosSistema.InscripcionesGestionar, Descripcion = "Administrar inscripciones y asignaciones." },
            new Permiso { Id = 7, Codigo = PermisosSistema.DocenciaGestionar, Descripcion = "Gestionar las comisiones asignadas." },
            new Permiso { Id = 8, Codigo = PermisosSistema.AsistenciasPropiasLeer, Descripcion = "Consultar sus asistencias." },
            new Permiso { Id = 9, Codigo = PermisosSistema.AsistenciaPersonalGestionar, Descripcion = "Registrar fichado del personal." },
            new Permiso { Id = 10, Codigo = PermisosSistema.ExamenesPropiosLeer, Descripcion = "Consultar sus evaluaciones." },
            new Permiso { Id = 11, Codigo = PermisosSistema.NotasPropiasLeer, Descripcion = "Consultar sus calificaciones." },
            new Permiso { Id = 12, Codigo = PermisosSistema.ReportesLeer, Descripcion = "Consultar reportes institucionales." },
            new Permiso { Id = 13, Codigo = PermisosSistema.NotificacionesGestionar, Descripcion = "Publicar y administrar notificaciones generales." });

        modelBuilder.Entity<RolPermiso>().HasData(
            new { RolId = 1, PermisoId = 5 },
            new { RolId = 1, PermisoId = 8 }, new { RolId = 1, PermisoId = 10 },
            new { RolId = 1, PermisoId = 11 },
            new { RolId = 2, PermisoId = 3 }, new { RolId = 2, PermisoId = 7 },
            new { RolId = 3, PermisoId = 1 }, new { RolId = 3, PermisoId = 2 },
            new { RolId = 3, PermisoId = 3 }, new { RolId = 3, PermisoId = 4 },
            new { RolId = 3, PermisoId = 5 }, new { RolId = 3, PermisoId = 6 },
            new { RolId = 3, PermisoId = 7 }, new { RolId = 3, PermisoId = 8 },
            new { RolId = 3, PermisoId = 9 }, new { RolId = 3, PermisoId = 10 },
            new { RolId = 3, PermisoId = 11 }, new { RolId = 3, PermisoId = 12 },
            new { RolId = 3, PermisoId = 13 },
            new { RolId = 4, PermisoId = 1 }, new { RolId = 4, PermisoId = 3 },
            new { RolId = 4, PermisoId = 12 });
    }
}
