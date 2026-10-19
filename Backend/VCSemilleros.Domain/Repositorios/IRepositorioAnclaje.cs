// Importa el espacio de nombres de los modelos (AnclajeStellar, EstadoAnclaje).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida para la tabla "anclaje_stellar" (RF10 y RF15; endpoints E20, E27, E28, E29).
// OJO: este puerto guarda el REGISTRO del anclaje en nuestra BD. Hablar con la red Stellar
// es trabajo de otro puerto: IClienteStellar.
public interface IRepositorioAnclaje
{
    Task<int> CrearAsync(AnclajeStellar anclaje);
    Task<AnclajeStellar?> ObtenerPorCredencialAsync(string credencialCodigo);
    Task<int> ActualizarResultadoAsync(int id, EstadoAnclaje estado, string? txHash, string? attestationUid);
}