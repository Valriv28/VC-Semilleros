// Importa el espacio de nombres de los modelos (Evaluacion).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;
    
public interface IRepositorioEvaluacion
{
    Task<Evaluacion?> ObtenerPorIdAsync(int id);
    Task<List<Evaluacion>> ObtenerPorPeriodoAsync(int periodoId, int limite);
    Task<Evaluacion?> ObtenerCriterioVigenteAsync(int periodoId);
    Task<int> CrearAsync(Evaluacion evaluacion);
    Task<int> DeclararElegibleAsync(int id, int coordinadorId);
}