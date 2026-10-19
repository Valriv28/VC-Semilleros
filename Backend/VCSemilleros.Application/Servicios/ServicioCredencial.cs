using VCSemilleros.Domain.Excepciones;           // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;               // Credencial, EstadoCredencial, EstadoAnclaje, RolUsuario.
using VCSemilleros.Domain.Repositorios;          // Los puertos que usa este caso de uso.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: consultar y revocar credenciales (RF11, RF12, RF13; E21 a E25).
public class ServicioCredencial : IServicioCredencial
{
    private readonly IRepositorioCredencial _repositorioCredencial;
    private readonly IRepositorioEvaluacion _repositorioEvaluacion;
    private readonly IRepositorioUsuario _repositorioUsuario;
    private readonly IRepositorioAnclaje _repositorioAnclaje;
    private readonly IClienteStellar _clienteStellar;
    private readonly IServicioCredencialVerificable _servicioCredencial;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioCredencial(
        IRepositorioCredencial repositorioCredencial,
        IRepositorioEvaluacion repositorioEvaluacion,
        IRepositorioUsuario repositorioUsuario,
        IRepositorioAnclaje repositorioAnclaje,
        IClienteStellar clienteStellar,
        IServicioCredencialVerificable servicioCredencial)
    {
        _repositorioCredencial = repositorioCredencial;
        _repositorioEvaluacion = repositorioEvaluacion;
        _repositorioUsuario = repositorioUsuario;
        _repositorioAnclaje = repositorioAnclaje;
        _clienteStellar = clienteStellar;
        _servicioCredencial = servicioCredencial;
    }

    // E21: lista las credenciales de un titular.
    public async Task<List<Credencial>> ListarPorTitularAsync(int titularId, int limite)
    {
        if (limite <= 0)
            throw new ArgumentException("El límite debe ser mayor que cero.");

        return await _repositorioCredencial.ObtenerPorTitularAsync(titularId, limite);
    }

    // E22: obtiene una credencial por su código (404 si no existe).
    public async Task<Credencial> ObtenerAsync(string codigo)
    {
        var credencial = await _repositorioCredencial.ObtenerPorCodigoAsync(codigo);
        if (credencial is null)
            throw new NoEncontradoExcepcion($"No existe una credencial con código = {codigo}");

        return credencial;
    }

    // E23: reconstruye la credencial firmada.
    public async Task<string> ObtenerVcAsync(string codigo)
    {
        var credencial = await ObtenerAsync(codigo);

        var evaluacion = await _repositorioEvaluacion.ObtenerPorIdAsync(credencial.EvaluacionId)
            ?? throw new NoEncontradoExcepcion($"No existe una evaluación con id = {credencial.EvaluacionId}");

        // Se reconstruye con la MISMA fecha de emisión que se firmó la primera vez.
        var datos = await _servicioCredencial.CargarDatosAsync(evaluacion, credencial.Codigo, credencial.FechaEmision);
        var contenido = _servicioCredencial.Construir(datos);

        var faltantes = _servicioCredencial.ValidarEstructura(contenido);
        if (faltantes.Count > 0)
            throw new InvalidOperationException(
                "La credencial reconstruida está incompleta. Faltan: " + string.Join(", ", faltantes) + ".");

        // Comprobación de integridad: el contenido reconstruido debe dar el mismo hash que se registró al emitir.
        // Si no coincide, los datos cambiaron después de emitir (por ejemplo, el nombre del titular).
        // No es un error del usuario sino de los datos: responde 500 (InvalidOperationException).
        if (_servicioCredencial.CalcularHash(contenido) != credencial.VcHash)
            throw new InvalidOperationException(
                "La credencial reconstruida no coincide con el hash registrado: los datos cambiaron después de emitirla.");

        // ECDSA no es determinista: la firma que sale aquí es DISTINTA de la original, pero igualmente válida.
        return _servicioCredencial.Serializar(_servicioCredencial.Firmar(contenido));
    }

    // E24: devuelve solo el estado vigente.
    public async Task<EstadoCredencial> ObtenerEstadoAsync(string codigo)
    {
        var credencial = await ObtenerAsync(codigo);
        return credencial.Estado;
    }

    // E25: revoca una credencial.
    public async Task<int> RevocarAsync(string codigo, int revocadaPor, string motivo)
    {
        // Regla de respaldo: la petición ya exige un motivo (422), pero el servicio no depende de eso (400).
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la revocación es obligatorio.");

        // 1) La credencial y quien revoca deben existir (404).
        var credencial = await ObtenerAsync(codigo);

        var usuario = await _repositorioUsuario.ObtenerPorIdAsync(revocadaPor)
            ?? throw new NoEncontradoExcepcion($"No existe un usuario con id = {revocadaPor}");

        // 2) Regla de negocio: solo un COORDINADOR revoca (400).
        if (usuario.Rol != RolUsuario.COORDINADOR)
            throw new ArgumentException("Quien revoca debe ser un usuario con rol COORDINADOR.");

        // 3) Regla de negocio: no se revoca dos veces (400).
        if (credencial.Estado == EstadoCredencial.REVOCADA)
            throw new ArgumentException("La credencial ya está revocada.");

        // 4) La BD es la fuente de verdad: se revoca primero allí.
        var filas = await _repositorioCredencial.RevocarAsync(codigo, revocadaPor, motivo.Trim());

        // 5) Después se avisa a la red, en "mejor esfuerzo": si la red falla, la revocación en la BD
        // NO se deshace y el endpoint responde 200. No hay dónde registrar el fallo (el enum estado_anclaje
        // no tiene un valor para eso), así que solo se intenta.
        var anclaje = await _repositorioAnclaje.ObtenerPorCredencialAsync(codigo);
        if (anclaje is not null && anclaje.Estado == EstadoAnclaje.ANCLADO && !string.IsNullOrEmpty(anclaje.AttestationUid))
        {
            try
            {
                await _clienteStellar.RevocarAsync(codigo, anclaje.AttestationUid);
            }
            catch
            {
                // Se ignora a propósito: ver el comentario del paso 5.
            }
        }

        return filas;
    }
}