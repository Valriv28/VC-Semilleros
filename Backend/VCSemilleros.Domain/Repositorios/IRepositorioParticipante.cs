// Importa el espacio de nombres de los modelos (Participante).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida para la tabla "participante" (RF6; endpoints E9 y E10).
public interface IRepositorioParticipante
{
    // Busca un participante por id. Devuelve null si no existe (el servicio lo traduce a 404).
    // Lo usan la asistencia (E13, E15) y la evaluación (E17).
    Task<Participante?> ObtenerPorIdAsync(int id);

    // Lista los participantes de un periodo, hasta "limite" filas (E9).
    Task<List<Participante>> ObtenerPorPeriodoAsync(int periodoId, int limite);

    // Inscribe a un estudiante en un periodo (E10) y devuelve el id generado.
    // Si el estudiante ya estaba inscrito, la BD lo rechaza por UNIQUE (periodo_id, estudiante_id)
    // y la API responde 500 con el error del motor (decisión D7).
    Task<int> CrearAsync(Participante participante);
}