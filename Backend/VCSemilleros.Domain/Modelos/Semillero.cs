// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "semillero".
// Un semillero es un grupo de estudio o investigación a cargo de un docente.
public class Semillero
{
    public int Id { get; set; }
    public int ResponsableId { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; }
}