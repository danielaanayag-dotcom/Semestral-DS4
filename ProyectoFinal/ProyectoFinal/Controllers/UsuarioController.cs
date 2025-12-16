using ProyectoFinal.Data;
using ProyectoFinal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace GestionCitasMedicas.Controllers
{
    public class UsuarioController : Controller
    {
        private DatabaseConnection db = new DatabaseConnection();

        // Verificar si el usuario está logueado
        private bool VerificarSesion()
        {
            return Session["UsuarioID"] != null && Session["Rol"]?.ToString() == "Usuario";
        }

        // GET: Dashboard del Usuario
        public ActionResult Index()
        {
            if (!VerificarSesion())
                return RedirectToAction("Login", "Auth");

            ViewBag.Nombre = Session["Nombre"];
            return View();
        }

        // GET: Reservar Cita
        public ActionResult ReservarCita()
        {
            if (!VerificarSesion())
                return RedirectToAction("Login", "Auth");

            // Obtener servicios disponibles
            DataTable servicios = db.ExecuteStoredProcedure("sp_ObtenerServicios");
            List<ServicioMedico> listaServicios = new List<ServicioMedico>();

            foreach (DataRow row in servicios.Rows)
            {
                listaServicios.Add(new ServicioMedico
                {
                    ServicioID = Convert.ToInt32(row["ServicioID"]),
                    NombreServicio = row["NombreServicio"].ToString(),
                    Descripcion = row["Descripcion"].ToString(),
                    Duracion = Convert.ToInt32(row["Duracion"])
                });
            }

            return View(listaServicios);
        }

        // POST: Obtener horarios disponibles por servicio (AJAX)
        [HttpPost]
        public JsonResult ObtenerHorarios(int servicioID)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@ServicioID", servicioID)
                };

                DataTable horarios = db.ExecuteStoredProcedure("sp_ObtenerHorariosDisponibles", parameters);
                List<object> listaHorarios = new List<object>();

                foreach (DataRow row in horarios.Rows)
                {
                    listaHorarios.Add(new
                    {
                        HorarioID = row["HorarioID"],
                        FechaHora = Convert.ToDateTime(row["FechaHora"]).ToString("dd/MM/yyyy HH:mm")
                    });
                }

                return Json(new { success = true, horarios = listaHorarios });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Crear Cita (Ahora es Solicitar)
        [HttpPost]
        public ActionResult CrearCita(int servicioID, int horarioID, string fechaCita)
        {
            if (!VerificarSesion())
                return RedirectToAction("Login", "Auth");

            try
            {
                int usuarioID = Convert.ToInt32(Session["UsuarioID"]);

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UsuarioID", usuarioID),
                    new SqlParameter("@HorarioID", horarioID),
                    new SqlParameter("@ServicioID", servicioID),
                    new SqlParameter("@FechaCita", DateTime.ParseExact(fechaCita, "dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture))
                };

                db.ExecuteStoredProcedure("sp_SolicitarCita", parameters);

                TempData["Mensaje"] = "Solicitud de cita enviada. Espera confirmación del administrador";
                return RedirectToAction("MisCitas");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al solicitar cita: " + ex.Message;
                return RedirectToAction("ReservarCita");
            }
        }

        // GET: Mis Citas
        public ActionResult MisCitas()
        {
            if (!VerificarSesion())
                return RedirectToAction("Login", "Auth");

            int usuarioID = Convert.ToInt32(Session["UsuarioID"]);

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UsuarioID", usuarioID)
            };

            DataTable citas = db.ExecuteStoredProcedure("sp_ObtenerCitasUsuario", parameters);
            List<Cita> listaCitas = new List<Cita>();

            foreach (DataRow row in citas.Rows)
            {
                listaCitas.Add(new Cita
                {
                    CitaID = Convert.ToInt32(row["CitaID"]),
                    FechaCita = Convert.ToDateTime(row["FechaCita"]),
                    Estado = row["Estado"].ToString(),
                    NombreServicio = row["NombreServicio"].ToString(),
                    Descripcion = row["Descripcion"].ToString()
                });
            }

            return View(listaCitas);
        }

        // POST: Cancelar Cita
        [HttpPost]
        public ActionResult CancelarCita(int citaID)
        {
            if (!VerificarSesion())
                return RedirectToAction("Login", "Auth");

            try
            {
                int usuarioID = Convert.ToInt32(Session["UsuarioID"]);

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@CitaID", citaID),
                    new SqlParameter("@UsuarioID", usuarioID)
                };

                db.ExecuteNonQuery("sp_CancelarCita", parameters);

                TempData["Mensaje"] = "Cita cancelada exitosamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cancelar cita: " + ex.Message;
            }

            return RedirectToAction("MisCitas");
        }
    }
}