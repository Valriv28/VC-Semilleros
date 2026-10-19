using VCSemilleros.Domain.Modelos;               // EstadoCredencial, RolUsuario.
using VCSemilleros.Domain.Repositorios;          // Los puertos de solo lectura que usa la verificación.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: verificar una credencial presentada (RF14, E26). Plan §4.8.
// Solo LEE: no escribe en la BD ni en la red. Un resultado negativo no lanza excepciones:
// los problemas de la credencial presentada son el resultado normal de este caso de uso.
public class ServicioVerificacion : IServicioVerificacion
{
    private readonly IServicioCredencialVerificable _servicioCredencial;
    private readonly IRepositorioCredencial _repositorioCredencial;
    private readonly IRepositorioEvaluacion _repositorioEvaluacion;
    private readonly IRepositorioUsuario _repositorioUsuario;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioVerificacion(
        IServicioCredencialVerificable servicioCredencial,
        IRepositorioCredencial repositorioCredencial,
        IRepositorioEvaluacion repositorioEvaluacion,
        IRepositorioUsuario repositorioUsuario)
    {
        _servicioCredencial = servicioCredencial;
        _repositorioCredencial = repositorioCredencial;
        _repositorioEvaluacion = repositorioEvaluacion;
        _repositorioUsuario = repositorioUsuario;
    }

    // E26: ejecuta los cuatro controles y calcula el veredicto.
    public async Task<ResultadoVerificacion> VerificarAsync(string vc)
    {
        var controles = new List<ControlVerificacion>();

        // Lee el texto como JSON. Si no es un objeto JSON válido, no hay nada más que analizar.
        var credencialPresentada = _servicioCredencial.Leer(vc);

        // ---------- Control 1: ESTRUCTURA ----------
        bool estructuraValida;
        if (credencialPresentada is null)
        {
            estructuraValida = false;
            controles.Add(new ControlVerificacion("ESTRUCTURA", ResultadoControl.FALLO,
                "El texto recibido no es una credencial en formato JSON."));
        }
        else
        {
            var faltantes = _servicioCredencial.ValidarEstructura(credencialPresentada);
            estructuraValida = faltantes.Count == 0;
            controles.Add(estructuraValida
                ? new ControlVerificacion("ESTRUCTURA", ResultadoControl.PASO, "La credencial cumple el perfil.")
                : new ControlVerificacion("ESTRUCTURA", ResultadoControl.FALLO,
                    "La credencial no cumple el perfil. Faltan: " + string.Join(", ", faltantes) + "."));
        }

        // ---------- Control 2: PRUEBA_CRIPTOGRAFICA ----------
        // Si la estructura falló, la firma queda NO_EVALUADO (así lo fija el contrato).
        if (!estructuraValida)
        {
            controles.Add(new ControlVerificacion("PRUEBA_CRIPTOGRAFICA", ResultadoControl.NO_EVALUADO,
                "No se evaluó porque la estructura de la credencial no es válida."));
        }
        else
        {
            // "!" le dice al compilador que aquí la credencial no es null (ya se comprobó arriba).
            controles.Add(_servicioCredencial.VerificarPrueba(credencialPresentada!)
                ? new ControlVerificacion("PRUEBA_CRIPTOGRAFICA", ResultadoControl.PASO, "La firma es válida e íntegra.")
                : new ControlVerificacion("PRUEBA_CRIPTOGRAFICA", ResultadoControl.FALLO,
                    "La firma no corresponde al contenido: la credencial fue alterada o no la emitió este sistema."));
        }

        // Busca la credencial en la BD por el código leído del documento (si se pudo leer).
        var codigo = credencialPresentada is null ? null : _servicioCredencial.ObtenerCodigo(credencialPresentada);
        var credencialRegistrada = codigo is null ? null : await _repositorioCredencial.ObtenerPorCodigoAsync(codigo);

        // ---------- Control 3: EMISOR ----------
        controles.Add(await EvaluarEmisorAsync(credencialPresentada, credencialRegistrada));

        // ---------- Control 4: ESTADO ----------
        controles.Add(EvaluarEstado(credencialRegistrada));

        return new ResultadoVerificacion(CalcularVeredicto(controles), codigo, controles);
    }

    // Control EMISOR: el emisor declarado debe ser el coordinador ACTIVO que declaró la elegibilidad.
    // LÍMITE (P9): solo se comprueba el estado ACTUAL del coordinador; la BD no guarda su historial.
    private async Task<ControlVerificacion> EvaluarEmisorAsync(
        System.Text.Json.Nodes.JsonObject? credencialPresentada, Credencial? credencialRegistrada)
    {
        // Sin credencial legible o sin registro en la BD no hay contra qué comparar.
        if (credencialPresentada is null || credencialRegistrada is null)
            return new ControlVerificacion("EMISOR", ResultadoControl.NO_EVALUADO,
                "No se pudo comprobar el emisor: la credencial no está registrada en el sistema.");

        var evaluacion = await _repositorioEvaluacion.ObtenerPorIdAsync(credencialRegistrada.EvaluacionId);
        if (evaluacion is null || evaluacion.ElegibleDeclaradoPor is null)
            return new ControlVerificacion("EMISOR", ResultadoControl.FALLO,
                "La credencial no tiene una elegibilidad declarada por un coordinador.");

        var coordinador = await _repositorioUsuario.ObtenerPorIdAsync(evaluacion.ElegibleDeclaradoPor.Value);
        if (coordinador is null || coordinador.Rol != RolUsuario.COORDINADOR)
            return new ControlVerificacion("EMISOR", ResultadoControl.FALLO,
                "Quien declaró la elegibilidad no es un coordinador.");

        if (!coordinador.Activo)
            return new ControlVerificacion("EMISOR", ResultadoControl.FALLO,
                "El coordinador que declaró la elegibilidad ya no está activo.");

        // El emisor que dice el documento debe ser exactamente ese coordinador.
        if (_servicioCredencial.ObtenerIdEmisor(credencialPresentada) != _servicioCredencial.IdEmisor(coordinador.Id))
            return new ControlVerificacion("EMISOR", ResultadoControl.FALLO,
                "El emisor del documento no coincide con el coordinador registrado.");

        return new ControlVerificacion("EMISOR", ResultadoControl.PASO, "El emisor está autorizado.");
    }

    // Control ESTADO: VIGENTE = PASO · SUSPENDIDA o REVOCADA = FALLO · desconocida o INDETERMINADA = NO_EVALUADO.
    private static ControlVerificacion EvaluarEstado(Credencial? credencialRegistrada)
    {
        if (credencialRegistrada is null)
            return new ControlVerificacion("ESTADO", ResultadoControl.NO_EVALUADO,
                "La credencial no está registrada: no se pudo conocer su estado.");

        // "switch" con "=>" es una expresión que elige un resultado según el valor del estado.
        return credencialRegistrada.Estado switch
        {
            EstadoCredencial.VIGENTE => new ControlVerificacion("ESTADO", ResultadoControl.PASO,
                "La credencial está VIGENTE."),
            EstadoCredencial.SUSPENDIDA => new ControlVerificacion("ESTADO", ResultadoControl.FALLO,
                "La credencial está SUSPENDIDA."),
            EstadoCredencial.REVOCADA => new ControlVerificacion("ESTADO", ResultadoControl.FALLO,
                "La credencial está REVOCADA."),
            _ => new ControlVerificacion("ESTADO", ResultadoControl.NO_EVALUADO,
                "El estado de la credencial es INDETERMINADA.")
        };
    }

    // Regla de RF14: RECHAZADA si algún control falla; INDETERMINADA si ninguno falla y alguno quedó sin evaluar;
    // VERIFICADA si todos pasan.
    private static Veredicto CalcularVeredicto(List<ControlVerificacion> controles)
    {
        if (controles.Any(c => c.Resultado == ResultadoControl.FALLO))
            return Veredicto.RECHAZADA;

        if (controles.Any(c => c.Resultado == ResultadoControl.NO_EVALUADO))
            return Veredicto.INDETERMINADA;

        return Veredicto.VERIFICADA;
    }
}