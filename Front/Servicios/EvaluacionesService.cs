
namespace Front.Servicios;

public sealed class EvaluacionesService
{
    private readonly ApiHttpService _api;

    public EvaluacionesService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<TRespuesta?> DeclararElegibilidadAsync<TPeticion, TRespuesta>(
        int evaluacionId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.ActualizarParcialAsync<TPeticion, TRespuesta>(
            $"api/evaluaciones/{evaluacionId}/elegibilidad",
            datos, ct);

    public Task<TRespuesta?> EmitirAsync<TRespuesta>(
        int evaluacionId,
        CancellationToken ct = default)
        => _api.EnviarAsync<object, TRespuesta>(
            $"api/evaluaciones/{evaluacionId}/emision",
            new { }, ct);
}
