namespace AcadionApi.DTOs;

public class SecretarioInicialCrearDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public long Dni { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;
    public int CodigoPostal { get; set; }
    public string EmailPersonal { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
}

public class CuentaInstitucionalCrearDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public long Dni { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;
    public int CodigoPostal { get; set; }
    public string EmailPersonal { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public int RolId { get; set; }
    public int? CarreraId { get; set; }
    public int? PlanEstudioId { get; set; }
    public string EstadoMatriculaInicial { get; set; } = "PENDIENTE";
    public string EstadoCuotaActual { get; set; } = "PENDIENTE";
    public string Especialidad { get; set; } = string.Empty;
    public string TituloAcademico { get; set; } = string.Empty;
    public List<AsignacionInicialMateriaDto> Materias { get; set; } = new();
}

public class AsignacionInicialMateriaDto
{
    public int MateriaId { get; set; }
    public int? DocenteId { get; set; }
    public int CicloLectivo { get; set; }
    public string Cuatrimestre { get; set; } = "1C";
}

public class CuentaInstitucionalCreadaDto
{
    public int UsuarioId { get; set; }
    public int PersonaId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordInicial { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Legajo { get; set; } = string.Empty;
    public int? CarreraId { get; set; }
    public int? PlanEstudioId { get; set; }
    public string PlanEstudio { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public bool DebeCambiarPassword { get; set; }
}

public class UsuarioGestionDto
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public long Dni { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;
    public int CodigoPostal { get; set; }
    public string EmailPersonal { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? CarreraId { get; set; }
    public int? PlanEstudioId { get; set; }
    public string PlanEstudio { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public string Legajo { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string TituloAcademico { get; set; } = string.Empty;
    public string FotoPerfilUrl { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class UsuarioGestionActualizarDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public long Dni { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Localidad { get; set; } = string.Empty;
    public int CodigoPostal { get; set; }
    public string EmailPersonal { get; set; } = string.Empty;
    public string EmailInstitucional { get; set; } = string.Empty;
    public string TelefonoContacto { get; set; } = string.Empty;
    public int? CarreraId { get; set; }
    public int? PlanEstudioId { get; set; }
    public string Estado { get; set; } = "Activo";
    public string Especialidad { get; set; } = string.Empty;
    public string TituloAcademico { get; set; } = string.Empty;
}
