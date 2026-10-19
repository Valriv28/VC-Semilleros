// Importa el espacio de nombres de los modelos del dominio (Evaluacion).
using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "criterio, evaluación y elegibilidad" (RF8, RF9; endpoints E16 a E19).
// Art. 2: evaluar (¿la evidencia cumple el criterio?) y declarar la elegibilidad son decisiones
// SEPARADAS de la asistencia y de la emisión de la credencial.
public interface IServicioEvaluacion
{
    Task<Evaluacion> ObtenerCriterioVigenteAsync(int periodoId);
    Task<Evaluacion> EvaluarAsync(int participanteId, decimal minAsistenciaPct, int definidoPor, int criterioVersion);
    Task<List<Evaluacion>> ListarPorPeriodoAsync(int periodoId, int limite);
    Task<int> DeclararElegibilidadAsync(int evaluacionId, int declaradoPor);
}