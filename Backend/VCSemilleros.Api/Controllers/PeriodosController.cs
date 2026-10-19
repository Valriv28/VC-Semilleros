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
// Importa los modelos del dominio (Participante, Sesion, Evaluacion) para tipar los sobres de lista.
using VCSemilleros.Domain.Modelos;

// Espacio de nombres de los controladores de la capa Api.
namespace VCSemilleros.Api.Controllers;

// Controlador de todo lo que cuelga de un PERIODO:
//   participantes (E9, E10) · sesiones (E11, E12) · criterio vigente (E16) · evaluaciones del periodo (E18).
// Usa tres servicios porque cada tema es un caso de uso distinto (Art. 2): gestionar el periodo,
// registrar la asistencia (evidencia) y evaluar (decisión sobre la evidencia).
//
// [ApiController]: valida el body contra la petición ANTES de entrar al método (422 con la lista de errores).
// [Route("api")]: toda ruta de este controlador empieza con /api.
// [Authorize]: por defecto TODO exige un token JWT válido (sin token → 401); cada método puede afinar el rol.
[ApiController]
[Route("api")]
[Authorize]
public class PeriodosController : ControllerBase
{
    // Los servicios que necesita. "readonly": se asignan una sola vez, en el constructor.
    private readonly IServicioPeriodo _servicioPeriodo;
    private readonly IServicioAsistencia _servicioAsistencia;
    private readonly IServicioEvaluacion _servicioEvaluacion;
    private readonly IServicioCriterioPeriodo _servicioCriterioPeriodo;

    // Constructor: ASP.NET Core inyecta aquí las implementaciones registradas en Program.cs.
    public PeriodosController(
        IServicioPeriodo servicioPeriodo,
        IServicioAsistencia servicioAsistencia,
        IServicioEvaluacion servicioEvaluacion,
        IServicioCriterioPeriodo servicioCriterioPeriodo)
    {
        _servicioPeriodo = servicioPeriodo;
        _servicioAsistencia = servicioAsistencia;
        _servicioEvaluacion = servicioEvaluacion;
        _servicioCriterioPeriodo = servicioCriterioPeriodo;
    }

    // ------------------------------------------------------------------
    // E9 · GET /api/periodos/{periodoId}/participantes[?limite=N] — Listar participantes
    // El contrato no limita el rol: basta con haber ingresado.
    // ------------------------------------------------------------------
    [HttpGet("periodos/{periodoId:int}/participantes")]
    public async Task<IActionResult> ListarParticipantes(int periodoId, [FromQuery] int limite = 1000)
    {
        try
        {
            // Si el periodo no existe → 404; si limite <= 0 → 400 (ambos los decide el servicio).
            var participantes = await _servicioPeriodo.ListarParticipantesAsync(periodoId, limite);

            // Lista vacía → 204 sin cuerpo (contrato §0).
            if (participantes.Count == 0)
                return NoContent();

            return Ok(new RespuestaLista<Participante>("participante", limite, participantes.Count, participantes));
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
    // E10 · POST /api/periodos/{periodoId}/participantes — Registrar un participante · Rol: docente
    // ------------------------------------------------------------------
    [HttpPost("periodos/{periodoId:int}/participantes")]
    [Authorize(Roles = "DOCENTE")]
    public async Task<IActionResult> RegistrarParticipante(int periodoId, [FromBody] ParticipanteCrear peticion)
    {
        try
        {
            // El servicio comprueba: periodo y usuario existen (404) y el usuario es ESTUDIANTE (400).
            // Si el estudiante ya estaba en el periodo, la BD lo rechaza (UNIQUE) y sale como 500.
            var id = await _servicioPeriodo.RegistrarParticipanteAsync(periodoId, peticion.EstudianteId!.Value);
            return Ok(new RespuestaCreada(200, "Participante registrado exitosamente.", id));
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
    // E11 · GET /api/periodos/{periodoId}/sesiones[?limite=N] — Listar sesiones
    // ------------------------------------------------------------------
    [HttpGet("periodos/{periodoId:int}/sesiones")]
    public async Task<IActionResult> ListarSesiones(int periodoId, [FromQuery] int limite = 1000)
    {
        try
        {
            var sesiones = await _servicioAsistencia.ListarSesionesAsync(periodoId, limite);

            if (sesiones.Count == 0)
                return NoContent();

            return Ok(new RespuestaLista<Sesion>("sesion", limite, sesiones.Count, sesiones));
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
    // E12 · POST /api/periodos/{periodoId}/sesiones — Registrar una sesión · Rol: docente
    // ------------------------------------------------------------------
    [HttpPost("periodos/{periodoId:int}/sesiones")]
    [Authorize(Roles = "DOCENTE")]
    public async Task<IActionResult> RegistrarSesion(int periodoId, [FromBody] SesionCrear peticion)
    {
        try
        {
            // "descripcion" es opcional: si el body no la trae llega null y se guarda como NULL.
            var id = await _servicioAsistencia.RegistrarSesionAsync(periodoId, peticion.Fecha!.Value, peticion.Descripcion);
            return Ok(new RespuestaCreada(200, "Sesión registrada exitosamente.", id));
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
    // E16 · GET /api/periodos/{periodoId}/criterio-vigente — Criterio vigente (propuesta P1)
    // El criterio vigente es el de MAYOR versión entre las evaluaciones del periodo.
    // ------------------------------------------------------------------
    [HttpGet("periodos/{periodoId:int}/criterio-vigente")]
    [Authorize(Roles = "DOCENTE,COORDINADOR,ADMINISTRADOR")]
    public async Task<IActionResult> ObtenerCriterioVigente(int periodoId)
    {
        try
        {
            // La configuración persistida es la fuente principal, incluso antes de la primera evaluación.
            var configurado = await _servicioCriterioPeriodo.ObtenerAsync(periodoId);
            if (configurado is not null)
            {
                return Ok(new RespuestaCriterioPeriodo(
                    configurado.PeriodoId, configurado.CriterioVersion,
                    configurado.MinAsistenciaPct, configurado.EntregablesRequeridosPct,
                    configurado.PermanenciaMeses, configurado.DefinidoPor,
                    configurado.FechaActualizacion));
            }

            // Compatibilidad con períodos históricos que solo tengan evaluaciones guardadas.
            var historico = await _servicioEvaluacion.ObtenerCriterioVigenteAsync(periodoId);
            return Ok(new RespuestaCriterioVigente(
                periodoId, historico.CriterioVersion, historico.MinAsistenciaPct, historico.DefinidoPor));
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

    // PUT /api/periodos/{periodoId}/criterio-vigente — configurar criterios (solo coordinador).
    [HttpPut("periodos/{periodoId:int}/criterio-vigente")]
    [Authorize(Roles = "COORDINADOR")]
    public async Task<IActionResult> GuardarCriterio(int periodoId, [FromBody] CriterioPeriodoGuardar peticion)
    {
        if (User.ObtenerId() is not int coordinadorId)
            return StatusCode(403, new RespuestaError(403, "Acceso denegado.", "El token no contiene un identificador válido."));

        try
        {
            var criterio = await _servicioCriterioPeriodo.GuardarAsync(
                periodoId, coordinadorId, peticion.MinAsistenciaPct,
                peticion.EntregablesRequeridosPct, peticion.PermanenciaMeses);

            return Ok(new RespuestaCriterioPeriodo(
                criterio.PeriodoId, criterio.CriterioVersion, criterio.MinAsistenciaPct,
                criterio.EntregablesRequeridosPct, criterio.PermanenciaMeses,
                criterio.DefinidoPor, criterio.FechaActualizacion));
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
    // E18 · GET /api/periodos/{periodoId}/evaluaciones[?limite=N] — Evaluaciones del periodo
    // ------------------------------------------------------------------
    [HttpGet("periodos/{periodoId:int}/evaluaciones")]
    public async Task<IActionResult> ListarEvaluaciones(int periodoId, [FromQuery] int limite = 1000)
    {
        try
        {
            var evaluaciones = await _servicioEvaluacion.ListarPorPeriodoAsync(periodoId, limite);

            if (evaluaciones.Count == 0)
                return NoContent();

            return Ok(new RespuestaLista<Evaluacion>("evaluacion", limite, evaluaciones.Count, evaluaciones));
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