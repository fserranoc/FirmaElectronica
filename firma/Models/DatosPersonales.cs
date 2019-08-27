using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace firma.Models
{
    public class DatosPersonales
    {
        public HttpPostedFileBase Document { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
    }
}