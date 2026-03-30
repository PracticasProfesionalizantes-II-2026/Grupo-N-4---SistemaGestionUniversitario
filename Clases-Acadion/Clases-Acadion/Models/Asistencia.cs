using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clases_Acadion.Models
{
    public class Asistencia
    {
        [Key] public int Id { get; set; }

        //Estudiante
        public int EstudianteId { get; set; }
        public Usuario Estudiante { get; set; }

        //Docente
        public int DocenteId { get; set; }
        public Usuario Docente { get; set; }

        public int MateriaId { get; set; }
        public Materia Materia { get; set; }

        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } //Presente, Ausenteo Justificada
    }
}
