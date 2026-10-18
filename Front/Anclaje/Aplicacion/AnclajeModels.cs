namespace Front.Anclaje;

/// <summary>Lo que se pide anclar. Solo referencia interna + huella: nunca datos personales.</summary>
/// <param name="Referencia">Id interno (credencial, entregable…), máx. 64 caracteres.</param>
/// <param name="Sha256Hex">SHA-256 en hexadecimal (64 caracteres).</param>
/// <param name="Tipo">"credencial", "entregable"… máx. 32 caracteres.</param>
/// <param name="ExpiraEn">Vigencia opcional; Attest la hace cumplir al consultar.</param>
public sealed record SolicitudAnclaje(string Referencia, string Sha256Hex, string Tipo, DateTimeOffset? ExpiraEn = null);

/// <summary>Qué guardar en tu base de datos tras anclar: el UID es la ÚNICA llave para consultar luego.</summary>
public sealed record ResultadoAnclaje(string AttestationUid, string TxHash, string ExplorerUrl);

public enum EstadoEvidencia { Vigente, Revocada, Expirada, NoEncontrada }

/// <summary>Lo que hay en la cadena para un UID.</summary>
/// <param name="EmitidaPorLaPlataforma">
/// true solo si la firmó NUESTRA cuenta con NUESTRO esquema. Cualquiera puede atestar en Attest;
/// la confianza viene de quién firmó. null = no se pudo saber (p. ej. expirada: el contrato no devuelve datos).
/// </param>
public sealed record EvidenciaAnclada(
    EstadoEvidencia Estado,
    string? Uid,
    string? Referencia,
    string? Sha256Hex,
    DateTimeOffset? RegistradaEn,
    DateTimeOffset? ExpiraEn,
    DateTimeOffset? RevocadaEn,
    bool? EmitidaPorLaPlataforma)
{
    public static EvidenciaAnclada SinRegistro { get; } =
        new(EstadoEvidencia.NoEncontrada, null, null, null, null, null, null, null);
}

public enum ResultadoVerificacion
{
    Valida,                  // vigente, la firmó la plataforma y el hash coincide
    HashDistinto,            // el archivo/credencial presentado NO es el que se ancló
    Revocada,
    Expirada,
    NoEncontrada,
    NoEmitidaPorLaPlataforma // existe, pero la firmó otra cuenta u otro esquema
}

public static class Verificacion
{
    /// <summary>Función pura (sin red): compara lo que hay en cadena con la huella que presenta el usuario.</summary>
    public static ResultadoVerificacion Evaluar(EvidenciaAnclada e, string sha256HexPresentado) => e.Estado switch
    {
        EstadoEvidencia.NoEncontrada => ResultadoVerificacion.NoEncontrada,
        EstadoEvidencia.Expirada => ResultadoVerificacion.Expirada,
        _ when e.EmitidaPorLaPlataforma != true => ResultadoVerificacion.NoEmitidaPorLaPlataforma,
        EstadoEvidencia.Revocada => ResultadoVerificacion.Revocada,
        _ when !string.Equals(e.Sha256Hex, sha256HexPresentado, StringComparison.OrdinalIgnoreCase)
            => ResultadoVerificacion.HashDistinto,
        _ => ResultadoVerificacion.Valida,
    };
}

/// <summary>La red Stellar o el puente no respondieron: guarda la operación como PENDIENTE y reintenta luego.</summary>
public sealed class AnclajeNoDisponibleException(string mensaje, Exception? inner = null) : Exception(mensaje, inner);

/// <summary>El contrato o el puente rechazaron la operación (hash inválido, no revocable, ya revocada…): reintentar no sirve.</summary>
public sealed class AnclajeRechazadoException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
