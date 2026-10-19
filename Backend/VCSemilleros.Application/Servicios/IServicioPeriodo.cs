// Importa el espacio de nombres de los modelos del dominio (Periodo, Participante).
using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "gestionar periodos y participantes"
// (RF5 y RF6; endpoints E7 a E10).
public interface IServicioPeriodo
{
    Task<List<Periodo>> ListarPeriodosAsync(int semilleroId, int limite);

    Task<int> CrearPeriodoAsync(int semilleroId, string nombre, DateOnly fechaInicio, DateOnly fechaFin);
    Task<List<Participante>> ListarParticipantesAsync(int periodoId, int limite);
    Task<int> RegistrarParticipanteAsync(int periodoId, int estudianteId);
}