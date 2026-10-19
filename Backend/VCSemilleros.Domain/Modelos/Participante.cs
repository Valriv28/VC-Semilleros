// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "participante".
// Un participante es un estudiante inscrito en un periodo concreto.
public class Participante
{
    public int Id { get; set; }
    public int PeriodoId { get; set; }
    public int EstudianteId { get; set; }
}