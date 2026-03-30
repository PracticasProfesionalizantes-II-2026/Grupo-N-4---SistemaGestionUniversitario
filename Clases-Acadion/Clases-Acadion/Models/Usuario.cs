using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clases_Acadion.Models
{
    public class Usuario : Persona
    {
        [Key] public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Contraseña { get; set; }
        public Rol Rol { get; set; }
        public EstadoUsuario Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimoAcceso { get; set; }
        public string EmailInstitucional { get; set; }
        public string TelefonoContacto { get; set; }
        public string FotoPerfil { get; set; }

        //Propiedades específicas según el rol

        //Estudiante
        public string Matricula { get; set; }
        public string Legajo { get; set; }
        public Carrera Carrera { get; set; }
        //public PlanEstudios PlanEstudios { get; set; }
        public double PromedioGeneral { get; set; }
        public DateTime? FechaInscripcion { get; set; }

        // Docente
        public string Especialidad { get; set; }
        public string TituloAcademico { get; set; }
        public List<Materia> MateriasDictadas { get; set; }

        //Secretario o Directivo
        public string Dependencia { get; set; }
        public int? NivelAcceso { get; set; }
        public string Observaciones { get; set; }

        public Usuario()
        {
            MateriasDictadas = new List<Materia>();
        }

        public List<Estudiante_Materia> MateriasCursadas { get; set; } = new();
        public List<EstudianteExamen> ExamenesInscriptos { get; set; } = new();
        public List<Asistencia> Asistencias { get; set; } = new();
    }
}
