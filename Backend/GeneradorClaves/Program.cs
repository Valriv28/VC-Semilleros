
using System.Security.Cryptography;

using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

File.WriteAllText(
    "credencial-privada.pem",
    ecdsa.ExportPkcs8PrivateKeyPem());

File.WriteAllText(
    "credencial-publica.pem",
    ecdsa.ExportSubjectPublicKeyInfoPem());

Console.WriteLine("Claves generadas correctamente.");
