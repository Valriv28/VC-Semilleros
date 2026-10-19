// Importa el espacio de nombres de los modelos del dominio (Sesion, Asistencia).
using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "sesiones y asistencia" (RF7; endpoints E11 a E15).
// Recuerda el Art. 2: la asistencia es la EVIDENCIA PRIMARIA; registrarla NO es certificar nada.
public interface IServicioAsistencia
{
    Task<List<Sesion>> ListarSesionesAsync(int periodoId, int limite);
    Task<int> RegistrarSesionAsync(int periodoId, DateOnly fecha, string? descripcion);
    Task<int> RegistrarAsistenciaAsync(int sesionId, List<Asistencia> asistencias);
    Task<List<Asistencia>> ListarAsistenciasAsync(int sesionId, int limite);
    Task<ResumenAsistencia> ObtenerResumenAsync(int participanteId);
}

// "record": tipo de C# para transportar datos, con propiedades de solo lectura.
// ResumenAsistencia es el resultado de E15. También lo reutiliza ServicioEvaluacion (E17)
// para obtener el porcentaje "como E15".
//   Sesiones:   cuántas sesiones tiene el periodo.
//   Presentes:  en cuántas estuvo presente el participante.
//   Porcentaje: presentes ÷ sesiones × 100, redondeado a 2 decimales.
public record ResumenAsistencia(int ParticipanteId, int PeriodoId, int Sesiones, int Presentes, decimal Porcentaje);