using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clases_Acadion.Models
{
    public class EstudianteExamen
    {
        [Key] public int Id { get; set; }

        public int EstudianteId { get; set; }
        public Usuario Estudiante { get; set; }

        public int ExamenId { get; set; }
        public Examen Examen { get; set; }

        public DateTime FechaInscripcion { get; set; }
        public string Estado { get; set; } // inscripto, ausente, aprobado, reprobado
    }
}
