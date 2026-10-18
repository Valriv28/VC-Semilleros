using System.Security.Cryptography;
using System.Text;

namespace Front.Anclaje;

/// <summary>SHA-256 en minúsculas y 64 caracteres: el formato que espera el adaptador.</summary>
public static class EvidenciaHasher
{
    /// <summary>Para archivos (en streaming: no se carga el archivo completo en memoria).</summary>
    public static async Task<string> Sha256HexAsync(Stream stream, CancellationToken ct = default) =>
        Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)).ToLowerInvariant();

    /// <summary>
    /// Para una credencial u otro texto. Pasa SIEMPRE la misma representación canónica
    /// (mismo orden de campos, mismo formato de fechas) al emitir y al verificar, o el hash no coincidirá.
    /// </summary>
    public static string Sha256Hex(string textoCanonico) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(textoCanonico))).ToLowerInvariant();
}
