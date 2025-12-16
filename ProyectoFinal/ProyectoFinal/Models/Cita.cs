using System;

namespace ProyectoFinal.Models
{
    public class Cita
    {
        public int CitaID { get; set; }
        public int UsuarioID { get; set; }
        public int ServicioID { get; set; }
        public DateTime FechaCita { get; set; }
        public string Estado { get; set; } // Confirmada, Cancelada, Pendiente
        public string NombreServicio { get; set; }
        public string Descripcion { get; set; }

        // Opcionales si quieres mostrar info del usuario
        public string NombrePaciente { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
