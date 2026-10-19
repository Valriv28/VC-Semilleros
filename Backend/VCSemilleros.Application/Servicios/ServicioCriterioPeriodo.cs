using VCSemilleros.Domain.Excepciones;
using VCSemilleros.Domain.Modelos;
using VCSemilleros.Domain.Repositorios;

namespace VCSemilleros.Application.Servicios;

public sealed class ServicioCriterioPeriodo : IServicioCriterioPeriodo
{
    private readonly IRepositorioCriterioPeriodo _criterios;
    private readonly IRepositorioPeriodo _periodos;
    private readonly IRepositorioUsuario _usuarios;
    private readonly IRepositorioParticipante _participantes;

    public ServicioCriterioPeriodo(IRepositorioCriterioPeriodo criterios,
        IRepositorioPeriodo periodos, IRepositorioUsuario usuarios, IRepositorioParticipante participantes)
    {
        _criterios = criterios;
        _periodos = periodos;
        _usuarios = usuarios;
        _participantes = participantes;
    }

    public async Task<CriterioPeriodo?> ObtenerAsync(int periodoId)
    {
        if (periodoId <= 0) throw new ArgumentException("El periodoId debe ser mayor que cero.");
        if (await _periodos.ObtenerPorIdAsync(periodoId) is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}.");
        return await _criterios.ObtenerPorPeriodoAsync(periodoId);
    }

    public async Task<CriterioPeriodo?> ObtenerPorParticipanteAsync(int participanteId)
    {
        if (participanteId <= 0) throw new ArgumentException("El participanteId debe ser mayor que cero.");
        var participante = await _participantes.ObtenerPorIdAsync(participanteId);
        if (participante is null)
            throw new NoEncontradoExcepcion($"No existe un participante con id = {participanteId}.");
        return await _criterios.ObtenerPorPeriodoAsync(participante.PeriodoId);
    }

    public async Task<CriterioPeriodo> GuardarAsync(int periodoId, int definidoPor,
        decimal minAsistenciaPct, decimal entregablesRequeridosPct, int permanenciaMeses)
    {
        if (periodoId <= 0) throw new ArgumentException("El periodoId debe ser mayor que cero.");
        if (minAsistenciaPct is < 0 or > 100)
            throw new ArgumentException("La asistencia mínima debe estar entre 0 y 100.");
        if (entregablesRequeridosPct is < 0 or > 100)
            throw new ArgumentException("El porcentaje de entregables debe estar entre 0 y 100.");
        if (permanenciaMeses < 0)
            throw new ArgumentException("La permanencia en meses no puede ser negativa.");

        if (await _periodos.ObtenerPorIdAsync(periodoId) is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}.");

        var coordinador = await _usuarios.ObtenerPorIdAsync(definidoPor);
        if (coordinador is null)
            throw new NoEncontradoExcepcion($"No existe un usuario con id = {definidoPor}.");
        if (coordinador.Rol != RolUsuario.COORDINADOR)
            throw new ArgumentException("Solo un usuario con rol COORDINADOR puede configurar los criterios.");

        return await _criterios.GuardarAsync(new CriterioPeriodo
        {
            PeriodoId = periodoId,
            MinAsistenciaPct = minAsistenciaPct,
            EntregablesRequeridosPct = entregablesRequeridosPct,
            PermanenciaMeses = permanenciaMeses,
            DefinidoPor = definidoPor
        });
    }
}
