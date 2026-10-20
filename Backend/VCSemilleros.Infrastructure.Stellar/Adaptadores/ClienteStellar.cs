// HttpStatusCode: para reconocer los códigos 502, 503 y 504 del puente.
using System.Net;
// PostAsJsonAsync y ReadFromJsonAsync: enviar y leer JSON sin escribirlo a mano.
using System.Net.Http.Json;
// JsonSerializerOptions y JsonException: opciones y errores del JSON.
using System.Text.Json;
// IClienteStellar y los records ResultadoAnclaje, ResultadoRevocacion y EvidenciaEnRed.
using VCSemilleros.Domain.Repositorios;

namespace VCSemilleros.Infrastructure.Stellar.Adaptadores;

// ADAPTADOR REAL del puerto IClienteStellar. Habla con el puente Node (tools/attest-bridge), que invoca
// el contrato Attest de Soroban y firma con la cuenta de la plataforma.
// Cumple el contrato del puerto: NUNCA lanza excepciones por fallos de red. Devuelve Exitoso = false
// (o null al consultar) para que el servicio guarde el anclaje como FALLIDO sin perder la credencial (P4).
public sealed class ClienteStellar : IClienteStellar
{
    private const string TipoEvidencia = "credencial";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly OpcionesStellar _opciones;

    // El HttpClient llega ya configurado desde Program.cs (dirección, tiempo máximo y clave del puente).
    public ClienteStellar(HttpClient http, OpcionesStellar opciones)
    {
        _http = http;
        _opciones = opciones;
    }

    // Lo que responde el puente (JSON en camelCase).
    private sealed record RespuestaAnclaje(string? AttestationUid, string? TxHash);
    private sealed record RespuestaRevocacion(string? TxHash);
    private sealed record RespuestaConsulta(string? Estado, string? SchemaUid, string? Attester, string? Sha256);
    private sealed record RespuestaError(string? Codigo, string? Mensaje);

    // E20: registra la evidencia en la red. La referencia es el código de la credencial: nunca datos personales.
    public async Task<ResultadoAnclaje> AnclarAsync(string credencialCodigo, string evidenciaHash)
    {
        var falta = ConfiguracionFaltante();
        if (falta is not null) return new ResultadoAnclaje(false, null, null, falta);

        try
        {
            var cuerpo = new
            {
                referencia = credencialCodigo,
                sha256 = evidenciaHash.Trim().ToLowerInvariant(),
                tipo = TipoEvidencia,
            };
            using var resp = await _http.PostAsJsonAsync("anclajes", cuerpo, Json);

            if (!resp.IsSuccessStatusCode)
                return new ResultadoAnclaje(false, null, null, Explicar(resp, await LeerErrorAsync(resp)));

            var r = await resp.Content.ReadFromJsonAsync<RespuestaAnclaje>(Json);
            if (r is null || string.IsNullOrWhiteSpace(r.AttestationUid) || string.IsNullOrWhiteSpace(r.TxHash))
                return new ResultadoAnclaje(false, null, null, "El puente respondió sin el identificador de la atestación.");

            return new ResultadoAnclaje(true, r.TxHash, r.AttestationUid, null);
        }
        catch (Exception ex) when (EsFalloDeRed(ex))
        {
            return new ResultadoAnclaje(false, null, null, "No se pudo contactar el puente de Stellar: " + ex.Message);
        }
    }

    // E25: revoca en la red la atestación. Solo puede hacerlo la cuenta que la creó (la de la plataforma).
    public async Task<ResultadoRevocacion> RevocarAsync(string credencialCodigo, string attestationUid)
    {
        var falta = ConfiguracionFaltante();
        if (falta is not null) return new ResultadoRevocacion(false, null, falta);

        try
        {
            using var resp = await _http.PostAsync($"anclajes/{Uri.EscapeDataString(attestationUid)}/revocar", content: null);

            if (resp.IsSuccessStatusCode)
            {
                var r = await resp.Content.ReadFromJsonAsync<RespuestaRevocacion>(Json);
                return new ResultadoRevocacion(true, r?.TxHash, null);
            }

            var error = await LeerErrorAsync(resp);
            // Si ya estaba revocada en la red, el resultado que se busca ya se cumple.
            if (error?.Codigo == "ya_revocada")
                return new ResultadoRevocacion(true, null, "La atestación ya estaba revocada en la red.");

            return new ResultadoRevocacion(false, null, Explicar(resp, error));
        }
        catch (Exception ex) when (EsFalloDeRed(ex))
        {
            return new ResultadoRevocacion(false, null, "No se pudo contactar el puente de Stellar: " + ex.Message);
        }
    }

    // E27 y E29: lee lo que quedó en la red. Devuelve null si no existe, si la red no responde,
    // o si NO la firmó nuestra cuenta con nuestro esquema (en Attest cualquiera puede atestar).
    public async Task<EvidenciaEnRed?> ConsultarAsync(string attestationUid)
    {
        if (ConfiguracionFaltante() is not null) return null;

        try
        {
            using var resp = await _http.GetAsync($"anclajes/{Uri.EscapeDataString(attestationUid)}");
            if (!resp.IsSuccessStatusCode) return null;   // 404 = no existe; otros códigos = red caída

            var r = await resp.Content.ReadFromJsonAsync<RespuestaConsulta>(Json);
            if (r is null || string.IsNullOrWhiteSpace(r.Sha256)) return null;

            var esNuestra =
                string.Equals(r.Attester, _opciones.DireccionEmisora, StringComparison.Ordinal)
                && string.Equals(r.SchemaUid, _opciones.SchemaUid, StringComparison.OrdinalIgnoreCase);
            if (!esNuestra) return null;

            return r.Estado switch
            {
                "vigente" => new EvidenciaEnRed(attestationUid, r.Sha256, false),
                "revocada" => new EvidenciaEnRed(attestationUid, r.Sha256, true),
                _ => null,   // "expirada": el contrato no entrega datos
            };
        }
        catch (Exception ex) when (EsFalloDeRed(ex))
        {
            return null;
        }
    }

    // ── Utilidades ──────────────────────────────────────────────────────────

    // Si falta configuración, no se intenta nada: el anclaje queda FALLIDO con un mensaje claro.
    private string? ConfiguracionFaltante()
    {
        if (string.IsNullOrWhiteSpace(_opciones.ApiKey))
            return "Stellar sin configurar: falta Stellar__ApiKey.";
        if (string.IsNullOrWhiteSpace(_opciones.DireccionEmisora))
            return "Stellar sin configurar: falta Stellar__DireccionEmisora.";
        if (string.IsNullOrWhiteSpace(_opciones.SchemaUid))
            return "Stellar sin configurar: falta Stellar__SchemaUid.";
        return null;
    }

    private static bool EsFalloDeRed(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException;

    private static async Task<RespuestaError?> LeerErrorAsync(HttpResponseMessage resp)
    {
        try { return await resp.Content.ReadFromJsonAsync<RespuestaError>(Json); }
        catch (Exception ex) when (ex is JsonException or NotSupportedException) { return null; }   // el cuerpo no era JSON
    }

    private static string Explicar(HttpResponseMessage resp, RespuestaError? error)
    {
        var detalle = error?.Mensaje ?? $"respuesta HTTP {(int)resp.StatusCode}";
        return resp.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
            ? $"La red Stellar o el puente no están disponibles: {detalle}"
            : $"El puente rechazó la operación ({error?.Codigo ?? "sin_codigo"}): {detalle}";
    }
}