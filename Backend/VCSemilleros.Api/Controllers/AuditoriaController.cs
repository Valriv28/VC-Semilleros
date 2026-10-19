// Importa [Authorize]: la anotación que exige un token JWT válido.
using Microsoft.AspNetCore.Authorization;
// Importa ControllerBase, [ApiController], [HttpGet], [Route], IActionResult...
using Microsoft.AspNetCore.Mvc;
// Importa nuestros DTOs: los sobres de respuesta.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) de Application. El controlador solo conoce INTERFACES (Art. 4).
using VCSemilleros.Application.Servicios;
// Importa NoEncontradoExcepcion, que el controlador traduce a 404.
using VCSemilleros.Domain.Excepciones;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de la AUDITORÍA de una credencial (E27, E28 y E29). Es SOLO LECTURA: no hay ningún
// POST, PATCH ni DELETE aquí (propuesta P5: tampoco hay DELETE en todo el sistema).
// Ninguna respuesta incluye nombre, correo ni otros datos personales: solo identificadores
// (así lo garantiza el servicio y lo pide el contrato §12).
//
// ROL: el contrato dice "auditor", pero el enum rol_usuario NO tiene ese rol (A2). Decisión acordada:
// no hay endpoint propio de auditor; E27 a E29 quedan abiertos a CUALQUIER usuario que haya ingresado.
// Por eso solo lleva [Authorize] (token válido, sin rol específico).
//
// [ApiController]: activa el comportamiento de API.
// [Route("api/auditoria")]: toda ruta de este controlador empieza con /api/auditoria.
[ApiController]
[Route("api/auditoria")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    // El servicio que necesita. "readonly": se asigna una sola vez, en el constructor.
    private readonly IServicioAuditoria _servicioAuditoria;

    // Constructor: ASP.NET Core inyecta aquí la implementación registrada en Program.cs.
    public AuditoriaController(IServicioAuditoria servicioAuditoria)
    {
        _servicioAuditoria = servicioAuditoria;
    }

    // ------------------------------------------------------------------
    // E27 · GET /api/auditoria/{codigo}/anclaje — Evidencia de anclaje
    // ------------------------------------------------------------------
    [HttpGet("{codigo}/anclaje")]
    public async Task<IActionResult> ObtenerAnclaje(string codigo)
    {
        try
        {
            // 404 si la credencial no existe o no tiene anclaje registrado.
            // "existeEnRed" es true solo si la red confirma la evidencia; false si no está o no respondió.
            var evidencia = await _servicioAuditoria.ObtenerAnclajeAsync(codigo);

            // EvidenciaAnclaje ya tiene la forma exacta del contrato E27: se devuelve tal cual.
            return Ok(evidencia);
        }
        catch (NoEncontradoExcepcion ex)
        {
            // Puede faltar la credencial O su anclaje, por eso el mensaje es el genérico.
            return StatusCode(404, new RespuestaError(404, "Recurso no encontrado.", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return StatusCode(400, new RespuestaError(400, "Parámetros inválidos.", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }

    // ------------------------------------------------------------------
    // E28 · GET /api/auditoria/{codigo}/linea-tiempo — Línea de tiempo (propuesta P3)
    // Eventos DERIVADOS de columnas existentes (EVALUADA, EMITIDA, ANCLAJE_SOLICITADO, REVOCADA),
    // ordenados por fecha. No hay tabla de eventos: el script la dejó comentada.
    // ------------------------------------------------------------------
    [HttpGet("{codigo}/linea-tiempo")]
    public async Task<IActionResult> ObtenerLineaTiempo(string codigo)
    {
        try
        {
            var eventos = await _servicioAuditoria.ObtenerLineaTiempoAsync(codigo);

            // Una credencial existente siempre tiene al menos el evento EVALUADA, pero se mantiene la
            // regla general de las listas del contrato: vacía → 204 sin cuerpo.
            if (eventos.Count == 0)
                return NoContent();

            // El servicio no recibe un límite (son como mucho 4 eventos): el sobre muestra el 1000
            // por defecto que fija el contrato para todas las listas.
            return Ok(new RespuestaLista<EventoLineaTiempo>("linea_tiempo", 1000, eventos.Count, eventos));
        }
        catch (NoEncontradoExcepcion ex)
        {
            return StatusCode(404, new RespuestaError(404, "Credencial no encontrada.", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return StatusCode(400, new RespuestaError(400, "Parámetros inválidos.", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }

    // ------------------------------------------------------------------
    // E29 · GET /api/auditoria/{codigo}/integridad — Integridad de la evidencia
    // Contrasta el hash de evidencia guardado en la BD con el que tiene la red.
    // ------------------------------------------------------------------
    [HttpGet("{codigo}/integridad")]
    public async Task<IActionResult> ObtenerIntegridad(string codigo)
    {
        try
        {
            // "coincide" es true solo si la red respondió Y el hash es idéntico al de la BD.
            // Si la red no tiene la evidencia o no respondió, "evidenciaHashRed" sale null y "coincide" false:
            // eso sigue siendo un 200, porque la comprobación se ejecutó.
            var integridad = await _servicioAuditoria.ObtenerIntegridadAsync(codigo);

            // IntegridadEvidencia ya tiene la forma exacta del contrato E29: se devuelve tal cual.
            return Ok(integridad);
        }
        catch (NoEncontradoExcepcion ex)
        {
            return StatusCode(404, new RespuestaError(404, "Recurso no encontrado.", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return StatusCode(400, new RespuestaError(400, "Parámetros inválidos.", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }
}