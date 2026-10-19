// Importa el puerto IClienteStellar y los tipos ResultadoAnclaje, ResultadoRevocacion y EvidenciaEnRed.
using VCSemilleros.Domain.Repositorios;

// Espacio de nombres de los adaptadores de Stellar (el RootNamespace del proyecto + la carpeta Adaptadores).
namespace VCSemilleros.Infrastructure.Stellar.Adaptadores;

// ADAPTADOR PROVISIONAL del puerto IClienteStellar.
//
// POR QUÉ EXISTE: el adaptador real (ClienteStellar, que habla con el puente de Attest) lo construye otro
// equipo y todavía no existe. Sin ALGUNA clase registrada para IClienteStellar, ASP.NET Core no puede crear
// los servicios de emisión, credencial y auditoría, y esos tres controladores darían 500 en todos sus endpoints.
//
// QUÉ HACE: se comporta como si la red Stellar NO respondiera. Es exactamente el caso que ya prevén
// el puerto y la propuesta P4: la emisión no se revierte, la credencial queda emitida y el anclaje
// queda FALLIDO. Así el resto del backend se puede probar completo.
//
// CÓMO SE REEMPLAZA: cuando exista ClienteStellar, se cambia UNA línea en Program.cs y se borra este archivo.
// Ningún servicio ni controlador se toca (Art. 4).
//
// No inventa ningún dato: nunca devuelve un hash de transacción ni un identificador de atestación falsos.
public class ClienteStellarProvisional : IClienteStellar
{
    // Mensaje único que se devuelve en cada fallo, en español y con la causa a la vista.
    private const string Mensaje =
        "La red Stellar no está disponible: todavía no se ha conectado el adaptador real (ClienteStellar).";

    // E20: pide anclar la evidencia. Responde "fallido" sin lanzar excepción, como exige el puerto.
    // "Task.FromResult" crea una tarea YA terminada con ese resultado: aquí no hay nada que esperar,
    // pero el puerto es asíncrono porque el adaptador real sí hará llamadas de red.
    public Task<ResultadoAnclaje> AnclarAsync(string credencialCodigo, string evidenciaHash)
    {
        // Exitoso = false; TxHash = null; AttestationUid = null; y el motivo del fallo.
        return Task.FromResult(new ResultadoAnclaje(false, null, null, Mensaje));
    }

    // E25: pide revocar la atestación en la red. También responde "fallido" sin lanzar excepción;
    // ServicioCredencial lo trata como mejor esfuerzo: la revocación en la BD se mantiene igual.
    public Task<ResultadoRevocacion> RevocarAsync(string credencialCodigo, string attestationUid)
    {
        return Task.FromResult(new ResultadoRevocacion(false, null, Mensaje));
    }

    // E27 y E29: consulta lo registrado en la red. Devuelve null = "no se encontró o la red no respondió",
    // que el servicio de auditoría traduce a existeEnRed = false y evidenciaHashRed = null.
    public Task<EvidenciaEnRed?> ConsultarAsync(string attestationUid)
    {
        return Task.FromResult<EvidenciaEnRed?>(null);
    }
}