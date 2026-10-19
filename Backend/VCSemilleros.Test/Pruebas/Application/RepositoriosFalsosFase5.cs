using VCSemilleros.Domain.Modelos;              // Credencial, AnclajeStellar, estados.
using VCSemilleros.Domain.Repositorios;         // Los puertos que estas clases falsas implementan.

namespace VCSemilleros.Tests.Pruebas.Application;

// REPOSITORIOS Y CLIENTE FALSOS de la Fase 5: guardan todo en memoria (sin PostgreSQL ni red).
// Se reutiliza la clase "Ids" de RepositoriosFalsos.cs (ambas están en el mismo proyecto de pruebas).

// ---------- Credencial ----------
public class RepositorioCredencialFalso : IRepositorioCredencial
{
    public List<Credencial> Credenciales { get; } = new();

    public Task<Credencial?> ObtenerPorCodigoAsync(string codigo)
        => Task.FromResult(Credenciales.FirstOrDefault(c => c.Codigo == codigo));

    public Task<Credencial?> ObtenerPorEvaluacionAsync(int evaluacionId)
        => Task.FromResult(Credenciales.FirstOrDefault(c => c.EvaluacionId == evaluacionId));

    public Task<List<Credencial>> ObtenerPorTitularAsync(int titularId, int limite)
        => Task.FromResult(Credenciales.Where(c => c.TitularId == titularId)
                                       .OrderBy(c => c.FechaEmision).ThenBy(c => c.Codigo)
                                       .Take(limite).ToList());

    public Task<int> CrearAsync(Credencial credencial)
    {
        // Imita el UNIQUE (evaluacion_id) y la clave primaria (codigo) de la BD.
        if (Credenciales.Any(c => c.EvaluacionId == credencial.EvaluacionId || c.Codigo == credencial.Codigo))
            throw new InvalidOperationException("duplicate key value violates unique constraint (credencial)");

        Credenciales.Add(credencial);
        return Task.FromResult(1);
    }

    // Imita el UPDATE del repositorio real: estado REVOCADA y las tres columnas de revocación.
    public Task<int> RevocarAsync(string codigo, int revocadaPor, string motivo)
    {
        var credencial = Credenciales.FirstOrDefault(c => c.Codigo == codigo);
        if (credencial is null) return Task.FromResult(0);

        credencial.Estado = EstadoCredencial.REVOCADA;
        credencial.RevocadaPor = revocadaPor;
        credencial.MotivoRevocacion = motivo;
        credencial.FechaRevocacion = DateTime.Now;
        return Task.FromResult(1);
    }
}

// ---------- Anclaje ----------
public class RepositorioAnclajeFalso : IRepositorioAnclaje
{
    public List<AnclajeStellar> Anclajes { get; } = new();

    public Task<int> CrearAsync(AnclajeStellar anclaje)
    {
        anclaje.Id = Ids.Siguiente(Anclajes.Select(a => a.Id));
        anclaje.FechaSolicitud = DateTime.Now;     // La BD aplica DEFAULT now().
        Anclajes.Add(anclaje);
        return Task.FromResult(anclaje.Id);
    }

    // El más reciente de la credencial, como el ORDER BY id DESC LIMIT 1 del SQL real.
    public Task<AnclajeStellar?> ObtenerPorCredencialAsync(string credencialCodigo)
        => Task.FromResult(Anclajes.Where(a => a.CredencialCodigo == credencialCodigo)
                                   .OrderByDescending(a => a.Id).FirstOrDefault());

    public Task<int> ActualizarResultadoAsync(int id, EstadoAnclaje estado, string? txHash, string? attestationUid)
    {
        var anclaje = Anclajes.FirstOrDefault(a => a.Id == id);
        if (anclaje is null) return Task.FromResult(0);

        anclaje.Estado = estado;
        anclaje.TxHash = txHash;
        anclaje.AttestationUid = attestationUid;
        return Task.FromResult(1);
    }
}

// ---------- Cliente de Stellar ----------
// Imita la red: cada prueba decide si "responde" o no, y puede alterar lo que la red tiene guardado.
public class ClienteStellarFalso : IClienteStellar
{
    // false = simula que la red (o el puente) no responde.
    public bool Disponible { get; set; } = true;

    // Lo que la "red" tiene guardado, por attestationUid. Las pruebas pueden modificarlo
    // (por ejemplo, para simular un hash alterado).
    public Dictionary<string, EvidenciaEnRed> Red { get; } = new();

    // Registro de las llamadas recibidas, para comprobar que el servicio sí le pidió algo a la red.
    public List<(string Codigo, string EvidenciaHash)> Anclados { get; } = new();
    public List<(string Codigo, string AttestationUid)> Revocados { get; } = new();

    public Task<ResultadoAnclaje> AnclarAsync(string credencialCodigo, string evidenciaHash)
    {
        Anclados.Add((credencialCodigo, evidenciaHash));

        if (!Disponible)
            return Task.FromResult(new ResultadoAnclaje(false, null, null, "La red no respondió."));

        var uid = "uid-" + credencialCodigo;
        Red[uid] = new EvidenciaEnRed(uid, evidenciaHash, false);
        return Task.FromResult(new ResultadoAnclaje(true, "tx-" + credencialCodigo, uid, null));
    }

    public Task<ResultadoRevocacion> RevocarAsync(string credencialCodigo, string attestationUid)
    {
        Revocados.Add((credencialCodigo, attestationUid));

        if (!Disponible)
            return Task.FromResult(new ResultadoRevocacion(false, null, "La red no respondió."));

        if (Red.TryGetValue(attestationUid, out var actual))
            Red[attestationUid] = actual with { Revocada = true };   // "with" copia el record cambiando un campo.

        return Task.FromResult(new ResultadoRevocacion(true, "tx-rev-" + credencialCodigo, null));
    }

    public Task<EvidenciaEnRed?> ConsultarAsync(string attestationUid)
    {
        if (!Disponible)
            return Task.FromResult<EvidenciaEnRed?>(null);

        return Task.FromResult(Red.TryGetValue(attestationUid, out var evidencia) ? evidencia : null);
    }
}