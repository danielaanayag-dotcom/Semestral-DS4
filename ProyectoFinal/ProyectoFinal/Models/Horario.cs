using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProyectoFinal.Models
{
    public class Horario
    {
        public int HorarioID { get; set; }
        public int ServicioID { get; set; }
        public DateTime FechaHora { get; set; }
        public bool Disponible { get; set; }
    }
}