
using ProyectoFinal.Data;
using ProyectoFinal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace GestionCitasMedicas.Controllers
{
    public class AdminController : Controller
    {
        private DatabaseConnection db = new DatabaseConnection();

        // Verificar si el usuario es administrador
        private bool VerificarSesionAdmin()
        {
            return Session["UsuarioID"] != null && Session["Rol"]?.ToString() == "Administrador";
        }

        // GET: Dashboard Admin
        public ActionResult Index()
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            ViewBag.Nombre = Session["Nombre"];
            return View();
        }

        // GET: Ver todas las citas
        public ActionResult VerCitas()
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            DataTable citas = db.ExecuteStoredProcedure("sp_ObtenerTodasCitas");
            List<Cita> listaCitas = new List<Cita>();

            foreach (DataRow row in citas.Rows)
            {
                listaCitas.Add(new Cita
                {
                    CitaID = Convert.ToInt32(row["CitaID"]),
                    NombrePaciente = row["NombrePaciente"].ToString(),
                    Email = row["Email"].ToString(),
                    Telefono = row["Telefono"].ToString(),
                    NombreServicio = row["NombreServicio"].ToString(),
                    FechaCita = Convert.ToDateTime(row["FechaCita"]),
                    Estado = row["Estado"].ToString(),
                    FechaCreacion = Convert.ToDateTime(row["FechaCreacion"])
                });
            }

            return View(listaCitas);
        }

        // GET: Gestionar Servicios
        public ActionResult GestionarServicios()
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

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

        // POST: Agregar Servicio
        [HttpPost]
        public ActionResult AgregarServicio(string nombreServicio, string descripcion, int duracion)
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@NombreServicio", nombreServicio),
                    new SqlParameter("@Descripcion", descripcion),
                    new SqlParameter("@Duracion", duracion)
                };

                db.ExecuteStoredProcedure("sp_AgregarServicio", parameters);

                TempData["Mensaje"] = "Servicio agregado exitosamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar servicio: " + ex.Message;
            }

            return RedirectToAction("GestionarServicios");
        }

        // GET: Gestionar Horarios
        public ActionResult GestionarHorarios()
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            // Obtener servicios para el dropdown
            DataTable servicios = db.ExecuteStoredProcedure("sp_ObtenerServicios");
            List<ServicioMedico> listaServicios = new List<ServicioMedico>();

            foreach (DataRow row in servicios.Rows)
            {
                listaServicios.Add(new ServicioMedico
                {
                    ServicioID = Convert.ToInt32(row["ServicioID"]),
                    NombreServicio = row["NombreServicio"].ToString()
                });
            }

            ViewBag.Servicios = listaServicios;
            return View();
        }

        // POST: Agregar Horario
        [HttpPost]
        public ActionResult AgregarHorario(int servicioID, string fechaHora)
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@ServicioID", servicioID),
                    new SqlParameter("@FechaHora", DateTime.Parse(fechaHora))
                };

                db.ExecuteStoredProcedure("sp_AgregarHorario", parameters);

                TempData["Mensaje"] = "Horario agregado exitosamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar horario: " + ex.Message;
            }

            return RedirectToAction("GestionarHorarios");
        }

        // POST: Confirmar Cita
        [HttpPost]
        public ActionResult ConfirmarCita(int citaID)
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@CitaID", citaID)
                };

                DataTable resultado = db.ExecuteStoredProcedure("sp_ConfirmarCita", parameters);

                if (resultado.Rows.Count > 0 && Convert.ToInt32(resultado.Rows[0]["Resultado"]) > 0)
                {
                    TempData["Mensaje"] = "✅ Cita confirmada exitosamente";
                }
                else
                {
                    TempData["Error"] = "⚠️ No se pudo confirmar la cita. Verifica que esté en estado Pendiente.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Error al confirmar cita: " + ex.Message;
            }

            return RedirectToAction("VerCitas");
        }

        // POST: Cancelar Cita (Admin)
        [HttpPost]
        public ActionResult CancelarCitaAdmin(int citaID)
        {
            if (!VerificarSesionAdmin())
                return RedirectToAction("Login", "Auth");

            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@CitaID", citaID)
                };

                DataTable resultado = db.ExecuteStoredProcedure("sp_CancelarCitaAdmin", parameters);

                if (resultado.Rows.Count > 0 && Convert.ToInt32(resultado.Rows[0]["Resultado"]) > 0)
                {
                    TempData["Mensaje"] = "✅ Cita cancelada exitosamente";
                }
                else
                {
                    TempData["Error"] = "⚠️ No se pudo cancelar la cita.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "❌ Error al cancelar cita: " + ex.Message;
            }

            return RedirectToAction("VerCitas");
        
        }  
        }

    
}