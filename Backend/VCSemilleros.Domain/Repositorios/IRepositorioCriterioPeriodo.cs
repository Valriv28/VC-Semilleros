using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Domain.Repositorios;

public interface IRepositorioCriterioPeriodo
{
    Task<CriterioPeriodo?> ObtenerPorPeriodoAsync(int periodoId);
    Task<CriterioPeriodo> GuardarAsync(CriterioPeriodo criterio);
}
