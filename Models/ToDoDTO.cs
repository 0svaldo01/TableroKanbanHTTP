using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableroKanbanHTTP.Models
{
    public enum Estados { Pendiente, EnProceso, Terminado }
    public class ToDoDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public Estados Estado { get; set; } = Estados.Pendiente;

        public string Nombre { get; set; } = "";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
