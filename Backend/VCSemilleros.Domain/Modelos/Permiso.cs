// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "permiso".
// Un permiso es una acción que un rol puede realizar (por ejemplo 'REVOCAR_CREDENCIAL').
// La tabla no tiene datos y su uso depende de la pregunta abierta A2 de la spec.
public class Permiso
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public string? Descripcion { get; set; }
}