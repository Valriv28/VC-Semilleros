using VCSemilleros.Application.Servicios;       // El servicio que se prueba.
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // Usuario, Semillero, RolUsuario.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioPeriodo con repositorios FALSOS en memoria.
// Cubre: semillero inactivo, fechas inválidas y rol del participante.
public class ServicioPeriodoPruebas
{
    private readonly RepositorioUsuarioFalso _usuarios = new();
    private readonly RepositorioSemilleroFalso _semilleros = new();
    private readonly RepositorioPeriodoFalso _periodos = new();
    private readonly RepositorioParticipanteFalso _participantes = new();
    private readonly ServicioPeriodo _servicio;

    // Constructor: siembra docente (1), estudiante (2), semillero activo (1) e inactivo (2), y un periodo (1).
    // CAMBIO: cada Usuario lleva ahora ContrasenaHash (propiedad "required" desde la enmienda al Art. 6).
    public ServicioPeriodoPruebas()
    {
        _usuarios.Usuarios.Add(new Usuario { Id = 1, Nombre = "Carlos", Correo = "c@x.edu", Rol = RolUsuario.DOCENTE, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });
        _usuarios.Usuarios.Add(new Usuario { Id = 2, Nombre = "Ana", Correo = "a@x.edu", Rol = RolUsuario.ESTUDIANTE, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });

        _semilleros.Semilleros.Add(new Semillero { Id = 1, ResponsableId = 1, Nombre = "Activo", Activo = true });
        _semilleros.Semilleros.Add(new Semillero { Id = 2, ResponsableId = 1, Nombre = "Inactivo", Activo = false });

        _periodos.Periodos.Add(new Periodo
        {
            Id = 1,
            SemilleroId = 1,
            Nombre = "2026-2",
            FechaInicio = new DateOnly(2026, 8, 1),
            FechaFin = new DateOnly(2026, 12, 15)
        });

        _servicio = new ServicioPeriodo(_semilleros, _periodos, _participantes, _usuarios);
    }

    [Fact]
    public async Task CrearPeriodo_Valido_DevuelveElId()
    {
        // Ya hay un periodo sembrado (id 1), así que el nuevo recibe el id 2.
        var id = await _servicio.CrearPeriodoAsync(1, "2027-1", new DateOnly(2027, 2, 1), new DateOnly(2027, 6, 30));

        Assert.Equal(2, id);
    }

    [Fact]
    public async Task CrearPeriodo_ConSemilleroInactivo_Lanza400()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicio.CrearPeriodoAsync(2, "2026-2", new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 15)));
    }

    [Fact]
    public async Task CrearPeriodo_ConFechaFinAnterior_Lanza400()
    {
        // La fecha de fin (agosto) es anterior a la de inicio (diciembre).
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _servicio.CrearPeriodoAsync(1, "P", new DateOnly(2026, 12, 15), new DateOnly(2026, 8, 1)));
    }

    [Fact]
    public async Task CrearPeriodo_ConSemilleroInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() =>
            _servicio.CrearPeriodoAsync(999, "P", new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 15)));
    }

    [Fact]
    public async Task ListarPeriodos_ConSemilleroInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.ListarPeriodosAsync(999, 10));
    }

    [Fact]
    public async Task RegistrarParticipante_ConEstudiante_DevuelveElId()
    {
        var id = await _servicio.RegistrarParticipanteAsync(1, 2);

        Assert.Equal(1, id);
    }

    [Fact]
    public async Task RegistrarParticipante_ConDocente_Lanza400()
    {
        // El usuario 1 es DOCENTE: no puede ser participante.
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.RegistrarParticipanteAsync(1, 1));
    }

    [Fact]
    public async Task RegistrarParticipante_ConUsuarioInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.RegistrarParticipanteAsync(1, 999));
    }

    [Fact]
    public async Task RegistrarParticipante_Repetido_LanzaErrorDelMotor()
    {
        await _servicio.RegistrarParticipanteAsync(1, 2);

        // Imita el UNIQUE de la BD. En la API real este error sube como 500 (decisión D7);
        // por eso aquí es InvalidOperationException y no ArgumentException.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.RegistrarParticipanteAsync(1, 2));
    }
}