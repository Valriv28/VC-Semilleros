namespace Front.Servicios;

public sealed class VerificacionesService
{
    private readonly ApiHttpService _api;

    public VerificacionesService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<TRespuesta?> VerificarAsync<TPeticion, TRespuesta>(
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            "api/verificaciones", datos, ct);
}
