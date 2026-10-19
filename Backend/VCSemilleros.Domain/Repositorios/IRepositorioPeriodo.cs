// Importa el espacio de nombres de los modelos (Periodo).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida para la tabla "periodo" (RF5; endpoints E7 y E8).
public interface IRepositorioPeriodo
{
	// Busca un periodo por id. Devuelve null si no existe (el servicio lo traduce a 404).
	// Lo usan varios servicios: participantes, sesiones, criterio vigente, evaluaciones...
	Task<Periodo?> ObtenerPorIdAsync(int id);

	// Lista los periodos de un semillero, hasta "limite" filas (E7).
	Task<List<Periodo>> ObtenerPorSemilleroAsync(int semilleroId, int limite);

	// Inserta un periodo nuevo (E8) y devuelve el id generado (INSERT ... RETURNING id).
	Task<int> CrearAsync(Periodo periodo);
}