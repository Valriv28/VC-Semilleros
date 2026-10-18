namespace Front.Anclaje;

/// <summary>
/// Puerto de salida: lo único que el dominio/casos de uso conocen de blockchain.
/// Hoy lo implementa Attest (<see cref="AttestAnclajeAdapter"/>); si mañana cambia la tecnología, solo cambia el adaptador.
/// </summary>
public interface IAnclajeEvidencia
{
    /// <exception cref="AnclajeNoDisponibleException">Red caída: dejar la operación pendiente y reintentar.</exception>
    /// <exception cref="AnclajeRechazadoException">Rechazo definitivo.</exception>
    Task<ResultadoAnclaje> AnclarAsync(SolicitudAnclaje solicitud, CancellationToken ct = default);

    /// <summary>Lee la evidencia por su UID. Devuelve <see cref="EvidenciaAnclada.SinRegistro"/> si no existe.</summary>
    Task<EvidenciaAnclada> ConsultarAsync(string attestationUid, CancellationToken ct = default);

    /// <summary>Revoca la evidencia (el esquema se registró como revocable).</summary>
    Task RevocarAsync(string attestationUid, CancellationToken ct = default);
}
