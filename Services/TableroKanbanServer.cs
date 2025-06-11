using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TableroKanbanHTTP.Models;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text.Json.Serialization;
using System.Threading;

namespace TableroKanbanHTTP.Services
{
    public class TableroKanbanServer
    {
        HttpListener server = new();
        public event Action? TableroActualizado;

        private readonly string archivoTareas = "tareas.json";
        private readonly ToDoList _tablero = new();
        byte[]? index;

        public TableroKanbanServer()
        {
            _tablero = CargarTablero();
        }

        private void GuardarTablero()
        {
            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            File.WriteAllText(archivoTareas, JsonSerializer.Serialize(_tablero, opciones));
        }

        private ToDoList CargarTablero()
        {
            try
            {
                if (File.Exists(archivoTareas))
                {
                    var opciones = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };
                    string json = File.ReadAllText(archivoTareas);
                    return JsonSerializer.Deserialize<ToDoList>(json, opciones) ?? new ToDoList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cargar tablero: " + ex.Message);
            }
            return new ToDoList();
        }

        public void Iniciar()
        {
            server.Prefixes.Add("http://*:20000/tablero/");
            server.Start();

            Thread hilo = new Thread(Escuchar)
            {
                IsBackground = true
            };
            hilo.Start();
        }

        void Escuchar()
        {
            var contexto = server.GetContext();
            new Thread(Escuchar) { IsBackground = true }.Start();

            if (contexto != null)
            {
                
                if (contexto.Request.HttpMethod == "GET" &&
                   (contexto.Request.RawUrl == "/kanban/" || contexto.Request.RawUrl == "/kanban/index"))
                {
                    if (index == null)
                    {
                        index = File.ReadAllBytes("assets/index.html");
                    }

                    contexto.Response.ContentLength64 = index.Length;
                    contexto.Response.ContentType = "text/html";
                    contexto.Response.OutputStream.Write(index, 0, index.Length);
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
          
                else if (contexto.Request.HttpMethod == "GET" && contexto.Request.RawUrl == "/kanban/tablero")
                {
                    var opciones = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };

                    string json = JsonSerializer.Serialize(_tablero, opciones);
                    byte[] buffer = Encoding.UTF8.GetBytes(json);

                    contexto.Response.ContentType = "application/json";
                    contexto.Response.ContentLength64 = buffer.Length;
                    contexto.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
               
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/agregar")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);

                    var tarea = JsonSerializer.Deserialize<ToDoDTO>(json);
                    if (tarea != null)
                    {
                        
                        if (string.IsNullOrWhiteSpace(tarea.Titulo) || tarea.Titulo.Length > 50)
                        {
                            contexto.Response.StatusCode = 400;
                            contexto.Response.Close();
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(tarea.Descripcion) || tarea.Descripcion.Length > 100)
                        {
                            contexto.Response.StatusCode = 400;
                            contexto.Response.Close();
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(tarea.Nombre))
                        {
                            contexto.Response.StatusCode = 400;
                            contexto.Response.Close();
                            return;
                        }

                        tarea.Estado = Estados.Pendiente;
                        tarea.Id = _tablero.Tareas.Count > 0 ? _tablero.Tareas.Max(t => t.Id) + 1 : 1;
                        tarea.FechaCreacion = DateTime.Now;

                        _tablero.Tareas.Add(tarea);
                        GuardarTablero();
                        TableroActualizado?.Invoke();
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
            
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/mover")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);

                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null && data.ContainsKey("id") && data.ContainsKey("estado") && data.ContainsKey("usuario"))
                    {
                        int id = int.Parse(data["id"]);
                        Estados nuevoEstado = Enum.Parse<Estados>(data["estado"]);
                        string usuario = data["usuario"];

                        var tarea = _tablero.Tareas.FirstOrDefault(t => t.Id == id);
                        if (tarea != null)
                        {
                            
                            if (tarea.Nombre != usuario)
                            {
                                contexto.Response.StatusCode = 403; 
                                contexto.Response.Close();
                                return;
                            }

                       
                            var estadosOrden = new[] { Estados.Pendiente, Estados.EnProceso, Estados.Terminado };
                            int estadoActualIndex = Array.IndexOf(estadosOrden, tarea.Estado);
                            int nuevoEstadoIndex = Array.IndexOf(estadosOrden, nuevoEstado);

                           
                            if (nuevoEstadoIndex == estadoActualIndex + 1 || nuevoEstadoIndex == estadoActualIndex)
                            {
                                tarea.Estado = nuevoEstado;
                                GuardarTablero();
                                TableroActualizado?.Invoke();
                            }
                            else
                            {
                                contexto.Response.StatusCode = 400; 
                                contexto.Response.Close();
                                return;
                            }
                        }
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
               
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/eliminar")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);

                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null && data.ContainsKey("id") && data.ContainsKey("usuario"))
                    {
                        int id = int.Parse(data["id"]);
                        string usuario = data["usuario"];

                        var tarea = _tablero.Tareas.FirstOrDefault(t => t.Id == id);
                        if (tarea != null)
                        {
                          
                            if (tarea.Nombre != usuario)
                            {
                                contexto.Response.StatusCode = 403; 
                                contexto.Response.Close();
                                return;
                            }

                            _tablero.Tareas.Remove(tarea);
                            GuardarTablero();
                            TableroActualizado?.Invoke();
                        }
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
               
                else if (contexto.Request.HttpMethod == "POST" && contexto.Request.RawUrl == "/kanban/editar")
                {
                    byte[] bufferEntrada = new byte[contexto.Request.ContentLength64];
                    contexto.Request.InputStream.Read(bufferEntrada, 0, bufferEntrada.Length);
                    string json = Encoding.UTF8.GetString(bufferEntrada);

                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null && data.ContainsKey("id") && data.ContainsKey("titulo") &&
                        data.ContainsKey("descripcion") && data.ContainsKey("usuario"))
                    {
                        int id = int.Parse(data["id"]);
                        string titulo = data["titulo"];
                        string descripcion = data["descripcion"];
                        string usuario = data["usuario"];

                      
                        if (string.IsNullOrWhiteSpace(titulo) || titulo.Length > 50 ||
                            string.IsNullOrWhiteSpace(descripcion) || descripcion.Length > 100)
                        {
                            contexto.Response.StatusCode = 400;
                            contexto.Response.Close();
                            return;
                        }

                        var tarea = _tablero.Tareas.FirstOrDefault(t => t.Id == id);
                        if (tarea != null)
                        {
                           
                            if (tarea.Nombre != usuario)
                            {
                                contexto.Response.StatusCode = 403; 
                                contexto.Response.Close();
                                return;
                            }

                            tarea.Titulo = titulo;
                            tarea.Descripcion = descripcion;
                            GuardarTablero();
                            TableroActualizado?.Invoke();
                        }
                    }
                    contexto.Response.StatusCode = 200;
                    contexto.Response.Close();
                }
                else
                {
                    contexto.Response.StatusCode = 404;
                    contexto.Response.Close();
                }
            }
        }
    }
}
