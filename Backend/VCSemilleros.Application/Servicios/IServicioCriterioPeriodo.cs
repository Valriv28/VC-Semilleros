using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Application.Servicios;

public interface IServicioCriterioPeriodo
{
    Task<CriterioPeriodo?> ObtenerAsync(int periodoId);
    Task<CriterioPeriodo?> ObtenerPorParticipanteAsync(int participanteId);
    Task<CriterioPeriodo> GuardarAsync(int periodoId, int definidoPor,
        decimal minAsistenciaPct, decimal entregablesRequeridosPct, int permanenciaMeses);
}
