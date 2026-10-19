namespace Front.Servicios;

public sealed class CredencialesService
{
    private readonly ApiHttpService _api;

    public CredencialesService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<T?> ListarAsync<T>(
        int? titularId = null,
        int limite = 1000,
        CancellationToken ct = default)
    {
        var ruta = $"api/credenciales?limite={limite}";

        if (titularId.HasValue)
            ruta += $"&titularId={titularId.Value}";

        return _api.ObtenerAsync<T>(ruta, ct);
    }

    public Task<T?> ObtenerAsync<T>(
        string codigo,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/credenciales/{Uri.EscapeDataString(codigo)}", ct);

    public Task<T?> ObtenerVcAsync<T>(
        string codigo,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/credenciales/{Uri.EscapeDataString(codigo)}/vc", ct);

    public Task<T?> ObtenerEstadoAsync<T>(
        string codigo,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/credenciales/{Uri.EscapeDataString(codigo)}/estado", ct);

    public Task<TRespuesta?> RevocarAsync<TPeticion, TRespuesta>(
        string codigo,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            $"api/credenciales/{Uri.EscapeDataString(codigo)}/revocacion",
            datos, ct);
}
