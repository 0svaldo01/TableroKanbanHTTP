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

namespace TableroKanbanHTTP.Services
{
    public class TableroKanbanServer
    {
        HttpListener server = new();

        public event Action<ToDoDTO>? TareaRecibida;

        byte[]? index;



        public void Iniciar()
        {
            server.Prefixes.Add("http://*:35000/tareas/");
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
                    (contexto.Request.RawUrl == "/tablero/" || contexto.Request.RawUrl == "/tablero/index"))
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




        }


    }



}
