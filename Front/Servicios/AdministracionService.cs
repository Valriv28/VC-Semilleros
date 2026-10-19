namespace Front.Servicios;

public sealed class AdministracionService
{
    private readonly ApiHttpService _api;
    public AdministracionService(ApiHttpService api) => _api = api;

    public Task<T?> ListarUsuariosAsync<T>(string? rol = null, CancellationToken ct = default)
    {
        var ruta = "api/administracion/usuarios";
        if (!string.IsNullOrWhiteSpace(rol))
            ruta += $"?rol={Uri.EscapeDataString(rol)}";
        return _api.ObtenerAsync<T>(ruta, ct);
    }

    public Task<TRespuesta?> CambiarRolAsync<TPeticion, TRespuesta>(int usuarioId, TPeticion datos, CancellationToken ct = default)
        => _api.ReemplazarAsync<TPeticion, TRespuesta>($"api/administracion/usuarios/{usuarioId}/rol", datos, ct);

    public Task<T?> ListarPermisosAsync<T>(string rol, CancellationToken ct = default)
        => _api.ObtenerAsync<T>($"api/administracion/permisos?rol={Uri.EscapeDataString(rol)}", ct);

    public Task<TRespuesta?> CrearPermisoAsync<TPeticion, TRespuesta>(TPeticion datos, CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>("api/administracion/permisos", datos, ct);

    public Task<TRespuesta?> GuardarPermisosRolAsync<TPeticion, TRespuesta>(string rol, TPeticion datos, CancellationToken ct = default)
        => _api.ReemplazarAsync<TPeticion, TRespuesta>($"api/administracion/roles/{Uri.EscapeDataString(rol)}/permisos", datos, ct);

    public Task<T?> ListarParametrosAccesoAsync<T>(CancellationToken ct = default)
        => _api.ObtenerAsync<T>("api/administracion/parametros-acceso", ct);

    public Task<TRespuesta?> GuardarParametroAccesoAsync<TPeticion, TRespuesta>(string clave, TPeticion datos, CancellationToken ct = default)
        => _api.ReemplazarAsync<TPeticion, TRespuesta>($"api/administracion/parametros-acceso/{Uri.EscapeDataString(clave)}", datos, ct);

    public Task<T?> ListarCredencialesEmitidasAsync<T>(int periodoId, CancellationToken ct = default)
        => _api.ObtenerAsync<T>($"api/administracion/credenciales-emitidas?periodoId={periodoId}", ct);

    public Task<T?> ListarTransaccionesBlockchainAsync<T>(CancellationToken ct = default)
        => _api.ObtenerAsync<T>("api/administracion/transacciones-blockchain", ct);
}
