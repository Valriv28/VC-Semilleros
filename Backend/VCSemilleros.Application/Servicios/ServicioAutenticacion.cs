using System.Security.Cryptography;
using VCSemilleros.Domain.Excepciones;
using VCSemilleros.Domain.Modelos;
using VCSemilleros.Domain.Repositorios;

namespace VCSemilleros.Application.Servicios;

public class ServicioAutenticacion : IServicioAutenticacion
{
    private const string MensajeInvalido =
        "Correo o contraseña incorrectos.";

    private readonly IRepositorioUsuario _repositorioUsuario;
    private readonly IRepositorioAdministrador _repositorioAdministrador;

    public ServicioAutenticacion(
        IRepositorioUsuario repositorioUsuario,
        IRepositorioAdministrador repositorioAdministrador)
    {
        _repositorioUsuario = repositorioUsuario;
        _repositorioAdministrador = repositorioAdministrador;
    }

    public async Task<IdentidadAutenticada> IngresarAsync(
        string correo,
        string contrasena)
    {
        correo = correo?.Trim() ?? string.Empty;

        var usuario =
            await _repositorioUsuario.ObtenerPorCorreoAsync(correo);

        if (usuario is not null)
        {
            if (!usuario.Activo ||
                !VerificarContrasena(contrasena, usuario.ContrasenaHash))
            {
                throw new NoAutorizadoExcepcion(MensajeInvalido);
            }

            return new IdentidadAutenticada(
                usuario.Id,
                usuario.Nombre,
                usuario.Correo,
                usuario.Rol.ToString());
        }

        var administrador =
            await _repositorioAdministrador.ObtenerPorCorreoAsync(correo);

        if (administrador is not null)
        {
            if (!VerificarContrasena(
                    contrasena,
                    administrador.ContrasenaHash))
            {
                throw new NoAutorizadoExcepcion(MensajeInvalido);
            }

            return new IdentidadAutenticada(
                administrador.Id,
                administrador.Nombre,
                administrador.Correo,
                "ADMINISTRADOR");
        }

        throw new NoAutorizadoExcepcion(MensajeInvalido);
    }

    public async Task<IdentidadAutenticada> RegistrarAsync(
        string nombre,
        string correo,
        string contrasena)
    {
        nombre = nombre?.Trim() ?? string.Empty;
        correo = correo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 120)
        {
            throw new ArgumentException(
                "El nombre es obligatorio y no puede superar 120 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(correo) ||
            correo.Length > 120 ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                correo,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException(
                "Debes proporcionar un correo válido.");
        }

        if (string.IsNullOrEmpty(contrasena) ||
            contrasena.Length < 8 ||
            contrasena.Length > 120)
        {
            throw new ArgumentException(
                "La contraseña debe tener entre 8 y 120 caracteres.");
        }

        var usuarioExistente =
            await _repositorioUsuario.ObtenerPorCorreoAsync(correo);

        var administradorExistente =
            await _repositorioAdministrador.ObtenerPorCorreoAsync(correo);

        if (usuarioExistente is not null ||
            administradorExistente is not null)
        {
            throw new ArgumentException(
                "Ya existe una cuenta registrada con ese correo.");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            Rol = RolUsuario.ESTUDIANTE,
            CreadoPor = null,
            Activo = true,
            ContrasenaHash = CrearHashContrasena(contrasena)
        };

        var id = await _repositorioUsuario.CrearAsync(usuario);

        return new IdentidadAutenticada(
            id,
            usuario.Nombre,
            usuario.Correo,
            usuario.Rol.ToString());
    }

    private static string CrearHashContrasena(string contrasena)
    {
        const int iteraciones = 100_000;

        byte[] sal = RandomNumberGenerator.GetBytes(16);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            sal,
            iteraciones,
            HashAlgorithmName.SHA256,
            32);

        return $"pbkdf2-sha256${iteraciones}$" +
               $"{Convert.ToBase64String(sal)}$" +
               $"{Convert.ToBase64String(hash)}";
    }

    private static bool VerificarContrasena(
        string contrasena,
        string hashGuardado)
    {
        if (string.IsNullOrEmpty(contrasena) ||
            string.IsNullOrEmpty(hashGuardado))
        {
            return false;
        }

        var partes = hashGuardado.Split('$');

        if (partes.Length != 4 ||
            partes[0] != "pbkdf2-sha256")
        {
            return false;
        }

        if (!int.TryParse(partes[1], out var iteraciones) ||
            iteraciones <= 0 ||
            iteraciones > 2_000_000)
        {
            return false;
        }

        byte[] sal;
        byte[] esperado;

        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (sal.Length == 0 || esperado.Length == 0)
        {
            return false;
        }

        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            sal,
            iteraciones,
            HashAlgorithmName.SHA256,
            esperado.Length);

        return CryptographicOperations.FixedTimeEquals(
            calculado,
            esperado);
    }
}
