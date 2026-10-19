using VCSemilleros.Domain.Excepciones;           // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;               // Credencial, AnclajeStellar.
using VCSemilleros.Domain.Repositorios;          // Los puertos de solo lectura que usa la auditoría.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: auditar (RF15; E27, E28, E29). Solo LEE.
// Nunca devuelve nombres ni correos: únicamente identificadores (el contrato lo exige).
public class ServicioAuditoria : IServicioAuditoria
{
    private readonly IRepositorioCredencial _repositorioCredencial;
    private readonly IRepositorioEvaluacion _repositorioEvaluacion;
    private readonly IRepositorioAnclaje _repositorioAnclaje;
    private readonly IClienteStellar _clienteStellar;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioAuditoria(
        IRepositorioCredencial repositorioCredencial,
        IRepositorioEvaluacion repositorioEvaluacion,
        IRepositorioAnclaje repositorioAnclaje,
        IClienteStellar clienteStellar)
    {
        _repositorioCredencial = repositorioCredencial;
        _repositorioEvaluacion = repositorioEvaluacion;
        _repositorioAnclaje = repositorioAnclaje;
        _clienteStellar = clienteStellar;
    }

    // E27: evidencia de anclaje y si existe en la red.
    public async Task<EvidenciaAnclaje> ObtenerAnclajeAsync(string codigo)
    {
        var anclaje = await ObtenerAnclajeODarErrorAsync(codigo);

        var enRed = await ConsultarEnRedAsync(anclaje);

        return new EvidenciaAnclaje(
            codigo, anclaje.Estado, anclaje.EvidenciaHash, anclaje.TxHash, anclaje.AttestationUid,
            anclaje.FechaSolicitud, anclaje.FechaExpiracion,
            ExisteEnRed: enRed is not null);
    }

    // E28: línea de tiempo derivada de columnas existentes (propuesta P3).
    public async Task<List<EventoLineaTiempo>> ObtenerLineaTiempoAsync(string codigo)
    {
        var credencial = await ObtenerCredencialODarErrorAsync(codigo);

        var eventos = new List<EventoLineaTiempo>();

        // EVALUADA: la fecha y el coordinador que definió el criterio.
        var evaluacion = await _repositorioEvaluacion.ObtenerPorIdAsync(credencial.EvaluacionId);
        if (evaluacion is not null)
            eventos.Add(new EventoLineaTiempo("EVALUADA", evaluacion.Fecha, evaluacion.DefinidoPor, null));

        // EMITIDA: la fecha de emisión y el coordinador que declaró la elegibilidad.
        eventos.Add(new EventoLineaTiempo("EMITIDA", credencial.FechaEmision, evaluacion?.ElegibleDeclaradoPor, null));

        // ANCLAJE_SOLICITADO: sin actor (lo hace el sistema).
        var anclaje = await _repositorioAnclaje.ObtenerPorCredencialAsync(codigo);
        if (anclaje is not null)
            eventos.Add(new EventoLineaTiempo("ANCLAJE_SOLICITADO", anclaje.FechaSolicitud, null, null));

        // REVOCADA: solo si tiene fecha de revocación, con quién la hizo y el motivo.
        if (credencial.FechaRevocacion is not null)
            eventos.Add(new EventoLineaTiempo("REVOCADA", credencial.FechaRevocacion.Value,
                credencial.RevocadaPor, credencial.MotivoRevocacion));

        // OrderBy es ESTABLE: si dos eventos tienen la misma fecha, conservan el orden en que se agregaron.
        return eventos.OrderBy(e => e.Fecha).ToList();
    }

    // E29: contrasta el hash de la BD con el de la red.
    public async Task<IntegridadEvidencia> ObtenerIntegridadAsync(string codigo)
    {
        var anclaje = await ObtenerAnclajeODarErrorAsync(codigo);

        var enRed = await ConsultarEnRedAsync(anclaje);

        // Solo "coincide" si la red respondió y el hash es idéntico (sin distinguir mayúsculas).
        var coincide = enRed is not null
            && string.Equals(enRed.EvidenciaHash, anclaje.EvidenciaHash, StringComparison.OrdinalIgnoreCase);

        return new IntegridadEvidencia(codigo, anclaje.EvidenciaHash, enRed?.EvidenciaHash, coincide);
    }

    // ---------- Auxiliares privados ----------

    // La credencial debe existir (404).
    private async Task<Credencial> ObtenerCredencialODarErrorAsync(string codigo)
    {
        var credencial = await _repositorioCredencial.ObtenerPorCodigoAsync(codigo);
        if (credencial is null)
            throw new NoEncontradoExcepcion($"No existe una credencial con código = {codigo}");

        return credencial;
    }

    // La credencial debe existir y tener un anclaje registrado (404 en ambos casos).
    private async Task<AnclajeStellar> ObtenerAnclajeODarErrorAsync(string codigo)
    {
        await ObtenerCredencialODarErrorAsync(codigo);

        var anclaje = await _repositorioAnclaje.ObtenerPorCredencialAsync(codigo);
        if (anclaje is null)
            throw new NoEncontradoExcepcion($"La credencial {codigo} no tiene un anclaje registrado.");

        return anclaje;
    }

    // Consulta la red por el attestationUid. Devuelve null si no hay UID (el anclaje falló o está pendiente),
    // si la red no lo encuentra o si no responde: en los tres casos "no existe en la red".
    private async Task<EvidenciaEnRed?> ConsultarEnRedAsync(AnclajeStellar anclaje)
    {
        if (string.IsNullOrWhiteSpace(anclaje.AttestationUid))
            return null;

        try
        {
            return await _clienteStellar.ConsultarAsync(anclaje.AttestationUid);
        }
        catch
        {
            // Una auditoría no debe fallar porque la red esté caída: se responde "no existe en la red".
            return null;
        }
    }
}