namespace Front.Servicios;

public sealed class PeriodosService
{
    private readonly ApiHttpService _api;

    public PeriodosService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<T?> ListarParticipantesAsync<T>(
        int periodoId,
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/periodos/{periodoId}/participantes?limite={limite}", ct);

    public Task<TRespuesta?> RegistrarParticipanteAsync<TPeticion, TRespuesta>(
        int periodoId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            $"api/periodos/{periodoId}/participantes", datos, ct);

    public Task<T?> ListarSesionesAsync<T>(
        int periodoId,
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/periodos/{periodoId}/sesiones?limite={limite}", ct);

    public Task<TRespuesta?> RegistrarSesionAsync<TPeticion, TRespuesta>(
        int periodoId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            $"api/periodos/{periodoId}/sesiones", datos, ct);

    public Task<T?> ObtenerCriterioVigenteAsync<T>(
        int periodoId,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/periodos/{periodoId}/criterio-vigente", ct);


    public Task<TRespuesta?> GuardarCriterioVigenteAsync<TPeticion, TRespuesta>(
        int periodoId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.ReemplazarAsync<TPeticion, TRespuesta>(
            $"api/periodos/{periodoId}/criterio-vigente", datos, ct);

    public Task<T?> ListarEvaluacionesAsync<T>(
        int periodoId,
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/periodos/{periodoId}/evaluaciones?limite={limite}", ct);
}
