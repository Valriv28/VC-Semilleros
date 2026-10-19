using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;              // Evaluacion, RolUsuario.
using VCSemilleros.Domain.Repositorios;         // Los puertos de evaluación, participante, periodo y usuario.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: criterio, evaluación y elegibilidad (RF8, RF9).
// Art. 2: evaluar y declarar la elegibilidad son decisiones SEPARADAS de la asistencia y de la emisión.
public class ServicioEvaluacion : IServicioEvaluacion
{
    private readonly IRepositorioEvaluacion _repositorioEvaluacion;
    private readonly IRepositorioParticipante _repositorioParticipante;
    private readonly IRepositorioPeriodo _repositorioPeriodo;
    private readonly IRepositorioUsuario _repositorioUsuario;

    // Este servicio reutiliza OTRO servicio (no un repositorio) para calcular el porcentaje,
    // porque E17 lo calcula "como E15" y así la fórmula vive en un solo lugar.
    private readonly IServicioAsistencia _servicioAsistencia;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioEvaluacion(
        IRepositorioEvaluacion repositorioEvaluacion,
        IRepositorioParticipante repositorioParticipante,
        IRepositorioPeriodo repositorioPeriodo,
        IRepositorioUsuario repositorioUsuario,
        IServicioAsistencia servicioAsistencia)
    {
        _repositorioEvaluacion = repositorioEvaluacion;
        _repositorioParticipante = repositorioParticipante;
        _repositorioPeriodo = repositorioPeriodo;
        _repositorioUsuario = repositorioUsuario;
        _servicioAsistencia = servicioAsistencia;
    }

    // E16: criterio vigente = evaluación de mayor versión del periodo (P1).
    public async Task<Evaluacion> ObtenerCriterioVigenteAsync(int periodoId)
    {
        // El periodo debe existir (404).
        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(periodoId);
        if (periodo is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}");

        // Si el periodo aún no tiene evaluaciones, no hay criterio vigente (404).
        var vigente = await _repositorioEvaluacion.ObtenerCriterioVigenteAsync(periodoId);
        if (vigente is null)
            throw new NoEncontradoExcepcion($"El periodo {periodoId} aún no tiene un criterio definido.");

        return vigente;
    }

    // E17: evalúa a un participante contra un criterio.
    public async Task<Evaluacion> EvaluarAsync(int participanteId, decimal minAsistenciaPct, int definidoPor, int criterioVersion)
    {
        // 1) El participante debe existir (404).
        var participante = await _repositorioParticipante.ObtenerPorIdAsync(participanteId);
        if (participante is null)
            throw new NoEncontradoExcepcion($"No existe un participante con id = {participanteId}");

        // 2) Quien define el criterio debe existir (404).
        var coordinador = await _repositorioUsuario.ObtenerPorIdAsync(definidoPor);
        if (coordinador is null)
            throw new NoEncontradoExcepcion($"No existe un usuario con id = {definidoPor}");

        // 3) Regla de negocio: solo un COORDINADOR define el criterio (400).
        if (coordinador.Rol != RolUsuario.COORDINADOR)
            throw new ArgumentException("Quien define el criterio debe ser un usuario con rol COORDINADOR.");

        // 4) Calcula el porcentaje con el mismo cálculo de E15. Si el periodo no tiene sesiones,
        // ObtenerResumenAsync lanza ArgumentException (400).
        var resumen = await _servicioAsistencia.ObtenerResumenAsync(participanteId);

        // 5) Regla de negocio: cumple si el porcentaje alcanza el mínimo exigido.
        var cumple = resumen.Porcentaje >= minAsistenciaPct;

        // 6) Arma la evaluación y la guarda. "fecha" y "elegible_declarado_por" los completa la BD.
        // Si ya existe esa versión para el participante, la BD viola UNIQUE y la API responde 500.
        var evaluacion = new Evaluacion
        {
            ParticipanteId = participanteId,
            CriterioVersion = criterioVersion,
            MinAsistenciaPct = minAsistenciaPct,
            DefinidoPor = definidoPor,
            PctAsistencia = resumen.Porcentaje,
            Cumple = cumple
        };

        // Guarda el id generado en el mismo objeto para devolverlo completo al controlador.
        evaluacion.Id = await _repositorioEvaluacion.CrearAsync(evaluacion);

        return evaluacion;
    }

    // E18: lista las evaluaciones de un periodo.
    public async Task<List<Evaluacion>> ListarPorPeriodoAsync(int periodoId, int limite)
    {
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        // El periodo debe existir (404).
        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(periodoId);
        if (periodo is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}");

        return await _repositorioEvaluacion.ObtenerPorPeriodoAsync(periodoId, limite);
    }

    // E19: un coordinador declara la elegibilidad de una evaluación.
    public async Task<int> DeclararElegibilidadAsync(int evaluacionId, int declaradoPor)
    {
        // 1) La evaluación debe existir (404).
        var evaluacion = await _repositorioEvaluacion.ObtenerPorIdAsync(evaluacionId);
        if (evaluacion is null)
            throw new NoEncontradoExcepcion($"No existe una evaluación con id = {evaluacionId}");

        // 2) Quien declara debe existir (404).
        var coordinador = await _repositorioUsuario.ObtenerPorIdAsync(declaradoPor);
        if (coordinador is null)
            throw new NoEncontradoExcepcion($"No existe un usuario con id = {declaradoPor}");

        // 3) Regla de negocio: solo se declara elegible a quien CUMPLE el criterio (400).
        if (!evaluacion.Cumple)
            throw new ArgumentException("La evaluación no cumple el criterio: no puede declararse elegible.");

        // 4) Regla de negocio: solo un COORDINADOR declara la elegibilidad (400).
        if (coordinador.Rol != RolUsuario.COORDINADOR)
            throw new ArgumentException("Quien declara la elegibilidad debe ser un usuario con rol COORDINADOR.");

        // 5) Regla de negocio: no se declara dos veces (400). Si ya tiene coordinador, ya está declarada.
        if (evaluacion.ElegibleDeclaradoPor is not null)
            throw new ArgumentException("La elegibilidad de esta evaluación ya había sido declarada.");

        return await _repositorioEvaluacion.DeclararElegibleAsync(evaluacionId, declaradoPor);
    }
}