// Importa el espacio de nombres de los modelos (Semillero).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida para la tabla "semillero" (RF3, RF4; endpoints E3 a E6).
public interface IRepositorioSemillero
{
	// Lista los semilleros, hasta "limite" filas (E3). En SQL: ... ORDER BY id LIMIT @limite.
	// "List<Semillero>" es una lista de objetos Semillero; si no hay filas, devuelve una lista vacía.
	Task<List<Semillero>> ObtenerTodosAsync(int limite);

	// Busca un semillero por id (E4 y comprobaciones de otros servicios).
	// Devuelve null si no existe; el servicio lo traduce a 404.
	Task<Semillero?> ObtenerPorIdAsync(int id);

	// Inserta un semillero nuevo (E5) y devuelve el id que generó PostgreSQL (INSERT ... RETURNING id).
	Task<int> CrearAsync(Semillero semillero);

	// Cambia solo la columna "activo" de un semillero (E6, PATCH = actualización parcial).
	// Devuelve las filas afectadas: 1 si lo actualizó, 0 si el id no existe.
	Task<int> ActualizarEstadoAsync(int id, bool activo);
}