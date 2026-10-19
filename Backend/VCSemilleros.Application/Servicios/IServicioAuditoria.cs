using VCSemilleros.Domain.Modelos;               // EstadoAnclaje.

namespace VCSemilleros.Application.Servicios;

// Puerto de ENTRADA del caso de uso "auditar" (RF15; endpoints E27, E28 y E29).
// Solo LECTURA. Ninguna respuesta incluye nombre, correo ni otros datos personales: solo identificadores.
public interface IServicioAuditoria
{
    // E27: evidencia de anclaje de una credencial, con la comprobación de si existe en la red.
    // 404 si la credencial no existe o no tiene anclaje registrado.
    Task<EvidenciaAnclaje> ObtenerAnclajeAsync(string codigo);

    // E28: línea de tiempo DERIVADA (propuesta P3) de columnas existentes, ordenada por fecha.
    // 404 si la credencial no existe.
    Task<List<EventoLineaTiempo>> ObtenerLineaTiempoAsync(string codigo);

    // E29: contrasta el hash de evidencia guardado en la BD con el que tiene la red.
    // 404 si la credencial no existe o no tiene anclaje registrado.
    Task<IntegridadEvidencia> ObtenerIntegridadAsync(string codigo);
}

// "record": tipo de C# para transportar datos, con propiedades de solo lectura.

// EvidenciaAnclaje: lo que devuelve E27.
//   ExisteEnRed: true si la red confirma la atestación; false si no está, no hay UID o la red no responde.
public record EvidenciaAnclaje(
    string Codigo,
    EstadoAnclaje Estado,
    string EvidenciaHash,
    string? TxHash,
    string? AttestationUid,
    DateTime FechaSolicitud,
    DateTime? FechaExpiracion,
    bool ExisteEnRed);

// EventoLineaTiempo: un evento de E28. Los eventos posibles son EVALUADA, EMITIDA, ANCLAJE_SOLICITADO y REVOCADA.
//   ActorId: quién lo hizo (null si no hay actor). Motivo: solo lo trae la revocación.
public record EventoLineaTiempo(string Evento, DateTime Fecha, int? ActorId, string? Motivo);

// IntegridadEvidencia: lo que devuelve E29.
//   EvidenciaHashRed: null si la red no tiene la evidencia o no respondió.
//   Coincide: true solo si la red respondió y el hash es idéntico al de la BD.
public record IntegridadEvidencia(string Codigo, string EvidenciaHashBD, string? EvidenciaHashRed, bool Coincide);