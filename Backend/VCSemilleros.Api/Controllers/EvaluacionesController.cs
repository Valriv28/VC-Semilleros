// Importa [Authorize]: la anotación que exige un token JWT válido (y, si se indica, un rol).
using Microsoft.AspNetCore.Authorization;
// Importa ControllerBase, [ApiController], [HttpPatch], [HttpPost], [Route], [FromBody], IActionResult...
using Microsoft.AspNetCore.Mvc;
// Importa nuestros DTOs: las peticiones de entrada y los sobres de respuesta.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) de Application. El controlador solo conoce INTERFACES (Art. 4).
using VCSemilleros.Application.Servicios;
// Importa NoEncontradoExcepcion, que el controlador traduce a 404.
using VCSemilleros.Domain.Excepciones;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de las decisiones que siguen a una evaluación:
//   declarar la ELEGIBILIDAD (E19) y AUTORIZAR/EMITIR la credencial (E20).
// Art. 2: son dos decisiones distintas y por eso usan dos servicios distintos.
// Declarar elegible NO emite la credencial; emitir exige que la elegibilidad ya esté declarada.
//
// [ApiController]: valida el body contra la petición ANTES de entrar al método (422 con la lista de errores).
// [Route("api/evaluaciones")]: toda ruta de este controlador empieza con /api/evaluaciones.
// [Authorize]: por defecto TODO exige un token JWT válido (sin token → 401); cada método puede afinar el rol.
[ApiController]
[Route("api/evaluaciones")]
[Authorize]
public class EvaluacionesController : ControllerBase
{
    // Los servicios que necesita. "readonly": se asignan una sola vez, en el constructor.
    private readonly IServicioEvaluacion _servicioEvaluacion;
    private readonly IServicioEmision _servicioEmision;

    // Constructor: ASP.NET Core inyecta aquí las implementaciones registradas en Program.cs.
    public EvaluacionesController(IServicioEvaluacion servicioEvaluacion, IServicioEmision servicioEmision)
    {
        _servicioEvaluacion = servicioEvaluacion;
        _servicioEmision = servicioEmision;
    }

    // E19 · PATCH /api/evaluaciones/{id}/elegibilidad — un coordinador declara la elegibilidad para emisión.
    // Rol: COORDINADOR. CAMBIO: además del rol, el "declaradoPor" del body debe ser la MISMA persona que
    // inició sesión. Si no, un coordinador podría declarar la elegibilidad "como" otro y figurar él como
    // emisor de la credencial.
    [HttpPatch("{id:int}/elegibilidad")]
    [Authorize(Roles = "COORDINADOR")]
    public async Task<IActionResult> DeclararElegibilidad(int id, [FromBody] EligibilidadDeclarar peticion)
    {
        // User es la persona identificada por el token JWT; ObtenerId() (UsuarioActual.cs) lee su id.
        // Se compara ANTES de llamar al servicio: si no coincide, no se toca nada. Responde 403.
        if (peticion.DeclaradoPor != User.ObtenerId())
        {
            return StatusCode(403, new RespuestaError(
                403, "Acceso denegado.", "El campo declaradoPor debe ser el id de la persona que inició sesión."));
        }

        try
        {
            var filas = await _servicioEvaluacion.DeclararElegibilidadAsync(id, peticion.DeclaradoPor!.Value);
            return Ok(new RespuestaFilas(200, "Elegibilidad declarada exitosamente.", filas));
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

    // ------------------------------------------------------------------
    // E20 · POST /api/evaluaciones/{id}/emision — Emitir la credencial
    // Rol: coordinador (emisor autorizado)
    // Sin body: toda la información sale de la BD (evaluación, titular, semillero, periodo).
    // ------------------------------------------------------------------
    [HttpPost("{id:int}/emision")]
    [Authorize(Roles = "COORDINADOR")]
    public async Task<IActionResult> Emitir(int id)
    {
        try
        {
            // Ejecuta el flujo de 9 pasos del plan §4.7: construir, validar estructura, firmar, serializar,
            // registrar la credencial, solicitar el anclaje y cerrar la emisión.
            //   404 si la evaluación no existe.
            //   400 si no tiene elegibilidad declarada, si ya tiene credencial o si la credencial
            //       construida está incompleta (en ese caso NO se firma).
            // Si Stellar falla, la credencial queda emitida igual con estadoAnclaje "FALLIDO" (P4):
            // eso NO es un error HTTP, por eso responde 200.
            var resultado = await _servicioEmision.EmitirAsync(id);

            return Ok(new RespuestaEmision(
                200,
                "Credencial emitida exitosamente.",
                resultado.Codigo,
                resultado.EstadoCredencial,
                resultado.EstadoAnclaje,
                resultado.TxHash,
                resultado.Vc));
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