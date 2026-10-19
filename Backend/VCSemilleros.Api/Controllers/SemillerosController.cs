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

// Controlador de semilleros (E3 a E6) y de los periodos de un semillero (E7 y E8).
// Un controlador solo recibe la petición HTTP, llama al servicio y traduce el resultado (o la excepción)
// a un código HTTP. Las reglas de negocio NO viven aquí: viven en ServicioSemillero y ServicioPeriodo.
//
// [ApiController]: activa el comportamiento de API (valida el body contra la petición ANTES de entrar
//   al método; si falla, Program.cs lo responde como 422 con la lista de errores).
// [Route("api")]: toda ruta de este controlador empieza con /api; cada método agrega el resto.
// [Authorize]: por defecto TODO exige un token JWT válido (si no hay token, la respuesta es 401).
//   Cada método afina el rol permitido con [Authorize(Roles = "...")] (si el rol no sirve, 403).
[ApiController]
[Route("api")]
[Authorize]
public class SemillerosController : ControllerBase
{
    // Los servicios que necesita. "readonly": se asignan una sola vez, en el constructor.
    private readonly IServicioSemillero _servicioSemillero;
    private readonly IServicioPeriodo _servicioPeriodo;

    // Constructor: ASP.NET Core inyecta aquí las implementaciones registradas en Program.cs.
    // Este controlador no sabe qué clase concreta recibe: solo ve las interfaces.
    public SemillerosController(IServicioSemillero servicioSemillero, IServicioPeriodo servicioPeriodo)
    {
        _servicioSemillero = servicioSemillero;
        _servicioPeriodo = servicioPeriodo;
    }

    // ------------------------------------------------------------------
    // E3 · GET /api/semilleros[?limite=N] — Listar semilleros · Rol: administrador
    // ------------------------------------------------------------------
    [HttpGet("semilleros")]
    [Authorize(Roles = "ADMINISTRADOR")]
    // "limite = 1000": si la URL no trae ?limite=, vale 1000 (el valor por defecto del contrato).
    public async Task<IActionResult> Listar([FromQuery] int limite = 1000)
    {
        try
        {
            // Pide la lista al servicio. Si limite <= 0, el servicio lanza ArgumentException (400).
            var semilleros = await _servicioSemillero.ListarAsync(limite);

            // Lista vacía → 204 sin cuerpo (contrato §0).
            if (semilleros.Count == 0)
                return NoContent();

            // Lista con filas → 200 con el sobre { recurso, limite, total, datos }.
            return Ok(new RespuestaLista<Domain.Modelos.Semillero>("semillero", limite, semilleros.Count, semilleros));
        }
        // 404: algo pedido no existe.
        catch (NoEncontradoExcepcion ex)
        {
            return StatusCode(404, new RespuestaError(404, "Recurso no encontrado.", ex.Message));
        }
        // 400: una regla de negocio se rompió (aquí, limite <= 0).
        catch (ArgumentException ex)
        {
            return StatusCode(400, new RespuestaError(400, "Parámetros inválidos.", ex.Message));
        }
        // 500: cualquier otra falla (por ejemplo, la BD no responde). El mensaje del motor va en "detalle".
        catch (Exception ex)
        {
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }

    // ------------------------------------------------------------------
    // E4 · GET /api/semilleros/{id} — Obtener un semillero
    // El contrato no limita el rol: basta con haber ingresado (el [Authorize] de la clase).
    // ------------------------------------------------------------------
    [HttpGet("semilleros/{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        try
        {
            // Si no existe, el servicio lanza NoEncontradoExcepcion → 404.
            var semillero = await _servicioSemillero.ObtenerAsync(id);
            return Ok(semillero);
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
    // E5 · POST /api/semilleros — Registrar un semillero (body completo) · Rol: administrador
    // ------------------------------------------------------------------
    [HttpPost("semilleros")]
    [Authorize(Roles = "ADMINISTRADOR")]
    // [FromBody]: el objeto SemilleroCrear se arma con el JSON del body. Si no cumple sus anotaciones,
    // el 422 sale solo, antes de ejecutar este método.
    public async Task<IActionResult> Crear([FromBody] SemilleroCrear peticion)
    {
        try
        {
            // ".Value" saca el int de un "int?". Es seguro: [Required] ya garantizó que no es null.
            var id = await _servicioSemillero.CrearAsync(peticion.ResponsableId!.Value, peticion.Nombre!);
            return Ok(new RespuestaCreada(200, "Semillero registrado exitosamente.", id));
        }
        catch (NoEncontradoExcepcion ex)
        {
            // El responsable no existe → 404.
            return StatusCode(404, new RespuestaError(404, "Recurso no encontrado.", ex.Message));
        }
        catch (ArgumentException ex)
        {
            // El responsable existe pero no es DOCENTE → 400.
            return StatusCode(400, new RespuestaError(400, "Parámetros inválidos.", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RespuestaError(500, "Error interno.", ex.Message));
        }
    }

    // ------------------------------------------------------------------
    // E6 · PATCH /api/semilleros/{id}/estado — Activar o inactivar · Rol: administrador
    // PATCH = actualización PARCIAL: el body solo trae el campo que cambia ({"activo": ...}).
    // ------------------------------------------------------------------
    [HttpPatch("semilleros/{id:int}/estado")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] SemilleroEstado peticion)
    {
        try
        {
            var filas = await _servicioSemillero.CambiarEstadoAsync(id, peticion.Activo!.Value);
            return Ok(new RespuestaFilas(200, "Estado del semillero actualizado exitosamente.", filas));
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
    // E7 · GET /api/semilleros/{semilleroId}/periodos[?limite=N] — Listar periodos
    // Rol: docente / administrador
    // ------------------------------------------------------------------
    [HttpGet("semilleros/{semilleroId:int}/periodos")]
    [Authorize(Roles = "DOCENTE,ADMINISTRADOR")]
    public async Task<IActionResult> ListarPeriodos(int semilleroId, [FromQuery] int limite = 1000)
    {
        try
        {
            // Si el semillero no existe → 404; si limite <= 0 → 400 (ambos los decide el servicio).
            var periodos = await _servicioPeriodo.ListarPeriodosAsync(semilleroId, limite);

            if (periodos.Count == 0)
                return NoContent();

            return Ok(new RespuestaLista<Domain.Modelos.Periodo>("periodo", limite, periodos.Count, periodos));
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
    // E8 · POST /api/semilleros/{semilleroId}/periodos — Crear un periodo · Rol: docente responsable
    // ------------------------------------------------------------------
    [HttpPost("semilleros/{semilleroId:int}/periodos")]
    [Authorize(Roles = "DOCENTE")]
    public async Task<IActionResult> CrearPeriodo(int semilleroId, [FromBody] PeriodoCrear peticion)
    {
        try
        {
            // El servicio comprueba: semillero existe (404), está activo (400) y fechaFin >= fechaInicio (400).
            var id = await _servicioPeriodo.CrearPeriodoAsync(
                semilleroId,
                peticion.Nombre!,
                peticion.FechaInicio!.Value,
                peticion.FechaFin!.Value);

            return Ok(new RespuestaCreada(200, "Periodo creado exitosamente.", id));
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