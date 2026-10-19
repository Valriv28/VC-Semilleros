
namespace Front.Servicios;

public sealed class SesionUsuario
{
    public int? Id { get; private set; }
    public string? Nombre { get; private set; }
    public string? Correo { get; private set; }
    public string? Rol { get; private set; }
    public string? Token { get; private set; }

    public bool EstaAutenticado =>
        !string.IsNullOrWhiteSpace(Token);

    public void IniciarSesion(RespuestaIngreso respuesta)
    {
        Id = respuesta.Id;
        Nombre = respuesta.Nombre;
        Correo = respuesta.Correo;
        Rol = respuesta.Rol;
        Token = respuesta.Token;
    }

    public void CerrarSesion()
    {
        Id = null;
        Nombre = null;
        Correo = null;
        Rol = null;
        Token = null;
    }
}

public sealed class RespuestaIngreso
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
