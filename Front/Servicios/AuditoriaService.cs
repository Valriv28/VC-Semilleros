
namespace Front.Servicios;

public sealed class AuditoriaService
{
    private readonly ApiHttpService _api;

    public AuditoriaService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<T?> ObtenerAnclajeAsync<T>(
        string codigo,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/auditoria/{Uri.EscapeDataString(codigo)}/anclaje", ct);

    public Task<T?> ObtenerLineaTiempoAsync<T>(
        string codigo,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/auditoria/{Uri.EscapeDataString(codigo)}/linea-tiempo", ct);

    public Task<T?> ObtenerIntegridadAsync<T>(
        string codigo,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/auditoria/{Uri.EscapeDataString(codigo)}/integridad", ct);
}
