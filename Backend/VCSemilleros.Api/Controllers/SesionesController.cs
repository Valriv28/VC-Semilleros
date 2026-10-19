// Importa [Authorize]: la anotación que exige un token JWT válido (y, si se indica, un rol).
using Microsoft.AspNetCore.Authorization;
// Importa ControllerBase, [ApiController], [HttpGet], [Route], [FromBody], IActionResult...
using Microsoft.AspNetCore.Mvc;
// Importa nuestros DTOs: las peticiones de entrada y los sobres de respuesta.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) de Application. El controlador solo conoce INTERFACES (Art. 4).
using VCSemilleros.Application.Servicios;
// Importa NoEncontradoExcepcion, que el controlador traduce a 404.
using VCSemilleros.Domain.Excepciones;
// Importa los modelos del dominio (Asistencia) para armar la lista que pide el servicio.
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de la asistencia de UNA sesión (E13 y E14).
// Recuerda el Art. 2: registrar la asistencia es guardar EVIDENCIA PRIMARIA; no es evaluar
// ni declarar elegibilidad ni certificar nada.
//
// [ApiController]: valida el body contra la petición ANTES de entrar al método (422 con la lista de errores).
// [Route("api/sesiones")]: toda ruta de este controlador empieza con /api/sesiones.
// [Authorize]: por defecto TODO exige un token JWT válido (sin token → 401); cada método puede afinar el rol.
[ApiController]
[Route("api/sesiones")]
[Authorize]
public class SesionesController : ControllerBase
{
    // El servicio que necesita. "readonly": se asigna una sola vez, en el constructor.
    private readonly IServicioAsistencia _servicioAsistencia;

    // Constructor: ASP.NET Core inyecta aquí la implementación registrada en Program.cs.
    public SesionesController(IServicioAsistencia servicioAsistencia)
    {
        _servicioAsistencia = servicioAsistencia;
    }

    // ------------------------------------------------------------------
    // E13 · POST /api/sesiones/{sesionId}/asistencias — Registrar la asistencia de TODA la sesión
    // Rol: docente
    // ------------------------------------------------------------------
    [HttpPost("{sesionId:int}/asistencias")]
    [Authorize(Roles = "DOCENTE")]
    public async Task<IActionResult> RegistrarAsistencia(int sesionId, [FromBody] AsistenciaRegistrar peticion)
    {
        try
        {
            // Convierte cada ítem de la petición (DTO de entrada) en un modelo del dominio.
            // La petición y el modelo son tipos distintos a propósito (Art. 9): la petición vive en la Api
            // y el modelo en Domain, que no puede conocer a la Api.
            // ".Select(...)" transforma cada elemento de la lista; ".ToList()" arma la lista resultante.
            // Los ".Value" son seguros: [Required] ya garantizó que ningún campo viene null.
            // El SesionId queda en 0 aquí: el servicio lo completa con el de la ruta.
            var asistencias = peticion.Asistencias!
                .Select(item => new Asistencia
                {
                    ParticipanteId = item.ParticipanteId!.Value,
                    Presente = item.Presente!.Value
                })
                .ToList();

            // El servicio comprueba: sesión y participantes existen (404) y cada participante pertenece al
            // periodo de la sesión (400). Guarda todo o nada: si una asistencia está repetida, la BD viola
            // UNIQUE, no se guarda ninguna fila y sale como 500.
            var filas = await _servicioAsistencia.RegistrarAsistenciaAsync(sesionId, asistencias);

            return Ok(new RespuestaFilas(200, "Asistencia registrada exitosamente.", filas));
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
            // Aquí cae el UNIQUE duplicado (NpgsqlException): el mensaje del motor va en "detalle".
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }

    // ------------------------------------------------------------------
    // E14 · GET /api/sesiones/{sesionId}/asistencias[?limite=N] — Consultar la asistencia por sesión
    // El contrato no limita el rol: basta con haber ingresado.
    // ------------------------------------------------------------------
    [HttpGet("{sesionId:int}/asistencias")]
    public async Task<IActionResult> ListarAsistencias(int sesionId, [FromQuery] int limite = 1000)
    {
        try
        {
            // Si la sesión no existe → 404; si limite <= 0 → 400 (ambos los decide el servicio).
            var asistencias = await _servicioAsistencia.ListarAsistenciasAsync(sesionId, limite);

            // Lista vacía → 204 sin cuerpo (contrato §0).
            if (asistencias.Count == 0)
                return NoContent();

            return Ok(new RespuestaLista<Asistencia>("asistencia", limite, asistencias.Count, asistencias));
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