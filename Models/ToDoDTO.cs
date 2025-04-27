using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableroKanbanHTTP.Models
{
    public class ToDoDTO
    {
        public string Nombre { get; set; } = "";
        public string Titulo { get; set; } = "";
        public int Estado { get; set; }
    }
}
