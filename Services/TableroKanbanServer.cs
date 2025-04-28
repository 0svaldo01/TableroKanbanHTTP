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

namespace TableroKanbanHTTP.Services
{
    public class TableroKanbanServer
    {
        HttpListener server = new();

        public event Action<ToDoDTO>? TareaRecibida;
        public event Action<ToDoDTO, int> TareaCambiada;
        byte[]? index;

        private readonly string assetsPath = "assets";

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
            try
            {
                var contexto = server.GetContext();
                new Thread(Escuchar) { IsBackground = true }.Start();
                if (contexto != null)
                {
                    ProcesarSolicitud(contexto);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error en el servidor: {ex.Message}");
            }
        }
        private void ProcesarSolicitud(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                if (context.Request.HttpMethod == "GET")
                {
                    if (path == "/" || path == "/tablero/" || path == "/tablero/index" || path == "/tablero/index.html")
                    {
                        ServirArchivoEstatico(context, "index.html", "text/html");
                    }
 
                    else if (path == "tareas")
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/json";
                        byte[] buffer = Encoding.UTF8.GetBytes("[]");
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.Close();
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
                else if (context.Request.HttpMethod == "POST")
                {
                    if (path == "/tablero/tarea")
                    {
                       
                        using StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                        string json = reader.ReadToEnd();

                        try
                        {
                            ToDoDTO tarea = JsonSerializer.Deserialize<ToDoDTO>(json);

                            if (tarea != null)
                            {
                              
                                TareaRecibida?.Invoke(tarea);

                              
                                byte[] respuesta = Encoding.UTF8.GetBytes("{\"success\": true}");
                                context.Response.StatusCode = 200;
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
                            }
                            else
                            {
                             
                                context.Response.StatusCode = 400;
                                byte[] respuesta = Encoding.UTF8.GetBytes("{\"error\": \"Formato de tarea inválido\"}");
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
                            }
                        }
                        catch (JsonException)
                        {
                           
                            context.Response.StatusCode = 400;
                            byte[] respuesta = Encoding.UTF8.GetBytes("{\"error\": \"JSON inválido\"}");
                            context.Response.ContentType = "application/json";
                            context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
                        }

                        context.Response.Close();
                    }
                    else if (path == "/tablero/cambio-estado")
                    {
                       
                        using StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                        string json = reader.ReadToEnd();

                        try
                        {
                            using JsonDocument doc = JsonDocument.Parse(json);
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("tarea", out JsonElement tareaElement) &&
                                root.TryGetProperty("estadoAnterior", out JsonElement estadoAnteriorElement))
                            {
                                ToDoDTO tarea = JsonSerializer.Deserialize<ToDoDTO>(tareaElement.GetRawText());
                                int estadoAnterior = estadoAnteriorElement.GetInt32();

                              
                                TareaCambiada?.Invoke(tarea, estadoAnterior);

                                
                                byte[] respuesta = Encoding.UTF8.GetBytes("{\"success\": true}");
                                context.Response.StatusCode = 200;
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
                            }
                            else
                            {
                                
                                context.Response.StatusCode = 400;
                                byte[] respuesta = Encoding.UTF8.GetBytes("{\"error\": \"Formato de datos inválido\"}");
                                context.Response.ContentType = "application/json";
                                context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
                            }
                        }
                        catch (Exception)
                        {
                        
                            context.Response.StatusCode = 400;
                            byte[] respuesta = Encoding.UTF8.GetBytes("{\"error\": \"Error al procesar los datos\"}");
                            context.Response.ContentType = "application/json";
                            context.Response.OutputStream.Write(respuesta, 0, respuesta.Length);
                        }
                        context.Response.Close();
                    }
                    else
                    {
                       
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error al procesar la solicitud: {ex.Message}");

                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                    
                }
            }
        }
        private void ServirArchivoEstatico(HttpListenerContext contexto, string nombreArchivo, string contentType)
        {
            try
            {
                string rutaArchivo = Path.Combine(assetsPath, nombreArchivo);

                if (File.Exists(rutaArchivo))
                {
                    byte[] contenido = File.ReadAllBytes(rutaArchivo);
                    contexto.Response.ContentLength64 = contenido.Length;
                    contexto.Response.ContentType = contentType;
                    contexto.Response.OutputStream.Write(contenido, 0, contenido.Length);
                    contexto.Response.StatusCode = 200;
                }
                else
                {
                    contexto.Response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al servir archivo estático: {ex.Message}");
                contexto.Response.StatusCode = 500;
            }

            contexto.Response.Close();
        }
    }

}
