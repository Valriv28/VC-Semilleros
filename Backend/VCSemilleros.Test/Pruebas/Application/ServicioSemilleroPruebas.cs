using VCSemilleros.Application.Servicios;       // El servicio que se prueba.
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // Usuario, Semillero, RolUsuario.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioSemillero con repositorios FALSOS en memoria (sin PostgreSQL ni red).
// xUnit crea una instancia NUEVA de esta clase por cada prueba, así que los campos y el
// constructor dejan cada prueba con datos limpios y sin afectar a las demás.
public class ServicioSemilleroPruebas
{
    private readonly RepositorioUsuarioFalso _usuarios = new();
    private readonly RepositorioSemilleroFalso _semilleros = new();
    private readonly ServicioSemillero _servicio;

    // Constructor: siembra un docente (id 1) y un estudiante (id 2), y arma el servicio con los falsos.
    // CAMBIO: cada Usuario lleva ahora ContrasenaHash (propiedad "required" desde la enmienda al Art. 6).
    public ServicioSemilleroPruebas()
    {
        _usuarios.Usuarios.Add(new Usuario { Id = 1, Nombre = "Carlos Ramírez", Correo = "carlos@x.edu", Rol = RolUsuario.DOCENTE, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });
        _usuarios.Usuarios.Add(new Usuario { Id = 2, Nombre = "Ana Torres", Correo = "ana@x.edu", Rol = RolUsuario.ESTUDIANTE, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });

        _servicio = new ServicioSemillero(_semilleros, _usuarios);
    }

    // [Fact] marca un método como una prueba de xUnit. Patrón de cada prueba:
    // preparar (Arrange) -> ejecutar (Act) -> comprobar (Assert).

    [Fact]
    public async Task Crear_ConDocenteComoResponsable_DevuelveElId()
    {
        // Act: ejecuta el caso de uso.
        var id = await _servicio.CrearAsync(1, "Semillero de Investigación VC");

        // Assert: el id es 1 y el semillero quedó guardado y activo.
        Assert.Equal(1, id);
        var guardado = await _servicio.ObtenerAsync(id);
        Assert.Equal("Semillero de Investigación VC", guardado.Nombre);
        Assert.True(guardado.Activo);
    }

    [Fact]
    public async Task Crear_ConEstudianteComoResponsable_Lanza400()
    {
        // Assert.ThrowsAsync<T> comprueba que el método lance exactamente una excepción de tipo T.
        // ArgumentException es la que el controlador traduce a 400.
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.CrearAsync(2, "X"));
    }

    [Fact]
    public async Task Crear_ConResponsableInexistente_Lanza404()
    {
        // NoEncontradoExcepcion es la que el controlador traduce a 404.
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.CrearAsync(999, "X"));
    }

    [Fact]
    public async Task Listar_ConLimiteCero_Lanza400()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.ListarAsync(0));
    }

    [Fact]
    public async Task Obtener_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.ObtenerAsync(999));
    }

    [Fact]
    public async Task CambiarEstado_Existente_ActualizaYDevuelveUnaFila()
    {
        var id = await _servicio.CrearAsync(1, "Semillero");

        var filas = await _servicio.CambiarEstadoAsync(id, false);

        Assert.Equal(1, filas);
        Assert.False((await _servicio.ObtenerAsync(id)).Activo);
    }

    [Fact]
    public async Task CambiarEstado_Inexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.CambiarEstadoAsync(999, true));
    }
}