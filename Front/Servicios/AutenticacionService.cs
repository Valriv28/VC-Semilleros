
using System.Net;
using System.Net.Http.Json;

namespace Front.Servicios;

public sealed class AutenticacionService
{
    private readonly HttpClient _http;
    private readonly SesionUsuario _sesion;

    public AutenticacionService(
        HttpClient http,
        SesionUsuario sesion)
    {
        _http = http;
        _sesion = sesion;
    }

    public async Task<(bool Exito, string Mensaje)> IngresarAsync(
        string correo,
        string contrasena,
        CancellationToken ct = default)
    {
        try
        {
            using var respuesta = await _http.PostAsJsonAsync(
                "api/autenticacion/ingresar",
                new PeticionIngreso(correo, contrasena),
                ct);

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                return (false, "Correo o contraseña incorrectos.");

            if (!respuesta.IsSuccessStatusCode)
                return (false,
                    $"No fue posible ingresar. Código HTTP: {(int)respuesta.StatusCode}.");

            var resultado =
                await respuesta.Content.ReadFromJsonAsync<RespuestaIngreso>(
                    cancellationToken: ct);

            if (resultado is null ||
                string.IsNullOrWhiteSpace(resultado.Token))
            {
                return (false,
                    "La API no devolvió un token de acceso válido.");
            }

            _sesion.IniciarSesion(resultado);

            return (true, "Inicio de sesión correcto.");
        }
        catch (HttpRequestException)
        {
            return (false,
                "No se pudo conectar con el backend. Comprueba que la API esté ejecutándose.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return (false, "La API tardó demasiado en responder.");
        }
    }

    private sealed record PeticionIngreso(
        string Correo,
        string Contrasena);
}
