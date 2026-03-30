using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clases_Acadion.Models
{
    public class NotaExamen
    {
        [Key] public int Id { get; set; }
        public Examen Examen { get; set; }
        public Usuario Estudiante { get; set; }
        public short Calificacion { get; set; }
    }
}
