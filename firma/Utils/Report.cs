using CrystalDecisions.CrystalReports.Engine;
using firma.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace firma.Utils
{
    public class Report : Controller
    {
        public void GenerateInMemory(DatosPersonales datos, string fileName)
        {

            ReportDocument rd = new ReportDocument();
            var path1 = System.Web.HttpContext.Current.Server.MapPath("~/Utils");
            var path = Path.Combine(path1, "doc.rpt");
            rd.Load(path);
            var dt = new DataTable();
            dt.Columns.Add("Nombre");
            dt.Columns.Add("Apellido");
            DataRow dr = dt.NewRow();
            dr["Nombre"] = datos.Nombre;
            dr["Apellido"] = datos.Apellido;

            dt.Rows.Add(dr);

            rd.SetDataSource(dt);

            rd.PrintOptions.PaperOrientation = CrystalDecisions.Shared.PaperOrientation.Landscape;
            rd.PrintOptions.ApplyPageMargins(new CrystalDecisions.Shared.PageMargins(5, 5, 5, 5));
            rd.PrintOptions.PaperSize = CrystalDecisions.Shared.PaperSize.PaperA5;

            Stream stream = rd.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            stream.Seek(0, SeekOrigin.Begin);
            var file = File(stream, "application/pdf", fileName);
            HttpPostedFileBase objFile = (HttpPostedFileBase)new MemoryPostedFile(ReadFully(file.FileStream), fileName);
            datos.Document = objFile;
        }

        public FileStreamResult GeneratePsychical(DatosPersonales datos, string fileName, string pathToSave)
        {

            ReportDocument rd = new ReportDocument();
            var path1 = System.Web.HttpContext.Current.Server.MapPath("~/Utils");
            var path = Path.Combine(path1, "doc.rpt");
            rd.Load(path);
            var dt = new DataTable();
            dt.Columns.Add("Nombre");
            dt.Columns.Add("Apellido");
            DataRow dr = dt.NewRow();
            dr["Nombre"] = datos.Nombre;
            dr["Apellido"] = datos.Apellido;

            dt.Rows.Add(dr);

            //Response.Buffer = true;
            //Response.ClearContent();
            //Response.ClearHeaders();

            rd.SetDataSource(dt);
            rd.PrintOptions.PaperOrientation = CrystalDecisions.Shared.PaperOrientation.Landscape;
            rd.PrintOptions.ApplyPageMargins(new CrystalDecisions.Shared.PageMargins(5, 5, 5, 5));
            rd.PrintOptions.PaperSize = CrystalDecisions.Shared.PaperSize.PaperA5;

            Stream stream = rd.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            stream.Seek(0, SeekOrigin.Begin);


            Response.Clear();
            Response.ContentType = "application/pdf";
            Response.AddHeader("content-disposition", $"attachment; filename={pathToSave}");
            return new FileStreamResult(stream, "application/pdf");
            
        }


        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }

    public class MemoryPostedFile : HttpPostedFileBase
    {
        private readonly byte[] fileBytes;

        public MemoryPostedFile(byte[] fileBytes, string fileName = null)
        {
            this.fileBytes = fileBytes;
            this.FileName = fileName;
            this.InputStream = new MemoryStream(fileBytes);
        }

        public override int ContentLength => fileBytes.Length;

        public override string FileName { get; }

        public override Stream InputStream { get; }
    }
}