using CERTENROLLLib;
using Pluralsight.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Web;

namespace firma.Utils
{
    public class Certificate
    {
        public string CertificateName { get; set; }

        public Certificate(string certName)
        {
            CertificateName = certName;
        }

        public X509Certificate2 Create()
        {
            X509Certificate2 cert = new X509Certificate2();
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string certificateFolder = "App_Data\\certificates\\";
                string certPath = Path.Combine(baseDir, certificateFolder);

                string certificateFile = $"{CertificateName}.cer";
                string certificatePassword = "";
                string certificateLocation = certPath + certificateFile;
                string path = "";

                cert = CreateSelfSignedCertificate(CertificateName);
                path = CreateCertFile(cert, certificateLocation, X509ContentType.Cert);
                //using (CryptContext ctx = new CryptContext())
                //{
                //    ctx.Open();

                //    cert = ctx.CreateSelfSignedCertificate(new SelfSignedCertProperties
                //    {
                //        IsPrivateKeyExportable = true,
                //        KeyBitLength = 4096,
                //        Name = new X500DistinguishedName($"cn={CertificateName}"),
                //        ValidFrom = DateTime.Today.AddDays(-1),
                //        ValidTo = DateTime.Today.AddYears(1),
                //        PrivateKey = ""
                //    });

                //    RSACryptoServiceProvider rsaCsp = (RSACryptoServiceProvider)cert.PrivateKey;
                //    rsaCsp.ToXmlString(false);
                //    // cert.Export(X509ContentType.Cert);

                //    path = CreateCertFile(cert, certificateLocation, X509ContentType.Cert);
                //}
                InstallCertificate(cert, path, certificatePassword);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return cert;
        }


        private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            // create DN for subject and issuer
            var dn = new CX500DistinguishedName();
            dn.Encode("CN=" + subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);

            // create a new private key for the certificate
            CX509PrivateKey privateKey = new CX509PrivateKey();
            privateKey.ProviderName = "Microsoft Base Cryptographic Provider v1.0";
            privateKey.MachineContext = true;
            privateKey.Length = 2048;
            privateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE; // use is not limited
            privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG;
            privateKey.Create();

            // Use the stronger SHA512 hashing algorithm
            var hashobj = new CObjectId();
            hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID,
                ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY,
                AlgorithmFlags.AlgorithmFlagsNone, "SHA512");

            // add extended key usage if you want - look at MSDN for a list of possible OIDs
            var oid = new CObjectId();
            oid.InitializeFromValue("1.3.6.1.5.5.7.3.1"); // SSL server
            var oidlist = new CObjectIds();
            oidlist.Add(oid);
            var eku = new CX509ExtensionEnhancedKeyUsage();
            eku.InitializeEncode(oidlist);

            // Create the self signing request
            var cert = new CX509CertificateRequestCertificate();
            cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, privateKey, "");
            cert.Subject = dn;
            cert.Issuer = dn; // the issuer and the subject are the same
            cert.NotBefore = DateTime.Now;
            // this cert expires immediately. Change to whatever makes sense for you
            cert.NotAfter = DateTime.Now.AddYears(2);
            cert.X509Extensions.Add((CX509Extension)eku); // add the EKU
            cert.HashAlgorithm = hashobj; // Specify the hashing algorithm
            cert.Encode(); // encode the certificate

            // Do the final enrollment process
            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(cert); // load the certificate
            enroll.CertificateFriendlyName = subjectName; // Optional: add a friendly name
            string csr = enroll.CreateRequest(); // Output the request in base64
                                                 // and install it back as the response
            enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate,
                csr, EncodingType.XCN_CRYPT_STRING_BASE64, ""); // no password
                                                                // output a base64 encoded PKCS#12 so we can import it back to the .Net security classes
            var base64encoded = enroll.CreatePFX("", // no password, this is for internal consumption
                PFXExportOptions.PFXExportChainWithRoot);

            // instantiate the target class with the PKCS#12 data (and the empty password)
            return new System.Security.Cryptography.X509Certificates.X509Certificate2(
                System.Convert.FromBase64String(base64encoded), "",
                // mark the private key as exportable (this is usually what you want to do)
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable
            );
        }

        private static void InstallCertificate(X509Certificate2 certObj, string certificatePath, string certificatePassword)
        {
            try
            {
                var serviceRuntimeUserCertificateStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                
                StorePermission sp = new StorePermission(PermissionState.Unrestricted);
                sp.Flags = StorePermissionFlags.AllFlags;
                sp.Assert();
                serviceRuntimeUserCertificateStore.Open(OpenFlags.ReadWrite);

                X509Certificate2 cert = null;

                try
                {
                    cert = certObj;// new X509Certificate2(certificatePath, certificatePassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load certificate " + certificatePath);
                    //throw new DataException("Certificate appeared to load successfully but also seems to be null.", ex);
                }

                try
                {
         

                    serviceRuntimeUserCertificateStore.Add(cert);
                    var certs = serviceRuntimeUserCertificateStore.Certificates;
                    var locations = serviceRuntimeUserCertificateStore.Location;
                    var handle = serviceRuntimeUserCertificateStore.StoreHandle;
                    serviceRuntimeUserCertificateStore.Close();

                }
                catch(CryptographicException ex)
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to install {0}.  Check the certificate index entry and verify the certificate file exists.", certificatePath);
            }
        }

        public static string CreateCertFile(X509Certificate2 cert, string NewCertFile, X509ContentType certFileType)
        {

           
            byte[] CerContents;
            var certPassword = "";
            // Write the content to a local .cer file so that users can upload it into Azure
            if (certFileType == X509ContentType.Cert)
            {
                CerContents = cert.Export(X509ContentType.Cert);
            }
            else
            {
                CerContents = cert.Export(X509ContentType.Pfx, certPassword);
            }

            using (FileStream fileStream = new FileStream(NewCertFile, FileMode.Create))
            {

                for (int i = 0; i < CerContents.Length; i++)
                {
                    fileStream.WriteByte(CerContents[i]);
                }

                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Close();
            }

            return System.IO.Path.GetFullPath(NewCertFile);
        }

        
    }
}