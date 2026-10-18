namespace Front.Anclaje;

/// <summary>Sección "Anclaje" de la configuración. ApiKey va en user-secrets o variables de entorno, NUNCA en appsettings.json.</summary>
public sealed class AnclajeOptions
{
    public const string SectionName = "Anclaje";

    /// <summary>Dirección del puente Node (misma máquina o red privada).</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:4010/";

    /// <summary>Misma clave que BRIDGE_API_KEY del puente.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Dirección G… de la cuenta de la plataforma (la que firma). Una evidencia solo cuenta si la firmó ella.</summary>
    public string DireccionEmisora { get; set; } = "";

    /// <summary>Schema UID (64 hex) que imprimió `npm run register-schema`.</summary>
    public string SchemaUid { get; set; } = "";

    public int TimeoutSegundos { get; set; } = 60;
}
