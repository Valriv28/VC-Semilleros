namespace Front.Servicios;

public sealed class ParticipantesService
{
    private readonly ApiHttpService _api;

    public ParticipantesService(ApiHttpService api)
    {
        _api = api;
    }

    public Task<T?> ObtenerResumenAsistenciaAsync<T>(
        int participanteId,
        CancellationToken ct = default)
        => _api.ObtenerAsync<T>(
            $"api/participantes/{participanteId}/asistencia", ct);

    public Task<TRespuesta?> EvaluarAsync<TPeticion, TRespuesta>(
        int participanteId,
        TPeticion datos,
        CancellationToken ct = default)
        => _api.EnviarAsync<TPeticion, TRespuesta>(
            $"api/participantes/{participanteId}/evaluaciones",
            datos, ct);
}
