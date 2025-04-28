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
    public class DatosTableroViewModel : INotifyPropertyChanged
    {
        private readonly TableroKanbanServer _server;
        private readonly string _dataFilePath = "tareas.json";
        public ObservableCollection<ToDoDTO> TareasToDo { get; } = new ObservableCollection<ToDoDTO>();
        public ObservableCollection<ToDoDTO> TareasDone { get; } = new ObservableCollection<ToDoDTO>();
        public ObservableCollection<ToDoDTO> TareasDoing { get; } = new ObservableCollection<ToDoDTO>();
        public ObservableCollection<string> UsuariosConectados { get; } = new ObservableCollection<string>();
        private int _tareasTotales;

        public int TareasTotales
        {
            get { return _tareasTotales; }
            set { _tareasTotales = value; }
        }
        private int _tareasEnToDo;

        public int TareasEnToDo
        {
            get { return _tareasEnToDo; }
            set { _tareasEnToDo = value; }
        }
        private int _tareasEnDoing;

        public int TareasEnDoing
        {
            get { return _tareasEnDoing; }
            set { _tareasEnDoing = value; }
        }
        private int _tareasEnDone;

        public int TareasEnDone
        {
            get { return _tareasEnDone; }
            set { _tareasEnDone = value; }
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public DatosTableroViewModel(TableroKanbanServer server)
        {
            _server = server;
            _server.TareaRecibida += ManejarTareaRecibida;
          
            CargarTareasGuardadas();
            _server.Iniciar();
        }

     

        private void ManejarTareaRecibida(ToDoDTO tarea)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ToDoDTO tareaAEliminar;   
                switch (tarea.Estado)
                {
                    case 1:
                        tareaAEliminar = TareasDoing.FirstOrDefault(t =>
                            t.Nombre == tarea.Nombre && t.Titulo == tarea.Titulo);
                        if (tareaAEliminar != null&& tareaAEliminar.Estado == 2)
                        {
                            TareasDoing.Remove(tareaAEliminar);
                        }
                        else if(tareaAEliminar!= null && tareaAEliminar.Estado== 3)
                        {
                            TareasDone.Remove(tareaAEliminar);
                        }
                        TareasToDo.Add(tarea);
                        break;
                    case 2:
                        tareaAEliminar = TareasDoing.FirstOrDefault(t =>
                          t.Nombre == tarea.Nombre && t.Titulo == tarea.Titulo);
                        if (tareaAEliminar != null && tareaAEliminar.Estado == 1)
                        {
                            TareasToDo.Remove(tareaAEliminar);
                        }
                        else if (tareaAEliminar != null && tareaAEliminar.Estado == 3)
                        {
                            TareasDone.Remove(tareaAEliminar);
                        }
                        TareasDoing.Add(tarea);
                        break;
                    case 3:
                        tareaAEliminar = TareasDoing.FirstOrDefault(t =>
                          t.Nombre == tarea.Nombre && t.Titulo == tarea.Titulo);
                        if (tareaAEliminar != null && tareaAEliminar.Estado == 1)
                        {
                            TareasToDo.Remove(tareaAEliminar);
                        }
                        else if (tareaAEliminar != null && tareaAEliminar.Estado == 2)
                        {
                            TareasDoing.Remove(tareaAEliminar);
                        }
                        TareasDone.Add(tarea);
                        break;
                }
                if (!UsuariosConectados.Contains(tarea.Nombre))
                {
                    UsuariosConectados.Add(tarea.Nombre);
                }
                ActualizarEstadisticas();
                GuardarTareas();
            });
        }
        private void ActualizarEstadisticas()
        {
            TareasEnToDo = TareasToDo.Count;
            TareasEnDoing = TareasDoing.Count;
            TareasEnDone = TareasDone.Count;
            TareasTotales = TareasEnToDo + TareasEnDoing + TareasEnDone;
        }
        private void GuardarTareas()
        {
            try
            {
                List<ToDoDTO> todasLasTareas = new List<ToDoDTO>();
                todasLasTareas.AddRange(TareasToDo);
                todasLasTareas.AddRange(TareasDoing);
                todasLasTareas.AddRange(TareasDone);

                string json = JsonSerializer.Serialize(todasLasTareas);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar tareas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CargarTareasGuardadas()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    string json = File.ReadAllText(_dataFilePath);
                    List<ToDoDTO> tareas = JsonSerializer.Deserialize<List<ToDoDTO>>(json);

                    if (tareas != null)
                    {
                        foreach (var tarea in tareas)
                        {
                            switch (tarea.Estado)
                            {
                                case 1:
                                    TareasToDo.Add(tarea);
                                    break;
                                case 2:
                                    TareasDoing.Add(tarea);
                                    break;
                                case 3:
                                    TareasDone.Add(tarea);
                                    break;
                            }
                            if (!UsuariosConectados.Contains(tarea.Nombre))
                            {
                                UsuariosConectados.Add(tarea.Nombre);
                            }
                        }          
                        ActualizarEstadisticas();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar tareas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
