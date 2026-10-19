// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto de esta clase representa UNA fila de la tabla "administrador".
// Una propiedad por cada columna de la tabla (5_data_model.md §2).
public class Administrador
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Correo { get; set; }
    public required string ContrasenaHash { get; set; }
}