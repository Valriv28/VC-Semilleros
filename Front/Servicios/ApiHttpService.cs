
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Front.Servicios;

public sealed class ApiHttpService
{
    private readonly HttpClient _http;
    private readonly SesionUsuario _sesion;

    public ApiHttpService(
        HttpClient http,
        SesionUsuario sesion)
    {
        _http = http;
        _sesion = sesion;
    }

    public async Task<T?> ObtenerAsync<T>(
        string ruta,
        CancellationToken ct = default)
    {
        using var request = CrearPeticion(HttpMethod.Get, ruta);

        using var respuesta = await _http.SendAsync(request, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Error HTTP {(int)respuesta.StatusCode} ({respuesta.StatusCode}) en {request.RequestUri}. Respuesta de la API: {detalle}");
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NoContent)
            return default;

        return await respuesta.Content.ReadFromJsonAsync<T>(
            cancellationToken: ct);
    }

    public async Task<TRespuesta?> EnviarAsync<TPeticion, TRespuesta>(
        string ruta,
        TPeticion datos,
        CancellationToken ct = default)
    {
        using var request = CrearPeticion(HttpMethod.Post, ruta);
        request.Content = JsonContent.Create(datos);

        using var respuesta = await _http.SendAsync(request, ct);

        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);

            throw new HttpRequestException(
                $"Error HTTP {(int)respuesta.StatusCode} " +
                $"({respuesta.StatusCode}) en {request.RequestUri}. " +
                $"Respuesta de la API: {detalle}");
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NoContent)
            return default;

        return await respuesta.Content.ReadFromJsonAsync<TRespuesta>(
            cancellationToken: ct);
    }

    public async Task<TRespuesta?> ActualizarParcialAsync<TPeticion, TRespuesta>(
    string ruta,
    TPeticion datos,
    CancellationToken ct = default)
    {
        using var request = CrearPeticion(HttpMethod.Patch, ruta);
        request.Content = JsonContent.Create(datos);

        using var respuesta = await _http.SendAsync(request, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Error HTTP {(int)respuesta.StatusCode} ({respuesta.StatusCode}) en {request.RequestUri}. Respuesta de la API: {detalle}");
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NoContent)
            return default;

        return await respuesta.Content.ReadFromJsonAsync<TRespuesta>(
            cancellationToken: ct);
    }


    public async Task<TRespuesta?> ReemplazarAsync<TPeticion, TRespuesta>(
        string ruta,
        TPeticion datos,
        CancellationToken ct = default)
    {
        using var request = CrearPeticion(HttpMethod.Put, ruta);
        request.Content = JsonContent.Create(datos);
        using var respuesta = await _http.SendAsync(request, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Error HTTP {(int)respuesta.StatusCode} ({respuesta.StatusCode}) en {request.RequestUri}. Respuesta de la API: {detalle}");
        }
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NoContent)
            return default;
        return await respuesta.Content.ReadFromJsonAsync<TRespuesta>(cancellationToken: ct);
    }

    private HttpRequestMessage CrearPeticion(
        HttpMethod metodo,
        string ruta)
    {
        var request = new HttpRequestMessage(metodo, ruta);

        if (_sesion.EstaAutenticado)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", _sesion.Token);
        }

        return request;
    }


}
