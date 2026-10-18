using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Front.Anclaje;

/// <summary>Adaptador de infraestructura: implementa el puerto hablando con el puente Node, que invoca el contrato de Attest.</summary>
public sealed class AttestAnclajeAdapter(HttpClient http, IOptions<AnclajeOptions> options) : IAnclajeEvidencia
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly AnclajeOptions _o = options.Value;

    // Respuestas del puente (camelCase; JsonSerializerDefaults.Web las lee sin distinguir mayúsculas).
    private sealed record RespAnclaje(string AttestationUid, string TxHash, string ExplorerUrl);
    private sealed record RespRevocar(string TxHash, string ExplorerUrl);
    private sealed record RespConsulta(
        string Estado, string? Uid, string? SchemaUid, string? Attester, string? Referencia,
        string? Sha256, long? RegistradaEn, long? ExpiraEn, long? RevocadaEn);
    private sealed record RespError(string? Codigo, string? Mensaje);

    public async Task<ResultadoAnclaje> AnclarAsync(SolicitudAnclaje s, CancellationToken ct = default)
    {
        var sha = NormalizarSha(s.Sha256Hex);
        var cuerpo = new
        {
            referencia = s.Referencia,
            sha256 = sha,
            tipo = s.Tipo,
            expiraEn = s.ExpiraEn?.ToUnixTimeSeconds(),
        };

        using var resp = await EnviarAsync(() => http.PostAsJsonAsync("anclajes", cuerpo, Json, ct), ct);
        await AsegurarExitoAsync(resp, ct);
        var r = await LeerAsync<RespAnclaje>(resp, ct);
        return new ResultadoAnclaje(r.AttestationUid, r.TxHash, r.ExplorerUrl);
    }

    public async Task<EvidenciaAnclada> ConsultarAsync(string attestationUid, CancellationToken ct = default)
    {
        using var resp = await EnviarAsync(() => http.GetAsync($"anclajes/{Uri.EscapeDataString(attestationUid)}", ct), ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return EvidenciaAnclada.SinRegistro;
        await AsegurarExitoAsync(resp, ct);

        var d = await LeerAsync<RespConsulta>(resp, ct);
        var estado = d.Estado switch
        {
            "vigente" => EstadoEvidencia.Vigente,
            "revocada" => EstadoEvidencia.Revocada,
            "expirada" => EstadoEvidencia.Expirada,
            _ => throw new AnclajeRechazadoException("estado_desconocido", $"Estado inesperado del puente: {d.Estado}"),
        };

        // Expirada: el contrato no devuelve datos, así que no se puede comprobar quién la firmó.
        bool? nuestra = estado == EstadoEvidencia.Expirada
            ? null
            : string.Equals(d.Attester, _o.DireccionEmisora, StringComparison.Ordinal)
              && string.Equals(d.SchemaUid, _o.SchemaUid, StringComparison.OrdinalIgnoreCase);

        return new EvidenciaAnclada(
            estado, d.Uid, d.Referencia, d.Sha256,
            Fecha(d.RegistradaEn), Fecha(d.ExpiraEn), Fecha(d.RevocadaEn), nuestra);
    }

    public async Task RevocarAsync(string attestationUid, CancellationToken ct = default)
    {
        using var resp = await EnviarAsync(
            () => http.PostAsync($"anclajes/{Uri.EscapeDataString(attestationUid)}/revocar", content: null, ct), ct);
        await AsegurarExitoAsync(resp, ct);
        _ = await LeerAsync<RespRevocar>(resp, ct);
    }

    // ── Utilidades ───────────────────────────────────────────────────────────

    private static string NormalizarSha(string hex)
    {
        var h = hex?.Trim().ToLowerInvariant() ?? "";
        if (h.Length != 64 || !h.All(Uri.IsHexDigit))
            throw new AnclajeRechazadoException("hash_invalido", "El SHA-256 debe tener 64 caracteres hexadecimales.");
        return h;
    }

    private static DateTimeOffset? Fecha(long? unix) => unix is null ? null : DateTimeOffset.FromUnixTimeSeconds(unix.Value);

    /// <summary>Convierte fallos de red/timeout y 502/503/504 en "no disponible" (reintentable).</summary>
    private static async Task<HttpResponseMessage> EnviarAsync(Func<Task<HttpResponseMessage>> envio, CancellationToken ct)
    {
        HttpResponseMessage resp;
        try
        {
            resp = await envio();
        }
        catch (HttpRequestException ex)
        {
            throw new AnclajeNoDisponibleException("No se pudo contactar el servicio de anclaje.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new AnclajeNoDisponibleException("El servicio de anclaje no respondió a tiempo.", ex);
        }

        if (resp.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        {
            resp.Dispose();
            throw new AnclajeNoDisponibleException("La red Stellar o el servicio de anclaje no están disponibles.");
        }
        return resp;
    }

    private static async Task AsegurarExitoAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        RespError? err = null;
        try { err = await resp.Content.ReadFromJsonAsync<RespError>(Json, ct); }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { /* cuerpo que no es JSON */ }

        throw new AnclajeRechazadoException(
            err?.Codigo ?? $"http_{(int)resp.StatusCode}",
            err?.Mensaje ?? $"El servicio de anclaje respondió {(int)resp.StatusCode}.");
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage resp, CancellationToken ct) =>
        await resp.Content.ReadFromJsonAsync<T>(Json, ct)
        ?? throw new AnclajeRechazadoException("respuesta_vacia", "El servicio de anclaje devolvió una respuesta vacía.");
}
