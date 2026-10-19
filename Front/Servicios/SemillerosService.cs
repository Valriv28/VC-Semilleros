namespace Front.Servicios;

public sealed class SemillerosService
{
    private readonly ApiHttpService _api;

    public SemillerosService(ApiHttpService api)
    {
        _api = api;
    }

    // Lista todos los semilleros.
    // Este método se conserva para los módulos que tienen permiso para usarlo.
    public Task<T?> ListarAsync<T>(
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/semilleros?limite={limite}", ct);

    // Lista únicamente los semilleros del docente autenticado.
    public Task<T?> ListarMiosAsync<T>(
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/semilleros/mios?limite={limite}", ct);

    public Task<T?> ObtenerAsync<T>(
        int id,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/semilleros/{id}", ct);

    public Task<TRespuesta?> CrearAsync<TPeticion, TRespuesta>(
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            "api/semilleros", datos, ct);

    public Task<TRespuesta?> CambiarEstadoAsync<TPeticion, TRespuesta>(
        int id,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.ActualizarParcialAsync<TPeticion, TRespuesta>(
            $"api/semilleros/{id}/estado", datos, ct);

    public Task<T?> ListarPeriodosAsync<T>(
        int semilleroId,
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/semilleros/{semilleroId}/periodos?limite={limite}", ct);

    public Task<TRespuesta?> CrearPeriodoAsync<TPeticion, TRespuesta>(
        int semilleroId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            $"api/semilleros/{semilleroId}/periodos", datos, ct);
}
