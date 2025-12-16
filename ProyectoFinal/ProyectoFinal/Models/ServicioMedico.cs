using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoFinal.Models
{
    public class ServicioMedico
    {
        public int ServicioID { get; set; }
        public string NombreServicio { get; set; }
        public string Descripcion { get; set; }
        public int Duracion { get; set; }
        public bool Activo { get; set; }
    }
}

