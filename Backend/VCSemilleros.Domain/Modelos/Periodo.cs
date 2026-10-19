// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "periodo".
// Un periodo es un ciclo de trabajo de un semillero (por ejemplo "2026-2").
public class Periodo
{
    public int Id { get; set; }
    public int SemilleroId { get; set; }
    public required string Nombre { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
}