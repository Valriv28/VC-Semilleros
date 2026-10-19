namespace VCSemilleros.Application.Servicios;

public interface IServicioAutenticacion
{
    Task<IdentidadAutenticada> IngresarAsync(
        string correo,
        string contrasena);

    Task<IdentidadAutenticada> RegistrarAsync(
        string nombre,
        string correo,
        string contrasena);
}

public record IdentidadAutenticada(
    int Id,
    string Nombre,
    string Correo,
    string Rol);
