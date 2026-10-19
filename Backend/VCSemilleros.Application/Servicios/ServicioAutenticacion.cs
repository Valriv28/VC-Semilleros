using System.Security.Cryptography;              // PBKDF2 (Rfc2898DeriveBytes) y la comparación segura: vienen con .NET, sin paquetes.
using VCSemilleros.Domain.Excepciones;           // NoAutorizadoExcepcion (el controlador la vuelve 401).
using VCSemilleros.Domain.Repositorios;          // Los puertos: IRepositorioUsuario e IRepositorioAdministrador.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: ingresar (RF2, E2). Solo identifica a la persona; el token JWT lo crea la Api.
// No conoce HTTP, Dapper, Npgsql ni el paquete JWT (Art. 4).
public class ServicioAutenticacion : IServicioAutenticacion
{
    // Un solo mensaje para TODOS los fallos de ingreso (correo inexistente, contraseña incorrecta,
    // usuario inactivo): así quien intenta entrar no puede descubrir qué correos están registrados.
    // "const" = valor fijo que nunca cambia.
    private const string MensajeInvalido = "Correo o contraseña incorrectos.";

    private readonly IRepositorioUsuario _repositorioUsuario;
    private readonly IRepositorioAdministrador _repositorioAdministrador;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioAutenticacion(IRepositorioUsuario repositorioUsuario, IRepositorioAdministrador repositorioAdministrador)
    {
        _repositorioUsuario = repositorioUsuario;
        _repositorioAdministrador = repositorioAdministrador;
    }

    // E2: comprueba correo y contraseña.
    public async Task<IdentidadAutenticada> IngresarAsync(string correo, string contrasena)
    {
        // 1) Busca primero en la tabla "usuario" (estudiantes, docentes y coordinadores).
        var usuario = await _repositorioUsuario.ObtenerPorCorreoAsync(correo);
        if (usuario is not null)
        {
            // Un usuario inactivo no entra aunque la contraseña sea correcta. Se usa el MISMO mensaje.
            if (!usuario.Activo || !VerificarContrasena(contrasena, usuario.ContrasenaHash))
                throw new NoAutorizadoExcepcion(MensajeInvalido);

            // usuario.Rol.ToString() convierte el enum a texto: "DOCENTE", "COORDINADOR" o "ESTUDIANTE".
            return new IdentidadAutenticada(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol.ToString());
        }

        // 2) Si no está en "usuario", busca en "administrador".
        var administrador = await _repositorioAdministrador.ObtenerPorCorreoAsync(correo);
        if (administrador is not null)
        {
            if (!VerificarContrasena(contrasena, administrador.ContrasenaHash))
                throw new NoAutorizadoExcepcion(MensajeInvalido);

            // El administrador no tiene rol en el enum de la BD: su rol "ADMINISTRADOR" se asigna aquí,
            // por el hecho de haber salido de la tabla "administrador".
            return new IdentidadAutenticada(administrador.Id, administrador.Nombre, administrador.Correo, "ADMINISTRADOR");
        }

        // 3) El correo no está en ninguna de las dos tablas: mismo mensaje, mismo 401.
        throw new NoAutorizadoExcepcion(MensajeInvalido);
    }

    // Comprueba una contraseña contra el hash guardado, con el formato:
    //   pbkdf2-sha256$<iteraciones>$<sal en base64>$<hash en base64>
    // PBKDF2 aplica el hash SHA-256 muchas veces (las "iteraciones") sobre la contraseña y una sal,
    // para que adivinar contraseñas sea lento. "static" = no necesita una instancia de la clase.
    private static bool VerificarContrasena(string contrasena, string hashGuardado)
    {
        // Separa el texto por "$": debe dar exactamente 4 partes y la primera debe ser el nombre del algoritmo.
        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2-sha256")
            return false;

        // TryParse intenta convertir el texto en número sin lanzar excepción si falla.
        if (!int.TryParse(partes[1], out var iteraciones) || iteraciones <= 0)
            return false;

        byte[] sal;
        byte[] esperado;
        try
        {
            // Base64 es una forma de escribir bytes como texto. Aquí se vuelve a convertir en bytes.
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            // El hash guardado está mal formado: se trata como contraseña incorrecta.
            return false;
        }

        // Calcula el hash de la contraseña recibida, con la MISMA sal, iteraciones y longitud que el guardado.
        var calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

        // FixedTimeEquals compara en tiempo constante: no se detiene en el primer byte distinto, así que
        // el tiempo de respuesta no delata cuántos bytes coincidían.
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}