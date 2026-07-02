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
    // ENUMS
    public Rol Rol { get; set; }
    public EstadoUsuario Estado { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaUltimoAcceso { get; set; }
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    //public string FotoPerfil { get; set; } = string.Empty;

    // SECCIÓN TEMPORAL: Atributos específicos del diagrama
    
    // Solo Estudiante
    public string Matricula { get; set; } = string.Empty;
    public string Legajo { get; set; } = string.Empty;
    public double PromedioGeneral { get; set; }
    
    // Solo Docente
    public string Especialidad { get; set; } = string.Empty;
    public string TituloAcademico { get; set; } = string.Empty;
}