using System.Security.Cryptography;              // PBKDF2 para fabricar un hash real en la prueba.
using VCSemilleros.Application.Servicios;        // El servicio que se prueba.
using VCSemilleros.Domain.Excepciones;           // NoAutorizadoExcepcion.
using VCSemilleros.Domain.Modelos;               // Usuario, Administrador, RolUsuario.
using Xunit;                                     // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioAutenticacion (E2) con repositorios FALSOS en memoria.
// Aquí SÍ se necesita un hash de contraseña REAL, porque el servicio lo verifica de verdad.
public class ServicioAutenticacionPruebas
{
    // Contraseña de prueba (solo existe dentro de las pruebas).
    private const string Contrasena = "Semilleros2026*";

    // Mensaje que el servicio debe dar SIEMPRE que falla el ingreso, sea cual sea el motivo.
    private const string MensajeInvalido = "Correo o contraseña incorrectos.";

    private readonly RepositorioUsuarioFalso _usuarios = new();
    private readonly RepositorioAdministradorFalso _administradores = new();
    private readonly ServicioAutenticacion _servicio;

    // Fabrica un hash con el MISMO formato que usa el servicio y la siembra de PowerShell:
    //   pbkdf2-sha256$<iteraciones>$<sal en base64>$<hash en base64>
    // Se usan solo 1000 iteraciones (y no 100000) para que las pruebas corran rápido:
    // el formato guarda las iteraciones dentro del hash, así que el servicio verifica ambos igual.
    private static string CrearHash(string contrasena)
    {
        const int iteraciones = 1000;
        var sal = RandomNumberGenerator.GetBytes(16);   // 16 bytes aleatorios, como en la siembra.
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2-sha256${iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    // Constructor: siembra un docente activo, un estudiante INACTIVO y un administrador.
    public ServicioAutenticacionPruebas()
    {
        var hash = CrearHash(Contrasena);

        _usuarios.Usuarios.Add(new Usuario { Id = 1, Nombre = "Carlos Ramírez", Correo = "carlos.ramirez@institucion.edu.co", Rol = RolUsuario.DOCENTE, Activo = true, ContrasenaHash = hash });
        _usuarios.Usuarios.Add(new Usuario { Id = 2, Nombre = "Ana Torres", Correo = "ana.torres@institucion.edu.co", Rol = RolUsuario.ESTUDIANTE, Activo = false, ContrasenaHash = hash });

        _administradores.Administradores.Add(new Administrador { Id = 1, Nombre = "Administrador VC", Correo = "admin@semilleros.com", ContrasenaHash = hash });

        _servicio = new ServicioAutenticacion(_usuarios, _administradores);
    }

    [Fact]
    public async Task Ingresar_ConUsuarioYContrasenaCorrectos_DevuelveLaIdentidad()
    {
        var identidad = await _servicio.IngresarAsync("carlos.ramirez@institucion.edu.co", Contrasena);

        Assert.Equal(1, identidad.Id);
        Assert.Equal("Carlos Ramírez", identidad.Nombre);
        Assert.Equal("DOCENTE", identidad.Rol);
    }

    [Fact]
    public async Task Ingresar_ConAdministrador_DevuelveRolAdministrador()
    {
        var identidad = await _servicio.IngresarAsync("admin@semilleros.com", Contrasena);

        // El rol ADMINISTRADOR no existe en el enum de la BD: sale de haber encontrado la persona
        // en la tabla "administrador".
        Assert.Equal("ADMINISTRADOR", identidad.Rol);
    }

    [Fact]
    public async Task Ingresar_ConCorreoEnMayusculas_FuncionaIgual()
    {
        var identidad = await _servicio.IngresarAsync("CARLOS.RAMIREZ@INSTITUCION.EDU.CO", Contrasena);

        Assert.Equal(1, identidad.Id);
    }

    [Fact]
    public async Task Ingresar_ConContrasenaIncorrecta_Lanza401()
    {
        // NoAutorizadoExcepcion es la que el controlador traduce a 401.
        await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() =>
            _servicio.IngresarAsync("carlos.ramirez@institucion.edu.co", "otra-clave"));
    }

    [Fact]
    public async Task Ingresar_ConAdministradorYContrasenaIncorrecta_Lanza401()
    {
        await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() =>
            _servicio.IngresarAsync("admin@semilleros.com", "otra-clave"));
    }

    [Fact]
    public async Task Ingresar_ConCorreoInexistente_Lanza401()
    {
        await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() =>
            _servicio.IngresarAsync("nadie@institucion.edu.co", Contrasena));
    }

    [Fact]
    public async Task Ingresar_ConUsuarioInactivo_Lanza401()
    {
        // La contraseña es correcta, pero el usuario está inactivo: no entra.
        await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() =>
            _servicio.IngresarAsync("ana.torres@institucion.edu.co", Contrasena));
    }

    [Fact]
    public async Task Ingresar_ConHashMalFormado_Lanza401()
    {
        // Si el hash guardado no tiene el formato esperado, no se puede verificar: se trata como incorrecto.
        _usuarios.Usuarios.Add(new Usuario { Id = 3, Nombre = "Roto", Correo = "roto@x.edu", Rol = RolUsuario.COORDINADOR, Activo = true, ContrasenaHash = "esto-no-es-un-hash" });

        await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() => _servicio.IngresarAsync("roto@x.edu", Contrasena));
    }

    [Fact]
    public async Task Ingresar_ConCualquierFallo_DaElMismoMensaje()
    {
        // Mismo mensaje para correo inexistente, contraseña incorrecta y usuario inactivo:
        // así nadie puede descubrir qué correos están registrados.
        var e1 = await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() => _servicio.IngresarAsync("nadie@x.edu", Contrasena));
        var e2 = await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() => _servicio.IngresarAsync("carlos.ramirez@institucion.edu.co", "mala"));
        var e3 = await Assert.ThrowsAsync<NoAutorizadoExcepcion>(() => _servicio.IngresarAsync("ana.torres@institucion.edu.co", Contrasena));

        Assert.Equal(MensajeInvalido, e1.Message);
        Assert.Equal(MensajeInvalido, e2.Message);
        Assert.Equal(MensajeInvalido, e3.Message);
    }
}