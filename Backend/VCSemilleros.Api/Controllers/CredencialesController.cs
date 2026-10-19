// Importa [Authorize]: la anotación que exige un token JWT válido (y, si se indica, un rol).
using Microsoft.AspNetCore.Authorization;
// Importa ControllerBase, [ApiController], [HttpGet], [HttpPost], [Route], [FromQuery], IActionResult...
using Microsoft.AspNetCore.Mvc;
// Importa nuestros DTOs: las peticiones de entrada y los sobres de respuesta.
using VCSemilleros.Api.Peticiones;
// Importa los servicios (casos de uso) de Application. El controlador solo conoce INTERFACES (Art. 4).
using VCSemilleros.Application.Servicios;
// Importa NoEncontradoExcepcion, que el controlador traduce a 404.
using VCSemilleros.Domain.Excepciones;
// Importa los modelos del dominio (Credencial) para tipar el sobre de lista.
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de las credenciales YA EMITIDAS: consultarlas (E21 a E24) y revocarlas (E25).
// Art. 2: la credencial es un artefacto distinto de la evidencia de asistencia y de la elegibilidad;
// aquí solo se consulta su estado o se cambia por una revocación. Emitirla es E20, en otro controlador.

[ApiController]
[Route("api/credenciales")]
[Authorize]
public class CredencialesController : ControllerBase
{
    // El servicio que necesita. "readonly": se asigna una sola vez, en el constructor.
    private readonly IServicioCredencial _servicioCredencial;

    // Constructor: ASP.NET Core inyecta aquí la implementación registrada en Program.cs.
    public CredencialesController(IServicioCredencial servicioCredencial)
    {
        _servicioCredencial = servicioCredencial;
    }

    // ------------------------------------------------------------------
    // E21 · GET /api/credenciales?titularId={id}[&limite=N] — Mis credenciales
    // Rol del contrato: estudiante. Se permiten también COORDINADOR y ADMINISTRADOR porque el plan §7
    // dice que sus pantallas (revocación y "Credenciales") listan credenciales con este mismo endpoint.
    // ------------------------------------------------------------------
    [HttpGet]
    [Authorize(Roles = "ESTUDIANTE,COORDINADOR,ADMINISTRADOR")]
    // "int? titularId": entero que puede ser null. El contrato lo declara OBLIGATORIO, así que
    // si no viene se responde 422 a mano (más abajo).
    public async Task<IActionResult> ListarPorTitular([FromQuery] int? titularId, [FromQuery] int limite = 1000)
    {
        // El titularId es obligatorio: sin él, el contrato pide 422 con la lista de errores.
        // Es el mismo formato que usa Program.cs para el 422 de los bodies: { estado, mensaje, errores }.
        if (titularId is null)
        {
            return StatusCode(422, new RespuestaError(
                422,
                "Datos inválidos.",
                Errores: new List<string> { "El campo titularId es obligatorio." }));
        }

        // Un estudiante solo puede consultar sus propias credenciales, aunque manipule la URL.
        if (User.IsInRole("ESTUDIANTE") &&
            (User.ObtenerId() is not int titularAutenticado || titularId.Value != titularAutenticado))
        {
            return StatusCode(403, new RespuestaError(
                403, "Acceso denegado.", "Solo puede consultar las credenciales de su propia cuenta."));
        }

        try
        {
            // Si limite <= 0, el servicio lanza ArgumentException (400).
            var credenciales = await _servicioCredencial.ListarPorTitularAsync(titularId.Value, limite);

            // Lista vacía → 204 sin cuerpo (contrato §0).
            if (credenciales.Count == 0)
                return NoContent();

            return Ok(new RespuestaLista<Credencial>("credencial", limite, credenciales.Count, credenciales));
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
    // E22 · GET /api/credenciales/{codigo} — Obtener una credencial
    // El código es texto (por ejemplo "CRED-2026-3F9A12BC"), por eso la ruta no lleva ":int".
    // ------------------------------------------------------------------
    [HttpGet("{codigo}")]
    public async Task<IActionResult> Obtener(string codigo)
    {
        try
        {
            var credencial = await _servicioCredencial.ObtenerAsync(codigo);
            return Ok(credencial);
        }
        catch (NoEncontradoExcepcion ex)
        {
            // El contrato E22 fija este mensaje exacto para el 404.
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

    // E23 · GET /api/credenciales/{codigo}/vc — obtener la credencial firmada.
    // CAMBIO: la credencial completa lleva nombre y correo del titular, así que ya no la puede pedir
    // cualquier usuario autenticado. La pueden pedir: el TITULAR (un ESTUDIANTE dueño de esa credencial),
    // un COORDINADOR y el ADMINISTRADOR. Cualquier otro recibe 403.
    [HttpGet("{codigo}/vc")]
    public async Task<IActionResult> ObtenerVc(string codigo)
    {
        try
        {
            // Primero se carga la credencial: sirve para saber quién es su titular y da el 404 si no existe.
            var credencial = await _servicioCredencial.ObtenerAsync(codigo);

            // IsInRole mira el claim "rol" del token (Program.cs lo declara como RoleClaimType).
            // Es titular solo si es ESTUDIANTE y su id es el del titular: el rol se comprueba también
            // porque el id de un ADMINISTRADOR sale de OTRA tabla y podría coincidir por casualidad.
            var esTitular = User.IsInRole("ESTUDIANTE") && credencial.TitularId == User.ObtenerId();

            if (!esTitular && !User.IsInRole("COORDINADOR") && !User.IsInRole("ADMINISTRADOR"))
            {
                return StatusCode(403, new RespuestaError(
                    403, "Acceso denegado.", "Solo el titular, un coordinador o el administrador pueden obtener la credencial firmada."));
            }

            var vc = await _servicioCredencial.ObtenerVcAsync(codigo);
            return Ok(new RespuestaCredencialVc(codigo, vc));
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
    // E24 · GET /api/credenciales/{codigo}/estado — Estado vigente de la credencial
    // ------------------------------------------------------------------
    [HttpGet("{codigo}/estado")]
    public async Task<IActionResult> ObtenerEstado(string codigo)
    {
        try
        {
            var estado = await _servicioCredencial.ObtenerEstadoAsync(codigo);
            return Ok(new RespuestaEstadoCredencial(codigo, estado));
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

    // E25 · POST /api/credenciales/{codigo}/revocacion — revocar con motivo. Rol: COORDINADOR.
    // CAMBIO: además del rol, el "revocadaPor" del body debe ser la MISMA persona que inició sesión.
    // Así la revocación no puede quedar registrada a nombre de otro coordinador.
    [HttpPost("{codigo}/revocacion")]
    [Authorize(Roles = "COORDINADOR")]
    public async Task<IActionResult> Revocar(string codigo, [FromBody] CredencialRevocar peticion)
    {
        // Se compara ANTES de llamar al servicio: si no coincide, no se toca nada. Responde 403.
        if (peticion.RevocadaPor != User.ObtenerId())
        {
            return StatusCode(403, new RespuestaError(
                403, "Acceso denegado.", "El campo revocadaPor debe ser el id de la persona que inició sesión."));
        }

        try
        {
            var filas = await _servicioCredencial.RevocarAsync(codigo, peticion.RevocadaPor!.Value, peticion.Motivo!);
            return Ok(new RespuestaFilas(200, "Credencial revocada exitosamente.", filas));
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