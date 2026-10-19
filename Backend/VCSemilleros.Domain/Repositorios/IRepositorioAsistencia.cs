
using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Domain.Repositorios;

public interface IRepositorioAsistencia
{   
    Task<List<Asistencia>> ObtenerPorSesionAsync(int sesionId, int limite);
    Task<int> CrearVariasAsync(List<Asistencia> asistencias);
    Task<int> ContarPresentesAsync(int participanteId);
}