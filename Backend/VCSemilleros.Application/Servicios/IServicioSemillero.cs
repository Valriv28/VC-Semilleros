// Importa el espacio de nombres de los modelos del dominio (Semillero).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los servicios (casos de uso) de la capa Application.
namespace VCSemilleros.Application.Servicios;

public interface IServicioSemillero
{
	Task<List<Semillero>> ListarAsync(int limite);
	Task<Semillero> ObtenerAsync(int id);
	Task<int> CrearAsync(int responsableId, string nombre);
	Task<int> CambiarEstadoAsync(int id, bool activo);
}