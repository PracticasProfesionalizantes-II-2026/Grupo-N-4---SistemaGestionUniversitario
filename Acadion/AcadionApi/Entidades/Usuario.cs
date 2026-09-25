using System;
using System.Collections.Generic;

public class Usuario
{
    public int Id { get; set; }
    // RELACIÓN CON PERSONA
    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;
    // LOGIN
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    // Cada usuario tiene un rol; los permisos se administran desde el rol.
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public EstadoUsuario Estado { get; set; }
    public bool DebeCambiarPassword { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaUltimoAcceso { get; set; }
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public string FotoPerfilUrl { get; set; } = string.Empty;

    // Los estudiantes pertenecen a una carrera. Para el resto de los roles es nulo.
    public int? CarreraId { get; set; }
    public Carrera? Carrera { get; set; }

    // SECCIÓN TEMPORAL: Atributos específicos del diagrama
    
    // Solo Estudiante
    public string Matricula { get; set; } = string.Empty;
    public string Legajo { get; set; } = string.Empty;
    public double PromedioGeneral { get; set; }
    
    // Solo Docente
    public string Especialidad { get; set; } = string.Empty;
    public string TituloAcademico { get; set; } = string.Empty;
}
