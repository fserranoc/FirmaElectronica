using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using iTextSharp.text;
using iTextSharp.text.pdf.crypto;
using SysX509 = System.Security.Cryptography.X509Certificates;
using System.Web.Mvc;
using Org.BouncyCastle.X509;
using iTextSharp.text.pdf;

namespace firma.Utils
{
    public class PDF2 : Controller
    {
        /// <summary>
        /// Firma un documento PDF
        /// </summary>
        /// <param name="Source">Path del PDF a firmar</param>
        /// <param name="Target">Path del PDF firmado</param>
        /// <param name="Certificate">Certificado para realizar la firma</param>
        /// <param name="Reason">Motivo</param>
        /// <param name="Location">Ubicación</param>
        /// <param name="AddVisibleSign">Indica si la firma es visible dentro del documento</param>
        /// <param name="AddTimeStamp">Indica si se va a añadir sello de tiempo en el documento</param>
        /// <param name="strTSA">TSA del sello de tiempo</param>

        public static void SignHashed(string Source, string Target, SysX509.X509Certificate2 Certificate, string Reason, string Location, bool AddVisibleSign, bool AddTimeStamp, string strTSA)
        {
            X509CertificateParser objCP = new X509CertificateParser();
            X509Certificate[] objChain = new X509Certificate[] { objCP.ReadCertificate(Certificate.RawData) };

            //IList<ICrlClient> crlList = new List<ICrlClient>();
            //crlList.Add(new CrlClientOnline(objChain));

            PdfReader objReader = new PdfReader(Source);
            PdfStamper objStamper = PdfStamper.CreateSignature(objReader, new FileStream(Target, FileMode.Create), '\0', null, true);

            // Creamos la apariencia
            PdfSignatureAppearance signatureAppearance = objStamper.SignatureAppearance;
            signatureAppearance.Reason = Reason;
            signatureAppearance.Location = Location;

            // Si está la firma visible:
            if (AddVisibleSign)
                signatureAppearance.SetVisibleSignature(new Rectangle(100, 100, 300, 200), 1, null); //signatureAppearance.SetVisibleSignature(new Rectangle(100, 100, 250, 150), objReader.NumberOfPages, "Signature");

            ITSAClient tsaClient = null;
            IOcspClient ocspClient = null;

            // Si se ha añadido el sello de tiempo
            if (AddTimeStamp)
            {
              //  ocspClient = new OcspClientBouncyCastle();
                tsaClient = new TSAClientBouncyCastle(strTSA);
            }

            // Creating the signature
          //  IExternalSignature externalSignature = new X509Certificate2Signature(Certificate, "SHA-1");
          //  MakeSignature.SignDetached(signatureAppearance, externalSignature, objChain, crlList, ocspClient, tsaClient, 0, CryptoStandard.CMS);

            if (objReader != null)
                objReader.Close();
            if (objStamper != null)
                objStamper.Close();
        }
    }
}