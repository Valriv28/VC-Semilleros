// Espacio de nombres de los adaptadores de Stellar (el RootNamespace del proyecto + la carpeta Adaptadores).
namespace VCSemilleros.Infrastructure.Stellar.Adaptadores;

// Configuración que necesita ClienteStellar para hablar con el puente de Attest.
//
// POR QUÉ ES UNA CLASE APARTE: este proyecto solo depende de Domain (Art. 4) y no tiene los paquetes de
// configuración de ASP.NET Core. Así que no lee appsettings ni variables de entorno por su cuenta:
// Program.cs (el ensamblador) lee la configuración y le entrega estos valores YA armados.
//
// "sealed" significa que nadie puede heredar de esta clase; es lo normal para una clase de datos simple.
public sealed class OpcionesStellar
{
    // Dirección del puente Node (misma máquina o red privada). Debe terminar en "/" para que las rutas
    // relativas ("anclajes") se sumen bien a esta dirección.
    public string BaseUrl { get; set; } = "http://127.0.0.1:4010/";

    // Clave del puente: la misma que BRIDGE_API_KEY del puente. Viaja en la cabecera "X-Bridge-Key".
    // NUNCA va en el repositorio: llega por variable de entorno (Stellar__ApiKey).
    public string ApiKey { get; set; } = "";

    // Dirección "G…" de la cuenta de la plataforma, la que firma las atestaciones.
    // Una evidencia solo cuenta si la firmó esta cuenta: en Attest cualquiera puede atestar,
    // y la confianza viene de QUIÉN firmó.
    public string DireccionEmisora { get; set; } = "";

    // Identificador del esquema de Attest (64 caracteres hexadecimales) que imprime `npm run register-schema`.
    public string SchemaUid { get; set; } = "";

    // Segundos máximos de espera por cada llamada al puente. Pasado ese tiempo se considera "no disponible".
    public int TimeoutSegundos { get; set; } = 60;
}