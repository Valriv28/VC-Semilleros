// Importa el espacio de nombres de los modelos (Credencial).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

public interface IRepositorioCredencial
{
    Task<Credencial?> ObtenerPorCodigoAsync(string codigo);
    Task<Credencial?> ObtenerPorEvaluacionAsync(int evaluacionId);
    Task<List<Credencial>> ObtenerPorTitularAsync(int titularId, int limite);
    Task<int> CrearAsync(Credencial credencial);
    Task<int> RevocarAsync(string codigo, int revocadaPor, string motivo);
}