using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Application.Servicios;

public interface IServicioSemillero
{
    Task<List<Semillero>> ListarAsync(int limite);

    Task<Semillero> ObtenerAsync(int id);

    Task<int> CrearAsync(int responsableId, string nombre);

    Task<int> CambiarEstadoAsync(int id, bool activo);

    Task<List<Semillero>> ListarPorResponsableAsync(int responsableId, int limite);
}
