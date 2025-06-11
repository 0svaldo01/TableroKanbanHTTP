using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TableroKanbanHTTP.Models;
using TableroKanbanHTTP.Services;

namespace TableroKanbanHTTP.ViewModels
{
    public class DatosTableroViewModel
    {
        TableroKanbanServer TableroKanbanServer = new();
        public DatosTableroViewModel()
        {
            TableroKanbanServer.Iniciar();
        }
    }
}
