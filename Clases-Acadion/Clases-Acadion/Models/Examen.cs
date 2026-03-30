using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clases_Acadion.Models
{
    public class Examen
    {
        [Key] public int Id { get; set; }

        public Materia Materia { get; set; }
        public DateTime Fecha { get; set; }
        public Usuario Docente { get; set; }

        public string TipoExamen { get; set; } //parcial,final orecuperatorio

        //Relación con estudiantes
        public List<EstudianteExamen> EstudiantesInscriptos { get; set; } = new();

    }
}
