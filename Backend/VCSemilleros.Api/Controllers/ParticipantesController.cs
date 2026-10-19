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

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de lo que se consulta o se hace sobre UN participante: su resumen de asistencia (E15)
// y su evaluación contra un criterio (E17).
// Art. 2: el resumen (evidencia) y la evaluación (decisión de que la evidencia cumple) son cosas
// distintas, por eso cada una usa su propio servicio.
//
// [ApiController]: valida el body contra la petición ANTES de entrar al método (422 con la lista de errores).
// [Route("api/participantes")]: toda ruta de este controlador empieza con /api/participantes.
// [Authorize]: por defecto TODO exige un token JWT válido (sin token → 401); cada método puede afinar el rol.
[ApiController]
[Route("api/participantes")]
[Authorize]
public class ParticipantesController : ControllerBase
{
	// Los servicios que necesita. "readonly": se asignan una sola vez, en el constructor.
	private readonly IServicioAsistencia _servicioAsistencia;
	private readonly IServicioEvaluacion _servicioEvaluacion;

	// Constructor: ASP.NET Core inyecta aquí las implementaciones registradas en Program.cs.
	public ParticipantesController(IServicioAsistencia servicioAsistencia, IServicioEvaluacion servicioEvaluacion)
	{
		_servicioAsistencia = servicioAsistencia;
		_servicioEvaluacion = servicioEvaluacion;
	}

	// ------------------------------------------------------------------
	// E15 · GET /api/participantes/{participanteId}/asistencia — Resumen de asistencia del participante
	// Porcentaje = presentes ÷ sesiones del periodo × 100, con 2 decimales.
	// El contrato no limita el rol: basta con haber ingresado.
	// ------------------------------------------------------------------
	[HttpGet("{participanteId:int}/asistencia")]
	public async Task<IActionResult> ObtenerResumen(int participanteId)
	{
		try
		{
			// 404 si el participante no existe; 400 si su periodo no tiene sesiones (no hay evidencia
			// que resumir). Ambos los decide el servicio.
			var resumen = await _servicioAsistencia.ObtenerResumenAsync(participanteId);

			// ResumenAsistencia ya tiene la forma exacta del contrato:
			// { participanteId, periodoId, sesiones, presentes, porcentaje }. Se devuelve tal cual.
			return Ok(resumen);
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

    // E17 · POST /api/participantes/{participanteId}/evaluaciones — evaluar contra un criterio.
    // Rol: DOCENTE o COORDINADOR. CAMBIO: si quien llama es COORDINADOR, el "definidoPor" del body debe ser
    // la MISMA persona que inició sesión (así nadie define un criterio "como" otro coordinador).
    // Si quien llama es DOCENTE no se compara: el docente evalúa con el criterio que defina un coordinador,
    // y el servicio ya exige que "definidoPor" sea un usuario con rol COORDINADOR (400 si no).
    [HttpPost("{participanteId:int}/evaluaciones")]
    [Authorize(Roles = "DOCENTE,COORDINADOR")]
    public async Task<IActionResult> Evaluar(int participanteId, [FromBody] EvaluacionCrear peticion)
    {
        // IsInRole("COORDINADOR") mira el claim "rol" del token. Si es coordinador y el id del body no es el suyo, 403.
        if (User.IsInRole("COORDINADOR") && peticion.DefinidoPor != User.ObtenerId())
        {
            return StatusCode(403, new RespuestaError(
                403, "Acceso denegado.", "Un coordinador solo puede definir el criterio a su propio nombre: definidoPor debe ser el id de la persona que inició sesión."));
        }

        try
        {
            // Si la petición no trae criterioVersion, se usa 1 (contrato E17).
            var criterioVersion = peticion.CriterioVersion ?? 1;

            var evaluacion = await _servicioEvaluacion.EvaluarAsync(
                participanteId,
                peticion.MinAsistenciaPct!.Value,
                peticion.DefinidoPor!.Value,
                criterioVersion);

            return Ok(new RespuestaEvaluacionCreada(
                200,
                "Evaluación registrada exitosamente.",
                evaluacion.Id,
                evaluacion.PctAsistencia,
                evaluacion.Cumple));
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