// Espacio de nombres: el mismo que el de los demás modelos del dominio.
namespace VCSemilleros.Domain.Modelos;

// Modelo entidad: cada objeto representa UNA fila de la tabla "rol_permiso".
// Es una tabla de unión: dice qué permisos tiene cada rol (relación muchos a muchos).
// Su clave primaria es compuesta: (rol, permiso_id). La tabla no tiene datos (A2).
public class RolPermiso
{
	public RolUsuario Rol { get; set; }
	public int PermisoId { get; set; }
}