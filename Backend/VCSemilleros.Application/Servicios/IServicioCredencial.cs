using VCSemilleros.Domain.Modelos;               // Credencial, EstadoCredencial.

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "consultar y revocar credenciales" (RF11, RF12, RF13; endpoints E21 a E25).
public interface IServicioCredencial
{
    // E21: lista las credenciales de un titular, hasta "limite" filas (puede devolver una lista vacía:
    // el controlador la traduce a 204). 400 si limite <= 0.
    Task<List<Credencial>> ListarPorTitularAsync(int titularId, int limite);

    // E22: obtiene una credencial por su código. 404 si no existe.
    Task<Credencial> ObtenerAsync(string codigo);

    // E23: devuelve la credencial firmada y serializada (el texto "vc").
    // La BD solo guarda vc_hash, así que la credencial se RECONSTRUYE desde los datos y se firma de nuevo.
    // Antes de devolverla se comprueba que el contenido reconstruido produzca el mismo vc_hash registrado:
    // si no coincide, los datos cambiaron después de emitir y la respuesta es un error del servidor (500).
    // 404 si la credencial no existe.
    Task<string> ObtenerVcAsync(string codigo);

    // E24: devuelve solo el estado vigente de la credencial. 404 si no existe.
    Task<EstadoCredencial> ObtenerEstadoAsync(string codigo);

    // E25: revoca una credencial con motivo y devuelve las filas afectadas.
    //   404 si la credencial o el usuario no existen.
    //   400 si quien revoca no es COORDINADOR, si el motivo está vacío o si la credencial ya estaba REVOCADA.
    // La BD es la fuente de verdad: primero se revoca allí y después se avisa a la red Stellar.
    // Si la red no responde, la revocación en la BD se mantiene igual (mejor esfuerzo).
    Task<int> RevocarAsync(string codigo, int revocadaPor, string motivo);
}
