using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using ProyectoFinal.Data;
using ProyectoFinal.Models;

namespace ProyectoFinal.Controllers
{
    public class AuthController : Controller
    {
        private DatabaseConnection db = new DatabaseConnection();

        // GET: Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Login(string email, string contrasena)
        {
            SqlParameter[] parameters = {
                new SqlParameter("@Email", email),
                new SqlParameter("@Contrasena", contrasena)
            };

            DataTable dt = db.ExecuteStoredProcedure("sp_LoginUsuario", parameters);

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                // Guardar datos en sesión
                Session["UsuarioID"] = row["UsuarioID"];
                Session["Nombre"] = row["Nombre"];
                Session["Email"] = row["Email"];
                Session["Rol"] = row["Rol"];

                // Redirigir según el rol
                if (row["Rol"].ToString() == "Administrador")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Usuario");
                }
            }

            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }

        // GET: Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}