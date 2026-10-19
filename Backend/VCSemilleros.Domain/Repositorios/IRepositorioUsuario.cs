// Importa el espacio de nombres de los modelos (Usuario).
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los puertos del dominio.
namespace VCSemilleros.Domain.Repositorios;

// Un PUERTO es una interfaz: un contrato que dice QUÉ operaciones existen, sin decir CÓMO se hacen.
// El servicio depende de esta interfaz; la clase RepositorioUsuarioPostgreSql (en Infrastructure)
// la implementa con SQL a mano. Así el dominio no conoce PostgreSQL (Art. 4).
public interface IRepositorioUsuario
{
    Task<Usuario?> ObtenerPorIdAsync(int id);
    Task<Usuario?> ObtenerPorCorreoAsync(string correo);
}