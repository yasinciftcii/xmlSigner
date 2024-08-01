using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;

class Program
{
    static void Main(string[] args)
    {
        // SoftHSM2 kütüphanesi yolu
        string libraryPath = @"C:\SoftHSM2\lib\softhsm2-x64.dll";

        // PKCS#11 kütüphanesi yükle
        Pkcs11InteropFactories factories = new Pkcs11InteropFactories();
        using (IPkcs11Library pkcs11 = factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, libraryPath, AppType.MultiThreaded))
        {
            // Slotları al
            List<ISlot> slots = pkcs11.GetSlotList(SlotsType.WithTokenPresent);
            if (slots == null || slots.Count == 0)
            {
                Console.WriteLine("No slots found");
                return;
            }

            // İlk slotu seç
            ISlot slot = slots[0];

            // Oturum aç
            using (ISession session = slot.OpenSession(SessionType.ReadWrite))
            {
                // PIN ile oturum aç
                session.Login(CKU.CKU_USER, "123456");

                // İmza anahtarını al
                List<IObjectHandle> foundObjects = session.FindAllObjects(new List<IObjectAttribute>
                {
                    factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY)
                });

                if (foundObjects == null || foundObjects.Count == 0)
                {
                    Console.WriteLine("No private key found");
                    return;
                }

                IObjectHandle privateKeyHandle = foundObjects[0];

                // XML belgesini yükle
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load("test.xml");

                try
                {
                    // XML belgesini imzala
                    SignXmlDocument(session, privateKeyHandle, xmlDoc);

                    // İmzalı belgeyi kaydet
                    xmlDoc.Save("../signed_test.xml");
                    Console.WriteLine("XML signing succeeded.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred during XML signing: {ex.Message}");
                }

                // Oturumu kapat
                session.Logout();
            }
        }
    }

    static void SignXmlDocument(ISession session, IObjectHandle privateKeyHandle, XmlDocument xmlDoc)
    {
        // XML belgesinin kök öğesini seç
        XmlElement rootElement = xmlDoc.DocumentElement;

        if (rootElement == null)
        {
            throw new InvalidOperationException("Root element not found in XML.");
        }

        // İmzalanacak referans oluştur
        Reference reference = new Reference();
        reference.Uri = "";

        // Referansın hash algoritmasını ayarla
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());

        // İmzalama algoritmasını ayarla
        SignedXml signedXml = new SignedXml(xmlDoc);
        signedXml.SigningKey = new Pkcs11RsaCryptoServiceProvider(session, privateKeyHandle);
        signedXml.AddReference(reference);

        // İmza metodunu ve digest metodunu ayarla
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;

        // İmza elementini oluştur ve kök öğeye ekle
        signedXml.ComputeSignature();
        XmlElement xmlDigitalSignature = signedXml.GetXml();
        rootElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
    }
}

// Pkcs11RsaCryptoServiceProvider sınıfı
public class Pkcs11RsaCryptoServiceProvider : RSA
{
    private ISession _session;
    private IObjectHandle _privateKeyHandle;

    public Pkcs11RsaCryptoServiceProvider(ISession session, IObjectHandle privateKeyHandle)
    {
        _session = session;
        _privateKeyHandle = privateKeyHandle;
    }

    public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
    {
        // İmzalama işlemi için mekanizma ayarla
        var mechanism = _session.Factories.MechanismFactory.Create(CKM.CKM_RSA_PKCS);
        return _session.Sign(mechanism, _privateKeyHandle, hash);
    }

    // Diğer RSA üyeleri burada override edilebilir
    public override RSAParameters ExportParameters(bool includePrivateParameters)
    {
        throw new NotImplementedException();
    }

    public override void ImportParameters(RSAParameters parameters)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
