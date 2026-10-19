// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "usuario".
// Un usuario puede ser ESTUDIANTE, DOCENTE o COORDINADOR (según su rol).
public class Usuario
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Correo { get; set; }
    public RolUsuario Rol { get; set; }
    public int? CreadoPor { get; set; }
    public bool Activo { get; set; }
    public required string ContrasenaHash { get; set; }
}