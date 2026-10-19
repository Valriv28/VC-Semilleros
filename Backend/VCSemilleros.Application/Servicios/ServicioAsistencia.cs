using VCSemilleros.Domain.Excepciones;          // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;              // Sesion, Asistencia.
using VCSemilleros.Domain.Repositorios;         // Los puertos de periodo, sesión, participante y asistencia.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: sesiones y asistencia (RF7). Incluye el resumen de asistencia de E15.
// Art. 2: aquí solo se registra y resume la EVIDENCIA PRIMARIA; no se decide elegibilidad ni se certifica.
public class ServicioAsistencia : IServicioAsistencia
{
    private readonly IRepositorioPeriodo _repositorioPeriodo;
    private readonly IRepositorioSesion _repositorioSesion;
    private readonly IRepositorioParticipante _repositorioParticipante;
    private readonly IRepositorioAsistencia _repositorioAsistencia;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioAsistencia(
        IRepositorioPeriodo repositorioPeriodo,
        IRepositorioSesion repositorioSesion,
        IRepositorioParticipante repositorioParticipante,
        IRepositorioAsistencia repositorioAsistencia)
    {
        _repositorioPeriodo = repositorioPeriodo;
        _repositorioSesion = repositorioSesion;
        _repositorioParticipante = repositorioParticipante;
        _repositorioAsistencia = repositorioAsistencia;
    }

    // E11: lista las sesiones de un periodo.
    public async Task<List<Sesion>> ListarSesionesAsync(int periodoId, int limite)
    {
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        // El periodo debe existir (404).
        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(periodoId);
        if (periodo is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}");

        return await _repositorioSesion.ObtenerPorPeriodoAsync(periodoId, limite);
    }

    // E12: registra una sesión.
    public async Task<int> RegistrarSesionAsync(int periodoId, DateOnly fecha, string? descripcion)
    {
        // El periodo debe existir (404).
        var periodo = await _repositorioPeriodo.ObtenerPorIdAsync(periodoId);
        if (periodo is null)
            throw new NoEncontradoExcepcion($"No existe un periodo con id = {periodoId}");

        var sesion = new Sesion
        {
            PeriodoId = periodoId,
            Fecha = fecha,
            Descripcion = descripcion
        };

        return await _repositorioSesion.CrearAsync(sesion);
    }

    // E13: registra la asistencia de toda la sesión en una sola operación atómica.
    public async Task<int> RegistrarAsistenciaAsync(int sesionId, List<Asistencia> asistencias)
    {
        // Regla de respaldo: la petición ya exige una lista no vacía (422), pero el servicio no
        // debe depender de que alguien más haya validado antes (400).
        if (asistencias.Count == 0)
            throw new ArgumentException("Debe registrarse al menos una asistencia.");

        // 1) La sesión debe existir (404).
        var sesion = await _repositorioSesion.ObtenerPorIdAsync(sesionId);
        if (sesion is null)
            throw new NoEncontradoExcepcion($"No existe una sesión con id = {sesionId}");

        // 2) Lista nueva, ya con el SesionId completo, que es lo que se va a guardar.
        var aGuardar = new List<Asistencia>();

        // "foreach" recorre cada elemento de la lista recibida.
        foreach (var item in asistencias)
        {
            // Cada participante debe existir (404).
            var participante = await _repositorioParticipante.ObtenerPorIdAsync(item.ParticipanteId);
            if (participante is null)
                throw new NoEncontradoExcepcion($"No existe un participante con id = {item.ParticipanteId}");

            // Regla de negocio: el participante debe pertenecer al periodo de la sesión (400).
            if (participante.PeriodoId != sesion.PeriodoId)
                throw new ArgumentException(
                    $"El participante {item.ParticipanteId} no pertenece al periodo de la sesión {sesionId}.");

            aGuardar.Add(new Asistencia
            {
                SesionId = sesionId,
                ParticipanteId = item.ParticipanteId,
                Presente = item.Presente
            });
        }

        // 3) Guarda todo o nada (transacción en el repositorio). Si hay una asistencia repetida,
        // la BD viola UNIQUE y la excepción sube hasta el controlador como 500.
        return await _repositorioAsistencia.CrearVariasAsync(aGuardar);
    }

    // E14: lista las asistencias de una sesión.
    public async Task<List<Asistencia>> ListarAsistenciasAsync(int sesionId, int limite)
    {
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        // La sesión debe existir (404).
        var sesion = await _repositorioSesion.ObtenerPorIdAsync(sesionId);
        if (sesion is null)
            throw new NoEncontradoExcepcion($"No existe una sesión con id = {sesionId}");

        return await _repositorioAsistencia.ObtenerPorSesionAsync(sesionId, limite);
    }

    // E15: resumen de asistencia de un participante.
    public async Task<ResumenAsistencia> ObtenerResumenAsync(int participanteId)
    {
        // 1) El participante debe existir (404).
        var participante = await _repositorioParticipante.ObtenerPorIdAsync(participanteId);
        if (participante is null)
            throw new NoEncontradoExcepcion($"No existe un participante con id = {participanteId}");

        // 2) Denominador: sesiones del periodo del participante.
        var sesiones = await _repositorioSesion.ContarPorPeriodoAsync(participante.PeriodoId);

        // Regla de negocio: sin sesiones no hay evidencia que resumir; además evita dividir entre cero (400).
        if (sesiones == 0)
            throw new ArgumentException("El periodo no tiene sesiones: no hay evidencia de asistencia que resumir.");

        // 3) Numerador: sesiones en las que estuvo presente.
        var presentes = await _repositorioAsistencia.ContarPresentesAsync(participanteId);

        // 4) Porcentaje con 2 decimales. La "m" marca el 100 como decimal (si no, la división sería entera
        // y 1/2 daría 0). Math.Round redondea a 2 decimales.
        // CAMBIO: "+ 0.00m" fuerza que el número SIEMPRE tenga 2 decimales. Math.Round(200m / 2, 2) queda como
        // 100 y el JSON saldría "100"; sumar 0.00m lo deja como 100.00, que es lo que muestra el contrato (E15 y E17).
        // El valor numérico es el mismo, así que las pruebas con Assert.Equal(100m, ...) siguen pasando.
        var porcentaje = Math.Round(presentes * 100m / sesiones, 2) + 0.00m;

        return new ResumenAsistencia(participanteId, participante.PeriodoId, sesiones, presentes, porcentaje);
    }
}