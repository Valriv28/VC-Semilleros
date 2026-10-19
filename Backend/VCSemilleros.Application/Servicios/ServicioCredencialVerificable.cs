using System.Globalization;                      // CultureInfo: formatear números y fechas igual en cualquier computador.
using System.Security.Cryptography;              // ECDsa (firma ECDSA P-256) y SHA256: vienen con .NET, sin paquetes.
using System.Text;                               // Encoding.UTF8: convierte texto a bytes.
using System.Text.Encodings.Web;                 // JavaScriptEncoder: decide cómo se escriben los caracteres especiales.
using System.Text.Json;                          // JsonSerializerOptions y JsonException.
using System.Text.Json.Nodes;                    // JsonObject, JsonArray, JsonNode: JSON en memoria.
using VCSemilleros.Domain.Excepciones;           // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;               // Evaluacion.
using VCSemilleros.Domain.Repositorios;          // Los puertos que se necesitan para reunir los datos.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: credencial verificable Open Badges 3.0 (HU-C01-01 a HU-C01-05).
// Construye, valida, firma (ECDSA P-256), serializa y verifica la prueba.
// No conoce HTTP, Dapper, Npgsql ni Stellar (Art. 4). La clave privada se la pasa Program.cs
// (que la lee de la variable de entorno Credenciales__ClavePrivada): este servicio nunca lee el entorno.
public class ServicioCredencialVerificable : IServicioCredencialVerificable
{
    // Prefijos de los identificadores (URN) que van dentro de la credencial.
    private const string PrefijoCredencial = "urn:vc-semilleros:credencial:";
    private const string PrefijoUsuario = "urn:vc-semilleros:usuario:";
    private const string PrefijoLogro = "urn:vc-semilleros:logro:";

    // Identifica la clave con la que se firmó (campo verificationMethod de la prueba).
    private const string MetodoVerificacion = "urn:vc-semilleros:emisor#clave-1";

    // Opciones de serialización, IGUALES siempre: sin sangrías ni espacios, y sin escapar los acentos.
    // Es lo que hace el texto "determinista": el mismo contenido da siempre exactamente los mismos bytes.
    // "static readonly" = un único objeto compartido que no cambia.
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IRepositorioParticipante _repositorioParticipante;
    private readonly IRepositorioPeriodo _repositorioPeriodo;
    private readonly IRepositorioSemillero _repositorioSemillero;
    private readonly IRepositorioUsuario _repositorioUsuario;
    private readonly string _clavePrivadaPem;

    // Constructor: recibe los puertos y la clave privada en formato PEM (el texto que empieza con
    // "-----BEGIN PRIVATE KEY-----").
    public ServicioCredencialVerificable(
        IRepositorioParticipante repositorioParticipante,
        IRepositorioPeriodo repositorioPeriodo,
        IRepositorioSemillero repositorioSemillero,
        IRepositorioUsuario repositorioUsuario,
        string clavePrivadaPem)
    {
        // Falla pronto y con un mensaje claro si la variable de entorno no está configurada.
        if (string.IsNullOrWhiteSpace(clavePrivadaPem))
            throw new InvalidOperationException(
                "Falta la clave de firma: configure la variable de entorno Credenciales__ClavePrivada.");

        _repositorioParticipante = repositorioParticipante;
        _repositorioPeriodo = repositorioPeriodo;
        _repositorioSemillero = repositorioSemillero;
        _repositorioUsuario = repositorioUsuario;
        _clavePrivadaPem = clavePrivadaPem;
    }

    // Reúne los datos autorizados de la credencial desde la BD.
    public async Task<DatosCredencial> CargarDatosAsync(Evaluacion evaluacion, string codigo, DateTime fechaEmision)
    {
        // Sin elegibilidad declarada no hay emisor ni credencial posible (400).
        if (evaluacion.ElegibleDeclaradoPor is null)
            throw new ArgumentException("La evaluación no tiene la elegibilidad declarada.");

        // "?? throw ..." significa: si el resultado es null, lanza esta excepción.
        var participante = await _repositorioParticipante.ObtenerPorIdAsync(evaluacion.ParticipanteId)
            ?? throw new NoEncontradoExcepcion($"No existe un participante con id = {evaluacion.ParticipanteId}");

        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(participante.PeriodoId)
            ?? throw new NoEncontradoExcepcion($"No existe un periodo con id = {participante.PeriodoId}");

        var semillero = await _repositorioSemillero.ObtenerPorIdAsync(periodo.SemilleroId)
            ?? throw new NoEncontradoExcepcion($"No existe un semillero con id = {periodo.SemilleroId}");

        var titular = await _repositorioUsuario.ObtenerPorIdAsync(participante.EstudianteId)
            ?? throw new NoEncontradoExcepcion($"No existe un usuario con id = {participante.EstudianteId}");

        // ".Value" saca el entero del "int?" (ya sabemos que no es null por la comprobación de arriba).
        var emisor = await _repositorioUsuario.ObtenerPorIdAsync(evaluacion.ElegibleDeclaradoPor.Value)
            ?? throw new NoEncontradoExcepcion($"No existe un usuario con id = {evaluacion.ElegibleDeclaradoPor.Value}");

        return new DatosCredencial(
            codigo, fechaEmision,
            emisor.Id, emisor.Nombre,
            titular.Id, titular.Nombre, titular.Correo,
            semillero.Nombre, periodo.Nombre,
            evaluacion.PctAsistencia, evaluacion.MinAsistenciaPct, evaluacion.CriterioVersion);
    }

    // HU-C01-01: arma el contenido de la credencial (SIN prueba).
    public JsonObject Construir(DatosCredencial datos)
    {
        // Los porcentajes van como TEXTO con 2 decimales y punto (cultura invariante): así el contenido
        // es idéntico en cualquier computador y no depende de cómo se guardó el número.
        var descripcion = string.Create(CultureInfo.InvariantCulture,
            $"Asistencia del {datos.PorcentajeAsistencia:F2}% en el semillero {datos.SemilleroNombre} durante el periodo {datos.PeriodoNombre}.");
        var criterio = string.Create(CultureInfo.InvariantCulture,
            $"Asistencia mínima exigida: {datos.MinimoAsistencia:F2}% (criterio versión {datos.CriterioVersion}). La elegibilidad fue declarada por un coordinador.");

        // Un JsonObject es un diccionario ordenado: el ORDEN en que se agregan los campos es el orden
        // en que se escriben, y es parte de lo que se firma.
        return new JsonObject
        {
            ["@context"] = new JsonArray(
                "https://www.w3.org/ns/credentials/v2",
                "https://purl.imsglobal.org/spec/ob/v3p0/context-3.0.3.json"),
            ["id"] = PrefijoCredencial + datos.Codigo,
            ["type"] = new JsonArray("VerifiableCredential", "OpenBadgeCredential"),
            ["issuer"] = new JsonObject
            {
                ["id"] = IdEmisor(datos.EmisorId),
                ["type"] = new JsonArray("Profile"),
                ["name"] = datos.EmisorNombre
            },
            // Fecha local sin zona horaria, la misma hora que guarda la BD en fecha_emision.
            // (Limitación: el estándar pide zona horaria; para el prototipo se omite a propósito.)
            ["validFrom"] = datos.FechaEmision.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            ["name"] = ConstantesCredencial.Tipo,
            ["credentialSubject"] = new JsonObject
            {
                ["id"] = PrefijoUsuario + datos.TitularId,
                ["type"] = new JsonArray("AchievementSubject"),
                ["name"] = datos.TitularNombre,
                ["identifier"] = new JsonArray(new JsonObject
                {
                    ["type"] = "IdentityObject",
                    ["hashed"] = false,
                    ["identityType"] = "emailAddress",
                    ["identityHash"] = datos.TitularCorreo
                }),
                ["achievement"] = new JsonObject
                {
                    ["id"] = PrefijoLogro + datos.Codigo,
                    ["type"] = new JsonArray("Achievement"),
                    ["name"] = $"Asistencia al semillero {datos.SemilleroNombre} ({datos.PeriodoNombre})",
                    ["description"] = descripcion,
                    ["criteria"] = new JsonObject { ["narrative"] = criterio }
                }
            }
        };
    }

    // HU-C01-02: devuelve los campos obligatorios que faltan (lista vacía = estructura completa).
    public List<string> ValidarEstructura(JsonObject credencial)
    {
        var faltantes = new List<string>();

        // Función local: comprueba que la ruta tenga un texto no vacío y, si no, la anota como faltante.
        void Exigir(string ruta)
        {
            if (string.IsNullOrWhiteSpace(Texto(credencial, ruta)))
                faltantes.Add(ruta);
        }

        Exigir("@context.0");
        Exigir("@context.1");
        Exigir("id");
        Exigir("issuer.id");
        Exigir("issuer.name");
        Exigir("validFrom");
        Exigir("name");
        Exigir("credentialSubject.id");
        Exigir("credentialSubject.name");
        Exigir("credentialSubject.identifier.0.identityHash");
        Exigir("credentialSubject.achievement.id");
        Exigir("credentialSubject.achievement.name");
        Exigir("credentialSubject.achievement.description");
        Exigir("credentialSubject.achievement.criteria.narrative");

        // El tipo debe declarar los dos perfiles: credencial verificable y Open Badge.
        if (Texto(credencial, "type.0") != "VerifiableCredential" || Texto(credencial, "type.1") != "OpenBadgeCredential")
            faltantes.Add("type");

        return faltantes;
    }

    // HU-C01-03: firma el contenido con ECDSA P-256 + SHA-256 y agrega la prueba.
    public JsonObject Firmar(JsonObject contenido)
    {
        // DeepClone copia el objeto completo: no se modifica el original que recibió el método.
        var copia = (JsonObject)contenido.DeepClone();

        var bytes = BytesCanonicos(copia);

        // "using var" libera la clave criptográfica al terminar el método.
        using var ecdsa = CrearClave();
        var firma = ecdsa.SignData(bytes, HashAlgorithmName.SHA256);

        // La prueba se agrega AL FINAL; no forma parte de lo que se firma.
        copia["proof"] = new JsonObject
        {
            ["type"] = "EcdsaSecp256r1Signature2019",
            ["created"] = Texto(copia, "validFrom"),
            ["verificationMethod"] = MetodoVerificacion,
            ["proofPurpose"] = "assertionMethod",
            ["proofValue"] = Convert.ToBase64String(firma)
        };

        return copia;
    }

    // HU-C01-05: el texto compacto que se entrega como "vc".
    public string Serializar(JsonObject credencialFirmada)
    {
        return credencialFirmada.ToJsonString(OpcionesJson);
    }

    // SHA-256 (hexadecimal en minúsculas) del contenido sin la prueba: el valor de vc_hash.
    public string CalcularHash(JsonObject credencial)
    {
        // SHA256.HashData calcula la huella de los bytes; ToHexString la escribe en hexadecimal.
        return Convert.ToHexString(SHA256.HashData(BytesCanonicos(credencial))).ToLowerInvariant();
    }

    // Convierte el texto recibido en un objeto JSON, o null si no lo es.
    public JsonObject? Leer(string vc)
    {
        if (string.IsNullOrWhiteSpace(vc))
            return null;

        try
        {
            // "as JsonObject" devuelve null si el JSON es válido pero no es un objeto (por ejemplo, un arreglo).
            return JsonNode.Parse(vc) as JsonObject;
        }
        catch (JsonException)
        {
            // El texto no es JSON válido.
            return null;
        }
    }

    // Comprueba que la firma corresponda al contenido y a la clave del emisor.
    public bool VerificarPrueba(JsonObject credencial)
    {
        var proofValue = Texto(credencial, "proof.proofValue");
        if (string.IsNullOrWhiteSpace(proofValue))
            return false;

        try
        {
            var firma = Convert.FromBase64String(proofValue);

            using var ecdsa = CrearClave();

            // VerifyData recalcula el hash del contenido y comprueba que la firma lo respalde.
            // Si alguien cambió una sola letra del contenido, devuelve false.
            return ecdsa.VerifyData(BytesCanonicos(credencial), firma, HashAlgorithmName.SHA256);
        }
        catch (FormatException)
        {
            // proofValue no es base64 válido.
            return false;
        }
        catch (CryptographicException)
        {
            // La firma tiene un formato inválido.
            return false;
        }
    }

    // Identificador del emisor (coordinador) dentro de la credencial.
    public string IdEmisor(int usuarioId)
    {
        return PrefijoUsuario + usuarioId;
    }

    // Lee el código de la credencial desde su "id".
    public string? ObtenerCodigo(JsonObject credencial)
    {
        var id = Texto(credencial, "id");

        // Solo vale si el id tiene el prefijo esperado; el código es lo que sigue.
        return id is not null && id.StartsWith(PrefijoCredencial, StringComparison.Ordinal)
            ? id[PrefijoCredencial.Length..]
            : null;
    }

    // Lee el emisor desde "issuer.id".
    public string? ObtenerIdEmisor(JsonObject credencial)
    {
        return Texto(credencial, "issuer.id");
    }

    // ---------- Auxiliares privados ----------

    // Los bytes que se firman y de los que sale vc_hash: el JSON compacto SIN la propiedad "proof".
    private static byte[] BytesCanonicos(JsonObject credencial)
    {
        var copia = (JsonObject)credencial.DeepClone();
        copia.Remove("proof");
        return Encoding.UTF8.GetBytes(copia.ToJsonString(OpcionesJson));
    }

    // Crea la clave ECDSA a partir del texto PEM. Se crea una por operación y se libera al terminar,
    // porque los objetos de criptografía no deben compartirse entre peticiones simultáneas.
    private ECDsa CrearClave()
    {
        var ecdsa = ECDsa.Create();
        try
        {
            ecdsa.ImportFromPem(_clavePrivadaPem);
            return ecdsa;
        }
        catch
        {
            // Si la clave está mal escrita, libera el objeto y vuelve a lanzar el error original.
            ecdsa.Dispose();
            throw;
        }
    }

    // Lee un texto siguiendo una ruta con puntos, por ejemplo "issuer.id" o "type.0".
    // Un segmento numérico entra a un arreglo por su posición. Devuelve null si algo falta
    // o si el valor no es texto.
    private static string? Texto(JsonNode? nodo, string ruta)
    {
        foreach (var segmento in ruta.Split('.'))
        {
            if (nodo is JsonObject objeto)
            {
                // Si la propiedad no existe, TryGetPropertyValue deja "nodo" en null.
                objeto.TryGetPropertyValue(segmento, out nodo);
            }
            else if (nodo is JsonArray arreglo && int.TryParse(segmento, out var posicion)
                     && posicion >= 0 && posicion < arreglo.Count)
            {
                nodo = arreglo[posicion];
            }
            else
            {
                return null;
            }
        }

        return nodo is JsonValue valor && valor.TryGetValue<string>(out var texto) ? texto : null;
    }
}