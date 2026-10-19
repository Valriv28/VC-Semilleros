
using VCSemilleros.Domain.Modelos;

namespace VCSemilleros.Domain.Repositorios;

public interface IRepositorioUsuario
{
    Task<Usuario?> ObtenerPorIdAsync(int id);

    Task<Usuario?> ObtenerPorCorreoAsync(string correo);

    Task<int> CrearAsync(Usuario usuario);
}
