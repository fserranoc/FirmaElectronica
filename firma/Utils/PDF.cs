using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Web.Mvc;
using firma.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OpenSSL.X509Certificate2Provider;
using Org.BouncyCastle.X509;
using SysX509 = System.Security.Cryptography.X509Certificates;


namespace firma.Utils
{
    public enum eTipoPagina
    {
        Especifica = 0,
        Ultima = 1
    }

    public class PDF : Controller
    {
        /// <summary>
        /// Firma un documento
        /// </summary>
        /// <param name="Source">Documento origen</param>
        /// <param name="Target">Documento destino</param>
        /// <param name="Certificate">Certificado a utilizar</param>
        /// <param name="Reason">Razón de la firma</param>
        /// <param name="Location">Ubicación</param>
        /// <param name="AddVisibleSign">Establece si hay que agregar la firma visible al documento</param>
        public void SignHashed(string Source, string Target, SysX509.X509Certificate2 Certificate, string Reason, string Location, bool AddVisibleSign, DatosPersonales datos)
        {
            X509CertificateParser objCP = new X509CertificateParser();
            Org.BouncyCastle.X509.X509Certificate[] objChain = new Org.BouncyCastle.X509.X509Certificate[] { objCP.ReadCertificate(Certificate.RawData) };
            
            PdfReader objReader = new PdfReader(Source);
            PdfStamper objStamper = PdfStamper.CreateSignature(objReader, new FileStream(Target, FileMode.Create), '\0', null, true);
            PdfSignatureAppearance objSA = objStamper.SignatureAppearance;

            if (AddVisibleSign)
                objSA.SetVisibleSignature(new Rectangle(100f, objReader.XrefSize, 500, 100), 1, null);

            objSA.SignDate = DateTime.Now;
            objSA.SetCrypto(null, objChain, null, null);
            objSA.Reason = Reason;
            objSA.Location = Location;
            objSA.Acro6Layers = true;
            objSA.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription;
            PdfSignature objSignature = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
            objSignature.Date = new PdfDate(objSA.SignDate);
            objSignature.Name = PdfPKCS7.GetSubjectFields(objChain[0]).GetField("CN");
            if (objSA.Reason != null)
                objSignature.Reason = objSA.Reason;
            if (objSA.Location != null)
                objSignature.Location = objSA.Location;
            objSA.CryptoDictionary = objSignature;
            int intCSize = 4000;
            

            //  Hashtable objTable = new Hashtable();
            //  objTable[PdfName.CONTENTS] = intCSize * 2 + 2;
            Dictionary<PdfName, int> objTable = new Dictionary<PdfName, int>();
            PdfName pdfname = new PdfName("firma");
            // Add some elements to the dictionary. There are no 
            // duplicate keys, but some of the values are duplicates.
            objTable.Add(pdfname, intCSize * 2 + 2);
            objSA.PreClose(objTable);

            HashAlgorithm objSHA1 = new SHA1CryptoServiceProvider();

            Stream objStream = objSA.RangeStream;
            int intRead = 0;
            byte[] bytBuffer = new byte[8192];
            while ((intRead = objStream.Read(bytBuffer, 0, 8192)) > 0)
                objSHA1.TransformBlock(bytBuffer, 0, intRead, bytBuffer, 0);
            objSHA1.TransformFinalBlock(bytBuffer, 0, 0);

            byte[] bytPK = SignMsg(objSHA1.Hash, Certificate, false);
            byte[] bytOut = new byte[intCSize];

            PdfDictionary objDict = new PdfDictionary();

            Array.Copy(bytPK, 0, bytOut, 0, bytPK.Length);

            objDict.Put(pdfname, new PdfString(bytOut).SetHexWriting(true));
            try
            {
                objSA.Close(objDict);

               
            }
            catch(Exception ex)
            {

            }
        }

        /// <summary>
        /// Crea la firma CMS/PKCS #7
        /// </summary>
        private static byte[] SignMsg(byte[] Message, SysX509.X509Certificate2 SignerCertificate, bool Detached)
        {
            //Creamos el contenedor
            ContentInfo contentInfo = new ContentInfo(Message);

            //Instanciamos el objeto SignedCms con el contenedor
            SignedCms objSignedCms = new SignedCms(contentInfo, Detached);

            //Creamos el "firmante"
            CmsSigner objCmsSigner = new CmsSigner(SignerCertificate);

            // Include the following line if the top certificate in the
            // smartcard is not in the trusted list.
            objCmsSigner.IncludeOption = SysX509.X509IncludeOption.EndCertOnly;
            //  Sign the CMS/PKCS #7 message. The second argument is
            //  needed to ask for the pin.
            objSignedCms.ComputeSignature(objCmsSigner, true);

            //Encodeamos el mensaje CMS/PKCS #7
            return objSignedCms.Encode();
        }

        public bool FirmarPDF(string pdfOriginal, string pdfFirmado, SysX509.X509Certificate2 certificado, string imagenFirma, bool firmaVisible, float puntoEsquinaInferiorIzquierdaX, float puntoEsquinaInferiorIzquierdaY, float puntoEsquinaSuperiorDerechaX, float puntoEsquinaSuperiorDerechaY, eTipoPagina paginaFirma, int pagina)
        {
            int numPagina = 0;
            try
            {
                X509CertificateParser objCP = new X509CertificateParser();
                Org.BouncyCastle.X509.X509Certificate[] objChain = new Org.BouncyCastle.X509.X509Certificate[] { objCP.ReadCertificate(certificado.RawData) };

                PdfReader objReader = new PdfReader(pdfOriginal);
                PdfStamper objStamper = PdfStamper.CreateSignature(objReader, new FileStream(pdfFirmado, FileMode.Create), '\0');
                PdfSignatureAppearance objSA = objStamper.SignatureAppearance;

                if (paginaFirma == eTipoPagina.Ultima)
                {
                    numPagina = objReader.NumberOfPages;
                }
                else
                {
                    if (pagina <= objReader.NumberOfPages)
                    {
                        numPagina = pagina;
                    }
                    else if (pagina > objReader.NumberOfPages)
                    {
                        numPagina = objReader.NumberOfPages;
                    }
                    else if (pagina < 1)
                    {
                        numPagina = 1;
                    }
                }
                if (firmaVisible)
                {
                    Rectangle rect = new Rectangle(puntoEsquinaInferiorIzquierdaX, puntoEsquinaInferiorIzquierdaY, puntoEsquinaSuperiorDerechaX, puntoEsquinaSuperiorDerechaY);
                    objSA.SetVisibleSignature(rect, numPagina, null);
                }


                objSA.CertificationLevel = PdfSignatureAppearance.CERTIFIED_NO_CHANGES_ALLOWED;

                objSA.SignDate = DateTime.Now;
                objSA.SetCrypto(null, objChain, null, null);
                objSA.Acro6Layers = true;
                objSA.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription;
                //objSA.SignatureGraphic = iTextSharp.text.Image.GetInstance(imagenFirma); //
                PdfSignature objSignature = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
                objSignature.Date = new PdfDate(objSA.SignDate);
                objSignature.Name = PdfPKCS7.GetSubjectFields(objChain[0]).GetField("CN");
                if (objSA.Reason != null)
                    objSignature.Reason = objSA.Reason;
                if (objSA.Location != null)
                    objSignature.Location = objSA.Location;
                if (objSA.Contact != null)
                    objSignature.Contact = objSA.Contact;
                objSA.CryptoDictionary = objSignature;
                int intCSize = 4000;
                Dictionary<PdfName, int> objTable = new Dictionary<PdfName, int>();
                objTable[PdfName.CONTENTS] = intCSize * 2 + 2;
                objSA.PreClose(objTable);

                HashAlgorithm objSHA1 = new SHA1CryptoServiceProvider();

                Stream objStream = objSA.RangeStream;
                int intRead = 0;
                byte[] bytBuffer = new byte[8192];
                while ((intRead = objStream.Read(bytBuffer, 0, 8192)) > 0)
                    objSHA1.TransformBlock(bytBuffer, 0, intRead, bytBuffer, 0);
                objSHA1.TransformFinalBlock(bytBuffer, 0, 0);

                byte[] bytPK = GenerarFirmar(objSHA1.Hash, certificado, false);
                byte[] bytOut = new byte[intCSize];

                PdfDictionary objDict = new PdfDictionary();

                Array.Copy(bytPK, 0, bytOut, 0, bytPK.Length);

                objDict.Put(PdfName.CONTENTS, new PdfString(bytOut).SetHexWriting(true));
                objSA.Close(objDict);

                return true;
            }
            catch
            {
                throw;
            }
        }

        private byte[] GenerarFirmar(byte[] Message, SysX509.X509Certificate2 SignerCertificate, bool Detached)
        {
            //Creamos el contenedor
            ContentInfo contentInfo = new ContentInfo(Message);

            //Instanciamos el objeto SignedCms con el contenedor
            SignedCms objSignedCms = new SignedCms(contentInfo, Detached);

            //Creamos el "firmante"
            CmsSigner objCmsSigner = new CmsSigner(SignerCertificate);


            // Include the following line if the top certificate in the
            // smartcard is not in the trusted list.
            objCmsSigner.IncludeOption = SysX509.X509IncludeOption.EndCertOnly;

            //  Sign the CMS/PKCS #7 message. The second argument is
            //  needed to ask for the pin.
            objSignedCms.ComputeSignature(objCmsSigner, false);

            //Encodeamos el mensaje CMS/PKCS #7
            return objSignedCms.Encode();
        }
    }
}