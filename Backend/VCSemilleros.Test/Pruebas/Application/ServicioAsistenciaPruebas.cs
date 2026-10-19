using VCSemilleros.Application.Servicios;       // El servicio que se prueba.
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // Periodo, Sesion, Participante, Asistencia.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioAsistencia con repositorios FALSOS en memoria.
// Cubre el porcentaje 100 / 0 / 100 del smoke test, el periodo sin sesiones y la asistencia atómica.
public class ServicioAsistenciaPruebas
{
    private readonly RepositorioPeriodoFalso _periodos = new();
    private readonly RepositorioSesionFalso _sesiones = new();
    private readonly RepositorioParticipanteFalso _participantes = new();
    private readonly RepositorioAsistenciaFalso _asistencias = new();
    private readonly ServicioAsistencia _servicio;

    // Constructor: siembra dos periodos (1 con sesiones, 2 sin ellas), tres participantes en el periodo 1,
    // uno en el periodo 2, y dos sesiones del periodo 1.
    public ServicioAsistenciaPruebas()
    {
        _periodos.Periodos.Add(new Periodo { Id = 1, SemilleroId = 1, Nombre = "2026-2", FechaInicio = new DateOnly(2026, 8, 1), FechaFin = new DateOnly(2026, 12, 15) });
        _periodos.Periodos.Add(new Periodo { Id = 2, SemilleroId = 1, Nombre = "2027-1", FechaInicio = new DateOnly(2027, 2, 1), FechaFin = new DateOnly(2027, 6, 30) });

        _participantes.Participantes.Add(new Participante { Id = 1, PeriodoId = 1, EstudianteId = 3 });
        _participantes.Participantes.Add(new Participante { Id = 2, PeriodoId = 1, EstudianteId = 4 });
        _participantes.Participantes.Add(new Participante { Id = 3, PeriodoId = 1, EstudianteId = 5 });
        _participantes.Participantes.Add(new Participante { Id = 4, PeriodoId = 2, EstudianteId = 3 });   // Periodo SIN sesiones.

        _sesiones.Sesiones.Add(new Sesion { Id = 1, PeriodoId = 1, Fecha = new DateOnly(2026, 10, 3), Descripcion = "Revisión de avances" });
        _sesiones.Sesiones.Add(new Sesion { Id = 2, PeriodoId = 1, Fecha = new DateOnly(2026, 10, 10), Descripcion = "Ponencias" });

        _servicio = new ServicioAsistencia(_periodos, _sesiones, _participantes, _asistencias);
    }

    // Método auxiliar: arma la lista de asistencias del smoke test (p1 y p3 presentes, p2 ausente).
    private static List<Asistencia> AsistenciasDelSmokeTest() => new()
    {
        new Asistencia { ParticipanteId = 1, Presente = true },
        new Asistencia { ParticipanteId = 2, Presente = false },
        new Asistencia { ParticipanteId = 3, Presente = true }
    };

    [Fact]
    public async Task RegistrarAsistencia_Valida_DevuelveTresFilas()
    {
        var filas = await _servicio.RegistrarAsistenciaAsync(1, AsistenciasDelSmokeTest());

        Assert.Equal(3, filas);
    }

    [Fact]
    public async Task ResumenDeAsistencia_DaPorcentaje100_0_100()
    {
        // Registra la asistencia en las dos sesiones, como el paso 4 del smoke test.
        await _servicio.RegistrarAsistenciaAsync(1, AsistenciasDelSmokeTest());
        await _servicio.RegistrarAsistenciaAsync(2, AsistenciasDelSmokeTest());

        var r1 = await _servicio.ObtenerResumenAsync(1);
        var r2 = await _servicio.ObtenerResumenAsync(2);
        var r3 = await _servicio.ObtenerResumenAsync(3);

        // Assert.Equal con decimales: 100m y 0m (la "m" marca el número como decimal).
        Assert.Equal(100m, r1.Porcentaje);
        Assert.Equal(0m, r2.Porcentaje);
        Assert.Equal(100m, r3.Porcentaje);

        // Comprueba además los conteos del primer participante: 2 sesiones, 2 presentes.
        Assert.Equal(2, r1.Sesiones);
        Assert.Equal(2, r1.Presentes);
    }

    [Fact]
    public async Task Resumen_ConAsistenciaParcial_RedondeaADosDecimales()
    {
        // El participante 1 asiste a 1 de 2 sesiones = 50%.
        await _servicio.RegistrarAsistenciaAsync(1, new List<Asistencia> { new() { ParticipanteId = 1, Presente = true } });
        await _servicio.RegistrarAsistenciaAsync(2, new List<Asistencia> { new() { ParticipanteId = 1, Presente = false } });

        var resumen = await _servicio.ObtenerResumenAsync(1);

        Assert.Equal(50m, resumen.Porcentaje);
    }

    [Fact]
    public async Task Resumen_ConPeriodoSinSesiones_Lanza400()
    {
        // El participante 4 pertenece al periodo 2, que no tiene sesiones.
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.ObtenerResumenAsync(4));
    }

    [Fact]
    public async Task Resumen_ConParticipanteInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.ObtenerResumenAsync(999));
    }

    [Fact]
    public async Task RegistrarAsistencia_ConParticipanteDeOtroPeriodo_Lanza400()
    {
        // El participante 4 es del periodo 2, pero la sesión 1 es del periodo 1.
        var lista = new List<Asistencia> { new() { ParticipanteId = 4, Presente = true } };

        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.RegistrarAsistenciaAsync(1, lista));
    }

    [Fact]
    public async Task RegistrarAsistencia_ConSesionInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() =>
            _servicio.RegistrarAsistenciaAsync(999, AsistenciasDelSmokeTest()));
    }

    [Fact]
    public async Task RegistrarAsistencia_ConParticipanteInexistente_Lanza404()
    {
        var lista = new List<Asistencia> { new() { ParticipanteId = 999, Presente = true } };

        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.RegistrarAsistenciaAsync(1, lista));
    }

    [Fact]
    public async Task RegistrarAsistencia_Repetida_NoGuardaNingunaFila()
    {
        await _servicio.RegistrarAsistenciaAsync(1, AsistenciasDelSmokeTest());

        // La lista nueva trae al participante 2 (nuevo para la sesión 2) y al 1 repetido en la sesión 1:
        // se registra de nuevo la sesión 1 con el participante 1, que ya existe -> viola el UNIQUE.
        var repetida = new List<Asistencia> { new() { ParticipanteId = 1, Presente = true } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.RegistrarAsistenciaAsync(1, repetida));

        // Atomicidad: siguen siendo solo las 3 filas del primer registro.
        Assert.Equal(3, _asistencias.Asistencias.Count);
    }

    [Fact]
    public async Task RegistrarSesion_ConPeriodoInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() =>
            _servicio.RegistrarSesionAsync(999, new DateOnly(2026, 10, 3), null));
    }

    [Fact]
    public async Task ListarAsistencias_ConLimiteNegativo_Lanza400()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.ListarAsistenciasAsync(1, -1));
    }
}