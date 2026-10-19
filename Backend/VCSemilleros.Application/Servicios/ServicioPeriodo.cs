using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;              // Periodo, Participante, RolUsuario.
using VCSemilleros.Domain.Repositorios;         // Los puertos de semillero, periodo, participante y usuario.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: gestionar periodos y participantes (RF5, RF6). Reglas de negocio en español, sin HTTP ni SQL.
public class ServicioPeriodo : IServicioPeriodo
{
    private readonly IRepositorioSemillero _repositorioSemillero;
    private readonly IRepositorioPeriodo _repositorioPeriodo;
    private readonly IRepositorioParticipante _repositorioParticipante;
    private readonly IRepositorioUsuario _repositorioUsuario;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioPeriodo(
        IRepositorioSemillero repositorioSemillero,
        IRepositorioPeriodo repositorioPeriodo,
        IRepositorioParticipante repositorioParticipante,
        IRepositorioUsuario repositorioUsuario)
    {
        _repositorioSemillero = repositorioSemillero;
        _repositorioPeriodo = repositorioPeriodo;
        _repositorioParticipante = repositorioParticipante;
        _repositorioUsuario = repositorioUsuario;
    }

    // E7: lista los periodos de un semillero.
    public async Task<List<Periodo>> ListarPeriodosAsync(int semilleroId, int limite)
    {
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        // El semillero debe existir (404).
        var semillero = await _repositorioSemillero.ObtenerPorIdAsync(semilleroId);
        if (semillero is null)
            throw new NoEncontradoExcepcion($"No existe un semillero con id = {semilleroId}");

        return await _repositorioPeriodo.ObtenerPorSemilleroAsync(semilleroId, limite);
    }

    // E8: crea un periodo.
    public async Task<int> CrearPeriodoAsync(int semilleroId, string nombre, DateOnly fechaInicio, DateOnly fechaFin)
    {
        // 1) El semillero debe existir (404).
        var semillero = await _repositorioSemillero.ObtenerPorIdAsync(semilleroId);
        if (semillero is null)
            throw new NoEncontradoExcepcion($"No existe un semillero con id = {semilleroId}");

        // 2) Regla de negocio: un semillero inactivo no admite periodos nuevos (400).
        if (!semillero.Activo)
            throw new ArgumentException("El semillero está inactivo; no admite periodos nuevos.");

        // 3) Regla de negocio: la fecha de fin no puede ser anterior a la de inicio (400).
        if (fechaFin < fechaInicio)
            throw new ArgumentException("La fecha de fin no puede ser anterior a la fecha de inicio.");

        var periodo = new Periodo
        {
            SemilleroId = semilleroId,
            Nombre = nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };

        return await _repositorioPeriodo.CrearAsync(periodo);
    }

    // E9: lista los participantes de un periodo.
    public async Task<List<Participante>> ListarParticipantesAsync(int periodoId, int limite)
    {
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        // El periodo debe existir (404).
        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(periodoId);
        if (periodo is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}");

        return await _repositorioParticipante.ObtenerPorPeriodoAsync(periodoId, limite);
    }

    // E10: inscribe a un estudiante en un periodo.
    public async Task<int> RegistrarParticipanteAsync(int periodoId, int estudianteId)
    {
        // 1) El periodo debe existir (404).
        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(periodoId);
        if (periodo is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}");

        // 2) El usuario debe existir (404).
        var usuario = await _repositorioUsuario.ObtenerPorIdAsync(estudianteId);
        if (usuario is null)
            throw new NoEncontradoExcepcion($"No existe un usuario con id = {estudianteId}");

        // 3) Regla de negocio: solo un ESTUDIANTE puede ser participante (400).
        if (usuario.Rol != RolUsuario.ESTUDIANTE)
            throw new ArgumentException("El usuario debe tener rol ESTUDIANTE para ser participante.");

        // 4) Guarda. Si ya estaba inscrito, la BD lanza la violación de UNIQUE y sube como 500.
        var participante = new Participante
        {
            PeriodoId = periodoId,
            EstudianteId = estudianteId
        };

        return await _repositorioParticipante.CrearAsync(participante);
    }
}