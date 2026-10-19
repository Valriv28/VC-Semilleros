namespace Front.Servicios;

public sealed class SesionesService
{
    private readonly ApiHttpService _api;

    public SesionesService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<TRespuesta?> RegistrarAsistenciasAsync<TPeticion, TRespuesta>(
        int sesionId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            $"api/sesiones/{sesionId}/asistencias", datos, ct);

    public Task<T?> ListarAsistenciasAsync<T>(
        int sesionId,
        int limite = 1000,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/sesiones/{sesionId}/asistencias?limite={limite}", ct);
}
