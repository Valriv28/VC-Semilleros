using VCSemilleros.Application.Servicios;       // Los servicios que se prueban.
using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion.
using VCSemilleros.Domain.Modelos;              // Usuario, Periodo, Sesion, Participante, Asistencia.
using Xunit;                                    // xUnit: [Fact] y Assert.

namespace VCSemilleros.Tests.Pruebas.Application;

// Pruebas de ServicioEvaluacion con repositorios FALSOS en memoria.
// Cubre: cumple, elegibilidad, criterio vigente y las reglas de rol.
// Usa el ServicioAsistencia REAL (también con falsos) porque ServicioEvaluacion lo necesita
// para calcular el porcentaje "como E15".
public class ServicioEvaluacionPruebas
{
    private readonly RepositorioUsuarioFalso _usuarios = new();
    private readonly RepositorioPeriodoFalso _periodos = new();
    private readonly RepositorioSesionFalso _sesiones = new();
    private readonly RepositorioParticipanteFalso _participantes = new();
    private readonly RepositorioAsistenciaFalso _asistencias = new();
    private readonly RepositorioEvaluacionFalso _evaluaciones;
    private readonly ServicioEvaluacion _servicio;

    // Constructor: siembra el escenario del smoke test.
    //   Usuarios: 1 docente, 2 coordinador, 3 estudiante.
    //   Periodo 1 con 2 sesiones y 3 participantes (p1 y p3 presentes en ambas; p2 ausente en ambas).
    //   Periodo 2 sin sesiones, con el participante 4.
    // CAMBIO: cada Usuario lleva ahora ContrasenaHash (propiedad "required" desde la enmienda al Art. 6).
    public ServicioEvaluacionPruebas()
    {
        _usuarios.Usuarios.Add(new Usuario { Id = 1, Nombre = "Carlos", Correo = "c@x.edu", Rol = RolUsuario.DOCENTE, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });
        _usuarios.Usuarios.Add(new Usuario { Id = 2, Nombre = "Coordinador", Correo = "k@x.edu", Rol = RolUsuario.COORDINADOR, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });
        _usuarios.Usuarios.Add(new Usuario { Id = 3, Nombre = "Ana", Correo = "a@x.edu", Rol = RolUsuario.ESTUDIANTE, Activo = true, ContrasenaHash = DatosDePrueba.HashContrasena });

        _periodos.Periodos.Add(new Periodo { Id = 1, SemilleroId = 1, Nombre = "2026-2", FechaInicio = new DateOnly(2026, 8, 1), FechaFin = new DateOnly(2026, 12, 15) });
        _periodos.Periodos.Add(new Periodo { Id = 2, SemilleroId = 1, Nombre = "2027-1", FechaInicio = new DateOnly(2027, 2, 1), FechaFin = new DateOnly(2027, 6, 30) });

        _participantes.Participantes.Add(new Participante { Id = 1, PeriodoId = 1, EstudianteId = 3 });
        _participantes.Participantes.Add(new Participante { Id = 2, PeriodoId = 1, EstudianteId = 4 });
        _participantes.Participantes.Add(new Participante { Id = 3, PeriodoId = 1, EstudianteId = 5 });
        _participantes.Participantes.Add(new Participante { Id = 4, PeriodoId = 2, EstudianteId = 3 });

        _sesiones.Sesiones.Add(new Sesion { Id = 1, PeriodoId = 1, Fecha = new DateOnly(2026, 10, 3) });
        _sesiones.Sesiones.Add(new Sesion { Id = 2, PeriodoId = 1, Fecha = new DateOnly(2026, 10, 10) });

        // Asistencia sembrada directamente en el falso: p1 y p3 presentes (100%), p2 ausente (0%).
        foreach (var sesionId in new[] { 1, 2 })
        {
            _asistencias.Asistencias.Add(new Asistencia { Id = sesionId * 10 + 1, SesionId = sesionId, ParticipanteId = 1, Presente = true });
            _asistencias.Asistencias.Add(new Asistencia { Id = sesionId * 10 + 2, SesionId = sesionId, ParticipanteId = 2, Presente = false });
            _asistencias.Asistencias.Add(new Asistencia { Id = sesionId * 10 + 3, SesionId = sesionId, ParticipanteId = 3, Presente = true });
        }

        // El falso de evaluaciones necesita el falso de participantes para saber el periodo.
        _evaluaciones = new RepositorioEvaluacionFalso(_participantes);

        var servicioAsistencia = new ServicioAsistencia(_periodos, _sesiones, _participantes, _asistencias);
        _servicio = new ServicioEvaluacion(_evaluaciones, _participantes, _periodos, _usuarios, servicioAsistencia);
    }

    // ---------- E17: evaluar ----------

    [Fact]
    public async Task Evaluar_ConAsistenciaCompleta_Cumple()
    {
        var evaluacion = await _servicio.EvaluarAsync(1, 80m, 2, 1);

        Assert.Equal(1, evaluacion.Id);
        Assert.Equal(100m, evaluacion.PctAsistencia);
        Assert.True(evaluacion.Cumple);
    }

    [Fact]
    public async Task Evaluar_SinAsistencia_NoCumple()
    {
        var evaluacion = await _servicio.EvaluarAsync(2, 80m, 2, 1);

        Assert.Equal(0m, evaluacion.PctAsistencia);
        Assert.False(evaluacion.Cumple);
    }

    [Fact]
    public async Task Evaluar_ConPorcentajeIgualAlMinimo_Cumple()
    {
        // La regla es "mayor o igual": 100% con mínimo 100% SÍ cumple.
        var evaluacion = await _servicio.EvaluarAsync(1, 100m, 2, 1);

        Assert.True(evaluacion.Cumple);
    }

    [Fact]
    public async Task Evaluar_ConDocenteComoDefinidor_Lanza400()
    {
        // El usuario 1 es DOCENTE: no define criterios.
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.EvaluarAsync(1, 80m, 1, 1));
    }

    [Fact]
    public async Task Evaluar_ConUsuarioInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.EvaluarAsync(1, 80m, 999, 1));
    }

    [Fact]
    public async Task Evaluar_ConParticipanteInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.EvaluarAsync(999, 80m, 2, 1));
    }

    [Fact]
    public async Task Evaluar_ConPeriodoSinSesiones_Lanza400()
    {
        // El participante 4 es del periodo 2, que no tiene sesiones.
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.EvaluarAsync(4, 80m, 2, 1));
    }

    [Fact]
    public async Task Evaluar_ConVersionRepetida_LanzaErrorDelMotor()
    {
        await _servicio.EvaluarAsync(1, 80m, 2, 1);

        // Imita el UNIQUE (participante_id, criterio_version): en la API real sube como 500.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servicio.EvaluarAsync(1, 80m, 2, 1));
    }

    // ---------- E16: criterio vigente ----------

    [Fact]
    public async Task CriterioVigente_DevuelveLaMayorVersion()
    {
        await _servicio.EvaluarAsync(1, 80m, 2, 1);
        await _servicio.EvaluarAsync(1, 90m, 2, 2);   // Misma persona, versión 2 del criterio.

        var vigente = await _servicio.ObtenerCriterioVigenteAsync(1);

        Assert.Equal(2, vigente.CriterioVersion);
        Assert.Equal(90m, vigente.MinAsistenciaPct);
        Assert.Equal(2, vigente.DefinidoPor);
    }

    [Fact]
    public async Task CriterioVigente_SinEvaluaciones_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.ObtenerCriterioVigenteAsync(1));
    }

    [Fact]
    public async Task CriterioVigente_ConPeriodoInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.ObtenerCriterioVigenteAsync(999));
    }

    // ---------- E18: listar ----------

    [Fact]
    public async Task ListarPorPeriodo_ConLimiteCero_Lanza400()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.ListarPorPeriodoAsync(1, 0));
    }

    [Fact]
    public async Task ListarPorPeriodo_DevuelveSoloLasDeEsePeriodo()
    {
        await _servicio.EvaluarAsync(1, 80m, 2, 1);
        await _servicio.EvaluarAsync(2, 80m, 2, 1);

        var lista = await _servicio.ListarPorPeriodoAsync(1, 10);

        Assert.Equal(2, lista.Count);
    }

    // ---------- E19: elegibilidad ----------

    [Fact]
    public async Task Declarar_ConEvaluacionQueCumple_GuardaAlCoordinador()
    {
        var evaluacion = await _servicio.EvaluarAsync(1, 80m, 2, 1);

        var filas = await _servicio.DeclararElegibilidadAsync(evaluacion.Id, 2);

        Assert.Equal(1, filas);
        Assert.Equal(2, _evaluaciones.Evaluaciones.First(e => e.Id == evaluacion.Id).ElegibleDeclaradoPor);
    }

    [Fact]
    public async Task Declarar_ConEvaluacionQueNoCumple_Lanza400()
    {
        var evaluacion = await _servicio.EvaluarAsync(2, 80m, 2, 1);   // 0%: no cumple.

        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.DeclararElegibilidadAsync(evaluacion.Id, 2));
    }

    [Fact]
    public async Task Declarar_ConUsuarioQueNoEsCoordinador_Lanza400()
    {
        var evaluacion = await _servicio.EvaluarAsync(1, 80m, 2, 1);

        // El usuario 1 es DOCENTE.
        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.DeclararElegibilidadAsync(evaluacion.Id, 1));
    }

    [Fact]
    public async Task Declarar_DosVeces_Lanza400()
    {
        var evaluacion = await _servicio.EvaluarAsync(1, 80m, 2, 1);
        await _servicio.DeclararElegibilidadAsync(evaluacion.Id, 2);

        await Assert.ThrowsAsync<ArgumentException>(() => _servicio.DeclararElegibilidadAsync(evaluacion.Id, 2));
    }

    [Fact]
    public async Task Declarar_ConEvaluacionInexistente_Lanza404()
    {
        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.DeclararElegibilidadAsync(999, 2));
    }

    [Fact]
    public async Task Declarar_ConUsuarioInexistente_Lanza404()
    {
        var evaluacion = await _servicio.EvaluarAsync(1, 80m, 2, 1);

        await Assert.ThrowsAsync<NoEncontradoExcepcion>(() => _servicio.DeclararElegibilidadAsync(evaluacion.Id, 999));
    }
}