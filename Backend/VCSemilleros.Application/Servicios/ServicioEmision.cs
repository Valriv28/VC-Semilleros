using System.Globalization;                      // CultureInfo: formatear la fecha igual en cualquier computador.
using System.Security.Cryptography;              // SHA256: calcula la huella de la evidencia (viene con .NET).
using System.Text;                               // Encoding.UTF8: convierte texto a bytes.
using VCSemilleros.Domain.Excepciones;           // NoEncontradoExcepcion (404).
using VCSemilleros.Domain.Modelos;               // Credencial, AnclajeStellar, estados.
using VCSemilleros.Domain.Repositorios;          // Los puertos: evaluación, credencial, anclaje y Stellar.

namespace VCSemilleros.Application.Servicios;

// Caso de uso: emitir la credencial (RF10, E20). Ejecuta el flujo de 9 pasos del plan §4.7.
// Solo conoce interfaces: no sabe si Stellar es un puente, una transacción o un contrato (Art. 4).
public class ServicioEmision : IServicioEmision
{
    private readonly IRepositorioEvaluacion _repositorioEvaluacion;
    private readonly IRepositorioCredencial _repositorioCredencial;
    private readonly IRepositorioAnclaje _repositorioAnclaje;
    private readonly IClienteStellar _clienteStellar;
    private readonly IServicioCredencialVerificable _servicioCredencial;

    // Constructor: recibe las interfaces por inyección de dependencias.
    public ServicioEmision(
        IRepositorioEvaluacion repositorioEvaluacion,
        IRepositorioCredencial repositorioCredencial,
        IRepositorioAnclaje repositorioAnclaje,
        IClienteStellar clienteStellar,
        IServicioCredencialVerificable servicioCredencial)
    {
        _repositorioEvaluacion = repositorioEvaluacion;
        _repositorioCredencial = repositorioCredencial;
        _repositorioAnclaje = repositorioAnclaje;
        _clienteStellar = clienteStellar;
        _servicioCredencial = servicioCredencial;
    }

    // E20: emite la credencial de una evaluación.
    public async Task<ResultadoEmision> EmitirAsync(int evaluacionId)
    {
        // PASO 1: carga la evaluación (404) y exige elegibilidad declarada y que no tenga credencial (400).
        var evaluacion = await _repositorioEvaluacion.ObtenerPorIdAsync(evaluacionId);
        if (evaluacion is null)
            throw new NoEncontradoExcepcion($"No existe una evaluación con id = {evaluacionId}");

        // Art. 2: la elegibilidad es una decisión previa y separada; aquí solo se exige.
        if (evaluacion.ElegibleDeclaradoPor is null)
            throw new ArgumentException("La evaluación no tiene la elegibilidad declarada; no puede emitirse la credencial.");

        var existente = await _repositorioCredencial.ObtenerPorEvaluacionAsync(evaluacionId);
        if (existente is not null)
            throw new ArgumentException($"La evaluación ya tiene una credencial emitida ({existente.Codigo}).");

        // La fecha se decide AHORA, antes de firmar, porque forma parte del contenido firmado.
        // Se trunca a segundos para que lo guardado en la BD sea idéntico a lo firmado, y se marca como
        // "Unspecified" porque la columna es timestamp sin zona horaria (Npgsql lo exige así).
        var ahora = DateTime.Now;
        var fechaEmision = new DateTime(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Unspecified);

        // Código de la credencial (propuesta P6): CRED-AAAA-XXXXXXXX, con 8 caracteres hexadecimales.
        var codigo = $"CRED-{fechaEmision.Year}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // PASO 2: construye la credencial con los datos autorizados.
        var datos = await _servicioCredencial.CargarDatosAsync(evaluacion, codigo, fechaEmision);
        var contenido = _servicioCredencial.Construir(datos);

        // PASO 3: valida la estructura. Si está incompleta responde 400 y NO avanza a firmar.
        var faltantes = _servicioCredencial.ValidarEstructura(contenido);
        if (faltantes.Count > 0)
            throw new ArgumentException(
                "La credencial construida está incompleta; no se firma. Faltan: " + string.Join(", ", faltantes) + ".");

        // PASOS 4 y 5: firma y serializa. El hash se calcula sobre el contenido SIN la prueba.
        var firmada = _servicioCredencial.Firmar(contenido);
        var vc = _servicioCredencial.Serializar(firmada);
        var vcHash = _servicioCredencial.CalcularHash(contenido);

        // PASO 6: registra la credencial (VIGENTE). Guarda solo el hash, no la credencial completa.
        var credencial = new Credencial
        {
            Codigo = codigo,
            EvaluacionId = evaluacionId,
            TitularId = datos.TitularId,
            Tipo = ConstantesCredencial.Tipo,
            Estado = EstadoCredencial.VIGENTE,
            VcHash = vcHash,
            FechaEmision = fechaEmision
        };
        await _repositorioCredencial.CrearAsync(credencial);

        // PASO 7: registra el anclaje en PENDIENTE (HU-C04-01).
        // La evidencia es una huella de (código + hash de la credencial + fecha): NO lleva datos personales
        // ni la credencial completa (Art. 5). El "|" separa los tres valores para que no se confundan.
        var evidenciaHash = CalcularEvidenciaHash(codigo, vcHash, fechaEmision);
        var anclajeId = await _repositorioAnclaje.CrearAsync(new AnclajeStellar
        {
            CredencialCodigo = codigo,
            EvidenciaHash = evidenciaHash,
            Estado = EstadoAnclaje.PENDIENTE
        });

        // PASO 8: pide el anclaje a la red. Si falla, la credencial NO se revierte (P4):
        // queda emitida y el anclaje se guarda como FALLIDO.
        ResultadoAnclaje resultado;
        try
        {
            resultado = await _clienteStellar.AnclarAsync(codigo, evidenciaHash);
        }
        catch (Exception ex)
        {
            // El adaptador no debería lanzar excepciones, pero si lo hace no se pierde la credencial ya registrada.
            resultado = new ResultadoAnclaje(false, null, null, "No se pudo anclar en la red: " + ex.Message);
        }

        var estadoAnclaje = resultado.Exitoso ? EstadoAnclaje.ANCLADO : EstadoAnclaje.FALLIDO;
        await _repositorioAnclaje.ActualizarResultadoAsync(
            anclajeId, estadoAnclaje,
            resultado.Exitoso ? resultado.TxHash : null,
            resultado.Exitoso ? resultado.AttestationUid : null);

        // PASO 9: devuelve el código, los estados y la credencial firmada (HU-C02-10).
        return new ResultadoEmision(
            codigo, EstadoCredencial.VIGENTE, estadoAnclaje,
            resultado.Exitoso ? resultado.TxHash : null, vc);
    }

    // SHA-256 en hexadecimal de "código|vc_hash|fecha". Se escribe en minúsculas, como lo espera el puente.
    private static string CalcularEvidenciaHash(string codigo, string vcHash, DateTime fechaEmision)
    {
        var texto = string.Create(CultureInfo.InvariantCulture,
            $"{codigo}|{vcHash}|{fechaEmision:yyyy-MM-ddTHH:mm:ss}");

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
    }
}