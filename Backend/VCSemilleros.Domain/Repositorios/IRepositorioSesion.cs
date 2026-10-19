// Importa el espacio de nombres de los modelos (Sesion).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Puerto de salida para la tabla "sesion" (RF7; endpoints E11, E12 y apoyo a E13, E14, E15).
public interface IRepositorioSesion
{
    // Busca una sesión por id. Devuelve null si no existe (el servicio lo traduce a 404).
    // Lo usan E13 y E14 para comprobar que la sesión existe y a qué periodo pertenece.
    Task<Sesion?> ObtenerPorIdAsync(int id);

    // Lista las sesiones de un periodo, hasta "limite" filas (E11).
    Task<List<Sesion>> ObtenerPorPeriodoAsync(int periodoId, int limite);

    // Cuenta cuántas sesiones tiene un periodo (SELECT count(*) ...).
    // Es el denominador del porcentaje de asistencia (E15 y E17):
    // porcentaje = presentes ÷ sesiones del periodo × 100.
    Task<int> ContarPorPeriodoAsync(int periodoId);

    // Inserta una sesión nueva (E12) y devuelve el id generado.
    Task<int> CrearAsync(Sesion sesion);
}