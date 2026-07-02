namespace AcadionApi.DTOs
{
    // =========================
    // CREAR USUARIO
    // =========================
    public class UsuarioCrearDto
    {
        // Persona ya existente
        public int PersonaId { get; set; }

        // Datos propios del usuario
        public string NombreUsuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Rol { get; set; }
        public int Estado { get; set; }
        public string TelefonoContacto { get; set; } = string.Empty;

        // Estudiante
        //public string Matricula { get; set; } = string.Empty;
        //public string Legajo { get; set; } = string.Empty;
        // Docente
        //public string Especialidad { get; set; } = string.Empty;
        //public string TituloAcademico { get; set; } = string.Empty;
    }
    // =========================
    // RESPUESTA INDIVIDUAL
    // =========================
    public class UsuarioDto
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        // Datos de Persona
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public long Dni { get; set; }
        public DateTime FechaNacimiento { get; set; }
        // Datos de Usuario
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
        public string TelefonoContacto { get; set; } = string.Empty;
        // Estudiante
        public string Matricula { get; set; } = string.Empty;
        public string Legajo { get; set; } = string.Empty;
        public double PromedioGeneral { get; set; }
        // Docente
        public string Especialidad { get; set; } = string.Empty;
        public string TituloAcademico { get; set; } = string.Empty;
    }
    // =========================
    // LISTADO
    // =========================
    public class UsuarioListaDto
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    // =========================
    // ACTUALIZAR
    // =========================
    public class UsuarioActualizarDto
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Rol { get; set; }
        public int Estado { get; set; }
        public string TelefonoContacto { get; set; } = string.Empty;
        // Estudiante
        public string Matricula { get; set; } = string.Empty;
        public string Legajo { get; set; } = string.Empty;
        public double PromedioGeneral { get; set; }
        // Docente
        public string Especialidad { get; set; } = string.Empty;
        public string TituloAcademico { get; set; } = string.Empty;
    }
}