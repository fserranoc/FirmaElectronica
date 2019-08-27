using CrystalDecisions.CrystalReports.Engine;
using firma.Models;
using firma.Utils;
using Pluralsight.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Mvc;
using smp.Utilidades;
using static smp.Utilidades.FirmaPdf;

namespace firma.Controllers
{
    public class FirmaController : Controller
    {
        // GET: Firma
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase fileInput)
        {
            
           

            return View();
        }

        // GET: Firma/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Firma/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Firma/Create
        [HttpPost]
        public ActionResult Create(DatosPersonales collection)
        {
            var rep = new Report();
            var rptInMemory = true;
            var fileName = "document.pdf";
            string pathDest = "";
            string pathOrigen = "";
            pathDest = Server.MapPath($"~/App_Data/{fileName}");
            pathOrigen = $@"C:\Users\FLP\Downloads\{fileName}";
            
            try
            {
               // if (rptInMemory)
                    rep.GenerateInMemory(collection, fileName);
               // else
                    GeneratePsychical(collection, fileName, pathOrigen);           

                if (!rptInMemory || (collection.Document != null && collection.Document.ContentLength > 0))
                {
                    /*
                    X509Certificate2 cert;
                    using (CryptContext ctx = new CryptContext())
                    {
                        ctx.Open();
                        cert = ctx.CreateSelfSignedCertificate(new SelfSignedCertProperties
                        {
                            IsPrivateKeyExportable = true,
                            KeyBitLength = 4096,
                            Name = new X500DistinguishedName($"cn={collection.Nombre} {collection.Apellido}"),
                            ValidFrom = DateTime.Today.AddDays(-1),
                            ValidTo = DateTime.Today.AddYears(1),
                        });

                       
                        X509Certificate2UI.DisplayCertificate(cert);
                    }
                    */
                    var certName = $"{collection.Nombre}-{collection.Apellido}";
                    var cer = new Certificate(certName);
                    var cert = cer.Create();
                   // X509Certificate2UI.DisplayCertificate(cert);
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string certificateFolder = "App_Data\\certificates\\";
                    string certPath = Path.Combine(baseDir, certificateFolder, certName + ".cer");

                    //X509Certificate2 cert = new X509Certificate2();
                    //cert.Import(certPath);
                  

                    var pdf = new PDF();
                    // pdf.SignHashed(pathOrigen, pathDest, cert, "razón", "location", true, collection);
                    pdf.FirmarPDF(pathOrigen, pathDest, cert,"", true, 20, 30, 300, 90, Utils.eTipoPagina.Ultima, 1);

                }
                else
                {
                    ViewBag.Message = "You have not specified a file.";
                }
            }
            catch(Exception ex)
            {
            }

            //if(rptInMemory)
               // return File(collection.Document.InputStream, "application/pdf", fileName);
            //else
           // {
              //  var streamResult = new FileStreamResult(new MemoryStream(ReadAllBytes(pathOrigen)), "application/pdf");
              //  streamResult.FileDownloadName = fileName;

                return View();
         //   }
               
        }



        // GET: Firma/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Firma/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Firma/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Firma/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public byte[] ReadAllBytes(string fileName)
        {
            byte[] buffer = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                buffer = new byte[fs.Length];
                fs.Read(buffer, 0, (int)fs.Length);
            }
            return buffer;
        }

        

        private void GeneratePsychical(DatosPersonales datos, string fileName, string pathToSave)
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

            var content = ReadFully(stream);


            using (FileStream fileStream = new FileStream(pathToSave, FileMode.Create))
            {

                for (int i = 0; i < content.Length; i++)
                {
                    fileStream.WriteByte(content[i]);
                }

                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Close();
            }

            //Response.Clear();
            //Response.ContentType = "application/pdf";
            //Response.AddHeader("content-disposition", $"attachment; filename={pathToSave}");
            //File(stream, "application/pdf");

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
}
