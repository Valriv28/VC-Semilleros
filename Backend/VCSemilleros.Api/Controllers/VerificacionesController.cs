// Importa [AllowAnonymous]: la anotación que permite llamar a un endpoint SIN token.
using Microsoft.AspNetCore.Authorization;
// Importa ControllerBase, [ApiController], [HttpPost], [Route], [FromBody], IActionResult...
using Microsoft.AspNetCore.Mvc;
// Importa nuestros DTOs: las peticiones de entrada y los sobres de respuesta.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) de Application. El controlador solo conoce INTERFACES (Art. 4).
using VCSemilleros.Application.Servicios;
// Importa NoEncontradoExcepcion, que el controlador traduce a 404.
using VCSemilleros.Domain.Excepciones;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de la verificación de una credencial presentada (E26).
// Art. 2: verificar es un análisis técnico y de confianza SEPARADO de emitir. Esta operación
// NO escribe nada: ni en la BD ni en la red; solo lee y compara.
//
// [ApiController]: valida el body contra la petición ANTES de entrar al método (422 con la lista de errores).
// [Route("api/verificaciones")]: la ruta de este controlador es /api/verificaciones.
// [AllowAnonymous]: el rol del contrato es "público (verificador)": cualquier persona puede verificar una
//   credencial sin haber ingresado. Es lo que da sentido a la verificación: quien recibe la credencial
//   (un empleador, otra institución) no tiene cuenta en el sistema.
[ApiController]
[Route("api/verificaciones")]
[AllowAnonymous]
public class VerificacionesController : ControllerBase
{
    // El servicio que necesita. "readonly": se asigna una sola vez, en el constructor.
    private readonly IServicioVerificacion _servicioVerificacion;

    // Constructor: ASP.NET Core inyecta aquí la implementación registrada en Program.cs.
    public VerificacionesController(IServicioVerificacion servicioVerificacion)
    {
        _servicioVerificacion = servicioVerificacion;
    }

    // ------------------------------------------------------------------
    // E26 · POST /api/verificaciones — Presentar una credencial y verificarla
    // El body es {"vc": "<credencial firmada y serializada>"}.
    // ------------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Verificar([FromBody] VerificacionSolicitar peticion)
    {
        try
        {
            // Ejecuta los cuatro controles independientes: ESTRUCTURA, PRUEBA_CRIPTOGRAFICA, EMISOR y ESTADO.
            // Un veredicto NEGATIVO (RECHAZADA o INDETERMINADA) NO es una excepción: la verificación se
            // ejecutó con éxito, así que la respuesta es 200 (contrato E26).
            // Un "vc" que no es JSON o está alterado tampoco es un error HTTP: el servicio lo refleja
            // en los controles (ESTRUCTURA o PRUEBA_CRIPTOGRAFICA en FALLO).
            var resultado = await _servicioVerificacion.VerificarAsync(peticion.Vc!);

            return Ok(new RespuestaVerificacion(
                200,
                resultado.Veredicto,
                resultado.Codigo,
                resultado.Controles));
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