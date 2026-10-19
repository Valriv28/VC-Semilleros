using System.Text.Json.Nodes;                    // JsonObject: un objeto JSON que se puede armar y recorrer en memoria (viene con .NET).
using VCSemilleros.Domain.Modelos;               // Evaluacion.

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "credencial verificable" (RF10 y RF14; HU-C01-01 a HU-C01-05).
// Sabe CONSTRUIR la credencial Open Badges 3.0, VALIDAR su estructura, FIRMARLA con ECDSA P-256,
// SERIALIZARLA y VERIFICAR su prueba. No toca la BD para guardar nada ni habla con la red.
//
// LIMITACIÓN HONESTA: el JSON se serializa de forma determinista (mismo orden de campos, sin espacios),
// pero NO se canonicaliza como JSON-LD completo (RDFC). Para este prototipo basta; una implementación
// completa de Data Integrity de W3C exigiría una librería de canonicalización.
public interface IServicioCredencialVerificable
{
    // Reúne los datos autorizados de la credencial a partir de la BD: el titular, el coordinador que
    // declaró la elegibilidad, el semillero, el periodo y el criterio aplicado.
    // 400 si la evaluación no tiene elegibilidad declarada · 404 si falta alguno de los datos relacionados.
    // Lo usan la emisión (E20) y la reconstrucción de la credencial firmada (E23).
    Task<DatosCredencial> CargarDatosAsync(Evaluacion evaluacion, string codigo, DateTime fechaEmision);

    // HU-C01-01: arma el contenido de la credencial (SIN prueba) como un objeto JSON.
    JsonObject Construir(DatosCredencial datos);

    // HU-C01-02: devuelve la lista de campos obligatorios que faltan (vacía = estructura completa).
    // La emisión lanza 400 si la lista no está vacía y NO avanza a firmar; la verificación la usa
    // para el control ESTRUCTURA.
    List<string> ValidarEstructura(JsonObject credencial);

    // HU-C01-03: firma el contenido con la clave privada (ECDSA P-256 + SHA-256) y devuelve la
    // credencial con su "proof" (prueba) agregada.
    JsonObject Firmar(JsonObject contenido);

    // HU-C01-05: convierte la credencial firmada en el texto compacto que se entrega (el campo "vc").
    string Serializar(JsonObject credencialFirmada);

    // SHA-256 (en hexadecimal) del contenido SIN la prueba. Es el valor de la columna vc_hash.
    // Se calcula sin la prueba porque ECDSA no es determinista: firmar dos veces da firmas distintas,
    // pero el contenido, y por tanto su hash, es siempre el mismo.
    string CalcularHash(JsonObject credencial);

    // Convierte el texto "vc" recibido en un objeto JSON. Devuelve null si no es un JSON válido de tipo objeto.
    JsonObject? Leer(string vc);

    // Comprueba la prueba criptográfica: la firma debe corresponder al contenido y a la clave del emisor.
    // Devuelve false si el contenido fue alterado, si falta la prueba o si está mal formada.
    bool VerificarPrueba(JsonObject credencial);

    // Identificador del emisor dentro de la credencial para un usuario (el coordinador).
    // Existe para que emisión y verificación usen EXACTAMENTE el mismo formato.
    string IdEmisor(int usuarioId);

    // Lee el código de la credencial desde su "id". Devuelve null si no se puede.
    string? ObtenerCodigo(JsonObject credencial);

    // Lee el identificador del emisor desde "issuer.id". Devuelve null si no se puede.
    string? ObtenerIdEmisor(JsonObject credencial);
}

// Constantes de la credencial. "static class" = clase que no se instancia: solo agrupa valores fijos.
public static class ConstantesCredencial
{
    // Valor de la columna "tipo" de la tabla credencial (propuesta P6).
    public const string Tipo = "Asistencia a semillero";
}

// "record": tipo de C# para transportar datos, con propiedades de solo lectura.
// DatosCredencial son los datos autorizados que entran en la credencial:
//   Codigo:                 el código (clave de la tabla credencial).
//   FechaEmision:           la fecha decidida ANTES de firmar (sin milisegundos), porque forma parte del contenido firmado.
//   EmisorId/EmisorNombre:  el coordinador que declaró la elegibilidad.
//   TitularId/Nombre/Correo: el estudiante dueño de la credencial. La credencial SÍ lleva sus datos
//                           (es el documento que se le entrega); a Stellar solo va un hash.
//   SemilleroNombre/PeriodoNombre, PorcentajeAsistencia, MinimoAsistencia, CriterioVersion: el logro y el criterio.
public record DatosCredencial(
    string Codigo,
    DateTime FechaEmision,
    int EmisorId,
    string EmisorNombre,
    int TitularId,
    string TitularNombre,
    string TitularCorreo,
    string SemilleroNombre,
    string PeriodoNombre,
    decimal PorcentajeAsistencia,
    decimal MinimoAsistencia,
    int CriterioVersion);